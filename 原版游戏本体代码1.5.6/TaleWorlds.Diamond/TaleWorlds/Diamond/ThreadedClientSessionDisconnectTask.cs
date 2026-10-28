namespace TaleWorlds.Diamond;

internal sealed class ThreadedClientSessionDisconnectTask : ThreadedClientSessionTask
{
	public ThreadedClientSessionDisconnectTask(IClientSession session)
		: base(session)
	{
	}

	public override void BeginJob()
	{
		base.Session.Disconnect();
	}

	public override void DoMainThreadJob()
	{
		base.Finished = true;
	}
}
