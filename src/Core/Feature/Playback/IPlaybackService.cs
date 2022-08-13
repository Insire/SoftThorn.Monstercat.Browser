namespace SoftThorn.Monstercat.Browser.Core
{
    public interface IPlaybackService
    {
        void Play(IPlaybackItem request, PlaybackIntent intent, int volume);

        void Pause(PlaybackIntent intent);

        void Play(PlaybackIntent intent);

        StreamingPlaybackState GetPlaybackState();

        int GetVolume();

        void SetVolume(int volume);

        void Stop(PlaybackIntent intent);
    }
}
