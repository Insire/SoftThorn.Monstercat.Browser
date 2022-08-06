using SoftThorn.Monstercat.Browser.Core;
using System.Windows;

namespace SoftThorn.Monstercat.Browser.Wpf
{
    public sealed class ShellViewModelProxy : BindingProxy<ShellViewModel>
    {
        protected override Freezable CreateInstanceCore()
        {
            return new ShellViewModelProxy();
        }
    }
}
