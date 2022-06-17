using CommunityToolkit.Mvvm.ComponentModel;

namespace SoftThorn.Monstercat.Browser.Core
{
    public sealed partial class PackageViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _packageName = default!;

        [ObservableProperty]
        private string _packageVersion = default!;

        [ObservableProperty]
        private string _packageUrl = default!;

        [ObservableProperty]
        private string _copyright = default!;

        [ObservableProperty]
        private string[] _authors = default!;

        [ObservableProperty]
        private string _description = default!;

        [ObservableProperty]
        private string _licenseUrl = default!;

        [ObservableProperty]
        private string _licenseType = default!;
    }
}
