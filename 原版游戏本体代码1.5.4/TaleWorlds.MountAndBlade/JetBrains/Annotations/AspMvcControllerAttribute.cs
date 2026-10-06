using System;

namespace JetBrains.Annotations;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Parameter)]
public sealed class AspMvcControllerAttribute : Attribute
{
	[UsedImplicitly]
	public string AnonymousProperty { get; private set; }

	public AspMvcControllerAttribute()
	{
	}

	public AspMvcControllerAttribute(string anonymousProperty)
	{
		AnonymousProperty = anonymousProperty;
	}
}
