using System.Collections;
using System.Collections.Generic;

namespace System.Reflection.Metadata;

internal readonly struct MethodImplementationHandleCollection : IReadOnlyCollection<MethodImplementationHandle>, IEnumerable<MethodImplementationHandle>, IEnumerable
{
	public struct Enumerator : IEnumerator<MethodImplementationHandle>, IDisposable, IEnumerator
	{
		private readonly int _lastRowId;

		private int _currentRowId;

		private const int EnumEnded = 16777216;

		public MethodImplementationHandle Current => MethodImplementationHandle.FromRowId((int)((long)_currentRowId & 0xFFFFFFL));

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

	private readonly int _lastRowId;

	public int Count => _lastRowId - _firstRowId + 1;

	internal MethodImplementationHandleCollection(System.Reflection.Metadata.MetadataReader reader, TypeDefinitionHandle containingType)
	{
		if (containingType.IsNil)
		{
			_firstRowId = 1;
			_lastRowId = reader.MethodImplTable.NumberOfRows;
		}
		else
		{
			reader.MethodImplTable.GetMethodImplRange(containingType, out _firstRowId, out _lastRowId);
		}
	}

	public Enumerator GetEnumerator()
	{
		return new Enumerator(_firstRowId, _lastRowId);
	}

	IEnumerator<MethodImplementationHandle> IEnumerable<MethodImplementationHandle>.GetEnumerator()
	{
		return GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}
}
