using System;
using System.Collections.Generic;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using BetterStepsRecorder.UI;

namespace BetterStepsRecorder.Exporters
{
    /// <summary>
    /// Abstract base class for all exporters with common export functionality
    /// </summary>
    public abstract class ExporterBase
    {
        // Remove the instance property - we'll use the static StatusManager directly
        // protected StatusStripManager StatusManager { get; private set; }

        /// <summary>
        /// Exports the current steps recording to the specified format
        /// </summary>
        /// <param name="filePath">The full path where the export file should be saved</param>
        /// <returns>True if export was successful, false otherwise</returns>
        public abstract bool Export(string filePath);

        /// <summary>
        /// Saves an image from base64 string to a file (kept for backward compatibility).
        /// </summary>
        protected bool SaveImageFromBase64(string base64Image, string filePath, ImageFormat format = null)
        {
            if (string.IsNullOrEmpty(base64Image)) return false;
            try { return SaveImageBytes(Convert.FromBase64String(base64Image), filePath, format); }
            catch (Exception ex) { ReportImageError(ex); return false; }
        }

        /// <summary>
        /// Saves a screenshot from a RecordEvent to a file, reading from spool disk or RAM as needed.
        /// </summary>
        protected bool SaveImageFromEvent(RecordEvent recordEvent, string filePath, ImageFormat format = null)
        {
            byte[]? bytes = Program.GetScreenshotBytes(recordEvent);
            if (bytes == null) return false;
            return SaveImageBytes(bytes, filePath, format);
        }

        private bool SaveImageBytes(byte[] imageBytes, string filePath, ImageFormat format = null)
        {
            try
            {
                using (MemoryStream ms = new MemoryStream(imageBytes))
                using (Image image = Image.FromStream(ms))
                {
                    image.Save(filePath, format ?? ImageFormat.Png);
                }
                return true;
            }
            catch (Exception ex) { ReportImageError(ex); return false; }
        }

        private void ReportImageError(Exception ex)
        {
            if (StatusManager.IsInitialized)
                StatusManager.ShowMessage($"保存图片失败：{ex.Message}", true);
            else
                MessageBox.Show($"保存图片失败：{ex.Message}", "导出错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        /// <summary>
        /// Ensures the directory for a file path exists, creating it if necessary
        /// </summary>
        /// <param name="filePath">The full path to a file</param>
        protected void EnsureDirectoryExists(string filePath)
        {
            string? directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }

        /// <summary>
        /// Shows an error message for export failures
        /// </summary>
        /// <param name="message">The error message</param>
        /// <param name="ex">The exception that occurred</param>
        protected void ShowExportError(string message, Exception ex = null)
        {
            string errorMessage = message;
            if (ex != null)
            {
                errorMessage += $": {ex.Message}";
            }

                MessageBox.Show(errorMessage, "导出错误", MessageBoxButtons.OK, MessageBoxIcon.Error);

        }

        /// <summary>
        /// Shows a success message after export
        /// </summary>
        /// <param name="filePath">The path where the file was exported</param>
        protected void ShowExportSuccess(string filePath)
        {
            // Use the static StatusManager which will throw an exception if not initialized
            StatusManager.ShowSuccess($"已成功导出到：{filePath}");
        }
    }
}