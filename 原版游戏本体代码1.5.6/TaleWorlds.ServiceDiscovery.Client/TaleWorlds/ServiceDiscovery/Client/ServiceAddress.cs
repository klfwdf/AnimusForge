namespace TaleWorlds.ServiceDiscovery.Client;

public class ServiceAddress
{
	private const string Prefix = "service://";

	private const char Suffix = '/';

	public string Service { get; private set; }

	public ServiceResolvedAddress[] ResolvedAddresses { get; private set; }

	public ServiceAddress(string service, ServiceResolvedAddress[] resolvedAddresses)
	{
		ResolvedAddresses = resolvedAddresses;
		Service = service;
	}

	public static bool IsServiceAddress(string address)
	{
		if (!string.IsNullOrEmpty(address))
		{
			string text = address.ToLower();
			if (text.StartsWith("service://") && text.EndsWith('/'.ToString()))
			{
				return true;
			}
		}
		return false;
	}

	public static bool TryGetAddressName(string serviceAddress, out string addressName)
	{
		if (IsServiceAddress(serviceAddress))
		{
			addressName = serviceAddress.Substring("service://".Length).Trim(new char[1] { '/' });
			return true;
		}
		addressName = null;
		return false;
	}
}
