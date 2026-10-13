# osu!lazer → stable 迁移工具

把 osu!lazer 中的**谱面**和**收藏夹**自动迁移到 osu!stable 的 Windows 图形化工具。

## 下载

👉 **最新版下载：https://github.com/fallencatww/OsuLazerToStable/releases/latest**

在 Assets 里下载 `OsuLazerToStable-win-x64.zip`（自包含版，解压后双击即用，无需安装 .NET）。

- 只读访问 lazer 的 `client.realm` 与 `files` 文件仓库，**绝不修改 lazer 的任何数据**（lazer 开着也能用）。
- 谱面按谱面集重建到 stable 的 `Songs` 目录，文件夹命名与 stable 惯例一致：`{谱面集ID} {艺术家} - {标题}`。
- 收藏夹写入 stable 的 `collection.db`（与 lazer 相同的「Favourites」收藏夹也会一并迁移）；写入前自动备份原文件。
- 自动跳过 stable 中已存在的谱面集；支持增量重跑。
- 中文界面，带进度条、完整日志（界面 + 本地日志文件）。

## 使用方法

1. 关闭 **osu!stable**（迁移谱面时不必关，但**写 collection.db 前必须关闭**，否则会被游戏退出时覆盖；工具会自动检测并提示）。
2. 运行 `OsuLazerToStable.exe`。
3. 确认两个路径（一般会自动检测）：
   - lazer 数据目录：`%APPDATA%\osu`（内含 `client.realm` 和 `files`）
   - stable 安装目录：`%LOCALAPPDATA%\osu!`（内含 `osu!.exe` 和 `Songs`）
4. 点「刷新检测」查看两边数据，勾选要迁移的内容，点「开始迁移」。
5. 完成后重启 osu!stable 即可看到谱面和收藏夹。

### 选项说明

| 选项 | 说明 |
| --- | --- |
| 迁移谱面 | 把 lazer 的谱面集复制进 stable 的 Songs 目录（已在 stable 中的自动跳过） |
| 迁移收藏夹 | 把 lazer 的收藏夹（含 Favourites）写入 stable 的 collection.db |
| 同名收藏夹合并 | 勾选时，与 stable 已有收藏夹同名的做并集合并；不勾选时以 lazer 内容覆盖同名项 |
| 演练模式 | 只统计和打印将要做的事，不写任何文件 |

### 命令行模式（进阶）

```
OsuLazerToStable.exe --cli [--lazer 目录] [--stable 目录] [--maps|--no-maps]
                  [--collections|--no-collections] [--dry-run] [--merge|--no-merge]
```

## 注意事项

- **收藏夹引用的是谱面的 MD5**。lazer 收藏夹里如果引用了 stable 中不存在的谱面（例如谱面还没迁移、或 lazer 的 .osu 被编辑过），该引用会被丢弃并在日志中计数。先把谱面迁移完再迁移收藏夹，命中率最高。
- lazer 的「Favourites」在 stable 中会成为一个同名**收藏夹**（stable 的心形「收藏」存在 osu!.db 里，本工具不修改数据库，避免破坏成绩数据）。
- 若 lazer 未来更新导致 realm 结构变化，工具会自动适配 schema 版本；若模型字段发生大改导致无法打开，会给出明确报错。
- 迁移大仓库（几万文件、几十 GB）需要一些时间，请耐心等待；随时可点「取消」，已完成的文件会保留。

## 构建

需要 .NET 8 SDK：

```
dotnet publish -c Release -r win-x64
```

生成自包含发布（无需安装运行时），或省略 `-r` 生成依赖 .NET 8 桌面运行时的版本。

## 技术要点

- Realm 数据通过 `Realm` NuGet（20.1.0，与 lazer 所用版本一致）以只读方式打开，schema 类仅声明迁移所需的 4 个模型（`BeatmapCollection`、`BeatmapSet`、`RealmNamedFileUsage`、`File`），与 lazer 2026.1005 的持久化结构精确对应。
- `collection.db` 的读写与 ppy/osu 的 `LegacyCollectionImporter` 完全一致：`int32 版本 + int32 数量 + (名称字符串[0x00 空 / 0x0B+7位长度+UTF-8] + int32 数量 + MD5 字符串列表)`，版本号沿用你 stable 现有文件的版本。
