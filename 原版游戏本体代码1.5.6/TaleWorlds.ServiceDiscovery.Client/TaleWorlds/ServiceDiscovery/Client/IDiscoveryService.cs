using System.Threading.Tasks;

namespace TaleWorlds.ServiceDiscovery.Client;

public interface IDiscoveryService
{
	Task<ServiceAddress[]> DiscoverServices();

	Task<ServiceAddress[]> ResolveService(string service, string tag = "");

	Task<ServiceResolvedAddress> ResolveServiceByTag(string tag);
}
