using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace BetterStepsRecorder.UI.Dialogs
{
    /// <summary>
    /// 统一的中文「设置」对话框：合并原「录制设置」与「高级设置」。
    /// 左侧分类导航 + 右侧内容面板，代码构建，样式统一。
    /// 所有改动在点击「确定」后一次性写入并保存。
    /// </summary>
    public class RecordingSettingsDialog : Form
    {
        // ── 常规 ────────────────────────────────────────────────────
        private readonly ComboBox _cboMinimize = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 200 };
        private readonly CheckBox _chkAllowRecordSelf = new() { Text = "允许录制本程序自身的窗口", AutoSize = true };

        // ── 截图区域（点击）─────────────────────────────────────────
        private readonly RadioButton _rdoAllScreens = new() { Text = "全屏（所有显示器）", AutoSize = true };
        private readonly RadioButton _rdoActiveScreen = new() { Text = "当前显示器", AutoSize = true };
        private readonly RadioButton _rdoActiveWindow = new() { Text = "当前活动窗口", AutoSize = true };
        private readonly RadioButton _rdoFollowMouse = new() { Text = "跟随鼠标（固定窗口大小）", AutoSize = true };
        private readonly RadioButton _rdoCropped = new() { Text = "围绕鼠标裁剪（正方形）", AutoSize = true };
        private readonly NumericUpDown _nudWidth = new() { Minimum = 160, Maximum = 3840, Increment = 20 };
        private readonly NumericUpDown _nudHeight = new() { Minimum = 120, Maximum = 2160, Increment = 20 };
        private readonly NumericUpDown _nudPadding = new() { Minimum = 50, Maximum = 500, Increment = 10 };

        // ── 拖拽截图 ────────────────────────────────────────────────
        private readonly RadioButton _rdoDragActiveWindow = new() { Text = "当前活动窗口", AutoSize = true };
        private readonly RadioButton _rdoDragActiveScreen = new() { Text = "当前显示器", AutoSize = true };
        private readonly RadioButton _rdoDragAllScreens = new() { Text = "全屏（所有显示器）", AutoSize = true };
        private readonly RadioButton _rdoDragCropped = new() { Text = "围绕鼠标裁剪", AutoSize = true };
        private readonly NumericUpDown _nudDragPadding = new() { Minimum = 50, Maximum = 500, Increment = 10 };
        private readonly ComboBox _cboDragFallback = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 200 };

        // ── 鼠标指针 ────────────────────────────────────────────────
        private readonly CheckBox _chkIndicator = new() { Text = "截图时叠加鼠标指针", AutoSize = true };
        private readonly ComboBox _cboStyle = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 140 };
        private readonly Panel _pnlIndicatorColor = new() { BorderStyle = BorderStyle.FixedSingle, Size = new Size(40, 22) };

        // ── 提示文字框 ──────────────────────────────────────────────
        private readonly CheckBox _chkLabel = new() { Text = "自动添加提示文字框", AutoSize = true };
        private readonly TextBox _txtLabel = new() { Width = 160 };
        private readonly NumericUpDown _nudFontSize = new() { Minimum = 10, Maximum = 48, Increment = 1 };
        private readonly NumericUpDown _nudOffsetX = new() { Minimum = -200, Maximum = 200, Increment = 2 };
        private readonly NumericUpDown _nudOffsetY = new() { Minimum = -200, Maximum = 200, Increment = 2 };
        private readonly Panel _pnlLabelBack = new() { BorderStyle = BorderStyle.FixedSingle, Size = new Size(40, 22) };
        private readonly Panel _pnlLabelBorder = new() { BorderStyle = BorderStyle.FixedSingle, Size = new Size(40, 22) };
        private readonly Panel _pnlLabelText = new() { BorderStyle = BorderStyle.FixedSingle, Size = new Size(40, 22) };

        // ── 导出·长图 ───────────────────────────────────────────────
        private readonly CheckBox _chkLiStepNumber = new() { Text = "显示步骤编号", AutoSize = true };
        private readonly CheckBox _chkLiStepText = new() { Text = "显示步骤说明", AutoSize = true };
        private readonly NumericUpDown _nudLiGap = new() { Minimum = 0, Maximum = 200, Increment = 2 };
        private readonly NumericUpDown _nudLiCaptionHeight = new() { Minimum = 0, Maximum = 120, Increment = 2 };
        private readonly NumericUpDown _nudLiUniformWidth = new() { Minimum = 0, Maximum = 4000, Increment = 20 };

        // ── 导出·图片序列 ───────────────────────────────────────────
        private readonly CheckBox _chkSeqStepText = new() { Text = "文件名包含步骤说明", AutoSize = true };
        private readonly NumericUpDown _nudSeqDigits = new() { Minimum = 1, Maximum = 6, Increment = 1 };
        private readonly TextBox _txtSeqSeparator = new() { Width = 40, MaxLength = 2 };

        // ── 导出·PDF ────────────────────────────────────────────────
        private readonly CheckBox _chkPdfStepNumber = new() { Text = "显示步骤编号", AutoSize = true };
        private readonly CheckBox _chkPdfStepText = new() { Text = "显示步骤说明", AutoSize = true };
        private readonly ComboBox _cboPdfPageSize = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 140 };
        private readonly NumericUpDown _nudPdfQuality = new() { Minimum = 40, Maximum = 100, Increment = 5 };

        // ── 导出·文档类（HTML/MD/RTF/ODT/Obsidian 共享结构）─────────
        private sealed class DetailControls
        {
            public CheckBox Summary = new() { Text = "摘要信息（步骤数 / 开始 / 结束 / 总耗时）", AutoSize = true };
            public CheckBox GeneratedDate = new() { Text = "生成日期", AutoSize = true };
            public CheckBox StepTimestamps = new() { Text = "步骤时间戳", AutoSize = true };
            public CheckBox Action = new() { Text = "操作", AutoSize = true };
            public CheckBox Application = new() { Text = "应用程序", AutoSize = true };
            public CheckBox Window = new() { Text = "窗口", AutoSize = true };
            public CheckBox Element = new() { Text = "元素", AutoSize = true };
            public CheckBox ElementType = new() { Text = "元素类型", AutoSize = true };
            public CheckBox MousePosition = new() { Text = "鼠标位置", AutoSize = true };
        }

        private enum DetailFormat { Html, Markdown, Rtf, Odt, Obsidian }

        private readonly Dictionary<DetailFormat, DetailControls> _detailControls = new();

        // ── 颜色暂存（仅确定时写回）─────────────────────────────────
        private int _indicatorColorArgb;
        private int _labelBackArgb;
        private int _labelBorderArgb;
        private int _labelTextArgb;

        // ── 导航与布局 ──────────────────────────────────────────────
        private readonly ListBox _navList = new() { Dock = DockStyle.Fill, IntegralHeight = false };
        private readonly Panel _contentHost = new() { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(12, 4, 4, 4) };
        private readonly List<Control> _pages = new();

        private readonly Button _btnOk = new() { Text = "确定", DialogResult = DialogResult.OK, Size = new Size(90, 30) };
        private readonly Button _btnCancel = new() { Text = "取消", DialogResult = DialogResult.Cancel, Size = new Size(90, 30) };
        private readonly Button _btnDefaults = new() { Text = "恢复默认", Size = new Size(90, 30) };
        private readonly Button _btnImport = new() { Text = "导入…", Size = new Size(90, 30) };
        private readonly Button _btnExport = new() { Text = "导出…", Size = new Size(90, 30) };

        private static readonly string[] Categories =
        {
            "常规", "截图区域", "拖拽截图", "鼠标指针", "提示文字框",
            "导出·长图", "导出·图片序列", "导出·PDF",
            "导出·HTML", "导出·Markdown", "导出·RTF", "导出·ODT", "导出·Obsidian"
        };

        public RecordingSettingsDialog()
        {
            Text = "设置";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            AutoScaleMode = AutoScaleMode.Font;
            Font = new Font("Microsoft YaHei UI", 9f);
            ClientSize = new Size(720, 560);

            AcceptButton = _btnOk;
            CancelButton = _btnCancel;

            BuildUi();
            LoadValues();
        }

        // ══════════════════════════════════════════════════════════
        // 界面构建
        // ══════════════════════════════════════════════════════════

        private void BuildUi()
        {
            _pages.Add(BuildGeneralPage());
            _pages.Add(BuildClickRegionPage());
            _pages.Add(BuildDragPage());
            _pages.Add(BuildIndicatorPage());
            _pages.Add(BuildLabelPage());
            _pages.Add(BuildLongImagePage());
            _pages.Add(BuildImageSequencePage());
            _pages.Add(BuildPdfPage());
            _pages.Add(BuildDetailPage(DetailFormat.Html, "选择要包含在 HTML 导出中的信息。步骤说明与截图始终包含。"));
            _pages.Add(BuildDetailPage(DetailFormat.Markdown, "选择要包含在 Markdown 导出中的信息。步骤说明与截图始终包含。"));
            _pages.Add(BuildDetailPage(DetailFormat.Rtf, "选择要包含在 RTF（Word）导出中的信息。步骤说明与截图始终包含。"));
            _pages.Add(BuildDetailPage(DetailFormat.Odt, "选择要包含在 ODT 导出中的信息。步骤说明与截图始终包含。"));
            _pages.Add(BuildDetailPage(DetailFormat.Obsidian, "选择要包含在 Obsidian 导出中的信息。步骤说明与截图始终包含。"));

            foreach (var page in _pages)
            {
                page.Visible = false;
                page.Dock = DockStyle.Top;
                _contentHost.Controls.Add(page);
            }

            _navList.Items.AddRange(Categories);
            _navList.SelectedIndexChanged += (_, _) => ShowPage(_navList.SelectedIndex);

            var navPanel = new Panel { Dock = DockStyle.Left, Width = 150, Padding = new Padding(12, 12, 4, 12) };
            navPanel.Controls.Add(_navList);

            var splitPanel = new Panel { Dock = DockStyle.Fill };
            splitPanel.Controls.Add(_contentHost);
            splitPanel.Controls.Add(navPanel);

            var leftButtons = new FlowLayoutPanel { Dock = DockStyle.Left, AutoSize = true };
            leftButtons.Controls.AddRange(new Control[] { _btnDefaults, _btnImport, _btnExport });
            var rightButtons = new FlowLayoutPanel { Dock = DockStyle.Right, AutoSize = true, FlowDirection = FlowDirection.RightToLeft };
            rightButtons.Controls.AddRange(new Control[] { _btnCancel, _btnOk });
            var buttonPanel = new Panel { Dock = DockStyle.Bottom, Height = 46, Padding = new Padding(12, 8, 12, 8) };
            buttonPanel.Controls.Add(rightButtons);
            buttonPanel.Controls.Add(leftButtons);

            _btnDefaults.Click += BtnDefaults_Click;
            _btnImport.Click += BtnImport_Click;
            _btnExport.Click += BtnExport_Click;

            Controls.Add(splitPanel);
            Controls.Add(buttonPanel);

            ShowPage(0);
            _navList.SelectedIndex = 0;
        }

        private void ShowPage(int index)
        {
            if (index < 0 || index >= _pages.Count) return;
            for (int i = 0; i < _pages.Count; i++)
                _pages[i].Visible = i == index;
        }

        // ── 小工具 ──────────────────────────────────────────────────

        private static Label Title(string text) => new()
        {
            Text = text,
            AutoSize = true,
            Font = new Font("Microsoft YaHei UI", 10f, FontStyle.Bold),
            Padding = new Padding(0, 10, 0, 4)
        };

        private static Label Label(string text) => new()
        {
            Text = text,
            AutoSize = true,
            Padding = new Padding(0, 6, 6, 0)
        };

        private static Label Hint(string text) => new()
        {
            Text = text,
            AutoSize = true,
            ForeColor = SystemColors.GrayText,
            Padding = new Padding(0, 8, 0, 8)
        };

        private static GroupBox Group(string text, params Control[] controls)
        {
            var box = new GroupBox { Text = text, Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(10) };
            var panel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, AutoSize = true, WrapContents = false };
            panel.Controls.AddRange(controls);
            box.Controls.Add(panel);
            return box;
        }

        private static FlowLayoutPanel Row(params Control[] controls)
        {
            var row = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(0, 3, 0, 3) };
            row.Controls.AddRange(controls);
            return row;
        }

        private Panel ColorPanel(Panel panel, int argb, Action<int> setter)
        {
            panel.BackColor = Color.FromArgb(argb);
            panel.Cursor = Cursors.Hand;
            panel.Click += (_, _) =>
            {
                using var dlg = new ColorDialog { Color = panel.BackColor, FullOpen = true };
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    int newArgb = Color.FromArgb(255, dlg.Color).ToArgb();
                    panel.BackColor = Color.FromArgb(newArgb);
                    setter(newArgb);
                }
            };
            return panel;
        }

        // ── 各分类页面 ──────────────────────────────────────────────

        private Control BuildGeneralPage()
        {
            var page = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, AutoSize = true, WrapContents = false };
            page.Controls.Add(Title("常规"));

            _cboMinimize.Items.AddRange(new object[] { "不最小化", "最小化到任务栏", "最小化到系统托盘" });
            page.Controls.Add(Group("开始录制时", Row(Label("主窗口"), _cboMinimize)));
            page.Controls.Add(Group("录制范围", _chkAllowRecordSelf));
            page.Controls.Add(Hint("提示：默认不会录制本程序自己的窗口，避免把主界面录进步骤里。"));
            return page;
        }

        private Control BuildClickRegionPage()
        {
            var page = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, AutoSize = true, WrapContents = false };
            page.Controls.Add(Title("截图区域"));
            page.Controls.Add(Group("每次点击时截取的范围",
                _rdoAllScreens, _rdoActiveScreen, _rdoActiveWindow, _rdoFollowMouse, _rdoCropped));
            page.Controls.Add(Row(Label("跟随窗口 宽"), _nudWidth, Label("高"), _nudHeight, Label("（像素，鼠标居中，贴边自动内收）")));
            page.Controls.Add(Row(Label("围绕鼠标裁剪 半径"), _nudPadding, Label("像素")));
            return page;
        }

        private Control BuildDragPage()
        {
            var page = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, AutoSize = true, WrapContents = false };
            page.Controls.Add(Title("拖拽截图"));
            page.Controls.Add(Group("每次拖拽时截取的范围",
                _rdoDragActiveWindow, _rdoDragActiveScreen, _rdoDragAllScreens, _rdoDragCropped));
            page.Controls.Add(Row(Label("围绕鼠标裁剪 半径"), _nudDragPadding, Label("像素")));
            _cboDragFallback.Items.AddRange(new object[] { "围绕鼠标裁剪", "当前显示器", "全屏（所有显示器）" });
            page.Controls.Add(Row(Label("跨窗口拖拽时改用"), _cboDragFallback));
            page.Controls.Add(Hint("提示：仅当截取范围为「当前活动窗口」，且拖拽起点和终点不在同一窗口时生效。"));
            return page;
        }

        private Control BuildIndicatorPage()
        {
            var page = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, AutoSize = true, WrapContents = false };
            page.Controls.Add(Title("鼠标指针"));
            page.Controls.Add(_chkIndicator);
            _cboStyle.Items.AddRange(new object[] { "标准光标", "圆圈", "指示箭头" });
            page.Controls.Add(Row(Label("样式"), _cboStyle, Label("颜色"),
                ColorPanel(_pnlIndicatorColor, _indicatorColorArgb, v => _indicatorColorArgb = v)));
            page.Controls.Add(Hint("提示：录制完成后仍可在草稿中单独移动或删除每一步的指针。"));
            return page;
        }

        private Control BuildLabelPage()
        {
            var page = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, AutoSize = true, WrapContents = false };
            page.Controls.Add(Title("点击提示文字框"));
            page.Controls.Add(_chkLabel);
            page.Controls.Add(Row(Label("默认文字"), _txtLabel, Label("字号"), _nudFontSize));
            page.Controls.Add(Row(Label("背景色"),
                ColorPanel(_pnlLabelBack, _labelBackArgb, v => _labelBackArgb = v),
                Label("边框色"),
                ColorPanel(_pnlLabelBorder, _labelBorderArgb, v => _labelBorderArgb = v),
                Label("文字色"),
                ColorPanel(_pnlLabelText, _labelTextArgb, v => _labelTextArgb = v)));
            page.Controls.Add(Row(Label("相对点击位置 右移"), _nudOffsetX, Label("下移"), _nudOffsetY, Label("像素")));
            page.Controls.Add(Hint("提示：双击画布中的文字框可随时修改每一步的文字内容。"));
            return page;
        }

        private Control BuildLongImagePage()
        {
            var page = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, AutoSize = true, WrapContents = false };
            page.Controls.Add(Title("导出·长图"));
            page.Controls.Add(Group("说明文字", _chkLiStepNumber, _chkLiStepText));
            page.Controls.Add(Row(Label("图片间距"), _nudLiGap, Label("像素")));
            page.Controls.Add(Row(Label("说明文字高度"), _nudLiCaptionHeight, Label("像素")));
            page.Controls.Add(Row(Label("统一缩放到宽度"), _nudLiUniformWidth, Label("像素（0 表示保持原尺寸）")));
            return page;
        }

        private Control BuildImageSequencePage()
        {
            var page = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, AutoSize = true, WrapContents = false };
            page.Controls.Add(Title("导出·图片序列"));
            page.Controls.Add(_chkSeqStepText);
            page.Controls.Add(Row(Label("编号位数"), _nudSeqDigits, Label("（3 表示 001、002……）")));
            page.Controls.Add(Row(Label("分隔符"), _txtSeqSeparator, Label("（编号与说明文字之间）")));
            return page;
        }

        private Control BuildPdfPage()
        {
            var page = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, AutoSize = true, WrapContents = false };
            page.Controls.Add(Title("导出·PDF"));
            page.Controls.Add(Group("页内标题", _chkPdfStepNumber, _chkPdfStepText));
            _cboPdfPageSize.Items.AddRange(new object[] { "A4", "Letter", "跟随图片" });
            page.Controls.Add(Row(Label("页面尺寸"), _cboPdfPageSize));
            page.Controls.Add(Row(Label("JPEG 质量"), _nudPdfQuality, Label("（40–100，越大越清晰、文件越大）")));
            return page;
        }

        private Control BuildDetailPage(DetailFormat format, string hint)
        {
            var c = new DetailControls();
            _detailControls[format] = c;

            var page = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, AutoSize = true, WrapContents = false };
            page.Controls.Add(Title(Categories[8 + (int)format]));
            page.Controls.Add(Group("页首", c.Summary, c.GeneratedDate));
            page.Controls.Add(Group("每个步骤", c.StepTimestamps));
            page.Controls.Add(Group("明细信息", c.Action, c.Application, c.Window, c.Element, c.ElementType, c.MousePosition));
            page.Controls.Add(Hint("提示：" + hint));
            return page;
        }

        // ══════════════════════════════════════════════════════════
        // 读取 / 写回
        // ══════════════════════════════════════════════════════════

        private static BSRSettings.IDetailExportSettings GetDetailSettings(DetailFormat fmt) => fmt switch
        {
            DetailFormat.Html => BSRSettings.Current.ExportOptions.Html,
            DetailFormat.Markdown => BSRSettings.Current.ExportOptions.Markdown,
            DetailFormat.Rtf => BSRSettings.Current.ExportOptions.Rtf,
            DetailFormat.Odt => BSRSettings.Current.ExportOptions.Odt,
            _ => BSRSettings.Current.ExportOptions.Obsidian
        };

        private void LoadValues()
        {
            var s = BSRSettings.Current;

            // 常规
            _cboMinimize.SelectedIndex = s.General.MinimizeOnStartRecording switch
            {
                MinimizeBehavior.DoNotMinimize => 0,
                MinimizeBehavior.MinimizeToSystemTray => 2,
                _ => 1
            };
            _chkAllowRecordSelf.Checked = s.General.AllowRecordSelf;

            // 截图区域
            switch (s.Screenshot.Click.Mode)
            {
                case ClickScreenshotMode.AllScreens: _rdoAllScreens.Checked = true; break;
                case ClickScreenshotMode.ActiveScreen: _rdoActiveScreen.Checked = true; break;
                case ClickScreenshotMode.FollowMouse: _rdoFollowMouse.Checked = true; break;
                case ClickScreenshotMode.Cropped: _rdoCropped.Checked = true; break;
                default: _rdoActiveWindow.Checked = true; break;
            }
            _nudWidth.Value = s.Screenshot.Click.FollowMouse.Width;
            _nudHeight.Value = s.Screenshot.Click.FollowMouse.Height;
            _nudPadding.Value = s.Screenshot.Click.Cropped.Padding;

            // 拖拽截图
            switch (s.Screenshot.Drag.Mode)
            {
                case DragScreenshotMode.ActiveScreen: _rdoDragActiveScreen.Checked = true; break;
                case DragScreenshotMode.AllScreens: _rdoDragAllScreens.Checked = true; break;
                case DragScreenshotMode.Cropped: _rdoDragCropped.Checked = true; break;
                default: _rdoDragActiveWindow.Checked = true; break;
            }
            _nudDragPadding.Value = s.Screenshot.Drag.Cropped.Padding;
            _cboDragFallback.SelectedIndex = s.Screenshot.Drag.Fallback.Mode switch
            {
                FallbackDragScreenshotMode.ActiveScreen => 1,
                FallbackDragScreenshotMode.AllScreens => 2,
                _ => 0
            };

            // 鼠标指针
            _chkIndicator.Checked = s.Indicator.Enabled;
            _cboStyle.SelectedIndex = s.Indicator.Style switch
            {
                ClickIndicatorStyle.Circle => 1,
                ClickIndicatorStyle.Arrow => 2,
                _ => 0
            };
            _indicatorColorArgb = s.Indicator.Color;
            _pnlIndicatorColor.BackColor = Color.FromArgb(_indicatorColorArgb);

            // 提示文字框
            _chkLabel.Checked = s.ClickLabel.Enabled;
            _txtLabel.Text = s.ClickLabel.DefaultText;
            _nudFontSize.Value = s.ClickLabel.FontSize;
            _nudOffsetX.Value = s.ClickLabel.OffsetX;
            _nudOffsetY.Value = s.ClickLabel.OffsetY;
            _labelBackArgb = s.ClickLabel.BackColor;
            _labelBorderArgb = s.ClickLabel.BorderColor;
            _labelTextArgb = s.ClickLabel.TextColor;
            _pnlLabelBack.BackColor = Color.FromArgb(_labelBackArgb);
            _pnlLabelBorder.BackColor = Color.FromArgb(_labelBorderArgb);
            _pnlLabelText.BackColor = Color.FromArgb(_labelTextArgb);

            // 长图
            _chkLiStepNumber.Checked = s.ExportOptions.LongImage.ShowStepNumber;
            _chkLiStepText.Checked = s.ExportOptions.LongImage.ShowStepText;
            _nudLiGap.Value = s.ExportOptions.LongImage.Gap;
            _nudLiCaptionHeight.Value = s.ExportOptions.LongImage.CaptionHeight;
            _nudLiUniformWidth.Value = s.ExportOptions.LongImage.UniformWidth;

            // 图片序列
            _chkSeqStepText.Checked = s.ExportOptions.ImageSequence.IncludeStepTextInFileName;
            _nudSeqDigits.Value = s.ExportOptions.ImageSequence.NumberDigits;
            _txtSeqSeparator.Text = s.ExportOptions.ImageSequence.Separator;

            // PDF
            _chkPdfStepNumber.Checked = s.ExportOptions.Pdf.ShowStepNumber;
            _chkPdfStepText.Checked = s.ExportOptions.Pdf.ShowStepText;
            _cboPdfPageSize.SelectedIndex = (s.ExportOptions.Pdf.PageSize ?? "A4").Trim().ToLowerInvariant() switch
            {
                "letter" => 1,
                "fitimage" => 2,
                _ => 0
            };
            _nudPdfQuality.Value = Math.Clamp(s.ExportOptions.Pdf.JpegQuality, 40, 100);

            // 文档类明细
            foreach (var kvp in _detailControls)
            {
                var d = GetDetailSettings(kvp.Key);
                var c = kvp.Value;
                c.Summary.Checked = d.ShowSummary;
                c.GeneratedDate.Checked = d.ShowGeneratedDate;
                c.StepTimestamps.Checked = d.ShowStepTimestamps;
                c.Action.Checked = d.ShowAction;
                c.Application.Checked = d.ShowApplication;
                c.Window.Checked = d.ShowWindow;
                c.Element.Checked = d.ShowElement;
                c.ElementType.Checked = d.ShowElementType;
                c.MousePosition.Checked = d.ShowMousePosition;
            }
        }

        private void SaveValues()
        {
            var s = BSRSettings.Current;

            // 常规
            s.General.MinimizeOnStartRecording = _cboMinimize.SelectedIndex switch
            {
                0 => MinimizeBehavior.DoNotMinimize,
                2 => MinimizeBehavior.MinimizeToSystemTray,
                _ => MinimizeBehavior.MinimizeToTaskbar
            };
            s.General.AllowRecordSelf = _chkAllowRecordSelf.Checked;

            // 截图区域
            if (_rdoAllScreens.Checked) s.Screenshot.Click.Mode = ClickScreenshotMode.AllScreens;
            else if (_rdoActiveScreen.Checked) s.Screenshot.Click.Mode = ClickScreenshotMode.ActiveScreen;
            else if (_rdoFollowMouse.Checked) s.Screenshot.Click.Mode = ClickScreenshotMode.FollowMouse;
            else if (_rdoCropped.Checked) s.Screenshot.Click.Mode = ClickScreenshotMode.Cropped;
            else s.Screenshot.Click.Mode = ClickScreenshotMode.ActiveWindow;
            s.Screenshot.Click.FollowMouse.Width = (int)_nudWidth.Value;
            s.Screenshot.Click.FollowMouse.Height = (int)_nudHeight.Value;
            s.Screenshot.Click.Cropped.Padding = (int)_nudPadding.Value;

            // 拖拽截图
            if (_rdoDragActiveScreen.Checked) s.Screenshot.Drag.Mode = DragScreenshotMode.ActiveScreen;
            else if (_rdoDragAllScreens.Checked) s.Screenshot.Drag.Mode = DragScreenshotMode.AllScreens;
            else if (_rdoDragCropped.Checked) s.Screenshot.Drag.Mode = DragScreenshotMode.Cropped;
            else s.Screenshot.Drag.Mode = DragScreenshotMode.ActiveWindow;
            s.Screenshot.Drag.Cropped.Padding = (int)_nudDragPadding.Value;
            s.Screenshot.Drag.Fallback.Mode = _cboDragFallback.SelectedIndex switch
            {
                1 => FallbackDragScreenshotMode.ActiveScreen,
                2 => FallbackDragScreenshotMode.AllScreens,
                _ => FallbackDragScreenshotMode.Cropped
            };

            // 鼠标指针
            s.Indicator.Enabled = _chkIndicator.Checked;
            s.Indicator.Style = _cboStyle.SelectedIndex switch
            {
                1 => ClickIndicatorStyle.Circle,
                2 => ClickIndicatorStyle.Arrow,
                _ => ClickIndicatorStyle.Cursor
            };
            s.Indicator.Color = _indicatorColorArgb;

            // 提示文字框
            s.ClickLabel.Enabled = _chkLabel.Checked;
            s.ClickLabel.DefaultText = string.IsNullOrWhiteSpace(_txtLabel.Text) ? "点击此" : _txtLabel.Text.Trim();
            s.ClickLabel.FontSize = (int)_nudFontSize.Value;
            s.ClickLabel.OffsetX = (int)_nudOffsetX.Value;
            s.ClickLabel.OffsetY = (int)_nudOffsetY.Value;
            s.ClickLabel.BackColor = _labelBackArgb;
            s.ClickLabel.BorderColor = _labelBorderArgb;
            s.ClickLabel.TextColor = _labelTextArgb;

            // 长图
            s.ExportOptions.LongImage.ShowStepNumber = _chkLiStepNumber.Checked;
            s.ExportOptions.LongImage.ShowStepText = _chkLiStepText.Checked;
            s.ExportOptions.LongImage.Gap = (int)_nudLiGap.Value;
            s.ExportOptions.LongImage.CaptionHeight = (int)_nudLiCaptionHeight.Value;
            s.ExportOptions.LongImage.UniformWidth = (int)_nudLiUniformWidth.Value;

            // 图片序列
            s.ExportOptions.ImageSequence.IncludeStepTextInFileName = _chkSeqStepText.Checked;
            s.ExportOptions.ImageSequence.NumberDigits = (int)_nudSeqDigits.Value;
            s.ExportOptions.ImageSequence.Separator = string.IsNullOrEmpty(_txtSeqSeparator.Text) ? "_" : _txtSeqSeparator.Text;

            // PDF
            s.ExportOptions.Pdf.ShowStepNumber = _chkPdfStepNumber.Checked;
            s.ExportOptions.Pdf.ShowStepText = _chkPdfStepText.Checked;
            s.ExportOptions.Pdf.PageSize = _cboPdfPageSize.SelectedIndex switch
            {
                1 => "Letter",
                2 => "FitImage",
                _ => "A4"
            };
            s.ExportOptions.Pdf.JpegQuality = (int)_nudPdfQuality.Value;

            // 文档类明细
            foreach (var kvp in _detailControls)
            {
                var d = GetDetailSettings(kvp.Key);
                var c = kvp.Value;
                d.ShowSummary = c.Summary.Checked;
                d.ShowGeneratedDate = c.GeneratedDate.Checked;
                d.ShowStepTimestamps = c.StepTimestamps.Checked;
                d.ShowAction = c.Action.Checked;
                d.ShowApplication = c.Application.Checked;
                d.ShowWindow = c.Window.Checked;
                d.ShowElement = c.Element.Checked;
                d.ShowElementType = c.ElementType.Checked;
                d.ShowMousePosition = c.MousePosition.Checked;
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (DialogResult == DialogResult.OK)
                SaveValues();

            base.OnFormClosing(e);
        }

        // ══════════════════════════════════════════════════════════
        // 恢复默认 / 导入 / 导出
        // ══════════════════════════════════════════════════════════

        private void BtnDefaults_Click(object? sender, EventArgs e)
        {
            var result = MessageBox.Show(this,
                "确定要将所有设置恢复为默认值吗？\n\n当前的自定义设置将全部丢失。",
                "恢复默认",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2);

            if (result != DialogResult.Yes) return;

            BSRSettings.Current.ResetToDefaults();
            BSRSettings.Current.Save();
            LoadValues();
            MessageBox.Show(this, "所有设置已恢复为默认值。", "恢复完成",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnExport_Click(object? sender, EventArgs e)
        {
            using var dlg = new SaveFileDialog
            {
                Filter = "JSON 文件 (*.json)|*.json|所有文件 (*.*)|*.*",
                DefaultExt = "json",
                FileName = "bsrsettings.json",
                Title = "导出设置"
            };

            if (dlg.ShowDialog(this) != DialogResult.OK) return;

            // 先把当前界面上的值写回，再导出，保证导出的是看到的内容
            SaveValues();
            BSRSettings.Current.Save();

            if (BSRSettings.Current.Export(dlg.FileName))
                MessageBox.Show(this, $"设置已导出到：\n{dlg.FileName}", "导出完成",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            else
                MessageBox.Show(this, "导出设置失败，请检查文件权限后重试。", "导出失败",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void BtnImport_Click(object? sender, EventArgs e)
        {
            var confirm = MessageBox.Show(this,
                "导入将覆盖当前所有设置，确定要继续吗？",
                "导入设置",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2);

            if (confirm != DialogResult.Yes) return;

            using var dlg = new OpenFileDialog
            {
                Filter = "JSON 文件 (*.json)|*.json|所有文件 (*.*)|*.*",
                DefaultExt = "json",
                Title = "导入设置"
            };

            if (dlg.ShowDialog(this) != DialogResult.OK) return;

            if (BSRSettings.Import(dlg.FileName))
            {
                LoadValues();
                MessageBox.Show(this, "设置导入成功。", "导入完成",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show(this, "导入设置失败，文件可能无效或已损坏。", "导入失败",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
