using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Text.Json;
using System.Windows.Forms;

namespace BetterStepsRecorder
{
    /// <summary>
    /// 草稿编辑器：所有标注都以「对象」形式保存在 StepOverlay 上（非破坏式），
    /// 因此可以随时拖动鼠标指针、改提示文字、删马赛克，底图永远是原始截图。
    /// </summary>
    public partial class MainForm
    {
        private enum ImageTool { Select, Mosaic, Highlight, Arrow, TextBox, Rectangle, Ellipse, Crop }

        private enum HitTarget { None, Indicator, Label, Annotation }

        private ImageTool _activeTool = ImageTool.Select;

        private bool _drawing = false;
        private Point _startImage;
        private Point _currentImage;

        private HitTarget _selTarget = HitTarget.None;
        private int _selIndex = -1;
        private bool _moving = false;
        private Point _moveStartImage;
        private int _moveOriginX, _moveOriginY;
        private int _moveOriginStartX, _moveOriginStartY, _moveOriginEndX, _moveOriginEndY;

        // 撤销栈：保存叠加层的 JSON 快照
        private readonly Dictionary<Guid, Stack<string>> _undoStacks = new();

        public static Color HighlightColor { get; set; } = Color.FromArgb(160, 255, 255, 0);
        public static Color ArrowColor { get; set; } = Color.FromArgb(255, 255, 0, 255);
        public static Color BoxColor { get; set; } = Color.FromArgb(255, 220, 53, 69);
        public static int MosaicBlock { get; set; } = 10;

        // ── 工具栏 ──────────────────────────────────────────────────

        private void selectToolStripButton_Click(object sender, EventArgs e) => ActivateTool(ImageTool.Select);

        private void mosaicToolStripButton_Click(object sender, EventArgs e)
            => ActivateTool(mosaicToolStripButton.Checked ? ImageTool.Mosaic : ImageTool.Select);

        private void highlightToolStripButton_Click(object sender, EventArgs e)
            => ActivateTool(highlightToolStripButton.Checked ? ImageTool.Highlight : ImageTool.Select);

        private void textLabelToolStripButton_Click(object sender, EventArgs e)
            => ActivateTool(textLabelToolStripButton.Checked ? ImageTool.TextBox : ImageTool.Select);

        private void arrowToolStripButton_Click(object sender, EventArgs e)
            => ActivateTool(arrowToolStripButton.Checked ? ImageTool.Arrow : ImageTool.Select);

        private void rectangleToolStripButton_Click(object sender, EventArgs e)
            => ActivateTool(rectangleToolStripButton.Checked ? ImageTool.Rectangle : ImageTool.Select);

        private void ellipseToolStripButton_Click(object sender, EventArgs e)
            => ActivateTool(ellipseToolStripButton.Checked ? ImageTool.Ellipse : ImageTool.Select);

        private void cropToolStripButton_Click(object sender, EventArgs e)
            => ActivateTool(cropToolStripButton.Checked ? ImageTool.Crop : ImageTool.Select);

        private void highlightColourToolStripButton_Click(object sender, EventArgs e)
        {
            using var dlg = new ColorDialog { Color = Color.FromArgb(255, HighlightColor), FullOpen = true };
            if (dlg.ShowDialog(this) == DialogResult.OK)
                HighlightColor = Color.FromArgb(160, dlg.Color.R, dlg.Color.G, dlg.Color.B);
        }

        private void arrowColourToolStripButton_Click(object sender, EventArgs e)
        {
            using var dlg = new ColorDialog { Color = ArrowColor, FullOpen = true };
            if (dlg.ShowDialog(this) == DialogResult.OK)
                ArrowColor = dlg.Color;
        }

        private void ActivateTool(ImageTool tool)
        {
            _activeTool = tool;
            _drawing = false;
            ClearSelection();

            selectToolStripButton.Checked = tool == ImageTool.Select;
            mosaicToolStripButton.Checked = tool == ImageTool.Mosaic;
            highlightToolStripButton.Checked = tool == ImageTool.Highlight;
            textLabelToolStripButton.Checked = tool == ImageTool.TextBox;
            arrowToolStripButton.Checked = tool == ImageTool.Arrow;
            rectangleToolStripButton.Checked = tool == ImageTool.Rectangle;
            ellipseToolStripButton.Checked = tool == ImageTool.Ellipse;
            cropToolStripButton.Checked = tool == ImageTool.Crop;

            pictureBox1.Cursor = tool == ImageTool.Select ? Cursors.Default : Cursors.Cross;
            pictureBox1.Invalidate();
        }

        /// <summary>切换步骤时重置工具状态。</summary>
        private void ResetImageTools()
        {
            _activeTool = ImageTool.Select;
            _drawing = false;
            _moving = false;
            _startImage = Point.Empty;
            _currentImage = Point.Empty;
            ClearSelection();

            selectToolStripButton.Checked = true;
            mosaicToolStripButton.Checked = false;
            highlightToolStripButton.Checked = false;
            textLabelToolStripButton.Checked = false;
            arrowToolStripButton.Checked = false;
            rectangleToolStripButton.Checked = false;
            ellipseToolStripButton.Checked = false;
            cropToolStripButton.Checked = false;

            pictureBox1.Cursor = Cursors.Default;
            undoToolStripButton.Enabled = CurrentUndoStackCount() > 0;
        }

        // ── 撤销 ────────────────────────────────────────────────────

        private int CurrentUndoStackCount()
        {
            if (Listbox_Events.SelectedItem is RecordEvent evt &&
                _undoStacks.TryGetValue(evt.ID, out var stack))
                return stack.Count;
            return 0;
        }

        private void PushUndo(RecordEvent evt)
        {
            if (!_undoStacks.TryGetValue(evt.ID, out var stack))
            {
                stack = new Stack<string>();
                _undoStacks[evt.ID] = stack;
            }
            stack.Push(JsonSerializer.Serialize(evt.Overlay));
            undoToolStripButton.Enabled = true;
        }

        private void undoToolStripButton_Click(object sender, EventArgs e)
        {
            if (!(Listbox_Events.SelectedItem is RecordEvent evt)) return;
            if (!_undoStacks.TryGetValue(evt.ID, out var stack) || stack.Count == 0)
            {
                undoToolStripButton.Enabled = false;
                return;
            }

            try
            {
                evt.Overlay = JsonSerializer.Deserialize<StepOverlay>(stack.Pop()) ?? new StepOverlay();
            }
            catch
            {
                evt.Overlay = new StepOverlay();
            }

            undoToolStripButton.Enabled = stack.Count > 0;
            RefreshCanvas();
            MarkDirty();
        }

        // ── 画布刷新 ────────────────────────────────────────────────

        private void RefreshCanvas()
        {
            if (!(Listbox_Events.SelectedItem is RecordEvent evt)) return;
            Bitmap? bmp = StepRenderer.Render(evt);
            var old = pictureBox1.Image;
            pictureBox1.Image = bmp;
            old?.Dispose();
            pictureBox1.Invalidate();
        }

        private void MarkDirty()
        {
            activityTimer.Stop();
            activityTimer.Start();
        }

        private void ClearSelection()
        {
            _selTarget = HitTarget.None;
            _selIndex = -1;
        }

        // ── 命中测试 ────────────────────────────────────────────────

        private static Rectangle IndicatorHitRect(StepOverlay ov) =>
            new Rectangle(ov.IndicatorX - 6, ov.IndicatorY - 6, 40, 46);

        private static Rectangle LabelHitRect(StepOverlay ov) =>
            new Rectangle(ov.LabelX, ov.LabelY, ov.LabelWidth, ov.LabelHeight);

        private static bool HitAnnotation(Annotation a, Point p)
        {
            if (a.Kind == AnnotationKind.Arrow)
            {
                const int tol = 8;
                var r = new Rectangle(
                    Math.Min(a.StartX, a.EndX) - tol, Math.Min(a.StartY, a.EndY) - tol,
                    Math.Abs(a.EndX - a.StartX) + tol * 2, Math.Abs(a.EndY - a.StartY) + tol * 2);
                return r.Contains(p);
            }
            if (a.Bounds.Contains(p)) return true;

            // 细长/极小的对象放宽命中范围，方便点选
            var padded = new Rectangle(a.X - 6, a.Y - 6, a.Width + 12, a.Height + 12);
            return (a.Width < 12 || a.Height < 12) && padded.Contains(p);
        }

        private HitTarget HitTest(RecordEvent evt, Point imagePt, out int index)
        {
            index = -1;
            var ov = evt.Overlay;
            if (ov == null) return HitTarget.None;

            for (int i = ov.Annotations.Count - 1; i >= 0; i--)
            {
                if (HitAnnotation(ov.Annotations[i], imagePt))
                {
                    index = i;
                    return HitTarget.Annotation;
                }
            }

            if (ov.LabelEnabled && LabelHitRect(ov).Contains(imagePt))
                return HitTarget.Label;

            if (ov.IndicatorEnabled && IndicatorHitRect(ov).Contains(imagePt))
                return HitTarget.Indicator;

            return HitTarget.None;
        }

        // ── 鼠标交互 ────────────────────────────────────────────────

        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            if (!(Listbox_Events.SelectedItem is RecordEvent evt)) return;
            if (pictureBox1.Image == null) return;
            if (evt.Overlay == null) evt.Overlay = new StepOverlay();

            Point pt = ControlPointToImagePoint(e.Location);

            // PictureBox 默认不派发双击事件，这里手动识别（改文字/改提示框用）
            long now = Environment.TickCount64;
            if (now - _lastClickTime <= SystemInformation.DoubleClickTime &&
                Math.Abs(e.Location.X - _lastClickPoint.X) <= 4 &&
                Math.Abs(e.Location.Y - _lastClickPoint.Y) <= 4)
            {
                _lastClickTime = 0;
                HandleDoubleClick(evt, e.Location);
                return;
            }
            _lastClickTime = now;
            _lastClickPoint = e.Location;

            _startImage = pt;
            _currentImage = pt;

            if (_activeTool == ImageTool.Select)
            {
                _selTarget = HitTest(evt, pt, out _selIndex);

                if (_selTarget != HitTarget.None)
                {
                    _moving = true;
                    _moveStartImage = pt;

                    if (_selTarget == HitTarget.Indicator)
                    {
                        _moveOriginX = evt.Overlay.IndicatorX;
                        _moveOriginY = evt.Overlay.IndicatorY;
                    }
                    else if (_selTarget == HitTarget.Label)
                    {
                        _moveOriginX = evt.Overlay.LabelX;
                        _moveOriginY = evt.Overlay.LabelY;
                    }
                    else if (_selIndex >= 0)
                    {
                        var a = evt.Overlay.Annotations[_selIndex];
                        _moveOriginX = a.X; _moveOriginY = a.Y;
                        _moveOriginStartX = a.StartX; _moveOriginStartY = a.StartY;
                        _moveOriginEndX = a.EndX; _moveOriginEndY = a.EndY;
                        PushUndo(evt);
                    }
                }
                pictureBox1.Invalidate();
                return;
            }

            _drawing = true;
        }

        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            if (!(Listbox_Events.SelectedItem is RecordEvent evt)) return;
            if (pictureBox1.Image == null) return;

            Point pt = ControlPointToImagePoint(e.Location);
            _currentImage = pt;

            if (_moving && _selTarget != HitTarget.None)
            {
                int dx = pt.X - _moveStartImage.X;
                int dy = pt.Y - _moveStartImage.Y;
                var ov = evt.Overlay;

                if (_selTarget == HitTarget.Indicator)
                {
                    ov.IndicatorX = _moveOriginX + dx;
                    ov.IndicatorY = _moveOriginY + dy;
                }
                else if (_selTarget == HitTarget.Label)
                {
                    ov.LabelX = _moveOriginX + dx;
                    ov.LabelY = _moveOriginY + dy;
                }
                else if (_selIndex >= 0 && _selIndex < ov.Annotations.Count)
                {
                    var a = ov.Annotations[_selIndex];
                    a.X = _moveOriginX + dx;
                    a.Y = _moveOriginY + dy;
                    a.StartX = _moveOriginStartX + dx;
                    a.StartY = _moveOriginStartY + dy;
                    a.EndX = _moveOriginEndX + dx;
                    a.EndY = _moveOriginEndY + dy;
                }

                pictureBox1.Invalidate();
                return;
            }

            if (_drawing) pictureBox1.Invalidate();
        }

        private void pictureBox1_MouseUp(object sender, MouseEventArgs e)
        {
            if (!(Listbox_Events.SelectedItem is RecordEvent evt)) return;
            if (pictureBox1.Image == null) return;

            if (_moving)
            {
                _moving = false;
                RefreshCanvas();
                MarkDirty();
                return;
            }

            if (!_drawing) return;
            _drawing = false;

            Point pt = ControlPointToImagePoint(e.Location);
            _currentImage = pt;

            var ov = evt.Overlay;
            Rectangle rect = RectFromPoints(_startImage, pt);

            switch (_activeTool)
            {
                case ImageTool.Mosaic:
                    if (rect.Width >= 4 && rect.Height >= 4)
                    {
                        PushUndo(evt);
                        ov.Annotations.Add(new Annotation
                        {
                            Kind = AnnotationKind.Mosaic,
                            X = rect.X, Y = rect.Y, Width = rect.Width, Height = rect.Height,
                            MosaicBlock = MosaicBlock
                        });
                    }
                    break;

                case ImageTool.Highlight:
                    if (rect.Width >= 4 && rect.Height >= 4)
                    {
                        PushUndo(evt);
                        ov.Annotations.Add(new Annotation
                        {
                            Kind = AnnotationKind.Highlight,
                            X = rect.X, Y = rect.Y, Width = rect.Width, Height = rect.Height,
                            ColorArgb = HighlightColor.ToArgb()
                        });
                    }
                    break;

                case ImageTool.TextBox:
                {
                    int w = Math.Max(60, rect.Width), h = Math.Max(28, rect.Height);
                    PushUndo(evt);
                    var a = new Annotation
                    {
                        Kind = AnnotationKind.TextBox,
                        X = rect.X, Y = rect.Y, Width = w, Height = h,
                        ColorArgb = BoxColor.ToArgb(),
                        FontSize = 16,
                        Bordered = true
                    };
                    ov.Annotations.Add(a);

                    string? input = ShowInputDialog("文字内容", "请输入文字：", "点击此");
                    if (string.IsNullOrWhiteSpace(input))
                    {
                        ov.Annotations.Remove(a);
                        if (_undoStacks.TryGetValue(evt.ID, out var st) && st.Count > 0) st.Pop();
                    }
                    else
                    {
                        a.Text = input!;
                    }
                    break;
                }

                case ImageTool.Arrow:
                    if (Math.Abs(pt.X - _startImage.X) >= 4 || Math.Abs(pt.Y - _startImage.Y) >= 4)
                    {
                        PushUndo(evt);
                        ov.Annotations.Add(new Annotation
                        {
                            Kind = AnnotationKind.Arrow,
                            X = rect.X, Y = rect.Y, Width = rect.Width, Height = rect.Height,
                            StartX = _startImage.X, StartY = _startImage.Y,
                            EndX = pt.X, EndY = pt.Y,
                            ColorArgb = ArrowColor.ToArgb()
                        });
                    }
                    break;

                case ImageTool.Rectangle:
                    if (rect.Width >= 4 && rect.Height >= 4)
                    {
                        PushUndo(evt);
                        ov.Annotations.Add(new Annotation
                        {
                            Kind = AnnotationKind.Rectangle,
                            X = rect.X, Y = rect.Y, Width = rect.Width, Height = rect.Height,
                            ColorArgb = BoxColor.ToArgb()
                        });
                    }
                    break;

                case ImageTool.Ellipse:
                    if (rect.Width >= 4 && rect.Height >= 4)
                    {
                        PushUndo(evt);
                        ov.Annotations.Add(new Annotation
                        {
                            Kind = AnnotationKind.Ellipse,
                            X = rect.X, Y = rect.Y, Width = rect.Width, Height = rect.Height,
                            ColorArgb = BoxColor.ToArgb()
                        });
                    }
                    break;

                case ImageTool.Crop:
                    if (rect.Width >= 16 && rect.Height >= 16)
                    {
                        PushUndo(evt);
                        ov.ApplyCrop(rect);
                        ActivateTool(ImageTool.Select);
                    }
                    break;
            }

            RefreshCanvas();
            MarkDirty();
        }

        private long _lastClickTime;
        private Point _lastClickPoint;

        private void HandleDoubleClick(RecordEvent evt, Point location)
        {
            Point pt = ControlPointToImagePoint(location);
            HitTarget target = HitTest(evt, pt, out int index);

            if (target == HitTarget.Label)
            {
                var ov = evt.Overlay;
                string? input = ShowInputDialog("提示文字", "请输入提示文字：", ov.LabelText);
                if (input == null) return;
                PushUndo(evt);
                ov.LabelText = input;
                ov.LabelEnabled = true;
                var size = StepRenderer.MeasureLabelBox(ov);
                ov.LabelWidth = size.Width;
                ov.LabelHeight = size.Height;
                RefreshCanvas();
                MarkDirty();
            }
            else if (target == HitTarget.Annotation && index >= 0)
            {
                var a = evt.Overlay.Annotations[index];
                if (a.Kind is AnnotationKind.TextBox or AnnotationKind.Text)
                {
                    string? input = ShowInputDialog("文字内容", "请输入文字：", a.Text);
                    if (input == null) return;
                    PushUndo(evt);
                    a.Text = input;
                    RefreshCanvas();
                    MarkDirty();
                }
            }
        }

        /// <summary>删除当前选中的指针 / 文字框 / 标注。</summary>
        private void DeleteSelectedAnnotation()
        {
            if (_selTarget == HitTarget.None) return;
            if (!(Listbox_Events.SelectedItem is RecordEvent evt)) return;

            PushUndo(evt);
            switch (_selTarget)
            {
                case HitTarget.Indicator: evt.Overlay.IndicatorEnabled = false; break;
                case HitTarget.Label: evt.Overlay.LabelEnabled = false; break;
                case HitTarget.Annotation:
                    if (_selIndex >= 0 && _selIndex < evt.Overlay.Annotations.Count)
                        evt.Overlay.Annotations.RemoveAt(_selIndex);
                    break;
            }
            ClearSelection();
            RefreshCanvas();
            MarkDirty();
        }

        private string? ShowInputDialog(string title, string prompt, string defaultValue)
        {
            using var dlg = new Form
            {
                Text = title,
                Size = new Size(420, 180),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                MaximizeBox = false, MinimizeBox = false
            };

            var lbl = new Label { Text = prompt, Dock = DockStyle.Top, Height = 40, Padding = new Padding(10, 12, 10, 0) };
            var tb = new TextBox { Dock = DockStyle.Top, Margin = new Padding(10), Text = defaultValue, Font = new Font("Microsoft YaHei UI", 11) };
            var ok = new Button { Text = "确定", DialogResult = DialogResult.OK, Dock = DockStyle.Bottom };
            var cancel = new Button { Text = "取消", DialogResult = DialogResult.Cancel, Dock = DockStyle.Bottom };

            dlg.Controls.AddRange(new Control[] { ok, cancel, tb, lbl });
            dlg.AcceptButton = ok;
            dlg.CancelButton = cancel;

            return dlg.ShowDialog(this) == DialogResult.OK ? tb.Text : null;
        }

        // ── 叠加绘制（绘制预览、选中框）────────────────────────────

        private void pictureBox1_Paint(object sender, PaintEventArgs e)
        {
            // 防止任何意外的 GDI+ 异常把应用带进 JIT 调试对话框
            try
            {
                PaintOverlay(e);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"pictureBox1_Paint: {ex.Message}");
            }
        }

        private void PaintOverlay(PaintEventArgs e)
        {
            if (!(Listbox_Events.SelectedItem is RecordEvent evt)) return;
            if (pictureBox1.Image == null) return;

            Rectangle view = GetImageRectInZoomMode(pictureBox1);
            if (view.Width == 0 || view.Height == 0) return;

            float sx = view.Width / (float)pictureBox1.Image.Width;
            float sy = view.Height / (float)pictureBox1.Image.Height;
            if (sx <= 0 || float.IsNaN(sx) || float.IsInfinity(sx)) return;

            // 注意：e.Graphics 是 WinForms 双缓冲共享的画布，绝不能 Dispose，
            // 否则事件返回后 BufferedGraphics.Render() 会因 GetHdc 失败而抛
            // “参数无效”，整张图无法上屏（PictureBox 只剩灰色背景）。
            var g = e.Graphics;
            var state = g.Save();
            g.TranslateTransform(view.X, view.Y);
            g.ScaleTransform(sx, sy);
            g.SmoothingMode = SmoothingMode.AntiAlias;

            if (_drawing && _activeTool != ImageTool.Select)
            {
                Rectangle rect = RectFromPoints(_startImage, _currentImage);
                if (rect.Width > 0 && rect.Height > 0)
                {
                    switch (_activeTool)
                    {
                        case ImageTool.Mosaic:
                            using (var b = new SolidBrush(Color.FromArgb(80, 30, 30, 30)))
                                g.FillRectangle(b, rect);
                            break;
                        case ImageTool.Highlight:
                            using (var b2 = new SolidBrush(HighlightColor))
                                g.FillRectangle(b2, rect);
                            break;
                        case ImageTool.Arrow:
                            using (var cap = new AdjustableArrowCap(6, 6))
                            using (var pen = new Pen(ArrowColor, 4f) { CustomEndCap = cap })
                                g.DrawLine(pen, _startImage, _currentImage);
                            break;
                        case ImageTool.Crop:
                        {
                            if (rect.Width >= 4 && rect.Height >= 4)
                            {
                                using var dim = new SolidBrush(Color.FromArgb(120, 0, 0, 0));
                                var outer = new Region(new Rectangle(0, 0, pictureBox1.Image.Width, pictureBox1.Image.Height));
                                outer.Exclude(rect);
                                if (!outer.IsEmpty(g))
                                    g.FillRegion(dim, outer);
                            }
                            break;
                        }
                    }

                    // 选区描边（避免 width=0 触发 GDI+ 异常）
                    if (rect.Width >= 2 && rect.Height >= 2)
                    {
                        using var p = new Pen(Color.FromArgb(220, 0, 120, 215), Math.Max(0.5f, 1.5f / sx)) { DashStyle = DashStyle.Dash };
                        g.DrawRectangle(p, rect);
                    }
                }
            }

            if (_selTarget == HitTarget.Indicator)
                DrawSelection(g, evt.Overlay.IndicatorX - 6, evt.Overlay.IndicatorY - 6, 40, 46, sx);
            else if (_selTarget == HitTarget.Label)
                DrawSelection(g, evt.Overlay.LabelX, evt.Overlay.LabelY, evt.Overlay.LabelWidth, evt.Overlay.LabelHeight, sx);
            else if (_selTarget == HitTarget.Annotation && _selIndex >= 0 && _selIndex < evt.Overlay.Annotations.Count)
            {
                var a = evt.Overlay.Annotations[_selIndex];
                if (a.Kind == AnnotationKind.Arrow)
                    DrawSelection(g,
                        Math.Min(a.StartX, a.EndX) - 6, Math.Min(a.StartY, a.EndY) - 6,
                        Math.Abs(a.EndX - a.StartX) + 12, Math.Abs(a.EndY - a.StartY) + 12, sx);
                else
                    DrawSelection(g, a.X - 3, a.Y - 3, a.Width + 6, a.Height + 6, sx);
            }

            // 还原坐标变换，保持共享画布干净
            g.Restore(state);
        }

        private static void DrawSelection(Graphics g, int x, int y, int w, int h, float scale)
        {
            using var pen = new Pen(Color.FromArgb(255, 0, 120, 215), 2f / scale) { DashStyle = DashStyle.Dash };
            g.DrawRectangle(pen, x, y, w, h);

            int hs = (int)(4 / scale) + 2;
            using var handle = new SolidBrush(Color.FromArgb(255, 0, 120, 215));
            g.FillRectangle(handle, x - hs, y - hs, hs * 2, hs * 2);
            g.FillRectangle(handle, x + w - hs, y - hs, hs * 2, hs * 2);
            g.FillRectangle(handle, x - hs, y + h - hs, hs * 2, hs * 2);
            g.FillRectangle(handle, x + w - hs, y + h - hs, hs * 2, hs * 2);
        }

        // ── 坐标换算 ────────────────────────────────────────────────

        private Point ControlPointToImagePoint(Point controlPt)
        {
            Rectangle view = GetImageRectInZoomMode(pictureBox1);
            if (view.Width == 0 || view.Height == 0 || pictureBox1.Image == null) return controlPt;

            float scaleX = pictureBox1.Image.Width / (float)view.Width;
            float scaleY = pictureBox1.Image.Height / (float)view.Height;

            int x = (int)Math.Round((controlPt.X - view.X) * scaleX);
            int y = (int)Math.Round((controlPt.Y - view.Y) * scaleY);

            x = Math.Max(0, Math.Min(pictureBox1.Image.Width - 1, x));
            y = Math.Max(0, Math.Min(pictureBox1.Image.Height - 1, y));
            return new Point(x, y);
        }

        private static Rectangle GetImageRectInZoomMode(PictureBox pb)
        {
            if (pb.Image == null) return Rectangle.Empty;
            float imgAspect = pb.Image.Width / (float)pb.Image.Height;
            float ctlAspect = pb.Width / (float)pb.Height;

            int drawW, drawH;
            if (imgAspect > ctlAspect) { drawW = pb.Width; drawH = (int)(pb.Width / imgAspect); }
            else { drawH = pb.Height; drawW = (int)(pb.Height * imgAspect); }

            return new Rectangle((pb.Width - drawW) / 2, (pb.Height - drawH) / 2, drawW, drawH);
        }

        private static Rectangle RectFromPoints(Point a, Point b) =>
            new Rectangle(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y), Math.Abs(a.X - b.X), Math.Abs(a.Y - b.Y));
    }
}
