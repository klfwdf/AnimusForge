namespace System.Reflection.Metadata.Ecma335;

internal readonly struct ExceptionRegionEncoder
{
	private const int TableHeaderSize = 4;

	private const int SmallRegionSize = 12;

	private const int FatRegionSize = 24;

	private const int ThreeBytesMaxValue = 16777215;

	internal const int MaxSmallExceptionRegions = 20;

	internal const int MaxExceptionRegions = 699050;

	public BlobBuilder Builder { get; }

	public bool HasSmallFormat { get; }

	internal ExceptionRegionEncoder(BlobBuilder builder, bool hasSmallFormat)
	{
		Builder = builder;
		HasSmallFormat = hasSmallFormat;
	}

	public static bool IsSmallRegionCount(int exceptionRegionCount)
	{
		return (uint)exceptionRegionCount <= 20u;
	}

	public static bool IsSmallExceptionRegion(int startOffset, int length)
	{
		if ((uint)startOffset <= 65535u)
		{
			return (uint)length <= 255u;
		}
		return false;
	}

	internal static bool IsSmallExceptionRegionFromBounds(int startOffset, int endOffset)
	{
		return IsSmallExceptionRegion(startOffset, endOffset - startOffset);
	}

	internal static int GetExceptionTableSize(int exceptionRegionCount, bool isSmallFormat)
	{
		return 4 + exceptionRegionCount * (isSmallFormat ? 12 : 24);
	}

	internal static bool IsExceptionRegionCountInBounds(int exceptionRegionCount)
	{
		return (uint)exceptionRegionCount <= 699050u;
	}

	internal static bool IsValidCatchTypeHandle(EntityHandle catchType)
	{
		if (!catchType.IsNil)
		{
			if (catchType.Kind != System.Reflection.Metadata.HandleKind.TypeDefinition && catchType.Kind != System.Reflection.Metadata.HandleKind.TypeSpecification)
			{
				return catchType.Kind == System.Reflection.Metadata.HandleKind.TypeReference;
			}
			return true;
		}
		return false;
	}

	internal static ExceptionRegionEncoder SerializeTableHeader(BlobBuilder builder, int exceptionRegionCount, bool hasSmallRegions)
	{
		bool flag = hasSmallRegions && IsSmallRegionCount(exceptionRegionCount);
		int exceptionTableSize = GetExceptionTableSize(exceptionRegionCount, flag);
		builder.Align(4);
		if (flag)
		{
			builder.WriteByte(1);
			builder.WriteByte((byte)exceptionTableSize);
			builder.WriteInt16(0);
		}
		else
		{
			builder.WriteByte(65);
			builder.WriteByte((byte)exceptionTableSize);
			builder.WriteUInt16((ushort)(exceptionTableSize >> 8));
		}
		return new ExceptionRegionEncoder(builder, flag);
	}

	public ExceptionRegionEncoder AddFinally(int tryOffset, int tryLength, int handlerOffset, int handlerLength)
	{
		return Add(ExceptionRegionKind.Finally, tryOffset, tryLength, handlerOffset, handlerLength);
	}

	public ExceptionRegionEncoder AddFault(int tryOffset, int tryLength, int handlerOffset, int handlerLength)
	{
		return Add(ExceptionRegionKind.Fault, tryOffset, tryLength, handlerOffset, handlerLength);
	}

	public ExceptionRegionEncoder AddCatch(int tryOffset, int tryLength, int handlerOffset, int handlerLength, EntityHandle catchType)
	{
		return Add(ExceptionRegionKind.Catch, tryOffset, tryLength, handlerOffset, handlerLength, catchType);
	}

	public ExceptionRegionEncoder AddFilter(int tryOffset, int tryLength, int handlerOffset, int handlerLength, int filterOffset)
	{
		return Add(ExceptionRegionKind.Filter, tryOffset, tryLength, handlerOffset, handlerLength, default(EntityHandle), filterOffset);
	}

	public ExceptionRegionEncoder Add(ExceptionRegionKind kind, int tryOffset, int tryLength, int handlerOffset, int handlerLength, EntityHandle catchType = default(EntityHandle), int filterOffset = 0)
	{
		if (Builder == null)
		{
			System.Reflection.Throw.InvalidOperation(System.SR.MethodHasNoExceptionRegions);
		}
		if (HasSmallFormat)
		{
			if ((ushort)tryOffset != tryOffset)
			{
				System.Reflection.Throw.ArgumentOutOfRange("tryOffset");
			}
			if ((byte)tryLength != tryLength)
			{
				System.Reflection.Throw.ArgumentOutOfRange("tryLength");
			}
			if ((ushort)handlerOffset != handlerOffset)
			{
				System.Reflection.Throw.ArgumentOutOfRange("handlerOffset");
			}
			if ((byte)handlerLength != handlerLength)
			{
				System.Reflection.Throw.ArgumentOutOfRange("handlerLength");
			}
		}
		else
		{
			if (tryOffset < 0)
			{
				System.Reflection.Throw.ArgumentOutOfRange("tryOffset");
			}
			if (tryLength < 0)
			{
				System.Reflection.Throw.ArgumentOutOfRange("tryLength");
			}
			if (handlerOffset < 0)
			{
				System.Reflection.Throw.ArgumentOutOfRange("handlerOffset");
			}
			if (handlerLength < 0)
			{
				System.Reflection.Throw.ArgumentOutOfRange("handlerLength");
			}
		}
		int catchTokenOrOffset;
		switch (kind)
		{
		case ExceptionRegionKind.Catch:
			if (!IsValidCatchTypeHandle(catchType))
			{
				System.Reflection.Throw.InvalidArgument_Handle("catchType");
			}
			catchTokenOrOffset = System.Reflection.Metadata.Ecma335.MetadataTokens.GetToken(catchType);
			break;
		case ExceptionRegionKind.Filter:
			if (filterOffset < 0)
			{
				System.Reflection.Throw.ArgumentOutOfRange("filterOffset");
			}
			catchTokenOrOffset = filterOffset;
			break;
		case ExceptionRegionKind.Finally:
		case ExceptionRegionKind.Fault:
			catchTokenOrOffset = 0;
			break;
		default:
			throw new ArgumentOutOfRangeException("kind");
		}
		AddUnchecked(kind, tryOffset, tryLength, handlerOffset, handlerLength, catchTokenOrOffset);
		return this;
	}

	internal void AddUnchecked(ExceptionRegionKind kind, int tryOffset, int tryLength, int handlerOffset, int handlerLength, int catchTokenOrOffset)
	{
		if (HasSmallFormat)
		{
			Builder.WriteUInt16((ushort)kind);
			Builder.WriteUInt16((ushort)tryOffset);
			Builder.WriteByte((byte)tryLength);
			Builder.WriteUInt16((ushort)handlerOffset);
			Builder.WriteByte((byte)handlerLength);
		}
		else
		{
			Builder.WriteInt32((int)kind);
			Builder.WriteInt32(tryOffset);
			Builder.WriteInt32(tryLength);
			Builder.WriteInt32(handlerOffset);
			Builder.WriteInt32(handlerLength);
		}
		Builder.WriteInt32(catchTokenOrOffset);
	}
}
