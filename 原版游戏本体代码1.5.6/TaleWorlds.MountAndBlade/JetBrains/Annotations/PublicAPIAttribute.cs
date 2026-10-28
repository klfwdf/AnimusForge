using System;

namespace JetBrains.Annotations;

[MeansImplicitUse]
public sealed class PublicAPIAttribute : Attribute
{
	public PublicAPIAttribute()
	{
	}

	public PublicAPIAttribute(string comment)
	{
	}
}
