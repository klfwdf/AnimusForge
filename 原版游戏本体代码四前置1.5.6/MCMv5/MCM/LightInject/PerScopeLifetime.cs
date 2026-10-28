using System;
using System.Diagnostics.CodeAnalysis;

namespace MCM.LightInject;

[LifeSpan(20)]
[ExcludeFromCodeCoverage]
internal class PerScopeLifetime : ILifetime, ICloneableLifeTime
{
	public object GetInstance(Func<object> createInstance, Scope scope)
	{
		throw new NotImplementedException("Optimized");
	}

	public ILifetime Clone()
	{
		return new PerScopeLifetime();
	}
}
