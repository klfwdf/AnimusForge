namespace TaleWorlds.Network;

public abstract class CoroutineState
{
	protected CoroutineManager CoroutineManager { get; private set; }

	protected internal abstract bool IsFinished { get; }

	protected internal virtual void Initialize(CoroutineManager coroutineManager)
	{
		CoroutineManager = coroutineManager;
	}
}
