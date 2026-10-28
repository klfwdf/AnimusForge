using System;
using System.Diagnostics.CodeAnalysis;

namespace MCM.LightInject;

[LifeSpan(30)]
[ExcludeFromCodeCoverage]
internal class PerContainerLifetime : ILifetime, ICloneableLifeTime
{
	private readonly object syncRoot = new object();

	private volatile object singleton;

	public object GetInstance(Func<object> createInstance, Scope scope)
	{
		throw new NotImplementedException("Optimized");
	}

	public object GetInstance(GetInstanceDelegate createInstance, Scope scope, object[] arguments)
	{
		if (singleton != null)
		{
			return singleton;
		}
		lock (syncRoot)
		{
			if (singleton == null)
			{
				singleton = createInstance(arguments, scope);
			}
		}
		return singleton;
	}

	public ILifetime Clone()
	{
		return new PerContainerLifetime();
	}
}
