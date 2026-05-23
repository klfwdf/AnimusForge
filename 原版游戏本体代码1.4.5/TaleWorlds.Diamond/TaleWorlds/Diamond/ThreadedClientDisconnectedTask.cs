namespace TaleWorlds.Diamond;

internal sealed class ThreadedClientDisconnectedTask : ThreadedClientTask
{
	public ThreadedClientDisconnectedTask(IClient client)
		: base(client)
	{
	}

	public override void DoJob()
	{
		base.Client.OnDisconnected();
	}
}
