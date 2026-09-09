using System.Windows.Forms;
using BetterStepsRecorder.UI;
using BetterStepsRecorder.UI.Dialogs;

namespace BetterStepsRecorder
{
    public partial class MainForm
    {
        /// <summary>
        /// Delete 删除当前选中的指针 / 提示文字框 / 标注。
        /// （Ctrl+Z 撤销已在 MainForm.ProcessCmdKey 中处理）
        /// </summary>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete && _selTarget != HitTarget.None)
            {
                DeleteSelectedAnnotation();
                e.Handled = true;
                e.SuppressKeyPress = true;
                return;
            }

            base.OnKeyDown(e);
        }

        private void openDraftFolderToolStripMenuItem_Click(object sender, System.EventArgs e)
        {
            try
            {
                System.IO.Directory.CreateDirectory(Program.DraftDir);
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = Program.DraftDir,
                    UseShellExecute = true
                });
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(this, $"无法打开草稿目录：{ex.Message}", "错误",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void recordingSettingsToolStripMenuItem_Click(object sender, System.EventArgs e)
        {
            using var dlg = new RecordingSettingsDialog();
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                BSRSettings.Current.Save();
                StatusManager.ShowSuccess("设置已保存");
            }
        }
    }
}
