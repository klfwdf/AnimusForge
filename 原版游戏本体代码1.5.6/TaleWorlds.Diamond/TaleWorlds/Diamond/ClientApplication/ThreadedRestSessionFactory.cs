using TaleWorlds.Diamond.Rest;
using TaleWorlds.Library;
using TaleWorlds.Library.Http;

namespace TaleWorlds.Diamond.ClientApplication;

public class ThreadedRestSessionFactory : IClientSessionFactory
{
	public const int DefaultThreadSleepTime = 100;

	private readonly string _address;

	private readonly IHttpDriver _httpDriver;

	private readonly ParameterContainer _parameters;

	public ThreadedRestSessionFactory(string address, IHttpDriver httpDriver, ParameterContainer parameters)
	{
		_address = address;
		_httpDriver = httpDriver;
		_parameters = parameters;
	}

	public IClientSession CreateSession(int aliveCheckInterval)
	{
		if (!_parameters.TryGetParameterAsInt("ThreadedClientSession.ThreadSleepTime", out var outValue))
		{
			outValue = 100;
		}
		return new ThreadedClientSession(new ClientRestSession(_address, _httpDriver, aliveCheckInterval), outValue);
	}
}
