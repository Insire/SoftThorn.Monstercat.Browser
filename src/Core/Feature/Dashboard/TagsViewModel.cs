using CommunityToolkit.Mvvm.ComponentModel;
using DynamicData;
using DynamicData.Binding;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using SoftThorn.MonstercatNet;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;

namespace SoftThorn.Monstercat.Browser.Core
{
    /// <summary>
    /// top 15 tags, sorted by newest release
    /// </summary>
    public sealed class TagsViewModel : ObservableObject, IDisposable
    {
        private readonly IDisposable _subscription;
        private readonly ObservableCollectionExtended<ISeries> _seriesCollection;

        private bool _disposedValue;

        public ReadOnlyObservableCollection<ISeries> SeriesCollection { get; }

        public Axis[] XAxes { get; }
        public Axis[] YAxes { get; }

        public TagsViewModel(SynchronizationContext synchronizationContext, TrackRepository trackRepository)
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

            _subscription = trackRepository
                .ConnectTags()
                .ObserveOn(TaskPoolScheduler.Default)
                .DistinctUntilChanged()
                .Sort(SortExpressionComparer<KeyValuePair<string, List<Track>>>
                    .Ascending(p => p.Value.Count)
                    .ThenByAscending(p => p.Key))
                .Filter(tag => !tag.Key.Equals("silkinitialbulkimport", StringComparison.InvariantCultureIgnoreCase)
                             && !tag.Key.Equals("nocontentid", StringComparison.InvariantCultureIgnoreCase))
                .LimitSizeTo(15)
                .SortBy(p => p.Value.Count, SortDirection.Descending)
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

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _subscription.Dispose();
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
