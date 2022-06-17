using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using SoftThorn.MonstercatNet;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;

namespace SoftThorn.Monstercat.Browser.Core
{
    [ObservableRecipient]
    public sealed partial class LoginViewModel : ObservableValidator
    {
        private readonly SettingsService _settingsService;
        private readonly IMonstercatApi _api;

        [EmailAddress]
        [Display(Name = "E-Mail")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "This field {0} may not be empty.")]
        [MinLength(6)]
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(LoginCommand))]
        private string? _email;

        [Display(Name = "Password")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "This field {0} may not be empty.")]
        [MinLength(12)]
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(LoginCommand))]
        private string? _password;

        public Action? OnLogin { get; set; }

        public LoginViewModel(SettingsService settingsService, IMonstercatApi api, IMessenger messenger)
        {
            _settingsService = settingsService;
            _api = api;

            Messenger = messenger;

            _email = settingsService.Email;
            _password = settingsService.Password;
        }

        [RelayCommand(AllowConcurrentExecutions = false, CanExecute = nameof(CanLogin), IncludeCancelCommand = true)]
        public async Task Login(CancellationToken token)
        {
            if (HasErrors)
            {
                return;
            }

            await _api.Login(new ApiCredentials()
            {
                Email = Email!,
                Password = Password!,
            }, token);

            Messenger.Send(new LoginChangedMessage(this, true));
            OnLogin?.Invoke();
        }

        public async Task TryLogin(Action? handleLoginValidationErrors, CancellationToken token)
        {
            Validate();
            if (HasErrors)
            {
                handleLoginValidationErrors?.Invoke();
            }
            else
            {
                await Login(token);
            }
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
