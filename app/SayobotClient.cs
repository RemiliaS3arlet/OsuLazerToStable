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

public class SayobotSearchSet
{
    public long Sid;
    public string Artist = string.Empty;
    public string Title = string.Empty;
    public string Creator = string.Empty;
    public int Approved;
    public int Modes;
    public int FavouriteCount;
    public int PlayCount;
    public long LastUpdate;
    public double Order;
    public double Bpm;
    public string Genre = string.Empty;
    public string Language = string.Empty;
}

public class SayobotDifficulty
{
    public int Bid;
    public string Version = string.Empty;
    public int Mode;
    public double OD;
    public double HP;
    public double Star;
    public double AR;
    public double CS;
    public int Length;
    public int MaxCombo;
}

public class SayobotSetMeta
{
    public long Sid;
    public int Approved;
    public double Bpm;
    public string Genre = string.Empty;
    public string Language = string.Empty;
    public string Creator = string.Empty;
    public int FavouriteCount;
    public int PlayCount;
    public long LastUpdate;
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

    /// <summary>下载谱面集封面缩略图（osu! 官方 CDN）。失败返回 null。</summary>
    public static async Task<Image?> GetCoverAsync(long sid, CancellationToken ct)
    {
        try
        {
            byte[] data = await Http.GetByteArrayAsync($"https://b.ppy.sh/thumb/{sid}l.jpg", ct);
            if (data.Length < 100) return null;
            using var ms = new MemoryStream(data);
            using var img = Image.FromStream(ms);
            return new Bitmap(img);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>下载试听音频（osu! 官方 CDN，实际为 OGG 编码），返回本地缓存路径。</summary>
    public static async Task<string> DownloadPreviewAsync(long sid, CancellationToken ct)
    {
        string dir = Path.Combine(Path.GetTempPath(), "osu-preview");
        Directory.CreateDirectory(dir);
        string path = Path.Combine(dir, $"{sid}.ogg");
        if (File.Exists(path) && new FileInfo(path).Length > 1000)
            return path;

        byte[] data = await Http.GetByteArrayAsync($"https://b.ppy.sh/preview/{sid}.mp3", ct);
        if (data.Length < 1000)
            throw new InvalidDataException("试听音频下载失败（谱面可能没有预览音频）");
        await File.WriteAllBytesAsync(path, data, ct);
        return path;
    }

    /// <summary>状态代码 → 网页分类名称（按 osu.ppy.sh/beatmapsets 的分类）。</summary>
    public static string StatusText(int approved) => approved switch
    {
        1 => "上架 Ranked",
        2 => "Approved",
        3 => "Qualified",
        4 => "Loved",
        0 => "待定 Pending",
        -1 => "制作中 WIP",
        -2 => "坟场 Graveyard",
        _ => "未知",
    };

    public static string ModeText(int mode) => mode switch
    {
        0 => "osu!",
        1 => "taiko",
        2 => "catch",
        3 => "mania",
        _ => "?",
    };

    private static string ModeMaskText(int modes)
    {
        var parts = new List<string>();
        if ((modes & 1) != 0) parts.Add("osu!");
        if ((modes & 2) != 0) parts.Add("taiko");
        if ((modes & 4) != 0) parts.Add("catch");
        if ((modes & 8) != 0) parts.Add("mania");
        return parts.Count > 0 ? string.Join("/", parts) : ModeText(modes);
    }

    public static string ModeMask(int modes) => ModeMaskText(modes);

    /// <summary>按关键词搜索谱面集（sayobot beatmaplist 接口）。</summary>
    public static async Task<List<SayobotSearchSet>> SearchAsync(string query, int amount, CancellationToken ct)
    {
        string body = JsonSerializer.Serialize(new Dictionary<string, object>
        {
            ["cmd"] = "beatmaplist",
            ["keyword"] = query,
            ["amount"] = amount,
        });
        using var content = new StringContent(body, System.Text.Encoding.UTF8, "application/json");
        using var resp = await Http.PostAsync("https://api.sayobot.cn/?post", content, ct);
        resp.EnsureSuccessStatusCode();
        string json = await resp.Content.ReadAsStringAsync(ct);

        var result = new List<SayobotSearchSet>();
        using var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Array)
            return result;

        foreach (var el in data.EnumerateArray())
        {
            if (!el.TryGetProperty("sid", out var sidEl)) continue;
            result.Add(new SayobotSearchSet
            {
                Sid = sidEl.GetInt64(),
                Artist = el.TryGetProperty("artist", out var a) ? a.GetString() ?? "" : "",
                Title = el.TryGetProperty("title", out var t) ? t.GetString() ?? "" : "",
                Creator = el.TryGetProperty("creator", out var c) ? c.GetString() ?? "" : "",
                Approved = el.TryGetProperty("approved", out var ap) ? ap.GetInt32() : 0,
                Modes = el.TryGetProperty("modes", out var m) ? m.GetInt32() : 0,
                FavouriteCount = el.TryGetProperty("favourite_count", out var fc) ? fc.GetInt32() : 0,
                PlayCount = el.TryGetProperty("play_count", out var pc) ? pc.GetInt32() : 0,
                LastUpdate = el.TryGetProperty("lastupdate", out var lu) && lu.ValueKind == JsonValueKind.Number ? lu.GetInt64() : 0,
                Order = el.TryGetProperty("order", out var od) && od.ValueKind == JsonValueKind.Number ? od.GetDouble() : 0,
            });
        }
        return result;
    }

    /// <summary>获取一个谱面集内的所有难度（版本、模式、AR/CS/OD/HP、星级、BPM 等）。</summary>
    public static async Task<(List<SayobotDifficulty> Diffs, SayobotSetMeta? Meta)> GetMapDetailAsync(long sid, CancellationToken ct)
    {
        string json = await Http.GetStringAsync($"https://api.sayobot.cn/v2/beatmapinfo?0={sid}", ct);
        var result = new List<SayobotDifficulty>();
        SayobotSetMeta? meta = null;
        using var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("data", out var data)) return (result, meta);

        meta = new SayobotSetMeta
        {
            Sid = sid,
            Approved = NumInt(data, "approved"),
            Bpm = NumDouble(data, "bpm"),
            Genre = NumText(data, "genre"),
            Language = NumText(data, "language"),
            Creator = NumText(data, "creator"),
            FavouriteCount = NumInt(data, "favourite_count"),
            PlayCount = NumInt(data, "play_count"),
            LastUpdate = (long)NumDouble(data, "last_update"),
        };

        if (!data.TryGetProperty("bid_data", out var bids) || bids.ValueKind != JsonValueKind.Array)
            return (result, meta);

        foreach (var el in bids.EnumerateArray())
        {
            result.Add(new SayobotDifficulty
            {
                Bid = el.TryGetProperty("bid", out var b) ? b.GetInt32() : 0,
                Version = el.TryGetProperty("version", out var v) ? v.GetString() ?? "" : "",
                Mode = el.TryGetProperty("mode", out var mo) ? mo.GetInt32() : 0,
                OD = el.TryGetProperty("OD", out var od) ? od.GetDouble() : 0,
                HP = el.TryGetProperty("HP", out var hp) ? hp.GetDouble() : 0,
                AR = el.TryGetProperty("AR", out var ar) ? ar.GetDouble() : 0,
                CS = el.TryGetProperty("CS", out var cs) ? cs.GetDouble() : 0,
                Star = el.TryGetProperty("star", out var st) ? st.GetDouble() : 0,
                Length = el.TryGetProperty("length", out var ln) ? ln.GetInt32() : 0,
                MaxCombo = el.TryGetProperty("maxcombo", out var mc) ? mc.GetInt32() : 0,
            });
        }
        return (result, meta);
    }

    public static async Task<List<SayobotDifficulty>> GetDifficultiesAsync(long sid, CancellationToken ct) =>
        (await GetMapDetailAsync(sid, ct)).Diffs;

    private static double NumDouble(JsonElement el, string name) =>
        el.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetDouble() : 0;

    private static int NumInt(JsonElement el, string name) => (int)NumDouble(el, name);

    /// <summary>字符串字段；如果是数字（如 genre/language 的 id）则转成文本。</summary>
    private static string NumText(JsonElement el, string name)
    {
        if (!el.TryGetProperty(name, out var v)) return string.Empty;
        return v.ValueKind switch
        {
            JsonValueKind.String => v.GetString() ?? string.Empty,
            JsonValueKind.Number => v.ToString(),
            _ => string.Empty,
        };
    }

    /// <summary>搜索并加载每个结果集的难度明细（并行，带并发限制）。</summary>
    public static async Task<List<(SayobotSearchSet Set, SayobotDifficulty Diff)>> SearchWithDifficultiesAsync(
        string query, int amount, CancellationToken ct, Action<int, int>? progress = null)
    {
        var sets = await SearchAsync(query, amount, ct);
        var output = new List<(SayobotSearchSet Set, SayobotDifficulty Diff)>();
        var gate = new SemaphoreSlim(6);
        int done = 0;

        var tasks = sets.Select(async set =>
        {
            await gate.WaitAsync(ct);
            try
            {
                var (diffs, meta) = await GetMapDetailAsync(set.Sid, ct);
                if (meta != null)
                {
                    set.Bpm = meta.Bpm;
                    set.Genre = meta.Genre;
                    set.Language = meta.Language;
                    if (set.FavouriteCount == 0) set.FavouriteCount = meta.FavouriteCount;
                    if (set.PlayCount == 0) set.PlayCount = meta.PlayCount;
                    if (set.LastUpdate == 0) set.LastUpdate = meta.LastUpdate;
                    if (set.Creator.Length == 0) set.Creator = meta.Creator;
                    if (set.Approved == 0) set.Approved = meta.Approved;
                }
                lock (output)
                {
                    foreach (var d in diffs) output.Add((set, d));
                }
            }
            catch
            {
                lock (output) output.Add((set, new SayobotDifficulty { Version = "(读取难度失败)" }));
            }
            finally
            {
                gate.Release();
                progress?.Invoke(Interlocked.Increment(ref done), sets.Count);
            }
        }).ToArray();

        await Task.WhenAll(tasks);
        return output;
    }

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

    /// <summary>下载 .osz。多下载源 + 重试：sayobot → osu.direct 镜像。
    /// progress 回调 (已下载字节, 总字节或 0)；onSource 回调当前使用的下载源名称。</summary>
    public static async Task DownloadAsync(long sid, string savePath, Action<long, long>? progress,
        CancellationToken ct, Action<string>? onSource = null)
    {
        var sources = new (string Name, string Url)[]
        {
            ("sayobot", $"https://dl.sayobot.cn/beatmaps/download/sets/{sid}"),
            ("osu.direct", $"https://osu.direct/api/d/{sid}"),
        };

        Exception? lastError = null;
        foreach (var (name, url) in sources)
        {
            for (int attempt = 1; attempt <= 2; attempt++)
            {
                ct.ThrowIfCancellationRequested();
                try
                {
                    onSource?.Invoke(name);
                    await DownloadToFileAsync(url, sid, savePath, progress, ct);
                    if (IsValidOsz(savePath)) return;
                    throw new InvalidDataException("下载内容不是有效的 osz 压缩包");
                }
                catch (OperationCanceledException)
                {
                    TryDelete(savePath);
                    throw;
                }
                catch (Exception e)
                {
                    lastError = e;
                    TryDelete(savePath);
                    if (attempt < 2)
                    {
                        try { await Task.Delay(800, ct); } catch (OperationCanceledException) { throw; }
                    }
                }
            }
        }

        throw new InvalidOperationException(
            "所有下载源都失败（sayobot 官方节点可能繁忙，osu.direct 镜像也不可用）。最后一次错误：" +
            (lastError?.Message ?? "未知错误"));
    }

    private static void TryDelete(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); } catch { }
    }

    private static bool IsValidOsz(string path)
    {
        try
        {
            var info = new FileInfo(path);
            if (!info.Exists || info.Length < 4) return false;
            using var fs = File.OpenRead(path);
            byte[] magic = new byte[2];
            return fs.Read(magic, 0, 2) == 2 && magic[0] == (byte)'P' && magic[1] == (byte)'K';
        }
        catch
        {
            return false;
        }
    }

    private static async Task DownloadToFileAsync(string url, long sid, string savePath,
        Action<long, long>? progress, CancellationToken ct)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.Referrer = new Uri(url.Contains("osu.direct", StringComparison.OrdinalIgnoreCase)
            ? "https://osu.direct/"
            : "https://osu.sayobot.cn/");
        req.Headers.TryAddWithoutValidation("User-Agent", "OsuLazerToStable/2.0");

        using var resp = await Http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
        resp.EnsureSuccessStatusCode();

        long total = resp.Content.Headers.ContentLength ?? 0;
        await using var src = await resp.Content.ReadAsStreamAsync(ct);
        await using (var dst = File.Create(savePath))
        {
            byte[] buf = new byte[256 * 1024];
            long done = 0;
            long lastReported = -1;
            long lastTick = Environment.TickCount64;
            int read;
            while ((read = await src.ReadAsync(buf, ct)) > 0)
            {
                await dst.WriteAsync(buf.AsMemory(0, read), ct);
                done += read;

                // 限流：进度只在百分比变化或每 120ms 时回调，避免高频 BeginInvoke 让界面抖动
                bool report;
                if (total > 0)
                {
                    long pct = done * 100 / total;
                    report = pct != lastReported;
                    if (report) lastReported = pct;
                }
                else
                {
                    long now = Environment.TickCount64;
                    report = now - lastTick >= 120;
                    if (report) lastTick = now;
                }
                if (report) progress?.Invoke(done, total);
            }
            progress?.Invoke(done, total);
        }
    }
}





