using System;
using System.Drawing;
using System.Windows.Forms;

namespace BetterStepsRecorder.UI.Dialogs
{
    /// <summary>
    /// 中文「录制设置」对话框：截图区域、鼠标指针、点击提示文字框。
    /// 完全用代码构建，不依赖设计器文件。
    /// </summary>
    public class RecordingSettingsDialog : Form
    {
        private readonly RadioButton _rdoAllScreens = new() { Text = "全屏（所有显示器）", AutoSize = true };
        private readonly RadioButton _rdoActiveScreen = new() { Text = "当前显示器", AutoSize = true };
        private readonly RadioButton _rdoActiveWindow = new() { Text = "当前活动窗口", AutoSize = true };
        private readonly RadioButton _rdoFollowMouse = new() { Text = "跟随鼠标（固定窗口大小）", AutoSize = true };
        private readonly RadioButton _rdoCropped = new() { Text = "围绕鼠标裁剪（正方形）", AutoSize = true };

        private readonly NumericUpDown _nudWidth = new() { Minimum = 160, Maximum = 3840, Increment = 20 };
        private readonly NumericUpDown _nudHeight = new() { Minimum = 120, Maximum = 2160, Increment = 20 };
        private readonly NumericUpDown _nudPadding = new() { Minimum = 50, Maximum = 500, Increment = 10 };

        private readonly CheckBox _chkIndicator = new() { Text = "截图时叠加鼠标指针（默认启用）", AutoSize = true };
        private readonly ComboBox _cboStyle = new() { DropDownStyle = ComboBoxStyle.DropDownList };
        private readonly Panel _pnlIndicatorColor = new() { BorderStyle = BorderStyle.FixedSingle, Size = new Size(40, 22) };

        private readonly CheckBox _chkLabel = new() { Text = "自动添加提示文字框（默认关闭）", AutoSize = true };
        private readonly TextBox _txtLabel = new();
        private readonly NumericUpDown _nudFontSize = new() { Minimum = 10, Maximum = 48, Increment = 1 };
        private readonly Panel _pnlLabelBorder = new() { BorderStyle = BorderStyle.FixedSingle, Size = new Size(40, 22) };

        private readonly Button _btnOk = new() { Text = "确定", DialogResult = DialogResult.OK, Size = new Size(90, 30) };
        private readonly Button _btnCancel = new() { Text = "取消", DialogResult = DialogResult.Cancel, Size = new Size(90, 30) };

        private int _indicatorColorArgb;
        private int _labelBorderArgb;

        public RecordingSettingsDialog()
        {
            Text = "录制设置";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            AutoScaleMode = AutoScaleMode.Font;
            Font = new Font("Microsoft YaHei UI", 9f);
            ClientSize = new Size(520, 520);

            AcceptButton = _btnOk;
            CancelButton = _btnCancel;

            BuildUi();
            LoadValues();
        }

        private void BuildUi()
        {
            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(16),
                ColumnCount = 1,
                AutoScroll = true
            };

            // ── 截图区域 ──
            root.Controls.Add(Title("截图区域"));

            var modeBox = new GroupBox { Text = "每次点击时截取的范围", Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(10) };
            var modePanel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, AutoSize = true };
            modePanel.Controls.AddRange(new Control[] { _rdoAllScreens, _rdoActiveScreen, _rdoActiveWindow, _rdoFollowMouse, _rdoCropped });
            modeBox.Controls.Add(modePanel);
            root.Controls.Add(modeBox);

            var sizePanel = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(0, 6, 0, 6) };
            sizePanel.Controls.Add(Label("跟随窗口 宽"));
            sizePanel.Controls.Add(_nudWidth);
            sizePanel.Controls.Add(Label("高"));
            sizePanel.Controls.Add(_nudHeight);
            sizePanel.Controls.Add(Label("（像素，鼠标居中，贴边自动内收）"));
            root.Controls.Add(sizePanel);

            var padPanel = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(0, 0, 0, 6) };
            padPanel.Controls.Add(Label("围绕鼠标裁剪 半径"));
            padPanel.Controls.Add(_nudPadding);
            padPanel.Controls.Add(Label("像素"));
            root.Controls.Add(padPanel);

            // ── 鼠标指针 ──
            root.Controls.Add(Title("鼠标指针"));
            root.Controls.Add(_chkIndicator);

            var indPanel = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(0, 6, 0, 6) };
            indPanel.Controls.Add(Label("样式"));
            _cboStyle.Items.AddRange(new object[] { "标准光标", "圆圈", "指示箭头" });
            indPanel.Controls.Add(_cboStyle);
            indPanel.Controls.Add(Label("颜色"));
            _pnlIndicatorColor.Click += (_, _) => PickColor(ref _indicatorColorArgb, _pnlIndicatorColor);
            indPanel.Controls.Add(_pnlIndicatorColor);
            root.Controls.Add(indPanel);

            // ── 提示文字框 ──
            root.Controls.Add(Title("点击提示文字框"));
            root.Controls.Add(_chkLabel);

            var lblPanel = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(0, 6, 0, 6) };
            lblPanel.Controls.Add(Label("默认文字"));
            _txtLabel.Width = 160;
            lblPanel.Controls.Add(_txtLabel);
            lblPanel.Controls.Add(Label("字号"));
            lblPanel.Controls.Add(_nudFontSize);
            lblPanel.Controls.Add(Label("边框色"));
            _pnlLabelBorder.Click += (_, _) => PickColor(ref _labelBorderArgb, _pnlLabelBorder);
            lblPanel.Controls.Add(_pnlLabelBorder);
            root.Controls.Add(lblPanel);

            root.Controls.Add(new Label
            {
                Text = "提示：所有设置在录制过程中可随时修改，只影响之后的截图；\n已生成的步骤可在草稿里单独拖动指针、改文字、加马赛克。",
                AutoSize = true,
                ForeColor = SystemColors.GrayText,
                Padding = new Padding(0, 8, 0, 8)
            });

            var buttonPanel = new FlowLayoutPanel { Dock = DockStyle.Bottom, FlowDirection = FlowDirection.RightToLeft, AutoSize = true };
            buttonPanel.Controls.Add(_btnCancel);
            buttonPanel.Controls.Add(_btnOk);

            Controls.Add(root);
            Controls.Add(buttonPanel);
        }

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

        private void PickColor(ref int argb, Panel preview)
        {
            using var dlg = new ColorDialog { Color = Color.FromArgb(argb), FullOpen = true };
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                argb = Color.FromArgb(255, dlg.Color).ToArgb();
                preview.BackColor = Color.FromArgb(argb);
            }
        }

        private void LoadValues()
        {
            var s = BSRSettings.Current;

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

            _chkIndicator.Checked = s.Indicator.Enabled;
            _cboStyle.SelectedIndex = s.Indicator.Style switch
            {
                ClickIndicatorStyle.Circle => 1,
                ClickIndicatorStyle.Arrow => 2,
                _ => 0
            };
            _indicatorColorArgb = s.Indicator.Color;
            _pnlIndicatorColor.BackColor = Color.FromArgb(_indicatorColorArgb);

            _chkLabel.Checked = s.ClickLabel.Enabled;
            _txtLabel.Text = s.ClickLabel.DefaultText;
            _nudFontSize.Value = s.ClickLabel.FontSize;
            _labelBorderArgb = s.ClickLabel.BorderColor;
            _pnlLabelBorder.BackColor = Color.FromArgb(_labelBorderArgb);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (DialogResult != DialogResult.OK) return;

            var s = BSRSettings.Current;

            if (_rdoAllScreens.Checked) s.Screenshot.Click.Mode = ClickScreenshotMode.AllScreens;
            else if (_rdoActiveScreen.Checked) s.Screenshot.Click.Mode = ClickScreenshotMode.ActiveScreen;
            else if (_rdoFollowMouse.Checked) s.Screenshot.Click.Mode = ClickScreenshotMode.FollowMouse;
            else if (_rdoCropped.Checked) s.Screenshot.Click.Mode = ClickScreenshotMode.Cropped;
            else s.Screenshot.Click.Mode = ClickScreenshotMode.ActiveWindow;

            s.Screenshot.Click.FollowMouse.Width = (int)_nudWidth.Value;
            s.Screenshot.Click.FollowMouse.Height = (int)_nudHeight.Value;
            s.Screenshot.Click.Cropped.Padding = (int)_nudPadding.Value;

            s.Indicator.Enabled = _chkIndicator.Checked;
            s.Indicator.Style = _cboStyle.SelectedIndex switch
            {
                1 => ClickIndicatorStyle.Circle,
                2 => ClickIndicatorStyle.Arrow,
                _ => ClickIndicatorStyle.Cursor
            };
            s.Indicator.Color = _indicatorColorArgb;

            s.ClickLabel.Enabled = _chkLabel.Checked;
            s.ClickLabel.DefaultText = string.IsNullOrWhiteSpace(_txtLabel.Text) ? "点击此" : _txtLabel.Text.Trim();
            s.ClickLabel.FontSize = (int)_nudFontSize.Value;
            s.ClickLabel.BorderColor = _labelBorderArgb;

            base.OnFormClosing(e);
        }
    }
}
