using System;
using System.Collections.Generic;

namespace AnimusForge;

internal sealed class WeeklyReportBlockMaterialCursor<T> where T : class
{
	private readonly IReadOnlyList<T> _source;
	private readonly List<T> _cloned = new List<T>();
	private int _index;

	internal WeeklyReportBlockMaterialCursor(IReadOnlyList<T> source)
	{
		_source = source ?? new List<T>();
	}

	internal bool Complete => _index >= _source.Count;
	internal List<T> Cloned => _cloned;

	internal bool Advance(Func<T, T> clone)
	{
		if (Complete)
		{
			return false;
		}
		T material = clone(_source[_index++]);
		if (material != null)
		{
			_cloned.Add(material);
		}
		return true;
	}
}
