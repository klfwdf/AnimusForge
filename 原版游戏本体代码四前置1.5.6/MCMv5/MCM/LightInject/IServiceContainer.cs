using System;

namespace MCM.LightInject;

internal interface IServiceContainer : IServiceRegistry, IServiceFactory, IDisposable
{
	IScopeManagerProvider ScopeManagerProvider { get; set; }

	bool CanGetInstance(Type serviceType, string serviceName);

	object InjectProperties(object instance);

	void Compile();

	void Compile(Func<ServiceRegistration, bool> predicate);

	void Compile<TService>(string serviceName = null);
}
