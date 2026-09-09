using System;
using System.IO;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using BetterStepsRecorder.Exporters;
using BetterStepsRecorder.UI.Dialogs.Obsidian;

namespace BetterStepsRecorder.UI.Dialogs
{
    /// <summary>
    /// Provides common dialog functionality for all exporters
    /// </summary>
    public static class ExportDialogs
    {
        // Import required Windows API functions for folder dialog customization
        [DllImport("user32.dll")]
        private static extern IntPtr GetParent(IntPtr hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, UInt32 Msg, IntPtr wParam, IntPtr lParam);

        /// <summary>
        /// Shows a save file dialog for RTF export
        /// </summary>
        /// <param name="defaultFileName">The default filename to use (without extension)</param>
        /// <returns>The selected file path, or null if canceled</returns>
        public static string ShowRtfSaveDialog(string defaultFileName = "步骤记录")
        {
            using (SaveFileDialog saveDialog = new SaveFileDialog())
            {
                saveDialog.Filter = "RTF 文档 (*.rtf)|*.rtf";
                saveDialog.Title = "导出 RTF 文档";
                saveDialog.DefaultExt = "rtf";
                saveDialog.FileName = $"{defaultFileName}.rtf";

                if (saveDialog.ShowDialog() == DialogResult.OK)
                {
                    return saveDialog.FileName;
                }
                return null;
            }
        }

        /// <summary>
        /// Shows a save file dialog for HTML export
        /// </summary>
        /// <param name="defaultFileName">The default filename to use (without extension)</param>
        /// <returns>The selected file path, or null if canceled</returns>
        public static string ShowHtmlSaveDialog(string defaultFileName = "步骤记录")
        {
            using (SaveFileDialog saveDialog = new SaveFileDialog())
            {
                saveDialog.Filter = "HTML 文件 (*.html)|*.html";
                saveDialog.Title = "导出 HTML";
                saveDialog.DefaultExt = "html";
                saveDialog.FileName = $"{defaultFileName}.html";

                if (saveDialog.ShowDialog() == DialogResult.OK)
                {
                    return saveDialog.FileName;
                }
                return null;
            }
        }

        /// <summary>
        /// Shows a save file dialog for Markdown export
        /// </summary>
        /// <param name="defaultFileName">The default filename to use (without extension)</param>
        /// <returns>The selected file path, or null if canceled</returns>
        public static string ShowMarkdownSaveDialog(string defaultFileName = "步骤记录")
        {
            using (SaveFileDialog saveDialog = new SaveFileDialog())
            {
                saveDialog.Filter = "Markdown 文件 (*.md)|*.md";
                saveDialog.Title = "导出 Markdown";
                saveDialog.DefaultExt = "md";
                saveDialog.FileName = $"{defaultFileName}.md";

                if (saveDialog.ShowDialog() == DialogResult.OK)
                {
                    return saveDialog.FileName;
                }
                return null;
            }
        }

        /// <summary>
        /// Shows a save file dialog for ODT export
        /// </summary>
        /// <param name="defaultFileName">The default filename to use (without extension)</param>
        /// <returns>The selected file path, or null if canceled</returns>
        public static string ShowOdtSaveDialog(string defaultFileName = "步骤记录")
        {
            using (SaveFileDialog saveDialog = new SaveFileDialog())
            {
                saveDialog.Filter = "ODT 文档 (*.odt)|*.odt";
                saveDialog.Title = "导出 ODT 文档";
                saveDialog.DefaultExt = "odt";
                saveDialog.FileName = $"{defaultFileName}.odt";

                if (saveDialog.ShowDialog() == DialogResult.OK)
                {
                    return saveDialog.FileName;
                }
                return null;
            }
        }

        /// <summary>
        /// Shows a dialog to select an Obsidian vault folder
        /// </summary>
        /// <returns>The selected vault path, or null if canceled</returns>
        public static string SelectObsidianVault()
        {
            using (FolderBrowserDialog folderDialog = new FolderBrowserDialog())
            {
                folderDialog.Description = "Select Obsidian Vault Folder";
                folderDialog.UseDescriptionForTitle = true;
                folderDialog.ShowNewFolderButton = false;

                if (folderDialog.ShowDialog() == DialogResult.OK)
                {
                    string vaultPath = folderDialog.SelectedPath;
                    
                    // Validate that this is an Obsidian vault
                    if (!Directory.Exists(Path.Combine(vaultPath, ".obsidian")))
                    {
                        MessageBox.Show("The selected folder is not a valid Obsidian vault. Please select a folder containing a .obsidian directory.", 
                            "Invalid Obsidian Vault", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return null;
                    }
                    
                    return vaultPath;
                }
                return null;
            }
        }

        /// <summary>
        /// Shows a dialog to select a subfolder within the Obsidian vault
        /// This restricts browsing to within the vault only
        /// </summary>
        /// <param name="vaultPath">The root path of the Obsidian vault</param>
        /// <returns>The selected subfolder path relative to the vault, or empty string if root selected</returns>
        public static string SelectSubfolder(string vaultPath)
        {
            // Create a custom folder browser that restricts navigation to within the vault
            using (ObsidianExporterRestrictedFolderBrowser restrictedBrowser = new ObsidianExporterRestrictedFolderBrowser(vaultPath))
            {
                if (restrictedBrowser.ShowDialog() == DialogResult.OK)
                {
                    // Return the path relative to the vault
                    string selectedPath = restrictedBrowser.SelectedPath;
                    if (selectedPath.StartsWith(vaultPath))
                    {
                        string relativePath = selectedPath.Substring(vaultPath.Length).TrimStart(Path.DirectorySeparatorChar);
                        return relativePath;
                    }
                }
                return "";
            }
        }

        /// <summary>
        /// Prompts the user for a file name
        /// </summary>
        /// <param name="defaultName">The default file name to display</param>
        /// <returns>The file name entered by the user, or null if canceled</returns>
        public static string PromptForFileName(string defaultName = "步骤记录")
        {
            return FileNamePrompt.PromptForFileName(defaultName);
        }

        /// <summary>
        /// Handles the complete Obsidian export process including all dialogs
        /// </summary>
        /// <param name="defaultFileName">The default filename to use (without extension)</param>
        /// <returns>True if export was successful, false otherwise</returns>
        public static bool HandleObsidianExport(string defaultFileName = "步骤记录")
        {
            // Select Obsidian vault
            string vaultPath = SelectObsidianVault();
            if (string.IsNullOrEmpty(vaultPath))
                return false;

            // Select subfolder (optional)
            string subfolderPath = SelectSubfolder(vaultPath);
            
            // Prompt for file name
            string fileName = PromptForFileName(defaultFileName);
            if (string.IsNullOrEmpty(fileName))
                return false;

            // Perform the export
            ObsidianExporter exporter = new ObsidianExporter();
            return exporter.ExportToObsidianVault(vaultPath, fileName, subfolderPath);
        }

        /// <summary>
        /// Handles the complete HTML export process including all dialogs
        /// </summary>
        /// <param name="defaultFileName">The default filename to use (without extension)</param>
        /// <returns>True if export was successful, false otherwise</returns>
        public static bool HandleHtmlExport(string defaultFileName = "步骤记录")
        {
            string filePath = ShowHtmlSaveDialog(defaultFileName);
            if (string.IsNullOrEmpty(filePath))
                return false;

            HtmlExporter exporter = new HtmlExporter();
            bool success = exporter.Export(filePath);
            if (success)
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(filePath) { UseShellExecute = true });
            }
            return success;
        }

        /// <summary>
        /// Handles the complete RTF export process including all dialogs
        /// </summary>
        /// <param name="defaultFileName">The default filename to use (without extension)</param>
        /// <returns>True if export was successful, false otherwise</returns>
        public static bool HandleRtfExport(string defaultFileName = "步骤记录")
        {
            string filePath = ShowRtfSaveDialog(defaultFileName);
            if (string.IsNullOrEmpty(filePath))
                return false;

            RtfExporter exporter = new RtfExporter();
            return exporter.Export(filePath);
        }

        /// <summary>
        /// Handles the complete ODT export process including all dialogs
        /// </summary>
        /// <param name="defaultFileName">The default filename to use (without extension)</param>
        /// <returns>True if export was successful, false otherwise</returns>
        public static bool HandleOdtExport(string defaultFileName = "步骤记录")
        {
            string filePath = ShowOdtSaveDialog(defaultFileName);
            if (string.IsNullOrEmpty(filePath))
                return false;

            OdtExporter exporter = new OdtExporter();
            return exporter.Export(filePath);
        }

        /// <summary>
        /// Handles the complete Markdown export process including all dialogs
        /// </summary>
        /// <param name="defaultFileName">The default filename to use (without extension)</param>
        /// <returns>True if export was successful, false otherwise</returns>
        public static bool HandleMarkdownExport(string defaultFileName = "步骤记录")
        {
            string filePath = ShowMarkdownSaveDialog(defaultFileName);
            if (string.IsNullOrEmpty(filePath))
                return false;

            MarkdownExporter exporter = new MarkdownExporter();
            bool success = exporter.Export(filePath);
            if (success)
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(filePath) { UseShellExecute = true });
            }
            return success;
        }
    }
}