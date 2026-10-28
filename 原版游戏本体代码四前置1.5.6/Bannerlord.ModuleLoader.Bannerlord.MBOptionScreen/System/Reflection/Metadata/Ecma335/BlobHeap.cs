using System.Reflection.Internal;
using System.Text;

namespace System.Reflection.Metadata.Ecma335;

internal struct BlobHeap
{
	private static byte[][] s_virtualValues;

	internal readonly System.Reflection.Internal.MemoryBlock Block;

	private VirtualHeap _lazyVirtualHeap;

	internal BlobHeap(System.Reflection.Internal.MemoryBlock block, System.Reflection.Metadata.MetadataKind metadataKind)
	{
		_lazyVirtualHeap = null;
		Block = block;
		if (s_virtualValues == null && metadataKind != System.Reflection.Metadata.MetadataKind.Ecma335)
		{
			s_virtualValues = new byte[5][]
			{
				null,
				new byte[8] { 176, 63, 95, 127, 17, 213, 10, 58 },
				new byte[160]
				{
					0, 36, 0, 0, 4, 128, 0, 0, 148, 0,
					0, 0, 6, 2, 0, 0, 0, 36, 0, 0,
					82, 83, 65, 49, 0, 4, 0, 0, 1, 0,
					1, 0, 7, 209, 250, 87, 196, 174, 217, 240,
					163, 46, 132, 170, 15, 174, 253, 13, 233, 232,
					253, 106, 236, 143, 135, 251, 3, 118, 108, 131,
					76, 153, 146, 30, 178, 59, 231, 154, 217, 213,
					220, 193, 221, 154, 210, 54, 19, 33, 2, 144,
					11, 114, 60, 249, 128, 149, 127, 196, 225, 119,
					16, 143, 198, 7, 119, 79, 41, 232, 50, 14,
					146, 234, 5, 236, 228, 232, 33, 192, 165, 239,
					232, 241, 100, 92, 76, 12, 147, 193, 171, 153,
					40, 93, 98, 44, 170, 101, 44, 29, 250, 214,
					61, 116, 93, 111, 45, 229, 241, 126, 94, 175,
					15, 196, 150, 61, 38, 28, 138, 18, 67, 101,
					24, 32, 109, 192, 147, 52, 77, 90, 210, 147
				},
				new byte[25]
				{
					1, 0, 0, 0, 0, 0, 1, 0, 84, 2,
					13, 65, 108, 108, 111, 119, 77, 117, 108, 116,
					105, 112, 108, 101, 0
				},
				new byte[25]
				{
					1, 0, 0, 0, 0, 0, 1, 0, 84, 2,
					13, 65, 108, 108, 111, 119, 77, 117, 108, 116,
					105, 112, 108, 101, 1
				}
			};
		}
	}

	internal byte[] GetBytes(System.Reflection.Metadata.BlobHandle handle)
	{
		if (handle.IsVirtual)
		{
			return GetVirtualBlobBytes(handle, unique: true);
		}
		int heapOffset = handle.GetHeapOffset();
		int num = Block.PeekCompressedInteger(heapOffset, out var numberOfBytesRead);
		if (num == int.MaxValue)
		{
			return Array.Empty<byte>();
		}
		return Block.PeekBytes(heapOffset + numberOfBytesRead, num);
	}

	internal System.Reflection.Internal.MemoryBlock GetMemoryBlock(System.Reflection.Metadata.BlobHandle handle)
	{
		if (handle.IsVirtual)
		{
			return GetVirtualHandleMemoryBlock(handle);
		}
		Block.PeekHeapValueOffsetAndSize(handle.GetHeapOffset(), out var offset, out var size);
		return Block.GetMemoryBlockAt(offset, size);
	}

	private System.Reflection.Internal.MemoryBlock GetVirtualHandleMemoryBlock(System.Reflection.Metadata.BlobHandle handle)
	{
		VirtualHeap orCreateVirtualHeap = VirtualHeap.GetOrCreateVirtualHeap(ref _lazyVirtualHeap);
		lock (orCreateVirtualHeap)
		{
			if (!orCreateVirtualHeap.TryGetMemoryBlock(handle.RawValue, out var block))
			{
				return orCreateVirtualHeap.AddBlob(handle.RawValue, GetVirtualBlobBytes(handle, unique: false));
			}
			return block;
		}
	}

	internal System.Reflection.Metadata.BlobReader GetBlobReader(System.Reflection.Metadata.BlobHandle handle)
	{
		return new System.Reflection.Metadata.BlobReader(GetMemoryBlock(handle));
	}

	internal System.Reflection.Metadata.BlobHandle GetNextHandle(System.Reflection.Metadata.BlobHandle handle)
	{
		if (handle.IsVirtual)
		{
			return default(System.Reflection.Metadata.BlobHandle);
		}
		if (!Block.PeekHeapValueOffsetAndSize(handle.GetHeapOffset(), out var offset, out var size))
		{
			return default(System.Reflection.Metadata.BlobHandle);
		}
		int num = offset + size;
		if (num >= Block.Length)
		{
			return default(System.Reflection.Metadata.BlobHandle);
		}
		return System.Reflection.Metadata.BlobHandle.FromOffset(num);
	}

	internal static byte[] GetVirtualBlobBytes(System.Reflection.Metadata.BlobHandle handle, bool unique)
	{
		System.Reflection.Metadata.BlobHandle.VirtualIndex virtualIndex = handle.GetVirtualIndex();
		byte[] array = s_virtualValues[(uint)virtualIndex];
		if (virtualIndex - 3 <= System.Reflection.Metadata.BlobHandle.VirtualIndex.ContractPublicKeyToken)
		{
			array = (byte[])array.Clone();
			handle.SubstituteTemplateParameters(array);
		}
		else if (unique)
		{
			array = (byte[])array.Clone();
		}
		return array;
	}

	public string GetDocumentName(System.Reflection.Metadata.DocumentNameBlobHandle handle)
	{
		System.Reflection.Metadata.BlobReader blobReader = GetBlobReader(handle);
		int num = blobReader.ReadByte();
		if (num > 127)
		{
			throw new BadImageFormatException(System.SR.InvalidDocumentName);
		}
		System.Reflection.Internal.PooledStringBuilder instance = System.Reflection.Internal.PooledStringBuilder.GetInstance();
		StringBuilder builder = instance.Builder;
		bool flag = true;
		while (blobReader.RemainingBytes > 0)
		{
			if (num != 0 && !flag)
			{
				builder.Append((char)num);
			}
			System.Reflection.Metadata.BlobReader blobReader2 = GetBlobReader(blobReader.ReadBlobHandle());
			builder.Append(blobReader2.ReadUTF8(blobReader2.Length));
			flag = false;
		}
		return instance.ToStringAndFree();
	}

	internal bool DocumentNameEquals(System.Reflection.Metadata.DocumentNameBlobHandle handle, string other, bool ignoreCase)
	{
		System.Reflection.Metadata.BlobReader blobReader = GetBlobReader(handle);
		int num = blobReader.ReadByte();
		if (num > 127)
		{
			return false;
		}
		int ignoreCaseMask = StringUtils.IgnoreCaseMask(ignoreCase);
		int num2 = 0;
		int firstDifferenceIndex;
		for (bool flag = true; blobReader.RemainingBytes > 0; num2 = firstDifferenceIndex, flag = false)
		{
			if (num != 0 && !flag)
			{
				if (num2 == other.Length || !StringUtils.IsEqualAscii(other[num2], num, ignoreCaseMask))
				{
					return false;
				}
				num2++;
			}
			System.Reflection.Internal.MemoryBlock memoryBlock = GetMemoryBlock(blobReader.ReadBlobHandle());
			switch (memoryBlock.Utf8NullTerminatedFastCompare(0, other, num2, out firstDifferenceIndex, '\0', ignoreCase))
			{
			case System.Reflection.Internal.MemoryBlock.FastComparisonResult.Inconclusive:
				return GetDocumentName(handle).Equals(other, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
			default:
				if (firstDifferenceIndex - num2 == memoryBlock.Length)
				{
					continue;
				}
				break;
			case System.Reflection.Internal.MemoryBlock.FastComparisonResult.Unequal:
				break;
			}
			return false;
		}
		return num2 == other.Length;
	}
}
