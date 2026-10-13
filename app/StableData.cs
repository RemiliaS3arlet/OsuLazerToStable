using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace OsuLazerToStable;

public static class StableData
{
    /// <summary>stable Songs 目录中已有曲目的 OnlineID 集合（按顶层文件夹名的开头数字识别）。</summary>
    public static HashSet<int> GetExistingOnlineIds(string songsDir)
    {
        var ids = new HashSet<int>();
        if (!Directory.Exists(songsDir)) return ids;

        foreach (string dir in Directory.EnumerateDirectories(songsDir))
        {
            var m = Regex.Match(Path.GetFileName(dir), @"^(\d+)\b");
            if (m.Success && int.TryParse(m.Groups[1].Value, out int id) && id > 0)
                ids.Add(id);
        }
        return ids;
    }

    /// <summary>建立 stable 现有 .osu 文件的 MD5 索引，用于校验收藏夹引用。</summary>
    public static HashSet<string> BuildMd5Index(string songsDir, Action<string>? log = null, CancellationToken ct = default)
    {
        var index = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (!Directory.Exists(songsDir)) return index;

        int scanned = 0;
        foreach (string osu in Directory.EnumerateFiles(songsDir, "*.osu", SearchOption.AllDirectories))
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                index.Add(ComputeMd5(osu));
            }
            catch
            {
                // 单个文件读取失败不影响整体
            }

            scanned++;
            if (scanned % 3000 == 0) log?.Invoke($"正在索引 stable 已有谱面… {scanned}");
        }

        log?.Invoke($"stable 已有谱面索引完成: {scanned} 个 .osu, {index.Count} 个唯一 MD5");
        return index;
    }

    public static string ComputeMd5(string path)
    {
        using var md5 = MD5.Create();
        using var fs = File.OpenRead(path);
        return Convert.ToHexString(md5.ComputeHash(fs)).ToLowerInvariant();
    }

    public static string Md5OfBytes(byte[] bytes)
    {
        using var md5 = MD5.Create();
        return Convert.ToHexString(md5.ComputeHash(bytes)).ToLowerInvariant();
    }
}
