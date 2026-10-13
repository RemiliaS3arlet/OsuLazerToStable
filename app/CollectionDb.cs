using System.Text;

namespace OsuLazerToStable;

/// <summary>
/// osu!stable 的 collection.db 读写。
/// 格式（与 ppy/osu 的 LegacyCollectionImporter / SerializationReader 一致）：
///   int32 版本号
///   int32 收藏夹数量
///   每个收藏夹:
///     字符串 名称   [0x00 = 空；否则 0x0B + .NET BinaryWriter 7 位长度前缀 + UTF-8]
///     int32 谱面数量
///     每个谱面: 字符串 MD5（32 位小写十六进制，编码同上）
/// </summary>
public class CollectionEntry
{
    public string Name = string.Empty;
    public List<string> Md5s = new();
}

public class CollectionDbFile
{
    public const int MinKnownVersion = 20211109;

    public int Version = 20260924;
    public List<CollectionEntry> Collections = new();

    public static CollectionDbFile? Read(string path)
    {
        if (!File.Exists(path)) return null;

        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var r = new BinaryReader(fs, Encoding.UTF8);

        var db = new CollectionDbFile { Version = r.ReadInt32() };
        int count = r.ReadInt32();
        if (count < 0 || count > 1_000_000)
            throw new InvalidDataException($"collection.db 中的收藏夹数量异常: {count}");

        for (int i = 0; i < count; i++)
        {
            var entry = new CollectionEntry { Name = ReadString(r) };
            int mapCount = r.ReadInt32();
            if (mapCount < 0 || mapCount > 10_000_000)
                throw new InvalidDataException($"收藏夹 '{entry.Name}' 的谱面数量异常: {mapCount}");
            for (int j = 0; j < mapCount; j++)
            {
                string hash = ReadString(r).Trim().ToLowerInvariant();
                if (hash.Length == 32) entry.Md5s.Add(hash);
            }
            db.Collections.Add(entry);
        }

        return db;
    }

    public void Write(string path)
    {
        byte[] data;
        using (var ms = new MemoryStream())
        {
            using (var w = new BinaryWriter(ms, Encoding.UTF8, true))
            {
                w.Write(Version);
                w.Write(Collections.Count);
                foreach (var c in Collections)
                {
                    WriteString(w, c.Name);
                    w.Write(c.Md5s.Count);
                    foreach (string h in c.Md5s) WriteString(w, h);
                }
            }
            data = ms.ToArray();
        }

        string tmp = path + ".tmp";
        File.WriteAllBytes(tmp, data);
        if (File.Exists(path)) File.Delete(path);
        File.Move(tmp, path);
    }

    private static string ReadString(BinaryReader r)
    {
        byte marker = r.ReadByte();
        if (marker == 0) return string.Empty;
        // 与 SerializationReader.ReadString 一致：标记非零时按 .NET 7 位长度前缀字符串读取
        return r.ReadString();
    }

    private static void WriteString(BinaryWriter w, string s)
    {
        if (string.IsNullOrEmpty(s))
        {
            w.Write((byte)0);
            return;
        }

        w.Write((byte)0x0B);
        w.Write(s); // BinaryWriter 写 7 位长度前缀 + UTF-8
    }
}
