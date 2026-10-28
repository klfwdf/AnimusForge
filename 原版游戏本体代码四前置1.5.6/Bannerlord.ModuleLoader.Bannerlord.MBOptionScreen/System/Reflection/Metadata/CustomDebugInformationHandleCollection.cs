using System.Collections;
using System.Collections.Generic;

namespace System.Reflection.Metadata;

internal readonly struct CustomDebugInformationHandleCollection : IReadOnlyCollection<CustomDebugInformationHandle>, IEnumerable<CustomDebugInformationHandle>, IEnumerable
{
	public struct Enumerator : IEnumerator<CustomDebugInformationHandle>, IDisposable, IEnumerator
	{
		private readonly System.Reflection.Metadata.MetadataReader _reader;

		private readonly int _lastRowId;

		private int _currentRowId;

		private const int EnumEnded = 16777216;

		public CustomDebugInformationHandle Current => CustomDebugInformationHandle.FromRowId((int)((long)_currentRowId & 0xFFFFFFL));

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

	internal CustomDebugInformationHandleCollection(System.Reflection.Metadata.MetadataReader reader)
	{
		_reader = reader;
		_firstRowId = 1;
		_lastRowId = reader.CustomDebugInformationTable.NumberOfRows;
	}

	internal CustomDebugInformationHandleCollection(System.Reflection.Metadata.MetadataReader reader, EntityHandle handle)
	{
		_reader = reader;
		reader.CustomDebugInformationTable.GetRange(handle, out _firstRowId, out _lastRowId);
	}

	public Enumerator GetEnumerator()
	{
		return new Enumerator(_reader, _firstRowId, _lastRowId);
	}

	IEnumerator<CustomDebugInformationHandle> IEnumerable<CustomDebugInformationHandle>.GetEnumerator()
	{
		return GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}
}
