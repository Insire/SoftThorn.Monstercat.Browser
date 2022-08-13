using DryIoc;
using Spectre.Console.Cli;

namespace SoftThorn.Monstercat.Browser.Cli.Services
{
    internal sealed class TypeResolver : ITypeResolver
    {
        private readonly IContainer _container;

        public TypeResolver(IContainer container)
        {
            _container = container;
        }

        public object? Resolve(Type? type)
        {
            return _container.Resolve(type);
        }
    }
}
