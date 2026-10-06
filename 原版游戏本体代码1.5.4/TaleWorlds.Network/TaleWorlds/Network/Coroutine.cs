using System.Collections;

namespace TaleWorlds.Network;

public class Coroutine
{
	public bool IsStarted { get; internal set; }

	internal CoroutineDelegate CoroutineMethod { get; set; }

	internal IEnumerator Enumerator { get; set; }

	internal CoroutineState CurrentState { get; set; }
}
