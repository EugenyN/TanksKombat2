using System;

namespace TanksKombat2.Client.UI
{
    // Synchronous IProgress<T> implementation: invokes the callback immediately on the calling
    // (main) thread instead of deferring through the SynchronizationContext like Progress<T>.
    public sealed class InlineProgress<T> : IProgress<T>
    {
        private readonly Action<T> _onReport;

        public InlineProgress(Action<T> onReport)
        {
            _onReport = onReport;
        }

        public void Report(T value) => _onReport(value);
    }
}
