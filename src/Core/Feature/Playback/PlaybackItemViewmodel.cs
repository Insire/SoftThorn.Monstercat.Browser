using CommunityToolkit.Mvvm.ComponentModel;
using SoftThorn.MonstercatNet;
using System.Diagnostics;

namespace SoftThorn.Monstercat.Browser.Core
{
    [DebuggerDisplay($"{{{nameof(GetDebuggerDisplay)}(),nq}}")]
    public sealed partial class PlaybackItemViewModel : ObservableObject, IPlaybackItem
    {
        public long Sequence { get; }

        public TrackViewModel Track { get; }

        [ObservableProperty]
        private bool _isCurrentlyPlayed;

        public PlaybackItemViewModel(long sequence, TrackViewModel track)
        {
            Sequence = sequence;
            Track = track;
        }

        public TrackStreamRequest GetStreamRequest()
        {
            return new TrackStreamRequest()
            {
                TrackId = Track.Id,
                ReleaseId = Track.Release.Id,
            };
        }

        private string GetDebuggerDisplay()
        {
            return $"[{Sequence}] {Track.Title} by {Track.ArtistsTitle}";
        }
    }
}
