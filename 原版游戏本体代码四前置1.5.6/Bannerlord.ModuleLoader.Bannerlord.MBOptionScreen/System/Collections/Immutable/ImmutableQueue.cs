using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;

namespace System.Collections.Immutable;

internal static class ImmutableQueue
{
	public static ImmutableQueue<T> Create<T>()
	{
		return ImmutableQueue<T>.Empty;
	}

	public static ImmutableQueue<T> Create<T>(T item)
	{
		return ImmutableQueue<T>.Empty.Enqueue(item);
	}

	public static ImmutableQueue<T> CreateRange<T>(IEnumerable<T> items)
	{
		Requires.NotNull(items, "items");
		if (items is T[] items2)
		{
			return Create(items2);
		}
		using IEnumerator<T> enumerator = items.GetEnumerator();
		if (!enumerator.MoveNext())
		{
			return ImmutableQueue<T>.Empty;
		}
		ImmutableStack<T> forwards = ImmutableStack.Create(enumerator.Current);
		ImmutableStack<T> immutableStack = ImmutableStack<T>.Empty;
		while (enumerator.MoveNext())
		{
			immutableStack = immutableStack.Push(enumerator.Current);
		}
		return new ImmutableQueue<T>(forwards, immutableStack);
	}

	public static ImmutableQueue<T> Create<T>(params T[] items)
	{
		Requires.NotNull(items, "items");
		return Create((ReadOnlySpan<T>)items);
	}

	public static ImmutableQueue<T> Create<T>(params ReadOnlySpan<T> items)
	{
		if (items.IsEmpty)
		{
			return ImmutableQueue<T>.Empty;
		}
		ImmutableStack<T> immutableStack = ImmutableStack<T>.Empty;
		for (int num = items.Length - 1; num >= 0; num--)
		{
			immutableStack = immutableStack.Push(items[num]);
		}
		return new ImmutableQueue<T>(immutableStack, ImmutableStack<T>.Empty);
	}

	public static IImmutableQueue<T> Dequeue<T>(this IImmutableQueue<T> queue, out T value)
	{
		Requires.NotNull(queue, "queue");
		value = queue.Peek();
		return queue.Dequeue();
	}
}
[CollectionBuilder(typeof(ImmutableQueue), "Create")]
[DebuggerDisplay("IsEmpty = {IsEmpty}")]
[DebuggerTypeProxy(typeof(ImmutableEnumerableDebuggerProxy<>))]
internal sealed class ImmutableQueue<T> : IImmutableQueue<T>, IEnumerable<T>, IEnumerable
{
	[EditorBrowsable(EditorBrowsableState.Advanced)]
	public struct Enumerator
	{
		private readonly ImmutableQueue<T> _originalQueue;

		private ImmutableStack<T> _remainingForwardsStack;

		private ImmutableStack<T> _remainingBackwardsStack;

		public T Current
		{
			get
			{
				if (_remainingForwardsStack == null)
				{
					throw new InvalidOperationException();
				}
				if (!_remainingForwardsStack.IsEmpty)
				{
					return _remainingForwardsStack.Peek();
				}
				if (!_remainingBackwardsStack.IsEmpty)
				{
					return _remainingBackwardsStack.Peek();
				}
				throw new InvalidOperationException();
			}
		}

		internal Enumerator(ImmutableQueue<T> queue)
		{
			_originalQueue = queue;
			_remainingForwardsStack = null;
			_remainingBackwardsStack = null;
		}

		public bool MoveNext()
		{
			if (_remainingForwardsStack == null)
			{
				_remainingForwardsStack = _originalQueue._forwards;
				_remainingBackwardsStack = _originalQueue.BackwardsReversed;
			}
			else if (!_remainingForwardsStack.IsEmpty)
			{
				_remainingForwardsStack = _remainingForwardsStack.Pop();
			}
			else if (!_remainingBackwardsStack.IsEmpty)
			{
				_remainingBackwardsStack = _remainingBackwardsStack.Pop();
			}
			if (_remainingForwardsStack.IsEmpty)
			{
				return !_remainingBackwardsStack.IsEmpty;
			}
			return true;
		}
	}

	private sealed class EnumeratorObject : IEnumerator<T>, IDisposable, IEnumerator
	{
		private readonly ImmutableQueue<T> _originalQueue;

		private ImmutableStack<T> _remainingForwardsStack;

		private ImmutableStack<T> _remainingBackwardsStack;

		private bool _disposed;

		public T Current
		{
			get
			{
				ThrowIfDisposed();
				if (_remainingForwardsStack == null)
				{
					throw new InvalidOperationException();
				}
				if (!_remainingForwardsStack.IsEmpty)
				{
					return _remainingForwardsStack.Peek();
				}
				if (!_remainingBackwardsStack.IsEmpty)
				{
					return _remainingBackwardsStack.Peek();
				}
				throw new InvalidOperationException();
			}
		}

		object IEnumerator.Current => Current;

		internal EnumeratorObject(ImmutableQueue<T> queue)
		{
			_originalQueue = queue;
		}

		public bool MoveNext()
		{
			ThrowIfDisposed();
			if (_remainingForwardsStack == null)
			{
				_remainingForwardsStack = _originalQueue._forwards;
				_remainingBackwardsStack = _originalQueue.BackwardsReversed;
			}
			else if (!_remainingForwardsStack.IsEmpty)
			{
				_remainingForwardsStack = _remainingForwardsStack.Pop();
			}
			else if (!_remainingBackwardsStack.IsEmpty)
			{
				_remainingBackwardsStack = _remainingBackwardsStack.Pop();
			}
			if (_remainingForwardsStack.IsEmpty)
			{
				return !_remainingBackwardsStack.IsEmpty;
			}
			return true;
		}

		public void Reset()
		{
			ThrowIfDisposed();
			_remainingBackwardsStack = null;
			_remainingForwardsStack = null;
		}

		public void Dispose()
		{
			_disposed = true;
		}

		private void ThrowIfDisposed()
		{
			if (_disposed)
			{
				Requires.FailObjectDisposed(this);
			}
		}
	}

	private static readonly ImmutableQueue<T> s_EmptyField = new ImmutableQueue<T>(ImmutableStack<T>.Empty, ImmutableStack<T>.Empty);

	private readonly ImmutableStack<T> _backwards;

	private readonly ImmutableStack<T> _forwards;

	private ImmutableStack<T> _backwardsReversed;

	public bool IsEmpty => _forwards.IsEmpty;

	public static ImmutableQueue<T> Empty => s_EmptyField;

	private ImmutableStack<T> BackwardsReversed
	{
		get
		{
			if (_backwardsReversed == null)
			{
				_backwardsReversed = _backwards.Reverse();
			}
			return _backwardsReversed;
		}
	}

	internal ImmutableQueue(ImmutableStack<T> forwards, ImmutableStack<T> backwards)
	{
		_forwards = forwards;
		_backwards = backwards;
	}

	public ImmutableQueue<T> Clear()
	{
		return Empty;
	}

	IImmutableQueue<T> IImmutableQueue<T>.Clear()
	{
		return Clear();
	}

	public T Peek()
	{
		if (IsEmpty)
		{
			throw new InvalidOperationException(_003Cfea68302_002Dff1f_002D430a_002Da79d_002Df3b3dc787afe_003ESR.InvalidEmptyOperation);
		}
		return _forwards.Peek();
	}

	public ref readonly T PeekRef()
	{
		if (IsEmpty)
		{
			throw new InvalidOperationException(_003Cfea68302_002Dff1f_002D430a_002Da79d_002Df3b3dc787afe_003ESR.InvalidEmptyOperation);
		}
		return ref _forwards.PeekRef();
	}

	public ImmutableQueue<T> Enqueue(T value)
	{
		if (IsEmpty)
		{
			return new ImmutableQueue<T>(ImmutableStack.Create(value), ImmutableStack<T>.Empty);
		}
		return new ImmutableQueue<T>(_forwards, _backwards.Push(value));
	}

	IImmutableQueue<T> IImmutableQueue<T>.Enqueue(T value)
	{
		return Enqueue(value);
	}

	public ImmutableQueue<T> Dequeue()
	{
		if (IsEmpty)
		{
			throw new InvalidOperationException(_003Cfea68302_002Dff1f_002D430a_002Da79d_002Df3b3dc787afe_003ESR.InvalidEmptyOperation);
		}
		ImmutableStack<T> immutableStack = _forwards.Pop();
		if (!immutableStack.IsEmpty)
		{
			return new ImmutableQueue<T>(immutableStack, _backwards);
		}
		if (_backwards.IsEmpty)
		{
			return Empty;
		}
		return new ImmutableQueue<T>(BackwardsReversed, ImmutableStack<T>.Empty);
	}

	public ImmutableQueue<T> Dequeue(out T value)
	{
		value = Peek();
		return Dequeue();
	}

	IImmutableQueue<T> IImmutableQueue<T>.Dequeue()
	{
		return Dequeue();
	}

	public Enumerator GetEnumerator()
	{
		return new Enumerator(this);
	}

	IEnumerator<T> IEnumerable<T>.GetEnumerator()
	{
		if (!IsEmpty)
		{
			return new EnumeratorObject(this);
		}
		return Enumerable.Empty<T>().GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return new EnumeratorObject(this);
	}
}
