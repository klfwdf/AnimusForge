using System;

namespace TaleWorlds.ServiceDiscovery.Client;

[Serializable]
public class ServiceResolvedAddress
{
	public string Address { get; private set; }

	public string[] Tags { get; private set; }

	public ServiceResolvedAddress(string address, string[] tags)
	{
		Address = address;
		Tags = tags;
	}
}
