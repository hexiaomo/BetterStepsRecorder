using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

namespace BetterStepsRecorder
{
    /// <summary>
    /// 把「原始截图 + 叠加层」合成为最终图片。
    /// 编辑器的显示、以及所有导出格式都走这里，保证所见即所得。
    /// </summary>
    public static class StepRenderer
    {
        /// <summary>合成一个步骤的最终图片（调用方负责 Dispose）。</summary>
        public static Bitmap? Render(RecordEvent evt)
        {
            byte[]? raw = Program.GetBaseScreenshotBytes(evt);
            if (raw == null) return null;

            Bitmap bmp;
            try
            {
                using var ms = new MemoryStream(raw);
                // GDI+ 要求源流在位图存活期间保持打开；这里立即复制一份与流无关的位图
                using var tmp = new Bitmap(ms);
                bmp = new Bitmap(tmp);
            }
            catch { return null; }

            var ov = evt.Overlay;

            // 1) 裁剪
            if (ov != null && ov.HasCrop)
            {
                Rectangle c = ov.CropRect;
                c.Intersect(new Rectangle(0, 0, bmp.Width, bmp.Height));
                if (c.Width >= 1 && c.Height >= 1 &&
                    (c.Width != bmp.Width || c.Height != bmp.Height))
                {
                    try
                    {
                        var cropped = bmp.Clone(c, bmp.PixelFormat);
                        bmp.Dispose();
                        bmp = cropped;
                    }
                    catch { /* 裁剪失败则保留原图 */ }
                }
            }

            if (ov == null) return bmp;

            // 2) 标注（马赛克直接改像素，其余用 GDI+ 绘制）
            foreach (var a in ov.Annotations)
            {
                if (a.Kind == AnnotationKind.Mosaic)
                    ApplyMosaic(bmp, a.Bounds, a.MosaicBlock);
            }

            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;

                foreach (var a in ov.Annotations)
                {
                    if (a.Kind != AnnotationKind.Mosaic)
                        DrawAnnotation(g, a);
                }

                // 3) 鼠标指针
                if (ov.IndicatorEnabled)
                    DrawIndicator(g, ov, bmp.Width, bmp.Height);

                // 4) 点击提示文字框
                if (ov.LabelEnabled && !string.IsNullOrEmpty(ov.LabelText))
                    DrawLabelBox(g, ov);
            }

            return bmp;
        }

        /// <summary>合成后编码为 PNG 字节。</summary>
        public static byte[]? RenderToBytes(RecordEvent evt)
        {
            using var bmp = Render(evt);
            if (bmp == null) return null;
            using var ms = new MemoryStream();
            bmp.Save(ms, ImageFormat.Png);
            return ms.ToArray();
        }

        // ── 马赛克 ──────────────────────────────────────────────────

        public static void ApplyMosaic(Bitmap bmp, Rectangle rect, int block)
        {
            rect.Intersect(new Rectangle(0, 0, bmp.Width, bmp.Height));
            if (rect.Width <= 0 || rect.Height <= 0) return;
            block = Math.Max(2, block);

            // LockBits 只能使用位图自身的 PixelFormat；非 32bppArgb 时临时复制一份
            Bitmap? scratch = null;
            Bitmap target = bmp;
            if (bmp.PixelFormat != PixelFormat.Format32bppArgb)
            {
                scratch = new Bitmap(bmp.Width, bmp.Height, PixelFormat.Format32bppArgb);
                using (var g = Graphics.FromImage(scratch))
                    g.DrawImage(bmp, new Rectangle(0, 0, bmp.Width, bmp.Height));
                target = scratch;
            }

            BitmapData data;
            try
            {
                data = target.LockBits(new Rectangle(0, 0, target.Width, target.Height),
                                       ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            }
            catch { scratch?.Dispose(); return; }

            try
            {
            int stride = data.Stride;
            int byteCount = stride * target.Height;
            byte[] px = new byte[byteCount];
            Marshal.Copy(data.Scan0, px, 0, byteCount);

                for (int y = rect.Top; y < rect.Bottom; y += block)
                {
                    for (int x = rect.Left; x < rect.Right; x += block)
                    {
                        int bxMax = Math.Min(x + block, rect.Right);
                        int byMax = Math.Min(y + block, rect.Bottom);

                        int sumB = 0, sumG = 0, sumR = 0, count = 0;
                        for (int yy = y; yy < byMax; yy++)
                            for (int xx = x; xx < bxMax; xx++)
                            {
                                int i = yy * stride + xx * 4;
                                sumB += px[i]; sumG += px[i + 1]; sumR += px[i + 2];
                                count++;
                            }
                        if (count == 0) continue;

                        byte b = (byte)(sumB / count), g = (byte)(sumG / count), r = (byte)(sumR / count);
                        for (int yy = y; yy < byMax; yy++)
                            for (int xx = x; xx < bxMax; xx++)
                            {
                                int i = yy * stride + xx * 4;
                                px[i] = b; px[i + 1] = g; px[i + 2] = r;
                            }
                    }
                }

                Marshal.Copy(px, 0, data.Scan0, byteCount);
            }
            finally
            {
                try { target.UnlockBits(data); } catch { }
            }

            if (scratch != null)
            {
                using (var g = Graphics.FromImage(bmp))
                    g.DrawImage(scratch, new Rectangle(0, 0, bmp.Width, bmp.Height));
                scratch.Dispose();
            }
        }

        // ── 标注绘制 ────────────────────────────────────────────────

        public static void DrawAnnotation(Graphics g, Annotation a)
        {
            switch (a.Kind)
            {
                case AnnotationKind.Highlight:
                    using (var b = new SolidBrush(Color.FromArgb(120, a.Color)))
                        g.FillRectangle(b, a.Bounds);
                    break;

                case AnnotationKind.Arrow:
                    using (var cap = new AdjustableArrowCap(6, 6))
                    using (var pen = new Pen(a.Color, 4f) { CustomEndCap = cap })
                        g.DrawLine(pen, a.StartPoint, a.EndPoint);
                    break;

                case AnnotationKind.Rectangle:
                    using (var pen = new Pen(a.Color, 3f))
                        g.DrawRectangle(pen, a.Bounds);
                    break;

                case AnnotationKind.Ellipse:
                    using (var pen = new Pen(a.Color, 3f))
                        g.DrawEllipse(pen, a.Bounds);
                    break;

                case AnnotationKind.Text:
                {
                    using var font = new Font("Microsoft YaHei UI", a.FontSize, FontStyle.Bold, GraphicsUnit.Pixel);
                    using var shadow = new SolidBrush(Color.FromArgb(160, 0, 0, 0));
                    using var brush = new SolidBrush(a.Color);
                    g.DrawString(a.Text, font, shadow, a.X + 1, a.Y + 1);
                    g.DrawString(a.Text, font, brush, a.X, a.Y);
                    break;
                }

                case AnnotationKind.TextBox:
                {
                    var rect = a.Bounds;
                    if (a.Bordered)
                    {
                        using var back = new SolidBrush(Color.FromArgb(230, 255, 255, 255));
                        using var path = RoundedRect(rect, 6);
                        g.FillPath(back, path);
                        using var border = new Pen(a.Color, 2.5f);
                        g.DrawPath(border, path);
                    }
                    if (!string.IsNullOrEmpty(a.Text))
                    {
                        using var font = new Font("Microsoft YaHei UI", a.FontSize, FontStyle.Bold, GraphicsUnit.Pixel);
                        using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                        using var brush = new SolidBrush(a.Color);
                        g.DrawString(a.Text, font, brush, rect, sf);
                    }
                    break;
                }
            }
        }

        // ── 鼠标指针 ────────────────────────────────────────────────

        public static void DrawIndicator(Graphics g, StepOverlay ov, int width, int height)
        {
            int x = ov.IndicatorX, y = ov.IndicatorY;
            Color c = Color.FromArgb(ov.IndicatorColorArgb);

            switch (ov.IndicatorStyle)
            {
                case ClickIndicatorStyle.Circle:
                {
                    int r = 28;
                    using (var fill = new SolidBrush(Color.FromArgb(60, c)))
                        g.FillEllipse(fill, x - r, y - r, r * 2, r * 2);
                    using (var border = new Pen(c, 3.5f))
                        g.DrawEllipse(border, x - r, y - r, r * 2, r * 2);
                    using (var dot = new SolidBrush(c))
                        g.FillEllipse(dot, x - 4, y - 4, 8, 8);
                    break;
                }

                case ClickIndicatorStyle.Arrow:
                {
                    int len = 200;
                    int endX = x;
                    int endY = y < height / 2 ? y + len : y - len;
                    using var cap = new AdjustableArrowCap(5, 5);
                    using var pen = new Pen(c, 5f) { CustomEndCap = cap };
                    g.DrawLine(pen, endX, endY, x, y);
                    break;
                }

                default: // Cursor
                {
                    DrawCursorGlyph(g, x, y, c, 1.0f);
                    break;
                }
            }
        }

        /// <summary>绘制一个标准的鼠标箭头指针（带白色描边，任何底色上都可见）。</summary>
        public static void DrawCursorGlyph(Graphics g, int x, int y, Color c, float scale)
        {
            float s = 28f * scale;
            var poly = new[]
            {
                new PointF(x,             y),
                new PointF(x,             y + s * 0.85f),
                new PointF(x + s * 0.25f, y + s * 0.62f),
                new PointF(x + s * 0.42f, y + s * 0.98f),
                new PointF(x + s * 0.54f, y + s * 0.93f),
                new PointF(x + s * 0.37f, y + s * 0.57f),
                new PointF(x + s * 0.65f, y + s * 0.57f),
            };

            using (var outline = new Pen(Color.White, 3f) { LineJoin = LineJoin.Round })
                g.DrawPolygon(outline, poly);
            using (var fill = new SolidBrush(c))
                g.FillPolygon(fill, poly);
        }

        // ── 提示文字框 ──────────────────────────────────────────────

        public static Size MeasureLabelBox(StepOverlay ov)
        {
            using var img = new Bitmap(1, 1);
            using var g = Graphics.FromImage(img);
            using var font = new Font("Microsoft YaHei UI", ov.LabelFontSize, FontStyle.Bold, GraphicsUnit.Pixel);
            SizeF s = g.MeasureString(ov.LabelText, font);
            return new Size((int)Math.Ceiling(s.Width) + 24, (int)Math.Ceiling(s.Height) + 14);
        }

        public static void DrawLabelBox(Graphics g, StepOverlay ov)
        {
            var rect = new Rectangle(ov.LabelX, ov.LabelY, ov.LabelWidth, ov.LabelHeight);
            if (rect.Width < 8 || rect.Height < 8) return;

            using var back = new SolidBrush(Color.FromArgb(ov.LabelBackArgb));
            using var path = RoundedRect(rect, 6);
            g.FillPath(back, path);
            using var border = new Pen(Color.FromArgb(ov.LabelBorderArgb), 2.5f);
            g.DrawPath(border, path);

            using var font = new Font("Microsoft YaHei UI", ov.LabelFontSize, FontStyle.Bold, GraphicsUnit.Pixel);
            using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            using var brush = new SolidBrush(Color.FromArgb(ov.LabelTextArgb));
            g.DrawString(ov.LabelText, font, brush, rect, sf);
        }

        // ── 小工具 ──────────────────────────────────────────────────

        public static GraphicsPath RoundedRect(Rectangle r, float radius)
        {
            var path = new GraphicsPath();
            if (radius <= 0) { path.AddRectangle(r); return path; }
            radius = Math.Min(radius, Math.Min(r.Width, r.Height) / 2f);
            path.AddArc(r.X, r.Y, radius * 2, radius * 2, 180, 90);
            path.AddArc(r.Right - radius * 2, r.Y, radius * 2, radius * 2, 270, 90);
            path.AddArc(r.Right - radius * 2, r.Bottom - radius * 2, radius * 2, radius * 2, 0, 90);
            path.AddArc(r.X, r.Bottom - radius * 2, radius * 2, radius * 2, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
