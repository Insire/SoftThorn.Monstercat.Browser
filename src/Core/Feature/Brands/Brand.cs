using System;

namespace SoftThorn.Monstercat.Browser.Core
{
    public abstract class Brand
    {
        protected Brand(string name)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }

        public string Name { get; }
    }
}
