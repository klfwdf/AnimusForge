using System.Collections;
using System.Collections.Generic;

namespace System.Reflection.Metadata;

internal readonly struct ParameterHandleCollection : IReadOnlyCollection<ParameterHandle>, IEnumerable<ParameterHandle>, IEnumerable
{
	public struct Enumerator : IEnumerator<ParameterHandle>, IDisposable, IEnumerator
	{
		private readonly System.Reflection.Metadata.MetadataReader _reader;

		private readonly int _lastRowId;

		private int _currentRowId;

		private const int EnumEnded = 16777216;

		public ParameterHandle Current
		{
			get
			{
				if (_reader.UseParamPtrTable)
				{
					return GetCurrentParameterIndirect();
				}
				return ParameterHandle.FromRowId((int)((long)_currentRowId & 0xFFFFFFL));
			}
		}

		object IEnumerator.Current => Current;

		internal Enumerator(System.Reflection.Metadata.MetadataReader reader, int firstRowId, int lastRowId)
		{
			_reader = reader;
			_lastRowId = lastRowId;
			_currentRowId = firstRowId - 1;
		}

		private ParameterHandle GetCurrentParameterIndirect()
		{
			return _reader.ParamPtrTable.GetParamFor(_currentRowId & 0xFFFFFF);
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

	internal ParameterHandleCollection(System.Reflection.Metadata.MetadataReader reader, System.Reflection.Metadata.MethodDefinitionHandle containingMethod)
	{
		_reader = reader;
		reader.GetParameterRange(containingMethod, out _firstRowId, out _lastRowId);
	}

	public Enumerator GetEnumerator()
	{
		return new Enumerator(_reader, _firstRowId, _lastRowId);
	}

	IEnumerator<ParameterHandle> IEnumerable<ParameterHandle>.GetEnumerator()
	{
		return GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}
}
