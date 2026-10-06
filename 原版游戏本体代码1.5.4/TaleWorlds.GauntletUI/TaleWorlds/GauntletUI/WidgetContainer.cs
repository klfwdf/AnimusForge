using System.Collections.Generic;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;

namespace TaleWorlds.GauntletUI;

internal class WidgetContainer
{
	internal enum ContainerType
	{
		Update,
		ParallelUpdate,
		LateUpdate,
		VisualDefinition,
		UpdateBrushes
	}

	private HashSet<Widget> _backList;

	private MBList<Widget> _frontList;

	private bool _isFragmented;

	internal int Count => GetActiveList().Count;

	internal WidgetContainer(int initialCapacity)
	{
		_backList = new HashSet<Widget>();
		_frontList = new MBList<Widget>(initialCapacity);
	}

	internal void Add(Widget widget)
	{
		lock (_backList)
		{
			_backList.Add(widget);
		}
		_isFragmented = true;
	}

	internal void Remove(Widget widget)
	{
		lock (_backList)
		{
			_backList.Remove(widget);
		}
		_isFragmented = true;
	}

	public void Clear()
	{
		_backList.Clear();
		_frontList.Clear();
		_backList = null;
		_frontList = null;
		_isFragmented = true;
	}

	public MBReadOnlyList<Widget> GetActiveList()
	{
		return _frontList;
	}

	public void Defrag()
	{
		if (!_isFragmented)
		{
			return;
		}
		_frontList.Clear();
		lock (_backList)
		{
			foreach (Widget back in _backList)
			{
				_frontList.Add(back);
			}
		}
		_isFragmented = false;
	}
}
