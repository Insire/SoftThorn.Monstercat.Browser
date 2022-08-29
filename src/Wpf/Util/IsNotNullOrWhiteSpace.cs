using System;
using System.Globalization;
using System.Windows.Data;

namespace SoftThorn.Monstercat.Browser.Wpf
{
    /// <summary>
    /// whether a string is not null or whitespace
    /// </summary>
    [ValueConversion(typeof(string), typeof(bool))]
    public sealed class IsNotNullOrWhiteSpace : ConverterMarkupExtension<IsNotNullOrWhiteSpace>
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string text)
            {
                return !string.IsNullOrWhiteSpace(text);
            }

            return false;
        }
    }
}
