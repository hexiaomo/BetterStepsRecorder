using System;
using System.IO;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace BetterStepsRecorder.Exporters
{
    /// <summary>
    /// Exporter for Rich Text Format (RTF) files
    /// </summary>
    public class RtfExporter : ExporterBase
    {
        private static string FormatDuration(TimeSpan ts) => ExportText.FormatDuration(ts);

        /// <summary>
        /// Exports the current steps recording to RTF format
        /// </summary>
        /// <param name="filePath">The full path where the RTF file should be saved</param>
        /// <returns>True if export was successful, false otherwise</returns>
        public override bool Export(string filePath)
        {
            var cfg = BSRSettings.Current.ExportOptions.Rtf;
            return Export(filePath, cfg);
        }

        /// <summary>
        /// Exports the current steps recording to RTF format using the supplied settings
        /// </summary>
        public bool Export(string filePath, BSRSettings.RtfSettings cfg)
        {
            try
            {
                EnsureDirectoryExists(filePath);

                // Get the filename without extension to use as title
                string title = Path.GetFileNameWithoutExtension(filePath);

                int totalSteps = Program._recordEvents.Count;
                string generated = ExportText.GeneratedAt(DateTime.Now);

                // Compute recording start/end/duration from event timestamps
                DateTime? recordingStart = totalSteps > 0 ? Program._recordEvents[0].CreationTime : (DateTime?)null;
                DateTime? recordingEnd = totalSteps > 0 ? Program._recordEvents[totalSteps - 1].CreationTime : (DateTime?)null;
                TimeSpan totalDuration = (recordingStart.HasValue && recordingEnd.HasValue)
                    ? recordingEnd.Value - recordingStart.Value
                    : TimeSpan.Zero;

                string startStr = recordingStart.HasValue ? ExportText.FormatDateTime(recordingStart.Value) : "—";
                string endStr = recordingEnd?.ToString("HH:mm:ss") ?? "—";
                string durationStr = totalSteps > 1 ? FormatDuration(totalDuration) : "—";

                using (RichTextBox rtfBox = new RichTextBox())
                using (var fontBody     = new Font("Microsoft YaHei UI", 10))
                using (var fontTitle    = new Font("Microsoft YaHei UI", 16, FontStyle.Bold))
                using (var fontMeta     = new Font("Microsoft YaHei UI", 9))
                using (var fontStep     = new Font("Microsoft YaHei UI", 12, FontStyle.Bold))
                using (var fontDetail   = new Font("Microsoft YaHei UI", 9))
                using (var fontDetailLabel = new Font("Microsoft YaHei UI", 9, FontStyle.Bold))
                using (var fontSep      = new Font("Microsoft YaHei UI", 9))
                using (var fontFooter   = new Font("Microsoft YaHei UI", 8))
                using (var fontLink     = new Font("Microsoft YaHei UI", 8, FontStyle.Underline))
                {
                    // Set document properties
                    rtfBox.Font = fontBody;

                    // Add title using the filename
                    rtfBox.SelectionFont = fontTitle;
                    rtfBox.AppendText($"{title}\n");

                    // Add generated date
                    if (cfg.ShowGeneratedDate)
                    {
                        rtfBox.SelectionFont = fontMeta;
                        rtfBox.SelectionColor = Color.Gray;
                        rtfBox.AppendText($"{generated}\n");
                        rtfBox.SelectionColor = rtfBox.ForeColor;
                    }

                    rtfBox.AppendText("\n");

                    // Add summary section
                    if (cfg.ShowSummary)
                    {
                        rtfBox.SelectionFont = fontDetailLabel;
                        rtfBox.AppendText("摘要\n");
                        rtfBox.SelectionFont = fontDetail;
                        rtfBox.AppendText($"步骤数：{totalSteps}\n");
                        rtfBox.AppendText($"开始时间：{startStr}\n");
                        rtfBox.AppendText($"结束时间：{endStr}\n");
                        rtfBox.AppendText($"总耗时：{durationStr}\n");
                        rtfBox.AppendText("\n");
                    }

                    // Add each step
                    DateTime? prevTime = null;
                    foreach (var recordEvent in Program._recordEvents)
                    {
                        // Add step header
                        rtfBox.SelectionFont = fontStep;
                        rtfBox.AppendText($"步骤 {recordEvent.Step}：{recordEvent._StepText}\n");

                        // Add timestamp
                        if (cfg.ShowStepTimestamps)
                        {
                            rtfBox.SelectionFont = fontMeta;
                            rtfBox.SelectionColor = Color.Gray;
                            string timeStr;
                            if (prevTime.HasValue)
                            {
                                TimeSpan delta = recordEvent.CreationTime - prevTime.Value;
                                timeStr = $"{prevTime.Value:HH:mm:ss} → {recordEvent.CreationTime:HH:mm:ss} (+{FormatDuration(delta)})";
                            }
                            else
                            {
                                timeStr = recordEvent.CreationTime.ToString("HH:mm:ss");
                            }
                            rtfBox.AppendText($"{timeStr}\n");
                            rtfBox.SelectionColor = rtfBox.ForeColor;
                        }
                        prevTime = recordEvent.CreationTime;

                        // Add detail strip - only if at least one detail option is on
                        if (!cfg.IsDetailStripEmpty)
                        {
                            rtfBox.AppendText("\n");
                            if (cfg.ShowAction && !string.IsNullOrWhiteSpace(recordEvent.EventType))
                            {
                                rtfBox.SelectionFont = fontDetailLabel;
                                rtfBox.AppendText("操作：");
                                rtfBox.SelectionFont = fontDetail;
                                rtfBox.AppendText($"{ExportText.ActionOf(recordEvent)}\n");
                            }
                            if (cfg.ShowApplication && !string.IsNullOrWhiteSpace(recordEvent.ApplicationName))
                            {
                                rtfBox.SelectionFont = fontDetailLabel;
                                rtfBox.AppendText("应用程序：");
                                rtfBox.SelectionFont = fontDetail;
                                rtfBox.AppendText($"{recordEvent.ApplicationName}\n");
                            }
                            if (cfg.ShowWindow && !string.IsNullOrWhiteSpace(recordEvent.WindowTitle))
                            {
                                rtfBox.SelectionFont = fontDetailLabel;
                                rtfBox.AppendText("窗口：");
                                rtfBox.SelectionFont = fontDetail;
                                rtfBox.AppendText($"{recordEvent.WindowTitle}\n");
                            }
                            if (cfg.ShowElement && !string.IsNullOrWhiteSpace(recordEvent.ElementName))
                            {
                                rtfBox.SelectionFont = fontDetailLabel;
                                rtfBox.AppendText("元素：");
                                rtfBox.SelectionFont = fontDetail;
                                rtfBox.AppendText($"{recordEvent.ElementName}\n");
                            }
                            if (cfg.ShowElementType && !string.IsNullOrWhiteSpace(recordEvent.ElementType))
                            {
                                rtfBox.SelectionFont = fontDetailLabel;
                                rtfBox.AppendText("元素类型：");
                                rtfBox.SelectionFont = fontDetail;
                                rtfBox.AppendText($"{recordEvent.ElementType}\n");
                            }
                            if (cfg.ShowMousePosition && (recordEvent.MouseCoordinates.X != 0 || recordEvent.MouseCoordinates.Y != 0))
                            {
                                rtfBox.SelectionFont = fontDetailLabel;
                                rtfBox.AppendText("鼠标位置：");
                                rtfBox.SelectionFont = fontDetail;
                                rtfBox.AppendText($"{recordEvent.MouseCoordinates.X}, {recordEvent.MouseCoordinates.Y}\n");
                            }
                        }

                        // Add screenshot if available
                        if (recordEvent.HasScreenshot)
                        {
                            rtfBox.AppendText("\n");

                            using (Image img = GetRtfImage(recordEvent))
                            {
                                if (img != null)
                                {
                                    Clipboard.SetImage(img);
                                    rtfBox.Paste();
                                    rtfBox.AppendText("\n");
                                }
                            }
                        }

                        // Add separator between steps
                        rtfBox.SelectionFont = fontSep;
                        rtfBox.AppendText("\n----------------------------\n\n");
                    }

                    // Add footer with link to GitHub
                    rtfBox.SelectionAlignment = HorizontalAlignment.Center;
                    rtfBox.AppendText("\n");
                    rtfBox.SelectionFont = fontFooter;
                    rtfBox.AppendText(ExportText.GeneratedWithPrefix);

                    // Add the hyperlink text
                    rtfBox.SelectionColor = Color.Blue;
                    rtfBox.SelectionFont = fontLink;
                    rtfBox.AppendText(ExportText.AppLinkText + ExportText.GeneratedWithSuffix);

                    // Add the URL in parentheses
                    rtfBox.SelectionFont = fontFooter;
                    rtfBox.SelectionColor = rtfBox.ForeColor;
                    rtfBox.AppendText($" ({ExportText.AppLinkUrl})");

                    // Save the RTF file
                    rtfBox.SaveFile(filePath);
                }

                ShowExportSuccess(filePath);
                return true;
            }
            catch (Exception ex)
            {
                ShowExportError("导出 RTF 失败", ex);
                return false;
            }
        }
        
        /// <summary>
        /// Loads and scales a screenshot from a RecordEvent for embedding in RTF.
        /// </summary>
        private Image GetRtfImage(RecordEvent recordEvent)
        {
            try
            {
                byte[]? imageBytes = Program.GetScreenshotBytes(recordEvent);
                if (imageBytes == null) return null;
                using (var ms = new MemoryStream(imageBytes))
                using (var original = new Bitmap(ms))
                {
                    const int maxWidth = 800;
                    int targetWidth = Math.Min(original.Width, maxWidth);
                    int targetHeight = (int)((double)original.Height / original.Width * targetWidth);
                    return new Bitmap(original, targetWidth, targetHeight);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetRtfImage failed: {ex.Message}");
                return null;
            }
        }
    }
}