using System;
using System.Diagnostics.CodeAnalysis;

namespace MCM.LightInject;

[ExcludeFromCodeCoverage]
internal class DecoratorRegistration : Registration
{
	public Func<ServiceRegistration, bool> CanDecorate { get; set; }

	public Func<IServiceFactory, ServiceRegistration, Type> ImplementingTypeFactory { get; set; }

	public int Index { get; set; }

	public bool HasDeferredImplementingType => ImplementingType == null && (object)base.FactoryExpression == null;
}
