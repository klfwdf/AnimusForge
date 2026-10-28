using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using BUTR.DependencyInjection;

namespace MCM.LightInject;

internal class LightInjectGenericServiceProvider : IGenericServiceProvider, IDisposable
{
	private class ReferenceEqualityComparer : IEqualityComparer<object>
	{
		bool IEqualityComparer<object>.Equals(object x, object y)
		{
			return x == y;
		}

		int IEqualityComparer<object>.GetHashCode(object obj)
		{
			return obj.GetHashCode();
		}
	}

	private delegate TService OfTypeDelegate<TService>(IEnumerable<object> enumerable);

	private static readonly ReferenceEqualityComparer Comparer = new ReferenceEqualityComparer();

	private static readonly ConcurrentDictionary<Type, object> OfTypeCache = new ConcurrentDictionary<Type, object>();

	private readonly IServiceContainer _serviceContainer;

	public LightInjectGenericServiceProvider(IServiceContainer serviceContainer)
	{
		_serviceContainer = serviceContainer;
	}

	public IGenericServiceProviderScope CreateScope()
	{
		return new LightInjectGenericServiceProviderScope(_serviceContainer.BeginScope());
	}

	public TService? GetService<TService>() where TService : class
	{
		TService instance = _serviceContainer.GetInstance<TService>();
		Type typeFromHandle = typeof(TService);
		if (typeFromHandle.IsGenericType && typeFromHandle.GetGenericTypeDefinition() == typeof(IEnumerable<>) && instance is IEnumerable source)
		{
			OfTypeDelegate<TService> ofTypeDelegate = (OfTypeDelegate<TService>)OfTypeCache.GetOrAdd(typeof(TService), (Type x) => typeof(Enumerable).GetMethod("OfType").MakeGenericMethod(x.GenericTypeArguments[0]).CreateDelegate(typeof(OfTypeDelegate<TService>)));
			return ofTypeDelegate(source.Cast<object>().Distinct(Comparer));
		}
		return instance;
	}

	public void Dispose()
	{
		_serviceContainer.Dispose();
	}
}
