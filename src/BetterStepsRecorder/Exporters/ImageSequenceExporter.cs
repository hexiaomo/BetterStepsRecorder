using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;

namespace BetterStepsRecorder.Exporters
{
    /// <summary>
    /// 按步骤顺序导出多张图片：001_点击此.png、002_xxx.png……
    /// 文件名跟随当前步骤顺序，重排后会重新编号。
    /// </summary>
    public class ImageSequenceExporter : ExporterBase
    {
        public override bool Export(string folderPath)
            => Export(folderPath, BSRSettings.Current.ExportOptions.ImageSequence);

        public bool Export(string folderPath, BSRSettings.ImageSequenceSettings cfg)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(folderPath)) return false;
                Directory.CreateDirectory(folderPath);

                var items = Program._recordEvents;
                if (items.Count == 0)
                {
                    ShowExportError("没有可导出的步骤。");
                    return false;
                }

                int digits = Math.Max(1, Math.Min(6, cfg.NumberDigits));
                int exported = 0;

                for (int i = 0; i < items.Count; i++)
                {
                    using Bitmap? bmp = StepRenderer.Render(items[i]);
                    if (bmp == null) continue;

                    string number = (i + 1).ToString("D" + digits);
                    string name = number.ToString();

                    if (cfg.IncludeStepTextInFileName)
                    {
                        string text = (items[i]._StepText ?? string.Empty).Trim();
                        if (text.Length > 0)
                            name = number + cfg.Separator + SafeFileName(text);
                    }

                    string path = Path.Combine(folderPath, name + ".png");
                    int dup = 1;
                    while (File.Exists(path))
                        path = Path.Combine(folderPath, $"{name}_{++dup}.png");

                    bmp.Save(path, ImageFormat.Png);
                    exported++;
                }

                if (exported == 0)
                {
                    ShowExportError("没有可导出的步骤截图。");
                    return false;
                }

                ShowExportSuccess($"{folderPath}（共 {exported} 张）");
                return true;
            }
            catch (Exception ex)
            {
                ShowExportError("导出图片序列失败", ex);
                return false;
            }
        }

        private static string SafeFileName(string text)
        {
            var sb = new StringBuilder();
            foreach (char c in text)
            {
                if (c is '\\' or '/' or ':' or '*' or '?' or '"' or '<' or '>' or '|')
                    continue;
                sb.Append(c);
                if (sb.Length >= 40) break;
            }
            string s = sb.ToString().Trim();
            return s.Length == 0 ? "步骤" : s;
        }
    }
}
