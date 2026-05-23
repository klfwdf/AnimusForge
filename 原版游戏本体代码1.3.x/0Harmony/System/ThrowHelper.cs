using System;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System
{
	// Token: 0x02000480 RID: 1152
	[NullableContext(1)]
	[Nullable(0)]
	internal static class ThrowHelper
	{
		// Token: 0x06001975 RID: 6517 RVA: 0x000542E0 File Offset: 0x000524E0
		[NullableContext(2)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static void ThrowIfArgumentNull([<1c2fb156-e9ba-45cc-af54-d5335bdb59af>NotNull] object obj, ExceptionArgument argument)
		{
			if (obj == null)
			{
				ThrowHelper.ThrowArgumentNullException(argument);
			}
		}

		// Token: 0x06001976 RID: 6518 RVA: 0x000542EB File Offset: 0x000524EB
		[NullableContext(2)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static void ThrowIfArgumentNull([<1c2fb156-e9ba-45cc-af54-d5335bdb59af>NotNull] object obj, [Nullable(1)] string argument, string message = null)
		{
			if (obj == null)
			{
				ThrowHelper.ThrowArgumentNullException(argument, message);
			}
		}

		// Token: 0x06001977 RID: 6519 RVA: 0x000542F7 File Offset: 0x000524F7
		[<1c2fb156-e9ba-45cc-af54-d5335bdb59af>DoesNotReturn]
		internal static void ThrowArgumentNullException(ExceptionArgument argument)
		{
			throw ThrowHelper.CreateArgumentNullException(argument);
		}

		// Token: 0x06001978 RID: 6520 RVA: 0x000542FF File Offset: 0x000524FF
		[<1c2fb156-e9ba-45cc-af54-d5335bdb59af>DoesNotReturn]
		internal static void ThrowArgumentNullException(string argument, [Nullable(2)] string message = null)
		{
			throw ThrowHelper.CreateArgumentNullException(argument, message);
		}

		// Token: 0x06001979 RID: 6521 RVA: 0x00054308 File Offset: 0x00052508
		[MethodImpl(MethodImplOptions.NoInlining)]
		private static Exception CreateArgumentNullException(ExceptionArgument argument)
		{
			return ThrowHelper.CreateArgumentNullException(argument.ToString(), null);
		}

		// Token: 0x0600197A RID: 6522 RVA: 0x0005431D File Offset: 0x0005251D
		[MethodImpl(MethodImplOptions.NoInlining)]
		private static Exception CreateArgumentNullException(string argument, [Nullable(2)] string message = null)
		{
			return new ArgumentNullException(argument, message);
		}

		// Token: 0x0600197B RID: 6523 RVA: 0x00054326 File Offset: 0x00052526
		[<1c2fb156-e9ba-45cc-af54-d5335bdb59af>DoesNotReturn]
		internal static void ThrowArrayTypeMismatchException()
		{
			throw ThrowHelper.CreateArrayTypeMismatchException();
		}

		// Token: 0x0600197C RID: 6524 RVA: 0x0005432D File Offset: 0x0005252D
		[MethodImpl(MethodImplOptions.NoInlining)]
		private static Exception CreateArrayTypeMismatchException()
		{
			return new ArrayTypeMismatchException();
		}

		// Token: 0x0600197D RID: 6525 RVA: 0x00054334 File Offset: 0x00052534
		[<1c2fb156-e9ba-45cc-af54-d5335bdb59af>DoesNotReturn]
		internal static void ThrowArgumentException_InvalidTypeWithPointersNotSupported(Type type)
		{
			throw ThrowHelper.CreateArgumentException_InvalidTypeWithPointersNotSupported(type);
		}

		// Token: 0x0600197E RID: 6526 RVA: 0x0005433C File Offset: 0x0005253C
		[MethodImpl(MethodImplOptions.NoInlining)]
		private static Exception CreateArgumentException_InvalidTypeWithPointersNotSupported(Type type)
		{
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(52, 1);
			defaultInterpolatedStringHandler.AppendLiteral("Type ");
			defaultInterpolatedStringHandler.AppendFormatted<Type>(type);
			defaultInterpolatedStringHandler.AppendLiteral(" with managed pointers cannot be used in a Span");
			return new ArgumentException(defaultInterpolatedStringHandler.ToStringAndClear());
		}

		// Token: 0x0600197F RID: 6527 RVA: 0x0005437F File Offset: 0x0005257F
		[<1c2fb156-e9ba-45cc-af54-d5335bdb59af>DoesNotReturn]
		internal static void ThrowArgumentException_DestinationTooShort()
		{
			throw ThrowHelper.CreateArgumentException_DestinationTooShort();
		}

		// Token: 0x06001980 RID: 6528 RVA: 0x00054386 File Offset: 0x00052586
		[MethodImpl(MethodImplOptions.NoInlining)]
		private static Exception CreateArgumentException_DestinationTooShort()
		{
			return new ArgumentException("Destination too short");
		}

		// Token: 0x06001981 RID: 6529 RVA: 0x00054392 File Offset: 0x00052592
		[<1c2fb156-e9ba-45cc-af54-d5335bdb59af>DoesNotReturn]
		internal static void ThrowArgumentException(string message, [Nullable(2)] string argument = null)
		{
			throw ThrowHelper.CreateArgumentException(message, argument);
		}

		// Token: 0x06001982 RID: 6530 RVA: 0x0005439B File Offset: 0x0005259B
		[MethodImpl(MethodImplOptions.NoInlining)]
		private static Exception CreateArgumentException(string message, [Nullable(2)] string argument)
		{
			return new ArgumentException(message, argument ?? "");
		}

		// Token: 0x06001983 RID: 6531 RVA: 0x000543AD File Offset: 0x000525AD
		[<1c2fb156-e9ba-45cc-af54-d5335bdb59af>DoesNotReturn]
		internal static void ThrowIndexOutOfRangeException()
		{
			throw ThrowHelper.CreateIndexOutOfRangeException();
		}

		// Token: 0x06001984 RID: 6532 RVA: 0x000543B4 File Offset: 0x000525B4
		[MethodImpl(MethodImplOptions.NoInlining)]
		private static Exception CreateIndexOutOfRangeException()
		{
			return new IndexOutOfRangeException();
		}

		// Token: 0x06001985 RID: 6533 RVA: 0x000543BB File Offset: 0x000525BB
		[<1c2fb156-e9ba-45cc-af54-d5335bdb59af>DoesNotReturn]
		internal static void ThrowArgumentOutOfRangeException()
		{
			throw ThrowHelper.CreateArgumentOutOfRangeException();
		}

		// Token: 0x06001986 RID: 6534 RVA: 0x000543C2 File Offset: 0x000525C2
		[MethodImpl(MethodImplOptions.NoInlining)]
		private static Exception CreateArgumentOutOfRangeException()
		{
			return new ArgumentOutOfRangeException();
		}

		// Token: 0x06001987 RID: 6535 RVA: 0x000543C9 File Offset: 0x000525C9
		[<1c2fb156-e9ba-45cc-af54-d5335bdb59af>DoesNotReturn]
		internal static void ThrowArgumentOutOfRangeException(ExceptionArgument argument)
		{
			throw ThrowHelper.CreateArgumentOutOfRangeException(argument);
		}

		// Token: 0x06001988 RID: 6536 RVA: 0x000543D1 File Offset: 0x000525D1
		[MethodImpl(MethodImplOptions.NoInlining)]
		private static Exception CreateArgumentOutOfRangeException(ExceptionArgument argument)
		{
			return new ArgumentOutOfRangeException(argument.ToString());
		}

		// Token: 0x06001989 RID: 6537 RVA: 0x000543E5 File Offset: 0x000525E5
		[<1c2fb156-e9ba-45cc-af54-d5335bdb59af>DoesNotReturn]
		internal static void ThrowArgumentOutOfRangeException_PrecisionTooLarge()
		{
			throw ThrowHelper.CreateArgumentOutOfRangeException_PrecisionTooLarge();
		}

		// Token: 0x0600198A RID: 6538 RVA: 0x000543EC File Offset: 0x000525EC
		[MethodImpl(MethodImplOptions.NoInlining)]
		private static Exception CreateArgumentOutOfRangeException_PrecisionTooLarge()
		{
			string paramName = "precision";
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(27, 1);
			defaultInterpolatedStringHandler.AppendLiteral("Precision too large (max: ");
			defaultInterpolatedStringHandler.AppendFormatted<byte>(99);
			defaultInterpolatedStringHandler.AppendLiteral(")");
			return new ArgumentOutOfRangeException(paramName, defaultInterpolatedStringHandler.ToStringAndClear());
		}

		// Token: 0x0600198B RID: 6539 RVA: 0x00054435 File Offset: 0x00052635
		[<1c2fb156-e9ba-45cc-af54-d5335bdb59af>DoesNotReturn]
		internal static void ThrowArgumentOutOfRangeException_SymbolDoesNotFit()
		{
			throw ThrowHelper.CreateArgumentOutOfRangeException_SymbolDoesNotFit();
		}

		// Token: 0x0600198C RID: 6540 RVA: 0x0005443C File Offset: 0x0005263C
		[MethodImpl(MethodImplOptions.NoInlining)]
		private static Exception CreateArgumentOutOfRangeException_SymbolDoesNotFit()
		{
			return new ArgumentOutOfRangeException("symbol", "Bad format specifier");
		}

		// Token: 0x0600198D RID: 6541 RVA: 0x0005444D File Offset: 0x0005264D
		[<1c2fb156-e9ba-45cc-af54-d5335bdb59af>DoesNotReturn]
		internal static void ThrowInvalidOperationException()
		{
			throw ThrowHelper.CreateInvalidOperationException();
		}

		// Token: 0x0600198E RID: 6542 RVA: 0x00054454 File Offset: 0x00052654
		[MethodImpl(MethodImplOptions.NoInlining)]
		private static Exception CreateInvalidOperationException()
		{
			return new InvalidOperationException();
		}

		// Token: 0x0600198F RID: 6543 RVA: 0x0005445B File Offset: 0x0005265B
		[<1c2fb156-e9ba-45cc-af54-d5335bdb59af>DoesNotReturn]
		internal static void ThrowInvalidOperationException_OutstandingReferences()
		{
			throw ThrowHelper.CreateInvalidOperationException_OutstandingReferences();
		}

		// Token: 0x06001990 RID: 6544 RVA: 0x00054462 File Offset: 0x00052662
		[MethodImpl(MethodImplOptions.NoInlining)]
		private static Exception CreateInvalidOperationException_OutstandingReferences()
		{
			return new InvalidOperationException("Outstanding references");
		}

		// Token: 0x06001991 RID: 6545 RVA: 0x0005446E File Offset: 0x0005266E
		[<1c2fb156-e9ba-45cc-af54-d5335bdb59af>DoesNotReturn]
		internal static void ThrowInvalidOperationException_UnexpectedSegmentType()
		{
			throw ThrowHelper.CreateInvalidOperationException_UnexpectedSegmentType();
		}

		// Token: 0x06001992 RID: 6546 RVA: 0x00054475 File Offset: 0x00052675
		[MethodImpl(MethodImplOptions.NoInlining)]
		private static Exception CreateInvalidOperationException_UnexpectedSegmentType()
		{
			return new InvalidOperationException("Unexpected segment type");
		}

		// Token: 0x06001993 RID: 6547 RVA: 0x00054481 File Offset: 0x00052681
		[<1c2fb156-e9ba-45cc-af54-d5335bdb59af>DoesNotReturn]
		internal static void ThrowInvalidOperationException_EndPositionNotReached()
		{
			throw ThrowHelper.CreateInvalidOperationException_EndPositionNotReached();
		}

		// Token: 0x06001994 RID: 6548 RVA: 0x00054488 File Offset: 0x00052688
		[MethodImpl(MethodImplOptions.NoInlining)]
		private static Exception CreateInvalidOperationException_EndPositionNotReached()
		{
			return new InvalidOperationException("End position not reached");
		}

		// Token: 0x06001995 RID: 6549 RVA: 0x00054494 File Offset: 0x00052694
		[<1c2fb156-e9ba-45cc-af54-d5335bdb59af>DoesNotReturn]
		internal static void ThrowArgumentOutOfRangeException_PositionOutOfRange()
		{
			throw ThrowHelper.CreateArgumentOutOfRangeException_PositionOutOfRange();
		}

		// Token: 0x06001996 RID: 6550 RVA: 0x0005449B File Offset: 0x0005269B
		[MethodImpl(MethodImplOptions.NoInlining)]
		private static Exception CreateArgumentOutOfRangeException_PositionOutOfRange()
		{
			return new ArgumentOutOfRangeException("position");
		}

		// Token: 0x06001997 RID: 6551 RVA: 0x000544A7 File Offset: 0x000526A7
		[<1c2fb156-e9ba-45cc-af54-d5335bdb59af>DoesNotReturn]
		internal static void ThrowArgumentOutOfRangeException_OffsetOutOfRange()
		{
			throw ThrowHelper.CreateArgumentOutOfRangeException_OffsetOutOfRange();
		}

		// Token: 0x06001998 RID: 6552 RVA: 0x000544AE File Offset: 0x000526AE
		[MethodImpl(MethodImplOptions.NoInlining)]
		private static Exception CreateArgumentOutOfRangeException_OffsetOutOfRange()
		{
			return new ArgumentOutOfRangeException("offset");
		}

		// Token: 0x06001999 RID: 6553 RVA: 0x000544BA File Offset: 0x000526BA
		[<1c2fb156-e9ba-45cc-af54-d5335bdb59af>DoesNotReturn]
		internal static void ThrowObjectDisposedException_ArrayMemoryPoolBuffer()
		{
			throw ThrowHelper.CreateObjectDisposedException_ArrayMemoryPoolBuffer();
		}

		// Token: 0x0600199A RID: 6554 RVA: 0x000544C1 File Offset: 0x000526C1
		[MethodImpl(MethodImplOptions.NoInlining)]
		private static Exception CreateObjectDisposedException_ArrayMemoryPoolBuffer()
		{
			return new ObjectDisposedException("ArrayMemoryPoolBuffer");
		}

		// Token: 0x0600199B RID: 6555 RVA: 0x000544CD File Offset: 0x000526CD
		[<1c2fb156-e9ba-45cc-af54-d5335bdb59af>DoesNotReturn]
		internal static void ThrowFormatException_BadFormatSpecifier()
		{
			throw ThrowHelper.CreateFormatException_BadFormatSpecifier();
		}

		// Token: 0x0600199C RID: 6556 RVA: 0x000544D4 File Offset: 0x000526D4
		[MethodImpl(MethodImplOptions.NoInlining)]
		private static Exception CreateFormatException_BadFormatSpecifier()
		{
			return new FormatException("Bad format specifier");
		}

		// Token: 0x0600199D RID: 6557 RVA: 0x000544E0 File Offset: 0x000526E0
		[<1c2fb156-e9ba-45cc-af54-d5335bdb59af>DoesNotReturn]
		internal static void ThrowArgumentException_OverlapAlignmentMismatch()
		{
			throw ThrowHelper.CreateArgumentException_OverlapAlignmentMismatch();
		}

		// Token: 0x0600199E RID: 6558 RVA: 0x000544E7 File Offset: 0x000526E7
		[MethodImpl(MethodImplOptions.NoInlining)]
		private static Exception CreateArgumentException_OverlapAlignmentMismatch()
		{
			return new ArgumentException("Overlap alignment mismatch");
		}

		// Token: 0x0600199F RID: 6559 RVA: 0x000544F3 File Offset: 0x000526F3
		[NullableContext(2)]
		[<1c2fb156-e9ba-45cc-af54-d5335bdb59af>DoesNotReturn]
		internal static void ThrowNotSupportedException(string msg = null)
		{
			throw ThrowHelper.CreateThrowNotSupportedException(msg);
		}

		// Token: 0x060019A0 RID: 6560 RVA: 0x000544FB File Offset: 0x000526FB
		[MethodImpl(MethodImplOptions.NoInlining)]
		private static Exception CreateThrowNotSupportedException([Nullable(2)] string msg)
		{
			return new NotSupportedException();
		}

		// Token: 0x060019A1 RID: 6561 RVA: 0x00054502 File Offset: 0x00052702
		[<1c2fb156-e9ba-45cc-af54-d5335bdb59af>DoesNotReturn]
		internal static void ThrowKeyNullException()
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.key);
		}

		// Token: 0x060019A2 RID: 6562 RVA: 0x0005450B File Offset: 0x0005270B
		[<1c2fb156-e9ba-45cc-af54-d5335bdb59af>DoesNotReturn]
		internal static void ThrowValueNullException()
		{
			throw ThrowHelper.CreateThrowValueNullException();
		}

		// Token: 0x060019A3 RID: 6563 RVA: 0x00054512 File Offset: 0x00052712
		[MethodImpl(MethodImplOptions.NoInlining)]
		private static Exception CreateThrowValueNullException()
		{
			return new ArgumentException("Value is null");
		}

		// Token: 0x060019A4 RID: 6564 RVA: 0x0005451E File Offset: 0x0005271E
		[<1c2fb156-e9ba-45cc-af54-d5335bdb59af>DoesNotReturn]
		internal static void ThrowOutOfMemoryException()
		{
			throw ThrowHelper.CreateOutOfMemoryException();
		}

		// Token: 0x060019A5 RID: 6565 RVA: 0x00054525 File Offset: 0x00052725
		[MethodImpl(MethodImplOptions.NoInlining)]
		private static Exception CreateOutOfMemoryException()
		{
			return new OutOfMemoryException();
		}

		// Token: 0x060019A6 RID: 6566 RVA: 0x0005452C File Offset: 0x0005272C
		public static bool TryFormatThrowFormatException(out int bytesWritten)
		{
			bytesWritten = 0;
			ThrowHelper.ThrowFormatException_BadFormatSpecifier();
			return false;
		}

		// Token: 0x060019A7 RID: 6567 RVA: 0x00054537 File Offset: 0x00052737
		public static bool TryParseThrowFormatException<[Nullable(2)] T>(out T value, out int bytesConsumed)
		{
			value = default(T);
			bytesConsumed = 0;
			ThrowHelper.ThrowFormatException_BadFormatSpecifier();
			return false;
		}

		// Token: 0x060019A8 RID: 6568 RVA: 0x00054549 File Offset: 0x00052749
		[NullableContext(2)]
		[<1c2fb156-e9ba-45cc-af54-d5335bdb59af>DoesNotReturn]
		public static void ThrowArgumentValidationException<T>([Nullable(new byte[] { 2, 1 })] ReadOnlySequenceSegment<T> startSegment, int startIndex, [Nullable(new byte[] { 2, 1 })] ReadOnlySequenceSegment<T> endSegment)
		{
			throw ThrowHelper.CreateArgumentValidationException<T>(startSegment, startIndex, endSegment);
		}

		// Token: 0x060019A9 RID: 6569 RVA: 0x00054554 File Offset: 0x00052754
		private static Exception CreateArgumentValidationException<[Nullable(2)] T>([Nullable(new byte[] { 2, 1 })] ReadOnlySequenceSegment<T> startSegment, int startIndex, [Nullable(new byte[] { 2, 1 })] ReadOnlySequenceSegment<T> endSegment)
		{
			if (startSegment == null)
			{
				return ThrowHelper.CreateArgumentNullException(ExceptionArgument.startSegment);
			}
			if (endSegment == null)
			{
				return ThrowHelper.CreateArgumentNullException(ExceptionArgument.endSegment);
			}
			if (startSegment != endSegment && startSegment.RunningIndex > endSegment.RunningIndex)
			{
				return ThrowHelper.CreateArgumentOutOfRangeException(ExceptionArgument.endSegment);
			}
			if (startSegment.Memory.Length < startIndex)
			{
				return ThrowHelper.CreateArgumentOutOfRangeException(ExceptionArgument.startIndex);
			}
			return ThrowHelper.CreateArgumentOutOfRangeException(ExceptionArgument.endIndex);
		}

		// Token: 0x060019AA RID: 6570 RVA: 0x000545B1 File Offset: 0x000527B1
		[NullableContext(2)]
		[<1c2fb156-e9ba-45cc-af54-d5335bdb59af>DoesNotReturn]
		public static void ThrowArgumentValidationException(Array array, int start)
		{
			throw ThrowHelper.CreateArgumentValidationException(array, start);
		}

		// Token: 0x060019AB RID: 6571 RVA: 0x000545BA File Offset: 0x000527BA
		private static Exception CreateArgumentValidationException([Nullable(2)] Array array, int start)
		{
			if (array == null)
			{
				return ThrowHelper.CreateArgumentNullException(ExceptionArgument.array);
			}
			if (start > array.Length)
			{
				return ThrowHelper.CreateArgumentOutOfRangeException(ExceptionArgument.start);
			}
			return ThrowHelper.CreateArgumentOutOfRangeException(ExceptionArgument.length);
		}

		// Token: 0x060019AC RID: 6572 RVA: 0x000545E0 File Offset: 0x000527E0
		[<1c2fb156-e9ba-45cc-af54-d5335bdb59af>DoesNotReturn]
		internal static void ThrowArgumentException_TupleIncorrectType(object other)
		{
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(38, 1);
			defaultInterpolatedStringHandler.AppendLiteral("Value tuple of incorrect type (found ");
			defaultInterpolatedStringHandler.AppendFormatted<Type>(other.GetType());
			defaultInterpolatedStringHandler.AppendLiteral(")");
			throw new ArgumentException(defaultInterpolatedStringHandler.ToStringAndClear(), "other");
		}

		// Token: 0x060019AD RID: 6573 RVA: 0x0005462D File Offset: 0x0005282D
		[<1c2fb156-e9ba-45cc-af54-d5335bdb59af>DoesNotReturn]
		public static void ThrowStartOrEndArgumentValidationException(long start)
		{
			throw ThrowHelper.CreateStartOrEndArgumentValidationException(start);
		}

		// Token: 0x060019AE RID: 6574 RVA: 0x00054635 File Offset: 0x00052835
		private static Exception CreateStartOrEndArgumentValidationException(long start)
		{
			if (start < 0L)
			{
				return ThrowHelper.CreateArgumentOutOfRangeException(ExceptionArgument.start);
			}
			return ThrowHelper.CreateArgumentOutOfRangeException(ExceptionArgument.length);
		}
	}
}
