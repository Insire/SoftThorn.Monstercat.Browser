using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using DynamicData;
using DynamicData.Binding;
using LiveChartsCore;
using LiveChartsCore.Kernel;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;

namespace SoftThorn.Monstercat.Browser.Core
{
    /// <summary>
    /// tracks grouped by genre - count Histogram
    /// </summary>
    public sealed class GenresViewModel : ObservableRecipient, IDisposable
    {
        private readonly ColumnSeries<GenreGroupViewModel> _series;
        private readonly ObservableCollectionExtended<GenreGroupViewModel> _entries;
        private readonly ObservableCollectionExtended<ISeries> _seriesCollection;

        private IDisposable? _subscription;
        private bool _disposedValue;

        public ReadOnlyObservableCollection<GenreGroupViewModel> Entries { get; }
        public ReadOnlyObservableCollection<ISeries> SeriesCollection { get; }

        public Axis[] XAxes { get; }
        public Axis[] YAxes { get; }

        public GenresViewModel(IScheduler scheduler, ITrackRepository trackRepository, IMessenger messenger)
            : base(messenger)
        {
            _entries = new ObservableCollectionExtended<GenreGroupViewModel>();
            Entries = new ReadOnlyObservableCollection<GenreGroupViewModel>(_entries);

            _series = new ColumnSeries<GenreGroupViewModel>()
            {
                Name = "Genre",
                Values = _entries,
                Fill = new SolidColorPaint(SKColor.Parse("#FF673AB7")),
                MaxBarWidth = 24,
                TooltipLabelFormatter = point => $"{point.Model?.Name} {point.Model?.Count} Tracks",
                Mapping = (group, point) =>
                {
                    point.PrimaryValue = group.Count;
                    point.SecondaryValue = point.Context.Index;
                }
            };

            _seriesCollection = new ObservableCollectionExtended<ISeries>()
            {
                _series,
            };
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
            Messenger.Register<GenresViewModel, SettingsChangedMessage>(this, (r, m) =>
            {
                r._subscription?.Dispose();
                r._subscription = CreateSubscription(m.Value.GenresCount);
            });

            IDisposable CreateSubscription(int size = 10)
            {
                return trackRepository
                    .ConnectGenres()
                    .Top(SortExpressionComparer<KeyValuePair<string, List<TrackViewModel>>>
                        .Descending(p => p.Value.Count)
                        .ThenByAscending(p => p.Key), size)
                    .Transform(p => new GenreGroupViewModel(p.Value)
                    {
                        Name = p.Key,
                        Count = p.Value.Count,
                    })
                    .ObserveOn(scheduler)
                    .Bind(_entries)
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
