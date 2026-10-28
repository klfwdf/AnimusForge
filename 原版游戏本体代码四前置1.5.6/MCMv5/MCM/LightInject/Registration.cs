using System;
using System.Diagnostics.CodeAnalysis;

namespace MCM.LightInject;

[ExcludeFromCodeCoverage]
internal abstract class Registration
{
	public Type ServiceType { get; set; }

	public virtual Type ImplementingType { get; set; }

	public Delegate FactoryExpression { get; set; }
}
