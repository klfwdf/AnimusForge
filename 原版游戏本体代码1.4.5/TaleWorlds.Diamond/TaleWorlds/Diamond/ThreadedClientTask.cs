namespace TaleWorlds.Diamond;

internal abstract class ThreadedClientTask
{
	public IClient Client { get; private set; }

	protected ThreadedClientTask(IClient client)
	{
		Client = client;
	}

	public abstract void DoJob();
}
