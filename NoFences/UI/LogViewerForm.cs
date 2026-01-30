using Fenceless.Util;
using Fenceless.Win32;
using System;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace Fenceless.UI
{
    public partial class LogViewerForm : Form
    {
        private Panel toolbarPanel;
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

        public LogViewerForm()
        {
            logger = Logger.Instance;
            var appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Fenceless");
            logFilePath = Path.Combine(appDataPath, "application.log");
            
            InitializeComponent();
            LoadLogContent();
            SetupRefreshTimer();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            // Form setup with proper controls for dragging and closing
            this.Text = "Fenceless - Log Viewer";
            this.Size = new Size(1000, 700);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MinimumSize = new Size(800, 500);
            this.MaximizeBox = true;
            this.MinimizeBox = true;
            this.ControlBox = true; // Ensure control box is visible
            this.ShowInTaskbar = true; // Show in taskbar for easier access

            // Create toolbar panel with dark styling
            toolbarPanel = new Panel
            {
                Height = 35,
                Dock = DockStyle.Top,
                BackColor = Color.FromArgb(60, 63, 65)
            };

            refreshButton = new Button
            {
                Text = "Refresh",
                Size = new Size(70, 25),
                Location = new Point(5, 5),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(70, 73, 75),
                ForeColor = Color.FromArgb(220, 220, 220)
            };
            refreshButton.FlatAppearance.BorderSize = 1;
            refreshButton.FlatAppearance.BorderColor = Color.FromArgb(100, 100, 100);
            refreshButton.Click += RefreshButton_Click;

            clearButton = new Button
            {
                Text = "Clear Log",
                Size = new Size(80, 25),
                Location = new Point(85, 5),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(70, 73, 75),
                ForeColor = Color.FromArgb(220, 220, 220)
            };
            clearButton.FlatAppearance.BorderSize = 1;
            clearButton.FlatAppearance.BorderColor = Color.FromArgb(100, 100, 100);
            clearButton.Click += ClearButton_Click;

            saveButton = new Button
            {
                Text = "Save As...",
                Size = new Size(80, 25),
                Location = new Point(175, 5),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(70, 73, 75),
                ForeColor = Color.FromArgb(220, 220, 220)
            };
            saveButton.FlatAppearance.BorderSize = 1;
            saveButton.FlatAppearance.BorderColor = Color.FromArgb(100, 100, 100);
            saveButton.Click += SaveButton_Click;

            autoScrollCheckBox = new CheckBox
            {
                Text = "Auto-scroll",
                Checked = true,
                Location = new Point(270, 8),
                AutoSize = true,
                ForeColor = Color.FromArgb(220, 220, 220),
                BackColor = Color.Transparent
            };

            var logLevelLabel = new Label
            {
                Text = "Filter:",
                Location = new Point(380, 8),
                AutoSize = true,
                ForeColor = Color.FromArgb(220, 220, 220),
                BackColor = Color.Transparent
            };

            logLevelComboBox = new ComboBox
            {
                Width = 100,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(420, 5),
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.FromArgb(220, 220, 220),
                BackColor = Color.FromArgb(50, 53, 55)
            };
            logLevelComboBox.Items.AddRange(new[] { "All", "Debug", "Info", "Warning", "Error", "Critical" });
            logLevelComboBox.SelectedIndex = 0;
            logLevelComboBox.SelectedIndexChanged += LogLevelComboBox_SelectedIndexChanged;

            toolbarPanel.Controls.AddRange(new Control[] { 
                refreshButton, clearButton, saveButton, autoScrollCheckBox, logLevelLabel, logLevelComboBox 
            });

            // Create log text box with dark styling
            logTextBox = new TextBox
            {
                Multiline = true,
                ScrollBars = ScrollBars.Both,
                ReadOnly = true,
                Dock = DockStyle.Fill,
                Font = new Font("Consolas", 9, FontStyle.Regular),
                BackColor = Color.FromArgb(43, 43, 43),
                ForeColor = Color.FromArgb(220, 220, 220),
                BorderStyle = BorderStyle.None
            };

            // Create status strip with dark styling
            statusStrip = new StatusStrip
            {
                Dock = DockStyle.Bottom,
                BackColor = Color.FromArgb(60, 63, 65),
                ForeColor = Color.FromArgb(220, 220, 220)
            };

            statusLabel = new ToolStripStatusLabel
            {
                Text = "Ready",
                Spring = true,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = Color.FromArgb(220, 220, 220)
            };

            statusStrip.Items.Add(statusLabel);

            // Add controls to form
            this.Controls.Add(logTextBox);
            this.Controls.Add(toolbarPanel);
            this.Controls.Add(statusStrip);

            this.ResumeLayout(false);
        }

        private void SetupRefreshTimer()
        {
            refreshTimer = new Timer
            {
                Interval = 2000 // Refresh every 2 seconds
            };
            refreshTimer.Tick += (s, e) => {
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
                    FilterAndDisplayLogs(content);
                    
                    var fileInfo = new FileInfo(logFilePath);
                    lastUpdateTime = fileInfo.LastWriteTime;
                    statusLabel.Text = $"Log loaded - {FormatFileSize(fileInfo.Length)} - Last updated: {lastUpdateTime:HH:mm:ss}";
                }
                else
                {
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
                        LoadLogContent();
                        
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

                // Check if line contains the selected log level
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
            var result = MessageBox.Show(
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
                    MessageBox.Show($"Error clearing log: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                        MessageBox.Show($"Error saving log: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void LogLevelComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (File.Exists(logFilePath))
            {
                var content = File.ReadAllText(logFilePath);
                FilterAndDisplayLogs(content);
                statusLabel.Text = $"Filtered to show: {logLevelComboBox.SelectedItem}";
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // Allow normal closing behavior instead of hiding
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