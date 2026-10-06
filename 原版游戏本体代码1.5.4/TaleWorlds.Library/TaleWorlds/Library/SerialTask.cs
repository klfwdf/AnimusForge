namespace TaleWorlds.Library;

public class SerialTask : ITask
{
	public delegate void DelegateDefinition();

	private DelegateDefinition _instance;

	public SerialTask(DelegateDefinition function)
	{
		_instance = function;
	}

	void ITask.Invoke()
	{
		_instance();
	}

	void ITask.Wait()
	{
	}
}
