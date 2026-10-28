using System;

namespace BUTR.DependencyInjection;

public interface IGenericServiceProvider : IDisposable
{
	IGenericServiceProviderScope CreateScope();

	TService? GetService<TService>() where TService : class;
}
