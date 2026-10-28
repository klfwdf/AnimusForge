using System.Collections.Immutable;
using System.IO;

namespace System.Reflection.Internal;

internal sealed class ImmutableMemoryStream : Stream
{
	private readonly System.Collections.Immutable.ImmutableArray<byte> _array;

	private int _position;

	public override bool CanRead => true;

	public override bool CanSeek => true;

	public override bool CanWrite => false;

	public override long Length => _array.Length;

	public override long Position
	{
		get
		{
			return _position;
		}
		set
		{
			if (value < 0 || value >= _array.Length)
			{
				throw new ArgumentOutOfRangeException("value");
			}
			_position = (int)value;
		}
	}

	internal ImmutableMemoryStream(System.Collections.Immutable.ImmutableArray<byte> array)
	{
		_array = array;
	}

	public System.Collections.Immutable.ImmutableArray<byte> GetBuffer()
	{
		return _array;
	}

	public override void Flush()
	{
	}

	public override int Read(byte[] buffer, int offset, int count)
	{
		int num = Math.Min(count, _array.Length - _position);
		_array.CopyTo(_position, buffer, offset, num);
		_position += num;
		return num;
	}

	public override long Seek(long offset, SeekOrigin origin)
	{
		long num;
		try
		{
			num = checked(origin switch
			{
				SeekOrigin.Begin => offset, 
				SeekOrigin.Current => offset + _position, 
				SeekOrigin.End => offset + _array.Length, 
				_ => throw new ArgumentOutOfRangeException("origin"), 
			});
		}
		catch (OverflowException)
		{
			throw new ArgumentOutOfRangeException("offset");
		}
		if (num < 0 || num >= _array.Length)
		{
			throw new ArgumentOutOfRangeException("offset");
		}
		_position = (int)num;
		return num;
	}

	public override void SetLength(long value)
	{
		throw new NotSupportedException();
	}

	public override void Write(byte[] buffer, int offset, int count)
	{
		throw new NotSupportedException();
	}
}
