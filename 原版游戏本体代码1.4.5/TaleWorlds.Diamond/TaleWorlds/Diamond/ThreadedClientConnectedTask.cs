namespace TaleWorlds.Diamond;

internal sealed class ThreadedClientConnectedTask : ThreadedClientTask
{
	public ThreadedClientConnectedTask(IClient client)
		: base(client)
	{
	}

	public override void DoJob()
	{
		base.Client.OnConnected();
	}
}
