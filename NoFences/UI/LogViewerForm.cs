using Fenceless.Model;
using Fenceless.Util;
using System;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace Fenceless.UI
{
    public class LogViewerForm : ThemedForm
    {
        private Button refreshButton;
        private Button clearButton;
        private Button saveButton;
        private CheckBox autoScrollCheckBox;
        private ComboBox logLevelComboBox;
        private TextBox logTextBox;
        private StatusStrip statusStrip;
        private ToolStripStatusLabel statusLabel;

        private readonly string logFilePath;
        private readonly Logger logger;
        private DateTime lastUpdateTime;
        private Timer refreshTimer;
        private long lastReadPosition;
        private string cachedFullContent = "";

        public LogViewerForm()
        {
            logger = Logger.Instance;
            var appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Fenceless");
            logFilePath = Path.Combine(appDataPath, "application.log");

            SetupThemedForm("Fenceless - Log Viewer", showMinimize: true, showMaximize: true, sizable: true);
            this.Size = new Size(1000, 700);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MinimumSize = new Size(800, 500);
            this.ShowInTaskbar = true;

            CreateControls();
            LoadLogContent();
            SetupRefreshTimer();

            this.Shown += (s, e) =>
            {
                if (AppSettings.Instance.EnableAnimations)
                    AnimationHelper.FadeIn(this, 200);
            };
        }

        private void CreateControls()
        {
            this.SuspendLayout();

            var toolbarPanel = new Panel
            {
                Height = 40,
                Dock = DockStyle.Top,
                BackColor = Theme.Colors.BackgroundDark,
                Padding = new Padding(8, 4, 8, 4)
            };

            refreshButton = Theme.CreateFlatButton("Refresh");
            refreshButton.Size = new Size(70, Theme.Sizes.ButtonHeight);
            refreshButton.Location = new Point(8, 6);
            refreshButton.Click += RefreshButton_Click;

            clearButton = Theme.CreateFlatButton("Clear Log");
            clearButton.Size = new Size(80, Theme.Sizes.ButtonHeight);
            clearButton.Location = new Point(86, 6);
            clearButton.Click += ClearButton_Click;

            saveButton = Theme.CreateFlatButton("Save As...");
            saveButton.Size = new Size(80, Theme.Sizes.ButtonHeight);
            saveButton.Location = new Point(174, 6);
            saveButton.Click += SaveButton_Click;

            autoScrollCheckBox = Theme.CreateCheckBox("Auto-scroll");
            autoScrollCheckBox.Checked = true;
            autoScrollCheckBox.Location = new Point(264, 9);

            var logLevelLabel = Theme.CreateLabel("Filter:");
            logLevelLabel.Location = new Point(370, 10);

            logLevelComboBox = Theme.CreateComboBox(new[] { "All", "Debug", "Info", "Warning", "Error", "Critical" });
            logLevelComboBox.Width = 100;
            logLevelComboBox.Location = new Point(410, 6);
            logLevelComboBox.SelectedIndex = 0;
            logLevelComboBox.SelectedIndexChanged += LogLevelComboBox_SelectedIndexChanged;

            toolbarPanel.Controls.AddRange(new Control[] {
                refreshButton, clearButton, saveButton, autoScrollCheckBox, logLevelLabel, logLevelComboBox
            });

            logTextBox = new TextBox
            {
                Multiline = true,
                ScrollBars = ScrollBars.Both,
                ReadOnly = true,
                Dock = DockStyle.Fill,
                Font = Theme.Fonts.Monospace,
                BackColor = Theme.Colors.InputBackground,
                ForeColor = Theme.Colors.InputText,
                BorderStyle = BorderStyle.None
            };

            statusStrip = new StatusStrip
            {
                Dock = DockStyle.Bottom,
                BackColor = Theme.Colors.BackgroundDark,
                ForeColor = Theme.Colors.TextSecondary
            };

            statusLabel = new ToolStripStatusLabel
            {
                Text = "Ready",
                Spring = true,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = Theme.Colors.TextSecondary
            };

            statusStrip.Items.Add(statusLabel);

            this.Controls.Add(logTextBox);
            this.Controls.Add(toolbarPanel);
            this.Controls.Add(statusStrip);

            BringChromeToFront();

            this.ResumeLayout(false);
        }

        private void SetupRefreshTimer()
        {
            refreshTimer = new Timer { Interval = 2000 };
            refreshTimer.Tick += (s, e) =>
            {
                if (autoScrollCheckBox.Checked)
                {
                    RefreshLogContent();
                }
            };
            refreshTimer.Start();
        }

        private void LoadLogContent()
        {
            try
            {
                if (File.Exists(logFilePath))
                {
                    var content = File.ReadAllText(logFilePath);
                    cachedFullContent = content;
                    lastReadPosition = 0;
                    FilterAndDisplayLogs(content);

                    var fileInfo = new FileInfo(logFilePath);
                    lastUpdateTime = fileInfo.LastWriteTime;
                    statusLabel.Text = $"Log loaded - {FormatFileSize(fileInfo.Length)} - Last updated: {lastUpdateTime:HH:mm:ss}";
                }
                else
                {
                    cachedFullContent = "";
                    lastReadPosition = 0;
                    logTextBox.Text = "No log file found. Logs will appear here once the application starts logging.";
                    statusLabel.Text = "No log file found";
                }
            }
            catch (Exception ex)
            {
                logTextBox.Text = $"Error loading log file: {ex.Message}";
                statusLabel.Text = "Error loading log";
            }
        }

        private void RefreshLogContent()
        {
            try
            {
                if (File.Exists(logFilePath))
                {
                    var fileInfo = new FileInfo(logFilePath);
                    if (fileInfo.LastWriteTime > lastUpdateTime)
                    {
                        using (var fs = new FileStream(logFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        {
                            fs.Position = lastReadPosition;
                            using (var sr = new StreamReader(fs))
                            {
                                var newContent = sr.ReadToEnd();
                                lastReadPosition = fs.Position;
                                cachedFullContent += newContent;
                            }
                        }

                        FilterAndDisplayLogs(cachedFullContent);
                        lastUpdateTime = fileInfo.LastWriteTime;

                        if (autoScrollCheckBox.Checked)
                        {
                            logTextBox.SelectionStart = logTextBox.Text.Length;
                            logTextBox.ScrollToCaret();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                statusLabel.Text = $"Error refreshing: {ex.Message}";
            }
        }

        private void FilterAndDisplayLogs(string content)
        {
            var selectedLevel = logLevelComboBox.SelectedItem?.ToString() ?? "All";

            if (selectedLevel == "All")
            {
                logTextBox.Text = content;
                return;
            }

            var lines = content.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
            var filteredLines = new StringBuilder();

            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    filteredLines.AppendLine(line);
                    continue;
                }

                if (line.Contains($"[{selectedLevel.ToUpper().PadRight(8)}]"))
                {
                    filteredLines.AppendLine(line);
                }
            }

            logTextBox.Text = filteredLines.ToString();
        }

        private string FormatFileSize(long bytes)
        {
            if (bytes < 1024) return $"{bytes} B";
            if (bytes < 1024 * 1024) return $"{bytes / 1024:F1} KB";
            if (bytes < 1024 * 1024 * 1024) return $"{bytes / (1024 * 1024):F1} MB";
            return $"{bytes / (1024 * 1024 * 1024):F1} GB";
        }

        private void RefreshButton_Click(object sender, EventArgs e)
        {
            LoadLogContent();
            if (autoScrollCheckBox.Checked)
            {
                logTextBox.SelectionStart = logTextBox.Text.Length;
                logTextBox.ScrollToCaret();
            }
            statusLabel.Text = "Log refreshed manually";
        }

        private void ClearButton_Click(object sender, EventArgs e)
        {
            var result = CustomMessageBox.Show(
                "This will permanently clear the log file. Are you sure?",
                "Clear Log File",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                try
                {
                    File.WriteAllText(logFilePath, string.Empty);
                    logTextBox.Clear();
                    statusLabel.Text = "Log file cleared";
                    logger.Info("Log file cleared by user", "LogViewer");
                }
                catch (Exception ex)
                {
                    CustomMessageBox.Show($"Error clearing log: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            using (var saveDialog = new SaveFileDialog())
            {
                saveDialog.Filter = "Text files (*.txt)|*.txt|Log files (*.log)|*.log|All files (*.*)|*.*";
                saveDialog.DefaultExt = "txt";
                saveDialog.FileName = $"Fenceless_Log_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
                saveDialog.Title = "Save Log File";

                if (saveDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        File.WriteAllText(saveDialog.FileName, logTextBox.Text);
                        statusLabel.Text = $"Log saved to: {Path.GetFileName(saveDialog.FileName)}";
                        logger.Info($"Log exported to: {saveDialog.FileName}", "LogViewer");
                    }
                    catch (Exception ex)
                    {
                        CustomMessageBox.Show($"Error saving log: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void LogLevelComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            FilterAndDisplayLogs(cachedFullContent);
            statusLabel.Text = $"Filtered to show: {logLevelComboBox.SelectedItem}";
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            refreshTimer?.Stop();
            refreshTimer?.Dispose();
            base.OnFormClosing(e);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                refreshTimer?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
