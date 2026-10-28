namespace TaleWorlds.Diamond;

internal sealed class ThreadedClientSessionMessageTask : ThreadedClientSessionTask
{
	public Message Message { get; private set; }

	public ThreadedClientSessionMessageTask(IClientSession session, Message message)
		: base(session)
	{
		Message = message;
	}

	public override void BeginJob()
	{
		base.Session.SendMessage(Message);
	}

	public override void DoMainThreadJob()
	{
		base.Finished = true;
	}
}
