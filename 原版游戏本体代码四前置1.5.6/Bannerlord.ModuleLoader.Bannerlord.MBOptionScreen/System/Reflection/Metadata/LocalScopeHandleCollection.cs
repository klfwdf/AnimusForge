using System.Collections;
using System.Collections.Generic;

namespace System.Reflection.Metadata;

internal readonly struct LocalScopeHandleCollection : IReadOnlyCollection<LocalScopeHandle>, IEnumerable<LocalScopeHandle>, IEnumerable
{
	public struct Enumerator : IEnumerator<LocalScopeHandle>, IDisposable, IEnumerator
	{
		private readonly System.Reflection.Metadata.MetadataReader _reader;

		private readonly int _lastRowId;

		private int _currentRowId;

		private const int EnumEnded = 16777216;

		public LocalScopeHandle Current => LocalScopeHandle.FromRowId((int)((long)_currentRowId & 0xFFFFFFL));

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

	public struct ChildrenEnumerator : IEnumerator<LocalScopeHandle>, IDisposable, IEnumerator
	{
		private readonly System.Reflection.Metadata.MetadataReader _reader;

		private readonly int _parentEndOffset;

		private readonly int _parentRowId;

		private readonly System.Reflection.Metadata.MethodDefinitionHandle _parentMethodRowId;

		private int _currentRowId;

		private const int EnumEnded = 16777216;

		public LocalScopeHandle Current => LocalScopeHandle.FromRowId((int)((long)_currentRowId & 0xFFFFFFL));

		object IEnumerator.Current => Current;

		internal ChildrenEnumerator(System.Reflection.Metadata.MetadataReader reader, int parentRowId)
		{
			_reader = reader;
			_parentEndOffset = reader.LocalScopeTable.GetEndOffset(parentRowId);
			_parentMethodRowId = reader.LocalScopeTable.GetMethod(parentRowId);
			_currentRowId = 0;
			_parentRowId = parentRowId;
		}

		public bool MoveNext()
		{
			int currentRowId = _currentRowId;
			int num;
			int num2;
			switch (currentRowId)
			{
			case 16777216:
				return false;
			case 0:
				num = -1;
				num2 = _parentRowId + 1;
				break;
			default:
				num = _reader.LocalScopeTable.GetEndOffset(currentRowId);
				num2 = currentRowId + 1;
				break;
			}
			int numberOfRows = _reader.LocalScopeTable.NumberOfRows;
			int endOffset;
			while (true)
			{
				if (num2 > numberOfRows || _parentMethodRowId != _reader.LocalScopeTable.GetMethod(num2))
				{
					_currentRowId = 16777216;
					return false;
				}
				endOffset = _reader.LocalScopeTable.GetEndOffset(num2);
				if (endOffset > num)
				{
					break;
				}
				num2++;
			}
			if (endOffset > _parentEndOffset)
			{
				_currentRowId = 16777216;
				return false;
			}
			_currentRowId = num2;
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

	internal LocalScopeHandleCollection(System.Reflection.Metadata.MetadataReader reader, int methodDefinitionRowId)
	{
		_reader = reader;
		if (methodDefinitionRowId == 0)
		{
			_firstRowId = 1;
			_lastRowId = reader.LocalScopeTable.NumberOfRows;
		}
		else
		{
			reader.LocalScopeTable.GetLocalScopeRange(methodDefinitionRowId, out _firstRowId, out _lastRowId);
		}
	}

	public Enumerator GetEnumerator()
	{
		return new Enumerator(_reader, _firstRowId, _lastRowId);
	}

	IEnumerator<LocalScopeHandle> IEnumerable<LocalScopeHandle>.GetEnumerator()
	{
		return GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}
}
