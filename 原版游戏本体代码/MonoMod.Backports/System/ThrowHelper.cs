using System;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System
{
	// Token: 0x0200001E RID: 30
	[NullableContext(1)]
	[Nullable(0)]
	internal static class ThrowHelper
	{
		// Token: 0x06000124 RID: 292 RVA: 0x00009238 File Offset: 0x00007438
		[NullableContext(2)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static void ThrowIfArgumentNull([NotNull] object obj, ExceptionArgument argument)
		{
			if (obj == null)
			{
				ThrowHelper.ThrowArgumentNullException(argument);
			}
		}

		// Token: 0x06000125 RID: 293 RVA: 0x00009243 File Offset: 0x00007443
		[NullableContext(2)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static void ThrowIfArgumentNull([NotNull] object obj, [Nullable(1)] string argument, string message = null)
		{
			if (obj == null)
			{
				ThrowHelper.ThrowArgumentNullException(argument, message);
			}
		}

		// Token: 0x06000126 RID: 294 RVA: 0x0000924F File Offset: 0x0000744F
		[DoesNotReturn]
		internal static void ThrowArgumentNullException(ExceptionArgument argument)
		{
			throw ThrowHelper.CreateArgumentNullException(argument);
		}

		// Token: 0x06000127 RID: 295 RVA: 0x00009257 File Offset: 0x00007457
		[DoesNotReturn]
		internal static void ThrowArgumentNullException(string argument, [Nullable(2)] string message = null)
		{
			throw ThrowHelper.CreateArgumentNullException(argument, message);
		}

		// Token: 0x06000128 RID: 296 RVA: 0x00009260 File Offset: 0x00007460
		[MethodImpl(MethodImplOptions.NoInlining)]
		private static Exception CreateArgumentNullException(ExceptionArgument argument)
		{
			return ThrowHelper.CreateArgumentNullException(argument.ToString(), null);
		}

		// Token: 0x06000129 RID: 297 RVA: 0x00009275 File Offset: 0x00007475
		[MethodImpl(MethodImplOptions.NoInlining)]
		private static Exception CreateArgumentNullException(string argument, [Nullable(2)] string message = null)
		{
			return new ArgumentNullException(argument, message);
		}

		// Token: 0x0600012A RID: 298 RVA: 0x0000927E File Offset: 0x0000747E
		[DoesNotReturn]
		internal static void ThrowArrayTypeMismatchException()
		{
			throw ThrowHelper.CreateArrayTypeMismatchException();
		}

		// Token: 0x0600012B RID: 299 RVA: 0x00009285 File Offset: 0x00007485
		[MethodImpl(MethodImplOptions.NoInlining)]
		private static Exception CreateArrayTypeMismatchException()
		{
			return new ArrayTypeMismatchException();
		}

		// Token: 0x0600012C RID: 300 RVA: 0x0000928C File Offset: 0x0000748C
		[DoesNotReturn]
		internal static void ThrowArgumentException_InvalidTypeWithPointersNotSupported(Type type)
		{
			throw ThrowHelper.CreateArgumentException_InvalidTypeWithPointersNotSupported(type);
		}

		// Token: 0x0600012D RID: 301 RVA: 0x00009294 File Offset: 0x00007494
		[MethodImpl(MethodImplOptions.NoInlining)]
		private static Exception CreateArgumentException_InvalidTypeWithPointersNotSupported(Type type)
		{
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(52, 1);
			defaultInterpolatedStringHandler.AppendLiteral("Type ");
			defaultInterpolatedStringHandler.AppendFormatted<Type>(type);
			defaultInterpolatedStringHandler.AppendLiteral(" with managed pointers cannot be used in a Span");
			return new ArgumentException(defaultInterpolatedStringHandler.ToStringAndClear());
		}

		// Token: 0x0600012E RID: 302 RVA: 0x000092D7 File Offset: 0x000074D7
		[DoesNotReturn]
		internal static void ThrowArgumentException_DestinationTooShort()
		{
			throw ThrowHelper.CreateArgumentException_DestinationTooShort();
		}

		// Token: 0x0600012F RID: 303 RVA: 0x000092DE File Offset: 0x000074DE
		[MethodImpl(MethodImplOptions.NoInlining)]
		private static Exception CreateArgumentException_DestinationTooShort()
		{
			return new ArgumentException("Destination too short");
		}

		// Token: 0x06000130 RID: 304 RVA: 0x000092EA File Offset: 0x000074EA
		[DoesNotReturn]
		internal static void ThrowArgumentException(string message, [Nullable(2)] string argument = null)
		{
			throw ThrowHelper.CreateArgumentException(message, argument);
		}

		// Token: 0x06000131 RID: 305 RVA: 0x000092F3 File Offset: 0x000074F3
		[MethodImpl(MethodImplOptions.NoInlining)]
		private static Exception CreateArgumentException(string message, [Nullable(2)] string argument)
		{
			return new ArgumentException(message, argument ?? "");
		}

		// Token: 0x06000132 RID: 306 RVA: 0x00009305 File Offset: 0x00007505
		[DoesNotReturn]
		internal static void ThrowIndexOutOfRangeException()
		{
			throw ThrowHelper.CreateIndexOutOfRangeException();
		}

		// Token: 0x06000133 RID: 307 RVA: 0x0000930C File Offset: 0x0000750C
		[MethodImpl(MethodImplOptions.NoInlining)]
		private static Exception CreateIndexOutOfRangeException()
		{
			return new IndexOutOfRangeException();
		}

		// Token: 0x06000134 RID: 308 RVA: 0x00009313 File Offset: 0x00007513
		[DoesNotReturn]
		internal static void ThrowArgumentOutOfRangeException()
		{
			throw ThrowHelper.CreateArgumentOutOfRangeException();
		}

		// Token: 0x06000135 RID: 309 RVA: 0x0000931A File Offset: 0x0000751A
		[MethodImpl(MethodImplOptions.NoInlining)]
		private static Exception CreateArgumentOutOfRangeException()
		{
			return new ArgumentOutOfRangeException();
		}

		// Token: 0x06000136 RID: 310 RVA: 0x00009321 File Offset: 0x00007521
		[DoesNotReturn]
		internal static void ThrowArgumentOutOfRangeException(ExceptionArgument argument)
		{
			throw ThrowHelper.CreateArgumentOutOfRangeException(argument);
		}

		// Token: 0x06000137 RID: 311 RVA: 0x00009329 File Offset: 0x00007529
		[MethodImpl(MethodImplOptions.NoInlining)]
		private static Exception CreateArgumentOutOfRangeException(ExceptionArgument argument)
		{
			return new ArgumentOutOfRangeException(argument.ToString());
		}

		// Token: 0x06000138 RID: 312 RVA: 0x0000933D File Offset: 0x0000753D
		[DoesNotReturn]
		internal static void ThrowArgumentOutOfRangeException_PrecisionTooLarge()
		{
			throw ThrowHelper.CreateArgumentOutOfRangeException_PrecisionTooLarge();
		}

		// Token: 0x06000139 RID: 313 RVA: 0x00009344 File Offset: 0x00007544
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

		// Token: 0x0600013A RID: 314 RVA: 0x0000938D File Offset: 0x0000758D
		[DoesNotReturn]
		internal static void ThrowArgumentOutOfRangeException_SymbolDoesNotFit()
		{
			throw ThrowHelper.CreateArgumentOutOfRangeException_SymbolDoesNotFit();
		}

		// Token: 0x0600013B RID: 315 RVA: 0x00009394 File Offset: 0x00007594
		[MethodImpl(MethodImplOptions.NoInlining)]
		private static Exception CreateArgumentOutOfRangeException_SymbolDoesNotFit()
		{
			return new ArgumentOutOfRangeException("symbol", "Bad format specifier");
		}

		// Token: 0x0600013C RID: 316 RVA: 0x000093A5 File Offset: 0x000075A5
		[DoesNotReturn]
		internal static void ThrowInvalidOperationException()
		{
			throw ThrowHelper.CreateInvalidOperationException();
		}

		// Token: 0x0600013D RID: 317 RVA: 0x000093AC File Offset: 0x000075AC
		[MethodImpl(MethodImplOptions.NoInlining)]
		private static Exception CreateInvalidOperationException()
		{
			return new InvalidOperationException();
		}

		// Token: 0x0600013E RID: 318 RVA: 0x000093B3 File Offset: 0x000075B3
		[DoesNotReturn]
		internal static void ThrowInvalidOperationException_OutstandingReferences()
		{
			throw ThrowHelper.CreateInvalidOperationException_OutstandingReferences();
		}

		// Token: 0x0600013F RID: 319 RVA: 0x000093BA File Offset: 0x000075BA
		[MethodImpl(MethodImplOptions.NoInlining)]
		private static Exception CreateInvalidOperationException_OutstandingReferences()
		{
			return new InvalidOperationException("Outstanding references");
		}

		// Token: 0x06000140 RID: 320 RVA: 0x000093C6 File Offset: 0x000075C6
		[DoesNotReturn]
		internal static void ThrowInvalidOperationException_UnexpectedSegmentType()
		{
			throw ThrowHelper.CreateInvalidOperationException_UnexpectedSegmentType();
		}

		// Token: 0x06000141 RID: 321 RVA: 0x000093CD File Offset: 0x000075CD
		[MethodImpl(MethodImplOptions.NoInlining)]
		private static Exception CreateInvalidOperationException_UnexpectedSegmentType()
		{
			return new InvalidOperationException("Unexpected segment type");
		}

		// Token: 0x06000142 RID: 322 RVA: 0x000093D9 File Offset: 0x000075D9
		[DoesNotReturn]
		internal static void ThrowInvalidOperationException_EndPositionNotReached()
		{
			throw ThrowHelper.CreateInvalidOperationException_EndPositionNotReached();
		}

		// Token: 0x06000143 RID: 323 RVA: 0x000093E0 File Offset: 0x000075E0
		[MethodImpl(MethodImplOptions.NoInlining)]
		private static Exception CreateInvalidOperationException_EndPositionNotReached()
		{
			return new InvalidOperationException("End position not reached");
		}

		// Token: 0x06000144 RID: 324 RVA: 0x000093EC File Offset: 0x000075EC
		[DoesNotReturn]
		internal static void ThrowArgumentOutOfRangeException_PositionOutOfRange()
		{
			throw ThrowHelper.CreateArgumentOutOfRangeException_PositionOutOfRange();
		}

		// Token: 0x06000145 RID: 325 RVA: 0x000093F3 File Offset: 0x000075F3
		[MethodImpl(MethodImplOptions.NoInlining)]
		private static Exception CreateArgumentOutOfRangeException_PositionOutOfRange()
		{
			return new ArgumentOutOfRangeException("position");
		}

		// Token: 0x06000146 RID: 326 RVA: 0x000093FF File Offset: 0x000075FF
		[DoesNotReturn]
		internal static void ThrowArgumentOutOfRangeException_OffsetOutOfRange()
		{
			throw ThrowHelper.CreateArgumentOutOfRangeException_OffsetOutOfRange();
		}

		// Token: 0x06000147 RID: 327 RVA: 0x00009406 File Offset: 0x00007606
		[MethodImpl(MethodImplOptions.NoInlining)]
		private static Exception CreateArgumentOutOfRangeException_OffsetOutOfRange()
		{
			return new ArgumentOutOfRangeException("offset");
		}

		// Token: 0x06000148 RID: 328 RVA: 0x00009412 File Offset: 0x00007612
		[DoesNotReturn]
		internal static void ThrowObjectDisposedException_ArrayMemoryPoolBuffer()
		{
			throw ThrowHelper.CreateObjectDisposedException_ArrayMemoryPoolBuffer();
		}

		// Token: 0x06000149 RID: 329 RVA: 0x00009419 File Offset: 0x00007619
		[MethodImpl(MethodImplOptions.NoInlining)]
		private static Exception CreateObjectDisposedException_ArrayMemoryPoolBuffer()
		{
			return new ObjectDisposedException("ArrayMemoryPoolBuffer");
		}

		// Token: 0x0600014A RID: 330 RVA: 0x00009425 File Offset: 0x00007625
		[DoesNotReturn]
		internal static void ThrowFormatException_BadFormatSpecifier()
		{
			throw ThrowHelper.CreateFormatException_BadFormatSpecifier();
		}

		// Token: 0x0600014B RID: 331 RVA: 0x0000942C File Offset: 0x0000762C
		[MethodImpl(MethodImplOptions.NoInlining)]
		private static Exception CreateFormatException_BadFormatSpecifier()
		{
			return new FormatException("Bad format specifier");
		}

		// Token: 0x0600014C RID: 332 RVA: 0x00009438 File Offset: 0x00007638
		[DoesNotReturn]
		internal static void ThrowArgumentException_OverlapAlignmentMismatch()
		{
			throw ThrowHelper.CreateArgumentException_OverlapAlignmentMismatch();
		}

		// Token: 0x0600014D RID: 333 RVA: 0x0000943F File Offset: 0x0000763F
		[MethodImpl(MethodImplOptions.NoInlining)]
		private static Exception CreateArgumentException_OverlapAlignmentMismatch()
		{
			return new ArgumentException("Overlap alignment mismatch");
		}

		// Token: 0x0600014E RID: 334 RVA: 0x0000944B File Offset: 0x0000764B
		[NullableContext(2)]
		[DoesNotReturn]
		internal static void ThrowNotSupportedException(string msg = null)
		{
			throw ThrowHelper.CreateThrowNotSupportedException(msg);
		}

		// Token: 0x0600014F RID: 335 RVA: 0x00009453 File Offset: 0x00007653
		[MethodImpl(MethodImplOptions.NoInlining)]
		private static Exception CreateThrowNotSupportedException([Nullable(2)] string msg)
		{
			return new NotSupportedException();
		}

		// Token: 0x06000150 RID: 336 RVA: 0x0000945A File Offset: 0x0000765A
		[DoesNotReturn]
		internal static void ThrowKeyNullException()
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.key);
		}

		// Token: 0x06000151 RID: 337 RVA: 0x00009463 File Offset: 0x00007663
		[DoesNotReturn]
		internal static void ThrowValueNullException()
		{
			throw ThrowHelper.CreateThrowValueNullException();
		}

		// Token: 0x06000152 RID: 338 RVA: 0x0000946A File Offset: 0x0000766A
		[MethodImpl(MethodImplOptions.NoInlining)]
		private static Exception CreateThrowValueNullException()
		{
			return new ArgumentException("Value is null");
		}

		// Token: 0x06000153 RID: 339 RVA: 0x00009476 File Offset: 0x00007676
		[DoesNotReturn]
		internal static void ThrowOutOfMemoryException()
		{
			throw ThrowHelper.CreateOutOfMemoryException();
		}

		// Token: 0x06000154 RID: 340 RVA: 0x0000947D File Offset: 0x0000767D
		[MethodImpl(MethodImplOptions.NoInlining)]
		private static Exception CreateOutOfMemoryException()
		{
			return new OutOfMemoryException();
		}

		// Token: 0x06000155 RID: 341 RVA: 0x00009484 File Offset: 0x00007684
		public static bool TryFormatThrowFormatException(out int bytesWritten)
		{
			bytesWritten = 0;
			ThrowHelper.ThrowFormatException_BadFormatSpecifier();
			return false;
		}

		// Token: 0x06000156 RID: 342 RVA: 0x0000948F File Offset: 0x0000768F
		public static bool TryParseThrowFormatException<[Nullable(2)] T>(out T value, out int bytesConsumed)
		{
			value = default(T);
			bytesConsumed = 0;
			ThrowHelper.ThrowFormatException_BadFormatSpecifier();
			return false;
		}

		// Token: 0x06000157 RID: 343 RVA: 0x000094A1 File Offset: 0x000076A1
		[NullableContext(2)]
		[DoesNotReturn]
		public static void ThrowArgumentValidationException<T>([Nullable(new byte[] { 2, 1 })] ReadOnlySequenceSegment<T> startSegment, int startIndex, [Nullable(new byte[] { 2, 1 })] ReadOnlySequenceSegment<T> endSegment)
		{
			throw ThrowHelper.CreateArgumentValidationException<T>(startSegment, startIndex, endSegment);
		}

		// Token: 0x06000158 RID: 344 RVA: 0x000094AC File Offset: 0x000076AC
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

		// Token: 0x06000159 RID: 345 RVA: 0x00009509 File Offset: 0x00007709
		[NullableContext(2)]
		[DoesNotReturn]
		public static void ThrowArgumentValidationException(Array array, int start)
		{
			throw ThrowHelper.CreateArgumentValidationException(array, start);
		}

		// Token: 0x0600015A RID: 346 RVA: 0x00009512 File Offset: 0x00007712
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

		// Token: 0x0600015B RID: 347 RVA: 0x00009538 File Offset: 0x00007738
		[DoesNotReturn]
		internal static void ThrowArgumentException_TupleIncorrectType(object other)
		{
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(38, 1);
			defaultInterpolatedStringHandler.AppendLiteral("Value tuple of incorrect type (found ");
			defaultInterpolatedStringHandler.AppendFormatted<Type>(other.GetType());
			defaultInterpolatedStringHandler.AppendLiteral(")");
			throw new ArgumentException(defaultInterpolatedStringHandler.ToStringAndClear(), "other");
		}

		// Token: 0x0600015C RID: 348 RVA: 0x00009585 File Offset: 0x00007785
		[DoesNotReturn]
		public static void ThrowStartOrEndArgumentValidationException(long start)
		{
			throw ThrowHelper.CreateStartOrEndArgumentValidationException(start);
		}

		// Token: 0x0600015D RID: 349 RVA: 0x0000958D File Offset: 0x0000778D
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
