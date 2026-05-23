using TaleWorlds.Diamond.Rest;
using TaleWorlds.Library.Http;

namespace TaleWorlds.Diamond.ClientApplication;

public class GenericThreadedRestSessionProvider<T> : IClientSessionProvider<T> where T : Client<T>
{
	public const int DefaultThreadSleepTime = 100;

	private string _address;

	private IHttpDriver _httpDriver;

	public GenericThreadedRestSessionProvider(string address, IHttpDriver httpDriver)
	{
		_address = address;
		_httpDriver = httpDriver;
	}

	public IClientSession CreateSession(T client)
	{
		if (!client.Application.Parameters.TryGetParameterAsInt("ThreadedClientSession.ThreadSleepTime", out var outValue))
		{
			outValue = 100;
		}
		ThreadedClient threadedClient = new ThreadedClient(client);
		ClientRestSession session = new ClientRestSession(threadedClient, _address, _httpDriver);
		return new ThreadedClientSession(threadedClient, session, outValue);
	}
}
