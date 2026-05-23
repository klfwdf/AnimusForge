using System;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;

namespace MonoMod.Backports.ILHelpers
{
	// Token: 0x02000009 RID: 9
	public static class UnsafeRaw
	{
		// Token: 0x0600000C RID: 12 RVA: 0x000020E3 File Offset: 0x000002E3
		[NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static T Read<T>(void* source)
		{
			return *(T*)source;
		}

		// Token: 0x0600000D RID: 13 RVA: 0x000020EB File Offset: 0x000002EB
		[NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static T ReadUnaligned<T>(void* source)
		{
			return *(T*)source;
		}

		// Token: 0x0600000E RID: 14 RVA: 0x000020EB File Offset: 0x000002EB
		[NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static T ReadUnaligned<T>(ref byte source)
		{
			return source;
		}

		// Token: 0x0600000F RID: 15 RVA: 0x000020F6 File Offset: 0x000002F6
		[NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static void Write<T>(void* destination, T value)
		{
			*(T*)destination = value;
		}

		// Token: 0x06000010 RID: 16 RVA: 0x000020FF File Offset: 0x000002FF
		[NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static void WriteUnaligned<T>(void* destination, T value)
		{
			*(T*)destination = value;
		}

		// Token: 0x06000011 RID: 17 RVA: 0x000020FF File Offset: 0x000002FF
		[NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void WriteUnaligned<T>(ref byte destination, T value)
		{
			destination = value;
		}

		// Token: 0x06000012 RID: 18 RVA: 0x0000210B File Offset: 0x0000030B
		[NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static void Copy<T>(void* destination, ref T source)
		{
			*(T*)destination = source;
		}

		// Token: 0x06000013 RID: 19 RVA: 0x0000210B File Offset: 0x0000030B
		[NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static void Copy<T>(ref T destination, void* source)
		{
			destination = *(T*)source;
		}

		// Token: 0x06000014 RID: 20 RVA: 0x00002119 File Offset: 0x00000319
		[NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static void* AsPointer<T>(ref T value)
		{
			return (void*)(&value);
		}

		// Token: 0x06000015 RID: 21 RVA: 0x0000211D File Offset: 0x0000031D
		[NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void SkipInit<T>(out T value)
		{
		}

		// Token: 0x06000016 RID: 22 RVA: 0x0000211F File Offset: 0x0000031F
		[NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int SizeOf<T>()
		{
			return sizeof(T);
		}

		// Token: 0x06000017 RID: 23 RVA: 0x00002127 File Offset: 0x00000327
		[NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static void CopyBlock(void* destination, void* source, uint byteCount)
		{
			cpblk(destination, source, byteCount);
		}

		// Token: 0x06000018 RID: 24 RVA: 0x00002127 File Offset: 0x00000327
		[NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void CopyBlock(ref byte destination, ref byte source, uint byteCount)
		{
			cpblk(ref destination, ref source, byteCount);
		}

		// Token: 0x06000019 RID: 25 RVA: 0x0000212E File Offset: 0x0000032E
		[NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static void CopyBlockUnaligned(void* destination, void* source, uint byteCount)
		{
			cpblk(destination, source, byteCount);
		}

		// Token: 0x0600001A RID: 26 RVA: 0x0000212E File Offset: 0x0000032E
		[NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void CopyBlockUnaligned(ref byte destination, ref byte source, uint byteCount)
		{
			cpblk(ref destination, ref source, byteCount);
		}

		// Token: 0x0600001B RID: 27 RVA: 0x00002138 File Offset: 0x00000338
		[NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static void InitBlock(void* startAddress, byte value, uint byteCount)
		{
			initblk(startAddress, value, byteCount);
		}

		// Token: 0x0600001C RID: 28 RVA: 0x00002138 File Offset: 0x00000338
		[NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void InitBlock(ref byte startAddress, byte value, uint byteCount)
		{
			initblk(ref startAddress, value, byteCount);
		}

		// Token: 0x0600001D RID: 29 RVA: 0x0000213F File Offset: 0x0000033F
		[NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static void InitBlockUnaligned(void* startAddress, byte value, uint byteCount)
		{
			initblk(startAddress, value, byteCount);
		}

		// Token: 0x0600001E RID: 30 RVA: 0x0000213F File Offset: 0x0000033F
		[NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void InitBlockUnaligned(ref byte startAddress, byte value, uint byteCount)
		{
			initblk(ref startAddress, value, byteCount);
		}

		// Token: 0x0600001F RID: 31 RVA: 0x00002149 File Offset: 0x00000349
		[NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static T As<T>(object o) where T : class
		{
			return o;
		}

		// Token: 0x06000020 RID: 32 RVA: 0x0000214C File Offset: 0x0000034C
		[NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static ref T AsRef<T>(void* source)
		{
			return ref *(T*)source;
		}

		// Token: 0x06000021 RID: 33 RVA: 0x00002149 File Offset: 0x00000349
		[NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ref T AsRef<T>(in T source)
		{
			return ref source;
		}

		// Token: 0x06000022 RID: 34 RVA: 0x00002149 File Offset: 0x00000349
		[NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ref TTo As<TFrom, TTo>(ref TFrom source)
		{
			return ref source;
		}

		// Token: 0x06000023 RID: 35 RVA: 0x0000215C File Offset: 0x0000035C
		[NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ref T Unbox<T>(object box) where T : struct
		{
			return ref (T)box;
		}

		// Token: 0x06000024 RID: 36 RVA: 0x00002164 File Offset: 0x00000364
		[NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ref T Add<T>(ref T source, int elementOffset)
		{
			return (ref source) + (IntPtr)elementOffset * (IntPtr)sizeof(T);
		}

		// Token: 0x06000025 RID: 37 RVA: 0x00002164 File Offset: 0x00000364
		[NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static void* Add<T>(void* source, int elementOffset)
		{
			return (void*)((byte*)source + (IntPtr)elementOffset * (IntPtr)sizeof(T));
		}

		// Token: 0x06000026 RID: 38 RVA: 0x00002171 File Offset: 0x00000371
		[NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ref T Add<T>(ref T source, [NativeInteger] IntPtr elementOffset)
		{
			return (ref source) + elementOffset * (IntPtr)sizeof(T);
		}

		// Token: 0x06000027 RID: 39 RVA: 0x00002171 File Offset: 0x00000371
		[NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ref T Add<T>(ref T source, [NativeInteger] UIntPtr elementOffset)
		{
			return (ref source) + elementOffset * (UIntPtr)sizeof(T);
		}

		// Token: 0x06000028 RID: 40 RVA: 0x0000217D File Offset: 0x0000037D
		[NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ref T AddByteOffset<T>(ref T source, [NativeInteger] IntPtr byteOffset)
		{
			return (ref source) + byteOffset;
		}

		// Token: 0x06000029 RID: 41 RVA: 0x0000217D File Offset: 0x0000037D
		[NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ref T AddByteOffset<T>(ref T source, [NativeInteger] UIntPtr byteOffset)
		{
			return (ref source) + byteOffset;
		}

		// Token: 0x0600002A RID: 42 RVA: 0x00002182 File Offset: 0x00000382
		[NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ref T Subtract<T>(ref T source, int elementOffset)
		{
			return (ref source) - (IntPtr)elementOffset * (IntPtr)sizeof(T);
		}

		// Token: 0x0600002B RID: 43 RVA: 0x00002182 File Offset: 0x00000382
		[NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static void* Subtract<T>(void* source, int elementOffset)
		{
			return (void*)((byte*)source - (IntPtr)elementOffset * (IntPtr)sizeof(T));
		}

		// Token: 0x0600002C RID: 44 RVA: 0x0000218F File Offset: 0x0000038F
		[NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ref T Subtract<T>(ref T source, [NativeInteger] IntPtr elementOffset)
		{
			return (ref source) - elementOffset * (IntPtr)sizeof(T);
		}

		// Token: 0x0600002D RID: 45 RVA: 0x0000218F File Offset: 0x0000038F
		[NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ref T Subtract<T>(ref T source, [NativeInteger] UIntPtr elementOffset)
		{
			return (ref source) - elementOffset * (UIntPtr)sizeof(T);
		}

		// Token: 0x0600002E RID: 46 RVA: 0x0000219B File Offset: 0x0000039B
		[NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ref T SubtractByteOffset<T>(ref T source, [NativeInteger] IntPtr byteOffset)
		{
			return (ref source) - byteOffset;
		}

		// Token: 0x0600002F RID: 47 RVA: 0x0000219B File Offset: 0x0000039B
		[NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ref T SubtractByteOffset<T>(ref T source, [NativeInteger] UIntPtr byteOffset)
		{
			return (ref source) - byteOffset;
		}

		// Token: 0x06000030 RID: 48 RVA: 0x000021A0 File Offset: 0x000003A0
		[NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[return: NativeInteger]
		public static IntPtr ByteOffset<T>(ref T origin, ref T target)
		{
			return (ref target) - (ref origin);
		}

		// Token: 0x06000031 RID: 49 RVA: 0x000021A5 File Offset: 0x000003A5
		[NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool AreSame<T>(ref T left, ref T right)
		{
			return (ref left) == (ref right);
		}

		// Token: 0x06000032 RID: 50 RVA: 0x000021AB File Offset: 0x000003AB
		[NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsAddressGreaterThan<T>(ref T left, ref T right)
		{
			return (ref left) != (ref right);
		}

		// Token: 0x06000033 RID: 51 RVA: 0x000021B1 File Offset: 0x000003B1
		[NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsAddressLessThan<T>(ref T left, ref T right)
		{
			return (ref left) < (ref right);
		}

		// Token: 0x06000034 RID: 52 RVA: 0x000021B7 File Offset: 0x000003B7
		[NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsNullRef<T>(ref T source)
		{
			return (ref source) == (UIntPtr)0;
		}

		// Token: 0x06000035 RID: 53 RVA: 0x000021BE File Offset: 0x000003BE
		[NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ref T NullRef<T>()
		{
			return (UIntPtr)0;
		}
	}
}
