using System.Collections;
using System.Collections.Generic;

namespace System.Reflection.Metadata;

internal readonly struct GenericParameterConstraintHandleCollection : IReadOnlyList<GenericParameterConstraintHandle>, IReadOnlyCollection<GenericParameterConstraintHandle>, IEnumerable<GenericParameterConstraintHandle>, IEnumerable
{
	public struct Enumerator : IEnumerator<GenericParameterConstraintHandle>, IDisposable, IEnumerator
	{
		private readonly int _lastRowId;

		private int _currentRowId;

		private const int EnumEnded = 16777216;

		public GenericParameterConstraintHandle Current => GenericParameterConstraintHandle.FromRowId((int)((long)_currentRowId & 0xFFFFFFL));

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

	public GenericParameterConstraintHandle this[int index]
	{
		get
		{
			if (index < 0 || index >= _count)
			{
				System.Reflection.Throw.IndexOutOfRange();
			}
			return GenericParameterConstraintHandle.FromRowId(_firstRowId + index);
		}
	}

	internal GenericParameterConstraintHandleCollection(int firstRowId, ushort count)
	{
		_firstRowId = firstRowId;
		_count = count;
	}

	public Enumerator GetEnumerator()
	{
		return new Enumerator(_firstRowId, _firstRowId + _count - 1);
	}

	IEnumerator<GenericParameterConstraintHandle> IEnumerable<GenericParameterConstraintHandle>.GetEnumerator()
	{
		return GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}
}
