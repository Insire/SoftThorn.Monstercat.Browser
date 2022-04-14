using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using SoftThorn.Monstercat.Browser.Core;

namespace SoftThorn.Monstercat.Browser.Avalonia
{
    public partial class Shell : Window
    {
        private readonly ShellViewModel _shellViewModel;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

        public Shell()
        {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
        }

#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

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
