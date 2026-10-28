using System;

namespace BUTR.DependencyInjection;

public interface IGenericServiceProviderScope : IDisposable
{
	TService? GetService<TService>() where TService : class;
}
