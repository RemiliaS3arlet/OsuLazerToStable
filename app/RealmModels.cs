using Realms;

namespace OsuLazerToStable;

// 与本机 osu!lazer (2026.1005, Realm 20.1.0) 的 client.realm 持久化 schema 精确对应的最小类集合。
// 属性名、类型、[MapTo] 均来自对本地 osu.Game.dll 的反射提取；缺失的持久化属性会被 Realm 容忍，
// 因此可以只声明需要的子集。

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
