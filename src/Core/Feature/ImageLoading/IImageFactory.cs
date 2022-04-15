using System;
using System.IO;

namespace SoftThorn.Monstercat.Browser.Core
{
    public interface IImageFactory<T>
    {
        T From(Stream stream);

        T From(Uri uri);

        T From(string uri);
    }
}
