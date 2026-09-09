using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

namespace BetterStepsRecorder.Exporters
{
    /// <summary>
    /// 导出 PDF：一步一页，页内是「步骤标题 + 合成后的截图」。
    /// 标题先由 GDI+ 渲染成位图再嵌入，因此中文不会乱码，也不需要嵌入字体。
    /// </summary>
    public class PdfExporter : ExporterBase
    {
        // A4 / Letter 尺寸（point）
        private const float A4Width = 595.28f, A4Height = 841.89f;
        private const float LetterWidth = 612f, LetterHeight = 792f;

        public override bool Export(string filePath)
            => Export(filePath, BSRSettings.Current.ExportOptions.Pdf);

        public bool Export(string filePath, BSRSettings.PdfSettings cfg)
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

                var writer = new MiniPdfWriter();
                int quality = Math.Max(40, Math.Min(100, cfg.JpegQuality));
                var encoder = GetJpegEncoder();

                for (int i = 0; i < items.Count; i++)
                {
                    using Bitmap? shot = StepRenderer.Render(items[i]);
                    if (shot == null) continue;

                    // 页面尺寸：A4 / Letter / 跟随图片
                    float pageW, pageH;
                    switch ((cfg.PageSize ?? "A4").Trim().ToLowerInvariant())
                    {
                        case "letter": pageW = LetterWidth; pageH = LetterHeight; break;
                        case "fitimage":
                        {
                            float s = 72f / 96f;
                            pageW = Math.Max(200f, shot.Width * s);
                            pageH = Math.Max(200f, shot.Height * s);
                            break;
                        }
                        default: pageW = A4Width; pageH = A4Height; break;
                    }

                    // 以 2 倍分辨率渲染页面，保证打印/放大后依然清晰
                    const int scale = 2;
                    int pxW = (int)Math.Round(pageW / 72f * 96f * scale);
                    int pxH = (int)Math.Round(pageH / 72f * 96f * scale);

                    using var page = new Bitmap(pxW, pxH, PixelFormat.Format24bppRgb);
                    using (var g = Graphics.FromImage(page))
                    {
                        g.SmoothingMode = SmoothingMode.AntiAlias;
                        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        g.Clear(Color.White);

                        int margin = (int)Math.Round(36f * scale);
                        int captionH = 0;
                        string caption = string.Empty;

                        if (cfg.ShowStepNumber) caption = $"步骤 {i + 1}";
                        if (cfg.ShowStepText && !string.IsNullOrWhiteSpace(items[i]._StepText))
                            caption = string.IsNullOrEmpty(caption)
                                ? items[i]._StepText!
                                : $"{caption}：{items[i]._StepText}";

                        if (!string.IsNullOrEmpty(caption))
                        {
                            captionH = (int)Math.Round(26f * scale);
                            using var font = new Font("Microsoft YaHei UI", 11f * scale, FontStyle.Bold, GraphicsUnit.Pixel);
                            using var brush = new SolidBrush(Color.FromArgb(33, 37, 41));
                            using var accent = new SolidBrush(Color.FromArgb(220, 53, 69));
                            g.FillRectangle(accent, margin, margin + 4 * scale, 4 * scale, captionH - 10 * scale);
                            g.DrawString(caption, font, brush,
                                new RectangleF(margin + 12 * scale, margin, pxW - margin * 2 - 12 * scale, captionH));
                        }

                        int areaX = margin;
                        int areaY = margin + captionH + (captionH > 0 ? (int)(8 * scale) : 0);
                        int areaW = pxW - margin * 2;
                        int areaH = pxH - margin * 2 - captionH - (captionH > 0 ? (int)(8 * scale) : 0);

                        if (areaW > 0 && areaH > 0)
                        {
                            double ratio = Math.Min(areaW / (double)shot.Width, areaH / (double)shot.Height);
                            int drawW = Math.Max(1, (int)Math.Round(shot.Width * ratio));
                            int drawH = Math.Max(1, (int)Math.Round(shot.Height * ratio));
                            int drawX = areaX + (areaW - drawW) / 2;
                            int drawY = areaY + (areaH - drawH) / 2;

                            g.DrawImage(shot, new Rectangle(drawX, drawY, drawW, drawH));
                            using var frame = new Pen(Color.FromArgb(222, 226, 230), scale);
                            g.DrawRectangle(frame, drawX, drawY, drawW, drawH);
                        }
                    }

                    byte[] jpeg;
                    using (var ms = new MemoryStream())
                    using (var ps = new EncoderParameters(1))
                    {
                        ps.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, (long)quality);
                        page.Save(ms, encoder, ps);
                        jpeg = ms.ToArray();
                    }

                    writer.AddPage(jpeg, pxW, pxH, pageW, pageH);
                }

                if (writer.PageCount == 0)
                {
                    ShowExportError("没有可导出的步骤截图。");
                    return false;
                }

                File.WriteAllBytes(filePath, writer.Build());
                ShowExportSuccess(filePath);
                return true;
            }
            catch (Exception ex)
            {
                ShowExportError("导出 PDF 失败", ex);
                return false;
            }
        }

        private static ImageCodecInfo GetJpegEncoder()
        {
            foreach (var codec in ImageCodecInfo.GetImageEncoders())
                if (codec.MimeType == "image/jpeg")
                    return codec;
            throw new InvalidOperationException("未找到 JPEG 编码器");
        }
    }
}
