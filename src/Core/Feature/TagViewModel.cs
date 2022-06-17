using CommunityToolkit.Mvvm.ComponentModel;

namespace SoftThorn.Monstercat.Browser.Core
{
    public sealed partial class TagViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _value = default!;

        [ObservableProperty]
        private bool _isSelected;
    }
}
