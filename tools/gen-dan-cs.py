# -*- coding: utf-8 -*-
"""把 dan-courses.json 生成 C# 内嵌数据文件 DanCoursesData.cs"""
import json, sys

sys.stdout.reconfigure(encoding='utf-8')
SRC = r'C:\Users\somer\Documents\deepseek-harness\default-workspace\OsuTransfer\app\dan-courses.json'
OUT = r'C:\Users\somer\Documents\deepseek-harness\default-workspace\OsuTransfer\app\DanCoursesData.cs'

d = json.load(open(SRC, encoding='utf-8'))

def cs_str(s):
    return '"' + str(s).replace('\\', '\\\\').replace('"', '\\"').replace('\n', '\\n') + '"'

def cs_acc(a):
    if a is None:
        return 'null'
    return ('{0:.6f}'.format(a)).rstrip('0').rstrip('.')

lines = []
lines.append('// 自动生成的数据文件：来自《4 6 7k段位 個別ACC計算 by gyoza_goki & Blaite1016.xlsx》')
lines.append('// 每级 4 首曲目 + 累计音符数（T），通过時ACC 示例值（缺省为 null，用户自行输入）。')
lines.append('namespace OsuLazerToStable;')
lines.append('')
lines.append('public class DanLevel')
lines.append('{')
lines.append('    public string Name = string.Empty;')
lines.append('    public string[] Songs = System.Array.Empty<string>();')
lines.append('    public long[] Cum = System.Array.Empty<long>();')
lines.append('    public double?[] Acc = System.Array.Empty<double?>();')
lines.append('}')
lines.append('')
lines.append('public class DanCourse')
lines.append('{')
lines.append('    public string Name = string.Empty;')
lines.append('    public System.Collections.Generic.List<DanLevel> Levels = new();')
lines.append('}')
lines.append('')
lines.append('public static class DanCoursesData')
lines.append('{')
lines.append('    public static readonly System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<DanCourse>> Modes = new()')
lines.append('    {')
for mode in d:
    lines.append(f'        [{cs_str(mode)}] = new System.Collections.Generic.List<DanCourse>')
    lines.append('        {')
    for course in d[mode]:
        lines.append(f'            new DanCourse {{ Name = {cs_str(course["course"])}, Levels = new()')
        lines.append('            {')
        for lv in course['levels']:
            songs = ', '.join(cs_str(s) for s in lv['songs'])
            cum = ', '.join(str(c) for c in lv['cum'])
            acc = ', '.join(cs_acc(a) for a in lv['acc'])
            lines.append(f'                new DanLevel {{ Name = {cs_str(lv["level"])}, Songs = new[] {{ {songs} }}, Cum = new long[] {{ {cum} }}, Acc = new double?[] {{ {acc} }} }},')
        lines.append('            } },')
    lines.append('        },')
lines.append('    };')
lines.append('}')
lines.append('')

with open(OUT, 'w', encoding='utf-8') as f:
    f.write('\n'.join(lines))
print('written:', OUT, 'lines:', len(lines))
