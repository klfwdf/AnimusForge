using System;
using System.Diagnostics.CodeAnalysis;

namespace MCM.LightInject;

[LifeSpan(10)]
[ExcludeFromCodeCoverage]
internal class PerRequestLifeTime : ILifetime, ICloneableLifeTime
{
	public object GetInstance(Func<object> createInstance, Scope scope)
	{
		throw new NotImplementedException("Optimized");
	}

	public ILifetime Clone()
	{
		return new PerRequestLifeTime();
	}
}
