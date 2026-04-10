using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Fenceless.Util
{
    public enum LogLevel
    {
        Debug,
        Info,
        Warning,
        Error,
        Critical
    }

    public class Logger : IDisposable
    {
        private static readonly Lazy<Logger> _instance = new Lazy<Logger>(() => new Logger());
        public static Logger Instance => _instance.Value;

        private readonly ConcurrentQueue<LogEntry> _logQueue = new ConcurrentQueue<LogEntry>();
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private readonly Task _logWriterTask;
        private readonly string _logFilePath;
        private readonly object _disposeLock = new object();
        private bool _disposed = false;
        private StreamWriter _streamWriter;
        private long _entriesSinceSizeCheck;
        private const long MaxLogFileSize = 10 * 1024 * 1024;
        private const long SizeCheckInterval = 100;

        public LogLevel MinimumLogLevel { get; set; } = LogLevel.Debug;
        public bool EnableFileOutput { get; set; } = true;

        private Logger()
        {
            var appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Fenceless");
            Directory.CreateDirectory(appDataPath);
            _logFilePath = Path.Combine(appDataPath, "application.log");

            _logWriterTask = Task.Run(ProcessLogQueue, _cancellationTokenSource.Token);

            Info("Logger initialized", "Logger");
        }

        public void Debug(string message, string category = null)
        {
            Log(LogLevel.Debug, message, category);
        }

        public void Info(string message, string category = null)
        {
            Log(LogLevel.Info, message, category);
        }

        public void Warning(string message, string category = null)
        {
            Log(LogLevel.Warning, message, category);
        }

        public void Error(string message, string category = null, Exception exception = null)
        {
            var fullMessage = exception != null ? $"{message}\nException: {exception}" : message;
            Log(LogLevel.Error, fullMessage, category);
        }

        public void Critical(string message, string category = null, Exception exception = null)
        {
            var fullMessage = exception != null ? $"{message}\nException: {exception}" : message;
            Log(LogLevel.Critical, fullMessage, category);
        }

        private void Log(LogLevel level, string message, string category)
        {
            if (level < MinimumLogLevel) return;

            var logEntry = new LogEntry
            {
                Timestamp = DateTime.Now,
                Level = level,
                Category = category ?? "General",
                Message = message,
                ThreadId = Thread.CurrentThread.ManagedThreadId
            };

            _logQueue.Enqueue(logEntry);
        }

        private async Task ProcessLogQueue()
        {
            while (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    if (_logQueue.TryDequeue(out var logEntry))
                    {
                        WriteLogEntry(logEntry);
                    }
                    else
                    {
                        FlushWriter();
                        await Task.Delay(50, _cancellationTokenSource.Token);
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    try
                    {
                        System.Diagnostics.Debug.WriteLine($"Logger error: {ex.Message}");
                    }
                    catch { }
                }
            }

            while (_logQueue.TryDequeue(out var logEntry))
            {
                try
                {
                    WriteLogEntry(logEntry);
                }
                catch
                {
                }
            }

            FlushWriter();
        }

        private void WriteLogEntry(LogEntry logEntry)
        {
            if (!EnableFileOutput) return;

            try
            {
                if (_streamWriter == null)
                {
                    _streamWriter = new StreamWriter(_logFilePath, true, System.Text.Encoding.UTF8, 4096) { AutoFlush = false };
                }

                var formattedMessage = FormatLogEntry(logEntry);
                _streamWriter.WriteLine(formattedMessage);

                _entriesSinceSizeCheck++;
                if (_entriesSinceSizeCheck >= SizeCheckInterval)
                {
                    _entriesSinceSizeCheck = 0;
                    _streamWriter.Flush();
                    CheckLogFileSize();
                }
            }
            catch
            {
            }
        }

        private void CheckLogFileSize()
        {
            try
            {
                if (_streamWriter != null && _streamWriter.BaseStream.Length > MaxLogFileSize)
                {
                    RotateLogFile();
                }
            }
            catch
            {
            }
        }

        private void FlushWriter()
        {
            try
            {
                _streamWriter?.Flush();
            }
            catch
            {
            }
        }

        private void RotateLogFile()
        {
            try
            {
                _streamWriter?.Flush();
                _streamWriter?.Dispose();
                _streamWriter = null;

                var backupPath = _logFilePath + ".old";
                if (File.Exists(backupPath))
                {
                    File.Delete(backupPath);
                }
                File.Move(_logFilePath, backupPath);
            }
            catch
            {
            }
        }

        private string FormatLogEntry(LogEntry logEntry)
        {
            return $"[{logEntry.Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{logEntry.Level.ToString().ToUpper().PadRight(8)}] [{logEntry.Category.PadRight(12)}] [T{logEntry.ThreadId:D2}] {logEntry.Message}";
        }

        public void FlushLogs()
        {
            var timeout = DateTime.Now.AddSeconds(5);
            while (!_logQueue.IsEmpty && DateTime.Now < timeout)
            {
                Thread.Sleep(10);
            }
            FlushWriter();
        }

        public void Dispose()
        {
            lock (_disposeLock)
            {
                if (_disposed) return;
                _disposed = true;

                Info("Logger shutting down", "Logger");
                
                _cancellationTokenSource?.Cancel();
                
                try
                {
                    _logWriterTask?.Wait(TimeSpan.FromSeconds(2));
                }
                catch
                {
                }

                try
                {
                    _streamWriter?.Flush();
                    _streamWriter?.Dispose();
                    _streamWriter = null;
                }
                catch
                {
                }
                
                _cancellationTokenSource?.Dispose();
            }
        }

        private class LogEntry
        {
            public DateTime Timestamp { get; set; }
            public LogLevel Level { get; set; }
            public string Category { get; set; }
            public string Message { get; set; }
            public int ThreadId { get; set; }
        }
    }
}
