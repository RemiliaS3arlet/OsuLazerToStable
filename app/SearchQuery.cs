using System.Globalization;

namespace OsuLazerToStable;

/// <summary>一条搜索过滤条件（对应 osu! 官网 beatmapsets 的搜索语法）。</summary>
public class SearchFilter
{
    public string Field = string.Empty;   // stars / od / hp / ar / cs / length / bpm / plays / favourites / mode / status / artist / title / creator / genre / language
    public string Op = "=";               // = > < >= <=
    public string Value = string.Empty;
    public double Number;

    public bool IsNumeric => Op is ">" or "<" or ">=" or "<=" || double.TryParse(Value, NumberStyles.Float, CultureInfo.InvariantCulture, out _);
}

/// <summary>
/// 把 osu! 官网风格的搜索框内容解析成「关键词 + 过滤条件」。
/// 支持：stars>6、od&lt;5、bpm>180、length&lt;120、plays>1000、favourites>500、
///       mode=mania、status=ranked、ar>=9、artist=xi、title=zenith、creator=Sotarks、genre=、language=
/// 其它文字作为关键词传给 sayobot 搜索。
/// </summary>
public static class SearchQuery
{
    private static readonly string[] NumericFields =
    {
        "stars", "od", "hp", "ar", "cs", "length", "bpm", "plays", "favourites", "id", "setid", "rating",
    };

    private static readonly string[] TextFields =
    {
        "artist", "title", "creator", "genre", "language", "source", "tags",
    };

    public static string ExampleText =>
        "示例：blue zenith stars>6  或  mode=mania stars>=5 status=ranked";

    /// <summary>解析输入，返回纯关键词与过滤条件。</summary>
    public static (string Text, List<SearchFilter> Filters) Parse(string input)
    {
        var filters = new List<SearchFilter>();
        if (string.IsNullOrWhiteSpace(input)) return (string.Empty, filters);

        var textParts = new List<string>();
        // 先按空格切分，再处理 field op value（允许 stars>6 / od<5 / mode=mania 无空格写法）
        var tokens = input.Split(new[] { ' ', '\t', ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (string raw in tokens)
        {
            string token = raw.Trim();
            if (token.Length == 0) continue;

            if (TryParseFilterToken(token, filters)) continue;
            textParts.Add(token);
        }

        return (string.Join(" ", textParts), filters);
    }

    private static bool TryParseFilterToken(string token, List<SearchFilter> filters)
    {
        string lower = token.ToLowerInvariant();
        string? field = null;
        string op = "=";
        string value = string.Empty;

        // 找出字段名（取冒号/比较符前的前缀）
        int idx = lower.IndexOfAny(new[] { ':', '>', '<', '=' });
        if (idx <= 0) return false;
        string prefix = lower[..idx];
        if (!NumericFields.Contains(prefix) && !TextFields.Contains(prefix) &&
            prefix != "mode" && prefix != "status" && prefix != "keys") return false;

        field = prefix;
        string rest = token[idx..];

        // 读取比较符
        if (rest.StartsWith(">=") || rest.StartsWith("<="))
        {
            op = rest[..2];
            rest = rest[2..];
        }
        else if (rest.StartsWith(">") || rest.StartsWith("<") || rest.StartsWith(":") || rest.StartsWith("="))
        {
            op = rest[0] is ':' ? "=" : rest[0].ToString();
            rest = rest[1..];
        }

        // 若后面还跟着布尔词（如 "stars>6 keys=4" 已拆分，无需处理）
        value = rest.Trim().Trim('"', '\'');
        if (value.Length == 0) return false;

        if (field == "keys") return false; // sayobot 数据里没有键位数，暂不支持

        filters.Add(new SearchFilter { Field = field, Op = op, Value = value, Number = ParseDouble(value) });
        return true;
    }

    private static double ParseDouble(string s) =>
        double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out double d) ? d : 0;

    /// <summary>判断一条难度是否满足条件。</summary>
    public static bool Matches(SayobotSearchSet set, SayobotDifficulty diff, List<SearchFilter> filters)
    {
        foreach (var f in filters)
        {
            if (!MatchOne(set, diff, f)) return false;
        }
        return true;
    }

    private static bool MatchOne(SayobotSearchSet set, SayobotDifficulty diff, SearchFilter f)
    {
        switch (f.Field)
        {
            case "stars":
                return Compare(diff.Star, f);
            case "od":
                return Compare(diff.OD, f);
            case "hp":
                return Compare(diff.HP, f);
            case "ar":
                return Compare(diff.AR, f);
            case "cs":
                return Compare(diff.CS, f);
            case "length":
                return Compare(diff.Length, f);
            case "bpm":
                return Compare(set.Bpm, f);
            case "plays":
                return Compare(set.PlayCount, f);
            case "favourites":
                return Compare(set.FavouriteCount, f);
            case "id":
            case "setid":
                return Compare(set.Sid, f);
            case "artist":
                return Contains(set.Artist, f.Value);
            case "title":
                return Contains(set.Title, f.Value);
            case "creator":
                return Contains(set.Creator, f.Value);
            case "genre":
                return Contains(set.Genre, f.Value);
            case "language":
                return Contains(set.Language, f.Value);
            case "source":
            case "tags":
                return true;
            case "mode":
                return ModeMatches(diff.Mode, f.Value);
            case "status":
                return StatusMatches(set.Approved, f.Value);
            default:
                return true;
        }
    }

    private static bool Compare(double actual, SearchFilter f)
    {
        double v = f.Number;
        return f.Op switch
        {
            ">" => actual > v,
            "<" => actual < v,
            ">=" => actual >= v,
            "<=" => actual <= v,
            _ => Math.Abs(actual - v) < 0.0001,
        };
    }

    private static bool Contains(string source, string value) =>
        source.Contains(value, StringComparison.OrdinalIgnoreCase);

    private static bool ModeMatches(int mode, string value)
    {
        value = value.ToLowerInvariant();
        int wanted = value switch
        {
            "osu" or "osu!" or "std" or "standard" => 0,
            "taiko" => 1,
            "catch" or "ctb" => 2,
            "mania" => 3,
            _ => -1,
        };
        return wanted < 0 || mode == wanted;
    }

    private static bool StatusMatches(int approved, string value)
    {
        value = value.ToLowerInvariant();
        if (value is "any" or "all" or "*") return true;
        return approved switch
        {
            1 => value is "ranked",
            2 => value is "approved",
            3 => value is "qualified",
            4 => value is "loved",
            0 => value is "pending",
            -1 => value is "wip",
            -2 => value is "graveyard",
            _ => false,
        };
    }

    /// <summary>把过滤条件格式化成可读文本（用于状态栏提示）。</summary>
    public static string Describe(List<SearchFilter> filters) =>
        string.Join(" ", filters.Select(f => $"{f.Field}{f.Op}{f.Value}"));
}

