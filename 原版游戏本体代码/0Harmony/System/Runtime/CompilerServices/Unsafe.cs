using System;
using MonoMod.Backports.ILHelpers;

namespace System.Runtime.CompilerServices
{
	// Token: 0x020004AC RID: 1196
	[CLSCompliant(false)]
	internal static class Unsafe
	{
		// Token: 0x06001AC0 RID: 6848 RVA: 0x000575B6 File Offset: 0x000557B6
		[NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static T Read<T>(void* source)
		{
			return UnsafeRaw.Read<T>(source);
		}

		// Token: 0x06001AC1 RID: 6849 RVA: 0x000575BE File Offset: 0x000557BE
		[NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static T ReadUnaligned<T>(void* source)
		{
			return UnsafeRaw.ReadUnaligned<T>(source);
		}

		// Token: 0x06001AC2 RID: 6850 RVA: 0x000575C6 File Offset: 0x000557C6
		[NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static T ReadUnaligned<T>(ref byte source)
		{
			return UnsafeRaw.ReadUnaligned<T>(ref source);
		}

		// Token: 0x06001AC3 RID: 6851 RVA: 0x000575CE File Offset: 0x000557CE
		[NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static void Write<T>(void* destination, T value)
		{
			UnsafeRaw.Write<T>(destination, value);
		}

		// Token: 0x06001AC4 RID: 6852 RVA: 0x000575D7 File Offset: 0x000557D7
		[NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static void WriteUnaligned<T>(void* destination, T value)
		{
			UnsafeRaw.WriteUnaligned<T>(destination, value);
		}

		// Token: 0x06001AC5 RID: 6853 RVA: 0x000575E0 File Offset: 0x000557E0
		[NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void WriteUnaligned<T>(ref byte destination, T value)
		{
			UnsafeRaw.WriteUnaligned<T>(ref destination, value);
		}

		// Token: 0x06001AC6 RID: 6854 RVA: 0x000575E9 File Offset: 0x000557E9
		[NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static void Copy<T>(void* destination, ref T source)
		{
			UnsafeRaw.Copy<T>(destination, ref source);
		}

		// Token: 0x06001AC7 RID: 6855 RVA: 0x000575F2 File Offset: 0x000557F2
		[NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static void Copy<T>(ref T destination, void* source)
		{
			UnsafeRaw.Copy<T>(ref destination, source);
		}

		// Token: 0x06001AC8 RID: 6856 RVA: 0x000575FB File Offset: 0x000557FB
		[NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static void* AsPointer<T>(ref T value)
		{
			return UnsafeRaw.AsPointer<T>(ref value);
		}

		// Token: 0x06001AC9 RID: 6857 RVA: 0x00057603 File Offset: 0x00055803
		[NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void SkipInit<T>(out T value)
		{
			UnsafeRaw.SkipInit<T>(out value);
		}

		// Token: 0x06001ACA RID: 6858 RVA: 0x0005760B File Offset: 0x0005580B
		[NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static void CopyBlock(void* destination, void* source, uint byteCount)
		{
			UnsafeRaw.CopyBlock(destination, source, byteCount);
		}

		// Token: 0x06001ACB RID: 6859 RVA: 0x00057615 File Offset: 0x00055815
		[NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void CopyBlock(ref byte destination, ref byte source, uint byteCount)
		{
			UnsafeRaw.CopyBlock(ref destination, ref source, byteCount);
		}

		// Token: 0x06001ACC RID: 6860 RVA: 0x0005761F File Offset: 0x0005581F
		[NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static void CopyBlockUnaligned(void* destination, void* source, uint byteCount)
		{
			UnsafeRaw.CopyBlockUnaligned(destination, source, byteCount);
		}

		// Token: 0x06001ACD RID: 6861 RVA: 0x00057629 File Offset: 0x00055829
		[NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void CopyBlockUnaligned(ref byte destination, ref byte source, uint byteCount)
		{
			UnsafeRaw.CopyBlockUnaligned(ref destination, ref source, byteCount);
		}

		// Token: 0x06001ACE RID: 6862 RVA: 0x00057633 File Offset: 0x00055833
		[NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static void InitBlock(void* startAddress, byte value, uint byteCount)
		{
			UnsafeRaw.InitBlock(startAddress, value, byteCount);
		}

		// Token: 0x06001ACF RID: 6863 RVA: 0x0005763D File Offset: 0x0005583D
		[NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void InitBlock(ref byte startAddress, byte value, uint byteCount)
		{
			UnsafeRaw.InitBlock(ref startAddress, value, byteCount);
		}

		// Token: 0x06001AD0 RID: 6864 RVA: 0x00057647 File Offset: 0x00055847
		[NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static void InitBlockUnaligned(void* startAddress, byte value, uint byteCount)
		{
			UnsafeRaw.InitBlockUnaligned(startAddress, value, byteCount);
		}

		// Token: 0x06001AD1 RID: 6865 RVA: 0x00057651 File Offset: 0x00055851
		[NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void InitBlockUnaligned(ref byte startAddress, byte value, uint byteCount)
		{
			UnsafeRaw.InitBlockUnaligned(ref startAddress, value, byteCount);
		}

		// Token: 0x06001AD2 RID: 6866 RVA: 0x0005765B File Offset: 0x0005585B
		[NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static T As<T>(object o) where T : class
		{
			return UnsafeRaw.As<T>(o);
		}

		// Token: 0x06001AD3 RID: 6867 RVA: 0x00057663 File Offset: 0x00055863
		[NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static ref T AsRef<T>(void* source)
		{
			return UnsafeRaw.AsRef<T>(source);
		}

		// Token: 0x06001AD4 RID: 6868 RVA: 0x0005766B File Offset: 0x0005586B
		[NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ref T AsRef<T>(in T source)
		{
			return UnsafeRaw.AsRef<T>(source);
		}

		// Token: 0x06001AD5 RID: 6869 RVA: 0x00057673 File Offset: 0x00055873
		[NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ref TTo As<TFrom, TTo>(ref TFrom source)
		{
			return UnsafeRaw.As<TFrom, TTo>(ref source);
		}

		// Token: 0x06001AD6 RID: 6870 RVA: 0x0005767B File Offset: 0x0005587B
		[NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ref T Unbox<T>(object box) where T : struct
		{
			return UnsafeRaw.Unbox<T>(box);
		}

		// Token: 0x06001AD7 RID: 6871 RVA: 0x00057683 File Offset: 0x00055883
		[NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ref T AddByteOffset<T>(ref T source, [NativeInteger] IntPtr byteOffset)
		{
			return UnsafeRaw.AddByteOffset<T>(ref source, byteOffset);
		}

		// Token: 0x06001AD8 RID: 6872 RVA: 0x0005768C File Offset: 0x0005588C
		[NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ref T AddByteOffset<T>(ref T source, [NativeInteger] UIntPtr byteOffset)
		{
			return UnsafeRaw.AddByteOffset<T>(ref source, byteOffset);
		}

		// Token: 0x06001AD9 RID: 6873 RVA: 0x00057695 File Offset: 0x00055895
		[NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ref T SubtractByteOffset<T>(ref T source, [NativeInteger] IntPtr byteOffset)
		{
			return UnsafeRaw.SubtractByteOffset<T>(ref source, byteOffset);
		}

		// Token: 0x06001ADA RID: 6874 RVA: 0x0005769E File Offset: 0x0005589E
		[NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ref T SubtractByteOffset<T>(ref T source, [NativeInteger] UIntPtr byteOffset)
		{
			return UnsafeRaw.SubtractByteOffset<T>(ref source, byteOffset);
		}

		// Token: 0x06001ADB RID: 6875 RVA: 0x000576A7 File Offset: 0x000558A7
		[NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[return: NativeInteger]
		public static IntPtr ByteOffset<T>(ref T origin, ref T target)
		{
			return UnsafeRaw.ByteOffset<T>(ref origin, ref target);
		}

		// Token: 0x06001ADC RID: 6876 RVA: 0x000576B0 File Offset: 0x000558B0
		[NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool AreSame<T>(ref T left, ref T right)
		{
			return UnsafeRaw.AreSame<T>(ref left, ref right);
		}

		// Token: 0x06001ADD RID: 6877 RVA: 0x000576B9 File Offset: 0x000558B9
		[NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsAddressGreaterThan<T>(ref T left, ref T right)
		{
			return UnsafeRaw.IsAddressGreaterThan<T>(ref left, ref right);
		}

		// Token: 0x06001ADE RID: 6878 RVA: 0x000576C2 File Offset: 0x000558C2
		[NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsAddressLessThan<T>(ref T left, ref T right)
		{
			return UnsafeRaw.IsAddressLessThan<T>(ref left, ref right);
		}

		// Token: 0x06001ADF RID: 6879 RVA: 0x000576CB File Offset: 0x000558CB
		[NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsNullRef<T>(ref T source)
		{
			return UnsafeRaw.IsNullRef<T>(ref source);
		}

		// Token: 0x06001AE0 RID: 6880 RVA: 0x000576D3 File Offset: 0x000558D3
		[NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ref T NullRef<T>()
		{
			return UnsafeRaw.NullRef<T>();
		}

		// Token: 0x06001AE1 RID: 6881 RVA: 0x000576DA File Offset: 0x000558DA
		[NullableContext(2)]
		[NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int SizeOf<T>()
		{
			return (int)Unsafe.PerTypeValues<T>.TypeSize;
		}

		// Token: 0x06001AE2 RID: 6882 RVA: 0x000576E2 File Offset: 0x000558E2
		[NullableContext(1)]
		[NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ref T Add<[Nullable(2)] T>(ref T source, int elementOffset)
		{
			return UnsafeRaw.AddByteOffset<T>(ref source, (IntPtr)elementOffset * Unsafe.PerTypeValues<T>.TypeSize);
		}

		// Token: 0x06001AE3 RID: 6883 RVA: 0x000576F2 File Offset: 0x000558F2
		[NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static void* Add<[Nullable(2)] T>(void* source, int elementOffset)
		{
			return (void*)((byte*)source + (long)((IntPtr)elementOffset * Unsafe.PerTypeValues<T>.TypeSize));
		}

		// Token: 0x06001AE4 RID: 6884 RVA: 0x00057700 File Offset: 0x00055900
		[NullableContext(1)]
		[NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ref T Add<[Nullable(2)] T>(ref T source, [NativeInteger] IntPtr elementOffset)
		{
			return UnsafeRaw.AddByteOffset<T>(ref source, elementOffset * Unsafe.PerTypeValues<T>.TypeSize);
		}

		// Token: 0x06001AE5 RID: 6885 RVA: 0x0005770F File Offset: 0x0005590F
		[NullableContext(1)]
		[NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ref T Add<[Nullable(2)] T>(ref T source, [NativeInteger] UIntPtr elementOffset)
		{
			return UnsafeRaw.AddByteOffset<T>(ref source, elementOffset * (UIntPtr)Unsafe.PerTypeValues<T>.TypeSize);
		}

		// Token: 0x06001AE6 RID: 6886 RVA: 0x0005771E File Offset: 0x0005591E
		[NullableContext(1)]
		[NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ref T Subtract<[Nullable(2)] T>(ref T source, int elementOffset)
		{
			return UnsafeRaw.SubtractByteOffset<T>(ref source, (IntPtr)elementOffset * Unsafe.PerTypeValues<T>.TypeSize);
		}

		// Token: 0x06001AE7 RID: 6887 RVA: 0x0005772E File Offset: 0x0005592E
		[NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static void* Subtract<[Nullable(2)] T>(void* source, int elementOffset)
		{
			return (void*)((byte*)source - (long)((IntPtr)elementOffset * Unsafe.PerTypeValues<T>.TypeSize));
		}

		// Token: 0x06001AE8 RID: 6888 RVA: 0x0005773C File Offset: 0x0005593C
		[NullableContext(1)]
		[NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ref T Subtract<[Nullable(2)] T>(ref T source, [NativeInteger] IntPtr elementOffset)
		{
			return UnsafeRaw.SubtractByteOffset<T>(ref source, elementOffset * Unsafe.PerTypeValues<T>.TypeSize);
		}

		// Token: 0x06001AE9 RID: 6889 RVA: 0x0005774B File Offset: 0x0005594B
		[NullableContext(1)]
		[NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ref T Subtract<[Nullable(2)] T>(ref T source, [NativeInteger] UIntPtr elementOffset)
		{
			return UnsafeRaw.SubtractByteOffset<T>(ref source, elementOffset * (UIntPtr)Unsafe.PerTypeValues<T>.TypeSize);
		}

		// Token: 0x020004AD RID: 1197
		private static class PerTypeValues<[Nullable(2)] T>
		{
			// Token: 0x06001AEA RID: 6890 RVA: 0x0005775C File Offset: 0x0005595C
			[return: NativeInteger]
			private static IntPtr ComputeTypeSize()
			{
				T[] array = new T[2];
				return UnsafeRaw.ByteOffset<T>(ref array[0], ref array[1]);
			}

			// Token: 0x0400111D RID: 4381
			[NativeInteger]
			public static readonly IntPtr TypeSize = Unsafe.PerTypeValues<T>.ComputeTypeSize();
		}
	}
}
