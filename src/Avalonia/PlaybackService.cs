using SoftThorn.Monstercat.Browser.Core;

namespace SoftThorn.Monstercat.Browser.Avalonia
{
    internal sealed class PlaybackService : IPlaybackService
    {
        public StreamingPlaybackState GetPlaybackState()
        {
            throw new System.NotImplementedException();
        }

        public int GetVolume()
        {
            throw new System.NotImplementedException();
        }

        public void Pause(PlaybackIntent intent)
        {
            throw new System.NotImplementedException();
        }

        public void Play(IPlaybackItem request, PlaybackIntent intent, int volume)
        {
            throw new System.NotImplementedException();
        }

        public void Play(PlaybackIntent intent)
        {
            throw new System.NotImplementedException();
        }

        public void SetVolume(int volume)
        {
            throw new System.NotImplementedException();
        }

        public void Stop(PlaybackIntent intent)
        {
            throw new System.NotImplementedException();
        }
    }
}
