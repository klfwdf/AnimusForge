using System.Collections;
using System.Collections.Generic;

namespace System.Reflection.Metadata;

internal readonly struct MethodDefinitionHandleCollection : IReadOnlyCollection<System.Reflection.Metadata.MethodDefinitionHandle>, IEnumerable<System.Reflection.Metadata.MethodDefinitionHandle>, IEnumerable
{
	public struct Enumerator : IEnumerator<System.Reflection.Metadata.MethodDefinitionHandle>, IDisposable, IEnumerator
	{
		private readonly System.Reflection.Metadata.MetadataReader _reader;

		private readonly int _lastRowId;

		private int _currentRowId;

		private const int EnumEnded = 16777216;

		public System.Reflection.Metadata.MethodDefinitionHandle Current
		{
			get
			{
				if (_reader.UseMethodPtrTable)
				{
					return GetCurrentMethodIndirect();
				}
				return System.Reflection.Metadata.MethodDefinitionHandle.FromRowId((int)((long)_currentRowId & 0xFFFFFFL));
			}
		}

		object IEnumerator.Current => Current;

		internal Enumerator(System.Reflection.Metadata.MetadataReader reader, int firstRowId, int lastRowId)
		{
			_reader = reader;
			_currentRowId = firstRowId - 1;
			_lastRowId = lastRowId;
		}

		private System.Reflection.Metadata.MethodDefinitionHandle GetCurrentMethodIndirect()
		{
			return _reader.MethodPtrTable.GetMethodFor(_currentRowId & 0xFFFFFF);
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

	internal MethodDefinitionHandleCollection(System.Reflection.Metadata.MetadataReader reader)
	{
		_reader = reader;
		_firstRowId = 1;
		_lastRowId = reader.MethodDefTable.NumberOfRows;
	}

	internal MethodDefinitionHandleCollection(System.Reflection.Metadata.MetadataReader reader, TypeDefinitionHandle containingType)
	{
		_reader = reader;
		reader.GetMethodRange(containingType, out _firstRowId, out _lastRowId);
	}

	public Enumerator GetEnumerator()
	{
		return new Enumerator(_reader, _firstRowId, _lastRowId);
	}

	IEnumerator<System.Reflection.Metadata.MethodDefinitionHandle> IEnumerable<System.Reflection.Metadata.MethodDefinitionHandle>.GetEnumerator()
	{
		return GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}
}
