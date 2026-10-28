using System.Collections;
using System.Collections.Generic;

namespace System.Reflection.Metadata;

internal readonly struct ExportedTypeHandleCollection : IReadOnlyCollection<ExportedTypeHandle>, IEnumerable<ExportedTypeHandle>, IEnumerable
{
	public struct Enumerator : IEnumerator<ExportedTypeHandle>, IDisposable, IEnumerator
	{
		private readonly int _lastRowId;

		private int _currentRowId;

		private const int EnumEnded = 16777216;

		public ExportedTypeHandle Current => ExportedTypeHandle.FromRowId((int)((long)_currentRowId & 0xFFFFFFL));

		object IEnumerator.Current => Current;

		internal Enumerator(int lastRowId)
		{
			_lastRowId = lastRowId;
			_currentRowId = 0;
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

	private readonly int _lastRowId;

	public int Count => _lastRowId;

	internal ExportedTypeHandleCollection(int lastRowId)
	{
		_lastRowId = lastRowId;
	}

	public Enumerator GetEnumerator()
	{
		return new Enumerator(_lastRowId);
	}

	IEnumerator<ExportedTypeHandle> IEnumerable<ExportedTypeHandle>.GetEnumerator()
	{
		return GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}
}
