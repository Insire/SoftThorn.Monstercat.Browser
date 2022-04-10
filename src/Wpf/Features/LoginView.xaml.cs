using SoftThorn.Monstercat.Browser.Core;
using System;

namespace SoftThorn.Monstercat.Browser.Wpf
{
    public partial class LoginView
    {
        public LoginView(LoginViewModel loginViewModel)
        {
            if (loginViewModel is null)
            {
                throw new ArgumentNullException(nameof(loginViewModel));
            }

            DataContext = loginViewModel;

            InitializeComponent();
        }
    }
}
