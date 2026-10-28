using System.Collections;
using System.Collections.Generic;
using System.Reflection.Internal;
using System.Reflection.Metadata.Ecma335;

namespace System.Reflection.Metadata;

internal readonly struct SequencePointCollection : IEnumerable<System.Reflection.Metadata.SequencePoint>, IEnumerable
{
	public struct Enumerator : IEnumerator<System.Reflection.Metadata.SequencePoint>, IDisposable, IEnumerator
	{
		private System.Reflection.Metadata.BlobReader _reader;

		private System.Reflection.Metadata.SequencePoint _current;

		private int _previousNonHiddenStartLine;

		private ushort _previousNonHiddenStartColumn;

		public System.Reflection.Metadata.SequencePoint Current => _current;

		object IEnumerator.Current => _current;

		internal Enumerator(System.Reflection.Internal.MemoryBlock block, System.Reflection.Metadata.DocumentHandle document)
		{
			_reader = new System.Reflection.Metadata.BlobReader(block);
			_current = new System.Reflection.Metadata.SequencePoint(document, -1);
			_previousNonHiddenStartLine = -1;
			_previousNonHiddenStartColumn = 0;
		}

		public bool MoveNext()
		{
			if (_reader.RemainingBytes == 0)
			{
				return false;
			}
			System.Reflection.Metadata.DocumentHandle document = _current.Document;
			int offset;
			if (_reader.Offset == 0)
			{
				_reader.ReadCompressedInteger();
				if (document.IsNil)
				{
					document = ReadDocumentHandle();
				}
				offset = _reader.ReadCompressedInteger();
			}
			else
			{
				int delta;
				while ((delta = _reader.ReadCompressedInteger()) == 0)
				{
					document = ReadDocumentHandle();
				}
				offset = AddOffsets(_current.Offset, delta);
			}
			ReadDeltaLinesAndColumns(out var deltaLines, out var deltaColumns);
			if (deltaLines == 0 && deltaColumns == 0)
			{
				_current = new System.Reflection.Metadata.SequencePoint(document, offset);
				return true;
			}
			int num;
			ushort num2;
			if (_previousNonHiddenStartLine < 0)
			{
				num = ReadLine();
				num2 = ReadColumn();
			}
			else
			{
				num = AddLines(_previousNonHiddenStartLine, _reader.ReadCompressedSignedInteger());
				num2 = AddColumns(_previousNonHiddenStartColumn, _reader.ReadCompressedSignedInteger());
			}
			_previousNonHiddenStartLine = num;
			_previousNonHiddenStartColumn = num2;
			_current = new System.Reflection.Metadata.SequencePoint(document, offset, num, num2, AddLines(num, deltaLines), AddColumns(num2, deltaColumns));
			return true;
		}

		private void ReadDeltaLinesAndColumns(out int deltaLines, out int deltaColumns)
		{
			deltaLines = _reader.ReadCompressedInteger();
			deltaColumns = ((deltaLines == 0) ? _reader.ReadCompressedInteger() : _reader.ReadCompressedSignedInteger());
		}

		private int ReadLine()
		{
			return _reader.ReadCompressedInteger();
		}

		private ushort ReadColumn()
		{
			int num = _reader.ReadCompressedInteger();
			if (num > 65535)
			{
				System.Reflection.Throw.SequencePointValueOutOfRange();
			}
			return (ushort)num;
		}

		private static int AddOffsets(int value, int delta)
		{
			int num = value + delta;
			if (num < 0)
			{
				System.Reflection.Throw.SequencePointValueOutOfRange();
			}
			return num;
		}

		private static int AddLines(int value, int delta)
		{
			int num = value + delta;
			if (num < 0 || num >= 16707566)
			{
				System.Reflection.Throw.SequencePointValueOutOfRange();
			}
			return num;
		}

		private static ushort AddColumns(ushort value, int delta)
		{
			int num = value + delta;
			if (num < 0 || num >= 65535)
			{
				System.Reflection.Throw.SequencePointValueOutOfRange();
			}
			return (ushort)num;
		}

		private System.Reflection.Metadata.DocumentHandle ReadDocumentHandle()
		{
			int num = _reader.ReadCompressedInteger();
			if (num == 0 || !System.Reflection.Metadata.Ecma335.TokenTypeIds.IsValidRowId(num))
			{
				System.Reflection.Throw.InvalidHandle();
			}
			return System.Reflection.Metadata.DocumentHandle.FromRowId(num);
		}

		public void Reset()
		{
			_reader.Reset();
			_current = default(System.Reflection.Metadata.SequencePoint);
		}

		void IDisposable.Dispose()
		{
		}
	}

	private readonly System.Reflection.Internal.MemoryBlock _block;

	private readonly System.Reflection.Metadata.DocumentHandle _document;

	internal SequencePointCollection(System.Reflection.Internal.MemoryBlock block, System.Reflection.Metadata.DocumentHandle document)
	{
		_block = block;
		_document = document;
	}

	public Enumerator GetEnumerator()
	{
		return new Enumerator(_block, _document);
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	IEnumerator<System.Reflection.Metadata.SequencePoint> IEnumerable<System.Reflection.Metadata.SequencePoint>.GetEnumerator()
	{
		return GetEnumerator();
	}
}
