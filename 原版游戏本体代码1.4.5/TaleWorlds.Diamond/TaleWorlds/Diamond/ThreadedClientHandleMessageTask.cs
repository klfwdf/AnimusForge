namespace TaleWorlds.Diamond;

internal sealed class ThreadedClientHandleMessageTask : ThreadedClientTask
{
	public Message Message { get; private set; }

	public ThreadedClientHandleMessageTask(IClient client, Message message)
		: base(client)
	{
		Message = message;
	}

	public override void DoJob()
	{
		base.Client.HandleMessage(Message);
	}
}
