namespace TaleWorlds.Diamond;

internal sealed class ThreadedClientCantConnectTask : ThreadedClientTask
{
	public ThreadedClientCantConnectTask(IClient client)
		: base(client)
	{
	}

	public override void DoJob()
	{
		base.Client.OnCantConnect();
	}
}
