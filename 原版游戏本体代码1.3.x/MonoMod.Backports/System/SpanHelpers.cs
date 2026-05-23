using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using MonoMod;

namespace System
{
	// Token: 0x0200001A RID: 26
	[NullableContext(1)]
	[Nullable(0)]
	internal static class SpanHelpers
	{
		// Token: 0x060000F7 RID: 247 RVA: 0x00006092 File Offset: 0x00004292
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int BinarySearch<[Nullable(2)] T, [Nullable(0)] TComparable>([Nullable(new byte[] { 0, 1 })] this ReadOnlySpan<T> span, TComparable comparable) where TComparable : IComparable<T>
		{
			if (comparable == null)
			{
				ThrowHelper.ThrowArgumentNullException(ExceptionArgument.comparable);
			}
			return SpanHelpers.BinarySearch<T, TComparable>(MemoryMarshal.GetReference<T>(span), span.Length, comparable);
		}

		// Token: 0x060000F8 RID: 248 RVA: 0x000060B8 File Offset: 0x000042B8
		public unsafe static int BinarySearch<[Nullable(2)] T, [Nullable(0)] TComparable>(ref T spanStart, int length, TComparable comparable) where TComparable : IComparable<T>
		{
			int i = 0;
			int num = length - 1;
			while (i <= num)
			{
				int num2 = (int)((uint)(num + i) >> 1);
				ref TComparable ptr = ref comparable;
				if (default(TComparable) == null)
				{
					TComparable tcomparable = comparable;
					ptr = ref tcomparable;
				}
				int num3 = ptr.CompareTo(*Unsafe.Add<T>(ref spanStart, num2));
				if (num3 == 0)
				{
					return num2;
				}
				if (num3 > 0)
				{
					i = num2 + 1;
				}
				else
				{
					num = num2 - 1;
				}
			}
			return ~i;
		}

		// Token: 0x060000F9 RID: 249 RVA: 0x00006120 File Offset: 0x00004320
		public static int IndexOf(ref byte searchSpace, int searchSpaceLength, ref byte value, int valueLength)
		{
			if (valueLength == 0)
			{
				return 0;
			}
			byte value2 = value;
			ref byte second = ref Unsafe.Add<byte>(ref value, 1);
			int num = valueLength - 1;
			int num2 = 0;
			for (;;)
			{
				int num3 = searchSpaceLength - num2 - num;
				if (num3 <= 0)
				{
					return -1;
				}
				int num4 = SpanHelpers.IndexOf(Unsafe.Add<byte>(ref searchSpace, num2), value2, num3);
				if (num4 == -1)
				{
					return -1;
				}
				num2 += num4;
				if (SpanHelpers.SequenceEqual<byte>(Unsafe.Add<byte>(ref searchSpace, num2 + 1), ref second, num))
				{
					break;
				}
				num2++;
			}
			return num2;
		}

		// Token: 0x060000FA RID: 250 RVA: 0x00006188 File Offset: 0x00004388
		public unsafe static int IndexOfAny(ref byte searchSpace, int searchSpaceLength, ref byte value, int valueLength)
		{
			if (valueLength == 0)
			{
				return 0;
			}
			int num = -1;
			for (int i = 0; i < valueLength; i++)
			{
				int num2 = SpanHelpers.IndexOf(ref searchSpace, *Unsafe.Add<byte>(ref value, i), searchSpaceLength);
				if (num2 < num)
				{
					num = num2;
					searchSpaceLength = num2;
					if (num == 0)
					{
						break;
					}
				}
			}
			return num;
		}

		// Token: 0x060000FB RID: 251 RVA: 0x000061C8 File Offset: 0x000043C8
		public unsafe static int LastIndexOfAny(ref byte searchSpace, int searchSpaceLength, ref byte value, int valueLength)
		{
			if (valueLength == 0)
			{
				return 0;
			}
			int num = -1;
			for (int i = 0; i < valueLength; i++)
			{
				int num2 = SpanHelpers.LastIndexOf(ref searchSpace, *Unsafe.Add<byte>(ref value, i), searchSpaceLength);
				if (num2 > num)
				{
					num = num2;
				}
			}
			return num;
		}

		// Token: 0x060000FC RID: 252 RVA: 0x00006200 File Offset: 0x00004400
		public unsafe static int IndexOf(ref byte searchSpace, byte value, int length)
		{
			IntPtr intPtr = (IntPtr)0;
			IntPtr intPtr2 = (IntPtr)length;
			while (intPtr2 >= (IntPtr)8)
			{
				intPtr2 -= (IntPtr)8;
				if (value == *Unsafe.AddByteOffset<byte>(ref searchSpace, intPtr))
				{
					IL_106:
					return (int)intPtr;
				}
				if (value == *Unsafe.AddByteOffset<byte>(ref searchSpace, intPtr + (IntPtr)1))
				{
					IL_109:
					return (int)(intPtr + (IntPtr)1);
				}
				if (value == *Unsafe.AddByteOffset<byte>(ref searchSpace, intPtr + (IntPtr)2))
				{
					IL_10F:
					return (int)(intPtr + (IntPtr)2);
				}
				if (value == *Unsafe.AddByteOffset<byte>(ref searchSpace, intPtr + (IntPtr)3))
				{
					IL_115:
					return (int)(intPtr + (IntPtr)3);
				}
				if (value == *Unsafe.AddByteOffset<byte>(ref searchSpace, intPtr + (IntPtr)4))
				{
					return (int)(intPtr + (IntPtr)4);
				}
				if (value == *Unsafe.AddByteOffset<byte>(ref searchSpace, intPtr + (IntPtr)5))
				{
					return (int)(intPtr + (IntPtr)5);
				}
				if (value == *Unsafe.AddByteOffset<byte>(ref searchSpace, intPtr + (IntPtr)6))
				{
					return (int)(intPtr + (IntPtr)6);
				}
				if (value == *Unsafe.AddByteOffset<byte>(ref searchSpace, intPtr + (IntPtr)7))
				{
					return (int)(intPtr + (IntPtr)7);
				}
				intPtr += (IntPtr)8;
			}
			if (intPtr2 >= (IntPtr)4)
			{
				intPtr2 -= (IntPtr)4;
				if (value == *Unsafe.AddByteOffset<byte>(ref searchSpace, intPtr))
				{
					goto IL_106;
				}
				if (value == *Unsafe.AddByteOffset<byte>(ref searchSpace, intPtr + (IntPtr)1))
				{
					goto IL_109;
				}
				if (value == *Unsafe.AddByteOffset<byte>(ref searchSpace, intPtr + (IntPtr)2))
				{
					goto IL_10F;
				}
				if (value == *Unsafe.AddByteOffset<byte>(ref searchSpace, intPtr + (IntPtr)3))
				{
					goto IL_115;
				}
				intPtr += (IntPtr)4;
			}
			while (intPtr2 > (IntPtr)0)
			{
				intPtr2 -= (IntPtr)1;
				if (value == *Unsafe.AddByteOffset<byte>(ref searchSpace, intPtr))
				{
					goto IL_106;
				}
				intPtr += (IntPtr)1;
			}
			return -1;
		}

		// Token: 0x060000FD RID: 253 RVA: 0x00006340 File Offset: 0x00004540
		public static int LastIndexOf(ref byte searchSpace, int searchSpaceLength, ref byte value, int valueLength)
		{
			if (valueLength == 0)
			{
				return 0;
			}
			byte value2 = value;
			ref byte second = ref Unsafe.Add<byte>(ref value, 1);
			int num = valueLength - 1;
			int num2 = 0;
			int num4;
			for (;;)
			{
				int num3 = searchSpaceLength - num2 - num;
				if (num3 <= 0)
				{
					return -1;
				}
				num4 = SpanHelpers.LastIndexOf(ref searchSpace, value2, num3);
				if (num4 == -1)
				{
					return -1;
				}
				if (SpanHelpers.SequenceEqual<byte>(Unsafe.Add<byte>(ref searchSpace, num4 + 1), ref second, num))
				{
					break;
				}
				num2 += num3 - num4;
			}
			return num4;
		}

		// Token: 0x060000FE RID: 254 RVA: 0x000063A0 File Offset: 0x000045A0
		public unsafe static int LastIndexOf(ref byte searchSpace, byte value, int length)
		{
			IntPtr intPtr = (IntPtr)length;
			IntPtr intPtr2 = (IntPtr)length;
			while (intPtr2 >= (IntPtr)8)
			{
				intPtr2 -= (IntPtr)8;
				intPtr -= (IntPtr)8;
				if (value == *Unsafe.AddByteOffset<byte>(ref searchSpace, intPtr + (IntPtr)7))
				{
					return (int)(intPtr + (IntPtr)7);
				}
				if (value == *Unsafe.AddByteOffset<byte>(ref searchSpace, intPtr + (IntPtr)6))
				{
					return (int)(intPtr + (IntPtr)6);
				}
				if (value == *Unsafe.AddByteOffset<byte>(ref searchSpace, intPtr + (IntPtr)5))
				{
					return (int)(intPtr + (IntPtr)5);
				}
				if (value == *Unsafe.AddByteOffset<byte>(ref searchSpace, intPtr + (IntPtr)4))
				{
					return (int)(intPtr + (IntPtr)4);
				}
				if (value == *Unsafe.AddByteOffset<byte>(ref searchSpace, intPtr + (IntPtr)3))
				{
					IL_10F:
					return (int)(intPtr + (IntPtr)3);
				}
				if (value == *Unsafe.AddByteOffset<byte>(ref searchSpace, intPtr + (IntPtr)2))
				{
					IL_109:
					return (int)(intPtr + (IntPtr)2);
				}
				if (value == *Unsafe.AddByteOffset<byte>(ref searchSpace, intPtr + (IntPtr)1))
				{
					IL_103:
					return (int)(intPtr + (IntPtr)1);
				}
				if (value == *Unsafe.AddByteOffset<byte>(ref searchSpace, intPtr))
				{
					IL_100:
					return (int)intPtr;
				}
			}
			if (intPtr2 >= (IntPtr)4)
			{
				intPtr2 -= (IntPtr)4;
				intPtr -= (IntPtr)4;
				if (value == *Unsafe.AddByteOffset<byte>(ref searchSpace, intPtr + (IntPtr)3))
				{
					goto IL_10F;
				}
				if (value == *Unsafe.AddByteOffset<byte>(ref searchSpace, intPtr + (IntPtr)2))
				{
					goto IL_109;
				}
				if (value == *Unsafe.AddByteOffset<byte>(ref searchSpace, intPtr + (IntPtr)1))
				{
					goto IL_103;
				}
				if (value == *Unsafe.AddByteOffset<byte>(ref searchSpace, intPtr))
				{
					goto IL_100;
				}
			}
			while (intPtr2 > (IntPtr)0)
			{
				intPtr2 -= (IntPtr)1;
				intPtr -= (IntPtr)1;
				if (value == *Unsafe.AddByteOffset<byte>(ref searchSpace, intPtr))
				{
					goto IL_100;
				}
			}
			return -1;
		}

		// Token: 0x060000FF RID: 255 RVA: 0x000064DC File Offset: 0x000046DC
		public unsafe static int IndexOfAny(ref byte searchSpace, byte value0, byte value1, int length)
		{
			IntPtr intPtr = (IntPtr)0;
			IntPtr intPtr2 = (IntPtr)length;
			while (intPtr2 >= (IntPtr)8)
			{
				intPtr2 -= (IntPtr)8;
				uint num = (uint)(*Unsafe.AddByteOffset<byte>(ref searchSpace, intPtr));
				if ((uint)value0 == num || (uint)value1 == num)
				{
					IL_198:
					return (int)intPtr;
				}
				num = (uint)(*Unsafe.AddByteOffset<byte>(ref searchSpace, intPtr + (IntPtr)1));
				if ((uint)value0 == num || (uint)value1 == num)
				{
					IL_19B:
					return (int)(intPtr + (IntPtr)1);
				}
				num = (uint)(*Unsafe.AddByteOffset<byte>(ref searchSpace, intPtr + (IntPtr)2));
				if ((uint)value0 == num || (uint)value1 == num)
				{
					IL_1A1:
					return (int)(intPtr + (IntPtr)2);
				}
				num = (uint)(*Unsafe.AddByteOffset<byte>(ref searchSpace, intPtr + (IntPtr)3));
				if ((uint)value0 == num || (uint)value1 == num)
				{
					IL_1A7:
					return (int)(intPtr + (IntPtr)3);
				}
				num = (uint)(*Unsafe.AddByteOffset<byte>(ref searchSpace, intPtr + (IntPtr)4));
				if ((uint)value0 == num || (uint)value1 == num)
				{
					return (int)(intPtr + (IntPtr)4);
				}
				num = (uint)(*Unsafe.AddByteOffset<byte>(ref searchSpace, intPtr + (IntPtr)5));
				if ((uint)value0 == num || (uint)value1 == num)
				{
					return (int)(intPtr + (IntPtr)5);
				}
				num = (uint)(*Unsafe.AddByteOffset<byte>(ref searchSpace, intPtr + (IntPtr)6));
				if ((uint)value0 == num || (uint)value1 == num)
				{
					return (int)(intPtr + (IntPtr)6);
				}
				num = (uint)(*Unsafe.AddByteOffset<byte>(ref searchSpace, intPtr + (IntPtr)7));
				if ((uint)value0 == num || (uint)value1 == num)
				{
					return (int)(intPtr + (IntPtr)7);
				}
				intPtr += (IntPtr)8;
			}
			if (intPtr2 >= (IntPtr)4)
			{
				intPtr2 -= (IntPtr)4;
				uint num = (uint)(*Unsafe.AddByteOffset<byte>(ref searchSpace, intPtr));
				if ((uint)value0 == num || (uint)value1 == num)
				{
					goto IL_198;
				}
				num = (uint)(*Unsafe.AddByteOffset<byte>(ref searchSpace, intPtr + (IntPtr)1));
				if ((uint)value0 == num || (uint)value1 == num)
				{
					goto IL_19B;
				}
				num = (uint)(*Unsafe.AddByteOffset<byte>(ref searchSpace, intPtr + (IntPtr)2));
				if ((uint)value0 == num || (uint)value1 == num)
				{
					goto IL_1A1;
				}
				num = (uint)(*Unsafe.AddByteOffset<byte>(ref searchSpace, intPtr + (IntPtr)3));
				if ((uint)value0 == num || (uint)value1 == num)
				{
					goto IL_1A7;
				}
				intPtr += (IntPtr)4;
			}
			while (intPtr2 > (IntPtr)0)
			{
				intPtr2 -= (IntPtr)1;
				uint num = (uint)(*Unsafe.AddByteOffset<byte>(ref searchSpace, intPtr));
				if ((uint)value0 == num || (uint)value1 == num)
				{
					goto IL_198;
				}
				intPtr += (IntPtr)1;
			}
			return -1;
		}

		// Token: 0x06000100 RID: 256 RVA: 0x000066B0 File Offset: 0x000048B0
		public unsafe static int IndexOfAny(ref byte searchSpace, byte value0, byte value1, byte value2, int length)
		{
			IntPtr intPtr = (IntPtr)0;
			IntPtr intPtr2 = (IntPtr)length;
			while (intPtr2 >= (IntPtr)8)
			{
				intPtr2 -= (IntPtr)8;
				uint num = (uint)(*Unsafe.AddByteOffset<byte>(ref searchSpace, intPtr));
				if ((uint)value0 == num || (uint)value1 == num || (uint)value2 == num)
				{
					IL_207:
					return (int)intPtr;
				}
				num = (uint)(*Unsafe.AddByteOffset<byte>(ref searchSpace, intPtr + (IntPtr)1));
				if ((uint)value0 == num || (uint)value1 == num || (uint)value2 == num)
				{
					IL_20A:
					return (int)(intPtr + (IntPtr)1);
				}
				num = (uint)(*Unsafe.AddByteOffset<byte>(ref searchSpace, intPtr + (IntPtr)2));
				if ((uint)value0 == num || (uint)value1 == num || (uint)value2 == num)
				{
					IL_210:
					return (int)(intPtr + (IntPtr)2);
				}
				num = (uint)(*Unsafe.AddByteOffset<byte>(ref searchSpace, intPtr + (IntPtr)3));
				if ((uint)value0 == num || (uint)value1 == num || (uint)value2 == num)
				{
					IL_216:
					return (int)(intPtr + (IntPtr)3);
				}
				num = (uint)(*Unsafe.AddByteOffset<byte>(ref searchSpace, intPtr + (IntPtr)4));
				if ((uint)value0 == num || (uint)value1 == num || (uint)value2 == num)
				{
					return (int)(intPtr + (IntPtr)4);
				}
				num = (uint)(*Unsafe.AddByteOffset<byte>(ref searchSpace, intPtr + (IntPtr)5));
				if ((uint)value0 == num || (uint)value1 == num || (uint)value2 == num)
				{
					return (int)(intPtr + (IntPtr)5);
				}
				num = (uint)(*Unsafe.AddByteOffset<byte>(ref searchSpace, intPtr + (IntPtr)6));
				if ((uint)value0 == num || (uint)value1 == num || (uint)value2 == num)
				{
					return (int)(intPtr + (IntPtr)6);
				}
				num = (uint)(*Unsafe.AddByteOffset<byte>(ref searchSpace, intPtr + (IntPtr)7));
				if ((uint)value0 == num || (uint)value1 == num || (uint)value2 == num)
				{
					return (int)(intPtr + (IntPtr)7);
				}
				intPtr += (IntPtr)8;
			}
			if (intPtr2 >= (IntPtr)4)
			{
				intPtr2 -= (IntPtr)4;
				uint num = (uint)(*Unsafe.AddByteOffset<byte>(ref searchSpace, intPtr));
				if ((uint)value0 == num || (uint)value1 == num || (uint)value2 == num)
				{
					goto IL_207;
				}
				num = (uint)(*Unsafe.AddByteOffset<byte>(ref searchSpace, intPtr + (IntPtr)1));
				if ((uint)value0 == num || (uint)value1 == num || (uint)value2 == num)
				{
					goto IL_20A;
				}
				num = (uint)(*Unsafe.AddByteOffset<byte>(ref searchSpace, intPtr + (IntPtr)2));
				if ((uint)value0 == num || (uint)value1 == num || (uint)value2 == num)
				{
					goto IL_210;
				}
				num = (uint)(*Unsafe.AddByteOffset<byte>(ref searchSpace, intPtr + (IntPtr)3));
				if ((uint)value0 == num || (uint)value1 == num || (uint)value2 == num)
				{
					goto IL_216;
				}
				intPtr += (IntPtr)4;
			}
			while (intPtr2 > (IntPtr)0)
			{
				intPtr2 -= (IntPtr)1;
				uint num = (uint)(*Unsafe.AddByteOffset<byte>(ref searchSpace, intPtr));
				if ((uint)value0 == num || (uint)value1 == num || (uint)value2 == num)
				{
					goto IL_207;
				}
				intPtr += (IntPtr)1;
			}
			return -1;
		}

		// Token: 0x06000101 RID: 257 RVA: 0x000068F0 File Offset: 0x00004AF0
		public unsafe static int LastIndexOfAny(ref byte searchSpace, byte value0, byte value1, int length)
		{
			IntPtr intPtr = (IntPtr)length;
			IntPtr intPtr2 = (IntPtr)length;
			while (intPtr2 >= (IntPtr)8)
			{
				intPtr2 -= (IntPtr)8;
				intPtr -= (IntPtr)8;
				uint num = (uint)(*Unsafe.AddByteOffset<byte>(ref searchSpace, intPtr + (IntPtr)7));
				if ((uint)value0 == num || (uint)value1 == num)
				{
					return (int)(intPtr + (IntPtr)7);
				}
				num = (uint)(*Unsafe.AddByteOffset<byte>(ref searchSpace, intPtr + (IntPtr)6));
				if ((uint)value0 == num || (uint)value1 == num)
				{
					return (int)(intPtr + (IntPtr)6);
				}
				num = (uint)(*Unsafe.AddByteOffset<byte>(ref searchSpace, intPtr + (IntPtr)5));
				if ((uint)value0 == num || (uint)value1 == num)
				{
					return (int)(intPtr + (IntPtr)5);
				}
				num = (uint)(*Unsafe.AddByteOffset<byte>(ref searchSpace, intPtr + (IntPtr)4));
				if ((uint)value0 == num || (uint)value1 == num)
				{
					return (int)(intPtr + (IntPtr)4);
				}
				num = (uint)(*Unsafe.AddByteOffset<byte>(ref searchSpace, intPtr + (IntPtr)3));
				if ((uint)value0 == num || (uint)value1 == num)
				{
					IL_1A7:
					return (int)(intPtr + (IntPtr)3);
				}
				num = (uint)(*Unsafe.AddByteOffset<byte>(ref searchSpace, intPtr + (IntPtr)2));
				if ((uint)value0 == num || (uint)value1 == num)
				{
					IL_1A1:
					return (int)(intPtr + (IntPtr)2);
				}
				num = (uint)(*Unsafe.AddByteOffset<byte>(ref searchSpace, intPtr + (IntPtr)1));
				if ((uint)value0 == num || (uint)value1 == num)
				{
					IL_19B:
					return (int)(intPtr + (IntPtr)1);
				}
				num = (uint)(*Unsafe.AddByteOffset<byte>(ref searchSpace, intPtr));
				if ((uint)value0 == num || (uint)value1 == num)
				{
					IL_198:
					return (int)intPtr;
				}
			}
			if (intPtr2 >= (IntPtr)4)
			{
				intPtr2 -= (IntPtr)4;
				intPtr -= (IntPtr)4;
				uint num = (uint)(*Unsafe.AddByteOffset<byte>(ref searchSpace, intPtr + (IntPtr)3));
				if ((uint)value0 == num || (uint)value1 == num)
				{
					goto IL_1A7;
				}
				num = (uint)(*Unsafe.AddByteOffset<byte>(ref searchSpace, intPtr + (IntPtr)2));
				if ((uint)value0 == num || (uint)value1 == num)
				{
					goto IL_1A1;
				}
				num = (uint)(*Unsafe.AddByteOffset<byte>(ref searchSpace, intPtr + (IntPtr)1));
				if ((uint)value0 == num || (uint)value1 == num)
				{
					goto IL_19B;
				}
				num = (uint)(*Unsafe.AddByteOffset<byte>(ref searchSpace, intPtr));
				if ((uint)value0 == num)
				{
					goto IL_198;
				}
				if ((uint)value1 == num)
				{
					goto IL_198;
				}
			}
			while (intPtr2 > (IntPtr)0)
			{
				intPtr2 -= (IntPtr)1;
				intPtr -= (IntPtr)1;
				uint num = (uint)(*Unsafe.AddByteOffset<byte>(ref searchSpace, intPtr));
				if ((uint)value0 == num || (uint)value1 == num)
				{
					goto IL_198;
				}
			}
			return -1;
		}

		// Token: 0x06000102 RID: 258 RVA: 0x00006AC4 File Offset: 0x00004CC4
		public unsafe static int LastIndexOfAny(ref byte searchSpace, byte value0, byte value1, byte value2, int length)
		{
			IntPtr intPtr = (IntPtr)length;
			IntPtr intPtr2 = (IntPtr)length;
			while (intPtr2 >= (IntPtr)8)
			{
				intPtr2 -= (IntPtr)8;
				intPtr -= (IntPtr)8;
				uint num = (uint)(*Unsafe.AddByteOffset<byte>(ref searchSpace, intPtr + (IntPtr)7));
				if ((uint)value0 == num || (uint)value1 == num || (uint)value2 == num)
				{
					return (int)(intPtr + (IntPtr)7);
				}
				num = (uint)(*Unsafe.AddByteOffset<byte>(ref searchSpace, intPtr + (IntPtr)6));
				if ((uint)value0 == num || (uint)value1 == num || (uint)value2 == num)
				{
					return (int)(intPtr + (IntPtr)6);
				}
				num = (uint)(*Unsafe.AddByteOffset<byte>(ref searchSpace, intPtr + (IntPtr)5));
				if ((uint)value0 == num || (uint)value1 == num || (uint)value2 == num)
				{
					return (int)(intPtr + (IntPtr)5);
				}
				num = (uint)(*Unsafe.AddByteOffset<byte>(ref searchSpace, intPtr + (IntPtr)4));
				if ((uint)value0 == num || (uint)value1 == num || (uint)value2 == num)
				{
					return (int)(intPtr + (IntPtr)4);
				}
				num = (uint)(*Unsafe.AddByteOffset<byte>(ref searchSpace, intPtr + (IntPtr)3));
				if ((uint)value0 == num || (uint)value1 == num || (uint)value2 == num)
				{
					IL_217:
					return (int)(intPtr + (IntPtr)3);
				}
				num = (uint)(*Unsafe.AddByteOffset<byte>(ref searchSpace, intPtr + (IntPtr)2));
				if ((uint)value0 == num || (uint)value1 == num || (uint)value2 == num)
				{
					IL_211:
					return (int)(intPtr + (IntPtr)2);
				}
				num = (uint)(*Unsafe.AddByteOffset<byte>(ref searchSpace, intPtr + (IntPtr)1));
				if ((uint)value0 == num || (uint)value1 == num || (uint)value2 == num)
				{
					IL_20B:
					return (int)(intPtr + (IntPtr)1);
				}
				num = (uint)(*Unsafe.AddByteOffset<byte>(ref searchSpace, intPtr));
				if ((uint)value0 == num || (uint)value1 == num || (uint)value2 == num)
				{
					IL_208:
					return (int)intPtr;
				}
			}
			if (intPtr2 >= (IntPtr)4)
			{
				intPtr2 -= (IntPtr)4;
				intPtr -= (IntPtr)4;
				uint num = (uint)(*Unsafe.AddByteOffset<byte>(ref searchSpace, intPtr + (IntPtr)3));
				if ((uint)value0 == num || (uint)value1 == num || (uint)value2 == num)
				{
					goto IL_217;
				}
				num = (uint)(*Unsafe.AddByteOffset<byte>(ref searchSpace, intPtr + (IntPtr)2));
				if ((uint)value0 == num || (uint)value1 == num || (uint)value2 == num)
				{
					goto IL_211;
				}
				num = (uint)(*Unsafe.AddByteOffset<byte>(ref searchSpace, intPtr + (IntPtr)1));
				if ((uint)value0 == num || (uint)value1 == num || (uint)value2 == num)
				{
					goto IL_20B;
				}
				num = (uint)(*Unsafe.AddByteOffset<byte>(ref searchSpace, intPtr));
				if ((uint)value0 == num || (uint)value1 == num)
				{
					goto IL_208;
				}
				if ((uint)value2 == num)
				{
					goto IL_208;
				}
			}
			while (intPtr2 > (IntPtr)0)
			{
				intPtr2 -= (IntPtr)1;
				intPtr -= (IntPtr)1;
				uint num = (uint)(*Unsafe.AddByteOffset<byte>(ref searchSpace, intPtr));
				if ((uint)value0 == num || (uint)value1 == num || (uint)value2 == num)
				{
					goto IL_208;
				}
			}
			return -1;
		}

		// Token: 0x06000103 RID: 259 RVA: 0x00006D08 File Offset: 0x00004F08
		public unsafe static bool SequenceEqual(ref byte first, ref byte second, [NativeInteger] UIntPtr length)
		{
			if (!Unsafe.AreSame<byte>(ref first, ref second))
			{
				IntPtr intPtr = (IntPtr)0;
				if (length >= (UIntPtr)((IntPtr)sizeof(UIntPtr)))
				{
					IntPtr intPtr2 = (IntPtr)(length - (UIntPtr)((IntPtr)sizeof(UIntPtr)));
					while (intPtr2 > intPtr)
					{
						if (Unsafe.ReadUnaligned<UIntPtr>(Unsafe.AddByteOffset<byte>(ref first, intPtr)) != Unsafe.ReadUnaligned<UIntPtr>(Unsafe.AddByteOffset<byte>(ref second, intPtr)))
						{
							return false;
						}
						intPtr += (IntPtr)sizeof(UIntPtr);
					}
					return Unsafe.ReadUnaligned<UIntPtr>(Unsafe.AddByteOffset<byte>(ref first, intPtr2)) == Unsafe.ReadUnaligned<UIntPtr>(Unsafe.AddByteOffset<byte>(ref second, intPtr2));
				}
				while (length > (UIntPtr)intPtr)
				{
					if (*Unsafe.AddByteOffset<byte>(ref first, intPtr) != *Unsafe.AddByteOffset<byte>(ref second, intPtr))
					{
						return false;
					}
					intPtr += (IntPtr)1;
				}
				return true;
			}
			return true;
		}

		// Token: 0x06000104 RID: 260 RVA: 0x00006DA8 File Offset: 0x00004FA8
		public unsafe static int SequenceCompareTo(ref byte first, int firstLength, ref byte second, int secondLength)
		{
			if (!Unsafe.AreSame<byte>(ref first, ref second))
			{
				IntPtr intPtr = (IntPtr)((firstLength < secondLength) ? firstLength : secondLength);
				IntPtr intPtr2 = (IntPtr)0;
				IntPtr intPtr3 = intPtr;
				if (intPtr3 > (IntPtr)sizeof(UIntPtr))
				{
					intPtr3 -= (IntPtr)sizeof(UIntPtr);
					while (intPtr3 > intPtr2)
					{
						if (Unsafe.ReadUnaligned<UIntPtr>(Unsafe.AddByteOffset<byte>(ref first, intPtr2)) != Unsafe.ReadUnaligned<UIntPtr>(Unsafe.AddByteOffset<byte>(ref second, intPtr2)))
						{
							break;
						}
						intPtr2 += (IntPtr)sizeof(UIntPtr);
					}
				}
				while (intPtr > intPtr2)
				{
					int num = Unsafe.AddByteOffset<byte>(ref first, intPtr2).CompareTo(*Unsafe.AddByteOffset<byte>(ref second, intPtr2));
					if (num != 0)
					{
						return num;
					}
					intPtr2 += (IntPtr)1;
				}
			}
			return firstLength - secondLength;
		}

		// Token: 0x06000105 RID: 261 RVA: 0x00006E38 File Offset: 0x00005038
		public unsafe static int SequenceCompareTo(ref char first, int firstLength, ref char second, int secondLength)
		{
			int result = firstLength - secondLength;
			if (!Unsafe.AreSame<char>(ref first, ref second))
			{
				IntPtr intPtr = (IntPtr)((firstLength < secondLength) ? firstLength : secondLength);
				IntPtr intPtr2 = (IntPtr)0;
				if (intPtr >= (IntPtr)(sizeof(UIntPtr) / 2))
				{
					while (intPtr >= intPtr2 + (IntPtr)(sizeof(UIntPtr) / 2) && !(Unsafe.ReadUnaligned<UIntPtr>(Unsafe.As<char, byte>(Unsafe.Add<char>(ref first, intPtr2))) != Unsafe.ReadUnaligned<UIntPtr>(Unsafe.As<char, byte>(Unsafe.Add<char>(ref second, intPtr2)))))
					{
						intPtr2 += (IntPtr)(sizeof(UIntPtr) / 2);
					}
				}
				if (sizeof(UIntPtr) > 4 && intPtr >= intPtr2 + (IntPtr)2 && Unsafe.ReadUnaligned<int>(Unsafe.As<char, byte>(Unsafe.Add<char>(ref first, intPtr2))) == Unsafe.ReadUnaligned<int>(Unsafe.As<char, byte>(Unsafe.Add<char>(ref second, intPtr2))))
				{
					intPtr2 += (IntPtr)2;
				}
				while (intPtr2 < intPtr)
				{
					int num = Unsafe.Add<char>(ref first, intPtr2).CompareTo(*Unsafe.Add<char>(ref second, intPtr2));
					if (num != 0)
					{
						return num;
					}
					intPtr2 += (IntPtr)1;
				}
			}
			return result;
		}

		// Token: 0x06000106 RID: 262 RVA: 0x00006F14 File Offset: 0x00005114
		public unsafe static int IndexOf(ref char searchSpace, char value, int length)
		{
			fixed (char* ptr = &searchSpace)
			{
				char* ptr2 = ptr;
				char* ptr3 = ptr2;
				IntPtr intPtr = (IntPtr)length;
				while (length >= 4)
				{
					length -= 4;
					if (*ptr3 != value)
					{
						if (ptr3[1] != value)
						{
							if (ptr3[2] != value)
							{
								if (ptr3[3] != value)
								{
									ptr3 += 4;
									continue;
								}
								ptr3++;
							}
							ptr3++;
						}
						ptr3++;
					}
					IL_5E:
					return (int)((long)(ptr3 - ptr2));
				}
				while (length > 0)
				{
					length--;
					if (*ptr3 == value)
					{
						goto IL_5E;
					}
					ptr3++;
				}
				return -1;
			}
		}

		// Token: 0x06000107 RID: 263 RVA: 0x00006F88 File Offset: 0x00005188
		public unsafe static int LastIndexOf(ref char searchSpace, char value, int length)
		{
			fixed (char* ptr = &searchSpace)
			{
				char* ptr2 = ptr;
				char* ptr3 = ptr2 + length;
				char* ptr4 = ptr2;
				while (length >= 4)
				{
					length -= 4;
					ptr3 -= 4;
					if (ptr3[3] == value)
					{
						return (int)((long)(ptr3 - ptr4)) + 3;
					}
					if (ptr3[2] == value)
					{
						return (int)((long)(ptr3 - ptr4)) + 2;
					}
					if (ptr3[1] == value)
					{
						return (int)((long)(ptr3 - ptr4)) + 1;
					}
					if (*ptr3 == value)
					{
						IL_54:
						return (int)((long)(ptr3 - ptr4));
					}
				}
				while (length > 0)
				{
					length--;
					ptr3--;
					if (*ptr3 == value)
					{
						goto IL_54;
					}
				}
				return -1;
			}
		}

		// Token: 0x06000108 RID: 264 RVA: 0x00007010 File Offset: 0x00005210
		public unsafe static void CopyTo<[Nullable(2)] T>(ref T dst, int dstLength, ref T src, int srcLength)
		{
			IntPtr value = Unsafe.ByteOffset<T>(ref src, Unsafe.Add<T>(ref src, srcLength));
			IntPtr value2 = Unsafe.ByteOffset<T>(ref dst, Unsafe.Add<T>(ref dst, dstLength));
			IntPtr value3 = Unsafe.ByteOffset<T>(ref src, ref dst);
			if (!((sizeof(IntPtr) == 4) ? ((int)value3 < (int)value || (int)value3 > -(int)value2) : ((long)value3 < (long)value || (long)value3 > -(long)value2)) && !SpanHelpers.IsReferenceOrContainsReferences<T>())
			{
				ref byte source = ref Unsafe.As<T, byte>(ref dst);
				ref byte source2 = ref Unsafe.As<T, byte>(ref src);
				ulong num = (ulong)(long)value;
				uint num3;
				for (ulong num2 = 0UL; num2 < num; num2 += (ulong)num3)
				{
					num3 = ((num - num2 > (ulong)(-1)) ? uint.MaxValue : ((uint)(num - num2)));
					Unsafe.CopyBlock(Unsafe.Add<byte>(ref source, (IntPtr)((long)num2)), Unsafe.Add<byte>(ref source2, (IntPtr)((long)num2)), num3);
				}
				return;
			}
			bool flag = ((sizeof(IntPtr) == 4) ? ((int)value3 > -(int)value2) : ((long)value3 > -(long)value2));
			int num4 = (flag ? 1 : (-1));
			int num5 = (flag ? 0 : (srcLength - 1));
			int i;
			for (i = 0; i < (srcLength & -8); i += 8)
			{
				*Unsafe.Add<T>(ref dst, num5) = *Unsafe.Add<T>(ref src, num5);
				*Unsafe.Add<T>(ref dst, num5 + num4) = *Unsafe.Add<T>(ref src, num5 + num4);
				*Unsafe.Add<T>(ref dst, num5 + num4 * 2) = *Unsafe.Add<T>(ref src, num5 + num4 * 2);
				*Unsafe.Add<T>(ref dst, num5 + num4 * 3) = *Unsafe.Add<T>(ref src, num5 + num4 * 3);
				*Unsafe.Add<T>(ref dst, num5 + num4 * 4) = *Unsafe.Add<T>(ref src, num5 + num4 * 4);
				*Unsafe.Add<T>(ref dst, num5 + num4 * 5) = *Unsafe.Add<T>(ref src, num5 + num4 * 5);
				*Unsafe.Add<T>(ref dst, num5 + num4 * 6) = *Unsafe.Add<T>(ref src, num5 + num4 * 6);
				*Unsafe.Add<T>(ref dst, num5 + num4 * 7) = *Unsafe.Add<T>(ref src, num5 + num4 * 7);
				num5 += num4 * 8;
			}
			if (i < (srcLength & -4))
			{
				*Unsafe.Add<T>(ref dst, num5) = *Unsafe.Add<T>(ref src, num5);
				*Unsafe.Add<T>(ref dst, num5 + num4) = *Unsafe.Add<T>(ref src, num5 + num4);
				*Unsafe.Add<T>(ref dst, num5 + num4 * 2) = *Unsafe.Add<T>(ref src, num5 + num4 * 2);
				*Unsafe.Add<T>(ref dst, num5 + num4 * 3) = *Unsafe.Add<T>(ref src, num5 + num4 * 3);
				num5 += num4 * 4;
				i += 4;
			}
			while (i < srcLength)
			{
				*Unsafe.Add<T>(ref dst, num5) = *Unsafe.Add<T>(ref src, num5);
				num5 += num4;
				i++;
			}
		}

		// Token: 0x06000109 RID: 265 RVA: 0x00007334 File Offset: 0x00005534
		[NullableContext(2)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static IntPtr Add<T>(this IntPtr start, int index)
		{
			if (sizeof(IntPtr) == 4)
			{
				uint num = (uint)(index * Unsafe.SizeOf<T>());
				return (IntPtr)((void*)((byte*)(void*)start + num));
			}
			ulong num2 = (ulong)((long)index * (long)Unsafe.SizeOf<T>());
			return (IntPtr)((void*)((byte*)(void*)start + num2));
		}

		// Token: 0x0600010A RID: 266 RVA: 0x00007379 File Offset: 0x00005579
		[NullableContext(2)]
		public static bool IsReferenceOrContainsReferences<T>()
		{
			return SpanHelpers.PerTypeValues<T>.IsReferenceOrContainsReferences;
		}

		// Token: 0x0600010B RID: 267 RVA: 0x00007380 File Offset: 0x00005580
		private static bool IsReferenceOrContainsReferencesCore(Type type)
		{
			if (type.GetTypeInfo().IsPrimitive)
			{
				return false;
			}
			if (!type.GetTypeInfo().IsValueType)
			{
				return true;
			}
			Type underlyingType = Nullable.GetUnderlyingType(type);
			if (underlyingType != null)
			{
				type = underlyingType;
			}
			if (type.GetTypeInfo().IsEnum)
			{
				return false;
			}
			foreach (FieldInfo fieldInfo in type.GetTypeInfo().DeclaredFields)
			{
				if (!fieldInfo.IsStatic && SpanHelpers.IsReferenceOrContainsReferencesCore(fieldInfo.FieldType))
				{
					return true;
				}
			}
			return false;
		}

		// Token: 0x0600010C RID: 268 RVA: 0x00007428 File Offset: 0x00005628
		[NullableContext(0)]
		public unsafe static void ClearLessThanPointerSized(byte* ptr, UIntPtr byteLength)
		{
			if (sizeof(UIntPtr) == 4)
			{
				Unsafe.InitBlockUnaligned((void*)ptr, 0, (uint)byteLength);
				return;
			}
			ulong num = (ulong)byteLength;
			uint num2 = (uint)(num & (ulong)(-1));
			Unsafe.InitBlockUnaligned((void*)ptr, 0, num2);
			num -= (ulong)num2;
			ptr += num2;
			while (num > 0UL)
			{
				num2 = ((num >= (ulong)(-1)) ? uint.MaxValue : ((uint)num));
				Unsafe.InitBlockUnaligned((void*)ptr, 0, num2);
				ptr += num2;
				num -= (ulong)num2;
			}
		}

		// Token: 0x0600010D RID: 269 RVA: 0x00007494 File Offset: 0x00005694
		public static void ClearLessThanPointerSized(ref byte b, UIntPtr byteLength)
		{
			if (sizeof(UIntPtr) == 4)
			{
				Unsafe.InitBlockUnaligned(ref b, 0, (uint)byteLength);
				return;
			}
			ulong num = (ulong)byteLength;
			uint num2 = (uint)(num & (ulong)(-1));
			Unsafe.InitBlockUnaligned(ref b, 0, num2);
			num -= (ulong)num2;
			long num3 = (long)((ulong)num2);
			while (num > 0UL)
			{
				num2 = ((num >= (ulong)(-1)) ? uint.MaxValue : ((uint)num));
				Unsafe.InitBlockUnaligned(Unsafe.Add<byte>(ref b, (IntPtr)num3), 0, num2);
				num3 += (long)((ulong)num2);
				num -= (ulong)num2;
			}
		}

		// Token: 0x0600010E RID: 270 RVA: 0x00007504 File Offset: 0x00005704
		public unsafe static void ClearPointerSizedWithoutReferences(ref byte b, [NativeInteger] UIntPtr byteLength)
		{
			IntPtr intPtr = (IntPtr)0;
			while (intPtr.LessThanEqual(byteLength - (UIntPtr)((IntPtr)sizeof(SpanHelpers.Reg64))))
			{
				*Unsafe.As<byte, SpanHelpers.Reg64>(Unsafe.Add<byte>(ref b, intPtr)) = default(SpanHelpers.Reg64);
				intPtr += (IntPtr)sizeof(SpanHelpers.Reg64);
			}
			if (intPtr.LessThanEqual(byteLength - (UIntPtr)((IntPtr)sizeof(SpanHelpers.Reg32))))
			{
				*Unsafe.As<byte, SpanHelpers.Reg32>(Unsafe.Add<byte>(ref b, intPtr)) = default(SpanHelpers.Reg32);
				intPtr += (IntPtr)sizeof(SpanHelpers.Reg32);
			}
			if (intPtr.LessThanEqual(byteLength - (UIntPtr)((IntPtr)sizeof(SpanHelpers.Reg16))))
			{
				*Unsafe.As<byte, SpanHelpers.Reg16>(Unsafe.Add<byte>(ref b, intPtr)) = default(SpanHelpers.Reg16);
				intPtr += (IntPtr)sizeof(SpanHelpers.Reg16);
			}
			if (intPtr.LessThanEqual(byteLength - (UIntPtr)((IntPtr)8)))
			{
				*Unsafe.As<byte, long>(Unsafe.Add<byte>(ref b, intPtr)) = 0L;
				intPtr += (IntPtr)8;
			}
			if (sizeof(IntPtr) == 4 && intPtr.LessThanEqual(byteLength - (UIntPtr)((IntPtr)4)))
			{
				*Unsafe.As<byte, int>(Unsafe.Add<byte>(ref b, intPtr)) = 0;
			}
		}

		// Token: 0x0600010F RID: 271 RVA: 0x000075E0 File Offset: 0x000057E0
		public unsafe static void ClearPointerSizedWithReferences(ref IntPtr ip, [NativeInteger] UIntPtr pointerSizeLength)
		{
			IntPtr intPtr = (IntPtr)0;
			IntPtr intPtr2;
			while ((intPtr2 = intPtr + (IntPtr)8).LessThanEqual(pointerSizeLength))
			{
				*Unsafe.Add<IntPtr>(ref ip, intPtr + (IntPtr)0) = 0;
				*Unsafe.Add<IntPtr>(ref ip, intPtr + (IntPtr)1) = 0;
				*Unsafe.Add<IntPtr>(ref ip, intPtr + (IntPtr)2) = 0;
				*Unsafe.Add<IntPtr>(ref ip, intPtr + (IntPtr)3) = 0;
				*Unsafe.Add<IntPtr>(ref ip, intPtr + (IntPtr)4) = 0;
				*Unsafe.Add<IntPtr>(ref ip, intPtr + (IntPtr)5) = 0;
				*Unsafe.Add<IntPtr>(ref ip, intPtr + (IntPtr)6) = 0;
				*Unsafe.Add<IntPtr>(ref ip, intPtr + (IntPtr)7) = 0;
				intPtr = intPtr2;
			}
			if ((intPtr2 = intPtr + (IntPtr)4).LessThanEqual(pointerSizeLength))
			{
				*Unsafe.Add<IntPtr>(ref ip, intPtr + (IntPtr)0) = 0;
				*Unsafe.Add<IntPtr>(ref ip, intPtr + (IntPtr)1) = 0;
				*Unsafe.Add<IntPtr>(ref ip, intPtr + (IntPtr)2) = 0;
				*Unsafe.Add<IntPtr>(ref ip, intPtr + (IntPtr)3) = 0;
				intPtr = intPtr2;
			}
			if ((intPtr2 = intPtr + (IntPtr)2).LessThanEqual(pointerSizeLength))
			{
				*Unsafe.Add<IntPtr>(ref ip, intPtr + (IntPtr)0) = 0;
				*Unsafe.Add<IntPtr>(ref ip, intPtr + (IntPtr)1) = 0;
				intPtr = intPtr2;
			}
			if ((intPtr + (IntPtr)1).LessThanEqual(pointerSizeLength))
			{
				*Unsafe.Add<IntPtr>(ref ip, intPtr) = 0;
			}
		}

		// Token: 0x06000110 RID: 272 RVA: 0x00007721 File Offset: 0x00005921
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static bool LessThanEqual(this IntPtr index, UIntPtr length)
		{
			if (sizeof(UIntPtr) != 4)
			{
				return (long)index <= (long)(ulong)length;
			}
			return (int)index <= (int)(uint)length;
		}

		// Token: 0x06000111 RID: 273 RVA: 0x00007750 File Offset: 0x00005950
		public static int IndexOf<[Nullable(0)] T>(ref T searchSpace, int searchSpaceLength, ref T value, int valueLength) where T : IEquatable<T>
		{
			if (valueLength == 0)
			{
				return 0;
			}
			T value2 = value;
			ref T second = ref Unsafe.Add<T>(ref value, 1);
			int num = valueLength - 1;
			int num2 = 0;
			for (;;)
			{
				int num3 = searchSpaceLength - num2 - num;
				if (num3 <= 0)
				{
					return -1;
				}
				int num4 = SpanHelpers.IndexOf<T>(Unsafe.Add<T>(ref searchSpace, num2), value2, num3);
				if (num4 == -1)
				{
					return -1;
				}
				num2 += num4;
				if (SpanHelpers.SequenceEqual<T>(Unsafe.Add<T>(ref searchSpace, num2 + 1), ref second, num))
				{
					break;
				}
				num2++;
			}
			return num2;
		}

		// Token: 0x06000112 RID: 274 RVA: 0x000077BC File Offset: 0x000059BC
		public unsafe static int IndexOf<[Nullable(0)] T>(ref T searchSpace, T value, int length) where T : IEquatable<T>
		{
			UIntPtr uintPtr = (UIntPtr)((IntPtr)0);
			while (length >= 8)
			{
				length -= 8;
				ref T ptr = ref value;
				if (default(T) == null)
				{
					T t = value;
					ptr = ref t;
				}
				if (ptr.Equals(*Unsafe.Add<T>(ref searchSpace, uintPtr)))
				{
					IL_312:
					return (int)uintPtr;
				}
				ref T ptr2 = ref value;
				if (default(T) == null)
				{
					T t = value;
					ptr2 = ref t;
				}
				if (ptr2.Equals(*Unsafe.Add<T>(ref searchSpace, uintPtr + (UIntPtr)((IntPtr)1))))
				{
					IL_315:
					return (int)(uintPtr + (UIntPtr)((IntPtr)1));
				}
				ref T ptr3 = ref value;
				if (default(T) == null)
				{
					T t = value;
					ptr3 = ref t;
				}
				if (ptr3.Equals(*Unsafe.Add<T>(ref searchSpace, uintPtr + (UIntPtr)((IntPtr)2))))
				{
					IL_31B:
					return (int)(uintPtr + (UIntPtr)((IntPtr)2));
				}
				ref T ptr4 = ref value;
				if (default(T) == null)
				{
					T t = value;
					ptr4 = ref t;
				}
				if (ptr4.Equals(*Unsafe.Add<T>(ref searchSpace, uintPtr + (UIntPtr)((IntPtr)3))))
				{
					IL_321:
					return (int)(uintPtr + (UIntPtr)((IntPtr)3));
				}
				ref T ptr5 = ref value;
				if (default(T) == null)
				{
					T t = value;
					ptr5 = ref t;
				}
				if (ptr5.Equals(*Unsafe.Add<T>(ref searchSpace, uintPtr + (UIntPtr)((IntPtr)4))))
				{
					return (int)(uintPtr + (UIntPtr)((IntPtr)4));
				}
				ref T ptr6 = ref value;
				if (default(T) == null)
				{
					T t = value;
					ptr6 = ref t;
				}
				if (ptr6.Equals(*Unsafe.Add<T>(ref searchSpace, uintPtr + (UIntPtr)((IntPtr)5))))
				{
					return (int)(uintPtr + (UIntPtr)((IntPtr)5));
				}
				ref T ptr7 = ref value;
				if (default(T) == null)
				{
					T t = value;
					ptr7 = ref t;
				}
				if (ptr7.Equals(*Unsafe.Add<T>(ref searchSpace, uintPtr + (UIntPtr)((IntPtr)6))))
				{
					return (int)(uintPtr + (UIntPtr)((IntPtr)6));
				}
				ref T ptr8 = ref value;
				if (default(T) == null)
				{
					T t = value;
					ptr8 = ref t;
				}
				if (ptr8.Equals(*Unsafe.Add<T>(ref searchSpace, uintPtr + (UIntPtr)((IntPtr)7))))
				{
					return (int)(uintPtr + (UIntPtr)((IntPtr)7));
				}
				uintPtr += (UIntPtr)((IntPtr)8);
			}
			if (length >= 4)
			{
				length -= 4;
				ref T ptr9 = ref value;
				if (default(T) == null)
				{
					T t = value;
					ptr9 = ref t;
				}
				if (ptr9.Equals(*Unsafe.Add<T>(ref searchSpace, uintPtr)))
				{
					goto IL_312;
				}
				ref T ptr10 = ref value;
				if (default(T) == null)
				{
					T t = value;
					ptr10 = ref t;
				}
				if (ptr10.Equals(*Unsafe.Add<T>(ref searchSpace, uintPtr + (UIntPtr)((IntPtr)1))))
				{
					goto IL_315;
				}
				ref T ptr11 = ref value;
				if (default(T) == null)
				{
					T t = value;
					ptr11 = ref t;
				}
				if (ptr11.Equals(*Unsafe.Add<T>(ref searchSpace, uintPtr + (UIntPtr)((IntPtr)2))))
				{
					goto IL_31B;
				}
				ref T ptr12 = ref value;
				if (default(T) == null)
				{
					T t = value;
					ptr12 = ref t;
				}
				if (ptr12.Equals(*Unsafe.Add<T>(ref searchSpace, uintPtr + (UIntPtr)((IntPtr)3))))
				{
					goto IL_321;
				}
				uintPtr += (UIntPtr)((IntPtr)4);
			}
			while (length > 0)
			{
				ref T ptr13 = ref value;
				if (default(T) == null)
				{
					T t = value;
					ptr13 = ref t;
				}
				if (ptr13.Equals(*Unsafe.Add<T>(ref searchSpace, uintPtr)))
				{
					goto IL_312;
				}
				uintPtr += (UIntPtr)((IntPtr)1);
				length--;
			}
			return -1;
		}

		// Token: 0x06000113 RID: 275 RVA: 0x00007B08 File Offset: 0x00005D08
		public unsafe static int IndexOfAny<[Nullable(0)] T>(ref T searchSpace, T value0, T value1, int length) where T : IEquatable<T>
		{
			int i = 0;
			while (length - i >= 8)
			{
				T other = *Unsafe.Add<T>(ref searchSpace, i);
				if (value0.Equals(other) || value1.Equals(other))
				{
					return i;
				}
				other = *Unsafe.Add<T>(ref searchSpace, i + 1);
				if (value0.Equals(other) || value1.Equals(other))
				{
					IL_2CB:
					return i + 1;
				}
				other = *Unsafe.Add<T>(ref searchSpace, i + 2);
				if (value0.Equals(other) || value1.Equals(other))
				{
					IL_2CF:
					return i + 2;
				}
				other = *Unsafe.Add<T>(ref searchSpace, i + 3);
				if (value0.Equals(other) || value1.Equals(other))
				{
					IL_2D3:
					return i + 3;
				}
				other = *Unsafe.Add<T>(ref searchSpace, i + 4);
				if (value0.Equals(other) || value1.Equals(other))
				{
					return i + 4;
				}
				other = *Unsafe.Add<T>(ref searchSpace, i + 5);
				if (value0.Equals(other) || value1.Equals(other))
				{
					return i + 5;
				}
				other = *Unsafe.Add<T>(ref searchSpace, i + 6);
				if (value0.Equals(other) || value1.Equals(other))
				{
					return i + 6;
				}
				other = *Unsafe.Add<T>(ref searchSpace, i + 7);
				if (value0.Equals(other) || value1.Equals(other))
				{
					return i + 7;
				}
				i += 8;
			}
			if (length - i >= 4)
			{
				T other = *Unsafe.Add<T>(ref searchSpace, i);
				if (value0.Equals(other) || value1.Equals(other))
				{
					return i;
				}
				other = *Unsafe.Add<T>(ref searchSpace, i + 1);
				if (value0.Equals(other) || value1.Equals(other))
				{
					goto IL_2CB;
				}
				other = *Unsafe.Add<T>(ref searchSpace, i + 2);
				if (value0.Equals(other) || value1.Equals(other))
				{
					goto IL_2CF;
				}
				other = *Unsafe.Add<T>(ref searchSpace, i + 3);
				if (value0.Equals(other) || value1.Equals(other))
				{
					goto IL_2D3;
				}
				i += 4;
			}
			while (i < length)
			{
				T other = *Unsafe.Add<T>(ref searchSpace, i);
				if (value0.Equals(other) || value1.Equals(other))
				{
					return i;
				}
				i++;
			}
			return -1;
		}

		// Token: 0x06000114 RID: 276 RVA: 0x00007DFC File Offset: 0x00005FFC
		public unsafe static int IndexOfAny<[Nullable(0)] T>(ref T searchSpace, T value0, T value1, T value2, int length) where T : IEquatable<T>
		{
			int i = 0;
			while (length - i >= 8)
			{
				T other = *Unsafe.Add<T>(ref searchSpace, i);
				if (value0.Equals(other) || value1.Equals(other) || value2.Equals(other))
				{
					return i;
				}
				other = *Unsafe.Add<T>(ref searchSpace, i + 1);
				if (value0.Equals(other) || value1.Equals(other) || value2.Equals(other))
				{
					IL_3C2:
					return i + 1;
				}
				other = *Unsafe.Add<T>(ref searchSpace, i + 2);
				if (value0.Equals(other) || value1.Equals(other) || value2.Equals(other))
				{
					IL_3C6:
					return i + 2;
				}
				other = *Unsafe.Add<T>(ref searchSpace, i + 3);
				if (value0.Equals(other) || value1.Equals(other) || value2.Equals(other))
				{
					IL_3CA:
					return i + 3;
				}
				other = *Unsafe.Add<T>(ref searchSpace, i + 4);
				if (value0.Equals(other) || value1.Equals(other) || value2.Equals(other))
				{
					return i + 4;
				}
				other = *Unsafe.Add<T>(ref searchSpace, i + 5);
				if (value0.Equals(other) || value1.Equals(other) || value2.Equals(other))
				{
					return i + 5;
				}
				other = *Unsafe.Add<T>(ref searchSpace, i + 6);
				if (value0.Equals(other) || value1.Equals(other) || value2.Equals(other))
				{
					return i + 6;
				}
				other = *Unsafe.Add<T>(ref searchSpace, i + 7);
				if (value0.Equals(other) || value1.Equals(other) || value2.Equals(other))
				{
					return i + 7;
				}
				i += 8;
			}
			if (length - i >= 4)
			{
				T other = *Unsafe.Add<T>(ref searchSpace, i);
				if (value0.Equals(other) || value1.Equals(other) || value2.Equals(other))
				{
					return i;
				}
				other = *Unsafe.Add<T>(ref searchSpace, i + 1);
				if (value0.Equals(other) || value1.Equals(other) || value2.Equals(other))
				{
					goto IL_3C2;
				}
				other = *Unsafe.Add<T>(ref searchSpace, i + 2);
				if (value0.Equals(other) || value1.Equals(other) || value2.Equals(other))
				{
					goto IL_3C6;
				}
				other = *Unsafe.Add<T>(ref searchSpace, i + 3);
				if (value0.Equals(other) || value1.Equals(other) || value2.Equals(other))
				{
					goto IL_3CA;
				}
				i += 4;
			}
			while (i < length)
			{
				T other = *Unsafe.Add<T>(ref searchSpace, i);
				if (value0.Equals(other) || value1.Equals(other) || value2.Equals(other))
				{
					return i;
				}
				i++;
			}
			return -1;
		}

		// Token: 0x06000115 RID: 277 RVA: 0x000081E8 File Offset: 0x000063E8
		public unsafe static int IndexOfAny<[Nullable(0)] T>(ref T searchSpace, int searchSpaceLength, ref T value, int valueLength) where T : IEquatable<T>
		{
			if (valueLength == 0)
			{
				return 0;
			}
			int num = -1;
			for (int i = 0; i < valueLength; i++)
			{
				int num2 = SpanHelpers.IndexOf<T>(ref searchSpace, *Unsafe.Add<T>(ref value, i), searchSpaceLength);
				if (num2 < num)
				{
					num = num2;
					searchSpaceLength = num2;
					if (num == 0)
					{
						break;
					}
				}
			}
			return num;
		}

		// Token: 0x06000116 RID: 278 RVA: 0x0000822C File Offset: 0x0000642C
		public static int LastIndexOf<[Nullable(0)] T>(ref T searchSpace, int searchSpaceLength, ref T value, int valueLength) where T : IEquatable<T>
		{
			if (valueLength == 0)
			{
				return 0;
			}
			T value2 = value;
			ref T second = ref Unsafe.Add<T>(ref value, 1);
			int num = valueLength - 1;
			int num2 = 0;
			int num4;
			for (;;)
			{
				int num3 = searchSpaceLength - num2 - num;
				if (num3 <= 0)
				{
					return -1;
				}
				num4 = SpanHelpers.LastIndexOf<T>(ref searchSpace, value2, num3);
				if (num4 == -1)
				{
					return -1;
				}
				if (SpanHelpers.SequenceEqual<T>(Unsafe.Add<T>(ref searchSpace, num4 + 1), ref second, num))
				{
					break;
				}
				num2 += num3 - num4;
			}
			return num4;
		}

		// Token: 0x06000117 RID: 279 RVA: 0x00008290 File Offset: 0x00006490
		public unsafe static int LastIndexOf<[Nullable(0)] T>(ref T searchSpace, T value, int length) where T : IEquatable<T>
		{
			while (length >= 8)
			{
				length -= 8;
				ref T ptr = ref value;
				if (default(T) == null)
				{
					T t = value;
					ptr = ref t;
				}
				if (ptr.Equals(*Unsafe.Add<T>(ref searchSpace, length + 7)))
				{
					return length + 7;
				}
				ref T ptr2 = ref value;
				if (default(T) == null)
				{
					T t = value;
					ptr2 = ref t;
				}
				if (ptr2.Equals(*Unsafe.Add<T>(ref searchSpace, length + 6)))
				{
					return length + 6;
				}
				ref T ptr3 = ref value;
				if (default(T) == null)
				{
					T t = value;
					ptr3 = ref t;
				}
				if (ptr3.Equals(*Unsafe.Add<T>(ref searchSpace, length + 5)))
				{
					return length + 5;
				}
				ref T ptr4 = ref value;
				if (default(T) == null)
				{
					T t = value;
					ptr4 = ref t;
				}
				if (ptr4.Equals(*Unsafe.Add<T>(ref searchSpace, length + 4)))
				{
					return length + 4;
				}
				ref T ptr5 = ref value;
				if (default(T) == null)
				{
					T t = value;
					ptr5 = ref t;
				}
				if (ptr5.Equals(*Unsafe.Add<T>(ref searchSpace, length + 3)))
				{
					IL_2FD:
					return length + 3;
				}
				ref T ptr6 = ref value;
				if (default(T) == null)
				{
					T t = value;
					ptr6 = ref t;
				}
				if (ptr6.Equals(*Unsafe.Add<T>(ref searchSpace, length + 2)))
				{
					IL_2F9:
					return length + 2;
				}
				ref T ptr7 = ref value;
				if (default(T) == null)
				{
					T t = value;
					ptr7 = ref t;
				}
				if (ptr7.Equals(*Unsafe.Add<T>(ref searchSpace, length + 1)))
				{
					IL_2F5:
					return length + 1;
				}
				ref T ptr8 = ref value;
				if (default(T) == null)
				{
					T t = value;
					ptr8 = ref t;
				}
				if (ptr8.Equals(*Unsafe.Add<T>(ref searchSpace, length)))
				{
					return length;
				}
			}
			if (length >= 4)
			{
				length -= 4;
				ref T ptr9 = ref value;
				if (default(T) == null)
				{
					T t = value;
					ptr9 = ref t;
				}
				if (ptr9.Equals(*Unsafe.Add<T>(ref searchSpace, length + 3)))
				{
					goto IL_2FD;
				}
				ref T ptr10 = ref value;
				if (default(T) == null)
				{
					T t = value;
					ptr10 = ref t;
				}
				if (ptr10.Equals(*Unsafe.Add<T>(ref searchSpace, length + 2)))
				{
					goto IL_2F9;
				}
				ref T ptr11 = ref value;
				if (default(T) == null)
				{
					T t = value;
					ptr11 = ref t;
				}
				if (ptr11.Equals(*Unsafe.Add<T>(ref searchSpace, length + 1)))
				{
					goto IL_2F5;
				}
				ref T ptr12 = ref value;
				if (default(T) == null)
				{
					T t = value;
					ptr12 = ref t;
				}
				if (ptr12.Equals(*Unsafe.Add<T>(ref searchSpace, length)))
				{
					return length;
				}
			}
			while (length > 0)
			{
				length--;
				ref T ptr13 = ref value;
				if (default(T) == null)
				{
					T t = value;
					ptr13 = ref t;
				}
				if (ptr13.Equals(*Unsafe.Add<T>(ref searchSpace, length)))
				{
					return length;
				}
			}
			return -1;
		}

		// Token: 0x06000118 RID: 280 RVA: 0x000085B0 File Offset: 0x000067B0
		public unsafe static int LastIndexOfAny<[Nullable(0)] T>(ref T searchSpace, T value0, T value1, int length) where T : IEquatable<T>
		{
			while (length >= 8)
			{
				length -= 8;
				T other = *Unsafe.Add<T>(ref searchSpace, length + 7);
				if (value0.Equals(other) || value1.Equals(other))
				{
					return length + 7;
				}
				other = *Unsafe.Add<T>(ref searchSpace, length + 6);
				if (value0.Equals(other) || value1.Equals(other))
				{
					return length + 6;
				}
				other = *Unsafe.Add<T>(ref searchSpace, length + 5);
				if (value0.Equals(other) || value1.Equals(other))
				{
					return length + 5;
				}
				other = *Unsafe.Add<T>(ref searchSpace, length + 4);
				if (value0.Equals(other) || value1.Equals(other))
				{
					return length + 4;
				}
				other = *Unsafe.Add<T>(ref searchSpace, length + 3);
				if (value0.Equals(other) || value1.Equals(other))
				{
					IL_2CD:
					return length + 3;
				}
				other = *Unsafe.Add<T>(ref searchSpace, length + 2);
				if (value0.Equals(other) || value1.Equals(other))
				{
					IL_2C9:
					return length + 2;
				}
				other = *Unsafe.Add<T>(ref searchSpace, length + 1);
				if (value0.Equals(other) || value1.Equals(other))
				{
					IL_2C5:
					return length + 1;
				}
				other = *Unsafe.Add<T>(ref searchSpace, length);
				if (value0.Equals(other) || value1.Equals(other))
				{
					return length;
				}
			}
			if (length >= 4)
			{
				length -= 4;
				T other = *Unsafe.Add<T>(ref searchSpace, length + 3);
				if (value0.Equals(other) || value1.Equals(other))
				{
					goto IL_2CD;
				}
				other = *Unsafe.Add<T>(ref searchSpace, length + 2);
				if (value0.Equals(other) || value1.Equals(other))
				{
					goto IL_2C9;
				}
				other = *Unsafe.Add<T>(ref searchSpace, length + 1);
				if (value0.Equals(other) || value1.Equals(other))
				{
					goto IL_2C5;
				}
				other = *Unsafe.Add<T>(ref searchSpace, length);
				if (value0.Equals(other))
				{
					return length;
				}
				if (value1.Equals(other))
				{
					return length;
				}
			}
			while (length > 0)
			{
				length--;
				T other = *Unsafe.Add<T>(ref searchSpace, length);
				if (value0.Equals(other) || value1.Equals(other))
				{
					return length;
				}
			}
			return -1;
		}

		// Token: 0x06000119 RID: 281 RVA: 0x000088A0 File Offset: 0x00006AA0
		public unsafe static int LastIndexOfAny<[Nullable(0)] T>(ref T searchSpace, T value0, T value1, T value2, int length) where T : IEquatable<T>
		{
			while (length >= 8)
			{
				length -= 8;
				T other = *Unsafe.Add<T>(ref searchSpace, length + 7);
				if (value0.Equals(other) || value1.Equals(other) || value2.Equals(other))
				{
					return length + 7;
				}
				other = *Unsafe.Add<T>(ref searchSpace, length + 6);
				if (value0.Equals(other) || value1.Equals(other) || value2.Equals(other))
				{
					return length + 6;
				}
				other = *Unsafe.Add<T>(ref searchSpace, length + 5);
				if (value0.Equals(other) || value1.Equals(other) || value2.Equals(other))
				{
					return length + 5;
				}
				other = *Unsafe.Add<T>(ref searchSpace, length + 4);
				if (value0.Equals(other) || value1.Equals(other) || value2.Equals(other))
				{
					return length + 4;
				}
				other = *Unsafe.Add<T>(ref searchSpace, length + 3);
				if (value0.Equals(other) || value1.Equals(other) || value2.Equals(other))
				{
					IL_3DA:
					return length + 3;
				}
				other = *Unsafe.Add<T>(ref searchSpace, length + 2);
				if (value0.Equals(other) || value1.Equals(other) || value2.Equals(other))
				{
					IL_3D5:
					return length + 2;
				}
				other = *Unsafe.Add<T>(ref searchSpace, length + 1);
				if (value0.Equals(other) || value1.Equals(other) || value2.Equals(other))
				{
					IL_3D0:
					return length + 1;
				}
				other = *Unsafe.Add<T>(ref searchSpace, length);
				if (value0.Equals(other) || value1.Equals(other) || value2.Equals(other))
				{
					return length;
				}
			}
			if (length >= 4)
			{
				length -= 4;
				T other = *Unsafe.Add<T>(ref searchSpace, length + 3);
				if (value0.Equals(other) || value1.Equals(other) || value2.Equals(other))
				{
					goto IL_3DA;
				}
				other = *Unsafe.Add<T>(ref searchSpace, length + 2);
				if (value0.Equals(other) || value1.Equals(other) || value2.Equals(other))
				{
					goto IL_3D5;
				}
				other = *Unsafe.Add<T>(ref searchSpace, length + 1);
				if (value0.Equals(other) || value1.Equals(other) || value2.Equals(other))
				{
					goto IL_3D0;
				}
				other = *Unsafe.Add<T>(ref searchSpace, length);
				if (value0.Equals(other) || value1.Equals(other))
				{
					return length;
				}
				if (value2.Equals(other))
				{
					return length;
				}
			}
			while (length > 0)
			{
				length--;
				T other = *Unsafe.Add<T>(ref searchSpace, length);
				if (value0.Equals(other) || value1.Equals(other) || value2.Equals(other))
				{
					return length;
				}
			}
			return -1;
		}

		// Token: 0x0600011A RID: 282 RVA: 0x00008CA0 File Offset: 0x00006EA0
		public unsafe static int LastIndexOfAny<[Nullable(0)] T>(ref T searchSpace, int searchSpaceLength, ref T value, int valueLength) where T : IEquatable<T>
		{
			if (valueLength == 0)
			{
				return 0;
			}
			int num = -1;
			for (int i = 0; i < valueLength; i++)
			{
				int num2 = SpanHelpers.LastIndexOf<T>(ref searchSpace, *Unsafe.Add<T>(ref value, i), searchSpaceLength);
				if (num2 > num)
				{
					num = num2;
				}
			}
			return num;
		}

		// Token: 0x0600011B RID: 283 RVA: 0x00008CDC File Offset: 0x00006EDC
		public unsafe static bool SequenceEqual<[Nullable(0)] T>(ref T first, ref T second, int length) where T : IEquatable<T>
		{
			if (!Unsafe.AreSame<T>(ref first, ref second))
			{
				UIntPtr uintPtr = (UIntPtr)((IntPtr)0);
				while (length >= 8)
				{
					length -= 8;
					T ptr2;
					ref T ptr = (ptr2 = Unsafe.Add<T>(ref first, uintPtr));
					if (default(T) == null)
					{
						T t = ptr;
						ptr2 = ref t;
					}
					if (ptr2.Equals(*Unsafe.Add<T>(ref second, uintPtr)))
					{
						T ptr4;
						ref T ptr3 = (ptr4 = Unsafe.Add<T>(ref first, uintPtr + (UIntPtr)((IntPtr)1)));
						if (default(T) == null)
						{
							T t = ptr3;
							ptr4 = ref t;
						}
						if (ptr4.Equals(*Unsafe.Add<T>(ref second, uintPtr + (UIntPtr)((IntPtr)1))))
						{
							T ptr6;
							ref T ptr5 = (ptr6 = Unsafe.Add<T>(ref first, uintPtr + (UIntPtr)((IntPtr)2)));
							if (default(T) == null)
							{
								T t = ptr5;
								ptr6 = ref t;
							}
							if (ptr6.Equals(*Unsafe.Add<T>(ref second, uintPtr + (UIntPtr)((IntPtr)2))))
							{
								T ptr8;
								ref T ptr7 = (ptr8 = Unsafe.Add<T>(ref first, uintPtr + (UIntPtr)((IntPtr)3)));
								if (default(T) == null)
								{
									T t = ptr7;
									ptr8 = ref t;
								}
								if (ptr8.Equals(*Unsafe.Add<T>(ref second, uintPtr + (UIntPtr)((IntPtr)3))))
								{
									T ptr10;
									ref T ptr9 = (ptr10 = Unsafe.Add<T>(ref first, uintPtr + (UIntPtr)((IntPtr)4)));
									if (default(T) == null)
									{
										T t = ptr9;
										ptr10 = ref t;
									}
									if (ptr10.Equals(*Unsafe.Add<T>(ref second, uintPtr + (UIntPtr)((IntPtr)4))))
									{
										T ptr12;
										ref T ptr11 = (ptr12 = Unsafe.Add<T>(ref first, uintPtr + (UIntPtr)((IntPtr)5)));
										if (default(T) == null)
										{
											T t = ptr11;
											ptr12 = ref t;
										}
										if (ptr12.Equals(*Unsafe.Add<T>(ref second, uintPtr + (UIntPtr)((IntPtr)5))))
										{
											T ptr14;
											ref T ptr13 = (ptr14 = Unsafe.Add<T>(ref first, uintPtr + (UIntPtr)((IntPtr)6)));
											if (default(T) == null)
											{
												T t = ptr13;
												ptr14 = ref t;
											}
											if (ptr14.Equals(*Unsafe.Add<T>(ref second, uintPtr + (UIntPtr)((IntPtr)6))))
											{
												T ptr16;
												ref T ptr15 = (ptr16 = Unsafe.Add<T>(ref first, uintPtr + (UIntPtr)((IntPtr)7)));
												if (default(T) == null)
												{
													T t = ptr15;
													ptr16 = ref t;
												}
												if (ptr16.Equals(*Unsafe.Add<T>(ref second, uintPtr + (UIntPtr)((IntPtr)7))))
												{
													uintPtr += (UIntPtr)((IntPtr)8);
													continue;
												}
											}
										}
									}
								}
							}
						}
					}
					return false;
				}
				if (length >= 4)
				{
					length -= 4;
					T ptr18;
					ref T ptr17 = (ptr18 = Unsafe.Add<T>(ref first, uintPtr));
					if (default(T) == null)
					{
						T t = ptr17;
						ptr18 = ref t;
					}
					if (!ptr18.Equals(*Unsafe.Add<T>(ref second, uintPtr)))
					{
						return false;
					}
					T ptr20;
					ref T ptr19 = (ptr20 = Unsafe.Add<T>(ref first, uintPtr + (UIntPtr)((IntPtr)1)));
					if (default(T) == null)
					{
						T t = ptr19;
						ptr20 = ref t;
					}
					if (!ptr20.Equals(*Unsafe.Add<T>(ref second, uintPtr + (UIntPtr)((IntPtr)1))))
					{
						return false;
					}
					T ptr22;
					ref T ptr21 = (ptr22 = Unsafe.Add<T>(ref first, uintPtr + (UIntPtr)((IntPtr)2)));
					if (default(T) == null)
					{
						T t = ptr21;
						ptr22 = ref t;
					}
					if (!ptr22.Equals(*Unsafe.Add<T>(ref second, uintPtr + (UIntPtr)((IntPtr)2))))
					{
						return false;
					}
					T ptr24;
					ref T ptr23 = (ptr24 = Unsafe.Add<T>(ref first, uintPtr + (UIntPtr)((IntPtr)3)));
					if (default(T) == null)
					{
						T t = ptr23;
						ptr24 = ref t;
					}
					if (!ptr24.Equals(*Unsafe.Add<T>(ref second, uintPtr + (UIntPtr)((IntPtr)3))))
					{
						return false;
					}
					uintPtr += (UIntPtr)((IntPtr)4);
				}
				while (length > 0)
				{
					T ptr26;
					ref T ptr25 = (ptr26 = Unsafe.Add<T>(ref first, uintPtr));
					if (default(T) == null)
					{
						T t = ptr25;
						ptr26 = ref t;
					}
					if (!ptr26.Equals(*Unsafe.Add<T>(ref second, uintPtr)))
					{
						return false;
					}
					uintPtr += (UIntPtr)((IntPtr)1);
					length--;
				}
			}
			return true;
		}

		// Token: 0x0600011C RID: 284 RVA: 0x00009068 File Offset: 0x00007268
		public unsafe static int SequenceCompareTo<[Nullable(0)] T>(ref T first, int firstLength, ref T second, int secondLength) where T : IComparable<T>
		{
			int num = firstLength;
			if (num > secondLength)
			{
				num = secondLength;
			}
			for (int i = 0; i < num; i++)
			{
				T ptr2;
				ref T ptr = (ptr2 = Unsafe.Add<T>(ref first, i));
				if (default(T) == null)
				{
					T t = ptr;
					ptr2 = ref t;
				}
				int num2 = ptr2.CompareTo(*Unsafe.Add<T>(ref second, i));
				if (num2 != 0)
				{
					return num2;
				}
			}
			return firstLength.CompareTo(secondLength);
		}

		// Token: 0x0200005A RID: 90
		[Nullable(0)]
		internal struct ComparerComparable<[Nullable(2)] T, [Nullable(0)] TComparer> : IComparable<T> where TComparer : IComparer<T>
		{
			// Token: 0x060002C2 RID: 706 RVA: 0x0000CD5D File Offset: 0x0000AF5D
			public ComparerComparable(T value, TComparer comparer)
			{
				this._value = value;
				this._comparer = comparer;
			}

			// Token: 0x060002C3 RID: 707 RVA: 0x0000CD70 File Offset: 0x0000AF70
			[NullableContext(2)]
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public int CompareTo(T other)
			{
				TComparer comparer = this._comparer;
				return comparer.Compare(this._value, other);
			}

			// Token: 0x040000A5 RID: 165
			private readonly T _value;

			// Token: 0x040000A6 RID: 166
			private readonly TComparer _comparer;
		}

		// Token: 0x0200005B RID: 91
		[NullableContext(0)]
		private struct Reg64
		{
		}

		// Token: 0x0200005C RID: 92
		[NullableContext(0)]
		private struct Reg32
		{
		}

		// Token: 0x0200005D RID: 93
		[NullableContext(0)]
		private struct Reg16
		{
		}

		// Token: 0x0200005E RID: 94
		[NullableContext(0)]
		public static class PerTypeValues<[Nullable(2)] T>
		{
			// Token: 0x060002C4 RID: 708 RVA: 0x0000CD98 File Offset: 0x0000AF98
			private static IntPtr MeasureArrayAdjustment()
			{
				T[] array = new T[1];
				return Unsafe.ByteOffset<T>(ILHelpers.ObjectAsRef<T>(array), ref array[0]);
			}

			// Token: 0x040000A7 RID: 167
			public static readonly bool IsReferenceOrContainsReferences = SpanHelpers.IsReferenceOrContainsReferencesCore(typeof(T));

			// Token: 0x040000A8 RID: 168
			[Nullable(1)]
			public static readonly T[] EmptyArray = ArrayEx.Empty<T>();

			// Token: 0x040000A9 RID: 169
			public static readonly IntPtr ArrayAdjustment = SpanHelpers.PerTypeValues<T>.MeasureArrayAdjustment();
		}
	}
}
