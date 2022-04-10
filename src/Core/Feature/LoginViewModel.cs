using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Configuration;
using SoftThorn.MonstercatNet;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;

namespace SoftThorn.Monstercat.Browser.Core
{
    public sealed partial class LoginViewModel : ObservableValidator
    {
        private readonly IMonstercatApi _api;
        private readonly ApiCredentials _credentials;

        [EmailAddress]
        [Display(Name = "E-Mail")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "This field {0} may not be empty.")]
        [MinLength(6)]
        [ObservableProperty]
        [AlsoNotifyCanExecuteFor(nameof(LoginCommand))]
        private string _email;

        [Display(Name = "Password")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "This field {0} may not be empty.")]
        [MinLength(12)]
        [ObservableProperty]
        [AlsoNotifyCanExecuteFor(nameof(LoginCommand))]
        private string _password;

        private bool _isLoggedIn;
        public bool IsLoggedIn
        {
            get { return _isLoggedIn; }
            private set { SetProperty(ref _isLoggedIn, value); }
        }

        public Action? OnLogin { get; set; }

        public LoginViewModel(IMonstercatApi api, IConfiguration configuration)
        {
            _api = api;

            _email = "";
            _password = "";

            _credentials = new ApiCredentials();
            var sectionName = typeof(ApiCredentials).Name;
            var section = configuration.GetSection(sectionName);

            section.Bind(_credentials);

            _email = _credentials.Email;
            _password = _credentials.Password;
        }

        [ICommand(AllowConcurrentExecutions = false, CanExecute = nameof(CanLogin), IncludeCancelCommand = true)]
        public async Task Login(CancellationToken token)
        {
            if (HasErrors)
            {
                return;
            }

            await _api.Login(new ApiCredentials()
            {
                Email = _email,
                Password = _password,
            }, token);
            IsLoggedIn = true;
            OnLogin?.Invoke();
        }

        public bool CanLogin()
        {
            ValidateAllProperties();
            return !HasErrors;
        }

        public void Validate()
        {
            ValidateAllProperties();
        }

        public void ClearValidation()
        {
            ClearErrors();
        }
    }
}
