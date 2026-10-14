using Realms;

// 探针：按文件名过滤列出谱面集（用于检查导入后的落库情况）
string path = args.Length > 0 && !args[0].StartsWith("--") ? args[0] : @"C:\Users\somer\AppData\Roaming\osu\client.realm";
string filter = args.Length > 1 ? args[1] : "";

var config = new RealmConfiguration(path) { IsReadOnly = true, SchemaVersion = 52 };

using var r = Realm.GetInstance(config);
Console.WriteLine($"opened OK, searching '{filter}'");
int matched = 0;
foreach (var s in r.All<BeatmapSetInfo>())
{
    bool hit = false;
    foreach (var f in s.Files)
    {
        if (filter.Length == 0 || (f.Filename ?? "").Contains(filter, StringComparison.OrdinalIgnoreCase))
        {
            hit = true;
            break;
        }
    }
    if (!hit) continue;
    matched++;
    Console.WriteLine($"set onlineId={s.OnlineID} hash={s.Hash} del={s.DeletePending} files={s.Files.Count}");
    foreach (var f in s.Files)
        Console.WriteLine($"    {f.Filename} -> {f.File.Hash}");
}
Console.WriteLine($"matched sets: {matched}");

// ---------- 最小模型声明（与 app/RealmModels.cs 一致） ----------
[MapTo("BeatmapSet")]
public class BeatmapSetInfo : RealmObject
{
    [PrimaryKey]
    public Guid ID { get; set; }

    [Indexed]
    public int OnlineID { get; set; } = -1;

    public DateTimeOffset DateAdded { get; set; }

    public DateTimeOffset? DateSubmitted { get; set; }

    public DateTimeOffset? DateRanked { get; set; }

    public IList<RealmNamedFileUsage> Files { get; } = null!;

    [MapTo("Status")]
    public int StatusInt { get; set; }

    public bool DeletePending { get; set; }

    public string Hash { get; set; } = string.Empty;

    public bool Protected { get; set; }
}

public class RealmNamedFileUsage : EmbeddedObject
{
    public RealmFile File { get; set; } = null!;

    public string Filename { get; set; } = string.Empty;
}

[MapTo("File")]
public class RealmFile : RealmObject
{
    [PrimaryKey]
    public string Hash { get; set; } = string.Empty;

    [Backlink(nameof(RealmNamedFileUsage.File))]
    public IQueryable<RealmNamedFileUsage> Usages { get; } = null!;
}
