using System;
using System.Security;

namespace System
{
	// Token: 0x020000B1 RID: 177
	[__DynamicallyInvokable]
	public static class BitConverter
	{
		// Token: 0x06000A1A RID: 2586 RVA: 0x00020A1C File Offset: 0x0001EC1C
		[__DynamicallyInvokable]
		public static byte[] GetBytes(bool value)
		{
			return new byte[] { value ? 1 : 0 };
		}

		// Token: 0x06000A1B RID: 2587 RVA: 0x00020A3B File Offset: 0x0001EC3B
		[__DynamicallyInvokable]
		public static byte[] GetBytes(char value)
		{
			return BitConverter.GetBytes((short)value);
		}

		// Token: 0x06000A1C RID: 2588 RVA: 0x00020A44 File Offset: 0x0001EC44
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		public unsafe static byte[] GetBytes(short value)
		{
			byte[] array = new byte[2];
			byte[] array2;
			byte* ptr;
			if ((array2 = array) == null || array2.Length == 0)
			{
				ptr = null;
			}
			else
			{
				ptr = &array2[0];
			}
			*(short*)ptr = value;
			array2 = null;
			return array;
		}

		// Token: 0x06000A1D RID: 2589 RVA: 0x00020A78 File Offset: 0x0001EC78
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		public unsafe static byte[] GetBytes(int value)
		{
			byte[] array = new byte[4];
			byte[] array2;
			byte* ptr;
			if ((array2 = array) == null || array2.Length == 0)
			{
				ptr = null;
			}
			else
			{
				ptr = &array2[0];
			}
			*(int*)ptr = value;
			array2 = null;
			return array;
		}

		// Token: 0x06000A1E RID: 2590 RVA: 0x00020AAC File Offset: 0x0001ECAC
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		public unsafe static byte[] GetBytes(long value)
		{
			byte[] array = new byte[8];
			byte[] array2;
			byte* ptr;
			if ((array2 = array) == null || array2.Length == 0)
			{
				ptr = null;
			}
			else
			{
				ptr = &array2[0];
			}
			*(long*)ptr = value;
			array2 = null;
			return array;
		}

		// Token: 0x06000A1F RID: 2591 RVA: 0x00020ADE File Offset: 0x0001ECDE
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public static byte[] GetBytes(ushort value)
		{
			return BitConverter.GetBytes((short)value);
		}

		// Token: 0x06000A20 RID: 2592 RVA: 0x00020AE7 File Offset: 0x0001ECE7
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public static byte[] GetBytes(uint value)
		{
			return BitConverter.GetBytes((int)value);
		}

		// Token: 0x06000A21 RID: 2593 RVA: 0x00020AEF File Offset: 0x0001ECEF
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public static byte[] GetBytes(ulong value)
		{
			return BitConverter.GetBytes((long)value);
		}

		// Token: 0x06000A22 RID: 2594 RVA: 0x00020AF7 File Offset: 0x0001ECF7
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		public unsafe static byte[] GetBytes(float value)
		{
			return BitConverter.GetBytes(*(int*)(&value));
		}

		// Token: 0x06000A23 RID: 2595 RVA: 0x00020B02 File Offset: 0x0001ED02
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		public unsafe static byte[] GetBytes(double value)
		{
			return BitConverter.GetBytes(*(long*)(&value));
		}

		// Token: 0x06000A24 RID: 2596 RVA: 0x00020B0D File Offset: 0x0001ED0D
		[__DynamicallyInvokable]
		public static char ToChar(byte[] value, int startIndex)
		{
			if (value == null)
			{
				ThrowHelper.ThrowArgumentNullException(ExceptionArgument.value);
			}
			if ((ulong)startIndex >= (ulong)((long)value.Length))
			{
				ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.startIndex, ExceptionResource.ArgumentOutOfRange_Index);
			}
			if (startIndex > value.Length - 2)
			{
				ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_ArrayPlusOffTooSmall);
			}
			return (char)BitConverter.ToInt16(value, startIndex);
		}

		// Token: 0x06000A25 RID: 2597 RVA: 0x00020B40 File Offset: 0x0001ED40
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		public unsafe static short ToInt16(byte[] value, int startIndex)
		{
			if (value == null)
			{
				ThrowHelper.ThrowArgumentNullException(ExceptionArgument.value);
			}
			if ((ulong)startIndex >= (ulong)((long)value.Length))
			{
				ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.startIndex, ExceptionResource.ArgumentOutOfRange_Index);
			}
			if (startIndex > value.Length - 2)
			{
				ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_ArrayPlusOffTooSmall);
			}
			fixed (byte* ptr = &value[startIndex])
			{
				byte* ptr2 = ptr;
				if (startIndex % 2 == 0)
				{
					return *(short*)ptr2;
				}
				if (BitConverter.IsLittleEndian)
				{
					return (short)((int)(*ptr2) | ((int)ptr2[1] << 8));
				}
				return (short)(((int)(*ptr2) << 8) | (int)ptr2[1]);
			}
		}

		// Token: 0x06000A26 RID: 2598 RVA: 0x00020BA8 File Offset: 0x0001EDA8
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		public unsafe static int ToInt32(byte[] value, int startIndex)
		{
			if (value == null)
			{
				ThrowHelper.ThrowArgumentNullException(ExceptionArgument.value);
			}
			if ((ulong)startIndex >= (ulong)((long)value.Length))
			{
				ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.startIndex, ExceptionResource.ArgumentOutOfRange_Index);
			}
			if (startIndex > value.Length - 4)
			{
				ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_ArrayPlusOffTooSmall);
			}
			fixed (byte* ptr = &value[startIndex])
			{
				byte* ptr2 = ptr;
				if (startIndex % 4 == 0)
				{
					return *(int*)ptr2;
				}
				if (BitConverter.IsLittleEndian)
				{
					return (int)(*ptr2) | ((int)ptr2[1] << 8) | ((int)ptr2[2] << 16) | ((int)ptr2[3] << 24);
				}
				return ((int)(*ptr2) << 24) | ((int)ptr2[1] << 16) | ((int)ptr2[2] << 8) | (int)ptr2[3];
			}
		}

		// Token: 0x06000A27 RID: 2599 RVA: 0x00020C2C File Offset: 0x0001EE2C
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		public unsafe static long ToInt64(byte[] value, int startIndex)
		{
			if (value == null)
			{
				ThrowHelper.ThrowArgumentNullException(ExceptionArgument.value);
			}
			if ((ulong)startIndex >= (ulong)((long)value.Length))
			{
				ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.startIndex, ExceptionResource.ArgumentOutOfRange_Index);
			}
			if (startIndex > value.Length - 8)
			{
				ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_ArrayPlusOffTooSmall);
			}
			fixed (byte* ptr = &value[startIndex])
			{
				byte* ptr2 = ptr;
				if (startIndex % 8 == 0)
				{
					return *(long*)ptr2;
				}
				if (BitConverter.IsLittleEndian)
				{
					int num = (int)(*ptr2) | ((int)ptr2[1] << 8) | ((int)ptr2[2] << 16) | ((int)ptr2[3] << 24);
					int num2 = (int)ptr2[4] | ((int)ptr2[5] << 8) | ((int)ptr2[6] << 16) | ((int)ptr2[7] << 24);
					return (long)((ulong)num | (ulong)((ulong)((long)num2) << 32));
				}
				int num3 = ((int)(*ptr2) << 24) | ((int)ptr2[1] << 16) | ((int)ptr2[2] << 8) | (int)ptr2[3];
				int num4 = ((int)ptr2[4] << 24) | ((int)ptr2[5] << 16) | ((int)ptr2[6] << 8) | (int)ptr2[7];
				return (long)((ulong)num4 | (ulong)((ulong)((long)num3) << 32));
			}
		}

		// Token: 0x06000A28 RID: 2600 RVA: 0x00020CFD File Offset: 0x0001EEFD
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public static ushort ToUInt16(byte[] value, int startIndex)
		{
			if (value == null)
			{
				ThrowHelper.ThrowArgumentNullException(ExceptionArgument.value);
			}
			if ((ulong)startIndex >= (ulong)((long)value.Length))
			{
				ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.startIndex, ExceptionResource.ArgumentOutOfRange_Index);
			}
			if (startIndex > value.Length - 2)
			{
				ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_ArrayPlusOffTooSmall);
			}
			return (ushort)BitConverter.ToInt16(value, startIndex);
		}

		// Token: 0x06000A29 RID: 2601 RVA: 0x00020D30 File Offset: 0x0001EF30
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public static uint ToUInt32(byte[] value, int startIndex)
		{
			if (value == null)
			{
				ThrowHelper.ThrowArgumentNullException(ExceptionArgument.value);
			}
			if ((ulong)startIndex >= (ulong)((long)value.Length))
			{
				ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.startIndex, ExceptionResource.ArgumentOutOfRange_Index);
			}
			if (startIndex > value.Length - 4)
			{
				ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_ArrayPlusOffTooSmall);
			}
			return (uint)BitConverter.ToInt32(value, startIndex);
		}

		// Token: 0x06000A2A RID: 2602 RVA: 0x00020D62 File Offset: 0x0001EF62
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public static ulong ToUInt64(byte[] value, int startIndex)
		{
			if (value == null)
			{
				ThrowHelper.ThrowArgumentNullException(ExceptionArgument.value);
			}
			if ((ulong)startIndex >= (ulong)((long)value.Length))
			{
				ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.startIndex, ExceptionResource.ArgumentOutOfRange_Index);
			}
			if (startIndex > value.Length - 8)
			{
				ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_ArrayPlusOffTooSmall);
			}
			return (ulong)BitConverter.ToInt64(value, startIndex);
		}

		// Token: 0x06000A2B RID: 2603 RVA: 0x00020D94 File Offset: 0x0001EF94
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		public unsafe static float ToSingle(byte[] value, int startIndex)
		{
			if (value == null)
			{
				ThrowHelper.ThrowArgumentNullException(ExceptionArgument.value);
			}
			if ((ulong)startIndex >= (ulong)((long)value.Length))
			{
				ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.startIndex, ExceptionResource.ArgumentOutOfRange_Index);
			}
			if (startIndex > value.Length - 4)
			{
				ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_ArrayPlusOffTooSmall);
			}
			int num = BitConverter.ToInt32(value, startIndex);
			return *(float*)(&num);
		}

		// Token: 0x06000A2C RID: 2604 RVA: 0x00020DD8 File Offset: 0x0001EFD8
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		public unsafe static double ToDouble(byte[] value, int startIndex)
		{
			if (value == null)
			{
				ThrowHelper.ThrowArgumentNullException(ExceptionArgument.value);
			}
			if ((ulong)startIndex >= (ulong)((long)value.Length))
			{
				ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.startIndex, ExceptionResource.ArgumentOutOfRange_Index);
			}
			if (startIndex > value.Length - 8)
			{
				ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_ArrayPlusOffTooSmall);
			}
			long num = BitConverter.ToInt64(value, startIndex);
			return *(double*)(&num);
		}

		// Token: 0x06000A2D RID: 2605 RVA: 0x00020E1A File Offset: 0x0001F01A
		private static char GetHexValue(int i)
		{
			if (i < 10)
			{
				return (char)(i + 48);
			}
			return (char)(i - 10 + 65);
		}

		// Token: 0x06000A2E RID: 2606 RVA: 0x00020E30 File Offset: 0x0001F030
		[__DynamicallyInvokable]
		public static string ToString(byte[] value, int startIndex, int length)
		{
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
			if (startIndex < 0 || (startIndex >= value.Length && startIndex > 0))
			{
				throw new ArgumentOutOfRangeException("startIndex", Environment.GetResourceString("ArgumentOutOfRange_StartIndex"));
			}
			if (length < 0)
			{
				throw new ArgumentOutOfRangeException("length", Environment.GetResourceString("ArgumentOutOfRange_GenericPositive"));
			}
			if (startIndex > value.Length - length)
			{
				throw new ArgumentException(Environment.GetResourceString("Arg_ArrayPlusOffTooSmall"));
			}
			if (length == 0)
			{
				return string.Empty;
			}
			if (length > 715827882)
			{
				throw new ArgumentOutOfRangeException("length", Environment.GetResourceString("ArgumentOutOfRange_LengthTooLarge", new object[] { 715827882 }));
			}
			int num = length * 3;
			char[] array = new char[num];
			int num2 = startIndex;
			for (int i = 0; i < num; i += 3)
			{
				byte b = value[num2++];
				array[i] = BitConverter.GetHexValue((int)(b / 16));
				array[i + 1] = BitConverter.GetHexValue((int)(b % 16));
				array[i + 2] = '-';
			}
			return new string(array, 0, array.Length - 1);
		}

		// Token: 0x06000A2F RID: 2607 RVA: 0x00020F2B File Offset: 0x0001F12B
		[__DynamicallyInvokable]
		public static string ToString(byte[] value)
		{
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
			return BitConverter.ToString(value, 0, value.Length);
		}

		// Token: 0x06000A30 RID: 2608 RVA: 0x00020F45 File Offset: 0x0001F145
		[__DynamicallyInvokable]
		public static string ToString(byte[] value, int startIndex)
		{
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
			return BitConverter.ToString(value, startIndex, value.Length - startIndex);
		}

		// Token: 0x06000A31 RID: 2609 RVA: 0x00020F64 File Offset: 0x0001F164
		[__DynamicallyInvokable]
		public static bool ToBoolean(byte[] value, int startIndex)
		{
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
			if (startIndex < 0)
			{
				throw new ArgumentOutOfRangeException("startIndex", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
			}
			if (startIndex > value.Length - 1)
			{
				throw new ArgumentOutOfRangeException("startIndex", Environment.GetResourceString("ArgumentOutOfRange_Index"));
			}
			return value[startIndex] != 0;
		}

		// Token: 0x06000A32 RID: 2610 RVA: 0x00020FBD File Offset: 0x0001F1BD
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		public unsafe static long DoubleToInt64Bits(double value)
		{
			return *(long*)(&value);
		}

		// Token: 0x06000A33 RID: 2611 RVA: 0x00020FC3 File Offset: 0x0001F1C3
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		public unsafe static double Int64BitsToDouble(long value)
		{
			return *(double*)(&value);
		}

		// Token: 0x040003EC RID: 1004
		[__DynamicallyInvokable]
		public static readonly bool IsLittleEndian = true;
	}
}
