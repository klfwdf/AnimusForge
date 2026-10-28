using System.Collections.Immutable;
using System.Reflection.Internal;
using System.Runtime.InteropServices;

namespace System.Reflection.Metadata;

internal sealed class MethodBodyBlock
{
	private readonly System.Reflection.Internal.MemoryBlock _il;

	private readonly int _size;

	private readonly ushort _maxStack;

	private readonly bool _localVariablesInitialized;

	private readonly StandaloneSignatureHandle _localSignature;

	private readonly System.Collections.Immutable.ImmutableArray<ExceptionRegion> _exceptionRegions;

	private const byte ILTinyFormat = 2;

	private const byte ILFatFormat = 3;

	private const byte ILFormatMask = 3;

	private const int ILTinyFormatSizeShift = 2;

	private const byte ILMoreSects = 8;

	private const byte ILInitLocals = 16;

	private const byte ILFatFormatHeaderSize = 3;

	private const int ILFatFormatHeaderSizeShift = 4;

	private const byte SectEHTable = 1;

	private const byte SectFatFormat = 64;

	public int Size => _size;

	public int MaxStack => _maxStack;

	public bool LocalVariablesInitialized => _localVariablesInitialized;

	public StandaloneSignatureHandle LocalSignature => _localSignature;

	public System.Collections.Immutable.ImmutableArray<ExceptionRegion> ExceptionRegions => _exceptionRegions;

	private MethodBodyBlock(bool localVariablesInitialized, ushort maxStack, StandaloneSignatureHandle localSignatureHandle, System.Reflection.Internal.MemoryBlock il, System.Collections.Immutable.ImmutableArray<ExceptionRegion> exceptionRegions, int size)
	{
		_localVariablesInitialized = localVariablesInitialized;
		_maxStack = maxStack;
		_localSignature = localSignatureHandle;
		_il = il;
		_exceptionRegions = exceptionRegions;
		_size = size;
	}

	public byte[]? GetILBytes()
	{
		return _il.ToArray();
	}

	public System.Collections.Immutable.ImmutableArray<byte> GetILContent()
	{
		return ImmutableCollectionsMarshal.AsImmutableArray(GetILBytes());
	}

	public System.Reflection.Metadata.BlobReader GetILReader()
	{
		return new System.Reflection.Metadata.BlobReader(_il);
	}

	public static MethodBodyBlock Create(System.Reflection.Metadata.BlobReader reader)
	{
		int offset = reader.Offset;
		byte b = reader.ReadByte();
		int num;
		if ((b & 3) == 2)
		{
			num = b >> 2;
			return new MethodBodyBlock(localVariablesInitialized: false, 8, default(StandaloneSignatureHandle), reader.GetMemoryBlockAt(0, num), System.Collections.Immutable.ImmutableArray<ExceptionRegion>.Empty, 1 + num);
		}
		if ((b & 3) != 3)
		{
			throw new BadImageFormatException(System.SR.Format(System.SR.InvalidMethodHeader1, b));
		}
		byte b2 = reader.ReadByte();
		if (b2 >> 4 != 3)
		{
			throw new BadImageFormatException(System.SR.Format(System.SR.InvalidMethodHeader2, b, b2));
		}
		bool localVariablesInitialized = (b & 0x10) == 16;
		bool num2 = (b & 8) == 8;
		ushort maxStack = reader.ReadUInt16();
		num = reader.ReadInt32();
		int num3 = reader.ReadInt32();
		StandaloneSignatureHandle localSignatureHandle;
		if (num3 == 0)
		{
			localSignatureHandle = default(StandaloneSignatureHandle);
		}
		else
		{
			if (((ulong)num3 & 0x7F000000uL) != 285212672)
			{
				throw new BadImageFormatException(System.SR.Format(System.SR.InvalidLocalSignatureToken, (uint)num3));
			}
			localSignatureHandle = StandaloneSignatureHandle.FromRowId(num3 & 0xFFFFFF);
		}
		System.Reflection.Internal.MemoryBlock memoryBlockAt = reader.GetMemoryBlockAt(0, num);
		reader.Offset += num;
		System.Collections.Immutable.ImmutableArray<ExceptionRegion> exceptionRegions;
		if (num2)
		{
			reader.Align(4);
			byte b3 = reader.ReadByte();
			if ((b3 & 1) != 1)
			{
				throw new BadImageFormatException(System.SR.Format(System.SR.InvalidSehHeader, b3));
			}
			bool num4 = (b3 & 0x40) == 64;
			int num5 = reader.ReadByte();
			if (num4)
			{
				num5 += reader.ReadUInt16() << 8;
				exceptionRegions = ReadFatExceptionHandlers(ref reader, num5 / 24);
			}
			else
			{
				reader.Offset += 2;
				exceptionRegions = ReadSmallExceptionHandlers(ref reader, num5 / 12);
			}
		}
		else
		{
			exceptionRegions = System.Collections.Immutable.ImmutableArray<ExceptionRegion>.Empty;
		}
		return new MethodBodyBlock(localVariablesInitialized, maxStack, localSignatureHandle, memoryBlockAt, exceptionRegions, reader.Offset - offset);
	}

	private static System.Collections.Immutable.ImmutableArray<ExceptionRegion> ReadSmallExceptionHandlers(ref System.Reflection.Metadata.BlobReader memReader, int count)
	{
		ExceptionRegion[] array = new ExceptionRegion[count];
		for (int i = 0; i < array.Length; i++)
		{
			ExceptionRegionKind kind = (ExceptionRegionKind)memReader.ReadUInt16();
			ushort tryOffset = memReader.ReadUInt16();
			byte tryLength = memReader.ReadByte();
			ushort handlerOffset = memReader.ReadUInt16();
			byte handlerLength = memReader.ReadByte();
			int classTokenOrFilterOffset = memReader.ReadInt32();
			array[i] = new ExceptionRegion(kind, tryOffset, tryLength, handlerOffset, handlerLength, classTokenOrFilterOffset);
		}
		return System.Collections.Immutable.ImmutableArray.Create(array);
	}

	private static System.Collections.Immutable.ImmutableArray<ExceptionRegion> ReadFatExceptionHandlers(ref System.Reflection.Metadata.BlobReader memReader, int count)
	{
		ExceptionRegion[] array = new ExceptionRegion[count];
		for (int i = 0; i < array.Length; i++)
		{
			ExceptionRegionKind kind = (ExceptionRegionKind)memReader.ReadUInt32();
			int tryOffset = memReader.ReadInt32();
			int tryLength = memReader.ReadInt32();
			int handlerOffset = memReader.ReadInt32();
			int handlerLength = memReader.ReadInt32();
			int classTokenOrFilterOffset = memReader.ReadInt32();
			array[i] = new ExceptionRegion(kind, tryOffset, tryLength, handlerOffset, handlerLength, classTokenOrFilterOffset);
		}
		return System.Collections.Immutable.ImmutableArray.Create(array);
	}
}
