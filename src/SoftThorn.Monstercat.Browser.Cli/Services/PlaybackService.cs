using SoftThorn.Monstercat.Browser.Core;

namespace SoftThorn.Monstercat.Browser.Cli.Services
{
    internal sealed class PlaybackService : IPlaybackService
    {
        public StreamingPlaybackState GetPlaybackState()
        {
            return StreamingPlaybackState.Stopped;
        }

        public int GetVolume()
        {
            return 0;
        }

        public void Pause(PlaybackIntent intent)
        {
        }

        public void Play(IPlaybackItem request, PlaybackIntent intent, int volume)
        {
        }

        public void Play(PlaybackIntent intent)
        {
        }

        public void SetVolume(int volume)
        {
        }

        public void Stop(PlaybackIntent intent)
        {
        }
    }
}
