using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DynamicData;
using DynamicData.Binding;
using SoftThorn.Monstercat.Browser.Core;
using System;
using System.Collections.ObjectModel;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace SoftThorn.Monstercat.Browser.Wpf
{
    public sealed partial class ToastService : ObservableObject, IToastService
    {
        private readonly SourceList<IToast> _cache;
        private readonly ObservableCollectionExtended<IToast> _items;

        private readonly IDisposable _itemsSubscription;
        private readonly IDisposable _countSubscription;
        private readonly IDisposable _removeSubscription;
        private readonly IScheduler _scheduler;

        private Window? _toastHost;
        private bool _disposedValue;

        /// <inheritdoc/>
        public string WindowStyleKey { get; }

        /// <inheritdoc/>
        public int WindowOffset { get; }

        /// <inheritdoc/>
        public TimeSpan WindowCloseDelay { get; }

        /// <inheritdoc/>
        public TimeSpan ToastCloseDelay { get; }

        /// <inheritdoc/>
        public TimeSpan ToastVisibleFor { get; }

        /// <inheritdoc/>
        public Rect? Origin { get; set; }

        /// <inheritdoc/>
        public ReadOnlyObservableCollection<IToast> Items { get; }

        public ToastService(ToastServiceConfiguration configuration, IScheduler scheduler)
        {
            if (configuration is null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            _cache = new SourceList<IToast>();
            _items = new ObservableCollectionExtended<IToast>();
            Items = new ReadOnlyObservableCollection<IToast>(_items);

            WindowStyleKey = configuration.WindowStyleKey;
            WindowOffset = configuration.WindowOffset;

            WindowCloseDelay = configuration.WindowCloseDelay;
            ToastCloseDelay = configuration.ToastCloseDelay;
            ToastVisibleFor = configuration.ToastVisibleFor;

            _itemsSubscription = _cache
                .Connect()
                .ObserveOn(TaskPoolScheduler.Default)
                .DistinctUntilChanged()
                .ObserveOn(scheduler)
                .Bind(_items)
                .DisposeMany()
                .Subscribe();

            _countSubscription = _cache.CountChanged
                .ObserveOn(TaskPoolScheduler.Default)
                .DistinctUntilChanged()
                .ObserveOn(scheduler)
                .Do(p =>
                {
                    if (p == 0)
                    {
                        var host = _toastHost;
                        if (host is null)
                        {
                            return;
                        }

                        host.Visibility = Visibility.Hidden;
                    }
                })
                .ObserveOn(TaskPoolScheduler.Default)
                .Delay(WindowCloseDelay)
                .ObserveOn(scheduler)
                .Subscribe(_ =>
                {
                    if (_cache.Count == 0)
                    {
                        var host = _toastHost;
                        _toastHost = null;

                        if (host is null)
                        {
                            return;
                        }

                        host.Close();
                    }
                });

            _removeSubscription = _cache
                .Connect()
                .ObserveOn(TaskPoolScheduler.Default)
                .DistinctUntilChanged()
                .Filter(t => !t.IsPersistent)
                .Delay(ToastVisibleFor)
                .Subscribe(async changes =>
                {
                    foreach (var change in changes)
                    {
                        switch (change.Reason)
                        {
                            case ListChangeReason.Add:
                            case ListChangeReason.AddRange:
                                var toast = change.Item.Current;
                                await CloseToast(toast).ConfigureAwait(false);

                                break;

                            case ListChangeReason.Replace:
                            case ListChangeReason.Remove:
                            case ListChangeReason.RemoveRange:
                            case ListChangeReason.Clear:
                            case ListChangeReason.Refresh:
                            case ListChangeReason.Moved:
                                break;
                        }
                    }
                });
            _scheduler = scheduler;
        }

        [RelayCommand(AllowConcurrentExecutions = false, CanExecute = nameof(CanDismiss))]
        private Task Dismiss(object? args)
        {
            if (args is IToast toast)
            {
                return CloseToast(toast);
            }

            return Task.CompletedTask;
        }

        private static bool CanDismiss(object? args)
        {
            return args is IToast;
        }

        private async Task CloseToast(IToast toast)
        {
            toast.IsRemoving = true;

            if (!toast.IsPersistent)
            {
                await Task.Delay(ToastCloseDelay).ConfigureAwait(false); // we need to wait a bit, since i'm not aware of a good way to animate removal
            }

            _cache.Remove(toast);
        }

        /// <inheritdoc/>
        public void Show(IToast toast)
        {
            _scheduler.Schedule(() =>
            {
                SetupHost(this);

                _cache.Add(toast);
            });
        }

        private static void SetupHost(ToastService context)
        {
            // setup host window, if its missing
            if (context._toastHost is null)
            {
                context._toastHost = new Window()
                {
                    DataContext = context,
                };

                var obj = Application.Current.FindResource(context.WindowStyleKey);
                if (obj is Style style && style.TargetType == typeof(Window))
                {
                    context._toastHost.Style = style;
                }

                context._toastHost.Closing += OnHostClosing;
                context._toastHost.Loaded += OnLoaded;
                context._toastHost.Show();
            }

            Reposition(context._toastHost, context.Origin, context.WindowOffset);

            if (!context._toastHost.IsVisible)
            {
                context._toastHost.Visibility = Visibility.Visible;
            }

            void OnLoaded(object sender, RoutedEventArgs e)
            {
                Reposition(context._toastHost, context.Origin, context.WindowOffset);
            }

            void OnHostClosing(object? sender, EventArgs e)
            {
                context._toastHost = null;

                if (sender is Window host)
                {
                    host.Closing -= OnHostClosing;
                }
            }
        }

        private static void Reposition(Window window, Rect? origin, int offset)
        {
            var area = origin ?? SystemParameters.WorkArea;

            // Display the toast at the top right of the area.
            window.Left = area.Right - window.Width - offset;
            window.Top = area.Top + offset;

            // set Height to screen height, so that animations are always smooth
            window.MinHeight = area.Height - (2 * offset);
            window.Height = window.MinHeight;
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _itemsSubscription.Dispose();
                    _countSubscription.Dispose();
                    _removeSubscription.Dispose();
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
