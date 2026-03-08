using System;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;

namespace MonoMod.Backports.ILHelpers
{
	// Token: 0x02000806 RID: 2054
	internal static class UnsafeRaw
	{
		// Token: 0x06002706 RID: 9990 RVA: 0x0008747B File Offset: 0x0008567B
		[System.Runtime.Versioning.NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static T Read<T>(void* source)
		{
			return *(T*)source;
		}

		// Token: 0x06002707 RID: 9991 RVA: 0x00087483 File Offset: 0x00085683
		[System.Runtime.Versioning.NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static T ReadUnaligned<T>(void* source)
		{
			return *(T*)source;
		}

		// Token: 0x06002708 RID: 9992 RVA: 0x00087483 File Offset: 0x00085683
		[System.Runtime.Versioning.NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static T ReadUnaligned<T>(ref byte source)
		{
			return source;
		}

		// Token: 0x06002709 RID: 9993 RVA: 0x0008748E File Offset: 0x0008568E
		[System.Runtime.Versioning.NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static void Write<T>(void* destination, T value)
		{
			*(T*)destination = value;
		}

		// Token: 0x0600270A RID: 9994 RVA: 0x00087497 File Offset: 0x00085697
		[System.Runtime.Versioning.NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static void WriteUnaligned<T>(void* destination, T value)
		{
			*(T*)destination = value;
		}

		// Token: 0x0600270B RID: 9995 RVA: 0x00087497 File Offset: 0x00085697
		[System.Runtime.Versioning.NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void WriteUnaligned<T>(ref byte destination, T value)
		{
			destination = value;
		}

		// Token: 0x0600270C RID: 9996 RVA: 0x000874A3 File Offset: 0x000856A3
		[System.Runtime.Versioning.NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static void Copy<T>(void* destination, ref T source)
		{
			*(T*)destination = source;
		}

		// Token: 0x0600270D RID: 9997 RVA: 0x000874A3 File Offset: 0x000856A3
		[System.Runtime.Versioning.NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static void Copy<T>(ref T destination, void* source)
		{
			destination = *(T*)source;
		}

		// Token: 0x0600270E RID: 9998 RVA: 0x000874B1 File Offset: 0x000856B1
		[System.Runtime.Versioning.NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static void* AsPointer<T>(ref T value)
		{
			return (void*)(&value);
		}

		// Token: 0x0600270F RID: 9999 RVA: 0x0001B842 File Offset: 0x00019A42
		[System.Runtime.Versioning.NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void SkipInit<T>(out T value)
		{
		}

		// Token: 0x06002710 RID: 10000 RVA: 0x000874B5 File Offset: 0x000856B5
		[System.Runtime.Versioning.NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int SizeOf<T>()
		{
			return sizeof(T);
		}

		// Token: 0x06002711 RID: 10001 RVA: 0x000874BD File Offset: 0x000856BD
		[System.Runtime.Versioning.NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static void CopyBlock(void* destination, void* source, uint byteCount)
		{
			cpblk(destination, source, byteCount);
		}

		// Token: 0x06002712 RID: 10002 RVA: 0x000874BD File Offset: 0x000856BD
		[System.Runtime.Versioning.NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void CopyBlock(ref byte destination, ref byte source, uint byteCount)
		{
			cpblk(ref destination, ref source, byteCount);
		}

		// Token: 0x06002713 RID: 10003 RVA: 0x000874C4 File Offset: 0x000856C4
		[System.Runtime.Versioning.NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static void CopyBlockUnaligned(void* destination, void* source, uint byteCount)
		{
			cpblk(destination, source, byteCount);
		}

		// Token: 0x06002714 RID: 10004 RVA: 0x000874C4 File Offset: 0x000856C4
		[System.Runtime.Versioning.NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void CopyBlockUnaligned(ref byte destination, ref byte source, uint byteCount)
		{
			cpblk(ref destination, ref source, byteCount);
		}

		// Token: 0x06002715 RID: 10005 RVA: 0x000874CE File Offset: 0x000856CE
		[System.Runtime.Versioning.NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static void InitBlock(void* startAddress, byte value, uint byteCount)
		{
			initblk(startAddress, value, byteCount);
		}

		// Token: 0x06002716 RID: 10006 RVA: 0x000874CE File Offset: 0x000856CE
		[System.Runtime.Versioning.NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void InitBlock(ref byte startAddress, byte value, uint byteCount)
		{
			initblk(ref startAddress, value, byteCount);
		}

		// Token: 0x06002717 RID: 10007 RVA: 0x000874D5 File Offset: 0x000856D5
		[System.Runtime.Versioning.NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static void InitBlockUnaligned(void* startAddress, byte value, uint byteCount)
		{
			initblk(startAddress, value, byteCount);
		}

		// Token: 0x06002718 RID: 10008 RVA: 0x000874D5 File Offset: 0x000856D5
		[System.Runtime.Versioning.NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void InitBlockUnaligned(ref byte startAddress, byte value, uint byteCount)
		{
			initblk(ref startAddress, value, byteCount);
		}

		// Token: 0x06002719 RID: 10009 RVA: 0x0001B6A2 File Offset: 0x000198A2
		[System.Runtime.Versioning.NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static T As<T>(object o) where T : class
		{
			return o;
		}

		// Token: 0x0600271A RID: 10010 RVA: 0x000874E0 File Offset: 0x000856E0
		[System.Runtime.Versioning.NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static ref T AsRef<T>(void* source)
		{
			return ref *(T*)source;
		}

		// Token: 0x0600271B RID: 10011 RVA: 0x0001B6A2 File Offset: 0x000198A2
		[System.Runtime.Versioning.NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ref T AsRef<T>(in T source)
		{
			return ref source;
		}

		// Token: 0x0600271C RID: 10012 RVA: 0x0001B6A2 File Offset: 0x000198A2
		[System.Runtime.Versioning.NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ref TTo As<TFrom, TTo>(ref TFrom source)
		{
			return ref source;
		}

		// Token: 0x0600271D RID: 10013 RVA: 0x000874F0 File Offset: 0x000856F0
		[System.Runtime.Versioning.NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ref T Unbox<T>(object box) where T : struct
		{
			return ref (T)box;
		}

		// Token: 0x0600271E RID: 10014 RVA: 0x000874F8 File Offset: 0x000856F8
		[System.Runtime.Versioning.NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ref T Add<T>(ref T source, int elementOffset)
		{
			return (ref source) + (IntPtr)elementOffset * (IntPtr)sizeof(T);
		}

		// Token: 0x0600271F RID: 10015 RVA: 0x000874F8 File Offset: 0x000856F8
		[System.Runtime.Versioning.NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static void* Add<T>(void* source, int elementOffset)
		{
			return (void*)((byte*)source + (IntPtr)elementOffset * (IntPtr)sizeof(T));
		}

		// Token: 0x06002720 RID: 10016 RVA: 0x00087505 File Offset: 0x00085705
		[System.Runtime.Versioning.NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ref T Add<T>(ref T source, [NativeInteger] IntPtr elementOffset)
		{
			return (ref source) + elementOffset * (IntPtr)sizeof(T);
		}

		// Token: 0x06002721 RID: 10017 RVA: 0x00087505 File Offset: 0x00085705
		[System.Runtime.Versioning.NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ref T Add<T>(ref T source, [NativeInteger] UIntPtr elementOffset)
		{
			return (ref source) + elementOffset * (UIntPtr)sizeof(T);
		}

		// Token: 0x06002722 RID: 10018 RVA: 0x00087511 File Offset: 0x00085711
		[System.Runtime.Versioning.NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ref T AddByteOffset<T>(ref T source, [NativeInteger] IntPtr byteOffset)
		{
			return (ref source) + byteOffset;
		}

		// Token: 0x06002723 RID: 10019 RVA: 0x00087511 File Offset: 0x00085711
		[System.Runtime.Versioning.NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ref T AddByteOffset<T>(ref T source, [NativeInteger] UIntPtr byteOffset)
		{
			return (ref source) + byteOffset;
		}

		// Token: 0x06002724 RID: 10020 RVA: 0x00087516 File Offset: 0x00085716
		[System.Runtime.Versioning.NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ref T Subtract<T>(ref T source, int elementOffset)
		{
			return (ref source) - (IntPtr)elementOffset * (IntPtr)sizeof(T);
		}

		// Token: 0x06002725 RID: 10021 RVA: 0x00087516 File Offset: 0x00085716
		[System.Runtime.Versioning.NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static void* Subtract<T>(void* source, int elementOffset)
		{
			return (void*)((byte*)source - (IntPtr)elementOffset * (IntPtr)sizeof(T));
		}

		// Token: 0x06002726 RID: 10022 RVA: 0x00087523 File Offset: 0x00085723
		[System.Runtime.Versioning.NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ref T Subtract<T>(ref T source, [NativeInteger] IntPtr elementOffset)
		{
			return (ref source) - elementOffset * (IntPtr)sizeof(T);
		}

		// Token: 0x06002727 RID: 10023 RVA: 0x00087523 File Offset: 0x00085723
		[System.Runtime.Versioning.NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ref T Subtract<T>(ref T source, [NativeInteger] UIntPtr elementOffset)
		{
			return (ref source) - elementOffset * (UIntPtr)sizeof(T);
		}

		// Token: 0x06002728 RID: 10024 RVA: 0x0008752F File Offset: 0x0008572F
		[System.Runtime.Versioning.NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ref T SubtractByteOffset<T>(ref T source, [NativeInteger] IntPtr byteOffset)
		{
			return (ref source) - byteOffset;
		}

		// Token: 0x06002729 RID: 10025 RVA: 0x0008752F File Offset: 0x0008572F
		[System.Runtime.Versioning.NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ref T SubtractByteOffset<T>(ref T source, [NativeInteger] UIntPtr byteOffset)
		{
			return (ref source) - byteOffset;
		}

		// Token: 0x0600272A RID: 10026 RVA: 0x00087534 File Offset: 0x00085734
		[System.Runtime.Versioning.NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[return: NativeInteger]
		public static IntPtr ByteOffset<T>(ref T origin, ref T target)
		{
			return (ref target) - (ref origin);
		}

		// Token: 0x0600272B RID: 10027 RVA: 0x00087539 File Offset: 0x00085739
		[System.Runtime.Versioning.NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool AreSame<T>(ref T left, ref T right)
		{
			return (ref left) == (ref right);
		}

		// Token: 0x0600272C RID: 10028 RVA: 0x0008753F File Offset: 0x0008573F
		[System.Runtime.Versioning.NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsAddressGreaterThan<T>(ref T left, ref T right)
		{
			return (ref left) != (ref right);
		}

		// Token: 0x0600272D RID: 10029 RVA: 0x00087545 File Offset: 0x00085745
		[System.Runtime.Versioning.NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsAddressLessThan<T>(ref T left, ref T right)
		{
			return (ref left) < (ref right);
		}

		// Token: 0x0600272E RID: 10030 RVA: 0x0008754B File Offset: 0x0008574B
		[System.Runtime.Versioning.NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsNullRef<T>(ref T source)
		{
			return (ref source) == (UIntPtr)0;
		}

		// Token: 0x0600272F RID: 10031 RVA: 0x00087552 File Offset: 0x00085752
		[System.Runtime.Versioning.NonVersionable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ref T NullRef<T>()
		{
			return (UIntPtr)0;
		}
	}
}
