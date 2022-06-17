namespace SoftThorn.Monstercat.Browser.Core
{
    internal sealed class SettingsChangedMessage
    {
        public SettingsModel Settings { get; init; } = default!;
    }
}
