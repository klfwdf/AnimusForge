using System;
using TaleWorlds.Library;

namespace TaleWorlds.Core;

public class MBFastRandomSelector<T>
{
	public struct IndexEntry
	{
		public ushort Index;

		public ushort Version;

		public IndexEntry(ushort index, ushort version)
		{
			Index = index;
			Version = version;
		}
	}

	public const ushort MinimumCapacity = 32;

	public const ushort MaximumCapacity = ushort.MaxValue;

	private const ushort InitialVersion = 1;

	private const ushort MaximumVersion = ushort.MaxValue;

	private MBReadOnlyList<T> _list;

	private IndexEntry[] _indexArray;

	private ushort _currentVersion;

	public ushort RemainingCount { get; private set; }

	public MBFastRandomSelector(ushort capacity = 32)
	{
		ReallocateIndexArray(capacity);
		_list = null;
	}

	public MBFastRandomSelector(MBReadOnlyList<T> list, ushort capacity = 32)
	{
		ReallocateIndexArray(capacity);
		Initialize(list);
	}

	public void Initialize(MBReadOnlyList<T> list)
	{
		if (list != null && list.Count <= 65535)
		{
			_list = list;
			TryExpand();
		}
		else
		{
			Debug.FailedAssert("Cannot initialize random selector as passed list is null or it exceeds " + ushort.MaxValue + " elements).", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.Core\\MBFastRandomSelector.cs", "Initialize", 63);
			_list = null;
		}
		Reset();
	}

	public void Reset()
	{
		if (_list != null)
		{
			if (_currentVersion < ushort.MaxValue)
			{
				_currentVersion++;
			}
			else
			{
				for (int i = 0; i < _indexArray.Length; i++)
				{
					_indexArray[i] = default(IndexEntry);
				}
				_currentVersion = 1;
			}
			RemainingCount = (ushort)_list.Count;
		}
		else
		{
			_currentVersion = 1;
			RemainingCount = 0;
		}
	}

	public void Pack()
	{
		if (_list != null)
		{
			ushort num = (ushort)TaleWorlds.Library.MathF.Max(32, _list.Count);
			if (_indexArray.Length != num)
			{
				ReallocateIndexArray(num);
			}
		}
		else if (_indexArray.Length != 32)
		{
			ReallocateIndexArray(32);
		}
	}

	public bool SelectRandom(out T selection, Predicate<T> conditions = null)
	{
		selection = default(T);
		if (_list == null)
		{
			return false;
		}
		bool flag = false;
		while (RemainingCount > 0 && !flag)
		{
			ushort num = (ushort)MBRandom.RandomInt(RemainingCount);
			ushort num2 = (ushort)(RemainingCount - 1);
			IndexEntry indexEntry = _indexArray[num];
			T val = ((indexEntry.Version == _currentVersion) ? _list[indexEntry.Index] : _list[num]);
			if (conditions == null || conditions(val))
			{
				flag = true;
				selection = val;
			}
			IndexEntry indexEntry2 = _indexArray[num2];
			_indexArray[num] = ((indexEntry2.Version == _currentVersion) ? new IndexEntry(indexEntry2.Index, _currentVersion) : new IndexEntry(num2, _currentVersion));
			RemainingCount--;
		}
		return flag;
	}

	private void TryExpand()
	{
		if (_indexArray.Length < _list.Count)
		{
			ushort capacity = (ushort)(_list.Count * 2);
			ReallocateIndexArray(capacity);
		}
	}

	private void ReallocateIndexArray(ushort capacity)
	{
		capacity = (ushort)MBMath.ClampInt(capacity, 32, 65535);
		_indexArray = new IndexEntry[capacity];
		_currentVersion = 1;
	}
}
