using System.Collections;
using System.Collections.Generic;

namespace System.Reflection.Metadata;

internal readonly struct EventDefinitionHandleCollection : IReadOnlyCollection<EventDefinitionHandle>, IEnumerable<EventDefinitionHandle>, IEnumerable
{
	public struct Enumerator : IEnumerator<EventDefinitionHandle>, IDisposable, IEnumerator
	{
		private readonly System.Reflection.Metadata.MetadataReader _reader;

		private readonly int _lastRowId;

		private int _currentRowId;

		private const int EnumEnded = 16777216;

		public EventDefinitionHandle Current
		{
			get
			{
				if (_reader.UseEventPtrTable)
				{
					return GetCurrentEventIndirect();
				}
				return EventDefinitionHandle.FromRowId((int)((long)_currentRowId & 0xFFFFFFL));
			}
		}

		object IEnumerator.Current => Current;

		internal Enumerator(System.Reflection.Metadata.MetadataReader reader, int firstRowId, int lastRowId)
		{
			_reader = reader;
			_currentRowId = firstRowId - 1;
			_lastRowId = lastRowId;
		}

		private EventDefinitionHandle GetCurrentEventIndirect()
		{
			return _reader.EventPtrTable.GetEventFor(_currentRowId & 0xFFFFFF);
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

	internal EventDefinitionHandleCollection(System.Reflection.Metadata.MetadataReader reader)
	{
		_reader = reader;
		_firstRowId = 1;
		_lastRowId = reader.EventTable.NumberOfRows;
	}

	internal EventDefinitionHandleCollection(System.Reflection.Metadata.MetadataReader reader, TypeDefinitionHandle containingType)
	{
		_reader = reader;
		reader.GetEventRange(containingType, out _firstRowId, out _lastRowId);
	}

	public Enumerator GetEnumerator()
	{
		return new Enumerator(_reader, _firstRowId, _lastRowId);
	}

	IEnumerator<EventDefinitionHandle> IEnumerable<EventDefinitionHandle>.GetEnumerator()
	{
		return GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}
}
