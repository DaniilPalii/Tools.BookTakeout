using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Spectre.Console.Cli;

namespace BookTakeout.ConsoleApplication.DependencyInjection;

public sealed class TypeRegistrar(HostApplicationBuilder hostBuilder) : ITypeRegistrar
{
	public ITypeResolver Build()
	{
		return new TypeResolver(hostBuilder.Build());
	}

	public void Register(Type service, Type implementation)
	{
		hostBuilder.Services.AddSingleton(service, implementation);
	}

	public void RegisterInstance(Type service, object implementation)
	{
		hostBuilder.Services.AddSingleton(service, implementation);
	}

	public void RegisterLazy(Type service, Func<object> factory)
	{
		hostBuilder.Services.AddSingleton(service, _ => factory());
	}

	private sealed class TypeResolver(IHost provider) : ITypeResolver, IDisposable
	{
		public object? Resolve(Type? type)
		{
			return type != null
				? provider.Services.GetService(type)
				: null;
		}

		public void Dispose()
		{
			provider.Dispose();
		}
	}
}
