namespace TaleWorlds.Network;

public class WaitForTicks : CoroutineState
{
	private int _beginTick;

	internal int TickCount { get; private set; }

	protected internal override bool IsFinished => _beginTick + TickCount >= base.CoroutineManager.CurrentTick;

	public WaitForTicks(int tickCount)
	{
		TickCount = tickCount;
	}

	protected internal override void Initialize(CoroutineManager coroutineManager)
	{
		base.Initialize(coroutineManager);
		_beginTick = coroutineManager.CurrentTick;
	}
}
