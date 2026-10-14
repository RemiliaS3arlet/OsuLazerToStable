using System.Diagnostics;
using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;

namespace OsuLazerToStable;

/// <summary>
/// osz 倍速生成器：把谱面整体变速 1.00x ~ 2.00x（音频用 ffmpeg atempo 变速，.osu 内所有时间点与 BPM 同步缩放）。
/// 生成结果另存为新文件，文件名加 "(1.XX)" 前缀。
/// </summary>
public static class RateChanger
{
    /// <summary>查找 ffmpeg.exe：程序同目录 → 系统 PATH。</summary>
    public static string FindFfmpeg()
    {
        string local = Path.Combine(AppContext.BaseDirectory, "ffmpeg.exe");
        if (File.Exists(local)) return local;

        string? pathVar = Environment.GetEnvironmentVariable("PATH");
        if (pathVar != null)
        {
            foreach (string dir in pathVar.Split(';'))
            {
                if (dir.Trim().Length == 0) continue;
                string candidate = Path.Combine(dir.Trim(), "ffmpeg.exe");
                if (File.Exists(candidate)) return candidate;
            }
        }
        return string.Empty;
    }

    public static void Generate(string input, double speed, string output, Action<string>? log = null, bool removeBackground = true)
    {
        speed = Math.Round(speed, 2, MidpointRounding.AwayFromZero);
        if (speed < 0.5 - 0.0001 || speed > 2.0 + 0.0001)
            throw new ArgumentException("倍速必须在 0.50 ~ 2.00 之间");
        if (!File.Exists(input))
            throw new FileNotFoundException("找不到输入文件: " + input);
        if (string.IsNullOrWhiteSpace(output))
            throw new ArgumentException("请指定输出文件路径");

        string ext = Path.GetExtension(input).ToLowerInvariant();
        if (ext != ".osz" && ext != ".osu")
            throw new ArgumentException("输入文件必须是 .osz 或 .osu");

        string workDir = Path.Combine(Path.GetTempPath(), "osuRate_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(workDir);
        try
        {
            if (ext == ".osz")
            {
                log?.Invoke($"解压 {Path.GetFileName(input)} …");
                ZipFile.ExtractToDirectory(input, workDir);
            }
            else
            {
                log?.Invoke($"读取 {Path.GetFileName(input)} …");
                File.Copy(input, Path.Combine(workDir, Path.GetFileName(input)));
                CopyReferencedFiles(input, workDir, removeBackground, log);
            }

            var osuFiles = Directory.GetFiles(workDir, "*.osu", SearchOption.AllDirectories).ToList();
            if (osuFiles.Count == 0)
                throw new InvalidDataException("没有找到 .osu 谱面文件");

            // 在改写 .osu 之前收集曲绘文件名（改写会删除 [Events] 中的背景引用行）
            var bgNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (removeBackground)
            {
                foreach (string f in osuFiles)
                    bgNames.UnionWith(GetBackgroundFilenames(f));
            }

            // 收集所有难度引用的音频
            var audioNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string f in osuFiles)
                audioNames.UnionWith(GetAudioFilenames(f));

            var audioFiles = new List<string>();
            foreach (string n in audioNames)
            {
                string p = Path.Combine(workDir, n.Replace('/', Path.DirectorySeparatorChar));
                if (File.Exists(p)) audioFiles.Add(p);
                else log?.Invoke($"[警告] 音频文件不存在，跳过: {n}");
            }

            if (audioFiles.Count > 0 && Math.Abs(speed - 1.0) > 0.0001)
            {
                string ffmpeg = FindFfmpeg();
                if (ffmpeg.Length == 0)
                    throw new InvalidOperationException(
                        "未找到 ffmpeg.exe。倍速谱面需要对音频变速，请下载 ffmpeg（https://www.gyan.dev/ffmpeg/builds/）" +
                        "并把 ffmpeg.exe 放到本程序同目录，或加入系统 PATH。");
                foreach (string a in audioFiles)
                {
                    log?.Invoke($"音频变速 {speed:0.00}x: {Path.GetFileName(a)} …");
                    StretchAudio(a, speed, ffmpeg);
                }
            }
            else if (audioFiles.Count == 0)
            {
                log?.Invoke("该谱面没有音频文件，仅改写谱面节奏");
            }

            foreach (string f in osuFiles)
            {
                log?.Invoke($"改写谱面节奏: {Path.GetFileName(f)}");
                RewriteOsu(f, speed, removeBackground, log);
            }

            // 按新元数据重命名内部 .osu 文件名：stable 按 .osu 文件名匹配旧谱面，重命名后才能被识别为全新谱面
            foreach (string f in osuFiles)
            {
                string? newName = GetOsuFileNameFromMetadata(f);
                if (newName != null
                    && !string.Equals(newName, Path.GetFileName(f), StringComparison.OrdinalIgnoreCase))
                {
                    string dest = Path.Combine(Path.GetDirectoryName(f)!, newName);
                    if (!File.Exists(dest))
                    {
                        File.Move(f, dest);
                        log?.Invoke($"重命名谱面文件: {Path.GetFileName(f)} -> {newName}");
                    }
                }
            }

            // 移除曲绘文件：让生成谱面与原谱面没有任何相同文件，绕过 stable 的更新检测
            if (removeBackground)
            {
                foreach (string n in bgNames)
                {
                    string p = Path.Combine(workDir, n.Replace('/', Path.DirectorySeparatorChar));
                    if (File.Exists(p))
                    {
                        File.Delete(p);
                        log?.Invoke($"已移除曲绘: {n}");
                    }
                }
            }

            log?.Invoke("打包生成新 osz …");
            string tmp = output + ".tmp";
            if (File.Exists(tmp)) File.Delete(tmp);
            ZipFile.CreateFromDirectory(workDir, tmp);
            if (File.Exists(output)) File.Delete(output);
            File.Move(tmp, output);
            log?.Invoke($"生成完成: {output}");
        }
        finally
        {
            try { Directory.Delete(workDir, true); } catch { }
        }
    }

    /// <summary>输入为单个 .osu 文件时，把其引用的音频（与背景图）复制进工作目录。</summary>
    private static void CopyReferencedFiles(string osuPath, string workDir, bool removeBackground, Action<string>? log)
    {
        string dir = Path.GetDirectoryName(Path.GetFullPath(osuPath))!;
        foreach (string name in GetReferencedFilenames(osuPath, !removeBackground))
        {
            string src = Path.Combine(dir, name.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(src))
            {
                log?.Invoke($"[警告] 未找到引用文件: {name}（生成的 osz 将不含该文件）");
                continue;
            }
            string dest = Path.Combine(workDir, name.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
            File.Copy(src, dest);
            log?.Invoke($"已附带: {name}");
        }
    }

    /// <summary>收集 .osu 引用的音频（AudioFilename）与可选背景图（[Events] 0,0,"..."）。</summary>
    private static IEnumerable<string> GetReferencedFilenames(string osuPath, bool includeBackground)
    {
        foreach (string a in GetAudioFilenames(osuPath))
            yield return a;

        if (includeBackground)
            foreach (string b in GetBackgroundFilenames(osuPath))
                yield return b;
    }

    /// <summary>收集 [Events] 里 0,0,"..." 引用的背景图（曲绘）文件名。</summary>
    private static IEnumerable<string> GetBackgroundFilenames(string osuPath)
    {
        string text = File.ReadAllText(osuPath);
        bool inEvents = false;
        foreach (string raw in text.Split('\n'))
        {
            string line = raw.TrimEnd('\r').Trim();
            if (line.StartsWith('['))
            {
                inEvents = line.StartsWith("[Events]", StringComparison.OrdinalIgnoreCase);
                continue;
            }
            if (!inEvents) continue;

            var m = Regex.Match(line, "^0,0,\"([^\"]+)\"", RegexOptions.IgnoreCase);
            if (m.Success) yield return m.Groups[1].Value;
        }
    }

    // ---------- .osu 改写 ----------

    private static void RewriteOsu(string path, double r, bool removeBackground, Action<string>? log)
    {
        byte[] raw = File.ReadAllBytes(path);
        bool utf8Bom = raw.Length >= 3 && raw[0] == 0xEF && raw[1] == 0xBB && raw[2] == 0xBF;
        bool utf16le = raw.Length >= 2 && raw[0] == 0xFF && raw[1] == 0xFE;
        bool utf16be = raw.Length >= 2 && raw[0] == 0xFE && raw[1] == 0xFF;
        string text = utf8Bom ? new UTF8Encoding(false).GetString(raw, 3, raw.Length - 3)
            : utf16le ? Encoding.Unicode.GetString(raw, 2, raw.Length - 2)
            : utf16be ? Encoding.BigEndianUnicode.GetString(raw, 2, raw.Length - 2)
            : Encoding.UTF8.GetString(raw);

        var sb = new StringBuilder(text.Length + 512);
        string section = "";
        bool warnedStoryboard = false;
        bool sawBeatmapId = false, sawBeatmapSetId = false, sawTags = false;
        char tagLetter = (char)('a' + Random.Shared.Next(26));

        using var sr = new StringReader(text);
        string? line;
        while ((line = sr.ReadLine()) != null)
        {
            string trimmed = line.Trim();
            if (trimmed.StartsWith('[') && trimmed.EndsWith(']'))
            {
                // 离开 [Metadata] 前补齐 ID / Tags 行，确保 osu 把该谱面当作全新本地谱面
                if (section == "[Metadata]")
                {
                    if (!sawBeatmapSetId) sb.AppendLine("BeatmapSetID:-1");
                    if (!sawBeatmapId) sb.AppendLine("BeatmapID:0");
                    if (!sawTags) sb.AppendLine("Tags:" + tagLetter);
                }
                section = trimmed;
                sb.AppendLine(line);
                continue;
            }

            if (section == "[General]")
            {
                if (line.StartsWith("AudioLeadIn:", StringComparison.OrdinalIgnoreCase)
                    && int.TryParse(line[12..].Trim(), out int lead))
                {
                    sb.AppendLine("AudioLeadIn:" + ScaleInt(lead, r));
                    continue;
                }
                if (line.StartsWith("PreviewTime:", StringComparison.OrdinalIgnoreCase)
                    && int.TryParse(line[12..].Trim(), out int preview) && preview > 0)
                {
                    sb.AppendLine("PreviewTime:" + ScaleInt(preview, r));
                    continue;
                }
            }
            else if (section == "[Metadata]")
            {
                if (line.StartsWith("BeatmapID:", StringComparison.OrdinalIgnoreCase))
                {
                    // 惯例：未提交的本地谱面 BeatmapID 为 0（而非 -1）
                    sb.AppendLine("BeatmapID:0");
                    sawBeatmapId = true;
                    continue;
                }
                if (line.StartsWith("BeatmapSetID:", StringComparison.OrdinalIgnoreCase))
                {
                    sb.AppendLine("BeatmapSetID:-1");
                    sawBeatmapSetId = true;
                    continue;
                }
                if (line.StartsWith("Version:", StringComparison.OrdinalIgnoreCase))
                {
                    // 难度名追加倍速后缀：即使与原始谱面合并进同一谱面集，也不会顶替原难度
                    sb.AppendLine("Version:" + line[8..].TrimEnd() + $" ({r:0.00}x)");
                    continue;
                }
                if (line.StartsWith("Title:", StringComparison.OrdinalIgnoreCase))
                {
                    // 标题追加倍速后缀，让 osu 在界面上明确区分变速版与原谱面
                    sb.AppendLine("Title:" + line[6..].TrimEnd() + $" ({r:0.00}x)");
                    continue;
                }
                if (line.StartsWith("TitleUnicode:", StringComparison.OrdinalIgnoreCase))
                {
                    sb.AppendLine("TitleUnicode:" + line[13..].TrimEnd() + $" ({r:0.00}x)");
                    continue;
                }
                if (line.StartsWith("Source:", StringComparison.OrdinalIgnoreCase))
                {
                    // 删除乐曲来源信息
                    sb.AppendLine("Source:");
                    continue;
                }
                if (line.StartsWith("Tags:", StringComparison.OrdinalIgnoreCase))
                {
                    // 在原有 tag 后随机追加一个字母，绕过 stable 的更新检测
                    sb.AppendLine("Tags:" + line[5..].TrimEnd() + " " + tagLetter);
                    sawTags = true;
                    continue;
                }
            }
            else if (section == "[Editor]" && line.StartsWith("Bookmarks:", StringComparison.OrdinalIgnoreCase))
            {
                var scaled = line[10..].Split(',')
                    .Where(p => int.TryParse(p.Trim(), out _))
                    .Select(p => ScaleInt(int.Parse(p.Trim()), r).ToString(CultureInfo.InvariantCulture));
                sb.AppendLine("Bookmarks:" + string.Join(",", scaled));
                continue;
            }
            else if (section == "[TimingPoints]" && trimmed.Length > 0)
            {
                var p = line.Split(',');
                if (p.Length >= 2
                    && double.TryParse(p[0].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double offset))
                {
                    p[0] = ScaleInt(offset, r).ToString(CultureInfo.InvariantCulture);
                    bool uninherited = p.Length >= 7 && p[6].Trim() == "1";
                    if (uninherited
                        && double.TryParse(p[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double beatLength))
                    {
                        p[1] = FormatDouble(beatLength / r);
                    }
                    sb.AppendLine(string.Join(",", p));
                    continue;
                }
            }
            else if (section == "[Events]" && trimmed.Length > 0)
            {
                var p = line.Split(',');
                if (removeBackground && p.Length >= 3 && p[0].Trim() == "0" && p[1].Trim() == "0")
                {
                    // 移除背景图引用（曲绘），让生成谱面与原谱面彻底无相同文件
                    continue;
                }
                if (p[0].Trim() == "Video" && p.Length >= 2 && int.TryParse(p[1].Trim(), out int videoStart))
                {
                    p[1] = ScaleInt(videoStart, r).ToString(CultureInfo.InvariantCulture);
                    sb.AppendLine(string.Join(",", p));
                    continue;
                }
                if (p[0].Trim() == "2" && p.Length >= 3
                    && int.TryParse(p[1].Trim(), out int breakStart) && int.TryParse(p[2].Trim(), out int breakEnd))
                {
                    p[1] = ScaleInt(breakStart, r).ToString(CultureInfo.InvariantCulture);
                    p[2] = ScaleInt(breakEnd, r).ToString(CultureInfo.InvariantCulture);
                    sb.AppendLine(string.Join(",", p));
                    continue;
                }
                if (!warnedStoryboard && p.Length >= 3 && p[0].Trim() is "4" or "5" or "6")
                {
                    warnedStoryboard = true;
                    log?.Invoke("[提示] 该谱面含 storyboard，其时间轴保持原速（不影响游玩，仅视觉效果）");
                }
            }
            else if (section == "[HitObjects]" && trimmed.Length > 0)
            {
                var p = line.Split(',');
                if (p.Length >= 4
                    && int.TryParse(p[2].Trim(), out int time)
                    && int.TryParse(p[3].Trim(), out int type))
                {
                    p[2] = ScaleInt(time, r).ToString(CultureInfo.InvariantCulture);

                    if ((type & 128) != 0 && p.Length >= 6)
                    {
                        // osu!mania 长条: endTime:hitSample
                        int ci = p[5].IndexOf(':');
                        if (ci > 0 && int.TryParse(p[5][..ci], out int holdEnd))
                            p[5] = ScaleInt(holdEnd, r).ToString(CultureInfo.InvariantCulture) + p[5][ci..];
                    }
                    else if ((type & 8) != 0 && p.Length >= 6 && int.TryParse(p[5].Trim(), out int spinEnd))
                    {
                        // 转盘: endTime
                        p[5] = ScaleInt(spinEnd, r).ToString(CultureInfo.InvariantCulture);
                    }
                    // 滑条: 长度与路径不变（BPM 已按比例加快，时长自动缩短）

                    sb.AppendLine(string.Join(",", p));
                    continue;
                }
            }

            sb.AppendLine(line);
        }

        // [Metadata] 是文件最后一段时，循环后补插 ID / Tags 行
        if (section == "[Metadata]")
        {
            if (!sawBeatmapSetId) sb.AppendLine("BeatmapSetID:-1");
            if (!sawBeatmapId) sb.AppendLine("BeatmapID:0");
            if (!sawTags) sb.AppendLine("Tags:" + tagLetter);
        }

        byte[] output = utf8Bom ? new UTF8Encoding(true).GetBytes(sb.ToString())
            : utf16le ? Encoding.Unicode.GetPreamble().Concat(Encoding.Unicode.GetBytes(sb.ToString())).ToArray()
            : utf16be ? Encoding.BigEndianUnicode.GetPreamble().Concat(Encoding.BigEndianUnicode.GetBytes(sb.ToString())).ToArray()
            : Encoding.UTF8.GetBytes(sb.ToString());
        File.WriteAllBytes(path, output);
    }

    private static long ScaleInt(double value, double r) =>
        Math.Max(0, (long)Math.Round(value / r, MidpointRounding.AwayFromZero));

    /// <summary>按改写后的元数据生成 .osu 文件名（Artist - Title (Creator) [Version].osu）。</summary>
    private static string? GetOsuFileNameFromMetadata(string osuPath)
    {
        string text = File.ReadAllText(osuPath);
        string title = "", artist = "", creator = "", version = "";
        bool inMeta = false;
        foreach (string raw in text.Split('\n'))
        {
            string line = raw.TrimEnd('\r').Trim();
            if (line.StartsWith('['))
            {
                if (inMeta) break;
                inMeta = line.StartsWith("[Metadata]", StringComparison.OrdinalIgnoreCase);
                continue;
            }
            if (!inMeta) continue;

            if (line.StartsWith("Title:", StringComparison.OrdinalIgnoreCase) && title.Length == 0)
                title = line[6..].Trim();
            else if (line.StartsWith("Artist:", StringComparison.OrdinalIgnoreCase) && artist.Length == 0)
                artist = line[7..].Trim();
            else if (line.StartsWith("Creator:", StringComparison.OrdinalIgnoreCase) && creator.Length == 0)
                creator = line[8..].Trim();
            else if (line.StartsWith("Version:", StringComparison.OrdinalIgnoreCase) && version.Length == 0)
                version = line[8..].Trim();
        }

        if (title.Length == 0 && artist.Length == 0 && version.Length == 0) return null;

        string name = TransferEngine.SanitizeName($"{artist} - {title} ({creator}) [{version}].osu");
        if (name.Length == 0 || name == ".osu") return null;
        return name;
    }

    private static string FormatDouble(double d)
    {
        string s = d.ToString("0.####################", CultureInfo.InvariantCulture);
        int dot = s.IndexOf('.');
        if (dot >= 0)
        {
            s = s.TrimEnd('0');
            if (s.EndsWith('.')) s = s[..^1];
        }
        return s.Length == 0 ? "0" : s;
    }

    private static IEnumerable<string> GetAudioFilenames(string osuPath)
    {
        string text = File.ReadAllText(osuPath); // 自动识别 BOM
        bool inGeneral = false;
        foreach (string raw in text.Split('\n'))
        {
            string line = raw.TrimEnd('\r').Trim();
            if (line.StartsWith('['))
            {
                inGeneral = line.StartsWith("[General]", StringComparison.OrdinalIgnoreCase);
                continue;
            }
            if (!inGeneral) continue;

            if (line.StartsWith("AudioFilename:", StringComparison.OrdinalIgnoreCase))
            {
                string v = line[14..].Trim();
                if (v.Length > 0) yield return v;
            }
        }
    }

    // ---------- 音频变速 ----------

    private static void StretchAudio(string file, double r, string ffmpeg)
    {
        string ext = Path.GetExtension(file).ToLowerInvariant();
        string codec = ext switch
        {
            ".mp3" => "libmp3lame",
            ".ogg" => "libvorbis",
            ".wav" => "pcm_s16le",
            ".m4a" => "aac",
            ".flac" => "flac",
            _ => "",
        };
        if (codec.Length == 0)
            throw new InvalidOperationException($"不支持的音频格式: {ext}");

        string tmp = file + ".rate" + ext; // 保持原扩展名，ffmpeg 依据扩展名判断输出格式
        if (File.Exists(tmp)) File.Delete(tmp);

        var psi = new ProcessStartInfo(ffmpeg)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardError = true,
        };
        psi.ArgumentList.Add("-hide_banner");
        psi.ArgumentList.Add("-y");
        psi.ArgumentList.Add("-i");
        psi.ArgumentList.Add(file);
        psi.ArgumentList.Add("-filter:a");
        psi.ArgumentList.Add("atempo=" + r.ToString("0.00", CultureInfo.InvariantCulture));
        psi.ArgumentList.Add("-vn");
        psi.ArgumentList.Add("-codec:a");
        psi.ArgumentList.Add(codec);
        if (ext is ".mp3" or ".m4a")
        {
            psi.ArgumentList.Add("-b:a");
            psi.ArgumentList.Add("192k");
        }
        else if (ext == ".ogg")
        {
            psi.ArgumentList.Add("-q:a");
            psi.ArgumentList.Add("5");
        }
        psi.ArgumentList.Add(tmp);

        using var p = Process.Start(psi)!;
        string err = p.StandardError.ReadToEnd();
        p.WaitForExit();
        if (p.ExitCode != 0)
        {
            string detail = err.TrimEnd();
            if (detail.Length > 300) detail = detail[^300..];
            throw new InvalidOperationException($"ffmpeg 音频变速失败: {detail}");
        }

        File.Copy(tmp, file, true);
        File.Delete(tmp);
    }
}
