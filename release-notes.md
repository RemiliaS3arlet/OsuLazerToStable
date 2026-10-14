# v1.1.1 更新

## 本次改进：变速谱面导入不再覆盖/更新原谱面

- 生成的变速谱面现在会被 osu! 识别为**全新的本地谱面**：
  - `BeatmapID` / `BeatmapSetID` 强制写为 `-1`（原文件缺这两行时也会自动补写）；
  - 难度名（Version）自动追加 `(1.XXx)` 后缀，例如 `MEGADROPTHRUUUUUUUCLOUD (1.25x)`。
- 导入 osu!stable 或 osu!lazer 时：不会覆盖原谱面、不会弹出「更新」提示；与原谱面并存，互不影响。

## 下载说明

- **`OsuLazerToStable-win-x64.zip`** — 自包含版，解压双击即用，无需安装 .NET（推荐）
- **`OsuLazerToStable-win-x64-framework.zip`** — 轻量版，需已安装 .NET 8 桌面运行时
