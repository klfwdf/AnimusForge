using System;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security;

namespace System
{
	// Token: 0x020000B3 RID: 179
	[ComVisible(true)]
	[__DynamicallyInvokable]
	public static class Buffer
	{
		// Token: 0x06000A50 RID: 2640
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		[MethodImpl(MethodImplOptions.InternalCall)]
		public static extern void BlockCopy(Array src, int srcOffset, Array dst, int dstOffset, int count);

		// Token: 0x06000A51 RID: 2641
		[SecuritySafeCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		internal static extern void InternalBlockCopy(Array src, int srcOffsetBytes, Array dst, int dstOffsetBytes, int byteCount);

		// Token: 0x06000A52 RID: 2642 RVA: 0x00021250 File Offset: 0x0001F450
		[SecurityCritical]
		internal unsafe static int IndexOfByte(byte* src, byte value, int index, int count)
		{
			byte* ptr = src + index;
			while ((ptr & 3) != 0)
			{
				if (count == 0)
				{
					return -1;
				}
				if (*ptr == value)
				{
					return (int)((long)(ptr - src));
				}
				count--;
				ptr++;
			}
			uint num = (uint)(((int)value << 8) + (int)value);
			num = (num << 16) + num;
			while (count > 3)
			{
				uint num2 = *(uint*)ptr;
				num2 ^= num;
				uint num3 = 2130640639U + num2;
				num2 ^= uint.MaxValue;
				num2 ^= num3;
				num2 &= 2164326656U;
				if (num2 != 0U)
				{
					int num4 = (int)((long)(ptr - src));
					if (*ptr == value)
					{
						return num4;
					}
					if (ptr[1] == value)
					{
						return num4 + 1;
					}
					if (ptr[2] == value)
					{
						return num4 + 2;
					}
					if (ptr[3] == value)
					{
						return num4 + 3;
					}
				}
				count -= 4;
				ptr += 4;
			}
			while (count > 0)
			{
				if (*ptr == value)
				{
					return (int)((long)(ptr - src));
				}
				count--;
				ptr++;
			}
			return -1;
		}

		// Token: 0x06000A53 RID: 2643
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private static extern bool IsPrimitiveTypeArray(Array array);

		// Token: 0x06000A54 RID: 2644
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private static extern byte _GetByte(Array array, int index);

		// Token: 0x06000A55 RID: 2645 RVA: 0x00021314 File Offset: 0x0001F514
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		public static byte GetByte(Array array, int index)
		{
			if (array == null)
			{
				throw new ArgumentNullException("array");
			}
			if (!Buffer.IsPrimitiveTypeArray(array))
			{
				throw new ArgumentException(Environment.GetResourceString("Arg_MustBePrimArray"), "array");
			}
			if (index < 0 || index >= Buffer._ByteLength(array))
			{
				throw new ArgumentOutOfRangeException("index");
			}
			return Buffer._GetByte(array, index);
		}

		// Token: 0x06000A56 RID: 2646
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private static extern void _SetByte(Array array, int index, byte value);

		// Token: 0x06000A57 RID: 2647 RVA: 0x0002136C File Offset: 0x0001F56C
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		public static void SetByte(Array array, int index, byte value)
		{
			if (array == null)
			{
				throw new ArgumentNullException("array");
			}
			if (!Buffer.IsPrimitiveTypeArray(array))
			{
				throw new ArgumentException(Environment.GetResourceString("Arg_MustBePrimArray"), "array");
			}
			if (index < 0 || index >= Buffer._ByteLength(array))
			{
				throw new ArgumentOutOfRangeException("index");
			}
			Buffer._SetByte(array, index, value);
		}

		// Token: 0x06000A58 RID: 2648
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private static extern int _ByteLength(Array array);

		// Token: 0x06000A59 RID: 2649 RVA: 0x000213C4 File Offset: 0x0001F5C4
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		public static int ByteLength(Array array)
		{
			if (array == null)
			{
				throw new ArgumentNullException("array");
			}
			if (!Buffer.IsPrimitiveTypeArray(array))
			{
				throw new ArgumentException(Environment.GetResourceString("Arg_MustBePrimArray"), "array");
			}
			return Buffer._ByteLength(array);
		}

		// Token: 0x06000A5A RID: 2650 RVA: 0x000213F7 File Offset: 0x0001F5F7
		[SecurityCritical]
		internal unsafe static void ZeroMemory(byte* src, long len)
		{
			for (;;)
			{
				long num = len;
				len = num - 1L;
				if (num <= 0L)
				{
					break;
				}
				src[len] = 0;
			}
		}

		// Token: 0x06000A5B RID: 2651 RVA: 0x0002140C File Offset: 0x0001F60C
		[SecurityCritical]
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		internal unsafe static void Memcpy(byte[] dest, int destIndex, byte* src, int srcIndex, int len)
		{
			if (len == 0)
			{
				return;
			}
			fixed (byte[] array = dest)
			{
				byte* ptr;
				if (dest == null || array.Length == 0)
				{
					ptr = null;
				}
				else
				{
					ptr = &array[0];
				}
				Buffer.Memcpy(ptr + destIndex, src + srcIndex, len);
			}
		}

		// Token: 0x06000A5C RID: 2652 RVA: 0x00021448 File Offset: 0x0001F648
		[SecurityCritical]
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		internal unsafe static void Memcpy(byte* pDest, int destIndex, byte[] src, int srcIndex, int len)
		{
			if (len == 0)
			{
				return;
			}
			fixed (byte[] array = src)
			{
				byte* ptr;
				if (src == null || array.Length == 0)
				{
					ptr = null;
				}
				else
				{
					ptr = &array[0];
				}
				Buffer.Memcpy(pDest + destIndex, ptr + srcIndex, len);
			}
		}

		// Token: 0x06000A5D RID: 2653 RVA: 0x00021481 File Offset: 0x0001F681
		[FriendAccessAllowed]
		[SecurityCritical]
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal unsafe static void Memcpy(byte* dest, byte* src, int len)
		{
			Buffer.Memmove(dest, src, (ulong)len);
		}

		// Token: 0x06000A5E RID: 2654 RVA: 0x0002148C File Offset: 0x0001F68C
		[SecurityCritical]
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		internal unsafe static void Memmove(byte* dest, byte* src, ulong len)
		{
			if ((ulong)(dest - src) >= len && (ulong)(src - dest) >= len)
			{
				byte* ptr = src + len;
				byte* ptr2 = dest + len;
				if (len > 16UL)
				{
					if (len > 64UL)
					{
						if (len > 2048UL)
						{
							goto IL_10D;
						}
						ulong num = len >> 6;
						do
						{
							*(Buffer.Block64*)dest = *(Buffer.Block64*)src;
							dest += 64;
							src += 64;
							num -= 1UL;
						}
						while (num != 0UL);
						len %= 64UL;
						if (len <= 16UL)
						{
							*(Buffer.Block16*)(ptr2 - 16) = *(Buffer.Block16*)(ptr - 16);
							return;
						}
					}
					*(Buffer.Block16*)dest = *(Buffer.Block16*)src;
					if (len > 32UL)
					{
						*(Buffer.Block16*)(dest + 16) = *(Buffer.Block16*)(src + 16);
						if (len > 48UL)
						{
							*(Buffer.Block16*)(dest + 32) = *(Buffer.Block16*)(src + 32);
						}
					}
					*(Buffer.Block16*)(ptr2 - 16) = *(Buffer.Block16*)(ptr - 16);
					return;
				}
				if ((len & 24UL) != 0UL)
				{
					*(long*)dest = *(long*)src;
					*(long*)(ptr2 - 8) = *(long*)(ptr - 8);
					return;
				}
				if ((len & 4UL) != 0UL)
				{
					*(int*)dest = *(int*)src;
					*(int*)(ptr2 - 4) = *(int*)(ptr - 4);
					return;
				}
				if (len == 0UL)
				{
					return;
				}
				*dest = *src;
				if ((len & 2UL) == 0UL)
				{
					return;
				}
				*(short*)(ptr2 - 2) = *(short*)(ptr - 2);
				return;
			}
			IL_10D:
			Buffer._Memmove(dest, src, len);
		}

		// Token: 0x06000A5F RID: 2655 RVA: 0x000215AE File Offset: 0x0001F7AE
		[SecurityCritical]
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		[MethodImpl(MethodImplOptions.NoInlining)]
		private unsafe static void _Memmove(byte* dest, byte* src, ulong len)
		{
			Buffer.__Memmove(dest, src, len);
		}

		// Token: 0x06000A60 RID: 2656
		[SuppressUnmanagedCodeSecurity]
		[SecurityCritical]
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		[DllImport("QCall", CharSet = CharSet.Unicode)]
		private unsafe static extern void __Memmove(byte* dest, byte* src, ulong len);

		// Token: 0x06000A61 RID: 2657 RVA: 0x000215B8 File Offset: 0x0001F7B8
		[SecurityCritical]
		[CLSCompliant(false)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static void MemoryCopy(void* source, void* destination, long destinationSizeInBytes, long sourceBytesToCopy)
		{
			if (sourceBytesToCopy > destinationSizeInBytes)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.sourceBytesToCopy);
			}
			Buffer.Memmove((byte*)destination, (byte*)source, checked((ulong)sourceBytesToCopy));
		}

		// Token: 0x06000A62 RID: 2658 RVA: 0x000215CE File Offset: 0x0001F7CE
		[SecurityCritical]
		[CLSCompliant(false)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static void MemoryCopy(void* source, void* destination, ulong destinationSizeInBytes, ulong sourceBytesToCopy)
		{
			if (sourceBytesToCopy > destinationSizeInBytes)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.sourceBytesToCopy);
			}
			Buffer.Memmove((byte*)destination, (byte*)source, sourceBytesToCopy);
		}

		// Token: 0x02000AD4 RID: 2772
		private struct Block16
		{
		}

		// Token: 0x02000AD5 RID: 2773
		private struct Block64
		{
		}
	}
}
