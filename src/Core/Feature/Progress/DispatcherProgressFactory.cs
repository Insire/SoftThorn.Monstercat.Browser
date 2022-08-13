using Gress;
using System;
using System.Collections.Generic;
using System.Threading;

namespace SoftThorn.Monstercat.Browser.Core
{
    public sealed class DispatcherProgressFactory<T>
    {
        private readonly SynchronizationContext _synchronizationContext;
        private readonly ProgressContainer<T> _progress;
        private readonly List<Action<T>> _callbacks;

        public DispatcherProgressFactory(SynchronizationContext synchronizationContext, ProgressContainer<T> progress)
        {
            _synchronizationContext = synchronizationContext ?? throw new ArgumentNullException(nameof(synchronizationContext));
            _progress = progress ?? throw new ArgumentNullException(nameof(progress));
            _callbacks = new List<Action<T>>();
        }

        public DispatcherProgress<T> Create()
        {
            return new DispatcherProgress<T>(_synchronizationContext, (p) => Report(p), TimeSpan.FromMilliseconds(250));
        }

        public DispatcherProgress<T> Create(Action<T> callback)
        {
            _callbacks.Add(callback);

            return new DispatcherProgress<T>(_synchronizationContext, (p) => callback.Invoke(p), TimeSpan.FromMilliseconds(250), new CallbackDisposeable(callback, _callbacks));
        }

        private void Report(T value)
        {
            _progress.Report(value);

            var array = _callbacks.ToArray();
            for (var i = 0; i < array.Length; i++)
            {
                var callback = array[i];
                callback.Invoke(value);
            }
        }

        private sealed class CallbackDisposeable : IDisposable
        {
            private readonly Action<T> _action;
            private readonly List<Action<T>> _callbacks;

            public CallbackDisposeable(Action<T> action, List<Action<T>> callbacks)
            {
                _action = action;
                _callbacks = callbacks;
            }

            public void Dispose()
            {
                _callbacks.Remove(_action);
            }
        }
    }
}
