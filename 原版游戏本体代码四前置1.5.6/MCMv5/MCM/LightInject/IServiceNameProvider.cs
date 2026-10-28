using System;

namespace MCM.LightInject;

internal interface IServiceNameProvider
{
	string GetServiceName(Type serviceType, Type implementingType);
}
