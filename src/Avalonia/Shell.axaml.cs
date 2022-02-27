using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using SoftThorn.Monstercat.Browser.Core;

namespace SoftThorn.Monstercat.Browser.Avalonia
{
    public partial class Shell : Window
    {
        private readonly ShellViewModel _shellViewModel;

        public Shell()
        {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
        }

        public Shell(ShellViewModel shellViewModel)
        {
            DataContext = _shellViewModel = shellViewModel;
            Opened += OnOpened;

            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
        }

        private void OnOpened(object? sender, System.EventArgs e)
        {
            _shellViewModel.RefreshCommand.Execute(null);
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
