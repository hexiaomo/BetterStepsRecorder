using System;
using System.IO;
using System.Text;

namespace BetterStepsRecorder.Exporters
{
    /// <summary>
    /// Exporter for Markdown files (GitHub-flavored markdown)
    /// </summary>
    public class MarkdownExporter : ExporterBase
    {
        private static string FormatDuration(TimeSpan ts) => ExportText.FormatDuration(ts);

        /// <summary>
        /// Exports the current steps recording to Markdown format
        /// </summary>
        /// <param name="filePath">The full path where the Markdown file should be saved</param>
        /// <returns>True if export was successful, false otherwise</returns>
        public override bool Export(string filePath)
        {
            var cfg = BSRSettings.Current.ExportOptions.Markdown;
            return Export(filePath, cfg);
        }

        /// <summary>
        /// Exports the current steps recording to Markdown format using the supplied settings
        /// </summary>
        public bool Export(string filePath, BSRSettings.MarkdownSettings cfg)
        {
            try
            {
                EnsureDirectoryExists(filePath);

                // Get the filename without extension to use as title and folder name
                string title = Path.GetFileNameWithoutExtension(filePath);
                string imagesFolderName = title.Replace(" ", "_") + "_images";

                // Create images folder with the same name as the .md file (with spaces replaced) + "_images"
                string folderPath = Path.GetDirectoryName(filePath);
                string imagesFolder = Path.Combine(folderPath, imagesFolderName);
                if (!Directory.Exists(imagesFolder))
                {
                    Directory.CreateDirectory(imagesFolder);
                }

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

                // Start building the Markdown content
                StringBuilder md = new StringBuilder();

                // Title
                md.AppendLine($"# {title}");
                md.AppendLine();

                // Generated date
                if (cfg.ShowGeneratedDate)
                {
                    md.AppendLine($"*{generated}*");
                    md.AppendLine();
                }

                // Summary section
                if (cfg.ShowSummary)
                {
                    md.AppendLine("## 摘要");
                    md.AppendLine();
                    md.AppendLine("| 项目 | 值 |");
                    md.AppendLine("|----------|-------|");
                    md.AppendLine($"| 步骤数 | {totalSteps} |");
                    md.AppendLine($"| 开始时间 | {startStr} |");
                    md.AppendLine($"| 结束时间 | {endStr} |");
                    md.AppendLine($"| 总耗时 | {durationStr} |");
                    md.AppendLine();
                }

                // Steps section
                md.AppendLine("## 步骤");
                md.AppendLine();

                // Add each step
                DateTime? prevTime = null;
                foreach (var recordEvent in Program._recordEvents)
                {
                    string stepText = recordEvent._StepText ?? string.Empty;

                    // Step header
                    md.AppendLine($"### 步骤 {recordEvent.Step}");
                    md.AppendLine();
                    md.AppendLine($"**{stepText}**");
                    md.AppendLine();

                    // Timestamp
                    if (cfg.ShowStepTimestamps)
                    {
                        string timeStr;
                        if (prevTime.HasValue)
                        {
                            TimeSpan delta = recordEvent.CreationTime - prevTime.Value;
                            timeStr = $"⏱️ {prevTime.Value:HH:mm:ss} → {recordEvent.CreationTime:HH:mm:ss} (+{FormatDuration(delta)})";
                        }
                        else
                        {
                            timeStr = $"⏱️ {recordEvent.CreationTime:HH:mm:ss}";
                        }
                        md.AppendLine(timeStr);
                        md.AppendLine();
                    }
                    prevTime = recordEvent.CreationTime;

                    // Detail table - only rendered when at least one detail option is on
                    if (!cfg.IsDetailTableEmpty)
                    {
                        md.AppendLine("| 明细 | 值 |");
                        md.AppendLine("|--------|-------|");
                        if (cfg.ShowAction && !string.IsNullOrWhiteSpace(recordEvent.EventType))
                            md.AppendLine($"| 操作 | {ExportText.ActionOf(recordEvent)} |");
                        if (cfg.ShowApplication && !string.IsNullOrWhiteSpace(recordEvent.ApplicationName))
                            md.AppendLine($"| 应用程序 | {recordEvent.ApplicationName} |");
                        if (cfg.ShowWindow && !string.IsNullOrWhiteSpace(recordEvent.WindowTitle))
                            md.AppendLine($"| 窗口 | {recordEvent.WindowTitle} |");
                        if (cfg.ShowElement && !string.IsNullOrWhiteSpace(recordEvent.ElementName))
                            md.AppendLine($"| 元素 | {recordEvent.ElementName} |");
                        if (cfg.ShowElementType && !string.IsNullOrWhiteSpace(recordEvent.ElementType))
                            md.AppendLine($"| 元素类型 | {recordEvent.ElementType} |");
                        if (cfg.ShowMousePosition && (recordEvent.MouseCoordinates.X != 0 || recordEvent.MouseCoordinates.Y != 0))
                            md.AppendLine($"| 鼠标位置 | {recordEvent.MouseCoordinates.X}, {recordEvent.MouseCoordinates.Y} |");
                        md.AppendLine();
                    }

                    // Screenshot
                    if (recordEvent.HasScreenshot)
                    {
                        string imageFileName = $"step_{recordEvent.Step}_{recordEvent.ShortId}.png";
                        string imageFilePath = Path.Combine(imagesFolder, imageFileName);

                        if (SaveImageFromEvent(recordEvent, imageFilePath))
                        {
                            md.AppendLine($"![步骤 {recordEvent.Step} 截图]({imagesFolderName}/{imageFileName})");
                            md.AppendLine();
                        }
                    }
                    else
                    {
                        md.AppendLine("*此步骤没有截图。*");
                        md.AppendLine();
                    }

                    // Add horizontal rule between steps (except after the last step)
                    if (recordEvent.Step < totalSteps)
                    {
                        md.AppendLine("---");
                        md.AppendLine();
                    }
                }

                // Footer
                md.AppendLine();
                md.AppendLine("---");
                md.AppendLine();
                md.AppendLine($"*{ExportText.GeneratedWithPrefix}[{ExportText.AppLinkText}]({ExportText.AppLinkUrl}){ExportText.GeneratedWithSuffix}*");

                // Write the Markdown file
                using (var writer = new StreamWriter(filePath, append: false, encoding: Encoding.UTF8))
                {
                    writer.Write(md);
                }

                ShowExportSuccess(filePath);
                return true;
            }
            catch (Exception ex)
            {
                ShowExportError("导出 Markdown 失败", ex);
                return false;
            }
        }
    }
}
