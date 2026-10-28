using System;

namespace JetBrains.Annotations;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Parameter)]
public sealed class AspMvcActionAttribute : Attribute
{
	[UsedImplicitly]
	public string AnonymousProperty { get; private set; }

	public AspMvcActionAttribute()
	{
	}

	public AspMvcActionAttribute(string anonymousProperty)
	{
		AnonymousProperty = anonymousProperty;
	}
}
