using System.Collections.Generic;
using System.Threading;
using TaleWorlds.Library;

namespace TaleWorlds.SaveSystem;

internal static class BinaryWriterFactory
{
	private static ThreadLocal<Stack<BinaryWriter>> _binaryWriters;

	public static BinaryWriter GetBinaryWriter()
	{
		if (_binaryWriters.Value == null)
		{
			_binaryWriters.Value = new Stack<BinaryWriter>();
		}
		Stack<BinaryWriter> value = _binaryWriters.Value;
		BinaryWriter binaryWriter = null;
		if (value.Count != 0)
		{
			return value.Pop();
		}
		return new BinaryWriter(4096);
	}

	public static void ReleaseBinaryWriter(BinaryWriter writer)
	{
		if (_binaryWriters != null)
		{
			if (_binaryWriters.Value == null)
			{
				Debug.Print("Release used before Get");
				_binaryWriters.Value = new Stack<BinaryWriter>();
			}
			writer.Clear();
			_binaryWriters.Value.Push(writer);
		}
		else
		{
			Debug.FailedAssert("_binaryWriters != null", "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\Base\\TaleWorlds.SaveSystem\\BinaryWriterFactory.cs", "ReleaseBinaryWriter", 46);
		}
	}

	public static void Initialize()
	{
		_binaryWriters = new ThreadLocal<Stack<BinaryWriter>>();
	}

	public static void Release()
	{
		_binaryWriters.Dispose();
		_binaryWriters = null;
	}
}
