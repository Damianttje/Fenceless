using System;
using System.Threading;
using System.Threading.Tasks;
using Fenceless.Model;

namespace Fenceless.Util
{
    public class AutoSaveManager : IDisposable
    {
        private readonly Timer autoSaveTimer;
        private int currentIntervalSeconds;
        private int saveInProgress = 0;
        private bool disposed = false;
        private readonly Logger logger = Logger.Instance;
        
        public AutoSaveManager()
        {
            currentIntervalSeconds = AppSettings.Instance.AutoSaveInterval;
            var interval = TimeSpan.FromSeconds(currentIntervalSeconds);
            autoSaveTimer = new Timer(AutoSave, null, interval, interval);
        }
        
        private void AutoSave(object? state)
        {
            if (disposed)
                return;

            RefreshIntervalIfNeeded();

            if (AppSettings.Instance.AutoSave)
            {
                if (Interlocked.Exchange(ref saveInProgress, 1) == 1)
                {
                    logger?.Warning("Skipping autosave because a previous save is still running", "AutoSaveManager");
                    return;
                }

                Task.Run(() =>
                {
                    try
                    {
                        FenceManager.Instance.SaveAllFences();
                        AppSettings.Instance.SaveSettings();
                    }
                    catch (Exception ex)
                    {
                        logger?.Error("Auto-save failed", "AutoSaveManager", ex);
                    }
                    finally
                    {
                        Interlocked.Exchange(ref saveInProgress, 0);
                    }
                });
            }
        }

        private void RefreshIntervalIfNeeded()
        {
            var requestedInterval = Math.Max(5, Math.Min(3600, AppSettings.Instance.AutoSaveInterval));
            if (requestedInterval == currentIntervalSeconds)
                return;

            currentIntervalSeconds = requestedInterval;
            var interval = TimeSpan.FromSeconds(currentIntervalSeconds);
            autoSaveTimer.Change(interval, interval);
            logger?.Info($"Autosave interval changed to {currentIntervalSeconds} seconds", "AutoSaveManager");
        }
        
        public void Dispose()
        {
            if (!disposed)
            {
                autoSaveTimer?.Dispose();
                disposed = true;
            }
        }
    }
}
