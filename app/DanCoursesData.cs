// 自动生成的数据文件：来自《4 6 7k段位 個別ACC計算 by gyoza_goki & Blaite1016.xlsx》
// 每级 4 首曲目 + 累计音符数（T），通过時ACC 示例值（缺省为 null，用户自行输入）。
namespace OsuLazerToStable;

public class DanLevel
{
    public string Name = string.Empty;
    public string[] Songs = System.Array.Empty<string>();
    public long[] Cum = System.Array.Empty<long>();
    public double?[] Acc = System.Array.Empty<double?>();
}

public class DanCourse
{
    public string Name = string.Empty;
    public System.Collections.Generic.List<DanLevel> Levels = new();
}

public static class DanCoursesData
{
    public static readonly System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<DanCourse>> Modes = new()
    {
        ["4k"] = new System.Collections.Generic.List<DanCourse>
        {
            new DanCourse { Name = "Reform段位", Levels = new()
            {
                new DanLevel { Name = "Reform 1st", Songs = new[] { "Miracle Chance", "Want U 2", "Shelter", "Tomorrow Parfume" }, Cum = new long[] { 878, 1574, 2528, 3695 }, Acc = new double?[] { null, null, null, null } },
                new DanLevel { Name = "Reform 2nd", Songs = new[] { "Flashes", "Vermillion", "Snow Storm", "Eternal Drain" }, Cum = new long[] { 1107, 2012, 2967, 4454 }, Acc = new double?[] { null, null, null, null } },
                new DanLevel { Name = "Reform 3rd", Songs = new[] { "Dimention Tripper!!!!", "Kirlian Shores", "Entelechia", "True Blue" }, Cum = new long[] { 1008, 1805, 2852, 4111 }, Acc = new double?[] { null, null, null, null } },
                new DanLevel { Name = "Reform 4th", Songs = new[] { "CrossOver", "Ephemera", "Shannon's Theorem", "Angel Of Darkness" }, Cum = new long[] { 905, 2404, 3687, 5758 }, Acc = new double?[] { null, null, null, null } },
                new DanLevel { Name = "Reform 5th", Songs = new[] { "Energy Flower 3307", "Brainfog", "TDPDG", "Elektric U-Phoria" }, Cum = new long[] { 1293, 3015, 4647, 6550 }, Acc = new double?[] { null, null, null, null } },
                new DanLevel { Name = "Reform 6th", Songs = new[] { "Sweet Cherry X", "The Bird's Midair Heatstroke", "Luv Lab 22ate!", "M-A" }, Cum = new long[] { 1487, 2753, 4131, 6317 }, Acc = new double?[] { null, null, null, null } },
                new DanLevel { Name = "Reform 7th", Songs = new[] { "Last Chance", "WAVE", "five Five Five Guys", "Hymn" }, Cum = new long[] { 2114, 3891, 4972, 8042 }, Acc = new double?[] { null, null, null, null } },
                new DanLevel { Name = "Reform 8th", Songs = new[] { "To The Limit", "RATO", "Trickstarz", "Anguish" }, Cum = new long[] { 2177, 3486, 5094, 6683 }, Acc = new double?[] { null, null, null, null } },
                new DanLevel { Name = "Reform 9th", Songs = new[] { "Can't Take my Eyes", "Cicadidae", "Disconnected Trance", "Firmament Castle Velier" }, Cum = new long[] { 2114, 4184, 5858, 8130 }, Acc = new double?[] { null, null, null, null } },
                new DanLevel { Name = "Reform 10th", Songs = new[] { "The Lost Dedicated", "Rave 7.7", "Oh Curry!", "Chandelier" }, Cum = new long[] { 1906, 3366, 5089, 7481 }, Acc = new double?[] { null, null, null, null } },
                new DanLevel { Name = "Reform alpha", Songs = new[] { "Dark Sambaland", "Odoru Mizushibuki", "Makiba", "LazorBeamz" }, Cum = new long[] { 2265, 3793, 6093, 9427 }, Acc = new double?[] { null, null, null, null } },
                new DanLevel { Name = "Reform beta", Songs = new[] { "Paradigm Shift", "Blue Planet", "Amber Starlight", "Time to Say Goodbye" }, Cum = new long[] { 2274, 4582, 6322, 8623 }, Acc = new double?[] { null, null, null, null } },
                new DanLevel { Name = "Reform gamma", Songs = new[] { "Break", "Fastest Crash", "Reflec Streamz", "We Luv Lama" }, Cum = new long[] { 1973, 3953, 5382, 9361 }, Acc = new double?[] { 91.05, 93.56, 92.66, 94.65 } },
                new DanLevel { Name = "Reform delta", Songs = new[] { "Aquaris", "Crescent Moon", "Volcanic", "Future Dominators" }, Cum = new long[] { 2018, 4729, 7997, 10626 }, Acc = new double?[] { null, null, null, null } },
                new DanLevel { Name = "Reform epsilon", Songs = new[] { "Rose Quartz", "Forgotten", "Mario Paint", "Hitsugi to Futago" }, Cum = new long[] { 2128, 4680, 6874, 9703 }, Acc = new double?[] { null, null, null, null } },
            } },
            new DanCourse { Name = "Malody段位 v3", Levels = new()
            {
                new DanLevel { Name = "Regular 0th", Songs = new[] { "Ikitoshi Ikerumono", "Rambling Pleat", "Yi Meng Qian Xiao", "White Eternity" }, Cum = new long[] { 492, 1021, 1616, 2297 }, Acc = new double?[] { null, null, null, null } },
                new DanLevel { Name = "Regular 1st", Songs = new[] { "Rex Incognito", "Koiseyo Otome!", "Break", "KING" }, Cum = new long[] { 695, 1316, 2034, 3313 }, Acc = new double?[] { null, null, null, null } },
                new DanLevel { Name = "Regular 2nd", Songs = new[] { "Stargazer", "Seyana", "Love Emotion!", "Mermaid Girl" }, Cum = new long[] { 1397, 2487, 3292, 4504 }, Acc = new double?[] { null, null, null, null } },
                new DanLevel { Name = "Regular 3rd", Songs = new[] { "Koi Kou Enishi", "Onegai! Kon Kon Oinari-sama", "The Last Page", "Hoshi ga Furanai Machi" }, Cum = new long[] { 1055, 2544, 3832, 5621 }, Acc = new double?[] { null, null, null, null } },
                new DanLevel { Name = "Regular 4th", Songs = new[] { "Umiyuri Kaiteitan", "Fire in the sky", "Icyxis", "The Crimson Empire" }, Cum = new long[] { 1865, 3299, 4583, 6422 }, Acc = new double?[] { null, null, null, null } },
                new DanLevel { Name = "Regular 5th", Songs = new[] { "Platinum Disco", "Cute na Kanojo", "Reimei Sketchbook", "joker" }, Cum = new long[] { 1282, 2988, 4461, 6400 }, Acc = new double?[] { null, null, null, null } },
                new DanLevel { Name = "Regular 6th", Songs = new[] { "Chocolate Disco", "Call Me, Beep Me [If You Wanna Reap Me]", "DIE IN THE SEA", "unhappy century" }, Cum = new long[] { 1694, 3330, 5133, 7248 }, Acc = new double?[] { null, null, null, null } },
                new DanLevel { Name = "Regular 7th", Songs = new[] { "Don't Stop the Music", "Six Acid Strings", "Arkadia [Illusion]", "Valkyrie Revolutia" }, Cum = new long[] { 1701, 3500, 5632, 7531 }, Acc = new double?[] { null, null, null, null } },
                new DanLevel { Name = "Regular 8th", Songs = new[] { "Don't let you down", "Yoru ni Kakeru", "Snow Veil -Shoujo to Kemono no Mori-", "Positive Dance Time" }, Cum = new long[] { 2237, 4318, 6598, 8598 }, Acc = new double?[] { null, null, null, null } },
                new DanLevel { Name = "Regular 9th", Songs = new[] { "Kikai Shoujo Gensou", "cold planet", "Stray Star", "Unleashed World" }, Cum = new long[] { 2374, 4263, 6405, 8215 }, Acc = new double?[] { null, null, null, null } },
                new DanLevel { Name = "Regular 10th", Songs = new[] { "Hiensou", "Rocky Buinne", "Yue Ai Yue Ye", "Spin Eternally" }, Cum = new long[] { 2034, 3774, 6044, 8210 }, Acc = new double?[] { null, null, null, null } },
                new DanLevel { Name = "Extra 1st", Songs = new[] { "Scirpion Dance", "Tailin no Soul", "Eiya no Parade", "Onsoku Uchuu Ryokou" }, Cum = new long[] { 1952, 3965, 5918, 8029 }, Acc = new double?[] { null, null, null, null } },
                new DanLevel { Name = "Extra 2nd", Songs = new[] { "Moments", "Towards the Horizon", "Torikago", "Frontier Explorer" }, Cum = new long[] { 2107, 4060, 6446, 9120 }, Acc = new double?[] { null, null, null, null } },
                new DanLevel { Name = "Extra 3rd", Songs = new[] { "Edison", "INTERNET OVERDOSE", "Euthanasia", "Fin.ArcDeaR" }, Cum = new long[] { 2518, 5154, 7480, 9991 }, Acc = new double?[] { null, null, null, null } },
                new DanLevel { Name = "Extra 4th", Songs = new[] { "ZENITHALIZE", "Asymmetrical Grooves", "Pure Ruby", "EVERLASTING HAPPiNESS" }, Cum = new long[] { 2634, 4846, 7182, 9784 }, Acc = new double?[] { null, null, null, null } },
                new DanLevel { Name = "Extra 5th", Songs = new[] { "Bring Our Ignition Back", "Unsan-musho", "Electric Angel", "Kegare Naki Bara Juuji" }, Cum = new long[] { 2734, 5151, 8240, 11214 }, Acc = new double?[] { null, null, null, null } },
                new DanLevel { Name = "Extra 6th", Songs = new[] { "Defeat awaken battle ship", "Extraction", "Pastel Subliminal", "Synthesized Fortress" }, Cum = new long[] { 2483, 4759, 7680, 10874 }, Acc = new double?[] { null, null, null, null } },
                new DanLevel { Name = "Extra 7th", Songs = new[] { "LiFE Garden", "Alpha", "Stay Alive [Zekk Remix]", "Heaven's Fall" }, Cum = new long[] { 2846, 5106, 7439, 10786 }, Acc = new double?[] { null, null, null, null } },
                new DanLevel { Name = "Extra 8th", Songs = new[] { "Hayabusa", "Shuu no Hazama", "Amatsukami", "CRIMSON FIGHTER" }, Cum = new long[] { 3789, 7452, 9876, 13131 }, Acc = new double?[] { null, null, null, null } },
                new DanLevel { Name = "Extra 9th", Songs = new[] { "crazy_tek [DJ Noriken Remix]", "Nhelv", "Shuuten", "Deadly force - Put an end" }, Cum = new long[] { 3888, 6918, 10499, 14199 }, Acc = new double?[] { null, null, null, null } },
                new DanLevel { Name = "Extra Final", Songs = new[] { "Infinity Heaven", "NEURO-CLOUD-9", "Kizuato", "Runengon" }, Cum = new long[] { 2828, 6190, 9583, 14683 }, Acc = new double?[] { null, null, null, null } },
            } },
            new DanCourse { Name = "Malody段位 v2", Levels = new()
            {
                new DanLevel { Name = "Regular 1st", Songs = new[] { "Glitch Nerds", "Borealis", "Niflheimr", "Moon" }, Cum = new long[] { 831, 1786, 2693, 3347 }, Acc = new double?[] { null, null, null, null } },
                new DanLevel { Name = "Regular 2nd", Songs = new[] { "Follow Tomorrow", "Bronze Coffin", "Keigan no Zettaireido", "Sakura Mirage" }, Cum = new long[] { 1152, 2002, 2952, 3921 }, Acc = new double?[] { null, null, null, null } },
                new DanLevel { Name = "Regular 3rd", Songs = new[] { "Quon", "Genkyoku o Kirikizamu", "Gin no Kaze", "Adjudicatorz-Danzai-" }, Cum = new long[] { 1169, 2312, 3286, 4632 }, Acc = new double?[] { null, null, null, null } },
                new DanLevel { Name = "Regular 4th", Songs = new[] { "Tenkuu no Yoake", "VALLISTA", "Crystal World ~Fracture~", "Zenithalize" }, Cum = new long[] { 1416, 2818, 4503, 6102 }, Acc = new double?[] { null, null, null, null } },
                new DanLevel { Name = "Regular 5th", Songs = new[] { "Ten Thousand Tons", "Chips of Notes", "Despair of ELFERIA", "Chronostasi" }, Cum = new long[] { 1953, 3331, 5497, 7164 }, Acc = new double?[] { null, null, null, null } },
                new DanLevel { Name = "Regular 6th", Songs = new[] { "Sweet Cherry X", "Akasagarbha", "HELLO EveryOne", "DataError" }, Cum = new long[] { 1487, 2911, 4292, 5879 }, Acc = new double?[] { null, null, null, null } },
                new DanLevel { Name = "Regular 7th", Songs = new[] { "Fairy Stage", "Xross Infection", "Wave", "Blue Zenith" }, Cum = new long[] { 1909, 3723, 5500, 8181 }, Acc = new double?[] { null, null, null, null } },
                new DanLevel { Name = "Regular 8th", Songs = new[] { "The Lost Dedicated", "The Party We Have Never Seen", "Finixe", "Ultimate Dream" }, Cum = new long[] { 1962, 3029, 5417, 7189 }, Acc = new double?[] { null, null, null, null } },
                new DanLevel { Name = "Regular 9th", Songs = new[] { "Dusk", "Panic Popn Picnic", "Kick-ass Kung-Fu Carnival", "Yakusoku no kimi" }, Cum = new long[] { 1799, 3822, 6103, 7890 }, Acc = new double?[] { null, null, null, null } },
                new DanLevel { Name = "Regular 10th", Songs = new[] { "Chaser", "Loli Fantasy", "Daydream", "Moon Gate" }, Cum = new long[] { 2606, 4794, 6988, 9175 }, Acc = new double?[] { null, null, null, null } },
                new DanLevel { Name = "Extra 1st", Songs = new[] { "Border od Life", "Scorpion Dance", "The Island of Albatross", "Beautiful Loli Thing" }, Cum = new long[] { 2164, 4116, 5937, 9186 }, Acc = new double?[] { null, null, null, null } },
                new DanLevel { Name = "Extra 2nd", Songs = new[] { "Kitsune no Yomeiri", "Zi Yang Hua -AZISAI-", "Love & Justice", "Message" }, Cum = new long[] { 2871, 4895, 6766, 9218 }, Acc = new double?[] { null, null, null, null } },
                new DanLevel { Name = "Extra 3rd", Songs = new[] { "Jumble Rumble", "End Time", "++", "Crow Solace" }, Cum = new long[] { 2327, 3920, 6086, 8286 }, Acc = new double?[] { null, null, null, null } },
                new DanLevel { Name = "Extra 4th", Songs = new[] { "EDM Jumpers", "[line-epsilon]", "Contrapasso-paradise-", "YELL!" }, Cum = new long[] { 2731, 5384, 7417, 10213 }, Acc = new double?[] { null, null, null, null } },
                new DanLevel { Name = "Extra 5th", Songs = new[] { "Satori de Pon!", "Legend of Seeker", "Crystal World -Fracture-", "Wizdomiot" }, Cum = new long[] { 3229, 5960, 8521, 10630 }, Acc = new double?[] { null, null, null, null } },
                new DanLevel { Name = "Extra 6th", Songs = new[] { "Sepia", "Nopea", "Satori Trisis", "Death" }, Cum = new long[] { 1766, 3627, 6798, 8477 }, Acc = new double?[] { null, null, null, null } },
                new DanLevel { Name = "Extra 7th", Songs = new[] { "Paranoia", "Boulafacet", "Paraclete -Miracle-", "Yumemi no Shonen" }, Cum = new long[] { 2339, 4800, 7311, 9488 }, Acc = new double?[] { null, null, null, null } },
                new DanLevel { Name = "Extra 8th", Songs = new[] { "Martail Arts", "Gene Disrupition", "Paradigm Shift[D-CSC]", "Death Malody" }, Cum = new long[] { 1929, 4309, 7124, 11799 }, Acc = new double?[] { null, null, null, null } },
                new DanLevel { Name = "Extra 9th", Songs = new[] { "Fragmentation", "Nostos2021", "Green-eyed Jealousy", "SILENT DESERT" }, Cum = new long[] { 3987, 5861, 10224, 14067 }, Acc = new double?[] { null, null, null, null } },
                new DanLevel { Name = "Extra Final", Songs = new[] { "Only my railgun", "-purgatorium-", "Blue Zenith", "Inner Universe" }, Cum = new long[] { 3468, 6803, 10501, 15562 }, Acc = new double?[] { null, null, null, null } },
            } },
            new DanCourse { Name = "LN段位(underjoy)", Levels = new()
            {
                new DanLevel { Name = "LN 1st", Songs = new[] { "Hitorigoto -TV MIX-", "Dango Daikazoku", "Nerevar Rising", "TRICKL4SH 220" }, Cum = new long[] { 893, 1420, 1702, 2722 }, Acc = new double?[] { null, null, null, null } },
                new DanLevel { Name = "LN 2nd", Songs = new[] { "Doki Doki Literature Club!", "SPF 420", "HeartBeats", "RockHill" }, Cum = new long[] { 1184, 2598, 3303, 4913 }, Acc = new double?[] { null, null, null, null } },
                new DanLevel { Name = "LN 3rd", Songs = new[] { "Aura", "Aura", "Aura", "OverDrivers" }, Cum = new long[] { 1305, 2647, 3527, 5222 }, Acc = new double?[] { null, null, null, null } },
                new DanLevel { Name = "LN 4th", Songs = new[] { "Otenba Koimusume", "GEE", "Future Gazer", "#FairyJoke #SDVX_Edit" }, Cum = new long[] { 1469, 3074, 3855, 5988 }, Acc = new double?[] { null, null, null, null } },
                new DanLevel { Name = "LN 5th", Songs = new[] { "Pink Gold", "Milk", "Promise", "Blacksphere [Exhaust effect]" }, Cum = new long[] { 2956, 4569, 6482, 8363 }, Acc = new double?[] { null, null, null, null } },
                new DanLevel { Name = "LN 6th", Songs = new[] { "Koborezu no Negaigoto", "Power to Progress", "Mousou Express", "Berry Go!!" }, Cum = new long[] { 2324, 3736, 5627, 8048 }, Acc = new double?[] { null, null, null, null } },
                new DanLevel { Name = "LN 7th", Songs = new[] { "Rock It", "On The FM", "<3", "Tsumibito" }, Cum = new long[] { 2076, 4047, 5711, 8097 }, Acc = new double?[] { null, null, null, null } },
                new DanLevel { Name = "LN 8th", Songs = new[] { "Missing Banquet", "Shotgun Senorita", "Sakase Natsuzora, Koi no Hana", "Owari to Hajimari no Oto" }, Cum = new long[] { 2694, 5118, 7604, 10164 }, Acc = new double?[] { null, null, null, null } },
                new DanLevel { Name = "LN 9th", Songs = new[] { "DOgger's Gadget [Unmixed]", "Raijin no Hidariude", "Cold Planet", "Ordinary World" }, Cum = new long[] { 4069, 6662, 9087, 12786 }, Acc = new double?[] { null, null, null, null } },
                new DanLevel { Name = "LN 10th", Songs = new[] { "Towa no Maigo", "Dopamine", "Sakura Trip", "Element of SPADA" }, Cum = new long[] { 3925, 7045, 10411, 14066 }, Acc = new double?[] { null, null, null, null } },
                new DanLevel { Name = "LN 11th Yoake", Songs = new[] { "Enigma", "Avalon no Oka", "Love is War", "Asphixia" }, Cum = new long[] { 3507, 6019, 10449, 14974 }, Acc = new double?[] { null, null, null, null } },
                new DanLevel { Name = "LN 12th Yuugure", Songs = new[] { "Carnarion [CK Remix]", "No. 6", "Endymion", "Infinite Synthesis" }, Cum = new long[] { 2986, 7797, 11349, 15739 }, Acc = new double?[] { null, null, null, null } },
                new DanLevel { Name = "LN 13th Yoru", Songs = new[] { "Kyouki Ranmai", "Intersect Thunderbolt - Remix", "Literal Crystal In The Cyphisonia", "NEURO-CLOUD-9" }, Cum = new long[] { 4725, 8928, 12579, 17640 }, Acc = new double?[] { 97.44, 97.98, 97.46, 97.14 } },
                new DanLevel { Name = "LN 14th Yami", Songs = new[] { "Mare Nectaris", "Nhelv", "Indomitable Spirit", "The Lost" }, Cum = new long[] { 4415, 8735, 13053, 17000 }, Acc = new double?[] { null, null, null, null } },
                new DanLevel { Name = "LN 15th Yume", Songs = new[] { "Kodoku no Kakurenbo", "Inaiinai Isonshou", "White Hair Little Swords Girl", "Nuclear-Star" }, Cum = new long[] { 6153, 11814, 17718, 23681 }, Acc = new double?[] { null, null, null, null } },
                new DanLevel { Name = "LN 16th Yokaze", Songs = new[] { "Protoflicker", "Cinderella Cage -Trancecore Mix-", "Lancelot ~Flame of the Rebellion~", "R U Still xxxx?" }, Cum = new long[] { 5052, 8027, 11617, 18217 }, Acc = new double?[] { null, null, null, null } },
            } },
        },
        ["6k"] = new System.Collections.Generic.List<DanCourse>
        {
            new DanCourse { Name = "6k Regular段位(Arkman)", Levels = new()
            {
                new DanLevel { Name = "Start", Songs = new[] { "Medicine of love", "Quo Vadis", "Mr.Magic", "Junbi Undou" }, Cum = new long[] { 1185, 2768, 4057, 5356 }, Acc = new double?[] { null, null, null, null } },
                new DanLevel { Name = "Regular 1st", Songs = new[] { "Twinkle Wonderland", "Invitation II", "fairy stage", "GOODTEK" }, Cum = new long[] { 1520, 3225, 4820, 6205 }, Acc = new double?[] { null, null, null, null } },
                new DanLevel { Name = "Regular 2nd", Songs = new[] { "waxing and wanding", "Joyeuse", "Life Will Change", "RegreT of MemoRy" }, Cum = new long[] { 1677, 3073, 4893, 6527 }, Acc = new double?[] { null, null, null, null } },
                new DanLevel { Name = "Regular 3rd", Songs = new[] { "Highscore", "SpaceLand TOYBOX", "sinus", "Reimei Sketchbook" }, Cum = new long[] { 1982, 3678, 5768, 7358 }, Acc = new double?[] { null, null, null, null } },
                new DanLevel { Name = "Regular 4th", Songs = new[] { "Clotho and the stargazer", "Kanagawa Cyber Culvert", "Broken", "++" }, Cum = new long[] { 2251, 4610, 6926, 8928 }, Acc = new double?[] { null, null, null, null } },
                new DanLevel { Name = "Regular 5th", Songs = new[] { "Tori no Uta", "The Lady is a Trump", "Paradission", "Amnehilesie" }, Cum = new long[] { 2334, 4650, 6846, 9255 }, Acc = new double?[] { null, null, null, null } },
                new DanLevel { Name = "Regular 6th", Songs = new[] { "Milk Crown on Sonnetica", "Danbo", "Hard Renaissamba", "Contrapasso -paradiso-" }, Cum = new long[] { 2751, 5131, 8082, 10135 }, Acc = new double?[] { null, null, null, null } },
                new DanLevel { Name = "Regular 7th", Songs = new[] { "fffff op.2", "Mizu Aomu", "Lazurite", "Airengokuka" }, Cum = new long[] { 2614, 4734, 7486, 10268 }, Acc = new double?[] { null, null, null, null } },
                new DanLevel { Name = "Regular 8th", Songs = new[] { "WHIRLWIND", "Futari no Haijou Yuutouen", "Ultimate Weapon", "SEITEN NO TERIYAKI" }, Cum = new long[] { 2683, 5132, 8133, 10185 }, Acc = new double?[] { null, null, null, null } },
                new DanLevel { Name = "Regular 9th", Songs = new[] { "LIGHTFALL", "Awakening", "Disorder", "Torikago no Serenade" }, Cum = new long[] { 2958, 5365, 8145, 11120 }, Acc = new double?[] { null, null, null, null } },
            } },
            new DanCourse { Name = "6k LN段位([Crz]sunnyxxy)", Levels = new()
            {
                new DanLevel { Name = "LN 0th", Songs = new[] { "Tsuki Akari", "Beautiful Soldier (TV-Size)", "aLIEz (TV-Size)", "Scarborough Fair" }, Cum = new long[] { 475, 1107, 1537, 2164 }, Acc = new double?[] { null, null, null, null } },
                new DanLevel { Name = "LN 1st", Songs = new[] { "White Princess", "Good job!", "Onigami", "Reimu-instrumental-" }, Cum = new long[] { 1438, 2346, 2743, 3391 }, Acc = new double?[] { null, null, null, null } },
                new DanLevel { Name = "LN 2nd", Songs = new[] { "Luminous Sword", "CANDY MAGIC", "Empyrean", "For Fruits Basket" }, Cum = new long[] { 1193, 2066, 2453, 3168 }, Acc = new double?[] { null, null, null, null } },
                new DanLevel { Name = "LN 3rd", Songs = new[] { "Nine Point Eight", "Eclipse Parade", "Protoflicker", "Loadstar" }, Cum = new long[] { 1671, 3660, 4186, 5275 }, Acc = new double?[] { null, null, null, null } },
                new DanLevel { Name = "LN 4th", Songs = new[] { "future gazer (TV Size)", "Hunny Bunny", "Vajra", "PRAGMATISM" }, Cum = new long[] { 1369, 3060, 3439, 4699 }, Acc = new double?[] { null, null, null, null } },
                new DanLevel { Name = "LN 5th", Songs = new[] { "Voice of Passion", "Akination", "Fable", "Yoiyami Hanabi" }, Cum = new long[] { 1885, 3750, 4530, 6477 }, Acc = new double?[] { null, null, null, null } },
                new DanLevel { Name = "LN 6th", Songs = new[] { "Dear Another Nova", "Knight of Firmament", "Bravely You (TV Size)", "Shoujo Kisou Kyoku ~ Dream Battle" }, Cum = new long[] { 2147, 4154, 5017, 6487 }, Acc = new double?[] { null, null, null, null } },
                new DanLevel { Name = "LN 7th", Songs = new[] { "BLACK or WHITE?", "Kono Yo no Hate de Koi wo Utau Shoujo YU-NO", "Saramandora no Odoriko", "PAPERMOON (TV Size)" }, Cum = new long[] { 1730, 3213, 5010, 6534 }, Acc = new double?[] { null, null, null, null } },
                new DanLevel { Name = "LN 8th", Songs = new[] { "Beat in Angel", "Snow Storm -Euphoria-", "Act Beloved", "bullet (TV Size)" }, Cum = new long[] { 1960, 4299, 6545, 8040 }, Acc = new double?[] { null, null, null, null } },
                new DanLevel { Name = "LN 9th", Songs = new[] { "Sink into Nightmare", "Heart of Witch", "Altale", "Raijin no Hidariude" }, Cum = new long[] { 2125, 4108, 5546, 7209 }, Acc = new double?[] { null, null, null, null } },
                new DanLevel { Name = "LN 10th", Songs = new[] { "Say A Vengeance", "Plastic Mind", "Identity Part 5", "The Clockwork Rose -Tokei Shikake no Bara Shoujo-" }, Cum = new long[] { 2432, 5694, 8046, 10249 }, Acc = new double?[] { null, null, null, null } },
                new DanLevel { Name = "LN 11th", Songs = new[] { "squartatrice", "Black Lotus", "ouroboros -twin stroke of the end-", "Kanagawa Cyber Culvert" }, Cum = new long[] { 2819, 5560, 8411, 10800 }, Acc = new double?[] { null, null, null, null } },
                new DanLevel { Name = "LN 12th", Songs = new[] { "LivedaM", "Hesperides", "Inherited World", "GEOMETRIA" }, Cum = new long[] { 2394, 5336, 7468, 9617 }, Acc = new double?[] { null, null, null, null } },
                new DanLevel { Name = "LN 13th", Songs = new[] { "Nibelungen", "Rubeus", "* Crow Solace *", "Invincible" }, Cum = new long[] { 3419, 6168, 8782, 11566 }, Acc = new double?[] { null, null, null, null } },
                new DanLevel { Name = "LN 14th", Songs = new[] { "Exitium", "HAVOX", "Xronier", "Ketsarku Mozgalom" }, Cum = new long[] { 3844, 7322, 10453, 13347 }, Acc = new double?[] { null, null, null, null } },
            } },
        },
        ["7k"] = new System.Collections.Generic.List<DanCourse>
        {
            new DanCourse { Name = "7k Regular段位", Levels = new()
            {
                new DanLevel { Name = "Regular 0th", Songs = new[] { "anemone", "Who You Evidently Thought Was Me", "End Time", "Yuki wa Doko ni? Kimi no Mune ni!" }, Cum = new long[] { 1018, 1869, 2892, 4115 }, Acc = new double?[] { null, null, null, null } },
                new DanLevel { Name = "Regular 1st", Songs = new[] { "dir", ".357 Magnum", "Aragami", "Tsuki -Yue-" }, Cum = new long[] { 1436, 3250, 4562, 6235 }, Acc = new double?[] { null, null, null, null } },
                new DanLevel { Name = "Regular 2nd", Songs = new[] { "East West Wobble", "Schur's Theorem", "Hesperides", "Gyspy Tronic" }, Cum = new long[] { 1411, 2847, 4646, 6800 }, Acc = new double?[] { null, null, null, null } },
                new DanLevel { Name = "Regular 3rd", Songs = new[] { "Haryu", "Electro Peaceful", "Altale", "CHEVRON" }, Cum = new long[] { 1970, 3876, 5747, 7729 }, Acc = new double?[] { null, null, null, null } },
                new DanLevel { Name = "Regular 4th", Songs = new[] { "MEGALOVANIA", "My Son, the Leopard", "ETERNAL DRAIN", "electro peaceful (8bit)" }, Cum = new long[] { 2189, 4042, 6150, 8414 }, Acc = new double?[] { null, null, null, null } },
                new DanLevel { Name = "Regular 5th", Songs = new[] { "AciculignosA", "Passage D", "Speed of Link", "Blue Planet" }, Cum = new long[] { 2087, 4254, 6229, 8620 }, Acc = new double?[] { null, null, null, null } },
                new DanLevel { Name = "Regular 6th", Songs = new[] { "Gabberse Town", "Isometry", "44river", "Imperishable Night 2006" }, Cum = new long[] { 1576, 4394, 6762, 9765 }, Acc = new double?[] { null, null, null, null } },
                new DanLevel { Name = "Regular 7th", Songs = new[] { "Sudden Romance", "Rengoku -Purgatorium-", "Witches Brew", "Out Of The Blue" }, Cum = new long[] { 2689, 5565, 7503, 10350 }, Acc = new double?[] { null, null, null, null } },
                new DanLevel { Name = "Regular 8th", Songs = new[] { "Overworld", "Bangin' Burst", "Yozakura", "Tiny Tales Continue" }, Cum = new long[] { 2366, 4874, 7699, 10635 }, Acc = new double?[] { null, null, null, null } },
                new DanLevel { Name = "Regular 9th", Songs = new[] { "gazer", "Ubiquity", "Mario Paint", "The Solace of Oblivion" }, Cum = new long[] { 2420, 5092, 7322, 10769 }, Acc = new double?[] { null, null, null, null } },
                new DanLevel { Name = "Regular 10th", Songs = new[] { "SAMBA LAND", "Toki (maidable Remix)", "Blue-White Crazystars", "Fox4-Raize-" }, Cum = new long[] { 3081, 5447, 7958, 11396 }, Acc = new double?[] { 97.66, 96.33, 95.47, 94.76 } },
                new DanLevel { Name = "Regular Gamma", Songs = new[] { "Identity Part 4", "EGOISM 440", "Vertex GAMMA", "Paraclete" }, Cum = new long[] { 3085, 5585, 8292, 12003 }, Acc = new double?[] { null, null, null, null } },
                new DanLevel { Name = "Regular Azumuth", Songs = new[] { "TENSHI (True Final Boss)", "najimi breakers", "Nijuu", "Finixe" }, Cum = new long[] { 3644, 6818, 10078, 14219 }, Acc = new double?[] { null, null, null, null } },
                new DanLevel { Name = "Regular Zenith", Songs = new[] { "Icicle Stinger", "Rinjuu", "Der Wald", "We Want To Run" }, Cum = new long[] { 3624, 6346, 9604, 14578 }, Acc = new double?[] { null, null, null, null } },
                new DanLevel { Name = "Regular Stellium", Songs = new[] { "EXILE", "Wanderflux", "P8107", "Triumphal Return" }, Cum = new long[] { 4000, 8502, 11816, 16384 }, Acc = new double?[] { null, null, null, null } },
            } },
            new DanCourse { Name = "7k LN段位", Levels = new()
            {
                new DanLevel { Name = "LN 0th", Songs = new[] { "Crayon Tulip", "lit(var)", "Reimu-instrumental-", "if Hitori Omou" }, Cum = new long[] { 820, 1420, 1635, 1882 }, Acc = new double?[] { null, null, null, null } },
                new DanLevel { Name = "LN 1st", Songs = new[] { "Mawaru Sora Usagi", "The First Layer", "Portamento", "Dango Daikazoku Mix Piano" }, Cum = new long[] { 1068, 1750, 2287, 2715 }, Acc = new double?[] { null, null, null, null } },
                new DanLevel { Name = "LN 2nd", Songs = new[] { "Mr. Darling", "Take It to the Limit", "Yumeoi", "HARAJUKU IYAHOI" }, Cum = new long[] { 1242, 2440, 3200, 4421 }, Acc = new double?[] { null, null, null, null } },
                new DanLevel { Name = "LN 3rd", Songs = new[] { "athletic meet of sound toy", "Panic Holic", "XROSS INFECTION", "12:30" }, Cum = new long[] { 1512, 2862, 3185, 4084 }, Acc = new double?[] { null, null, null, null } },
                new DanLevel { Name = "LN 4th", Songs = new[] { "Doku Ryuu no Kobura", "The Limbo", "Be Happy*", "Main Theme" }, Cum = new long[] { 1309, 2827, 3370, 4106 }, Acc = new double?[] { null, null, null, null } },
                new DanLevel { Name = "LN 5th", Songs = new[] { "Chocolate Disco", "Oath Nova Apocalypse", "SAVIOR OF SONG", "inside" }, Cum = new long[] { 1516, 4211, 4813, 5431 }, Acc = new double?[] { null, null, null, null } },
                new DanLevel { Name = "LN 6th", Songs = new[] { "Lune", "'Trockne Blumen'", "Chiisana Boukensha", "in flight (zygani Remix)" }, Cum = new long[] { 1635, 3537, 4293, 5374 }, Acc = new double?[] { null, null, null, null } },
                new DanLevel { Name = "LN 7th", Songs = new[] { "YES or YES", "Towards the Horizon", "brave heart", "Rockhill" }, Cum = new long[] { 1796, 3889, 5248, 6639 }, Acc = new double?[] { null, null, null, null } },
                new DanLevel { Name = "LN 8th", Songs = new[] { "Imprinting", "Sailing Force", "Heart", "Romancing Layer" }, Cum = new long[] { 2230, 4366, 5831, 7193 }, Acc = new double?[] { null, null, null, null } },
                new DanLevel { Name = "LN 9th", Songs = new[] { "New Gravity", "Enigma", "Hustle Beat!!", "Onigami" }, Cum = new long[] { 1900, 4059, 5656, 7333 }, Acc = new double?[] { 96.34, 95.62, 94.77, 94.93 } },
                new DanLevel { Name = "LN 10th", Songs = new[] { "SpaceLand TOYBOX", "CODE -CRiMSON-", "attack in the minor key", "Internet bitch P*Light Remix" }, Cum = new long[] { 2151, 4647, 6492, 8200 }, Acc = new double?[] { null, null, null, null } },
                new DanLevel { Name = "LN Gamma", Songs = new[] { "quaver", "Hellfire", "Pure Ruby", "Schrodinger no neko" }, Cum = new long[] { 2551, 4877, 7150, 9315 }, Acc = new double?[] { null, null, null, null } },
                new DanLevel { Name = "LN Azimuth", Songs = new[] { "JOMANDA", "Big Brother", "licca", "Zettai Reido" }, Cum = new long[] { 3029, 5501, 8054, 10980 }, Acc = new double?[] { null, null, null, null } },
                new DanLevel { Name = "LN Zenith", Songs = new[] { "Redshift", "Dement ~After Legend~", "Black Lotus", "Alexandrite" }, Cum = new long[] { 3240, 6265, 9446, 12350 }, Acc = new double?[] { null, null, null, null } },
                new DanLevel { Name = "LN Stellium", Songs = new[] { "Decoy", "Unleashed World", "Nibelungen", "Lancelot ~Flame of the Rebellion~" }, Cum = new long[] { 4073, 7527, 10723, 14444 }, Acc = new double?[] { null, null, null, null } },
            } },
        },
    };
}
