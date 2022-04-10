using CommunityToolkit.Mvvm.ComponentModel;

namespace SoftThorn.Monstercat.Browser.Core
{
    [ObservableObject]
    public sealed partial class TagViewModel
    {
        [ObservableProperty]
        private string _value = default!;

        [ObservableProperty]
        private bool _isSelected;
    }
}
