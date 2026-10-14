using System.Diagnostics;
using System.Globalization;
using System.IO.Compression;
using System.Text;

namespace OsuLazerToStable;

/// <summary>
/// osz 倍速生成器：把谱面整体变速 1.00x ~ 2.00x（音频用 ffmpeg atempo 变速，.osu 内所有时间点与 BPM 同步缩放）。
/// 生成结果另存为新文件，文件名加 "(1.XX)" 前缀。
/// </summary>
public static class RateChanger
{
    /// <summary>查找 ffmpeg.exe：程序同目录 → 系统 PATH。</summary>
    public static string FindFfmpeg()
    {
        string local = Path.Combine(AppContext.BaseDirectory, "ffmpeg.exe");
        if (File.Exists(local)) return local;

        string? pathVar = Environment.GetEnvironmentVariable("PATH");
        if (pathVar != null)
        {
            foreach (string dir in pathVar.Split(';'))
            {
                if (dir.Trim().Length == 0) continue;
                string candidate = Path.Combine(dir.Trim(), "ffmpeg.exe");
                if (File.Exists(candidate)) return candidate;
            }
        }
        return string.Empty;
    }

    public static void Generate(string inputOsz, double speed, string outputOsz, Action<string>? log = null)
    {
        speed = Math.Round(speed, 2, MidpointRounding.AwayFromZero);
        if (speed < 1.0 - 0.0001 || speed > 2.0 + 0.0001)
            throw new ArgumentException("倍速必须在 1.00 ~ 2.00 之间");
        if (!File.Exists(inputOsz))
            throw new FileNotFoundException("找不到输入文件: " + inputOsz);
        if (string.IsNullOrWhiteSpace(outputOsz))
            throw new ArgumentException("请指定输出文件路径");

        string workDir = Path.Combine(Path.GetTempPath(), "osuRate_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(workDir);
        try
        {
            log?.Invoke($"解压 {Path.GetFileName(inputOsz)} …");
            ZipFile.ExtractToDirectory(inputOsz, workDir);

            var osuFiles = Directory.GetFiles(workDir, "*.osu", SearchOption.AllDirectories).ToList();
            if (osuFiles.Count == 0)
                throw new InvalidDataException("该 osz 中没有 .osu 谱面文件");

            // 收集所有难度引用的音频
            var audioNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string f in osuFiles)
                audioNames.UnionWith(GetAudioFilenames(f));

            var audioFiles = new List<string>();
            foreach (string n in audioNames)
            {
                string p = Path.Combine(workDir, n.Replace('/', Path.DirectorySeparatorChar));
                if (File.Exists(p)) audioFiles.Add(p);
                else log?.Invoke($"[警告] 音频文件不存在，跳过: {n}");
            }

            if (audioFiles.Count > 0 && Math.Abs(speed - 1.0) > 0.0001)
            {
                string ffmpeg = FindFfmpeg();
                if (ffmpeg.Length == 0)
                    throw new InvalidOperationException(
                        "未找到 ffmpeg.exe。倍速谱面需要对音频变速，请下载 ffmpeg（https://www.gyan.dev/ffmpeg/builds/）" +
                        "并把 ffmpeg.exe 放到本程序同目录，或加入系统 PATH。");
                foreach (string a in audioFiles)
                {
                    log?.Invoke($"音频变速 {speed:0.00}x: {Path.GetFileName(a)} …");
                    StretchAudio(a, speed, ffmpeg);
                }
            }
            else if (audioFiles.Count == 0)
            {
                log?.Invoke("该谱面没有音频文件，仅改写谱面节奏");
            }

            foreach (string f in osuFiles)
            {
                log?.Invoke($"改写谱面节奏: {Path.GetFileName(f)}");
                RewriteOsu(f, speed, log);
            }

            log?.Invoke("打包生成新 osz …");
            string tmp = outputOsz + ".tmp";
            if (File.Exists(tmp)) File.Delete(tmp);
            ZipFile.CreateFromDirectory(workDir, tmp);
            if (File.Exists(outputOsz)) File.Delete(outputOsz);
            File.Move(tmp, outputOsz);
            log?.Invoke($"生成完成: {outputOsz}");
        }
        finally
        {
            try { Directory.Delete(workDir, true); } catch { }
        }
    }

    // ---------- .osu 改写 ----------

    private static void RewriteOsu(string path, double r, Action<string>? log)
    {
        byte[] raw = File.ReadAllBytes(path);
        bool utf8Bom = raw.Length >= 3 && raw[0] == 0xEF && raw[1] == 0xBB && raw[2] == 0xBF;
        bool utf16le = raw.Length >= 2 && raw[0] == 0xFF && raw[1] == 0xFE;
        bool utf16be = raw.Length >= 2 && raw[0] == 0xFE && raw[1] == 0xFF;
        string text = utf8Bom ? new UTF8Encoding(false).GetString(raw, 3, raw.Length - 3)
            : utf16le ? Encoding.Unicode.GetString(raw, 2, raw.Length - 2)
            : utf16be ? Encoding.BigEndianUnicode.GetString(raw, 2, raw.Length - 2)
            : Encoding.UTF8.GetString(raw);

        var sb = new StringBuilder(text.Length + 512);
        string section = "";
        bool warnedStoryboard = false;
        bool sawBeatmapId = false, sawBeatmapSetId = false;

        using var sr = new StringReader(text);
        string? line;
        while ((line = sr.ReadLine()) != null)
        {
            string trimmed = line.Trim();
            if (trimmed.StartsWith('[') && trimmed.EndsWith(']'))
            {
                // 离开 [Metadata] 前补齐 ID 行，确保 osu 把该谱面当作全新本地谱面
                if (section == "[Metadata]")
                {
                    if (!sawBeatmapSetId) sb.AppendLine("BeatmapSetID:-1");
                    if (!sawBeatmapId) sb.AppendLine("BeatmapID:-1");
                }
                section = trimmed;
                sb.AppendLine(line);
                continue;
            }

            if (section == "[General]")
            {
                if (line.StartsWith("AudioLeadIn:", StringComparison.OrdinalIgnoreCase)
                    && int.TryParse(line[12..].Trim(), out int lead))
                {
                    sb.AppendLine("AudioLeadIn:" + ScaleInt(lead, r));
                    continue;
                }
                if (line.StartsWith("PreviewTime:", StringComparison.OrdinalIgnoreCase)
                    && int.TryParse(line[12..].Trim(), out int preview) && preview > 0)
                {
                    sb.AppendLine("PreviewTime:" + ScaleInt(preview, r));
                    continue;
                }
            }
            else if (section == "[Metadata]")
            {
                if (line.StartsWith("BeatmapID:", StringComparison.OrdinalIgnoreCase))
                {
                    sb.AppendLine("BeatmapID:-1");
                    sawBeatmapId = true;
                    continue;
                }
                if (line.StartsWith("BeatmapSetID:", StringComparison.OrdinalIgnoreCase))
                {
                    sb.AppendLine("BeatmapSetID:-1");
                    sawBeatmapSetId = true;
                    continue;
                }
                if (line.StartsWith("Version:", StringComparison.OrdinalIgnoreCase))
                {
                    // 难度名追加倍速后缀：即使与原始谱面合并进同一谱面集，也不会顶替原难度
                    sb.AppendLine("Version:" + line[8..].TrimEnd() + $" ({r:0.00}x)");
                    continue;
                }
            }
            else if (section == "[Editor]" && line.StartsWith("Bookmarks:", StringComparison.OrdinalIgnoreCase))
            {
                var scaled = line[10..].Split(',')
                    .Where(p => int.TryParse(p.Trim(), out _))
                    .Select(p => ScaleInt(int.Parse(p.Trim()), r).ToString(CultureInfo.InvariantCulture));
                sb.AppendLine("Bookmarks:" + string.Join(",", scaled));
                continue;
            }
            else if (section == "[TimingPoints]" && trimmed.Length > 0)
            {
                var p = line.Split(',');
                if (p.Length >= 2
                    && double.TryParse(p[0].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double offset))
                {
                    p[0] = ScaleInt(offset, r).ToString(CultureInfo.InvariantCulture);
                    bool uninherited = p.Length >= 7 && p[6].Trim() == "1";
                    if (uninherited
                        && double.TryParse(p[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double beatLength))
                    {
                        p[1] = FormatDouble(beatLength / r);
                    }
                    sb.AppendLine(string.Join(",", p));
                    continue;
                }
            }
            else if (section == "[Events]" && trimmed.Length > 0)
            {
                var p = line.Split(',');
                if (p[0].Trim() == "Video" && p.Length >= 2 && int.TryParse(p[1].Trim(), out int videoStart))
                {
                    p[1] = ScaleInt(videoStart, r).ToString(CultureInfo.InvariantCulture);
                    sb.AppendLine(string.Join(",", p));
                    continue;
                }
                if (p[0].Trim() == "2" && p.Length >= 3
                    && int.TryParse(p[1].Trim(), out int breakStart) && int.TryParse(p[2].Trim(), out int breakEnd))
                {
                    p[1] = ScaleInt(breakStart, r).ToString(CultureInfo.InvariantCulture);
                    p[2] = ScaleInt(breakEnd, r).ToString(CultureInfo.InvariantCulture);
                    sb.AppendLine(string.Join(",", p));
                    continue;
                }
                if (!warnedStoryboard && p.Length >= 3 && p[0].Trim() is "4" or "5" or "6")
                {
                    warnedStoryboard = true;
                    log?.Invoke("[提示] 该谱面含 storyboard，其时间轴保持原速（不影响游玩，仅视觉效果）");
                }
            }
            else if (section == "[HitObjects]" && trimmed.Length > 0)
            {
                var p = line.Split(',');
                if (p.Length >= 4
                    && int.TryParse(p[2].Trim(), out int time)
                    && int.TryParse(p[3].Trim(), out int type))
                {
                    p[2] = ScaleInt(time, r).ToString(CultureInfo.InvariantCulture);

                    if ((type & 128) != 0 && p.Length >= 6)
                    {
                        // osu!mania 长条: endTime:hitSample
                        int ci = p[5].IndexOf(':');
                        if (ci > 0 && int.TryParse(p[5][..ci], out int holdEnd))
                            p[5] = ScaleInt(holdEnd, r).ToString(CultureInfo.InvariantCulture) + p[5][ci..];
                    }
                    else if ((type & 8) != 0 && p.Length >= 6 && int.TryParse(p[5].Trim(), out int spinEnd))
                    {
                        // 转盘: endTime
                        p[5] = ScaleInt(spinEnd, r).ToString(CultureInfo.InvariantCulture);
                    }
                    // 滑条: 长度与路径不变（BPM 已按比例加快，时长自动缩短）

                    sb.AppendLine(string.Join(",", p));
                    continue;
                }
            }

            sb.AppendLine(line);
        }

        // [Metadata] 是文件最后一段时，循环后补插 ID 行
        if (section == "[Metadata]")
        {
            if (!sawBeatmapSetId) sb.AppendLine("BeatmapSetID:-1");
            if (!sawBeatmapId) sb.AppendLine("BeatmapID:-1");
        }

        byte[] output = utf8Bom ? new UTF8Encoding(true).GetBytes(sb.ToString())
            : utf16le ? Encoding.Unicode.GetPreamble().Concat(Encoding.Unicode.GetBytes(sb.ToString())).ToArray()
            : utf16be ? Encoding.BigEndianUnicode.GetPreamble().Concat(Encoding.BigEndianUnicode.GetBytes(sb.ToString())).ToArray()
            : Encoding.UTF8.GetBytes(sb.ToString());
        File.WriteAllBytes(path, output);
    }

    private static long ScaleInt(double value, double r) =>
        Math.Max(0, (long)Math.Round(value / r, MidpointRounding.AwayFromZero));

    private static string FormatDouble(double d)
    {
        string s = d.ToString("0.####################", CultureInfo.InvariantCulture);
        int dot = s.IndexOf('.');
        if (dot >= 0)
        {
            s = s.TrimEnd('0');
            if (s.EndsWith('.')) s = s[..^1];
        }
        return s.Length == 0 ? "0" : s;
    }

    private static IEnumerable<string> GetAudioFilenames(string osuPath)
    {
        string text = File.ReadAllText(osuPath); // 自动识别 BOM
        bool inGeneral = false;
        foreach (string raw in text.Split('\n'))
        {
            string line = raw.TrimEnd('\r').Trim();
            if (line.StartsWith('['))
            {
                inGeneral = line.StartsWith("[General]", StringComparison.OrdinalIgnoreCase);
                continue;
            }
            if (!inGeneral) continue;

            if (line.StartsWith("AudioFilename:", StringComparison.OrdinalIgnoreCase))
            {
                string v = line[14..].Trim();
                if (v.Length > 0) yield return v;
            }
        }
    }

    // ---------- 音频变速 ----------

    private static void StretchAudio(string file, double r, string ffmpeg)
    {
        string ext = Path.GetExtension(file).ToLowerInvariant();
        string codec = ext switch
        {
            ".mp3" => "libmp3lame",
            ".ogg" => "libvorbis",
            ".wav" => "pcm_s16le",
            ".m4a" => "aac",
            ".flac" => "flac",
            _ => "",
        };
        if (codec.Length == 0)
            throw new InvalidOperationException($"不支持的音频格式: {ext}");

        string tmp = file + ".rate" + ext; // 保持原扩展名，ffmpeg 依据扩展名判断输出格式
        if (File.Exists(tmp)) File.Delete(tmp);

        var psi = new ProcessStartInfo(ffmpeg)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardError = true,
        };
        psi.ArgumentList.Add("-hide_banner");
        psi.ArgumentList.Add("-y");
        psi.ArgumentList.Add("-i");
        psi.ArgumentList.Add(file);
        psi.ArgumentList.Add("-filter:a");
        psi.ArgumentList.Add("atempo=" + r.ToString("0.00", CultureInfo.InvariantCulture));
        psi.ArgumentList.Add("-vn");
        psi.ArgumentList.Add("-codec:a");
        psi.ArgumentList.Add(codec);
        if (ext is ".mp3" or ".m4a")
        {
            psi.ArgumentList.Add("-b:a");
            psi.ArgumentList.Add("192k");
        }
        else if (ext == ".ogg")
        {
            psi.ArgumentList.Add("-q:a");
            psi.ArgumentList.Add("5");
        }
        psi.ArgumentList.Add(tmp);

        using var p = Process.Start(psi)!;
        string err = p.StandardError.ReadToEnd();
        p.WaitForExit();
        if (p.ExitCode != 0)
        {
            string detail = err.TrimEnd();
            if (detail.Length > 300) detail = detail[^300..];
            throw new InvalidOperationException($"ffmpeg 音频变速失败: {detail}");
        }

        File.Copy(tmp, file, true);
        File.Delete(tmp);
    }
}
