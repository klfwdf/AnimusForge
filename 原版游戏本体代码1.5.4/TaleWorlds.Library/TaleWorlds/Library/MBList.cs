using System.Collections.Generic;

namespace TaleWorlds.Library;

public class MBList<T> : MBReadOnlyList<T>
{
	public MBList()
	{
	}

	public MBList(int capacity)
		: base(capacity)
	{
	}

	public MBList(IEnumerable<T> collection)
		: base(collection)
	{
	}

	public MBList(List<T> collection)
		: base((IEnumerable<T>)collection)
	{
	}
}
