# v2.0.0 更新

## 全新的深色界面

- 整套 UI 重做：顶部皮肤横幅（程序化生成）、左侧分类导航、圆角卡片与按钮、自绘进度条、深色输入框 / 下拉框 / 列表。
- 窗口缩放自适应：列表列宽按可用宽度重新分配，右侧封面栏随窗口伸缩。
- 去掉了界面里的运行日志面板，改为底部细进度条 + 单行状态；完整日志仍写入 `logs\` 目录。

## 谱面下载页：搜索 / 封面 / 试听 / 排序

- **按关键词搜索**：输入歌曲名、艺术家或谱师即可搜索 sayobot 谱面库，结果按难度展开，显示「谱面 + 难度 + 模式 + 状态 + OD + HP + 星级」。
- **支持 osu! 官网搜索语法**（与 osu.ppy.sh/beatmapsets 一致）：
  - 数值条件：`stars>6`、`od<8`、`hp>=5`、`ar>9`、`cs<4`、`bpm>180`、`length<180`、`plays>10000`、`favourites>500`
  - 文本条件：`artist=xi`、`title=zenith`、`creator=Sotarks`
  - 分类条件：`mode=mania|taiko|catch|osu`、`status=ranked|approved|qualified|loved|pending|wip|graveyard`
  - 例：`blue zenith stars>6 mode=osu status=ranked`
- **状态标注**按官网分类：上架 Ranked / Approved / Qualified / Loved / 待定 Pending / 制作中 WIP / 坟场 Graveyard。
- **封面显示**：列表每行缩略图 + 右侧大图预览（谱师、状态、收藏数、游玩数）。
- **排序**：相关度 / 标题 / 艺术家 / 难度(★) / 收藏数 / 游玩数 / 更新时间 / 状态，支持升序、降序切换。
- **试听**：只有 `▶` / `⏸` 两个图标按钮，点击播放 / 暂停（osu! 官方预览音频，10 秒）。
- 单击结果选中、双击直接下载；仍可直接输入谱面集 ID 或链接下载。

## 段位单曲 ACC 计算

- 按需求去掉了「我的ACC」栏（以及依赖它的「检查」栏），现在是 曲名 / 通過時ACC / 個別ACC / 精确值 四列。

## 其它

- 谱面迁移页新增「lazer 谱面集 / lazer 收藏夹 / stable 缺失谱面集」三个统计卡片，检测后自动填充。
- 修复高 DPI（125% 缩放）下控件裁切、输入框高度异常等问题。
- 命令行新增 `--cli --sayobot-search "关键词 + 语法"`，方便脚本批量查询。

## 下载说明

- **`OsuLazerToStable-win-x64.zip`** — 自包含版，解压双击即用，无需安装 .NET（推荐）
- **`OsuLazerToStable-win-x64-framework.zip`** — 轻量版，需已安装 .NET 8 桌面运行时
