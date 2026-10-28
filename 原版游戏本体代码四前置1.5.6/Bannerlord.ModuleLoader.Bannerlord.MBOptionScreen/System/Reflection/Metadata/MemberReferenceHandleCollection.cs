using System.Collections;
using System.Collections.Generic;

namespace System.Reflection.Metadata;

internal readonly struct MemberReferenceHandleCollection : IReadOnlyCollection<MemberReferenceHandle>, IEnumerable<MemberReferenceHandle>, IEnumerable
{
	public struct Enumerator : IEnumerator<MemberReferenceHandle>, IDisposable, IEnumerator
	{
		private readonly int _lastRowId;

		private int _currentRowId;

		private const int EnumEnded = 16777216;

		public MemberReferenceHandle Current => MemberReferenceHandle.FromRowId((int)((long)_currentRowId & 0xFFFFFFL));

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

	internal MemberReferenceHandleCollection(int lastRowId)
	{
		_lastRowId = lastRowId;
	}

	public Enumerator GetEnumerator()
	{
		return new Enumerator(_lastRowId);
	}

	IEnumerator<MemberReferenceHandle> IEnumerable<MemberReferenceHandle>.GetEnumerator()
	{
		return GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}
}
