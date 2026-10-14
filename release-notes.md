# v1.1.4 更新

## 根因修复：stable「更新」检测的真正钥匙

经过对 stable 导入行为的实测追踪，确认 stable 是按 **osz 内部的 .osu 文件名**匹配旧谱面的（此前改 Tags、删曲绘都无效）。本次修复：

- **内部 .osu 文件按新元数据重命名**：`Oriri - ... (1.20x) (-GRJ-) [MEGADROP... (1.20x)].osu`，与原谱面文件名不同，stable 不再将其识别为旧谱面的更新。
- 移除曲绘（背景图）默认开启：生成谱面与原谱面没有任何相同文件。
- 保留既有加固：BeatmapID=0 / BeatmapSetID=-1、标题与难度名后缀、Source 清空、Tags 随机字母、文件名去谱面集 ID。

## 实测（本机 stable 端到端）

导入 `(1.20) renamed test.osz` 后：
- Songs 出现独立新文件夹，**内容为修改版谱面原样**（标题/难度带 (1.20x)、ID 0/-1）；
- 原谱面文件夹与 osu!.db 原记录完全未受影响。

## 下载说明

- **`OsuLazerToStable-win-x64.zip`** — 自包含版，解压双击即用，无需安装 .NET（推荐）
- **`OsuLazerToStable-win-x64-framework.zip`** — 轻量版，需已安装 .NET 8 桌面运行时
