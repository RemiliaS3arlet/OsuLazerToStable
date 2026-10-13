using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace OsuLazerToStable;

public class TransferOptions
{
    public bool TransferMaps = true;
    public bool TransferCollections = true;
    public bool DryRun = false;
    public bool MergeCollections = true;
}

public class TransferSummary
{
    public int TotalSets;
    public int CopiedSets;
    public int SkippedSets;
    public long CopiedFiles;
    public long CopiedBytes;
    public int CollectionsWritten;
    public int CollectionHashesWritten;
    public int CollectionHashesMissing;
    public string CollectionBackupPath = string.Empty;
}

public class TransferEngine
{
    public Action<string>? Log;
    public Action<int, int>? Progress; // current,total（文件粒度）

    private CancellationToken _ct;
    private TransferOptions _opts = new();
    private readonly TransferSummary _sum = new();
    private string _filesDir = string.Empty;
    private string _songsDir = string.Empty;

    public TransferSummary Run(string lazerDir, string stableDir, TransferOptions opts, CancellationToken ct)
    {
        _opts = opts;
        _ct = ct;
        _filesDir = Path.Combine(lazerDir, "files");
        _songsDir = Path.Combine(stableDir, "Songs");
        if (!Directory.Exists(_songsDir))
            throw new DirectoryNotFoundException($"stable 的 Songs 目录不存在: {_songsDir}");

        Log?.Invoke("== 读取 osu!lazer 数据 ==");
        var lazer = LazerData.Read(lazerDir, Log);
        Log?.Invoke($"lazer 谱面集: {lazer.Sets.Count}（另有 {lazer.DeletedSets} 个已删除），收藏夹: {lazer.Collections.Count}");

        var existingIds = StableData.GetExistingOnlineIds(_songsDir);
        Log?.Invoke($"stable 已有曲目文件夹: {existingIds.Count}");

        var md5Index = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (opts.TransferCollections)
            md5Index = StableData.BuildMd5Index(_songsDir, Log, ct);

        if (opts.TransferMaps)
            TransferMaps(lazer.Sets, existingIds, md5Index);

        if (opts.TransferCollections)
            TransferCollections(lazer.Collections, stableDir, md5Index);

        return _sum;
    }

    private void TransferMaps(List<LazerSet> sets, HashSet<int> existingIds, HashSet<string> md5Index)
    {
        Log?.Invoke("== 迁移谱面 ==");

        if (!_opts.DryRun)
            PreflightWriteCheck();

        var existingNames = new HashSet<string>(
            Directory.EnumerateDirectories(_songsDir).Select(Path.GetFileName).Where(n => n != null)!,
            StringComparer.OrdinalIgnoreCase);

        var plan = sets.Where(s => !(s.OnlineId > 0 && existingIds.Contains(s.OnlineId))).ToList();
        _sum.TotalSets = plan.Count;
        int skippedExisting = sets.Count - plan.Count;
        Log?.Invoke($"需要迁移 {plan.Count} 个谱面集（{skippedExisting} 个在 stable 中已存在，自动跳过）");

        int fileTotal = Math.Max(1, plan.Sum(s => s.Files.Count));
        int fileDone = 0;
        Progress?.Invoke(0, fileTotal);

        foreach (var set in plan)
        {
            _ct.ThrowIfCancellationRequested();

            if (set.Files.Count == 0)
            {
                Log?.Invoke($"[跳过] 空集 {set.SetHash}");
                _sum.SkippedSets++;
                continue;
            }

            var (title, artist) = ReadSetMetadata(set);
            string folder = BuildFolderName(set, title, artist, existingNames);
            existingNames.Add(folder);

            string destDir = Path.Combine(_songsDir, folder);
            if (Directory.Exists(destDir))
            {
                Log?.Invoke($"[跳过] 目录已存在: {folder}");
                _sum.SkippedSets++;
                continue;
            }

            if (_opts.DryRun)
            {
                Log?.Invoke($"[演练] 将创建 {folder}（{set.Files.Count} 个文件）");
                // 演练模式下也为 .osu 计算 MD5，保证收藏夹演练统计准确
                foreach (var f in set.Files)
                {
                    _ct.ThrowIfCancellationRequested();
                    fileDone++;
                    Progress?.Invoke(fileDone, fileTotal);
                    if (!f.IsOsu) continue;
                    string src = GetStorePath(f.Hash);
                    if (!File.Exists(src)) continue;
                    try { md5Index.Add(StableData.Md5OfBytes(File.ReadAllBytes(src))); } catch { }
                }
                _sum.CopiedSets++;
                continue;
            }

            Directory.CreateDirectory(destDir);
            int copiedInSet = 0;
            try
            {
                foreach (var f in set.Files)
                {
                    _ct.ThrowIfCancellationRequested();
                    fileDone++;
                    Progress?.Invoke(fileDone, fileTotal);

                    string rel = SanitizeRelPath(f.Filename);
                    if (rel.Length == 0)
                    {
                        Log?.Invoke($"[警告] 无效文件名，跳过: '{f.Filename}'");
                        continue;
                    }

                    string src = GetStorePath(f.Hash);
                    if (!File.Exists(src))
                    {
                        Log?.Invoke($"[警告] 仓库缺少文件: {f.Filename} ({f.Hash})");
                        continue;
                    }

                    string dest = Path.Combine(destDir, rel);
                    if (File.Exists(dest))
                    {
                        Log?.Invoke($"[警告] 目标已存在，跳过: {rel}");
                        continue;
                    }

                    string? parent = Path.GetDirectoryName(dest);
                    if (!string.IsNullOrEmpty(parent)) Directory.CreateDirectory(parent);

                    if (f.IsOsu)
                    {
                        byte[] bytes = File.ReadAllBytes(src);
                        md5Index.Add(StableData.Md5OfBytes(bytes));
                        File.WriteAllBytes(dest, bytes);
                    }
                    else
                    {
                        File.Copy(src, dest);
                    }

                    copiedInSet++;
                    _sum.CopiedFiles++;
                    _sum.CopiedBytes += new FileInfo(dest).Length;
                }

                _sum.CopiedSets++;
                Log?.Invoke($"[完成] {folder}（{copiedInSet} 个文件）");
            }
            catch (OperationCanceledException)
            {
                TryDeleteDir(destDir);
                throw;
            }
            catch (Exception e)
            {
                Log?.Invoke($"[错误] 迁移 '{folder}' 失败: {e.Message}");
                TryDeleteDir(destDir);
                _sum.SkippedSets++;
            }
        }

        Progress?.Invoke(fileDone, fileTotal);
    }

    /// <summary>迁移前预检：确认能真正在 Songs 目录里创建文件夹，避免逐个报错。</summary>
    private void PreflightWriteCheck()
    {
        try
        {
            string probe = Path.Combine(_songsDir, ".__write_test__");
            Directory.CreateDirectory(probe);
            Directory.Delete(probe);
            Log?.Invoke("写入权限预检通过");
        }
        catch (Exception e)
        {
            throw new UnauthorizedAccessException(
                $"无法在 Songs 目录创建文件夹: {e.Message}{Environment.NewLine}" +
                "如果本工具是从聊天窗口/卡片里启动的，请关闭它，改从资源管理器直接双击运行 OsuLazerToStable.exe" +
                "（建议把整个 dist 文件夹先移到桌面或其它普通位置再运行）。" +
                "也可能是杀毒软件拦截，请检查 Windows 安全中心的「保护历史」。", e);
        }
    }

    private void TransferCollections(List<LazerCollection> lazerCols, string stableDir, HashSet<string> md5Index)
    {
        Log?.Invoke("== 迁移收藏夹 ==");

        string dbPath = Path.Combine(stableDir, "collection.db");

        var existing = CollectionDbFile.Read(dbPath);        if (existing != null)
            Log?.Invoke($"已读取现有 collection.db（版本 {existing.Version}，{existing.Collections.Count} 个收藏夹）");

        int version = existing != null && existing.Version >= CollectionDbFile.MinKnownVersion
            ? existing.Version
            : CollectionDbFile.MinKnownVersion;

        var outDb = new CollectionDbFile { Version = version };
        var byName = new Dictionary<string, CollectionEntry>(StringComparer.OrdinalIgnoreCase);

        if (_opts.MergeCollections && existing != null)
        {
            foreach (var c in existing.Collections)
            {
                outDb.Collections.Add(c);
                byName[c.Name] = c;
            }
        }

        foreach (var lc in lazerCols)
        {
            _ct.ThrowIfCancellationRequested();

            var hashes = new List<string>();
            foreach (string h in lc.Md5s.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (md5Index.Contains(h)) hashes.Add(h.ToLowerInvariant());
                else _sum.CollectionHashesMissing++;
            }

            if (byName.TryGetValue(lc.Name, out var target))
            {
                int added = 0;
                foreach (string h in hashes)
                    if (!target.Md5s.Contains(h))
                    {
                        target.Md5s.Add(h);
                        added++;
                    }
                Log?.Invoke($"合并收藏夹 '{lc.Name}': 新增 {added} 首（lazer 中共 {lc.Md5s.Count} 首）");
            }
            else
            {
                var entry = new CollectionEntry { Name = lc.Name, Md5s = hashes };
                outDb.Collections.Add(entry);
                byName[lc.Name] = entry;
                _sum.CollectionsWritten++;
                Log?.Invoke($"新增收藏夹 '{lc.Name}': {hashes.Count} 首（lazer 中共 {lc.Md5s.Count} 首）");
            }
        }

        _sum.CollectionHashesWritten = outDb.Collections.Sum(c => c.Md5s.Count);

        if (_opts.DryRun)
        {
            Log?.Invoke($"[演练] 将写入 {outDb.Collections.Count} 个收藏夹（{_sum.CollectionHashesWritten} 个谱面引用）到 {dbPath}");
            return;
        }

        if (IsStableRunning(stableDir))
            throw new InvalidOperationException(
                "检测到 osu!stable 正在运行。请先关闭 osu!stable 再迁移收藏夹，否则写入的 collection.db 会被游戏退出时覆盖。");

        if (File.Exists(dbPath))
        {
            string bak = dbPath + $".backup-{DateTime.Now:yyyyMMdd-HHmmss}";
            File.Copy(dbPath, bak, overwrite: true);
            _sum.CollectionBackupPath = bak;
            Log?.Invoke($"已备份原 collection.db -> {bak}");
        }

        outDb.Write(dbPath);
        Log?.Invoke($"已写入 collection.db（{outDb.Collections.Count} 个收藏夹, {_sum.CollectionHashesWritten} 个谱面引用）。重启 osu! 后生效。");
    }

    private (string Title, string Artist) ReadSetMetadata(LazerSet set)
    {
        foreach (var f in set.Files.Where(f => f.IsOsu))
        {
            string src = GetStorePath(f.Hash);
            if (!File.Exists(src)) continue;

            byte[] bytes;
            try
            {
                bytes = File.ReadAllBytes(src);
            }
            catch
            {
                continue;
            }

            if (bytes.Length > 128 * 1024) bytes = bytes[..(128 * 1024)];
            string text = DecodeOsuText(bytes);
            string title = "", artist = "";
            bool inMetadata = false;

            foreach (string raw in text.Split('\n'))
            {
                string line = raw.TrimEnd('\r').Trim();
                if (line.StartsWith('['))
                {
                    if (inMetadata) break;
                    if (line.StartsWith("[Metadata]", StringComparison.OrdinalIgnoreCase)) inMetadata = true;
                    continue;
                }
                if (!inMetadata) continue;

                if (line.StartsWith("Title:", StringComparison.OrdinalIgnoreCase) && title.Length == 0)
                    title = line[6..].Trim();
                else if (line.StartsWith("Artist:", StringComparison.OrdinalIgnoreCase) && artist.Length == 0)
                    artist = line[7..].Trim();

                if (title.Length > 0 && artist.Length > 0) break;
            }

            if (title.Length > 0 || artist.Length > 0)
                return (title, artist);
        }

        return (string.Empty, string.Empty);
    }

    private static string DecodeOsuText(byte[] bytes)
    {
        if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
            return Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3);
        if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE)
            return Encoding.Unicode.GetString(bytes, 2, bytes.Length - 2);
        if (bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF)
            return Encoding.BigEndianUnicode.GetString(bytes, 2, bytes.Length - 2);
        return Encoding.UTF8.GetString(bytes);
    }

    private string GetStorePath(string hash)
    {
        if (hash.Length < 2) return Path.Combine(_filesDir, hash);
        return Path.Combine(_filesDir, hash.Substring(0, 1), hash.Substring(0, 2), hash);
    }

    private string BuildFolderName(LazerSet set, string title, string artist, HashSet<string> usedNames)
    {
        string idPart = set.OnlineId > 0 ? set.OnlineId.ToString() : set.SetHash;
        string meta = SanitizeName($"{artist} - {title}");
        string baseName = meta.Length > 0 ? $"{idPart} {meta}" : idPart;
        if (string.IsNullOrWhiteSpace(baseName)) baseName = Guid.NewGuid().ToString("N")[..12];

        string name = baseName;
        int i = 2;
        while (usedNames.Contains(name))
            name = $"{baseName} ({i++})";
        return name;
    }

    public static string SanitizeName(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return string.Empty;

        var sb = new StringBuilder(s.Length);
        foreach (char c in s.Trim())
        {
            if (c < 32 || "<>:\"/\\|?*".Contains(c)) sb.Append(' ');
            else sb.Append(c);
        }

        string result = Regex.Replace(sb.ToString(), @"\s+", " ").Trim().TrimEnd('.');
        if (result.Length > 180) result = result[..180].TrimEnd('.');
        if (result.Length > 0 && IsReservedName(Path.GetFileNameWithoutExtension(result)))
            result = "_" + result;
        return result;
    }

    private static bool IsReservedName(string name)
    {
        string[] reserved = { "CON", "PRN", "AUX", "NUL",
            "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
            "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9" };
        int dot = name.IndexOf('.');
        string stem = dot >= 0 ? name[..dot] : name;
        return reserved.Contains(stem, StringComparer.OrdinalIgnoreCase);
    }

    private static string SanitizeRelPath(string rel)
    {
        string[] parts = rel.Split('/', '\\');
        var clean = new List<string>();
        foreach (string p in parts)
        {
            string s = SanitizeName(p);
            if (s.Length == 0 || s == "." || s == "..") continue;
            clean.Add(s);
        }
        return string.Join(Path.DirectorySeparatorChar, clean);
    }

    /// <summary>
    /// 判断 stable 是否在运行。注意 lazer 的进程名同样是 "osu!"，必须按可执行文件路径区分。
    /// </summary>
    private static bool IsStableRunning(string stableDir)
    {
        string stableRoot = Path.GetFullPath(stableDir).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        foreach (var p in Process.GetProcessesByName("osu!"))
        {
            try
            {
                string? exe = p.MainModule?.FileName;
                if (exe != null && exe.StartsWith(stableRoot, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            catch
            {
                // 无法读取进程路径时忽略
            }
        }
        return false;
    }

    private static void TryDeleteDir(string dir)
    {
        try
        {
            if (Directory.Exists(dir) && !Directory.EnumerateFileSystemEntries(dir).Any())
                Directory.Delete(dir);
        }
        catch
        {
            // 忽略清理失败
        }
    }
}
