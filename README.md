# osu!lazer → stable 迁移工具

把 osu!lazer 中的**谱面**和**收藏夹**自动迁移到 osu!stable 的 Windows 图形化工具，附带 **osz 倍速生成器**。

## 下载

👉 **最新版下载：https://github.com/fallencatww/OsuLazerToStable/releases/latest**

在 Assets 里下载 `OsuLazerToStable-win-x64.zip`（自包含版，解压后双击即用，无需安装 .NET；已附带 ffmpeg.exe）。

## 功能

**一、谱面迁移**
- 只读访问 lazer 的 `client.realm` 与 `files` 文件仓库，**绝不修改 lazer 的任何数据**（lazer 开着也能用）。
- 两种迁移模式：**仅迁移 stable 缺失的谱面**（默认，按谱面集 ID 或 .osu 文件 MD5 判定）/ **迁移全部**。
- 「检测差异」按钮：一键检测 lazer 中有哪些谱面集在 stable 中缺失，并生成缺失清单文件（logs 目录）。
- 谱面按谱面集重建到 stable 的 `Songs` 目录，文件夹命名与 stable 惯例一致：`{谱面集ID} {艺术家} - {标题}`。
- 收藏夹写入 stable 的 `collection.db`（与 lazer 相同的「Favourites」收藏夹也会一并迁移）；写入前自动备份原文件。
- 中文界面，带进度条、完整日志（界面 + 本地日志文件）。

**二、倍速生成（osz 变速）**
- 把 `.osz` 谱面文件**拖入**（或点击选择），选择倍速 **1.00x ~ 2.00x（精确到 0.01）**，点「生成变速谱面」。
- 音频（ffmpeg atempo）与谱面节奏（时间点、BPM、滑条/长条/转盘、休息段等）整体同步变速。
- 生成结果**另存为新文件**，文件名自动加 `(1.XX)` 前缀，保存位置由你选择；原谱面不受影响。
- 生成的谱面自动清空 BeatmapID/BeatmapSetID（原文件缺这两行时也会自动补写 `-1`），并把难度名改为 `原难度 (1.XXx)`——导入 stable 或 lazer 时会被识别为**全新的本地谱面**，绝不覆盖原谱面、不会触发更新提示。

## 使用方法

### 谱面迁移

1. 关闭 **osu!stable**（迁移谱面时不必关，但**写 collection.db 前必须关闭**，否则会被游戏退出时覆盖；工具会自动检测并提示）。
2. 运行 `OsuLazerToStable.exe`，在「谱面迁移」页确认两个路径（一般会自动检测）：
   - lazer 数据目录：`%APPDATA%\osu`（内含 `client.realm` 和 `files`）
   - stable 安装目录：`%LOCALAPPDATA%\osu!`（内含 `osu!.exe` 和 `Songs`）
3. 点「刷新检测」看两边数据，或点「检测差异」看缺失清单。
4. 勾选要迁移的内容，点「开始迁移」；完成后重启 osu!stable。

### 倍速生成

1. 切到「倍速生成」页，把 `.osz` 拖进虚线框（或点击选择文件）。
2. 选择倍速（1.00 ~ 2.00）。
3. 点「生成变速谱面」，选择保存位置（默认文件名已带 `(1.XX)` 前缀），等待完成。
4. 音频变速依赖 **ffmpeg.exe**：发布包已附带；若自行构建，请把 ffmpeg.exe 放到程序同目录或加入系统 PATH。

### 选项说明（迁移页）

| 选项 | 说明 |
| --- | --- |
| 迁移谱面 | 把 lazer 的谱面集复制进 stable 的 Songs 目录 |
| 仅迁移 stable 缺失的谱面 | 默认开启；关闭则迁移全部（stable 已有的会以 (2) 等后缀另建文件夹，不会覆盖） |
| 迁移收藏夹 | 把 lazer 的收藏夹（含 Favourites）写入 stable 的 collection.db |
| 同名收藏夹合并 | 勾选时，与 stable 已有收藏夹同名的做并集合并；不勾选时以 lazer 内容覆盖同名项 |
| 演练模式 | 只统计和打印将要做的事，不写任何文件 |

### 命令行模式（进阶）

```
# 迁移 / 差异检测
OsuLazerToStable.exe --cli [--lazer 目录] [--stable 目录] [--maps|--no-maps]
                  [--collections|--no-collections] [--only-missing|--all-maps]
                  [--detect-only] [--dry-run] [--merge|--no-merge]

# 倍速生成
OsuLazerToStable.exe --cli --rate 输入.osz --speed 1.25 [--out 输出.osz]
```

## 注意事项

- **收藏夹引用的是谱面的 MD5**。lazer 收藏夹里如果引用了 stable 中不存在的谱面（例如谱面还没迁移、或 lazer 的 .osu 被编辑过），该引用会被丢弃并在日志中计数。先把谱面迁移完再迁移收藏夹，命中率最高。
- lazer 的「Favourites」在 stable 中会成为一个同名**收藏夹**（stable 的心形「收藏」存在 osu!.db 里，本工具不修改数据库，避免破坏成绩数据）。
- 倍速生成的谱面若带 storyboard，其时间轴保持原速（仅视觉，不影响游玩）。
- 若 lazer 未来更新导致 realm 结构变化，工具会自动适配 schema 版本；若模型字段发生大改导致无法打开，会给出明确报错。
- 迁移大仓库（几万文件、几十 GB）需要一些时间，请耐心等待；随时可点「取消」，已完成的文件会保留。

## 构建

需要 .NET 8 SDK：

```
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

生成自包含单文件（无需安装运行时）；省略 `--self-contained` 则生成依赖 .NET 8 桌面运行时的轻量版。倍速功能需把 ffmpeg.exe 放到程序同目录（LGPL 构建可从 https://github.com/BtbN/FFmpeg-Builds/releases 获取）。

## 技术要点

- Realm 数据通过 `Realm` NuGet（20.1.0，与 lazer 所用版本一致）以只读方式打开，schema 类仅声明迁移所需的 4 个模型（`BeatmapCollection`、`BeatmapSet`、`RealmNamedFileUsage`、`File`），与 lazer 2026.1005 的持久化结构精确对应。
- `collection.db` 的读写与 ppy/osu 的 `LegacyCollectionImporter` 完全一致：`int32 版本 + int32 数量 + (名称字符串[0x00 空 / 0x0B+7位长度+UTF-8] + int32 数量 + MD5 字符串列表)`，版本号沿用你 stable 现有文件的版本。
- 倍速改写规则：所有时间点 ÷ 倍速（含 AudioLeadIn、PreviewTime、红/绿线 offset、红线 beatLength、HitObjects 时间与滑条/长条/转盘结束时间、休息段、Bookmarks），滑条长度与 SV 不变（时长随 BPM 自动缩短）；音频用 ffmpeg `atempo` 变速（1.00 ~ 2.00 单次滤镜即可）。
