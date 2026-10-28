using System;
using System.Diagnostics.CodeAnalysis;

namespace MCM.LightInject;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
[ExcludeFromCodeCoverage]
internal class LifeSpanAttribute : Attribute
{
	public int Value { get; }

	public LifeSpanAttribute(int value)
	{
		Value = value;
	}
}
