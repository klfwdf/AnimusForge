using System;
using System.Collections.Generic;

namespace MCM.LightInject;

internal interface IServiceFactory
{
	Scope BeginScope();

	object GetInstance(Type serviceType);

	object GetInstance(Type serviceType, object[] arguments);

	object GetInstance(Type serviceType, string serviceName, object[] arguments);

	object GetInstance(Type serviceType, string serviceName);

	object TryGetInstance(Type serviceType);

	object TryGetInstance(Type serviceType, string serviceName);

	IEnumerable<object> GetAllInstances(Type serviceType);

	object Create(Type serviceType);
}
