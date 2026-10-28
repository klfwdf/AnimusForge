using System;
using System.Reflection;

namespace MCM.LightInject;

internal interface IAssemblyScanner
{
	void Scan(Assembly assembly, IServiceRegistry serviceRegistry, Func<ILifetime> lifetime, Func<Type, Type, bool> shouldRegister, Func<Type, Type, string> serviceNameProvider);

	void Scan(Assembly assembly, IServiceRegistry serviceRegistry);
}
