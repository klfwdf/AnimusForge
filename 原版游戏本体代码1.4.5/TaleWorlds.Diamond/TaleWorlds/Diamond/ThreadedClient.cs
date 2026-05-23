using System.Collections.Generic;
using System.Threading.Tasks;

namespace TaleWorlds.Diamond;

public class ThreadedClient : IClient
{
	private IClient _client;

	private Queue<ThreadedClientTask> _tasks;

	public ILoginAccessProvider AccessProvider => _client.AccessProvider;

	public bool IsInCriticalState => _client.IsInCriticalState;

	public long AliveCheckTimeInMiliSeconds => _client.AliveCheckTimeInMiliSeconds;

	public ThreadedClient(IClient client)
	{
		_client = client;
		_tasks = new Queue<ThreadedClientTask>();
	}

	public void Tick()
	{
		ThreadedClientTask threadedClientTask = null;
		lock (_tasks)
		{
			if (_tasks.Count > 0)
			{
				threadedClientTask = _tasks.Dequeue();
			}
		}
		threadedClientTask?.DoJob();
	}

	void IClient.HandleMessage(Message message)
	{
		ThreadedClientHandleMessageTask item = new ThreadedClientHandleMessageTask(_client, message);
		lock (_tasks)
		{
			_tasks.Enqueue(item);
		}
	}

	void IClient.OnConnected()
	{
		ThreadedClientConnectedTask item = new ThreadedClientConnectedTask(_client);
		lock (_tasks)
		{
			_tasks.Enqueue(item);
		}
	}

	void IClient.OnDisconnected()
	{
		ThreadedClientDisconnectedTask item = new ThreadedClientDisconnectedTask(_client);
		lock (_tasks)
		{
			_tasks.Enqueue(item);
		}
	}

	void IClient.OnCantConnect()
	{
		ThreadedClientCantConnectTask item = new ThreadedClientCantConnectTask(_client);
		lock (_tasks)
		{
			_tasks.Enqueue(item);
		}
	}

	public Task<bool> CheckConnection()
	{
		return _client.CheckConnection();
	}
}
