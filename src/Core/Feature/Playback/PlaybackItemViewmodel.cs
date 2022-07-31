using CommunityToolkit.Mvvm.ComponentModel;
using SoftThorn.MonstercatNet;

namespace SoftThorn.Monstercat.Browser.Core
{
    public sealed class PlaybackItemViewModel : ObservableObject, IPlaybackItem
    {
        public long Sequence { get; }

        public TrackViewModel Track { get; }

        private bool _playbackComnpleted;
        public bool PlaybackCompleted
        {
            get { return _playbackComnpleted; }
            private set { SetProperty(ref _playbackComnpleted, value); }
        }

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

        public void Dispose()
        {
            PlaybackCompleted = true;
        }
    }
}
