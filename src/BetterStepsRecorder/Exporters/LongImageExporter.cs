using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

namespace BetterStepsRecorder.Exporters
{
    /// <summary>
    /// 把所有步骤按纵向顺序拼接成一张长图，每步带编号与说明。
    /// 适合直接贴到文档、聊天窗口或 Wiki 里。
    /// </summary>
    public class LongImageExporter : ExporterBase
    {
        public override bool Export(string filePath)
            => Export(filePath, BSRSettings.Current.ExportOptions.LongImage);

        public bool Export(string filePath, BSRSettings.LongImageSettings cfg)
        {
            try
            {
                EnsureDirectoryExists(filePath);

                var items = Program._recordEvents;
                if (items.Count == 0)
                {
                    ShowExportError("没有可导出的步骤。");
                    return false;
                }

                // 1) 先渲染并（可选）统一宽度
                var bitmaps = new List<Bitmap>();
                var captions = new List<string>();
                int uniformWidth = cfg.UniformWidth > 0 ? cfg.UniformWidth : 0;

                for (int i = 0; i < items.Count; i++)
                {
                    Bitmap? bmp = StepRenderer.Render(items[i]);
                    if (bmp == null) continue;

                    if (uniformWidth > 0 && bmp.Width != uniformWidth)
                    {
                        int h = (int)Math.Round(bmp.Height * (uniformWidth / (double)bmp.Width));
                        if (h > 0)
                        {
                            var scaled = new Bitmap(uniformWidth, h, PixelFormat.Format32bppArgb);
                            using (var g = Graphics.FromImage(scaled))
                            {
                                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                                g.DrawImage(bmp, new Rectangle(0, 0, uniformWidth, h));
                            }
                            bmp.Dispose();
                            bmp = scaled;
                        }
                    }

                    string caption = string.Empty;
                    if (cfg.ShowStepNumber) caption = $"步骤 {i + 1}";
                    if (cfg.ShowStepText && !string.IsNullOrWhiteSpace(items[i]._StepText))
                        caption = string.IsNullOrEmpty(caption)
                            ? items[i]._StepText!
                            : $"{caption}：{items[i]._StepText}";

                    bitmaps.Add(bmp);
                    captions.Add(caption);
                }

                if (bitmaps.Count == 0)
                {
                    ShowExportError("没有可导出的步骤截图。");
                    return false;
                }

                // 2) 计算画布尺寸
                int gap = Math.Max(0, cfg.Gap);
                int captionH = cfg.ShowStepNumber || cfg.ShowStepText ? Math.Max(24, cfg.CaptionHeight) : 0;

                int canvasW = 0, canvasH = 0;
                foreach (var b in bitmaps) canvasW = Math.Max(canvasW, b.Width);
                foreach (var b in bitmaps) canvasH += captionH + b.Height + gap;
                canvasH += gap; // 顶部留白

                // 3) 绘制
                using var canvas = new Bitmap(canvasW, canvasH, PixelFormat.Format32bppArgb);
                using (var g = Graphics.FromImage(canvas))
                {
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.Clear(Color.White);

                    int y = gap;
                    for (int i = 0; i < bitmaps.Count; i++)
                    {
                        if (captionH > 0 && !string.IsNullOrEmpty(captions[i]))
                        {
                            using var font = new Font("Microsoft YaHei UI", Math.Max(12, captionH - 18), FontStyle.Bold, GraphicsUnit.Pixel);
                            using var brush = new SolidBrush(Color.FromArgb(33, 37, 41));
                            using var accent = new SolidBrush(Color.FromArgb(220, 53, 69));
                            g.FillRectangle(accent, 0, y + 4, 4, captionH - 12);
                            g.DrawString(captions[i], font, brush, new PointF(12, y + 2));
                        }
                        y += captionH;

                        g.DrawImage(bitmaps[i], 0, y);
                        y += bitmaps[i].Height + gap;
                    }
                }

                foreach (var b in bitmaps) b.Dispose();

                string ext = Path.GetExtension(filePath).ToLowerInvariant();
                ImageFormat format = ext == ".jpg" || ext == ".jpeg" ? ImageFormat.Jpeg : ImageFormat.Png;
                canvas.Save(filePath, format);

                ShowExportSuccess(filePath);
                return true;
            }
            catch (Exception ex)
            {
                ShowExportError("导出长图失败", ex);
                return false;
            }
        }
    }
}
