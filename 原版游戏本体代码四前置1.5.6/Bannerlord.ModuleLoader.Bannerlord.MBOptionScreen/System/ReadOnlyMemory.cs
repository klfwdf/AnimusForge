using System.Buffers;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System;

[DebuggerTypeProxy(typeof(MemoryDebugView<>))]
[DebuggerDisplay("{ToString(),raw}")]
internal readonly struct ReadOnlyMemory<T>
{
	private readonly object _object;

	private readonly int _index;

	private readonly int _length;

	internal const int RemoveFlagsBitMask = int.MaxValue;

	public static ReadOnlyMemory<T> Empty => default(ReadOnlyMemory<T>);

	public int Length => _length & 0x7FFFFFFF;

	public bool IsEmpty => (_length & 0x7FFFFFFF) == 0;

	public ReadOnlySpan<T> Span
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			if (_index < 0)
			{
				return ((MemoryManager<T>)_object).GetSpan().Slice(_index & 0x7FFFFFFF, _length);
			}
			ReadOnlySpan<T> result;
			if (typeof(T) == typeof(char) && _object is string text)
			{
				result = new ReadOnlySpan<T>(Unsafe.As<Pinnable<T>>(text), MemoryExtensions.StringAdjustment, text.Length);
				return result.Slice(_index, _length);
			}
			if (_object != null)
			{
				return new ReadOnlySpan<T>((T[])_object, _index, _length & 0x7FFFFFFF);
			}
			result = default(ReadOnlySpan<T>);
			return result;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ReadOnlyMemory(T[] array)
	{
		if (array == null)
		{
			this = default(ReadOnlyMemory<T>);
			return;
		}
		_object = array;
		_index = 0;
		_length = array.Length;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ReadOnlyMemory(T[] array, int start, int length)
	{
		if (array == null)
		{
			if (start != 0 || length != 0)
			{
				System.ThrowHelper.ThrowArgumentOutOfRangeException();
			}
			this = default(ReadOnlyMemory<T>);
			return;
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
	internal ReadOnlyMemory(object obj, int start, int length)
	{
		_object = obj;
		_index = start;
		_length = length;
	}

	public static implicit operator ReadOnlyMemory<T>(T[] array)
	{
		return new ReadOnlyMemory<T>(array);
	}

	public static implicit operator ReadOnlyMemory<T>(ArraySegment<T> segment)
	{
		return new ReadOnlyMemory<T>(segment.Array, segment.Offset, segment.Count);
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
		return $"System.ReadOnlyMemory<{typeof(T).Name}>[{_length & 0x7FFFFFFF}]";
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ReadOnlyMemory<T> Slice(int start)
	{
		int length = _length;
		int num = length & 0x7FFFFFFF;
		if ((uint)start > (uint)num)
		{
			System.ThrowHelper.ThrowArgumentOutOfRangeException(System.ExceptionArgument.start);
		}
		return new ReadOnlyMemory<T>(_object, _index + start, length - start);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ReadOnlyMemory<T> Slice(int start, int length)
	{
		int length2 = _length;
		int num = _length & 0x7FFFFFFF;
		if ((uint)start > (uint)num || (uint)length > (uint)(num - start))
		{
			System.ThrowHelper.ThrowArgumentOutOfRangeException(System.ExceptionArgument.start);
		}
		return new ReadOnlyMemory<T>(_object, _index + start, length | (length2 & int.MinValue));
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
		if (obj is ReadOnlyMemory<T> other)
		{
			return Equals(other);
		}
		if (obj is Memory<T> memory)
		{
			return Equals(memory);
		}
		return false;
	}

	public bool Equals(ReadOnlyMemory<T> other)
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

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal object GetObjectStartLength(out int start, out int length)
	{
		start = _index;
		length = _length;
		return _object;
	}
}
