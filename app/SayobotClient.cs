using System.Text.Json;

namespace OsuLazerToStable;

/// <summary>
/// osu.sayobot.cn 谱面下载客户端：
/// 元数据: GET https://api.sayobot.cn/v2/beatmapinfo?0={sid}
/// 下载:   GET https://dl.sayobot.cn/beatmaps/download/sets/{sid}（需 Referer 头）
/// </summary>
public class SayobotMapInfo
{
    public long Sid;
    public string Artist = string.Empty;
    public string Title = string.Empty;
}

public static class SayobotClient
{
    private static readonly HttpClient Http = new(new HttpClientHandler
    {
        // 默认使用系统代理（用户开了 Clash 等代理时自动走代理）
        AutomaticDecompression = System.Net.DecompressionMethods.All,
    })
    {
        Timeout = TimeSpan.FromMinutes(5),
    };

    public static string WebUrl(long sid) => $"https://osu.sayobot.cn/beatmaps?sid={sid}";

    /// <summary>从谱面集 ID 或 sayobot 链接中解析 sid。</summary>
    public static bool TryParseSid(string input, out long sid)
    {
        sid = 0;
        string s = input.Trim();
        if (s.Length == 0) return false;
        if (long.TryParse(s, out sid) && sid > 0) return true;

        foreach (string key in new[] { "sid=", "s=", "sets/" })
        {
            int i = s.IndexOf(key, StringComparison.OrdinalIgnoreCase);
            if (i >= 0)
            {
                string tail = s[(i + key.Length)..];
                int end = 0;
                while (end < tail.Length && char.IsDigit(tail[end])) end++;
                if (end > 0 && long.TryParse(tail[..end], out sid) && sid > 0)
                    return true;
            }
        }
        return false;
    }

    public static async Task<SayobotMapInfo> GetMapInfoAsync(long sid, CancellationToken ct)
    {
        string json = await Http.GetStringAsync($"https://api.sayobot.cn/v2/beatmapinfo?0={sid}", ct);
        using var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("data", out var data))
            throw new InvalidDataException($"未找到谱面集 {sid} 的信息");
        return new SayobotMapInfo
        {
            Sid = data.TryGetProperty("sid", out var sidEl) ? sidEl.GetInt64() : sid,
            Artist = data.TryGetProperty("artist", out var a) ? a.GetString() ?? "" : "",
            Title = data.TryGetProperty("title", out var t) ? t.GetString() ?? "" : "",
        };
    }

    /// <summary>下载 .osz。progress 回调 (已下载字节, 总字节或 0)。</summary>
    public static async Task DownloadAsync(long sid, string savePath, Action<long, long>? progress, CancellationToken ct)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, $"https://dl.sayobot.cn/beatmaps/download/sets/{sid}");
        req.Headers.Referrer = new Uri("https://osu.sayobot.cn/");
        req.Headers.TryAddWithoutValidation("User-Agent", "OsuLazerToStable/1.5");

        using var resp = await Http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
        resp.EnsureSuccessStatusCode();

        long total = resp.Content.Headers.ContentLength ?? 0;
        await using var src = await resp.Content.ReadAsStreamAsync(ct);

        {
            await using var dst = File.Create(savePath);
            byte[] buf = new byte[256 * 1024];
            long done = 0;
            int read;
            while ((read = await src.ReadAsync(buf, ct)) > 0)
            {
                await dst.WriteAsync(buf.AsMemory(0, read), ct);
                done += read;
                progress?.Invoke(done, total);
            }
        }

        // 校验是否为 zip（osz）
        if (new FileInfo(savePath).Length < 4)
            throw new InvalidDataException("下载内容过小，可能不是有效的 osz");
        await using var check = File.OpenRead(savePath);
        byte[] magic = new byte[2];
        if (check.Read(magic, 0, 2) != 2 || magic[0] != (byte)'P' || magic[1] != (byte)'K')
            throw new InvalidDataException("下载内容不是有效的 osz 压缩包（谱面可能已被删除或不存在）");
    }
}
