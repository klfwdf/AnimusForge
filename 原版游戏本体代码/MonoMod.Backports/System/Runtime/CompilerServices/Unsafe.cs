using System;
using MonoMod.Backports.ILHelpers;

namespace System.Runtime.CompilerServices
{
	// Token: 0x0200003C RID: 60
	[CLSCompliant(false)]
	public static class Unsafe
	{
		// Token: 0x06000245 RID: 581 RVA: 0x0000BEC2 File Offset: 0x0000A0C2
		[NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static T Read<T>(void* source)
		{
			return UnsafeRaw.Read<T>(source);
		}

		// Token: 0x06000246 RID: 582 RVA: 0x0000BECA File Offset: 0x0000A0CA
		[NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static T ReadUnaligned<T>(void* source)
		{
			return UnsafeRaw.ReadUnaligned<T>(source);
		}

		// Token: 0x06000247 RID: 583 RVA: 0x0000BED2 File Offset: 0x0000A0D2
		[NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static T ReadUnaligned<T>(ref byte source)
		{
			return UnsafeRaw.ReadUnaligned<T>(ref source);
		}

		// Token: 0x06000248 RID: 584 RVA: 0x0000BEDA File Offset: 0x0000A0DA
		[NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static void Write<T>(void* destination, T value)
		{
			UnsafeRaw.Write<T>(destination, value);
		}

		// Token: 0x06000249 RID: 585 RVA: 0x0000BEE3 File Offset: 0x0000A0E3
		[NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static void WriteUnaligned<T>(void* destination, T value)
		{
			UnsafeRaw.WriteUnaligned<T>(destination, value);
		}

		// Token: 0x0600024A RID: 586 RVA: 0x0000BEEC File Offset: 0x0000A0EC
		[NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void WriteUnaligned<T>(ref byte destination, T value)
		{
			UnsafeRaw.WriteUnaligned<T>(ref destination, value);
		}

		// Token: 0x0600024B RID: 587 RVA: 0x0000BEF5 File Offset: 0x0000A0F5
		[NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static void Copy<T>(void* destination, ref T source)
		{
			UnsafeRaw.Copy<T>(destination, ref source);
		}

		// Token: 0x0600024C RID: 588 RVA: 0x0000BEFE File Offset: 0x0000A0FE
		[NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static void Copy<T>(ref T destination, void* source)
		{
			UnsafeRaw.Copy<T>(ref destination, source);
		}

		// Token: 0x0600024D RID: 589 RVA: 0x0000BF07 File Offset: 0x0000A107
		[NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static void* AsPointer<T>(ref T value)
		{
			return UnsafeRaw.AsPointer<T>(ref value);
		}

		// Token: 0x0600024E RID: 590 RVA: 0x0000BF0F File Offset: 0x0000A10F
		[NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void SkipInit<T>(out T value)
		{
			UnsafeRaw.SkipInit<T>(out value);
		}

		// Token: 0x0600024F RID: 591 RVA: 0x0000BF17 File Offset: 0x0000A117
		[NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static void CopyBlock(void* destination, void* source, uint byteCount)
		{
			UnsafeRaw.CopyBlock(destination, source, byteCount);
		}

		// Token: 0x06000250 RID: 592 RVA: 0x0000BF21 File Offset: 0x0000A121
		[NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void CopyBlock(ref byte destination, ref byte source, uint byteCount)
		{
			UnsafeRaw.CopyBlock(ref destination, ref source, byteCount);
		}

		// Token: 0x06000251 RID: 593 RVA: 0x0000BF2B File Offset: 0x0000A12B
		[NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static void CopyBlockUnaligned(void* destination, void* source, uint byteCount)
		{
			UnsafeRaw.CopyBlockUnaligned(destination, source, byteCount);
		}

		// Token: 0x06000252 RID: 594 RVA: 0x0000BF35 File Offset: 0x0000A135
		[NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void CopyBlockUnaligned(ref byte destination, ref byte source, uint byteCount)
		{
			UnsafeRaw.CopyBlockUnaligned(ref destination, ref source, byteCount);
		}

		// Token: 0x06000253 RID: 595 RVA: 0x0000BF3F File Offset: 0x0000A13F
		[NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static void InitBlock(void* startAddress, byte value, uint byteCount)
		{
			UnsafeRaw.InitBlock(startAddress, value, byteCount);
		}

		// Token: 0x06000254 RID: 596 RVA: 0x0000BF49 File Offset: 0x0000A149
		[NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void InitBlock(ref byte startAddress, byte value, uint byteCount)
		{
			UnsafeRaw.InitBlock(ref startAddress, value, byteCount);
		}

		// Token: 0x06000255 RID: 597 RVA: 0x0000BF53 File Offset: 0x0000A153
		[NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static void InitBlockUnaligned(void* startAddress, byte value, uint byteCount)
		{
			UnsafeRaw.InitBlockUnaligned(startAddress, value, byteCount);
		}

		// Token: 0x06000256 RID: 598 RVA: 0x0000BF5D File Offset: 0x0000A15D
		[NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void InitBlockUnaligned(ref byte startAddress, byte value, uint byteCount)
		{
			UnsafeRaw.InitBlockUnaligned(ref startAddress, value, byteCount);
		}

		// Token: 0x06000257 RID: 599 RVA: 0x0000BF67 File Offset: 0x0000A167
		[NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static T As<T>(object o) where T : class
		{
			return UnsafeRaw.As<T>(o);
		}

		// Token: 0x06000258 RID: 600 RVA: 0x0000BF6F File Offset: 0x0000A16F
		[NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static ref T AsRef<T>(void* source)
		{
			return UnsafeRaw.AsRef<T>(source);
		}

		// Token: 0x06000259 RID: 601 RVA: 0x0000BF77 File Offset: 0x0000A177
		[NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ref T AsRef<T>(in T source)
		{
			return UnsafeRaw.AsRef<T>(source);
		}

		// Token: 0x0600025A RID: 602 RVA: 0x0000BF7F File Offset: 0x0000A17F
		[NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ref TTo As<TFrom, TTo>(ref TFrom source)
		{
			return UnsafeRaw.As<TFrom, TTo>(ref source);
		}

		// Token: 0x0600025B RID: 603 RVA: 0x0000BF87 File Offset: 0x0000A187
		[NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ref T Unbox<T>(object box) where T : struct
		{
			return UnsafeRaw.Unbox<T>(box);
		}

		// Token: 0x0600025C RID: 604 RVA: 0x0000BF8F File Offset: 0x0000A18F
		[NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ref T AddByteOffset<T>(ref T source, [NativeInteger] IntPtr byteOffset)
		{
			return UnsafeRaw.AddByteOffset<T>(ref source, byteOffset);
		}

		// Token: 0x0600025D RID: 605 RVA: 0x0000BF98 File Offset: 0x0000A198
		[NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ref T AddByteOffset<T>(ref T source, [NativeInteger] UIntPtr byteOffset)
		{
			return UnsafeRaw.AddByteOffset<T>(ref source, byteOffset);
		}

		// Token: 0x0600025E RID: 606 RVA: 0x0000BFA1 File Offset: 0x0000A1A1
		[NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ref T SubtractByteOffset<T>(ref T source, [NativeInteger] IntPtr byteOffset)
		{
			return UnsafeRaw.SubtractByteOffset<T>(ref source, byteOffset);
		}

		// Token: 0x0600025F RID: 607 RVA: 0x0000BFAA File Offset: 0x0000A1AA
		[NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ref T SubtractByteOffset<T>(ref T source, [NativeInteger] UIntPtr byteOffset)
		{
			return UnsafeRaw.SubtractByteOffset<T>(ref source, byteOffset);
		}

		// Token: 0x06000260 RID: 608 RVA: 0x0000BFB3 File Offset: 0x0000A1B3
		[NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[return: NativeInteger]
		public static IntPtr ByteOffset<T>(ref T origin, ref T target)
		{
			return UnsafeRaw.ByteOffset<T>(ref origin, ref target);
		}

		// Token: 0x06000261 RID: 609 RVA: 0x0000BFBC File Offset: 0x0000A1BC
		[NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool AreSame<T>(ref T left, ref T right)
		{
			return UnsafeRaw.AreSame<T>(ref left, ref right);
		}

		// Token: 0x06000262 RID: 610 RVA: 0x0000BFC5 File Offset: 0x0000A1C5
		[NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsAddressGreaterThan<T>(ref T left, ref T right)
		{
			return UnsafeRaw.IsAddressGreaterThan<T>(ref left, ref right);
		}

		// Token: 0x06000263 RID: 611 RVA: 0x0000BFCE File Offset: 0x0000A1CE
		[NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsAddressLessThan<T>(ref T left, ref T right)
		{
			return UnsafeRaw.IsAddressLessThan<T>(ref left, ref right);
		}

		// Token: 0x06000264 RID: 612 RVA: 0x0000BFD7 File Offset: 0x0000A1D7
		[NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsNullRef<T>(ref T source)
		{
			return UnsafeRaw.IsNullRef<T>(ref source);
		}

		// Token: 0x06000265 RID: 613 RVA: 0x0000BFDF File Offset: 0x0000A1DF
		[NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ref T NullRef<T>()
		{
			return UnsafeRaw.NullRef<T>();
		}

		// Token: 0x06000266 RID: 614 RVA: 0x0000BFE6 File Offset: 0x0000A1E6
		[NullableContext(2)]
		[NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int SizeOf<T>()
		{
			return (int)Unsafe.PerTypeValues<T>.TypeSize;
		}

		// Token: 0x06000267 RID: 615 RVA: 0x0000BFEE File Offset: 0x0000A1EE
		[NullableContext(1)]
		[NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ref T Add<[Nullable(2)] T>(ref T source, int elementOffset)
		{
			return UnsafeRaw.AddByteOffset<T>(ref source, (IntPtr)elementOffset * Unsafe.PerTypeValues<T>.TypeSize);
		}

		// Token: 0x06000268 RID: 616 RVA: 0x0000BFFE File Offset: 0x0000A1FE
		[NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static void* Add<[Nullable(2)] T>(void* source, int elementOffset)
		{
			return (void*)((byte*)source + (long)((IntPtr)elementOffset * Unsafe.PerTypeValues<T>.TypeSize));
		}

		// Token: 0x06000269 RID: 617 RVA: 0x0000C00C File Offset: 0x0000A20C
		[NullableContext(1)]
		[NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ref T Add<[Nullable(2)] T>(ref T source, [NativeInteger] IntPtr elementOffset)
		{
			return UnsafeRaw.AddByteOffset<T>(ref source, elementOffset * Unsafe.PerTypeValues<T>.TypeSize);
		}

		// Token: 0x0600026A RID: 618 RVA: 0x0000C01B File Offset: 0x0000A21B
		[NullableContext(1)]
		[NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ref T Add<[Nullable(2)] T>(ref T source, [NativeInteger] UIntPtr elementOffset)
		{
			return UnsafeRaw.AddByteOffset<T>(ref source, elementOffset * (UIntPtr)Unsafe.PerTypeValues<T>.TypeSize);
		}

		// Token: 0x0600026B RID: 619 RVA: 0x0000C02A File Offset: 0x0000A22A
		[NullableContext(1)]
		[NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ref T Subtract<[Nullable(2)] T>(ref T source, int elementOffset)
		{
			return UnsafeRaw.SubtractByteOffset<T>(ref source, (IntPtr)elementOffset * Unsafe.PerTypeValues<T>.TypeSize);
		}

		// Token: 0x0600026C RID: 620 RVA: 0x0000C03A File Offset: 0x0000A23A
		[NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static void* Subtract<[Nullable(2)] T>(void* source, int elementOffset)
		{
			return (void*)((byte*)source - (long)((IntPtr)elementOffset * Unsafe.PerTypeValues<T>.TypeSize));
		}

		// Token: 0x0600026D RID: 621 RVA: 0x0000C048 File Offset: 0x0000A248
		[NullableContext(1)]
		[NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ref T Subtract<[Nullable(2)] T>(ref T source, [NativeInteger] IntPtr elementOffset)
		{
			return UnsafeRaw.SubtractByteOffset<T>(ref source, elementOffset * Unsafe.PerTypeValues<T>.TypeSize);
		}

		// Token: 0x0600026E RID: 622 RVA: 0x0000C057 File Offset: 0x0000A257
		[NullableContext(1)]
		[NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ref T Subtract<[Nullable(2)] T>(ref T source, [NativeInteger] UIntPtr elementOffset)
		{
			return UnsafeRaw.SubtractByteOffset<T>(ref source, elementOffset * (UIntPtr)Unsafe.PerTypeValues<T>.TypeSize);
		}

		// Token: 0x0200006D RID: 109
		private static class PerTypeValues<[Nullable(2)] T>
		{
			// Token: 0x060002F0 RID: 752 RVA: 0x0000D500 File Offset: 0x0000B700
			[return: NativeInteger]
			private static IntPtr ComputeTypeSize()
			{
				T[] array = new T[2];
				return UnsafeRaw.ByteOffset<T>(ref array[0], ref array[1]);
			}

			// Token: 0x040000D4 RID: 212
			[NativeInteger]
			public static readonly IntPtr TypeSize = Unsafe.PerTypeValues<T>.ComputeTypeSize();
		}
	}
}
