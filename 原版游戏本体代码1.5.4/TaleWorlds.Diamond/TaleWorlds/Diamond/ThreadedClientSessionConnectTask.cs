namespace TaleWorlds.Diamond;

internal sealed class ThreadedClientSessionConnectTask : ThreadedClientSessionTask
{
	public ThreadedClientSessionConnectTask(IClientSession session)
		: base(session)
	{
	}

	public override void BeginJob()
	{
		base.Session.Connect();
	}

	public override void DoMainThreadJob()
	{
		base.Finished = true;
	}
}
