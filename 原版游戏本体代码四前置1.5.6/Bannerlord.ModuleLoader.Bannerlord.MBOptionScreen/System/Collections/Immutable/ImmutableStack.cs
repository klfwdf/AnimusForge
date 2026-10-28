using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;

namespace System.Collections.Immutable;

internal static class ImmutableStack
{
	public static ImmutableStack<T> Create<T>()
	{
		return ImmutableStack<T>.Empty;
	}

	public static ImmutableStack<T> Create<T>(T item)
	{
		return ImmutableStack<T>.Empty.Push(item);
	}

	public static ImmutableStack<T> CreateRange<T>(IEnumerable<T> items)
	{
		Requires.NotNull(items, "items");
		ImmutableStack<T> immutableStack = ImmutableStack<T>.Empty;
		foreach (T item in items)
		{
			immutableStack = immutableStack.Push(item);
		}
		return immutableStack;
	}

	public static ImmutableStack<T> Create<T>(params T[] items)
	{
		Requires.NotNull(items, "items");
		return Create((ReadOnlySpan<T>)items);
	}

	public static ImmutableStack<T> Create<T>(params ReadOnlySpan<T> items)
	{
		ImmutableStack<T> immutableStack = ImmutableStack<T>.Empty;
		ReadOnlySpan<T> readOnlySpan = items;
		for (int i = 0; i < readOnlySpan.Length; i++)
		{
			T value = readOnlySpan[i];
			immutableStack = immutableStack.Push(value);
		}
		return immutableStack;
	}

	public static IImmutableStack<T> Pop<T>(this IImmutableStack<T> stack, out T value)
	{
		Requires.NotNull(stack, "stack");
		value = stack.Peek();
		return stack.Pop();
	}
}
[CollectionBuilder(typeof(ImmutableStack), "Create")]
[DebuggerDisplay("IsEmpty = {IsEmpty}, Top = {_head}")]
[DebuggerTypeProxy(typeof(ImmutableEnumerableDebuggerProxy<>))]
internal sealed class ImmutableStack<T> : IImmutableStack<T>, IEnumerable<T>, IEnumerable
{
	[EditorBrowsable(EditorBrowsableState.Advanced)]
	public struct Enumerator
	{
		private readonly ImmutableStack<T> _originalStack;

		private ImmutableStack<T> _remainingStack;

		public T Current
		{
			get
			{
				if (_remainingStack == null || _remainingStack.IsEmpty)
				{
					throw new InvalidOperationException();
				}
				return _remainingStack.Peek();
			}
		}

		internal Enumerator(ImmutableStack<T> stack)
		{
			Requires.NotNull(stack, "stack");
			_originalStack = stack;
			_remainingStack = null;
		}

		public bool MoveNext()
		{
			if (_remainingStack == null)
			{
				_remainingStack = _originalStack;
			}
			else if (!_remainingStack.IsEmpty)
			{
				_remainingStack = _remainingStack.Pop();
			}
			return !_remainingStack.IsEmpty;
		}
	}

	private sealed class EnumeratorObject : IEnumerator<T>, IDisposable, IEnumerator
	{
		private readonly ImmutableStack<T> _originalStack;

		private ImmutableStack<T> _remainingStack;

		private bool _disposed;

		public T Current
		{
			get
			{
				ThrowIfDisposed();
				if (_remainingStack == null || _remainingStack.IsEmpty)
				{
					throw new InvalidOperationException();
				}
				return _remainingStack.Peek();
			}
		}

		object IEnumerator.Current => Current;

		internal EnumeratorObject(ImmutableStack<T> stack)
		{
			Requires.NotNull(stack, "stack");
			_originalStack = stack;
		}

		public bool MoveNext()
		{
			ThrowIfDisposed();
			if (_remainingStack == null)
			{
				_remainingStack = _originalStack;
			}
			else if (!_remainingStack.IsEmpty)
			{
				_remainingStack = _remainingStack.Pop();
			}
			return !_remainingStack.IsEmpty;
		}

		public void Reset()
		{
			ThrowIfDisposed();
			_remainingStack = null;
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

	private static readonly ImmutableStack<T> s_EmptyField = new ImmutableStack<T>();

	private readonly T _head;

	private readonly ImmutableStack<T> _tail;

	public static ImmutableStack<T> Empty => s_EmptyField;

	public bool IsEmpty => _tail == null;

	private ImmutableStack()
	{
	}

	private ImmutableStack(T head, ImmutableStack<T> tail)
	{
		_head = head;
		_tail = tail;
	}

	public ImmutableStack<T> Clear()
	{
		return Empty;
	}

	IImmutableStack<T> IImmutableStack<T>.Clear()
	{
		return Clear();
	}

	public T Peek()
	{
		if (IsEmpty)
		{
			throw new InvalidOperationException(_003Cfea68302_002Dff1f_002D430a_002Da79d_002Df3b3dc787afe_003ESR.InvalidEmptyOperation);
		}
		return _head;
	}

	public ref readonly T PeekRef()
	{
		if (IsEmpty)
		{
			throw new InvalidOperationException(_003Cfea68302_002Dff1f_002D430a_002Da79d_002Df3b3dc787afe_003ESR.InvalidEmptyOperation);
		}
		return ref _head;
	}

	public ImmutableStack<T> Push(T value)
	{
		return new ImmutableStack<T>(value, this);
	}

	IImmutableStack<T> IImmutableStack<T>.Push(T value)
	{
		return Push(value);
	}

	public ImmutableStack<T> Pop()
	{
		if (IsEmpty)
		{
			throw new InvalidOperationException(_003Cfea68302_002Dff1f_002D430a_002Da79d_002Df3b3dc787afe_003ESR.InvalidEmptyOperation);
		}
		return _tail;
	}

	public ImmutableStack<T> Pop(out T value)
	{
		value = Peek();
		return Pop();
	}

	IImmutableStack<T> IImmutableStack<T>.Pop()
	{
		return Pop();
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

	internal ImmutableStack<T> Reverse()
	{
		ImmutableStack<T> immutableStack = Clear();
		ImmutableStack<T> immutableStack2 = this;
		while (!immutableStack2.IsEmpty)
		{
			immutableStack = immutableStack.Push(immutableStack2.Peek());
			immutableStack2 = immutableStack2.Pop();
		}
		return immutableStack;
	}
}
