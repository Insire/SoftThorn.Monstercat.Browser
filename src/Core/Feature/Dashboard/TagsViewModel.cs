using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using DynamicData;
using DynamicData.Binding;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reactive.Linq;
using System.Threading;

namespace SoftThorn.Monstercat.Browser.Core
{
    /// <summary>
    /// top x tags, sorted by newest release
    /// </summary>
    public sealed class TagsViewModel : ObservableRecipient, IDisposable
    {
        private readonly ObservableCollectionExtended<ISeries> _seriesCollection;

        private IDisposable? _subscription;
        private bool _disposedValue;

        public ReadOnlyObservableCollection<ISeries> SeriesCollection { get; }

        public Axis[] XAxes { get; }
        public Axis[] YAxes { get; }

        public TagsViewModel(SynchronizationContext synchronizationContext, ITrackRepository trackRepository, IMessenger messenger)
            : base(messenger)
        {
            _seriesCollection = new ObservableCollectionExtended<ISeries>();
            SeriesCollection = new ReadOnlyObservableCollection<ISeries>(_seriesCollection);

            XAxes = new[]
            {
                new Axis
                {
                    IsVisible = false,
                }
            };

            YAxes = new[]
            {
                new Axis
                {
                    IsVisible = false,
                }
            };

            _subscription = CreateSubscription();

            // messages
            Messenger.Register<TagsViewModel, SettingsChangedMessage>(this, (r, m) =>
            {
                r._subscription?.Dispose();
                r._subscription = CreateSubscription(m.Value.TagsCount);
            });

            IDisposable CreateSubscription(int size = 10)
            {
                return trackRepository
                    .ConnectTags()
                    .Top(SortExpressionComparer<KeyValuePair<string, List<TrackViewModel>>>
                        .Descending(p => p.Value.Count)
                        .ThenByAscending(p => p.Key), size)
                    .Transform(p => (ISeries)new ColumnSeries<int>()
                    {
                        Name = p.Key,
                        Values = new[] { p.Value.Count },
                        Fill = new SolidColorPaint(SKColor.Parse("#FF673AB7")),
                        MaxBarWidth = 24,
                    })
                    .ObserveOn(synchronizationContext)
                    .Bind(_seriesCollection)
                    .Subscribe();
            }
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _subscription?.Dispose();
                    Messenger.UnregisterAll(this);
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
