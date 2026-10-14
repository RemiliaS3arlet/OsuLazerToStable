# v1.1.2 更新

## 修复：变速谱面导入 stable / lazer 不再干扰原谱面、不再提示更新

彻底本地化变速谱面（已在真实 lazer 上端到端实测通过）：

- `BeatmapID` 写为 **0**、`BeatmapSetID` 写为 **-1**（社区惯例，原文件缺这两行时自动补写）；
- **标题**与**难度名**自动追加倍速后缀，例如 `HYPERFREEFALLTOOOOOOOSKY (1.25x)` / `MEGADROPTHRUUUUUUUCLOUD (1.25x)`；
- 默认输出文件名**去掉原文件名开头的谱面集 ID**（如 `2606612`），stable 用文件名建新目录时不会与原谱面混淆；
- 实测：生成文件经命令行交给 osu!lazer 导入后，realm 中出现**独立的新谱面集且 OnlineID=-1**（纯本地身份），与原谱面集完全分离。

## 新增：倍速范围扩大到 0.50x ~ 2.00x

- 支持 **0.50x（半速）到 2.00x（双倍速）**，精确到 0.01；
- 音频（ffmpeg atempo）与谱面节奏（时间点、BPM、滑条/长条/转盘、休息段等）整体同步变速。

## 下载说明

- **`OsuLazerToStable-win-x64.zip`** — 自包含版，解压双击即用，无需安装 .NET（推荐）
- **`OsuLazerToStable-win-x64-framework.zip`** — 轻量版，需已安装 .NET 8 桌面运行时
