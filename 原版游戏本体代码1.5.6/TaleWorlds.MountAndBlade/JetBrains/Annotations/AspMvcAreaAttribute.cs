using System;

namespace JetBrains.Annotations;

[AttributeUsage(AttributeTargets.Parameter)]
public sealed class AspMvcAreaAttribute : PathReferenceAttribute
{
	[UsedImplicitly]
	public string AnonymousProperty { get; private set; }

	[UsedImplicitly]
	public AspMvcAreaAttribute()
	{
	}

	public AspMvcAreaAttribute(string anonymousProperty)
	{
		AnonymousProperty = anonymousProperty;
	}
}
