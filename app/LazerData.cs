using Realms;
using System.Text.RegularExpressions;

namespace OsuLazerToStable;

public class LazerFile
{
    public string Filename = string.Empty;
    public string Hash = string.Empty;
    public bool IsOsu => Filename.EndsWith(".osu", StringComparison.OrdinalIgnoreCase);
}

public class LazerSet
{
    public Guid Id = Guid.Empty;
    public int OnlineId = -1;
    public string SetHash = string.Empty;
    public List<LazerFile> Files = new();
}

public class LazerCollection
{
    public string Name = string.Empty;
    public List<string> Md5s = new();
}

public class LazerReadResult
{
    public List<LazerSet> Sets = new();
    public List<LazerCollection> Collections = new();
    public int DeletedSets;
}

public static class LazerData
{
    /// <summary>读取 lazer 数据目录（含 client.realm 与 files 仓库）。只读打开，可在 lazer 运行时使用。</summary>
    public static LazerReadResult Read(string lazerDir, Action<string>? log = null)
    {
        string realmPath = Path.Combine(lazerDir, "client.realm");
        string filesDir = Path.Combine(lazerDir, "files");
        if (!File.Exists(realmPath)) throw new FileNotFoundException($"找不到 client.realm: {realmPath}");
        if (!Directory.Exists(filesDir)) throw new DirectoryNotFoundException($"找不到文件仓库目录: {filesDir}");

        using var realm = OpenReadOnly(realmPath, log);
        var result = new LazerReadResult();

        foreach (var s in realm.All<BeatmapSetInfo>())
        {
            if (s.DeletePending)
            {
                result.DeletedSets++;
                continue;
            }

            var set = new LazerSet { Id = s.ID, OnlineId = s.OnlineID, SetHash = s.Hash ?? string.Empty };
            foreach (var f in s.Files)
            {
                if (f?.File == null) continue;
                if (string.IsNullOrWhiteSpace(f.File.Hash)) continue;
                set.Files.Add(new LazerFile
                {
                    Filename = f.Filename ?? string.Empty,
                    Hash = f.File.Hash,
                });
            }
            result.Sets.Add(set);
        }

        foreach (var c in realm.All<BeatmapCollection>())
        {
            var col = new LazerCollection { Name = c.Name ?? string.Empty };
            foreach (string h in c.BeatmapMD5Hashes)
                if (!string.IsNullOrWhiteSpace(h))
                    col.Md5s.Add(h.Trim().ToLowerInvariant());
            result.Collections.Add(col);
        }

        log?.Invoke($"lazer 读取完成: {result.Sets.Count} 个谱面集, {result.Collections.Count} 个收藏夹");
        return result;
    }

    private static Realm OpenReadOnly(string realmPath, Action<string>? log)
    {
        var config = new RealmConfiguration(realmPath)
        {
            IsReadOnly = true,
            SchemaVersion = 52,
        };

        try
        {
            return Realm.GetInstance(config);
        }
        catch (Realms.Exceptions.RealmException e) when (e.Message.Contains("schema version", StringComparison.OrdinalIgnoreCase))
        {
            var m = Regex.Match(e.Message, @"last set version (\d+)");
            if (!m.Success) throw;
            config.SchemaVersion = ulong.Parse(m.Groups[1].Value);
            log?.Invoke($"检测到 realm schema 版本 {config.SchemaVersion}，重试打开…");
            return Realm.GetInstance(config);
        }
    }
}
