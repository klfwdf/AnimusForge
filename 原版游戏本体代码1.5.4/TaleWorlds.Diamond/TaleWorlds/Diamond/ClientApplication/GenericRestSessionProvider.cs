using TaleWorlds.Diamond.Rest;
using TaleWorlds.Library.Http;

namespace TaleWorlds.Diamond.ClientApplication;

public class GenericRestSessionProvider : IClientSessionFactory
{
	private string _address;

	private IHttpDriver _httpDriver;

	public GenericRestSessionProvider(string address, IHttpDriver httpDriver)
	{
		_address = address;
		_httpDriver = httpDriver;
	}

	public IClientSession CreateSession(int aliveCheckInterval)
	{
		return new ClientRestSession(_address, _httpDriver, aliveCheckInterval);
	}
}
