using System.Windows;

namespace SoftThorn.Monstercat.Browser.Wpf
{
    public abstract class BindingProxy<T> : Freezable
        where T : class
    {
        /// <summary>Identifies the <see cref="Data"/> dependency property.</summary>
        public static readonly DependencyProperty DataProperty = DependencyProperty.Register(
            nameof(Data),
            typeof(T),
            typeof(BindingProxy<T>),
            new UIPropertyMetadata(default));

        protected abstract override Freezable CreateInstanceCore();

        public T? Data
        {
            get { return (T)GetValue(DataProperty); }
            set { SetValue(DataProperty, value); }
        }
    }
}
