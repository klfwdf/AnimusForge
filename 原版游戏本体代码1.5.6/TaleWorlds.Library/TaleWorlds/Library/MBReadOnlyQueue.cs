using System.Collections.Generic;

namespace TaleWorlds.Library;

public class MBReadOnlyQueue<T> : Queue<T>
{
	public MBReadOnlyQueue()
	{
	}

	public MBReadOnlyQueue(int capacity)
		: base(capacity)
	{
	}

	public MBReadOnlyQueue(Queue<T> queue)
		: base((IEnumerable<T>)queue)
	{
	}

	public MBReadOnlyQueue(IEnumerable<T> collection)
		: base(collection)
	{
	}
}
