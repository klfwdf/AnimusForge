using System.Collections.Generic;

namespace TaleWorlds.TwoDimension;

public class MaterialPool<T> where T : Material, new()
{
	private List<T> _materialList;

	private int _nextAvailableIndex;

	public MaterialPool(int initialBufferSize)
	{
		_materialList = new List<T>(initialBufferSize);
	}

	public T New()
	{
		if (_nextAvailableIndex < _materialList.Count)
		{
			T result = _materialList[_nextAvailableIndex];
			_nextAvailableIndex++;
			return result;
		}
		T val = new T();
		_materialList.Add(val);
		_nextAvailableIndex++;
		return val;
	}

	public void ResetAll()
	{
		_nextAvailableIndex = 0;
	}
}
