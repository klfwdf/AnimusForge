using System;
using System.Diagnostics.CodeAnalysis;

namespace MCM.LightInject;

[ExcludeFromCodeCoverage]
internal class ServiceRequest
{
	public Type ServiceType { get; private set; }

	public string ServiceName { get; private set; }

	public IServiceFactory ServiceFactory { get; private set; }

	public ServiceRequest(Type serviceType, string serviceName, IServiceFactory serviceFactory)
	{
		ServiceType = serviceType;
		ServiceName = serviceName;
		ServiceFactory = serviceFactory;
	}
}
