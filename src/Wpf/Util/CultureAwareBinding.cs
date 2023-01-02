using System.Globalization;
using System.Windows.Data;

namespace SoftThorn.Monstercat.Browser.Wpf
{
    public sealed class CultureAwareBinding : Binding
    {
        public CultureAwareBinding()
        {
            ConverterCulture = CultureInfo.CurrentCulture;
        }

        public CultureAwareBinding(string path)
            : base(path)
        {
            ConverterCulture = CultureInfo.CurrentCulture;
        }
    }
}
