using System.Threading.Tasks;

namespace SoftThorn.Monstercat.Browser.Core
{
    public interface IPlaybackService
    {
        Task Play(IPlaybackItem request, int volume);

        void Pause();

        void Play();

        StreamingPlaybackState GetPlaybackState();

        int GetVolume();

        void SetVolume(int volume);
    }
}
