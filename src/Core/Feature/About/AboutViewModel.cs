using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;

namespace SoftThorn.Monstercat.Browser.Core
{
    public sealed class AboutViewModel : ObservableObject
    {
        private readonly ObservableCollection<PackageViewModel> _packages;

        public string AssemblyVersionString { get; }

        public string Copyright { get; }

        public string Product { get; }

        public Version AssemblyVersion { get; }

        public string ProjectUrl { get; }

        public ReadOnlyObservableCollection<PackageViewModel> Packages { get; }

        public AboutViewModel(Assembly assembly)
        {
            _packages = new ObservableCollection<PackageViewModel>();
            Packages = new ReadOnlyObservableCollection<PackageViewModel>(_packages);

            AssemblyVersion = GetVersion(assembly);
            AssemblyVersionString = AssemblyVersion.ToString(3);
            Product = GetProduct(assembly);
            Copyright = GetCopyRight(assembly);
            ProjectUrl = "https://github.com/Insire/SoftThorn.Monstercat.Browser";

            var licenses = assembly.GetManifestResourceNames().Single(p => p.EndsWith("licenses.json"));

            using (var stream = assembly.GetManifestResourceStream(licenses))
            {
                if (stream is null)
                {
                    return;
                }

                using (var reader = new StreamReader(stream))
                {
                    var json = reader.ReadToEnd();
                    if (string.IsNullOrWhiteSpace(json))
                    {
                        return;
                    }

                    var packages = JsonSerializer.Deserialize<PackageViewModel[]>(json);
                    if (packages is null)
                    {
                        return;
                    }

                    foreach (var package in packages)
                    {
                        _packages.Add(package);
                    }
                }
            }
        }

        private static T GetAttribute<T>(Assembly assembly)
          where T : Attribute
        {
            var attributes = assembly.GetCustomAttributes<T>();
            return attributes.First();
        }

        private static Version GetVersion(Assembly assembly)
        {
            var attribute = GetAttribute<AssemblyFileVersionAttribute>(assembly);

            return new Version(attribute.Version);
        }

        private static string GetProduct(Assembly assembly)
        {
            var attribute = GetAttribute<AssemblyProductAttribute>(assembly);
            return attribute.Product;
        }

        private static string GetCopyRight(Assembly assembly)
        {
            var attribute = GetAttribute<AssemblyCopyrightAttribute>(assembly);
            return attribute.Copyright;
        }
    }
}
