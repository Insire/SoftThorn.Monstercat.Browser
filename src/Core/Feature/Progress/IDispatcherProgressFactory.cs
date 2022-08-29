using System;

namespace SoftThorn.Monstercat.Browser.Core
{
    public interface IDispatcherProgressFactory<T>
    {
        DispatcherProgress<T> Create();
        DispatcherProgress<T> Create(Action<T> callback);
    }
}