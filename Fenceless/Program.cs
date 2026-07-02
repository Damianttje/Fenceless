using Fenceless.Model;
using Fenceless.Win32;
using Fenceless.Util;
using Fenceless.UI;
using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Fenceless
{
    static class Program
    {
        private static Logger logger;
        private static UI.LogViewerForm logViewerForm;
        private static Util.AutoSaveManager autoSaveManager;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            try
            {
                // Initialize logging first
                logger = Logger.Instance;
                
                // Load settings to configure logging properly
                var settings = AppSettings.Instance;
                
                logger.Info("Fenceless application starting...", "Main");

                _ = Task.Run(() => UpdateService.Instance.CheckAndPromptAsync(force: false));

                //allows the context menu to be in dark mode
                //inherits from the system settings
                WindowUtil.SetPreferredAppMode(1);

                using (var mutex = new Mutex(true, "fenceless", out var createdNew))
                {
                    if (createdNew)
                    {
                        logger.Info("Application mutex acquired, starting main application", "Main");
                        
                        Application.EnableVisualStyles();
                        Application.SetCompatibleTextRenderingDefault(false);

                        // Handle application exit
                        Application.ApplicationExit += Application_ApplicationExit;

                        // Handle unhandled exceptions
                        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
                        Application.ThreadException += Application_ThreadException;
                        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

                        var trayIcon = new NotifyIcon();
                        try
                        {
                            using (var ms = new MemoryStream(Properties.Resources.AppIconIco))
                            {
                                trayIcon.Icon = new Icon(ms);
                            }
                            trayIcon.Visible = true;
                            trayIcon.Text = "Fenceless - Desktop organization tool";

                            var contextMenu = new ContextMenuStrip();
                            contextMenu.Renderer = new UI.FluentMenuRenderer();
                            contextMenu.ShowImageMargin = false;

                            // Add Fence menu item with sub menu for fence type
                            var addFenceMenuItem = new ToolStripMenuItem("Add Fence");

                            var normalFenceMenuItem = new ToolStripMenuItem("Standard Fence");
                            normalFenceMenuItem.Click += (s, e) =>
                            {
                                logger.Info("Add Standard Fence requested from tray menu", "Main");
                                FenceManager.Instance.CreateFence("New Fence", FenceType.Standard);
                            };

                            var liveFolderMenuItem = new ToolStripMenuItem("Live Folder");
                            liveFolderMenuItem.Click += (s, e) =>
                            {
                                logger.Info("Add Live Folder Fence requested from tray menu", "Main");
                                FenceManager.Instance.CreateFence("Live Folder", FenceType.LiveFolder);
                            };

                            var runningTasksMenuItem = new ToolStripMenuItem("Running Tasks");
                            runningTasksMenuItem.Click += (s, e) =>
                            {
                                logger.Info("Add Running Tasks Fence requested from tray menu", "Main");
                                FenceManager.Instance.CreateFence("Running Tasks", FenceType.RunningTasks);
                            };

                            var clipboardMenuItem = new ToolStripMenuItem("Clipboard History");
                            clipboardMenuItem.Click += (s, e) =>
                            {
                                logger.Info("Add Clipboard History Fence requested from tray menu", "Main");
                                FenceManager.Instance.CreateFence("Clipboard History", FenceType.ClipboardHistory);
                            };

                            addFenceMenuItem.DropDownItems.Add(normalFenceMenuItem);
                            addFenceMenuItem.DropDownItems.Add(new ToolStripSeparator());
                            addFenceMenuItem.DropDownItems.Add(liveFolderMenuItem);
                            addFenceMenuItem.DropDownItems.Add(runningTasksMenuItem);
                            addFenceMenuItem.DropDownItems.Add(clipboardMenuItem);

                            contextMenu.Items.Add(addFenceMenuItem);

                            // Add Log Viewer menu item
                            var logViewerMenuItem = new ToolStripMenuItem("View Logs");
                            logViewerMenuItem.Click += (s, e) => ShowLogViewer();
                            contextMenu.Items.Add(logViewerMenuItem);
                            
                            // Add Settings menu item
                            var settingsMenuItem = new ToolStripMenuItem("Settings");
                            settingsMenuItem.Click += (s, e) => FenceManager.Instance.ShowGlobalSettings();
                            contextMenu.Items.Add(settingsMenuItem);
                            
                            contextMenu.Items.Add(new ToolStripSeparator());
                            
                            // Add Start with Windows checkbox
                            var startWithWindowsMenuItem = new ToolStripMenuItem("Start with Windows");
                            startWithWindowsMenuItem.CheckOnClick = true;
                            
                            // Sync the setting with actual registry state at startup
                            bool actualStartupState = Util.StartupManager.IsStartupEnabled();
                            var appSettings = AppSettings.Instance;
                            if (appSettings.StartWithWindows != actualStartupState)
                            {
                                logger.Info($"Syncing startup setting - Registry: {actualStartupState}, Settings: {appSettings.StartWithWindows}", "Main");
                                appSettings.StartWithWindows = actualStartupState;
                                appSettings.SaveSettings();
                            }
                            startWithWindowsMenuItem.Checked = actualStartupState;
                            startWithWindowsMenuItem.CheckedChanged += (s, e) =>
                            {
                                logger.Debug($"Start with Windows toggled: {startWithWindowsMenuItem.Checked}", "Main");
                                var appSettings = AppSettings.Instance;
                                appSettings.StartWithWindows = startWithWindowsMenuItem.Checked;

                                if (startWithWindowsMenuItem.Checked)
                                {
                                    if (!Util.StartupManager.EnableStartup())
                                    {
                                        logger.Error("Failed to enable startup", "Main");
                                        MessageBox.Show("Failed to enable startup with Windows. Please check the logs for details.", 
                                            "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                        startWithWindowsMenuItem.Checked = false;
                                        appSettings.StartWithWindows = false;
                                    }
                                    else
                                    {
                                        logger.Info("Startup with Windows enabled", "Main");
                                    }
                                }
                                else
                                {
                                    if (!Util.StartupManager.DisableStartup())
                                    {
                                        logger.Error("Failed to disable startup", "Main");
                                        MessageBox.Show("Failed to disable startup with Windows. Please check the logs for details.", 
                                            "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                        startWithWindowsMenuItem.Checked = true;
                                        appSettings.StartWithWindows = true;
                                    }
                                    else
                                    {
                                        logger.Info("Startup with Windows disabled", "Main");
                                    }
                                }

                                appSettings.SaveSettings();
                            };
                            contextMenu.Items.Add(startWithWindowsMenuItem);
                            

                            contextMenu.Items.Add(new ToolStripSeparator());
                            
                            var exitMenuItem = new ToolStripMenuItem("Exit");
                            exitMenuItem.Click += (s, e) =>
                            {
                                logger.Info("Exit requested from tray menu", "Main");
                                trayIcon.Visible = false;
                                Application.Exit();
                            };
                            contextMenu.Items.Add(exitMenuItem);
                            trayIcon.ContextMenuStrip = contextMenu;

                            trayIcon.DoubleClick += (s, e) =>
                            {
                                logger.Debug("Tray icon double-clicked - opening settings", "Main");
                                FenceManager.Instance.ShowGlobalSettings();
                            };

                            try
                            {
                                logger.Info("Loading fences...", "Main");
                                FenceManager.Instance.LoadFences();
                                if (Application.OpenForms.Count == 0)
                                {
                                    logger.Info("No existing fences found, creating first fence", "Main");
                                    FenceManager.Instance.CreateFence("First fence");
                                }

                                autoSaveManager = new Util.AutoSaveManager();
                                logger.Info("AutoSaveManager started", "Main");
                                
                                logger.Info("Fenceless initialized successfully", "Main");
                                Application.Run();
                            }
                            catch (Exception ex)
                            {
                                logger.Critical("Application error during initialization", "Main", ex);
                                MessageBox.Show($"Application error: {ex.Message}\n\nPlease check the log files for more details.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        }
                        finally
                        {
                            logger.Debug("Disposing tray icon", "Main");
                            trayIcon?.Dispose();
                        }
                    }
                    else
                    {
                        logger.Warning("Another instance of Fenceless is already running", "Main");
                        MessageBox.Show("Fenceless is already running.", "Fenceless", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                if (logger != null)
                    logger.Critical("Critical error in main application", "Main", ex);
                else
                    System.Diagnostics.Debug.WriteLine($"Critical error: {ex}");
                
                MessageBox.Show($"Critical application error: {ex.Message}\n\nPlease check the log files for more details.", "Critical Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
        {
            logger.Error("Unhandled thread exception", "Exception", e.Exception);
            
            var result = MessageBox.Show(
                $"An unexpected error occurred:\n{e.Exception.Message}\n\nWould you like to continue running the application?\n\nCheck the log viewer for more details.",
                "Unexpected Error", MessageBoxButtons.YesNo, MessageBoxIcon.Error);
            
            if (result == DialogResult.No)
            {
                Application.Exit();
            }
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            logger.Critical("Unhandled domain exception", "Exception", e.ExceptionObject as Exception);
            
            if (e.IsTerminating)
            {
                logger.Critical("Application is terminating due to unhandled exception", "Exception");
                MessageBox.Show(
                    $"A critical error occurred and the application must close:\n{e.ExceptionObject}\n\nPlease check the log files for more details.",
                    "Critical Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static void ShowLogViewer()
        {
            try
            {
                logger.Debug("Log viewer requested", "Main");
                if (logViewerForm == null || logViewerForm.IsDisposed)
                {
                    logViewerForm = new UI.LogViewerForm();
                }
                
                if (logViewerForm.Visible)
                {
                    logViewerForm.BringToFront();
                }
                else
                {
                    logViewerForm.Show();
                }
            }
            catch (Exception ex)
            {
                logger.Error("Failed to show log viewer", "Main", ex);
                MessageBox.Show($"Failed to show log viewer: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        private static void Application_ApplicationExit(object sender, EventArgs e)
        {
            try
            {
                logger.Info("Fenceless shutting down...", "Main");
                
                CreateBackup();
                
                FenceManager.Instance.SaveAllFences();
                AppSettings.Instance.SaveSettings();
                autoSaveManager?.Dispose();
                FenceManager.Instance.Dispose();
                
                logger.Info("Fenceless shutdown complete", "Main");

                logger.FlushLogs();
                logger.Dispose();
            }
            catch (Exception ex)
            {
                if (logger != null)
                    logger.Error("Error during application exit", "Main", ex);
                else
                    System.Diagnostics.Debug.WriteLine($"Error during application exit: {ex.Message}");
            }
        }

        private static void CreateBackup()
        {
            try
            {
                var backupDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Fenceless", "backups");

                Directory.CreateDirectory(backupDir);

                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var backupFile = Path.Combine(backupDir, $"backup_{timestamp}.json");
                var json = FenceManager.Instance.ExportAllFences(false);
                File.WriteAllText(backupFile, json);

                var maxBackups = 10;
                var backups = Directory.GetFiles(backupDir, "backup_*.json")
                    .OrderByDescending(f => f)
                    .Skip(maxBackups)
                    .ToList();
                foreach (var old in backups)
                {
                    try { File.Delete(old); } catch { }
                }

                logger.Info($"Created backup: {backupFile}", "Main");
            }
            catch (Exception ex)
            {
                logger.Error("Failed to create backup", "Main", ex);
            }
        }
    }
}
