using DryIoc;
using Spectre.Console.Cli;

namespace SoftThorn.Monstercat.Browser.Cli.Services
{
    internal sealed class TypeRegistrar : ITypeRegistrar
    {
        private readonly IContainer _container;

        public TypeRegistrar(IContainer container)
        {
            _container = container;
        }

        public ITypeResolver Build()
        {
            return new TypeResolver(_container);
        }

        public void Register(Type service, Type implementation)
        {
            _container.Register(service, implementation);
        }

        public void RegisterInstance(Type service, object implementation)
        {
            _container.RegisterInstance(service, implementation);
        }

        public void RegisterLazy(Type service, Func<object> factory)
        {
            _container.RegisterDelegate(service, _ => factory());
        }
    }
}
