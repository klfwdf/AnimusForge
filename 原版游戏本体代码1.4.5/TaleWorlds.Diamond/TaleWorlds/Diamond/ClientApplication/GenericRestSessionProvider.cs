using TaleWorlds.Diamond.Rest;
using TaleWorlds.Library.Http;

namespace TaleWorlds.Diamond.ClientApplication;

public class GenericRestSessionProvider<T> : IClientSessionProvider<T> where T : Client<T>
{
	private string _address;

	private IHttpDriver _httpDriver;

	public GenericRestSessionProvider(string address, IHttpDriver httpDriver)
	{
		_address = address;
		_httpDriver = httpDriver;
	}

	public IClientSession CreateSession(T session)
	{
		return new ClientRestSession(session, _address, _httpDriver);
	}
}
