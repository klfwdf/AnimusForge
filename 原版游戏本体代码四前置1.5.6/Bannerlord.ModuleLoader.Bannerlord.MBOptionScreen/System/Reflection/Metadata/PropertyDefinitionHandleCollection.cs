using System.Collections;
using System.Collections.Generic;

namespace System.Reflection.Metadata;

internal readonly struct PropertyDefinitionHandleCollection : IReadOnlyCollection<PropertyDefinitionHandle>, IEnumerable<PropertyDefinitionHandle>, IEnumerable
{
	public struct Enumerator : IEnumerator<PropertyDefinitionHandle>, IDisposable, IEnumerator
	{
		private readonly System.Reflection.Metadata.MetadataReader _reader;

		private readonly int _lastRowId;

		private int _currentRowId;

		private const int EnumEnded = 16777216;

		public PropertyDefinitionHandle Current
		{
			get
			{
				if (_reader.UsePropertyPtrTable)
				{
					return GetCurrentPropertyIndirect();
				}
				return PropertyDefinitionHandle.FromRowId((int)((long)_currentRowId & 0xFFFFFFL));
			}
		}

		object IEnumerator.Current => Current;

		internal Enumerator(System.Reflection.Metadata.MetadataReader reader, int firstRowId, int lastRowId)
		{
			_reader = reader;
			_currentRowId = firstRowId - 1;
			_lastRowId = lastRowId;
		}

		private PropertyDefinitionHandle GetCurrentPropertyIndirect()
		{
			return _reader.PropertyPtrTable.GetPropertyFor(_currentRowId & 0xFFFFFF);
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

	internal PropertyDefinitionHandleCollection(System.Reflection.Metadata.MetadataReader reader)
	{
		_reader = reader;
		_firstRowId = 1;
		_lastRowId = reader.PropertyTable.NumberOfRows;
	}

	internal PropertyDefinitionHandleCollection(System.Reflection.Metadata.MetadataReader reader, TypeDefinitionHandle containingType)
	{
		_reader = reader;
		reader.GetPropertyRange(containingType, out _firstRowId, out _lastRowId);
	}

	public Enumerator GetEnumerator()
	{
		return new Enumerator(_reader, _firstRowId, _lastRowId);
	}

	IEnumerator<PropertyDefinitionHandle> IEnumerable<PropertyDefinitionHandle>.GetEnumerator()
	{
		return GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}
}
