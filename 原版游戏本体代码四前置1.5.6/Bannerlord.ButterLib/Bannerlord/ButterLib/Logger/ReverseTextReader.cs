using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Bannerlord.ButterLib.Logger;

internal sealed class ReverseTextReader : IEnumerable<string>, IEnumerable
{
	private const int BufferSize = 16384;

	private readonly Stream _stream;

	private readonly Encoding _encoding;

	private byte[] _leftoverBuffer;

	private readonly Queue<string> _lines;

	public ReverseTextReader(Stream stream)
		: this(stream, Encoding.Default)
	{
	}

	public ReverseTextReader(Stream stream, Encoding encoding)
	{
		_stream = stream;
		_encoding = encoding;
		_lines = new Queue<string>(128);
		if (!_stream.CanSeek)
		{
			throw new InvalidOperationException("The specified stream needs to support seeking to be read backwards.");
		}
		if (!_stream.CanRead)
		{
			throw new InvalidOperationException("The specified stream needs to support reading to be read backwards.");
		}
		_stream.Position = _stream.Length;
		_leftoverBuffer = Array.Empty<byte>();
	}

	public string? ReadLine()
	{
		if (_lines.Count != 0)
		{
			return _lines.Dequeue();
		}
		if (_stream.Position == 0L)
		{
			return null;
		}
		long position = _stream.Position;
		long num = position - 16384;
		if (num < 0)
		{
			num = 0L;
		}
		int num2 = (int)(position - num);
		_stream.Position = num;
		byte[] array = new byte[num2 + _leftoverBuffer.Length];
		_stream.Read(array, 0, num2);
		_stream.Position = num;
		if (_leftoverBuffer.Length != 0)
		{
			Array.Copy(_leftoverBuffer, 0, array, num2, _leftoverBuffer.Length);
		}
		int num3 = array.Length - 1;
		for (int num4 = array.Length - 2; num4 >= 0; num4--)
		{
			if (array[num4] == 10)
			{
				_lines.Enqueue(_encoding.GetString(array, num4 + 1, num3 - num4 - 2));
				num3 = num4;
			}
		}
		_leftoverBuffer = new byte[num3 + 1];
		Array.Copy(array, 0, _leftoverBuffer, 0, num3 + 1);
		if (_stream.Position == 0L)
		{
			_lines.Enqueue(_encoding.GetString(_leftoverBuffer, 0, num3 - 1));
		}
		if (_lines.Count != 0)
		{
			return _lines.Dequeue();
		}
		return null;
	}

	public IEnumerator<string> GetEnumerator()
	{
		while (true)
		{
			string text = ReadLine();
			if (text != null)
			{
				yield return text;
				continue;
			}
			break;
		}
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		throw new NotImplementedException();
	}
}
