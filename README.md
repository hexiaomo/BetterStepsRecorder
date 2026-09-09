# 步骤记录器

按鼠标点击逐步截图、即时编辑、导出长图/图片序列/PDF 的 Windows 桌面工具。
不是录视频，只产出一组带标注的步骤图片。

> 本项目基于 [BetterStepsRecorder-Community](https://github.com/Better-World-Solutions/BetterStepsRecorder-Community) 二次开发。

## 主要特性

- **录制区域**：全屏 / 当前显示器 / 当前活动窗口 / 跟随鼠标（可设宽高）/ 围绕鼠标裁剪
- **三键支持**：左键、右键、中键都自动截图
- **自动标注**：可在截图上叠加鼠标指针（箭头/圆圈/标准光标三选一）、带边框的文字提示框（默认文字"点击此"）
- **非破坏式编辑**：所有标注都是对象化的，事后可拖动指针、改文字、删马赛克
- **常见标注**：选择 / 马赛克 / 高亮 / 文字框 / 箭头 / 矩形 / 椭圆 / 裁剪 / 撤销
- **5 种导出**：纵向合成长图、按步骤编号的图片序列、PDF（每步一页），以及 HTML / Markdown / Word(RTF) / ODT / Obsidian
- **中断续录**：录制中每步都自动落盘到草稿目录，意外关闭后可继续

## 运行要求

- Windows 10 / 11
- .NET 10 桌面运行时（发布为 framework-dependent 模式，未自带运行时）

## 构建

```cmd
build.cmd
```

构建完成后：`dist\StepRecorder.exe`

## 使用流程

1. 打开 `StepRecorder.exe`
2. 菜单 **录制设置** 中选择截图区域、是否叠加指针与文字框
3. 点击右上角 **开始录制**，窗口会自动最小化
4. 在屏幕上按鼠标（建议只点要记录的步骤），每次点击自动截图
5. 回到主窗口：可拖拽重排步骤、对单张截图做标注
6. 菜单 **导出** → 选择「合成一张长图 / 多张图片（按步骤编号）/ PDF 文档」

## 草稿与续录

- 录制中会实时自动保存到：`%LOCALAPPDATA%\StepRecorder\drafts\草稿_YYYYMMDD_HHMMSS.bsr`
- 菜单 **文件 → 打开草稿目录** 可直达该文件夹
- 意外关闭后：直接 **打开草稿目录**，用 **文件 → 打开** 加载最后一次的 .bsr，再点 **开始录制** 即在此基础上继续

## 文件格式

- **工程文件**（.bsr）：实际是 zip 包，内含 `events/event_{id}.json`，每条事件包含原始截图（Base64 PNG）+ 叠加层 + 元数据
- **导出格式**：PNG / JPEG 长图、PNG 序列、PDF、HTML、MD、RTF（Word 可打开）、ODT

## 目录结构

```
StepRecorder/
├── src/BetterStepsRecorder/      # 主项目
│   ├── Core/
│   │   ├── Annotations.cs         # 叠加层数据模型
│   │   ├── StepRenderer.cs        # 底图 + 叠加层 → Bitmap 合成
│   │   ├── Program.Recording.cs   # 全局鼠标钩子
│   │   ├── Program.ClickCapture.cs# 截图区域计算 / 中键 / 叠加层初始化
│   │   ├── Program.ImageHandling.cs
│   │   └── Program.FileOperations.cs
│   ├── Exporters/
│   │   ├── LongImageExporter.cs   # 合成一张长图
│   │   ├── ImageSequenceExporter.cs# 多张图片按步骤编号
│   │   ├── PdfExporter.cs         # PDF（每步一页）
│   │   └── MiniPdfWriter.cs       # 零依赖 PDF 写入器
│   ├── UI/
│   │   ├── MainForm/              # 主窗体分片
│   │   ├── Dialogs/RecordingSettingsDialog.cs  # 中文录制设置
│   │   └── Settings/              # 旧的高级设置
│   └── MainForm.Designer.cs
├── dist/StepRecorder.exe          # 构建产物
└── build.cmd
```

## 已知限制

- 中键截图只触发一次（不是组合键）
- 拖拽（Drag）截图沿用原项目实现：起点和终点画贝塞尔箭头
- 录制中无法运行全屏独占程序（如某些游戏）—— Windows 自身限制
