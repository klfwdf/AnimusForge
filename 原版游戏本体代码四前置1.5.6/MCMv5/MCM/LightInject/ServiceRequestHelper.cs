using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace MCM.LightInject;

[ExcludeFromCodeCoverage]
internal static class ServiceRequestHelper
{
	public static readonly MethodInfo CreateServiceRequestMethod;

	static ServiceRequestHelper()
	{
		CreateServiceRequestMethod = typeof(ServiceRequestHelper).GetTypeInfo().GetDeclaredMethod("CreateServiceRequest");
	}

	public static ServiceRequest CreateServiceRequest<TService>(string serviceName, IServiceFactory serviceFactory)
	{
		return new ServiceRequest(typeof(TService), serviceName, serviceFactory);
	}
}
