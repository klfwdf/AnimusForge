using System.Collections.Generic;

namespace AnimusForge;

internal sealed class WeeklyMaterialStageCursor<T>
{
	private readonly IReadOnlyList<T> _items;
	private int _index;

	internal WeeklyMaterialStageCursor(IReadOnlyList<T> items)
	{
		_items = items ?? new List<T>();
	}

	internal bool Complete => _index >= _items.Count;

	// The host checks its frame budget before taking each single group or batch.
	internal bool TryTake(out T item)
	{
		if (Complete)
		{
			item = default;
			return false;
		}
		item = _items[_index++];
		return true;
	}
}
