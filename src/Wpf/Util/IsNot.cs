using System;
using System.Globalization;
using System.Windows.Data;

namespace SoftThorn.Monstercat.Browser.Wpf
{
    /// <summary>
    /// negate a boolean value
    /// </summary>
    [ValueConversion(typeof(bool), typeof(bool))]
    public sealed class IsNot : ConverterMarkupExtension<IsNot>
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolean)
            {
                return !boolean;
            }

            return false;
        }
    }
}
