using System.Reflection;
using System.Runtime.InteropServices;

string lazerDir = @"C:\Users\somer\AppData\Local\osulazer\current";
string outFile = @"C:\Users\somer\Documents\deepseek-harness\default-workspace\OsuTransfer\tools\schema-dump.txt";

var paths = new List<string>(Directory.GetFiles(lazerDir, "*.dll"));

// 找 ref pack（用户级 SDK 或系统级），否则退回共享运行时目录
var refDirs = new List<string>();
var localPacks = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "dotnet", "packs");
foreach (var packRoot in new[] { localPacks, @"C:\Program Files\dotnet\packs" })
{
    if (!Directory.Exists(packRoot)) continue;
    foreach (var packName in new[] { "Microsoft.NETCore.App.Ref", "Microsoft.WindowsDesktop.App.Ref" })
    {
        var dir = Path.Combine(packRoot, packName);
        if (!Directory.Exists(dir)) continue;
        var v = Directory.GetDirectories(dir).OrderByDescending(x => x).FirstOrDefault();
        if (v == null) continue;
        var tfm = Path.Combine(v, "ref", "net8.0");
        if (Directory.Exists(tfm)) refDirs.Add(tfm);
    }
}
if (refDirs.Count == 0)
{
    refDirs.Add(Directory.GetDirectories(@"C:\Program Files\dotnet\shared\Microsoft.NETCore.App")
        .OrderByDescending(x => x).First());
}
foreach (var d in refDirs) paths.AddRange(Directory.GetFiles(d, "*.dll"));
paths = paths.Where(p => !string.Equals(Path.GetFileName(p), "mscorlib.dll", StringComparison.OrdinalIgnoreCase))
    .Distinct(StringComparer.OrdinalIgnoreCase).ToList();
foreach (var d in refDirs) Console.WriteLine("ref dir: " + d);

var resolver = new PathAssemblyResolver(paths);
using var mlc = new MetadataLoadContext(resolver, "System.Private.CoreLib");

var sw = new StringWriter();

string TypeName(Type t)
{
    if (t.IsArray) return TypeName(t.GetElementType()) + "[]";
    if (t.IsGenericType)
    {
        string def = t.GetGenericTypeDefinition().FullName;
        def = def.Substring(0, def.IndexOf('`'));
        string args = string.Join(", ", t.GetGenericArguments().Select(TypeName));
        string prefix = "";
        if (def == "System.Collections.Generic.List") { def = "IList"; }
        if (t.FullName.StartsWith("System.Collections.Generic.IList") || def.StartsWith("System.Collections.Generic.IList")) { def = "IList"; }
        if (def == "System.Nullable") return TypeName(t.GetGenericArguments()[0]) + "?";
        return $"{def}<{args}>";
    }
    return t.Name;
}

Assembly osu = mlc.LoadFromAssemblyPath(Path.Combine(lazerDir, "osu.Game.dll"));
sw.WriteLine($"osu.Game: {osu.GetName().Version}");

var want = new[]
{
    "BeatmapSetInfo", "BeatmapInfo", "BeatmapMetadata", "BeatmapCollection",
    "RealmUser", "RulesetInfo", "BeatmapDifficulty", "BeatmapUserSettings",
    "RealmNamedFileUsage", "RealmFile", "ScoreInfo", "HitEvent",
    "BeatmapUserRating", "RealmBeatmapStatistic" 
};

foreach (var name in want)
{
    var t = osu.GetTypes().FirstOrDefault(x => x.Name == name);
    if (t == null) { sw.WriteLine($"\n=== {name}: NOT FOUND ==="); continue; }
    sw.WriteLine($"\n=== {name} : base={t.BaseType?.Name} {(t.IsAbstract ? "abstract" : "")} ===");
    sw.WriteLine($"    classAttrs: {string.Join(", ", t.GetCustomAttributesData().Select(a => a.AttributeType.Name + (a.AttributeType.Name == "MapToAttribute" ? "(" + string.Join(",", a.ConstructorArguments.Select(c => c.Value)) + ")" : "")))}");
    foreach (var p in t.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
    {
        var attrs = new List<string>();
        foreach (var a in p.GetCustomAttributesData())
        {
            string an = a.AttributeType.Name;
            if (an.StartsWith("MapTo")) attrs.Add("MapTo(" + string.Join(",", a.ConstructorArguments.Select(c => c.Value)) + ")");
            else if (an.StartsWith("PrimaryKey") || an.StartsWith("Indexed") || an.StartsWith("Ignored") ||
                     an.StartsWith("Backlink") || an.StartsWith("Required")) attrs.Add(an);
        }
        string access = (p.GetMethod?.IsPublic == true ? "get;" : "") + (p.SetMethod?.IsPublic == true ? "set;" : "");
        sw.WriteLine($"    prop {TypeName(p.PropertyType),-34} {p.Name,-28} {access,-8} [{string.Join(", ", attrs)}]");
    }
}

File.WriteAllText(outFile, sw.ToString());
Console.WriteLine("written: " + outFile);
Console.WriteLine(sw.ToString());
