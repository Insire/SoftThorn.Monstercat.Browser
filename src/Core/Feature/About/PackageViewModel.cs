using CommunityToolkit.Mvvm.ComponentModel;

namespace SoftThorn.Monstercat.Browser.Core
{
    public sealed partial class PackageViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _packageName;

        [ObservableProperty]
        private string _packageVersion;

        [ObservableProperty]
        private string _packageUrl;

        [ObservableProperty]
        private string _copyright;

        [ObservableProperty]
        private string[] _authors;

        [ObservableProperty]
        private string _description;

        [ObservableProperty]
        private string _licenseUrl;

        [ObservableProperty]
        private string _licenseType;
    }
}
