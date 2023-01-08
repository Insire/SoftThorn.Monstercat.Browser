using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace SoftThorn.Monstercat.Browser.Core
{
    public interface IToastService : INotifyPropertyChanged, IDisposable
    {
        ReadOnlyObservableCollection<IToast> Items { get; }

        /// <summary>
        /// how long to wait, before finally removing toasts from the toast collection
        /// </summary>
        TimeSpan ToastCloseDelay { get; }

        /// <summary>
        /// Show a toast notification according to the service configuration
        /// </summary>
        void Show(IToast toast);

        /// <summary>
        /// will close and remove a toast immediately
        /// </summary>
        IAsyncRelayCommand<object?> DismissCommand { get; }
    }
}
