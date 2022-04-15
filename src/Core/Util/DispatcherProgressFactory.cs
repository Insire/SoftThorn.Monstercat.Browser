using Gress;
using System;
using System.Threading;

namespace SoftThorn.Monstercat.Browser.Core
{
    public sealed class DispatcherProgressFactory<T>
    {
        private readonly SynchronizationContext _synchronizationContext;
        private readonly ProgressContainer<T> _progress;

        public DispatcherProgressFactory(SynchronizationContext synchronizationContext, ProgressContainer<T> progress)
        {
            _synchronizationContext = synchronizationContext ?? throw new ArgumentNullException(nameof(synchronizationContext));
            _progress = progress ?? throw new ArgumentNullException(nameof(progress));
        }

        public DispatcherProgress<T> Create()
        {
            return new DispatcherProgress<T>(_synchronizationContext, (p) => _progress.Report(p), TimeSpan.FromMilliseconds(250));
        }
    }
}
