using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Fenceless.Util
{
    /// <summary>
    /// Centralized error handling and user notification system
    /// </summary>
    public static class ErrorHandler
    {
        private static readonly Logger Logger = Logger.Instance;

        /// <summary>
        /// Handles exceptions with proper logging and user notification
        /// </summary>
        public static void HandleException(Exception ex, string context = null, bool showUserMessage = true)
        {
            if (ex == null) return;

            var contextInfo = string.IsNullOrEmpty(context) ? "Unknown" : context;
            
            // Log the exception
            Logger?.Error($"Exception in {contextInfo}: {ex.Message}", contextInfo, ex);

            // Show user-friendly message if requested
            if (showUserMessage)
            {
                ShowUserFriendlyError(ex, contextInfo);
            }
        }

        /// <summary>
        /// Handles exceptions with custom user message
        /// </summary>
        public static void HandleException(Exception ex, string userMessage, string context = null)
        {
            if (ex == null) return;

            var contextInfo = string.IsNullOrEmpty(context) ? "Unknown" : context;
            
            // Log the exception
            Logger?.Error($"Exception in {contextInfo}: {ex.Message}", contextInfo, ex);

            // Show custom user message
            ShowUserMessage(userMessage, MessageBoxIcon.Error);
        }

        /// <summary>
        /// Shows a user-friendly error message based on exception type
        /// </summary>
        private static void ShowUserFriendlyError(Exception ex, string context)
        {
            var message = GetUserFriendlyMessage(ex, context);
            var icon = GetMessageBoxIcon(ex);
            
            ShowUserMessage(message, icon);
        }

        /// <summary>
        /// Converts technical exceptions to user-friendly messages
        /// </summary>
        private static string GetUserFriendlyMessage(Exception ex, string context)
        {
            switch (ex)
            {
                case UnauthorizedAccessException:
                    return "Access denied. Please check your file permissions and try again.";
                
                case FileNotFoundException:
                    return "The requested file or folder could not be found. It may have been moved or deleted.";
                
                case DirectoryNotFoundException:
                    return "The requested directory could not be found. It may have been moved or deleted.";
                
                case IOException ioEx when ioEx.Message.Contains("being used"):
                    return "The file is currently in use by another program. Please close the file and try again.";
                
                case OutOfMemoryException:
                    return "The system is running low on memory. Please close some applications and try again.";
                
                case System.ComponentModel.Win32Exception winEx:
                    return $"A Windows system error occurred: {winEx.Message}";
                
                case ArgumentException:
                    return "Invalid input provided. Please check your input and try again.";
                
                case InvalidOperationException:
                    return "This operation is not valid in the current state. Please try again.";
                
                case TaskCanceledException:
                    return "The operation was cancelled.";
                
                case TimeoutException:
                    return "The operation timed out. Please try again.";
                
                default:
                    return $"An unexpected error occurred in {context}:\n\n{ex.Message}\n\nPlease check the log files for more details.";
            }
        }

        /// <summary>
        /// Determines appropriate message box icon based on exception type
        /// </summary>
        private static MessageBoxIcon GetMessageBoxIcon(Exception ex)
        {
            switch (ex)
            {
                case UnauthorizedAccessException:
                case System.Security.SecurityException:
                    return MessageBoxIcon.Warning;
                
                case FileNotFoundException:
                case DirectoryNotFoundException:
                    return MessageBoxIcon.Information;
                
                case OutOfMemoryException:
                    return MessageBoxIcon.Stop;
                
                default:
                    return MessageBoxIcon.Error;
            }
        }

        /// <summary>
        /// Shows a message box safely (thread-safe)
        /// </summary>
        private static void ShowUserMessage(string message, MessageBoxIcon icon)
        {
            try
            {
                if (System.Windows.Forms.Application.MessageLoop)
                {
                    MessageBox.Show(message, "Fenceless", MessageBoxButtons.OK, icon);
                }
                else
                {
                    var result = DialogResult.None;
                    System.Threading.ThreadStart threadStart = () => {
                        result = MessageBox.Show(message, "Fenceless", MessageBoxButtons.OK, icon);
                    };
                    var thread = new System.Threading.Thread(threadStart);
                    thread.SetApartmentState(System.Threading.ApartmentState.STA);
                    thread.Start();
                    thread.Join();
                }
            }
            catch (Exception ex)
            {
                Logger?.Error($"Failed to show user message: {ex.Message}", "ErrorHandler");
                Debug.WriteLine($"Error: {message}");
            }
        }

        /// <summary>
        /// Logs and handles critical errors that may require application restart
        /// </summary>
        public static void HandleCriticalError(Exception ex, string context = null)
        {
            HandleException(ex, context, false);
            
            var message = $"A critical error has occurred:\n\n{ex.Message}\n\n" +
                         "The application may need to be restarted.\n\n" +
                         "Please check the log files for more details.";
            
            var result = ShowUserMessage(message, MessageBoxIcon.Stop, MessageBoxButtons.OKCancel);
            if (result == DialogResult.OK)
            {
                Application.Restart();
            }
        }

        /// <summary>
        /// Shows message box with custom buttons
        /// </summary>
        private static DialogResult ShowUserMessage(string message, MessageBoxIcon icon, MessageBoxButtons buttons)
        {
            try
            {
                if (System.Windows.Forms.Application.MessageLoop)
                {
                    return MessageBox.Show(message, "Fenceless", buttons, icon);
                }
                else
                {
                    DialogResult result = DialogResult.None;
                    System.Threading.ThreadStart threadStart = () => {
                        result = MessageBox.Show(message, "Fenceless", buttons, icon);
                    };
                    var thread = new System.Threading.Thread(threadStart);
                    thread.SetApartmentState(System.Threading.ApartmentState.STA);
                    thread.Start();
                    thread.Join();
                    return result;
                }
            }
            catch (Exception ex)
            {
                Logger?.Error($"Failed to show user message: {ex.Message}", "ErrorHandler");
                Debug.WriteLine($"Error: {message}");
                return DialogResult.Cancel;
            }
        }

        /// <summary>
        /// Handles exceptions with retry logic
        /// </summary>
        public static bool HandleWithRetry(Action action, int maxRetries = 3, string context = null)
        {
            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    action();
                    return true;
                }
                catch (Exception ex)
                {
                    var contextInfo = string.IsNullOrEmpty(context) ? "Unknown" : context;
                    Logger?.Warning($"Attempt {attempt} failed in {contextInfo}: {ex.Message}", contextInfo);
                    
                    if (attempt == maxRetries)
                    {
                        HandleException(ex, contextInfo, true);
                        return false;
                    }
                    
                    // Wait before retry
                    System.Threading.Thread.Sleep(1000 * attempt);
                }
            }
            
            return false;
        }

        /// <summary>
        /// Handles exceptions with retry logic for async operations
        /// </summary>
        public static async Task<bool> HandleWithRetryAsync(Func<Task> action, int maxRetries = 3, string context = null)
        {
            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    await action();
                    return true;
                }
                catch (Exception ex)
                {
                    var contextInfo = string.IsNullOrEmpty(context) ? "Unknown" : context;
                    Logger?.Warning($"Attempt {attempt} failed in {contextInfo}: {ex.Message}", contextInfo);
                    
                    if (attempt == maxRetries)
                    {
                        HandleException(ex, contextInfo, true);
                        return false;
                    }
                    
                    // Wait before retry
                    await Task.Delay(1000 * attempt);
                }
            }
            
            return false;
        }
    }
}