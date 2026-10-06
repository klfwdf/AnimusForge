namespace TaleWorlds.Library;

public interface ITask
{
	void Invoke();

	void Wait();
}
