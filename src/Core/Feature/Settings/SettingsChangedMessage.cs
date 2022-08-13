using CommunityToolkit.Mvvm.Messaging.Messages;

namespace SoftThorn.Monstercat.Browser.Core
{
    public sealed class SettingsChangedMessage : ValueChangedMessage<SettingsModel>
    {
        public SettingsChangedMessage(SettingsModel value)
            : base(value)
        {
        }
    }
}
