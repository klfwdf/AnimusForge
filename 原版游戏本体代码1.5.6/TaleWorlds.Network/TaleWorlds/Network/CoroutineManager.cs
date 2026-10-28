using System.Collections.Generic;

namespace TaleWorlds.Network;

public class CoroutineManager
{
	private List<Coroutine> _coroutines;

	public int CurrentTick { get; private set; }

	public int CoroutineCount => _coroutines.Count;

	public CoroutineManager()
	{
		_coroutines = new List<Coroutine>();
		CurrentTick = 0;
	}

	public void AddCoroutine(CoroutineDelegate coroutineMethod)
	{
		Coroutine coroutine = new Coroutine();
		coroutine.CoroutineMethod = coroutineMethod;
		coroutine.IsStarted = false;
		_coroutines.Add(coroutine);
	}

	public void Tick()
	{
		for (int i = 0; i < _coroutines.Count; i++)
		{
			Coroutine coroutine = _coroutines[i];
			bool flag = false;
			if (!coroutine.IsStarted)
			{
				coroutine.IsStarted = true;
				flag = true;
				coroutine.Enumerator = coroutine.CoroutineMethod();
			}
			if (flag || coroutine.CurrentState.IsFinished)
			{
				if (!coroutine.Enumerator.MoveNext())
				{
					_coroutines.Remove(coroutine);
					i--;
				}
				else
				{
					coroutine.CurrentState = coroutine.Enumerator.Current as CoroutineState;
					coroutine.CurrentState.Initialize(this);
				}
			}
		}
		CurrentTick++;
	}
}
