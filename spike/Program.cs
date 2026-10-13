using Realms;

// 探针：验证 Realm 打开时的属性级严格性，以及最小类集合能否读取。
// PROBE_STRICT 编译开关：仅声明部分属性的 BeatmapCollection
string path = @"C:\Users\somer\AppData\Roaming\osu\client.realm";
var config = new RealmConfiguration(path) { IsReadOnly = true, SchemaVersion = 52 };

#if PROBE_STRICT
using (var r = Realm.GetInstance(config))
{
    Console.WriteLine("strict: opened OK (missing-props tolerated!)");
    foreach (var c in r.All<BeatmapCollectionPartial>())
        Console.WriteLine("  col: " + c.Name);
}
#else
using (var r = Realm.GetInstance(config))
{
    Console.WriteLine("min: opened OK");
    Console.WriteLine("schema classes visible: " + r.Schema.Count);
    foreach (var os in r.Schema) Console.WriteLine("  " + os.Name);
    var cols = r.All<BeatmapCollection>();
    Console.WriteLine("collections: " + cols.Count());
    int cc = 0;
    foreach (var c in cols)
    {
        Console.WriteLine($"  col '{c.Name}' maps={c.BeatmapMD5Hashes.Count}");
        if (++cc >= 8) break;
    }
    var sets = r.All<BeatmapSetInfo>();
    Console.WriteLine("sets: " + sets.Count());
    int sc = 0;
    foreach (var s in sets)
    {
        Console.WriteLine($"  set id={s.OnlineID} hash={s.Hash} del={s.DeletePending} files={s.Files.Count}");
        int fc = 0;
        foreach (var f in s.Files)
        {
            Console.WriteLine($"      file '{f.Filename}' -> {f.File.Hash}");
            if (++fc >= 2) break;
        }
        if (++sc >= 3) break;
    }
}
#endif

// ---------- 类声明 ----------

#if PROBE_STRICT
[MapTo("BeatmapCollection")]
public class BeatmapCollectionPartial : RealmObject
{
    public string Name { get; set; } = string.Empty;
}
#else
public class BeatmapCollection : RealmObject
{
    [PrimaryKey]
    public Guid ID { get; set; }

    public string Name { get; set; } = string.Empty;

    public IList<string> BeatmapMD5Hashes { get; } = null!;

    public DateTimeOffset LastModified { get; set; }
}

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
    public string Filename { get; set; } = string.Empty;

    public RealmFile File { get; set; } = null!;
}

[MapTo("File")]
public class RealmFile : RealmObject
{
    [PrimaryKey]
    public string Hash { get; set; } = string.Empty;

    [Backlink(nameof(RealmNamedFileUsage.File))]
    public IQueryable<RealmNamedFileUsage> Usages { get; } = null!;
}
#endif
