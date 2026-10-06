using System.Collections.Generic;

namespace TaleWorlds.Library;

public class MBReadOnlyList<T> : List<T>
{
	public MBReadOnlyList()
	{
	}

	public MBReadOnlyList(int capacity)
		: base(capacity)
	{
	}

	public MBReadOnlyList(IEnumerable<T> collection)
		: base(collection)
	{
	}
}
