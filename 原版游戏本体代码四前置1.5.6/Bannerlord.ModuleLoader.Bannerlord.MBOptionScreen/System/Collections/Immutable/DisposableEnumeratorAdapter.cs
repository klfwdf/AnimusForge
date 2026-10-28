using System.Collections.Generic;

namespace System.Collections.Immutable;

internal struct DisposableEnumeratorAdapter<T, TEnumerator> : IDisposable where TEnumerator : struct, IEnumerator<T>
{
	private readonly IEnumerator<T> _enumeratorObject;

	private TEnumerator _enumeratorStruct;

	public T Current
	{
		get
		{
			if (_enumeratorObject == null)
			{
				return _enumeratorStruct.Current;
			}
			return _enumeratorObject.Current;
		}
	}

	internal DisposableEnumeratorAdapter(TEnumerator enumerator)
	{
		_enumeratorStruct = enumerator;
		_enumeratorObject = null;
	}

	internal DisposableEnumeratorAdapter(IEnumerator<T> enumerator)
	{
		_enumeratorStruct = default(TEnumerator);
		_enumeratorObject = enumerator;
	}

	public bool MoveNext()
	{
		if (_enumeratorObject == null)
		{
			return _enumeratorStruct.MoveNext();
		}
		return _enumeratorObject.MoveNext();
	}

	public void Dispose()
	{
		if (_enumeratorObject != null)
		{
			_enumeratorObject.Dispose();
		}
		else
		{
			_enumeratorStruct.Dispose();
		}
	}

	public DisposableEnumeratorAdapter<T, TEnumerator> GetEnumerator()
	{
		return this;
	}
}
