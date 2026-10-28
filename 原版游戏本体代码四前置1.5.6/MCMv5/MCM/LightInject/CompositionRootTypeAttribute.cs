using System;
using System.Diagnostics.CodeAnalysis;

namespace MCM.LightInject;

[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
[ExcludeFromCodeCoverage]
internal class CompositionRootTypeAttribute : Attribute
{
	public Type CompositionRootType { get; private set; }

	public CompositionRootTypeAttribute(Type compositionRootType)
	{
		CompositionRootType = compositionRootType;
	}
}
