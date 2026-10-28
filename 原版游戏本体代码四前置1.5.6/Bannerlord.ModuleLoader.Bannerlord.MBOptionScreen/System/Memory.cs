using System.Buffers;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System;

[DebuggerTypeProxy(typeof(MemoryDebugView<>))]
[DebuggerDisplay("{ToString(),raw}")]
internal readonly struct Memory<T>
{
	private readonly object _object;

	private readonly int _index;

	private readonly int _length;

	private const int RemoveFlagsBitMask = int.MaxValue;

	public static Memory<T> Empty => default(Memory<T>);

	public int Length => _length & 0x7FFFFFFF;

	public bool IsEmpty => (_length & 0x7FFFFFFF) == 0;

	public Span<T> Span
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			Span<T> result;
			if (_index < 0)
			{
				result = ((MemoryManager<T>)_object).GetSpan();
				return result.Slice(_index & 0x7FFFFFFF, _length);
			}
			if (typeof(T) == typeof(char) && _object is string text)
			{
				result = new Span<T>(Unsafe.As<Pinnable<T>>(text), MemoryExtensions.StringAdjustment, text.Length);
				return result.Slice(_index, _length);
			}
			if (_object != null)
			{
				return new Span<T>((T[])_object, _index, _length & 0x7FFFFFFF);
			}
			result = default(Span<T>);
			return result;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Memory(T[] array)
	{
		if (array == null)
		{
			this = default(Memory<T>);
			return;
		}
		if (default(T) == null && array.GetType() != typeof(T[]))
		{
			System.ThrowHelper.ThrowArrayTypeMismatchException();
		}
		_object = array;
		_index = 0;
		_length = array.Length;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal Memory(T[] array, int start)
	{
		if (array == null)
		{
			if (start != 0)
			{
				System.ThrowHelper.ThrowArgumentOutOfRangeException();
			}
			this = default(Memory<T>);
			return;
		}
		if (default(T) == null && array.GetType() != typeof(T[]))
		{
			System.ThrowHelper.ThrowArrayTypeMismatchException();
		}
		if ((uint)start > (uint)array.Length)
		{
			System.ThrowHelper.ThrowArgumentOutOfRangeException();
		}
		_object = array;
		_index = start;
		_length = array.Length - start;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Memory(T[] array, int start, int length)
	{
		if (array == null)
		{
			if (start != 0 || length != 0)
			{
				System.ThrowHelper.ThrowArgumentOutOfRangeException();
			}
			this = default(Memory<T>);
			return;
		}
		if (default(T) == null && array.GetType() != typeof(T[]))
		{
			System.ThrowHelper.ThrowArrayTypeMismatchException();
		}
		if ((uint)start > (uint)array.Length || (uint)length > (uint)(array.Length - start))
		{
			System.ThrowHelper.ThrowArgumentOutOfRangeException();
		}
		_object = array;
		_index = start;
		_length = length;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal Memory(MemoryManager<T> manager, int length)
	{
		if (length < 0)
		{
			System.ThrowHelper.ThrowArgumentOutOfRangeException();
		}
		_object = manager;
		_index = int.MinValue;
		_length = length;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal Memory(MemoryManager<T> manager, int start, int length)
	{
		if (length < 0 || start < 0)
		{
			System.ThrowHelper.ThrowArgumentOutOfRangeException();
		}
		_object = manager;
		_index = start | int.MinValue;
		_length = length;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal Memory(object obj, int start, int length)
	{
		_object = obj;
		_index = start;
		_length = length;
	}

	public static implicit operator Memory<T>(T[] array)
	{
		return new Memory<T>(array);
	}

	public static implicit operator Memory<T>(ArraySegment<T> segment)
	{
		return new Memory<T>(segment.Array, segment.Offset, segment.Count);
	}

	public static implicit operator ReadOnlyMemory<T>(Memory<T> memory)
	{
		return Unsafe.As<Memory<T>, ReadOnlyMemory<T>>(ref memory);
	}

	public override string ToString()
	{
		if (typeof(T) == typeof(char))
		{
			if (!(_object is string text))
			{
				return Span.ToString();
			}
			return text.Substring(_index, _length & 0x7FFFFFFF);
		}
		return $"System.Memory<{typeof(T).Name}>[{_length & 0x7FFFFFFF}]";
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Memory<T> Slice(int start)
	{
		int length = _length;
		int num = length & 0x7FFFFFFF;
		if ((uint)start > (uint)num)
		{
			System.ThrowHelper.ThrowArgumentOutOfRangeException(System.ExceptionArgument.start);
		}
		return new Memory<T>(_object, _index + start, length - start);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Memory<T> Slice(int start, int length)
	{
		int length2 = _length;
		int num = length2 & 0x7FFFFFFF;
		if ((uint)start > (uint)num || (uint)length > (uint)(num - start))
		{
			System.ThrowHelper.ThrowArgumentOutOfRangeException();
		}
		return new Memory<T>(_object, _index + start, length | (length2 & int.MinValue));
	}

	public void CopyTo(Memory<T> destination)
	{
		Span.CopyTo(destination.Span);
	}

	public bool TryCopyTo(Memory<T> destination)
	{
		return Span.TryCopyTo(destination.Span);
	}

	public unsafe MemoryHandle Pin()
	{
		if (_index < 0)
		{
			return ((MemoryManager<T>)_object).Pin(_index & 0x7FFFFFFF);
		}
		if (typeof(T) == typeof(char) && _object is string value)
		{
			GCHandle handle = GCHandle.Alloc(value, GCHandleType.Pinned);
			void* pointer = Unsafe.Add<T>((void*)handle.AddrOfPinnedObject(), _index);
			return new MemoryHandle(pointer, handle);
		}
		if (_object is T[] array)
		{
			if (_length < 0)
			{
				void* pointer2 = Unsafe.Add<T>(Unsafe.AsPointer(ref MemoryMarshal.GetReference((Span<T>)array)), _index);
				return new MemoryHandle(pointer2);
			}
			GCHandle handle2 = GCHandle.Alloc(array, GCHandleType.Pinned);
			void* pointer3 = Unsafe.Add<T>((void*)handle2.AddrOfPinnedObject(), _index);
			return new MemoryHandle(pointer3, handle2);
		}
		return default(MemoryHandle);
	}

	public T[] ToArray()
	{
		return Span.ToArray();
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	public override bool Equals(object obj)
	{
		if (obj is ReadOnlyMemory<T> readOnlyMemory)
		{
			return readOnlyMemory.Equals(this);
		}
		if (obj is Memory<T> other)
		{
			return Equals(other);
		}
		return false;
	}

	public bool Equals(Memory<T> other)
	{
		if (_object == other._object && _index == other._index)
		{
			return _length == other._length;
		}
		return false;
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	public override int GetHashCode()
	{
		if (_object == null)
		{
			return 0;
		}
		return CombineHashCodes(_object.GetHashCode(), _index.GetHashCode(), _length.GetHashCode());
	}

	private static int CombineHashCodes(int left, int right)
	{
		return ((left << 5) + left) ^ right;
	}

	private static int CombineHashCodes(int h1, int h2, int h3)
	{
		return CombineHashCodes(CombineHashCodes(h1, h2), h3);
	}
}
