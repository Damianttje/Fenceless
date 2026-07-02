using System;
using System.Threading;
using System.Threading.Tasks;

namespace Fenceless.Util
{
    public class ThrottledExecution : IDisposable
    {
        private TimeSpan delay;
        private DateTime lastExecution = DateTime.Now;
        private TimeSpan TimeSinceLastExecution => DateTime.Now - lastExecution;
        private volatile bool isAwaiting;
        private volatile bool disposed = false;
        private CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

        public ThrottledExecution(TimeSpan delay)
        {
            this.delay = delay;
        }

        public async void Run(Action action)
        {
            if (disposed) return;

            if (TimeSinceLastExecution > delay)
            {
                action.Invoke();
            }
            else if (!isAwaiting)
            {
                isAwaiting = true;
                try
                {
                    while (TimeSinceLastExecution < delay && !disposed)
                    {
                        var delayMs = (int)(delay.TotalMilliseconds - TimeSinceLastExecution.TotalMilliseconds);
                        if (delayMs > 0)
                        {
                            await Task.Delay(delayMs, cancellationTokenSource.Token);
                        }
                        
                        if (!disposed)
                        {
                            action.Invoke();
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    // Expected when disposing
                }
                finally
                {
                    isAwaiting = false;
                }
            }
            lastExecution = DateTime.Now;
        }

        public void Dispose()
        {
            if (!disposed)
            {
                disposed = true;
                cancellationTokenSource?.Cancel();
                cancellationTokenSource?.Dispose();
            }
        }
    }
}
