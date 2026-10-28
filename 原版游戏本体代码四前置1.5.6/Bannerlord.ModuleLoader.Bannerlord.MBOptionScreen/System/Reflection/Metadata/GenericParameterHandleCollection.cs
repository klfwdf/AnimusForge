using System.Collections;
using System.Collections.Generic;

namespace System.Reflection.Metadata;

internal readonly struct GenericParameterHandleCollection : IReadOnlyList<GenericParameterHandle>, IReadOnlyCollection<GenericParameterHandle>, IEnumerable<GenericParameterHandle>, IEnumerable
{
	public struct Enumerator : IEnumerator<GenericParameterHandle>, IDisposable, IEnumerator
	{
		private readonly int _lastRowId;

		private int _currentRowId;

		private const int EnumEnded = 16777216;

		public GenericParameterHandle Current => GenericParameterHandle.FromRowId((int)((long)_currentRowId & 0xFFFFFFL));

		object IEnumerator.Current => Current;

		internal Enumerator(int firstRowId, int lastRowId)
		{
			_currentRowId = firstRowId - 1;
			_lastRowId = lastRowId;
		}

		public bool MoveNext()
		{
			if (_currentRowId >= _lastRowId)
			{
				_currentRowId = 16777216;
				return false;
			}
			_currentRowId++;
			return true;
		}

		void IEnumerator.Reset()
		{
			throw new NotSupportedException();
		}

		void IDisposable.Dispose()
		{
		}
	}

	private readonly int _firstRowId;

	private readonly ushort _count;

	public int Count => _count;

	public GenericParameterHandle this[int index]
	{
		get
		{
			if (index < 0 || index >= _count)
			{
				System.Reflection.Throw.IndexOutOfRange();
			}
			return GenericParameterHandle.FromRowId(_firstRowId + index);
		}
	}

	internal GenericParameterHandleCollection(int firstRowId, ushort count)
	{
		_firstRowId = firstRowId;
		_count = count;
	}

	public Enumerator GetEnumerator()
	{
		return new Enumerator(_firstRowId, _firstRowId + _count - 1);
	}

	IEnumerator<GenericParameterHandle> IEnumerable<GenericParameterHandle>.GetEnumerator()
	{
		return GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}
}
