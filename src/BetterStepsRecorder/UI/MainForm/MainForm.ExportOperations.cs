using System;
using System.IO;
using System.Windows.Forms;
using BetterStepsRecorder.Exporters;
using BetterStepsRecorder.UI.Dialogs;

namespace BetterStepsRecorder
{
    public partial class MainForm
    {
        /// <summary>
        /// Gets the default filename for exports based on the current BSR file
        /// </summary>
        /// <returns>The filename without extension</returns>
        private string GetDefaultExportFileName()
        {
            if (Program.zip != null && !string.IsNullOrEmpty(Program.zip.ZipFilePath))
            {
                // Extract the filename without extension
                string fileName = Path.GetFileNameWithoutExtension(Program.zip.ZipFilePath);
                return fileName;
            }
            
            // Default if no file is loaded
            return "步骤记录";
        }

        /// <summary>
        /// Handles the main export menu item click (defaults to RTF export)
        /// </summary>
        private void exportToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Program.SaveRecordEvents();
            // Use the new RtfExporter class through the ExportDialogs helper
            ExportDialogs.HandleRtfExport(GetDefaultExportFileName());
        }

        /// <summary>
        /// Handles export to RTF format
        /// </summary>
        private void exportToRtfToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Program.SaveRecordEvents();
            // Use the new RtfExporter class through the ExportDialogs helper with default filename
            ExportDialogs.HandleRtfExport(GetDefaultExportFileName());
        }

        /// <summary>
        /// Handles export to HTML format
        /// </summary>
        private void exportToHtmlToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Program.SaveRecordEvents();
            // Use the new HtmlExporter class through the ExportDialogs helper with default filename
            ExportDialogs.HandleHtmlExport(GetDefaultExportFileName());
        }

        private void exportToSingleFileHtmlToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Program.SaveRecordEvents();
            ExportDialogs.HandleSingleFileHtmlExport(GetDefaultExportFileName());
        }

        /// <summary>
        /// Handles export to ODT (OpenDocument Text) format
        /// </summary>
        private void exportToOdtToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Program.SaveRecordEvents();
            // Use the new OdtExporter class through the ExportDialogs helper with default filename
            ExportDialogs.HandleOdtExport(GetDefaultExportFileName());
        }

        /// <summary>
        /// Handles export to Obsidian vault format
        /// </summary>
        private void exportToObsidianVaultToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Program.SaveRecordEvents();
            // Use the new ObsidianExporter class through the ExportDialogs helper with default filename
            ExportDialogs.HandleObsidianExport(GetDefaultExportFileName());
        }

        /// <summary>
        /// Handles export to Markdown format
        /// </summary>
        private void exportToMarkdownToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Program.SaveRecordEvents();
            // Use the new MarkdownExporter class through the ExportDialogs helper with default filename
            ExportDialogs.HandleMarkdownExport(GetDefaultExportFileName());
        }

        /// <summary>
        /// 导出为纵向拼接的一张长图
        /// </summary>
        private void exportToLongImageToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Program.SaveRecordEvents();
            using var dlg = new SaveFileDialog
            {
                Title = "导出长图",
                FileName = GetDefaultExportFileName() + ".png",
                Filter = "PNG 图片 (*.png)|*.png|JPEG 图片 (*.jpg)|*.jpg",
                DefaultExt = "png"
            };
            if (dlg.ShowDialog(this) != DialogResult.OK) return;

            new LongImageExporter().Export(dlg.FileName);
        }

        /// <summary>
        /// 导出为按步骤编号的多张图片
        /// </summary>
        private void exportToImageSequenceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Program.SaveRecordEvents();
            using var dlg = new FolderBrowserDialog
            {
                Description = "选择一个空文件夹，用于存放按步骤编号的截图",
                UseDescriptionForTitle = true
            };
            if (dlg.ShowDialog(this) != DialogResult.OK) return;

            new ImageSequenceExporter().Export(dlg.SelectedPath);
        }

        /// <summary>
        /// 导出为 PDF
        /// </summary>
        private void exportToPdfToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Program.SaveRecordEvents();
            using var dlg = new SaveFileDialog
            {
                Title = "导出 PDF",
                FileName = GetDefaultExportFileName() + ".pdf",
                Filter = "PDF 文档 (*.pdf)|*.pdf",
                DefaultExt = "pdf"
            };
            if (dlg.ShowDialog(this) != DialogResult.OK) return;

            new PdfExporter().Export(dlg.FileName);
        }

        /// <summary>
        /// Enables or disables the export menu items based on whether there are items to export
        /// </summary>
        private void EnableDisable_exportToolStripMenuItem()
        {
            if (Listbox_Events.Items.Count > 0)
            {
                exportToolStripMenuItem.Enabled = true;
                toolStripMenuItem1_SaveAs.Enabled = true;
            }
            else
            {
                exportToolStripMenuItem.Enabled = false;
                toolStripMenuItem1_SaveAs.Enabled = false;
            }
        }
    }
}
