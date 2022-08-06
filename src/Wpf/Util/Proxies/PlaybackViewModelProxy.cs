using SoftThorn.Monstercat.Browser.Core;
using System.Windows;

namespace SoftThorn.Monstercat.Browser.Wpf
{
    public sealed class PlaybackViewModelProxy : BindingProxy<PlaybackViewModel>
    {
        protected override Freezable CreateInstanceCore()
        {
            return new PlaybackViewModelProxy();
        }
    }
}
