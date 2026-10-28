using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;

namespace MCM.LightInject;

[ExcludeFromCodeCoverage]
internal class Scope : IServiceFactory, IDisposable
{
	private class ReferenceEqualityComparer<T> : IEqualityComparer<T>
	{
		public static readonly ReferenceEqualityComparer<T> Default = new ReferenceEqualityComparer<T>();

		public bool Equals(T x, T y)
		{
			return (object)x == (object)y;
		}

		public int GetHashCode(T obj)
		{
			return obj.GetHashCode();
		}
	}

	public bool IsDisposed;

	public Scope ParentScope;

	private readonly object lockObject = new object();

	private readonly IScopeManager scopeManager;

	private readonly ServiceContainer serviceFactory;

	private List<object> disposableObjects;

	private ImmutableMapTree<object> createdInstances = ImmutableMapTree<object>.Empty;

	public event EventHandler<EventArgs> Completed;

	public Scope(IScopeManager scopeManager, Scope parentScope)
	{
		this.scopeManager = scopeManager;
		serviceFactory = (ServiceContainer)scopeManager.ServiceFactory;
		ParentScope = parentScope;
	}

	public Scope(ServiceContainer serviceFactory)
	{
		this.serviceFactory = serviceFactory;
	}

	public void TrackInstance(object disposable)
	{
		lock (lockObject)
		{
			if (disposableObjects == null)
			{
				disposableObjects = new List<object>();
			}
			disposableObjects.Add(disposable);
		}
	}

	public void Dispose()
	{
		if (disposableObjects != null && disposableObjects.Count > 0)
		{
			HashSet<IDisposable> hashSet = new HashSet<IDisposable>(ReferenceEqualityComparer<IDisposable>.Default);
			int num = disposableObjects.Count - 1;
			while (num >= 0)
			{
				if (disposableObjects[num] is IDisposable disposable)
				{
					if (hashSet.Add(disposable))
					{
						disposable.Dispose();
					}
					num--;
					continue;
				}
				throw new InvalidOperationException($"The type {disposableObjects[num].GetType()} only implements `IAsyncDisposable` and can only be disposed in an asynchronous scope started with `BeginScopeAsync()`");
			}
		}
		EndScope();
	}

	private void EndScope()
	{
		scopeManager?.EndScope(this);
		Completed?.Invoke(this, new EventArgs());
		IsDisposed = true;
	}

	public Scope BeginScope()
	{
		return serviceFactory.BeginScope();
	}

	public object GetInstance(Type serviceType)
	{
		return serviceFactory.GetInstance(serviceType, this);
	}

	public object GetInstance(Type serviceType, string serviceName)
	{
		return serviceFactory.GetInstance(serviceType, this, serviceName);
	}

	public object GetInstance(Type serviceType, object[] arguments)
	{
		return serviceFactory.GetInstance(serviceType, arguments, this);
	}

	public object GetInstance(Type serviceType, string serviceName, object[] arguments)
	{
		return serviceFactory.GetInstance(serviceType, serviceName, arguments, this);
	}

	public object TryGetInstance(Type serviceType)
	{
		return serviceFactory.TryGetInstance(serviceType, this);
	}

	public object TryGetInstance(Type serviceType, string serviceName)
	{
		return serviceFactory.TryGetInstance(serviceType, serviceName, this);
	}

	public IEnumerable<object> GetAllInstances(Type serviceType)
	{
		return serviceFactory.GetAllInstances(serviceType, this);
	}

	public object Create(Type serviceType)
	{
		return serviceFactory.Create(serviceType, this);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal object GetScopedInstance(GetInstanceDelegate getInstanceDelegate, object[] arguments, int instanceDelegateIndex)
	{
		object obj = createdInstances.Search(instanceDelegateIndex);
		if (obj != null)
		{
			return obj;
		}
		lock (lockObject)
		{
			obj = createdInstances.Search(instanceDelegateIndex);
			if (obj == null)
			{
				obj = getInstanceDelegate(arguments, this);
				if (obj is IDisposable)
				{
					TrackInstance(obj);
				}
				Interlocked.Exchange(ref createdInstances, createdInstances.Add(instanceDelegateIndex, obj));
			}
			return obj;
		}
	}
}
