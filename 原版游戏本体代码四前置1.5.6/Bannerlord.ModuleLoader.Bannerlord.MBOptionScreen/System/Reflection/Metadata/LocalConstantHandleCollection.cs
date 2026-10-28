using System.Collections;
using System.Collections.Generic;

namespace System.Reflection.Metadata;

internal readonly struct LocalConstantHandleCollection : IReadOnlyCollection<LocalConstantHandle>, IEnumerable<LocalConstantHandle>, IEnumerable
{
	public struct Enumerator : IEnumerator<LocalConstantHandle>, IDisposable, IEnumerator
	{
		private readonly System.Reflection.Metadata.MetadataReader _reader;

		private readonly int _lastRowId;

		private int _currentRowId;

		private const int EnumEnded = 16777216;

		public LocalConstantHandle Current => LocalConstantHandle.FromRowId((int)((long)_currentRowId & 0xFFFFFFL));

		object IEnumerator.Current => Current;

		internal Enumerator(System.Reflection.Metadata.MetadataReader reader, int firstRowId, int lastRowId)
		{
			_reader = reader;
			_lastRowId = lastRowId;
			_currentRowId = firstRowId - 1;
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

	private readonly System.Reflection.Metadata.MetadataReader _reader;

	private readonly int _firstRowId;

	private readonly int _lastRowId;

	public int Count => _lastRowId - _firstRowId + 1;

	internal LocalConstantHandleCollection(System.Reflection.Metadata.MetadataReader reader, LocalScopeHandle scope)
	{
		_reader = reader;
		if (scope.IsNil)
		{
			_firstRowId = 1;
			_lastRowId = reader.LocalConstantTable.NumberOfRows;
		}
		else
		{
			reader.GetLocalConstantRange(scope, out _firstRowId, out _lastRowId);
		}
	}

	public Enumerator GetEnumerator()
	{
		return new Enumerator(_reader, _firstRowId, _lastRowId);
	}

	IEnumerator<LocalConstantHandle> IEnumerable<LocalConstantHandle>.GetEnumerator()
	{
		return GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}
}
