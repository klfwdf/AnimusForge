using System.Buffers;
using System.Runtime.CompilerServices;

namespace System;

internal static class ThrowHelper
{
	internal static void ThrowArgumentNullException(System.ExceptionArgument argument)
	{
		throw CreateArgumentNullException(argument);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static Exception CreateArgumentNullException(System.ExceptionArgument argument)
	{
		return new ArgumentNullException(argument.ToString());
	}

	internal static void ThrowArrayTypeMismatchException()
	{
		throw CreateArrayTypeMismatchException();
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static Exception CreateArrayTypeMismatchException()
	{
		return new ArrayTypeMismatchException();
	}

	internal static void ThrowArgumentException_InvalidTypeWithPointersNotSupported(Type type)
	{
		throw CreateArgumentException_InvalidTypeWithPointersNotSupported(type);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static Exception CreateArgumentException_InvalidTypeWithPointersNotSupported(Type type)
	{
		return new ArgumentException(_003C805945f3_002D27b0_002D47ad_002Db8f6_002D389d9d8f82c3_003ESR.Format(_003C805945f3_002D27b0_002D47ad_002Db8f6_002D389d9d8f82c3_003ESR.Argument_InvalidTypeWithPointersNotSupported, type));
	}

	internal static void ThrowArgumentException_DestinationTooShort()
	{
		throw CreateArgumentException_DestinationTooShort();
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static Exception CreateArgumentException_DestinationTooShort()
	{
		return new ArgumentException(_003C805945f3_002D27b0_002D47ad_002Db8f6_002D389d9d8f82c3_003ESR.Argument_DestinationTooShort);
	}

	internal static void ThrowIndexOutOfRangeException()
	{
		throw CreateIndexOutOfRangeException();
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static Exception CreateIndexOutOfRangeException()
	{
		return new IndexOutOfRangeException();
	}

	internal static void ThrowArgumentOutOfRangeException()
	{
		throw CreateArgumentOutOfRangeException();
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static Exception CreateArgumentOutOfRangeException()
	{
		return new ArgumentOutOfRangeException();
	}

	internal static void ThrowArgumentOutOfRangeException(System.ExceptionArgument argument)
	{
		throw CreateArgumentOutOfRangeException(argument);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static Exception CreateArgumentOutOfRangeException(System.ExceptionArgument argument)
	{
		return new ArgumentOutOfRangeException(argument.ToString());
	}

	internal static void ThrowArgumentOutOfRangeException_PrecisionTooLarge()
	{
		throw CreateArgumentOutOfRangeException_PrecisionTooLarge();
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static Exception CreateArgumentOutOfRangeException_PrecisionTooLarge()
	{
		return new ArgumentOutOfRangeException("precision", _003C805945f3_002D27b0_002D47ad_002Db8f6_002D389d9d8f82c3_003ESR.Format(_003C805945f3_002D27b0_002D47ad_002Db8f6_002D389d9d8f82c3_003ESR.Argument_PrecisionTooLarge, (byte)99));
	}

	internal static void ThrowArgumentOutOfRangeException_SymbolDoesNotFit()
	{
		throw CreateArgumentOutOfRangeException_SymbolDoesNotFit();
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static Exception CreateArgumentOutOfRangeException_SymbolDoesNotFit()
	{
		return new ArgumentOutOfRangeException("symbol", _003C805945f3_002D27b0_002D47ad_002Db8f6_002D389d9d8f82c3_003ESR.Argument_BadFormatSpecifier);
	}

	internal static void ThrowInvalidOperationException()
	{
		throw CreateInvalidOperationException();
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static Exception CreateInvalidOperationException()
	{
		return new InvalidOperationException();
	}

	internal static void ThrowInvalidOperationException_OutstandingReferences()
	{
		throw CreateInvalidOperationException_OutstandingReferences();
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static Exception CreateInvalidOperationException_OutstandingReferences()
	{
		return new InvalidOperationException(_003C805945f3_002D27b0_002D47ad_002Db8f6_002D389d9d8f82c3_003ESR.OutstandingReferences);
	}

	internal static void ThrowInvalidOperationException_UnexpectedSegmentType()
	{
		throw CreateInvalidOperationException_UnexpectedSegmentType();
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static Exception CreateInvalidOperationException_UnexpectedSegmentType()
	{
		return new InvalidOperationException(_003C805945f3_002D27b0_002D47ad_002Db8f6_002D389d9d8f82c3_003ESR.UnexpectedSegmentType);
	}

	internal static void ThrowInvalidOperationException_EndPositionNotReached()
	{
		throw CreateInvalidOperationException_EndPositionNotReached();
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static Exception CreateInvalidOperationException_EndPositionNotReached()
	{
		return new InvalidOperationException(_003C805945f3_002D27b0_002D47ad_002Db8f6_002D389d9d8f82c3_003ESR.EndPositionNotReached);
	}

	internal static void ThrowArgumentOutOfRangeException_PositionOutOfRange()
	{
		throw CreateArgumentOutOfRangeException_PositionOutOfRange();
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static Exception CreateArgumentOutOfRangeException_PositionOutOfRange()
	{
		return new ArgumentOutOfRangeException("position");
	}

	internal static void ThrowArgumentOutOfRangeException_OffsetOutOfRange()
	{
		throw CreateArgumentOutOfRangeException_OffsetOutOfRange();
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static Exception CreateArgumentOutOfRangeException_OffsetOutOfRange()
	{
		return new ArgumentOutOfRangeException("offset");
	}

	internal static void ThrowObjectDisposedException_ArrayMemoryPoolBuffer()
	{
		throw CreateObjectDisposedException_ArrayMemoryPoolBuffer();
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static Exception CreateObjectDisposedException_ArrayMemoryPoolBuffer()
	{
		return new ObjectDisposedException("ArrayMemoryPoolBuffer");
	}

	internal static void ThrowFormatException_BadFormatSpecifier()
	{
		throw CreateFormatException_BadFormatSpecifier();
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static Exception CreateFormatException_BadFormatSpecifier()
	{
		return new FormatException(_003C805945f3_002D27b0_002D47ad_002Db8f6_002D389d9d8f82c3_003ESR.Argument_BadFormatSpecifier);
	}

	internal static void ThrowArgumentException_OverlapAlignmentMismatch()
	{
		throw CreateArgumentException_OverlapAlignmentMismatch();
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static Exception CreateArgumentException_OverlapAlignmentMismatch()
	{
		return new ArgumentException(_003C805945f3_002D27b0_002D47ad_002Db8f6_002D389d9d8f82c3_003ESR.Argument_OverlapAlignmentMismatch);
	}

	internal static void ThrowNotSupportedException()
	{
		throw CreateThrowNotSupportedException();
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static Exception CreateThrowNotSupportedException()
	{
		return new NotSupportedException();
	}

	public static bool TryFormatThrowFormatException(out int bytesWritten)
	{
		bytesWritten = 0;
		ThrowFormatException_BadFormatSpecifier();
		return false;
	}

	public static bool TryParseThrowFormatException<T>(out T value, out int bytesConsumed)
	{
		value = default(T);
		bytesConsumed = 0;
		ThrowFormatException_BadFormatSpecifier();
		return false;
	}

	public static void ThrowArgumentValidationException<T>(ReadOnlySequenceSegment<T> startSegment, int startIndex, ReadOnlySequenceSegment<T> endSegment)
	{
		throw CreateArgumentValidationException(startSegment, startIndex, endSegment);
	}

	private static Exception CreateArgumentValidationException<T>(ReadOnlySequenceSegment<T> startSegment, int startIndex, ReadOnlySequenceSegment<T> endSegment)
	{
		if (startSegment == null)
		{
			return CreateArgumentNullException(System.ExceptionArgument.startSegment);
		}
		if (endSegment == null)
		{
			return CreateArgumentNullException(System.ExceptionArgument.endSegment);
		}
		if (startSegment != endSegment && startSegment.RunningIndex > endSegment.RunningIndex)
		{
			return CreateArgumentOutOfRangeException(System.ExceptionArgument.endSegment);
		}
		if ((uint)startSegment.Memory.Length < (uint)startIndex)
		{
			return CreateArgumentOutOfRangeException(System.ExceptionArgument.startIndex);
		}
		return CreateArgumentOutOfRangeException(System.ExceptionArgument.endIndex);
	}

	public static void ThrowArgumentValidationException(Array array, int start)
	{
		throw CreateArgumentValidationException(array, start);
	}

	private static Exception CreateArgumentValidationException(Array array, int start)
	{
		if (array == null)
		{
			return CreateArgumentNullException(System.ExceptionArgument.array);
		}
		if ((uint)start > (uint)array.Length)
		{
			return CreateArgumentOutOfRangeException(System.ExceptionArgument.start);
		}
		return CreateArgumentOutOfRangeException(System.ExceptionArgument.length);
	}

	public static void ThrowStartOrEndArgumentValidationException(long start)
	{
		throw CreateStartOrEndArgumentValidationException(start);
	}

	private static Exception CreateStartOrEndArgumentValidationException(long start)
	{
		if (start < 0)
		{
			return CreateArgumentOutOfRangeException(System.ExceptionArgument.start);
		}
		return CreateArgumentOutOfRangeException(System.ExceptionArgument.length);
	}
}
