using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using MonoMod;

namespace System
{
	// Token: 0x02000477 RID: 1143
	[NullableContext(1)]
	[Nullable(0)]
	internal static class SpanHelpers
	{
		// Token: 0x06001944 RID: 6468 RVA: 0x000510BB File Offset: 0x0004F2BB
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int BinarySearch<[Nullable(2)] T, [Nullable(0)] TComparable>([Nullable(new byte[] { 0, 1 })] this ReadOnlySpan<T> span, TComparable comparable) where TComparable : IComparable<T>
		{
			if (comparable == null)
			{
				ThrowHelper.ThrowArgumentNullException(ExceptionArgument.comparable);
			}
			return SpanHelpers.BinarySearch<T, TComparable>(MemoryMarshal.GetReference<T>(span), span.Length, comparable);
		}

		// Token: 0x06001945 RID: 6469 RVA: 0x000510E0 File Offset: 0x0004F2E0
		public unsafe static int BinarySearch<[Nullable(2)] T, [Nullable(0)] TComparable>(ref T spanStart, int length, TComparable comparable) where TComparable : IComparable<T>
		{
			int lo = 0;
			int hi = length - 1;
			while (lo <= hi)
			{
				int i = (int)((uint)(hi + lo) >> 1);
				ref TComparable ptr = ref comparable;
				if (default(TComparable) == null)
				{
					TComparable tcomparable = comparable;
					ptr = ref tcomparable;
				}
				int c = ptr.CompareTo(*Unsafe.Add<T>(ref spanStart, i));
				if (c == 0)
				{
					return i;
				}
				if (c > 0)
				{
					lo = i + 1;
				}
				else
				{
					hi = i - 1;
				}
			}
			return ~lo;
		}

		// Token: 0x06001946 RID: 6470 RVA: 0x00051148 File Offset: 0x0004F348
		public static int IndexOf(ref byte searchSpace, int searchSpaceLength, ref byte value, int valueLength)
		{
			if (valueLength == 0)
			{
				return 0;
			}
			byte valueHead = value;
			ref byte valueTail = ref Unsafe.Add<byte>(ref value, 1);
			int valueTailLength = valueLength - 1;
			int index = 0;
			for (;;)
			{
				int remainingSearchSpaceLength = searchSpaceLength - index - valueTailLength;
				if (remainingSearchSpaceLength <= 0)
				{
					return -1;
				}
				int relativeIndex = SpanHelpers.IndexOf(Unsafe.Add<byte>(ref searchSpace, index), valueHead, remainingSearchSpaceLength);
				if (relativeIndex == -1)
				{
					return -1;
				}
				index += relativeIndex;
				if (SpanHelpers.SequenceEqual<byte>(Unsafe.Add<byte>(ref searchSpace, index + 1), ref valueTail, valueTailLength))
				{
					break;
				}
				index++;
			}
			return index;
		}

		// Token: 0x06001947 RID: 6471 RVA: 0x000511B0 File Offset: 0x0004F3B0
		public unsafe static int IndexOfAny(ref byte searchSpace, int searchSpaceLength, ref byte value, int valueLength)
		{
			if (valueLength == 0)
			{
				return 0;
			}
			int index = -1;
			for (int i = 0; i < valueLength; i++)
			{
				int tempIndex = SpanHelpers.IndexOf(ref searchSpace, *Unsafe.Add<byte>(ref value, i), searchSpaceLength);
				if (tempIndex < index)
				{
					index = tempIndex;
					searchSpaceLength = tempIndex;
					if (index == 0)
					{
						break;
					}
				}
			}
			return index;
		}

		// Token: 0x06001948 RID: 6472 RVA: 0x000511F0 File Offset: 0x0004F3F0
		public unsafe static int LastIndexOfAny(ref byte searchSpace, int searchSpaceLength, ref byte value, int valueLength)
		{
			if (valueLength == 0)
			{
				return 0;
			}
			int index = -1;
			for (int i = 0; i < valueLength; i++)
			{
				int tempIndex = SpanHelpers.LastIndexOf(ref searchSpace, *Unsafe.Add<byte>(ref value, i), searchSpaceLength);
				if (tempIndex > index)
				{
					index = tempIndex;
				}
			}
			return index;
		}

		// Token: 0x06001949 RID: 6473 RVA: 0x00051228 File Offset: 0x0004F428
		public unsafe static int IndexOf(ref byte searchSpace, byte value, int length)
		{
			IntPtr index = (IntPtr)0;
			IntPtr nLength = (IntPtr)length;
			while (nLength >= (IntPtr)8)
			{
				nLength -= (IntPtr)8;
				if (value == *Unsafe.AddByteOffset<byte>(ref searchSpace, index))
				{
					IL_106:
					return (int)index;
				}
				if (value == *Unsafe.AddByteOffset<byte>(ref searchSpace, index + (IntPtr)1))
				{
					IL_109:
					return (int)(index + (IntPtr)1);
				}
				if (value == *Unsafe.AddByteOffset<byte>(ref searchSpace, index + (IntPtr)2))
				{
					IL_10F:
					return (int)(index + (IntPtr)2);
				}
				if (value == *Unsafe.AddByteOffset<byte>(ref searchSpace, index + (IntPtr)3))
				{
					IL_115:
					return (int)(index + (IntPtr)3);
				}
				if (value == *Unsafe.AddByteOffset<byte>(ref searchSpace, index + (IntPtr)4))
				{
					return (int)(index + (IntPtr)4);
				}
				if (value == *Unsafe.AddByteOffset<byte>(ref searchSpace, index + (IntPtr)5))
				{
					return (int)(index + (IntPtr)5);
				}
				if (value == *Unsafe.AddByteOffset<byte>(ref searchSpace, index + (IntPtr)6))
				{
					return (int)(index + (IntPtr)6);
				}
				if (value == *Unsafe.AddByteOffset<byte>(ref searchSpace, index + (IntPtr)7))
				{
					return (int)(index + (IntPtr)7);
				}
				index += (IntPtr)8;
			}
			if (nLength >= (IntPtr)4)
			{
				nLength -= (IntPtr)4;
				if (value == *Unsafe.AddByteOffset<byte>(ref searchSpace, index))
				{
					goto IL_106;
				}
				if (value == *Unsafe.AddByteOffset<byte>(ref searchSpace, index + (IntPtr)1))
				{
					goto IL_109;
				}
				if (value == *Unsafe.AddByteOffset<byte>(ref searchSpace, index + (IntPtr)2))
				{
					goto IL_10F;
				}
				if (value == *Unsafe.AddByteOffset<byte>(ref searchSpace, index + (IntPtr)3))
				{
					goto IL_115;
				}
				index += (IntPtr)4;
			}
			while (nLength > (IntPtr)0)
			{
				nLength -= (IntPtr)1;
				if (value == *Unsafe.AddByteOffset<byte>(ref searchSpace, index))
				{
					goto IL_106;
				}
				index += (IntPtr)1;
			}
			return -1;
		}

		// Token: 0x0600194A RID: 6474 RVA: 0x00051368 File Offset: 0x0004F568
		public static int LastIndexOf(ref byte searchSpace, int searchSpaceLength, ref byte value, int valueLength)
		{
			if (valueLength == 0)
			{
				return 0;
			}
			byte valueHead = value;
			ref byte valueTail = ref Unsafe.Add<byte>(ref value, 1);
			int valueTailLength = valueLength - 1;
			int index = 0;
			int relativeIndex;
			for (;;)
			{
				int remainingSearchSpaceLength = searchSpaceLength - index - valueTailLength;
				if (remainingSearchSpaceLength <= 0)
				{
					return -1;
				}
				relativeIndex = SpanHelpers.LastIndexOf(ref searchSpace, valueHead, remainingSearchSpaceLength);
				if (relativeIndex == -1)
				{
					return -1;
				}
				if (SpanHelpers.SequenceEqual<byte>(Unsafe.Add<byte>(ref searchSpace, relativeIndex + 1), ref valueTail, valueTailLength))
				{
					break;
				}
				index += remainingSearchSpaceLength - relativeIndex;
			}
			return relativeIndex;
		}

		// Token: 0x0600194B RID: 6475 RVA: 0x000513C8 File Offset: 0x0004F5C8
		public unsafe static int LastIndexOf(ref byte searchSpace, byte value, int length)
		{
			IntPtr index = (IntPtr)length;
			IntPtr nLength = (IntPtr)length;
			while (nLength >= (IntPtr)8)
			{
				nLength -= (IntPtr)8;
				index -= (IntPtr)8;
				if (value == *Unsafe.AddByteOffset<byte>(ref searchSpace, index + (IntPtr)7))
				{
					return (int)(index + (IntPtr)7);
				}
				if (value == *Unsafe.AddByteOffset<byte>(ref searchSpace, index + (IntPtr)6))
				{
					return (int)(index + (IntPtr)6);
				}
				if (value == *Unsafe.AddByteOffset<byte>(ref searchSpace, index + (IntPtr)5))
				{
					return (int)(index + (IntPtr)5);
				}
				if (value == *Unsafe.AddByteOffset<byte>(ref searchSpace, index + (IntPtr)4))
				{
					return (int)(index + (IntPtr)4);
				}
				if (value == *Unsafe.AddByteOffset<byte>(ref searchSpace, index + (IntPtr)3))
				{
					IL_10F:
					return (int)(index + (IntPtr)3);
				}
				if (value == *Unsafe.AddByteOffset<byte>(ref searchSpace, index + (IntPtr)2))
				{
					IL_109:
					return (int)(index + (IntPtr)2);
				}
				if (value == *Unsafe.AddByteOffset<byte>(ref searchSpace, index + (IntPtr)1))
				{
					IL_103:
					return (int)(index + (IntPtr)1);
				}
				if (value == *Unsafe.AddByteOffset<byte>(ref searchSpace, index))
				{
					IL_100:
					return (int)index;
				}
			}
			if (nLength >= (IntPtr)4)
			{
				nLength -= (IntPtr)4;
				index -= (IntPtr)4;
				if (value == *Unsafe.AddByteOffset<byte>(ref searchSpace, index + (IntPtr)3))
				{
					goto IL_10F;
				}
				if (value == *Unsafe.AddByteOffset<byte>(ref searchSpace, index + (IntPtr)2))
				{
					goto IL_109;
				}
				if (value == *Unsafe.AddByteOffset<byte>(ref searchSpace, index + (IntPtr)1))
				{
					goto IL_103;
				}
				if (value == *Unsafe.AddByteOffset<byte>(ref searchSpace, index))
				{
					goto IL_100;
				}
			}
			while (nLength > (IntPtr)0)
			{
				nLength -= (IntPtr)1;
				index -= (IntPtr)1;
				if (value == *Unsafe.AddByteOffset<byte>(ref searchSpace, index))
				{
					goto IL_100;
				}
			}
			return -1;
		}

		// Token: 0x0600194C RID: 6476 RVA: 0x00051504 File Offset: 0x0004F704
		public unsafe static int IndexOfAny(ref byte searchSpace, byte value0, byte value1, int length)
		{
			IntPtr index = (IntPtr)0;
			IntPtr nLength = (IntPtr)length;
			while (nLength >= (IntPtr)8)
			{
				nLength -= (IntPtr)8;
				uint lookUp = (uint)(*Unsafe.AddByteOffset<byte>(ref searchSpace, index));
				if ((uint)value0 == lookUp || (uint)value1 == lookUp)
				{
					IL_198:
					return (int)index;
				}
				lookUp = (uint)(*Unsafe.AddByteOffset<byte>(ref searchSpace, index + (IntPtr)1));
				if ((uint)value0 == lookUp || (uint)value1 == lookUp)
				{
					IL_19B:
					return (int)(index + (IntPtr)1);
				}
				lookUp = (uint)(*Unsafe.AddByteOffset<byte>(ref searchSpace, index + (IntPtr)2));
				if ((uint)value0 == lookUp || (uint)value1 == lookUp)
				{
					IL_1A1:
					return (int)(index + (IntPtr)2);
				}
				lookUp = (uint)(*Unsafe.AddByteOffset<byte>(ref searchSpace, index + (IntPtr)3));
				if ((uint)value0 == lookUp || (uint)value1 == lookUp)
				{
					IL_1A7:
					return (int)(index + (IntPtr)3);
				}
				lookUp = (uint)(*Unsafe.AddByteOffset<byte>(ref searchSpace, index + (IntPtr)4));
				if ((uint)value0 == lookUp || (uint)value1 == lookUp)
				{
					return (int)(index + (IntPtr)4);
				}
				lookUp = (uint)(*Unsafe.AddByteOffset<byte>(ref searchSpace, index + (IntPtr)5));
				if ((uint)value0 == lookUp || (uint)value1 == lookUp)
				{
					return (int)(index + (IntPtr)5);
				}
				lookUp = (uint)(*Unsafe.AddByteOffset<byte>(ref searchSpace, index + (IntPtr)6));
				if ((uint)value0 == lookUp || (uint)value1 == lookUp)
				{
					return (int)(index + (IntPtr)6);
				}
				lookUp = (uint)(*Unsafe.AddByteOffset<byte>(ref searchSpace, index + (IntPtr)7));
				if ((uint)value0 == lookUp || (uint)value1 == lookUp)
				{
					return (int)(index + (IntPtr)7);
				}
				index += (IntPtr)8;
			}
			if (nLength >= (IntPtr)4)
			{
				nLength -= (IntPtr)4;
				uint lookUp = (uint)(*Unsafe.AddByteOffset<byte>(ref searchSpace, index));
				if ((uint)value0 == lookUp || (uint)value1 == lookUp)
				{
					goto IL_198;
				}
				lookUp = (uint)(*Unsafe.AddByteOffset<byte>(ref searchSpace, index + (IntPtr)1));
				if ((uint)value0 == lookUp || (uint)value1 == lookUp)
				{
					goto IL_19B;
				}
				lookUp = (uint)(*Unsafe.AddByteOffset<byte>(ref searchSpace, index + (IntPtr)2));
				if ((uint)value0 == lookUp || (uint)value1 == lookUp)
				{
					goto IL_1A1;
				}
				lookUp = (uint)(*Unsafe.AddByteOffset<byte>(ref searchSpace, index + (IntPtr)3));
				if ((uint)value0 == lookUp || (uint)value1 == lookUp)
				{
					goto IL_1A7;
				}
				index += (IntPtr)4;
			}
			while (nLength > (IntPtr)0)
			{
				nLength -= (IntPtr)1;
				uint lookUp = (uint)(*Unsafe.AddByteOffset<byte>(ref searchSpace, index));
				if ((uint)value0 == lookUp || (uint)value1 == lookUp)
				{
					goto IL_198;
				}
				index += (IntPtr)1;
			}
			return -1;
		}

		// Token: 0x0600194D RID: 6477 RVA: 0x000516D8 File Offset: 0x0004F8D8
		public unsafe static int IndexOfAny(ref byte searchSpace, byte value0, byte value1, byte value2, int length)
		{
			IntPtr index = (IntPtr)0;
			IntPtr nLength = (IntPtr)length;
			while (nLength >= (IntPtr)8)
			{
				nLength -= (IntPtr)8;
				uint lookUp = (uint)(*Unsafe.AddByteOffset<byte>(ref searchSpace, index));
				if ((uint)value0 == lookUp || (uint)value1 == lookUp || (uint)value2 == lookUp)
				{
					IL_207:
					return (int)index;
				}
				lookUp = (uint)(*Unsafe.AddByteOffset<byte>(ref searchSpace, index + (IntPtr)1));
				if ((uint)value0 == lookUp || (uint)value1 == lookUp || (uint)value2 == lookUp)
				{
					IL_20A:
					return (int)(index + (IntPtr)1);
				}
				lookUp = (uint)(*Unsafe.AddByteOffset<byte>(ref searchSpace, index + (IntPtr)2));
				if ((uint)value0 == lookUp || (uint)value1 == lookUp || (uint)value2 == lookUp)
				{
					IL_210:
					return (int)(index + (IntPtr)2);
				}
				lookUp = (uint)(*Unsafe.AddByteOffset<byte>(ref searchSpace, index + (IntPtr)3));
				if ((uint)value0 == lookUp || (uint)value1 == lookUp || (uint)value2 == lookUp)
				{
					IL_216:
					return (int)(index + (IntPtr)3);
				}
				lookUp = (uint)(*Unsafe.AddByteOffset<byte>(ref searchSpace, index + (IntPtr)4));
				if ((uint)value0 == lookUp || (uint)value1 == lookUp || (uint)value2 == lookUp)
				{
					return (int)(index + (IntPtr)4);
				}
				lookUp = (uint)(*Unsafe.AddByteOffset<byte>(ref searchSpace, index + (IntPtr)5));
				if ((uint)value0 == lookUp || (uint)value1 == lookUp || (uint)value2 == lookUp)
				{
					return (int)(index + (IntPtr)5);
				}
				lookUp = (uint)(*Unsafe.AddByteOffset<byte>(ref searchSpace, index + (IntPtr)6));
				if ((uint)value0 == lookUp || (uint)value1 == lookUp || (uint)value2 == lookUp)
				{
					return (int)(index + (IntPtr)6);
				}
				lookUp = (uint)(*Unsafe.AddByteOffset<byte>(ref searchSpace, index + (IntPtr)7));
				if ((uint)value0 == lookUp || (uint)value1 == lookUp || (uint)value2 == lookUp)
				{
					return (int)(index + (IntPtr)7);
				}
				index += (IntPtr)8;
			}
			if (nLength >= (IntPtr)4)
			{
				nLength -= (IntPtr)4;
				uint lookUp = (uint)(*Unsafe.AddByteOffset<byte>(ref searchSpace, index));
				if ((uint)value0 == lookUp || (uint)value1 == lookUp || (uint)value2 == lookUp)
				{
					goto IL_207;
				}
				lookUp = (uint)(*Unsafe.AddByteOffset<byte>(ref searchSpace, index + (IntPtr)1));
				if ((uint)value0 == lookUp || (uint)value1 == lookUp || (uint)value2 == lookUp)
				{
					goto IL_20A;
				}
				lookUp = (uint)(*Unsafe.AddByteOffset<byte>(ref searchSpace, index + (IntPtr)2));
				if ((uint)value0 == lookUp || (uint)value1 == lookUp || (uint)value2 == lookUp)
				{
					goto IL_210;
				}
				lookUp = (uint)(*Unsafe.AddByteOffset<byte>(ref searchSpace, index + (IntPtr)3));
				if ((uint)value0 == lookUp || (uint)value1 == lookUp || (uint)value2 == lookUp)
				{
					goto IL_216;
				}
				index += (IntPtr)4;
			}
			while (nLength > (IntPtr)0)
			{
				nLength -= (IntPtr)1;
				uint lookUp = (uint)(*Unsafe.AddByteOffset<byte>(ref searchSpace, index));
				if ((uint)value0 == lookUp || (uint)value1 == lookUp || (uint)value2 == lookUp)
				{
					goto IL_207;
				}
				index += (IntPtr)1;
			}
			return -1;
		}

		// Token: 0x0600194E RID: 6478 RVA: 0x00051918 File Offset: 0x0004FB18
		public unsafe static int LastIndexOfAny(ref byte searchSpace, byte value0, byte value1, int length)
		{
			IntPtr index = (IntPtr)length;
			IntPtr nLength = (IntPtr)length;
			while (nLength >= (IntPtr)8)
			{
				nLength -= (IntPtr)8;
				index -= (IntPtr)8;
				uint lookUp = (uint)(*Unsafe.AddByteOffset<byte>(ref searchSpace, index + (IntPtr)7));
				if ((uint)value0 == lookUp || (uint)value1 == lookUp)
				{
					return (int)(index + (IntPtr)7);
				}
				lookUp = (uint)(*Unsafe.AddByteOffset<byte>(ref searchSpace, index + (IntPtr)6));
				if ((uint)value0 == lookUp || (uint)value1 == lookUp)
				{
					return (int)(index + (IntPtr)6);
				}
				lookUp = (uint)(*Unsafe.AddByteOffset<byte>(ref searchSpace, index + (IntPtr)5));
				if ((uint)value0 == lookUp || (uint)value1 == lookUp)
				{
					return (int)(index + (IntPtr)5);
				}
				lookUp = (uint)(*Unsafe.AddByteOffset<byte>(ref searchSpace, index + (IntPtr)4));
				if ((uint)value0 == lookUp || (uint)value1 == lookUp)
				{
					return (int)(index + (IntPtr)4);
				}
				lookUp = (uint)(*Unsafe.AddByteOffset<byte>(ref searchSpace, index + (IntPtr)3));
				if ((uint)value0 == lookUp || (uint)value1 == lookUp)
				{
					IL_1A7:
					return (int)(index + (IntPtr)3);
				}
				lookUp = (uint)(*Unsafe.AddByteOffset<byte>(ref searchSpace, index + (IntPtr)2));
				if ((uint)value0 == lookUp || (uint)value1 == lookUp)
				{
					IL_1A1:
					return (int)(index + (IntPtr)2);
				}
				lookUp = (uint)(*Unsafe.AddByteOffset<byte>(ref searchSpace, index + (IntPtr)1));
				if ((uint)value0 == lookUp || (uint)value1 == lookUp)
				{
					IL_19B:
					return (int)(index + (IntPtr)1);
				}
				lookUp = (uint)(*Unsafe.AddByteOffset<byte>(ref searchSpace, index));
				if ((uint)value0 == lookUp || (uint)value1 == lookUp)
				{
					IL_198:
					return (int)index;
				}
			}
			if (nLength >= (IntPtr)4)
			{
				nLength -= (IntPtr)4;
				index -= (IntPtr)4;
				uint lookUp = (uint)(*Unsafe.AddByteOffset<byte>(ref searchSpace, index + (IntPtr)3));
				if ((uint)value0 == lookUp || (uint)value1 == lookUp)
				{
					goto IL_1A7;
				}
				lookUp = (uint)(*Unsafe.AddByteOffset<byte>(ref searchSpace, index + (IntPtr)2));
				if ((uint)value0 == lookUp || (uint)value1 == lookUp)
				{
					goto IL_1A1;
				}
				lookUp = (uint)(*Unsafe.AddByteOffset<byte>(ref searchSpace, index + (IntPtr)1));
				if ((uint)value0 == lookUp || (uint)value1 == lookUp)
				{
					goto IL_19B;
				}
				lookUp = (uint)(*Unsafe.AddByteOffset<byte>(ref searchSpace, index));
				if ((uint)value0 == lookUp)
				{
					goto IL_198;
				}
				if ((uint)value1 == lookUp)
				{
					goto IL_198;
				}
			}
			while (nLength > (IntPtr)0)
			{
				nLength -= (IntPtr)1;
				index -= (IntPtr)1;
				uint lookUp = (uint)(*Unsafe.AddByteOffset<byte>(ref searchSpace, index));
				if ((uint)value0 == lookUp || (uint)value1 == lookUp)
				{
					goto IL_198;
				}
			}
			return -1;
		}

		// Token: 0x0600194F RID: 6479 RVA: 0x00051AEC File Offset: 0x0004FCEC
		public unsafe static int LastIndexOfAny(ref byte searchSpace, byte value0, byte value1, byte value2, int length)
		{
			IntPtr index = (IntPtr)length;
			IntPtr nLength = (IntPtr)length;
			while (nLength >= (IntPtr)8)
			{
				nLength -= (IntPtr)8;
				index -= (IntPtr)8;
				uint lookUp = (uint)(*Unsafe.AddByteOffset<byte>(ref searchSpace, index + (IntPtr)7));
				if ((uint)value0 == lookUp || (uint)value1 == lookUp || (uint)value2 == lookUp)
				{
					return (int)(index + (IntPtr)7);
				}
				lookUp = (uint)(*Unsafe.AddByteOffset<byte>(ref searchSpace, index + (IntPtr)6));
				if ((uint)value0 == lookUp || (uint)value1 == lookUp || (uint)value2 == lookUp)
				{
					return (int)(index + (IntPtr)6);
				}
				lookUp = (uint)(*Unsafe.AddByteOffset<byte>(ref searchSpace, index + (IntPtr)5));
				if ((uint)value0 == lookUp || (uint)value1 == lookUp || (uint)value2 == lookUp)
				{
					return (int)(index + (IntPtr)5);
				}
				lookUp = (uint)(*Unsafe.AddByteOffset<byte>(ref searchSpace, index + (IntPtr)4));
				if ((uint)value0 == lookUp || (uint)value1 == lookUp || (uint)value2 == lookUp)
				{
					return (int)(index + (IntPtr)4);
				}
				lookUp = (uint)(*Unsafe.AddByteOffset<byte>(ref searchSpace, index + (IntPtr)3));
				if ((uint)value0 == lookUp || (uint)value1 == lookUp || (uint)value2 == lookUp)
				{
					IL_217:
					return (int)(index + (IntPtr)3);
				}
				lookUp = (uint)(*Unsafe.AddByteOffset<byte>(ref searchSpace, index + (IntPtr)2));
				if ((uint)value0 == lookUp || (uint)value1 == lookUp || (uint)value2 == lookUp)
				{
					IL_211:
					return (int)(index + (IntPtr)2);
				}
				lookUp = (uint)(*Unsafe.AddByteOffset<byte>(ref searchSpace, index + (IntPtr)1));
				if ((uint)value0 == lookUp || (uint)value1 == lookUp || (uint)value2 == lookUp)
				{
					IL_20B:
					return (int)(index + (IntPtr)1);
				}
				lookUp = (uint)(*Unsafe.AddByteOffset<byte>(ref searchSpace, index));
				if ((uint)value0 == lookUp || (uint)value1 == lookUp || (uint)value2 == lookUp)
				{
					IL_208:
					return (int)index;
				}
			}
			if (nLength >= (IntPtr)4)
			{
				nLength -= (IntPtr)4;
				index -= (IntPtr)4;
				uint lookUp = (uint)(*Unsafe.AddByteOffset<byte>(ref searchSpace, index + (IntPtr)3));
				if ((uint)value0 == lookUp || (uint)value1 == lookUp || (uint)value2 == lookUp)
				{
					goto IL_217;
				}
				lookUp = (uint)(*Unsafe.AddByteOffset<byte>(ref searchSpace, index + (IntPtr)2));
				if ((uint)value0 == lookUp || (uint)value1 == lookUp || (uint)value2 == lookUp)
				{
					goto IL_211;
				}
				lookUp = (uint)(*Unsafe.AddByteOffset<byte>(ref searchSpace, index + (IntPtr)1));
				if ((uint)value0 == lookUp || (uint)value1 == lookUp || (uint)value2 == lookUp)
				{
					goto IL_20B;
				}
				lookUp = (uint)(*Unsafe.AddByteOffset<byte>(ref searchSpace, index));
				if ((uint)value0 == lookUp || (uint)value1 == lookUp)
				{
					goto IL_208;
				}
				if ((uint)value2 == lookUp)
				{
					goto IL_208;
				}
			}
			while (nLength > (IntPtr)0)
			{
				nLength -= (IntPtr)1;
				index -= (IntPtr)1;
				uint lookUp = (uint)(*Unsafe.AddByteOffset<byte>(ref searchSpace, index));
				if ((uint)value0 == lookUp || (uint)value1 == lookUp || (uint)value2 == lookUp)
				{
					goto IL_208;
				}
			}
			return -1;
		}

		// Token: 0x06001950 RID: 6480 RVA: 0x00051D30 File Offset: 0x0004FF30
		public unsafe static bool SequenceEqual(ref byte first, ref byte second, [NativeInteger] UIntPtr length)
		{
			if (!Unsafe.AreSame<byte>(ref first, ref second))
			{
				IntPtr i = (IntPtr)0;
				if (length >= (UIntPtr)((IntPtr)sizeof(UIntPtr)))
				{
					IntPtr j = (IntPtr)(length - (UIntPtr)((IntPtr)sizeof(UIntPtr)));
					while (j > i)
					{
						if (Unsafe.ReadUnaligned<UIntPtr>(Unsafe.AddByteOffset<byte>(ref first, i)) != Unsafe.ReadUnaligned<UIntPtr>(Unsafe.AddByteOffset<byte>(ref second, i)))
						{
							return false;
						}
						i += (IntPtr)sizeof(UIntPtr);
					}
					return Unsafe.ReadUnaligned<UIntPtr>(Unsafe.AddByteOffset<byte>(ref first, j)) == Unsafe.ReadUnaligned<UIntPtr>(Unsafe.AddByteOffset<byte>(ref second, j));
				}
				while (length > (UIntPtr)i)
				{
					if (*Unsafe.AddByteOffset<byte>(ref first, i) != *Unsafe.AddByteOffset<byte>(ref second, i))
					{
						return false;
					}
					i += (IntPtr)1;
				}
				return true;
			}
			return true;
		}

		// Token: 0x06001951 RID: 6481 RVA: 0x00051DD0 File Offset: 0x0004FFD0
		public unsafe static int SequenceCompareTo(ref byte first, int firstLength, ref byte second, int secondLength)
		{
			if (!Unsafe.AreSame<byte>(ref first, ref second))
			{
				IntPtr minLength = (IntPtr)((firstLength < secondLength) ? firstLength : secondLength);
				IntPtr i = (IntPtr)0;
				IntPtr j = minLength;
				if (j > (IntPtr)sizeof(UIntPtr))
				{
					j -= (IntPtr)sizeof(UIntPtr);
					while (j > i)
					{
						if (Unsafe.ReadUnaligned<UIntPtr>(Unsafe.AddByteOffset<byte>(ref first, i)) != Unsafe.ReadUnaligned<UIntPtr>(Unsafe.AddByteOffset<byte>(ref second, i)))
						{
							break;
						}
						i += (IntPtr)sizeof(UIntPtr);
					}
				}
				while (minLength > i)
				{
					int result = Unsafe.AddByteOffset<byte>(ref first, i).CompareTo(*Unsafe.AddByteOffset<byte>(ref second, i));
					if (result != 0)
					{
						return result;
					}
					i += (IntPtr)1;
				}
			}
			return firstLength - secondLength;
		}

		// Token: 0x06001952 RID: 6482 RVA: 0x00051E60 File Offset: 0x00050060
		public unsafe static int SequenceCompareTo(ref char first, int firstLength, ref char second, int secondLength)
		{
			int lengthDelta = firstLength - secondLength;
			if (!Unsafe.AreSame<char>(ref first, ref second))
			{
				IntPtr minLength = (IntPtr)((firstLength < secondLength) ? firstLength : secondLength);
				IntPtr i = (IntPtr)0;
				if (minLength >= (IntPtr)(sizeof(UIntPtr) / 2))
				{
					while (minLength >= i + (IntPtr)(sizeof(UIntPtr) / 2) && !(Unsafe.ReadUnaligned<UIntPtr>(Unsafe.As<char, byte>(Unsafe.Add<char>(ref first, i))) != Unsafe.ReadUnaligned<UIntPtr>(Unsafe.As<char, byte>(Unsafe.Add<char>(ref second, i)))))
					{
						i += (IntPtr)(sizeof(UIntPtr) / 2);
					}
				}
				if (sizeof(UIntPtr) > 4 && minLength >= i + (IntPtr)2 && Unsafe.ReadUnaligned<int>(Unsafe.As<char, byte>(Unsafe.Add<char>(ref first, i))) == Unsafe.ReadUnaligned<int>(Unsafe.As<char, byte>(Unsafe.Add<char>(ref second, i))))
				{
					i += (IntPtr)2;
				}
				while (i < minLength)
				{
					int result = Unsafe.Add<char>(ref first, i).CompareTo(*Unsafe.Add<char>(ref second, i));
					if (result != 0)
					{
						return result;
					}
					i += (IntPtr)1;
				}
			}
			return lengthDelta;
		}

		// Token: 0x06001953 RID: 6483 RVA: 0x00051F3C File Offset: 0x0005013C
		public unsafe static int IndexOf(ref char searchSpace, char value, int length)
		{
			fixed (char* ptr = &searchSpace)
			{
				char* pChars = ptr;
				char* pCh = pChars;
				IntPtr intPtr = (IntPtr)length;
				while (length >= 4)
				{
					length -= 4;
					if (*pCh != value)
					{
						if (pCh[1] != value)
						{
							if (pCh[2] != value)
							{
								if (pCh[3] != value)
								{
									pCh += 4;
									continue;
								}
								pCh++;
							}
							pCh++;
						}
						pCh++;
					}
					IL_5E:
					return (int)((long)(pCh - pChars));
				}
				while (length > 0)
				{
					length--;
					if (*pCh == value)
					{
						goto IL_5E;
					}
					pCh++;
				}
				return -1;
			}
		}

		// Token: 0x06001954 RID: 6484 RVA: 0x00051FB0 File Offset: 0x000501B0
		public unsafe static int LastIndexOf(ref char searchSpace, char value, int length)
		{
			fixed (char* ptr = &searchSpace)
			{
				char* ptr2 = ptr;
				char* pCh = ptr2 + length;
				char* pEndCh = ptr2;
				while (length >= 4)
				{
					length -= 4;
					pCh -= 4;
					if (pCh[3] == value)
					{
						return (int)((long)(pCh - pEndCh)) + 3;
					}
					if (pCh[2] == value)
					{
						return (int)((long)(pCh - pEndCh)) + 2;
					}
					if (pCh[1] == value)
					{
						return (int)((long)(pCh - pEndCh)) + 1;
					}
					if (*pCh == value)
					{
						IL_54:
						return (int)((long)(pCh - pEndCh));
					}
				}
				while (length > 0)
				{
					length--;
					pCh--;
					if (*pCh == value)
					{
						goto IL_54;
					}
				}
				return -1;
			}
		}

		// Token: 0x06001955 RID: 6485 RVA: 0x00052038 File Offset: 0x00050238
		public unsafe static void CopyTo<[Nullable(2)] T>(ref T dst, int dstLength, ref T src, int srcLength)
		{
			IntPtr srcByteCount = Unsafe.ByteOffset<T>(ref src, Unsafe.Add<T>(ref src, srcLength));
			IntPtr dstByteCount = Unsafe.ByteOffset<T>(ref dst, Unsafe.Add<T>(ref dst, dstLength));
			IntPtr diff = Unsafe.ByteOffset<T>(ref src, ref dst);
			if (!((sizeof(IntPtr) == 4) ? ((int)diff < (int)srcByteCount || (int)diff > -(int)dstByteCount) : ((long)diff < (long)srcByteCount || (long)diff > -(long)dstByteCount)) && !SpanHelpers.IsReferenceOrContainsReferences<T>())
			{
				ref byte dstBytes = ref Unsafe.As<T, byte>(ref dst);
				ref byte srcBytes = ref Unsafe.As<T, byte>(ref src);
				ulong byteCount = (ulong)(long)srcByteCount;
				uint blockSize;
				for (ulong index = 0UL; index < byteCount; index += (ulong)blockSize)
				{
					blockSize = ((byteCount - index > (ulong)(-1)) ? uint.MaxValue : ((uint)(byteCount - index)));
					Unsafe.CopyBlock(Unsafe.Add<byte>(ref dstBytes, (IntPtr)((long)index)), Unsafe.Add<byte>(ref srcBytes, (IntPtr)((long)index)), blockSize);
				}
				return;
			}
			bool flag = ((sizeof(IntPtr) == 4) ? ((int)diff > -(int)dstByteCount) : ((long)diff > -(long)dstByteCount));
			int direction = (flag ? 1 : (-1));
			int runCount = (flag ? 0 : (srcLength - 1));
			int loopCount;
			for (loopCount = 0; loopCount < (srcLength & -8); loopCount += 8)
			{
				*Unsafe.Add<T>(ref dst, runCount) = *Unsafe.Add<T>(ref src, runCount);
				*Unsafe.Add<T>(ref dst, runCount + direction) = *Unsafe.Add<T>(ref src, runCount + direction);
				*Unsafe.Add<T>(ref dst, runCount + direction * 2) = *Unsafe.Add<T>(ref src, runCount + direction * 2);
				*Unsafe.Add<T>(ref dst, runCount + direction * 3) = *Unsafe.Add<T>(ref src, runCount + direction * 3);
				*Unsafe.Add<T>(ref dst, runCount + direction * 4) = *Unsafe.Add<T>(ref src, runCount + direction * 4);
				*Unsafe.Add<T>(ref dst, runCount + direction * 5) = *Unsafe.Add<T>(ref src, runCount + direction * 5);
				*Unsafe.Add<T>(ref dst, runCount + direction * 6) = *Unsafe.Add<T>(ref src, runCount + direction * 6);
				*Unsafe.Add<T>(ref dst, runCount + direction * 7) = *Unsafe.Add<T>(ref src, runCount + direction * 7);
				runCount += direction * 8;
			}
			if (loopCount < (srcLength & -4))
			{
				*Unsafe.Add<T>(ref dst, runCount) = *Unsafe.Add<T>(ref src, runCount);
				*Unsafe.Add<T>(ref dst, runCount + direction) = *Unsafe.Add<T>(ref src, runCount + direction);
				*Unsafe.Add<T>(ref dst, runCount + direction * 2) = *Unsafe.Add<T>(ref src, runCount + direction * 2);
				*Unsafe.Add<T>(ref dst, runCount + direction * 3) = *Unsafe.Add<T>(ref src, runCount + direction * 3);
				runCount += direction * 4;
				loopCount += 4;
			}
			while (loopCount < srcLength)
			{
				*Unsafe.Add<T>(ref dst, runCount) = *Unsafe.Add<T>(ref src, runCount);
				runCount += direction;
				loopCount++;
			}
		}

		// Token: 0x06001956 RID: 6486 RVA: 0x0005235C File Offset: 0x0005055C
		[NullableContext(2)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static IntPtr Add<T>(this IntPtr start, int index)
		{
			if (sizeof(IntPtr) == 4)
			{
				uint byteLength = (uint)(index * Unsafe.SizeOf<T>());
				return (IntPtr)((void*)((byte*)(void*)start + byteLength));
			}
			ulong byteLength2 = (ulong)((long)index * (long)Unsafe.SizeOf<T>());
			return (IntPtr)((void*)((byte*)(void*)start + byteLength2));
		}

		// Token: 0x06001957 RID: 6487 RVA: 0x000523A1 File Offset: 0x000505A1
		[NullableContext(2)]
		public static bool IsReferenceOrContainsReferences<T>()
		{
			return SpanHelpers.PerTypeValues<T>.IsReferenceOrContainsReferences;
		}

		// Token: 0x06001958 RID: 6488 RVA: 0x000523A8 File Offset: 0x000505A8
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
			Type underlyingNullable = Nullable.GetUnderlyingType(type);
			if (underlyingNullable != null)
			{
				type = underlyingNullable;
			}
			if (type.GetTypeInfo().IsEnum)
			{
				return false;
			}
			foreach (FieldInfo field in type.GetTypeInfo().DeclaredFields)
			{
				if (!field.IsStatic && SpanHelpers.IsReferenceOrContainsReferencesCore(field.FieldType))
				{
					return true;
				}
			}
			return false;
		}

		// Token: 0x06001959 RID: 6489 RVA: 0x00052450 File Offset: 0x00050650
		[NullableContext(0)]
		public unsafe static void ClearLessThanPointerSized(byte* ptr, UIntPtr byteLength)
		{
			if (sizeof(UIntPtr) == 4)
			{
				Unsafe.InitBlockUnaligned((void*)ptr, 0, (uint)byteLength);
				return;
			}
			ulong bytesRemaining = (ulong)byteLength;
			uint bytesToClear = (uint)(bytesRemaining & (ulong)(-1));
			Unsafe.InitBlockUnaligned((void*)ptr, 0, bytesToClear);
			bytesRemaining -= (ulong)bytesToClear;
			ptr += bytesToClear;
			while (bytesRemaining > 0UL)
			{
				bytesToClear = ((bytesRemaining >= (ulong)(-1)) ? uint.MaxValue : ((uint)bytesRemaining));
				Unsafe.InitBlockUnaligned((void*)ptr, 0, bytesToClear);
				ptr += bytesToClear;
				bytesRemaining -= (ulong)bytesToClear;
			}
		}

		// Token: 0x0600195A RID: 6490 RVA: 0x000524BC File Offset: 0x000506BC
		public static void ClearLessThanPointerSized(ref byte b, UIntPtr byteLength)
		{
			if (sizeof(UIntPtr) == 4)
			{
				Unsafe.InitBlockUnaligned(ref b, 0, (uint)byteLength);
				return;
			}
			ulong bytesRemaining = (ulong)byteLength;
			uint bytesToClear = (uint)(bytesRemaining & (ulong)(-1));
			Unsafe.InitBlockUnaligned(ref b, 0, bytesToClear);
			bytesRemaining -= (ulong)bytesToClear;
			long byteOffset = (long)((ulong)bytesToClear);
			while (bytesRemaining > 0UL)
			{
				bytesToClear = ((bytesRemaining >= (ulong)(-1)) ? uint.MaxValue : ((uint)bytesRemaining));
				Unsafe.InitBlockUnaligned(Unsafe.Add<byte>(ref b, (IntPtr)byteOffset), 0, bytesToClear);
				byteOffset += (long)((ulong)bytesToClear);
				bytesRemaining -= (ulong)bytesToClear;
			}
		}

		// Token: 0x0600195B RID: 6491 RVA: 0x0005252C File Offset: 0x0005072C
		public unsafe static void ClearPointerSizedWithoutReferences(ref byte b, [NativeInteger] UIntPtr byteLength)
		{
			IntPtr i = (IntPtr)0;
			while (i.LessThanEqual(byteLength - (UIntPtr)((IntPtr)sizeof(SpanHelpers.Reg64))))
			{
				*Unsafe.As<byte, SpanHelpers.Reg64>(Unsafe.Add<byte>(ref b, i)) = default(SpanHelpers.Reg64);
				i += (IntPtr)sizeof(SpanHelpers.Reg64);
			}
			if (i.LessThanEqual(byteLength - (UIntPtr)((IntPtr)sizeof(SpanHelpers.Reg32))))
			{
				*Unsafe.As<byte, SpanHelpers.Reg32>(Unsafe.Add<byte>(ref b, i)) = default(SpanHelpers.Reg32);
				i += (IntPtr)sizeof(SpanHelpers.Reg32);
			}
			if (i.LessThanEqual(byteLength - (UIntPtr)((IntPtr)sizeof(SpanHelpers.Reg16))))
			{
				*Unsafe.As<byte, SpanHelpers.Reg16>(Unsafe.Add<byte>(ref b, i)) = default(SpanHelpers.Reg16);
				i += (IntPtr)sizeof(SpanHelpers.Reg16);
			}
			if (i.LessThanEqual(byteLength - (UIntPtr)((IntPtr)8)))
			{
				*Unsafe.As<byte, long>(Unsafe.Add<byte>(ref b, i)) = 0L;
				i += (IntPtr)8;
			}
			if (sizeof(IntPtr) == 4 && i.LessThanEqual(byteLength - (UIntPtr)((IntPtr)4)))
			{
				*Unsafe.As<byte, int>(Unsafe.Add<byte>(ref b, i)) = 0;
			}
		}

		// Token: 0x0600195C RID: 6492 RVA: 0x00052608 File Offset: 0x00050808
		public unsafe static void ClearPointerSizedWithReferences(ref IntPtr ip, [NativeInteger] UIntPtr pointerSizeLength)
		{
			IntPtr i = (IntPtr)0;
			IntPtr j;
			while ((j = i + (IntPtr)8).LessThanEqual(pointerSizeLength))
			{
				*Unsafe.Add<IntPtr>(ref ip, i + (IntPtr)0) = 0;
				*Unsafe.Add<IntPtr>(ref ip, i + (IntPtr)1) = 0;
				*Unsafe.Add<IntPtr>(ref ip, i + (IntPtr)2) = 0;
				*Unsafe.Add<IntPtr>(ref ip, i + (IntPtr)3) = 0;
				*Unsafe.Add<IntPtr>(ref ip, i + (IntPtr)4) = 0;
				*Unsafe.Add<IntPtr>(ref ip, i + (IntPtr)5) = 0;
				*Unsafe.Add<IntPtr>(ref ip, i + (IntPtr)6) = 0;
				*Unsafe.Add<IntPtr>(ref ip, i + (IntPtr)7) = 0;
				i = j;
			}
			if ((j = i + (IntPtr)4).LessThanEqual(pointerSizeLength))
			{
				*Unsafe.Add<IntPtr>(ref ip, i + (IntPtr)0) = 0;
				*Unsafe.Add<IntPtr>(ref ip, i + (IntPtr)1) = 0;
				*Unsafe.Add<IntPtr>(ref ip, i + (IntPtr)2) = 0;
				*Unsafe.Add<IntPtr>(ref ip, i + (IntPtr)3) = 0;
				i = j;
			}
			if ((j = i + (IntPtr)2).LessThanEqual(pointerSizeLength))
			{
				*Unsafe.Add<IntPtr>(ref ip, i + (IntPtr)0) = 0;
				*Unsafe.Add<IntPtr>(ref ip, i + (IntPtr)1) = 0;
				i = j;
			}
			if ((i + (IntPtr)1).LessThanEqual(pointerSizeLength))
			{
				*Unsafe.Add<IntPtr>(ref ip, i) = 0;
			}
		}

		// Token: 0x0600195D RID: 6493 RVA: 0x00052749 File Offset: 0x00050949
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static bool LessThanEqual(this IntPtr index, UIntPtr length)
		{
			if (sizeof(UIntPtr) != 4)
			{
				return (long)index <= (long)(ulong)length;
			}
			return (int)index <= (int)(uint)length;
		}

		// Token: 0x0600195E RID: 6494 RVA: 0x00052778 File Offset: 0x00050978
		public static int IndexOf<[Nullable(0)] T>(ref T searchSpace, int searchSpaceLength, ref T value, int valueLength) where T : IEquatable<T>
		{
			if (valueLength == 0)
			{
				return 0;
			}
			T valueHead = value;
			ref T valueTail = ref Unsafe.Add<T>(ref value, 1);
			int valueTailLength = valueLength - 1;
			int index = 0;
			for (;;)
			{
				int remainingSearchSpaceLength = searchSpaceLength - index - valueTailLength;
				if (remainingSearchSpaceLength <= 0)
				{
					return -1;
				}
				int relativeIndex = SpanHelpers.IndexOf<T>(Unsafe.Add<T>(ref searchSpace, index), valueHead, remainingSearchSpaceLength);
				if (relativeIndex == -1)
				{
					return -1;
				}
				index += relativeIndex;
				if (SpanHelpers.SequenceEqual<T>(Unsafe.Add<T>(ref searchSpace, index + 1), ref valueTail, valueTailLength))
				{
					break;
				}
				index++;
			}
			return index;
		}

		// Token: 0x0600195F RID: 6495 RVA: 0x000527E4 File Offset: 0x000509E4
		public unsafe static int IndexOf<[Nullable(0)] T>(ref T searchSpace, T value, int length) where T : IEquatable<T>
		{
			UIntPtr index = (UIntPtr)((IntPtr)0);
			while (length >= 8)
			{
				length -= 8;
				ref T ptr = ref value;
				if (default(T) == null)
				{
					T t = value;
					ptr = ref t;
				}
				if (ptr.Equals(*Unsafe.Add<T>(ref searchSpace, index)))
				{
					IL_312:
					return (int)index;
				}
				ref T ptr2 = ref value;
				if (default(T) == null)
				{
					T t = value;
					ptr2 = ref t;
				}
				if (ptr2.Equals(*Unsafe.Add<T>(ref searchSpace, index + (UIntPtr)((IntPtr)1))))
				{
					IL_315:
					return (int)(index + (UIntPtr)((IntPtr)1));
				}
				ref T ptr3 = ref value;
				if (default(T) == null)
				{
					T t = value;
					ptr3 = ref t;
				}
				if (ptr3.Equals(*Unsafe.Add<T>(ref searchSpace, index + (UIntPtr)((IntPtr)2))))
				{
					IL_31B:
					return (int)(index + (UIntPtr)((IntPtr)2));
				}
				ref T ptr4 = ref value;
				if (default(T) == null)
				{
					T t = value;
					ptr4 = ref t;
				}
				if (ptr4.Equals(*Unsafe.Add<T>(ref searchSpace, index + (UIntPtr)((IntPtr)3))))
				{
					IL_321:
					return (int)(index + (UIntPtr)((IntPtr)3));
				}
				ref T ptr5 = ref value;
				if (default(T) == null)
				{
					T t = value;
					ptr5 = ref t;
				}
				if (ptr5.Equals(*Unsafe.Add<T>(ref searchSpace, index + (UIntPtr)((IntPtr)4))))
				{
					return (int)(index + (UIntPtr)((IntPtr)4));
				}
				ref T ptr6 = ref value;
				if (default(T) == null)
				{
					T t = value;
					ptr6 = ref t;
				}
				if (ptr6.Equals(*Unsafe.Add<T>(ref searchSpace, index + (UIntPtr)((IntPtr)5))))
				{
					return (int)(index + (UIntPtr)((IntPtr)5));
				}
				ref T ptr7 = ref value;
				if (default(T) == null)
				{
					T t = value;
					ptr7 = ref t;
				}
				if (ptr7.Equals(*Unsafe.Add<T>(ref searchSpace, index + (UIntPtr)((IntPtr)6))))
				{
					return (int)(index + (UIntPtr)((IntPtr)6));
				}
				ref T ptr8 = ref value;
				if (default(T) == null)
				{
					T t = value;
					ptr8 = ref t;
				}
				if (ptr8.Equals(*Unsafe.Add<T>(ref searchSpace, index + (UIntPtr)((IntPtr)7))))
				{
					return (int)(index + (UIntPtr)((IntPtr)7));
				}
				index += (UIntPtr)((IntPtr)8);
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
				if (ptr9.Equals(*Unsafe.Add<T>(ref searchSpace, index)))
				{
					goto IL_312;
				}
				ref T ptr10 = ref value;
				if (default(T) == null)
				{
					T t = value;
					ptr10 = ref t;
				}
				if (ptr10.Equals(*Unsafe.Add<T>(ref searchSpace, index + (UIntPtr)((IntPtr)1))))
				{
					goto IL_315;
				}
				ref T ptr11 = ref value;
				if (default(T) == null)
				{
					T t = value;
					ptr11 = ref t;
				}
				if (ptr11.Equals(*Unsafe.Add<T>(ref searchSpace, index + (UIntPtr)((IntPtr)2))))
				{
					goto IL_31B;
				}
				ref T ptr12 = ref value;
				if (default(T) == null)
				{
					T t = value;
					ptr12 = ref t;
				}
				if (ptr12.Equals(*Unsafe.Add<T>(ref searchSpace, index + (UIntPtr)((IntPtr)3))))
				{
					goto IL_321;
				}
				index += (UIntPtr)((IntPtr)4);
			}
			while (length > 0)
			{
				ref T ptr13 = ref value;
				if (default(T) == null)
				{
					T t = value;
					ptr13 = ref t;
				}
				if (ptr13.Equals(*Unsafe.Add<T>(ref searchSpace, index)))
				{
					goto IL_312;
				}
				index += (UIntPtr)((IntPtr)1);
				length--;
			}
			return -1;
		}

		// Token: 0x06001960 RID: 6496 RVA: 0x00052B30 File Offset: 0x00050D30
		public unsafe static int IndexOfAny<[Nullable(0)] T>(ref T searchSpace, T value0, T value1, int length) where T : IEquatable<T>
		{
			int index = 0;
			while (length - index >= 8)
			{
				T lookUp = *Unsafe.Add<T>(ref searchSpace, index);
				if (value0.Equals(lookUp) || value1.Equals(lookUp))
				{
					return index;
				}
				lookUp = *Unsafe.Add<T>(ref searchSpace, index + 1);
				if (value0.Equals(lookUp) || value1.Equals(lookUp))
				{
					IL_2CB:
					return index + 1;
				}
				lookUp = *Unsafe.Add<T>(ref searchSpace, index + 2);
				if (value0.Equals(lookUp) || value1.Equals(lookUp))
				{
					IL_2CF:
					return index + 2;
				}
				lookUp = *Unsafe.Add<T>(ref searchSpace, index + 3);
				if (value0.Equals(lookUp) || value1.Equals(lookUp))
				{
					IL_2D3:
					return index + 3;
				}
				lookUp = *Unsafe.Add<T>(ref searchSpace, index + 4);
				if (value0.Equals(lookUp) || value1.Equals(lookUp))
				{
					return index + 4;
				}
				lookUp = *Unsafe.Add<T>(ref searchSpace, index + 5);
				if (value0.Equals(lookUp) || value1.Equals(lookUp))
				{
					return index + 5;
				}
				lookUp = *Unsafe.Add<T>(ref searchSpace, index + 6);
				if (value0.Equals(lookUp) || value1.Equals(lookUp))
				{
					return index + 6;
				}
				lookUp = *Unsafe.Add<T>(ref searchSpace, index + 7);
				if (value0.Equals(lookUp) || value1.Equals(lookUp))
				{
					return index + 7;
				}
				index += 8;
			}
			if (length - index >= 4)
			{
				T lookUp = *Unsafe.Add<T>(ref searchSpace, index);
				if (value0.Equals(lookUp) || value1.Equals(lookUp))
				{
					return index;
				}
				lookUp = *Unsafe.Add<T>(ref searchSpace, index + 1);
				if (value0.Equals(lookUp) || value1.Equals(lookUp))
				{
					goto IL_2CB;
				}
				lookUp = *Unsafe.Add<T>(ref searchSpace, index + 2);
				if (value0.Equals(lookUp) || value1.Equals(lookUp))
				{
					goto IL_2CF;
				}
				lookUp = *Unsafe.Add<T>(ref searchSpace, index + 3);
				if (value0.Equals(lookUp) || value1.Equals(lookUp))
				{
					goto IL_2D3;
				}
				index += 4;
			}
			while (index < length)
			{
				T lookUp = *Unsafe.Add<T>(ref searchSpace, index);
				if (value0.Equals(lookUp) || value1.Equals(lookUp))
				{
					return index;
				}
				index++;
			}
			return -1;
		}

		// Token: 0x06001961 RID: 6497 RVA: 0x00052E24 File Offset: 0x00051024
		public unsafe static int IndexOfAny<[Nullable(0)] T>(ref T searchSpace, T value0, T value1, T value2, int length) where T : IEquatable<T>
		{
			int index = 0;
			while (length - index >= 8)
			{
				T lookUp = *Unsafe.Add<T>(ref searchSpace, index);
				if (value0.Equals(lookUp) || value1.Equals(lookUp) || value2.Equals(lookUp))
				{
					return index;
				}
				lookUp = *Unsafe.Add<T>(ref searchSpace, index + 1);
				if (value0.Equals(lookUp) || value1.Equals(lookUp) || value2.Equals(lookUp))
				{
					IL_3C2:
					return index + 1;
				}
				lookUp = *Unsafe.Add<T>(ref searchSpace, index + 2);
				if (value0.Equals(lookUp) || value1.Equals(lookUp) || value2.Equals(lookUp))
				{
					IL_3C6:
					return index + 2;
				}
				lookUp = *Unsafe.Add<T>(ref searchSpace, index + 3);
				if (value0.Equals(lookUp) || value1.Equals(lookUp) || value2.Equals(lookUp))
				{
					IL_3CA:
					return index + 3;
				}
				lookUp = *Unsafe.Add<T>(ref searchSpace, index + 4);
				if (value0.Equals(lookUp) || value1.Equals(lookUp) || value2.Equals(lookUp))
				{
					return index + 4;
				}
				lookUp = *Unsafe.Add<T>(ref searchSpace, index + 5);
				if (value0.Equals(lookUp) || value1.Equals(lookUp) || value2.Equals(lookUp))
				{
					return index + 5;
				}
				lookUp = *Unsafe.Add<T>(ref searchSpace, index + 6);
				if (value0.Equals(lookUp) || value1.Equals(lookUp) || value2.Equals(lookUp))
				{
					return index + 6;
				}
				lookUp = *Unsafe.Add<T>(ref searchSpace, index + 7);
				if (value0.Equals(lookUp) || value1.Equals(lookUp) || value2.Equals(lookUp))
				{
					return index + 7;
				}
				index += 8;
			}
			if (length - index >= 4)
			{
				T lookUp = *Unsafe.Add<T>(ref searchSpace, index);
				if (value0.Equals(lookUp) || value1.Equals(lookUp) || value2.Equals(lookUp))
				{
					return index;
				}
				lookUp = *Unsafe.Add<T>(ref searchSpace, index + 1);
				if (value0.Equals(lookUp) || value1.Equals(lookUp) || value2.Equals(lookUp))
				{
					goto IL_3C2;
				}
				lookUp = *Unsafe.Add<T>(ref searchSpace, index + 2);
				if (value0.Equals(lookUp) || value1.Equals(lookUp) || value2.Equals(lookUp))
				{
					goto IL_3C6;
				}
				lookUp = *Unsafe.Add<T>(ref searchSpace, index + 3);
				if (value0.Equals(lookUp) || value1.Equals(lookUp) || value2.Equals(lookUp))
				{
					goto IL_3CA;
				}
				index += 4;
			}
			while (index < length)
			{
				T lookUp = *Unsafe.Add<T>(ref searchSpace, index);
				if (value0.Equals(lookUp) || value1.Equals(lookUp) || value2.Equals(lookUp))
				{
					return index;
				}
				index++;
			}
			return -1;
		}

		// Token: 0x06001962 RID: 6498 RVA: 0x00053210 File Offset: 0x00051410
		public unsafe static int IndexOfAny<[Nullable(0)] T>(ref T searchSpace, int searchSpaceLength, ref T value, int valueLength) where T : IEquatable<T>
		{
			if (valueLength == 0)
			{
				return 0;
			}
			int index = -1;
			for (int i = 0; i < valueLength; i++)
			{
				int tempIndex = SpanHelpers.IndexOf<T>(ref searchSpace, *Unsafe.Add<T>(ref value, i), searchSpaceLength);
				if (tempIndex < index)
				{
					index = tempIndex;
					searchSpaceLength = tempIndex;
					if (index == 0)
					{
						break;
					}
				}
			}
			return index;
		}

		// Token: 0x06001963 RID: 6499 RVA: 0x00053254 File Offset: 0x00051454
		public static int LastIndexOf<[Nullable(0)] T>(ref T searchSpace, int searchSpaceLength, ref T value, int valueLength) where T : IEquatable<T>
		{
			if (valueLength == 0)
			{
				return 0;
			}
			T valueHead = value;
			ref T valueTail = ref Unsafe.Add<T>(ref value, 1);
			int valueTailLength = valueLength - 1;
			int index = 0;
			int relativeIndex;
			for (;;)
			{
				int remainingSearchSpaceLength = searchSpaceLength - index - valueTailLength;
				if (remainingSearchSpaceLength <= 0)
				{
					return -1;
				}
				relativeIndex = SpanHelpers.LastIndexOf<T>(ref searchSpace, valueHead, remainingSearchSpaceLength);
				if (relativeIndex == -1)
				{
					return -1;
				}
				if (SpanHelpers.SequenceEqual<T>(Unsafe.Add<T>(ref searchSpace, relativeIndex + 1), ref valueTail, valueTailLength))
				{
					break;
				}
				index += remainingSearchSpaceLength - relativeIndex;
			}
			return relativeIndex;
		}

		// Token: 0x06001964 RID: 6500 RVA: 0x000532B8 File Offset: 0x000514B8
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

		// Token: 0x06001965 RID: 6501 RVA: 0x000535D8 File Offset: 0x000517D8
		public unsafe static int LastIndexOfAny<[Nullable(0)] T>(ref T searchSpace, T value0, T value1, int length) where T : IEquatable<T>
		{
			while (length >= 8)
			{
				length -= 8;
				T lookUp = *Unsafe.Add<T>(ref searchSpace, length + 7);
				if (value0.Equals(lookUp) || value1.Equals(lookUp))
				{
					return length + 7;
				}
				lookUp = *Unsafe.Add<T>(ref searchSpace, length + 6);
				if (value0.Equals(lookUp) || value1.Equals(lookUp))
				{
					return length + 6;
				}
				lookUp = *Unsafe.Add<T>(ref searchSpace, length + 5);
				if (value0.Equals(lookUp) || value1.Equals(lookUp))
				{
					return length + 5;
				}
				lookUp = *Unsafe.Add<T>(ref searchSpace, length + 4);
				if (value0.Equals(lookUp) || value1.Equals(lookUp))
				{
					return length + 4;
				}
				lookUp = *Unsafe.Add<T>(ref searchSpace, length + 3);
				if (value0.Equals(lookUp) || value1.Equals(lookUp))
				{
					IL_2CD:
					return length + 3;
				}
				lookUp = *Unsafe.Add<T>(ref searchSpace, length + 2);
				if (value0.Equals(lookUp) || value1.Equals(lookUp))
				{
					IL_2C9:
					return length + 2;
				}
				lookUp = *Unsafe.Add<T>(ref searchSpace, length + 1);
				if (value0.Equals(lookUp) || value1.Equals(lookUp))
				{
					IL_2C5:
					return length + 1;
				}
				lookUp = *Unsafe.Add<T>(ref searchSpace, length);
				if (value0.Equals(lookUp) || value1.Equals(lookUp))
				{
					return length;
				}
			}
			if (length >= 4)
			{
				length -= 4;
				T lookUp = *Unsafe.Add<T>(ref searchSpace, length + 3);
				if (value0.Equals(lookUp) || value1.Equals(lookUp))
				{
					goto IL_2CD;
				}
				lookUp = *Unsafe.Add<T>(ref searchSpace, length + 2);
				if (value0.Equals(lookUp) || value1.Equals(lookUp))
				{
					goto IL_2C9;
				}
				lookUp = *Unsafe.Add<T>(ref searchSpace, length + 1);
				if (value0.Equals(lookUp) || value1.Equals(lookUp))
				{
					goto IL_2C5;
				}
				lookUp = *Unsafe.Add<T>(ref searchSpace, length);
				if (value0.Equals(lookUp))
				{
					return length;
				}
				if (value1.Equals(lookUp))
				{
					return length;
				}
			}
			while (length > 0)
			{
				length--;
				T lookUp = *Unsafe.Add<T>(ref searchSpace, length);
				if (value0.Equals(lookUp) || value1.Equals(lookUp))
				{
					return length;
				}
			}
			return -1;
		}

		// Token: 0x06001966 RID: 6502 RVA: 0x000538C8 File Offset: 0x00051AC8
		public unsafe static int LastIndexOfAny<[Nullable(0)] T>(ref T searchSpace, T value0, T value1, T value2, int length) where T : IEquatable<T>
		{
			while (length >= 8)
			{
				length -= 8;
				T lookUp = *Unsafe.Add<T>(ref searchSpace, length + 7);
				if (value0.Equals(lookUp) || value1.Equals(lookUp) || value2.Equals(lookUp))
				{
					return length + 7;
				}
				lookUp = *Unsafe.Add<T>(ref searchSpace, length + 6);
				if (value0.Equals(lookUp) || value1.Equals(lookUp) || value2.Equals(lookUp))
				{
					return length + 6;
				}
				lookUp = *Unsafe.Add<T>(ref searchSpace, length + 5);
				if (value0.Equals(lookUp) || value1.Equals(lookUp) || value2.Equals(lookUp))
				{
					return length + 5;
				}
				lookUp = *Unsafe.Add<T>(ref searchSpace, length + 4);
				if (value0.Equals(lookUp) || value1.Equals(lookUp) || value2.Equals(lookUp))
				{
					return length + 4;
				}
				lookUp = *Unsafe.Add<T>(ref searchSpace, length + 3);
				if (value0.Equals(lookUp) || value1.Equals(lookUp) || value2.Equals(lookUp))
				{
					IL_3DA:
					return length + 3;
				}
				lookUp = *Unsafe.Add<T>(ref searchSpace, length + 2);
				if (value0.Equals(lookUp) || value1.Equals(lookUp) || value2.Equals(lookUp))
				{
					IL_3D5:
					return length + 2;
				}
				lookUp = *Unsafe.Add<T>(ref searchSpace, length + 1);
				if (value0.Equals(lookUp) || value1.Equals(lookUp) || value2.Equals(lookUp))
				{
					IL_3D0:
					return length + 1;
				}
				lookUp = *Unsafe.Add<T>(ref searchSpace, length);
				if (value0.Equals(lookUp) || value1.Equals(lookUp) || value2.Equals(lookUp))
				{
					return length;
				}
			}
			if (length >= 4)
			{
				length -= 4;
				T lookUp = *Unsafe.Add<T>(ref searchSpace, length + 3);
				if (value0.Equals(lookUp) || value1.Equals(lookUp) || value2.Equals(lookUp))
				{
					goto IL_3DA;
				}
				lookUp = *Unsafe.Add<T>(ref searchSpace, length + 2);
				if (value0.Equals(lookUp) || value1.Equals(lookUp) || value2.Equals(lookUp))
				{
					goto IL_3D5;
				}
				lookUp = *Unsafe.Add<T>(ref searchSpace, length + 1);
				if (value0.Equals(lookUp) || value1.Equals(lookUp) || value2.Equals(lookUp))
				{
					goto IL_3D0;
				}
				lookUp = *Unsafe.Add<T>(ref searchSpace, length);
				if (value0.Equals(lookUp) || value1.Equals(lookUp))
				{
					return length;
				}
				if (value2.Equals(lookUp))
				{
					return length;
				}
			}
			while (length > 0)
			{
				length--;
				T lookUp = *Unsafe.Add<T>(ref searchSpace, length);
				if (value0.Equals(lookUp) || value1.Equals(lookUp) || value2.Equals(lookUp))
				{
					return length;
				}
			}
			return -1;
		}

		// Token: 0x06001967 RID: 6503 RVA: 0x00053CC8 File Offset: 0x00051EC8
		public unsafe static int LastIndexOfAny<[Nullable(0)] T>(ref T searchSpace, int searchSpaceLength, ref T value, int valueLength) where T : IEquatable<T>
		{
			if (valueLength == 0)
			{
				return 0;
			}
			int index = -1;
			for (int i = 0; i < valueLength; i++)
			{
				int tempIndex = SpanHelpers.LastIndexOf<T>(ref searchSpace, *Unsafe.Add<T>(ref value, i), searchSpaceLength);
				if (tempIndex > index)
				{
					index = tempIndex;
				}
			}
			return index;
		}

		// Token: 0x06001968 RID: 6504 RVA: 0x00053D04 File Offset: 0x00051F04
		public unsafe static bool SequenceEqual<[Nullable(0)] T>(ref T first, ref T second, int length) where T : IEquatable<T>
		{
			if (!Unsafe.AreSame<T>(ref first, ref second))
			{
				UIntPtr index = (UIntPtr)((IntPtr)0);
				while (length >= 8)
				{
					length -= 8;
					T ptr2;
					ref T ptr = (ptr2 = Unsafe.Add<T>(ref first, index));
					if (default(T) == null)
					{
						T t = ptr;
						ptr2 = ref t;
					}
					if (ptr2.Equals(*Unsafe.Add<T>(ref second, index)))
					{
						T ptr4;
						ref T ptr3 = (ptr4 = Unsafe.Add<T>(ref first, index + (UIntPtr)((IntPtr)1)));
						if (default(T) == null)
						{
							T t = ptr3;
							ptr4 = ref t;
						}
						if (ptr4.Equals(*Unsafe.Add<T>(ref second, index + (UIntPtr)((IntPtr)1))))
						{
							T ptr6;
							ref T ptr5 = (ptr6 = Unsafe.Add<T>(ref first, index + (UIntPtr)((IntPtr)2)));
							if (default(T) == null)
							{
								T t = ptr5;
								ptr6 = ref t;
							}
							if (ptr6.Equals(*Unsafe.Add<T>(ref second, index + (UIntPtr)((IntPtr)2))))
							{
								T ptr8;
								ref T ptr7 = (ptr8 = Unsafe.Add<T>(ref first, index + (UIntPtr)((IntPtr)3)));
								if (default(T) == null)
								{
									T t = ptr7;
									ptr8 = ref t;
								}
								if (ptr8.Equals(*Unsafe.Add<T>(ref second, index + (UIntPtr)((IntPtr)3))))
								{
									T ptr10;
									ref T ptr9 = (ptr10 = Unsafe.Add<T>(ref first, index + (UIntPtr)((IntPtr)4)));
									if (default(T) == null)
									{
										T t = ptr9;
										ptr10 = ref t;
									}
									if (ptr10.Equals(*Unsafe.Add<T>(ref second, index + (UIntPtr)((IntPtr)4))))
									{
										T ptr12;
										ref T ptr11 = (ptr12 = Unsafe.Add<T>(ref first, index + (UIntPtr)((IntPtr)5)));
										if (default(T) == null)
										{
											T t = ptr11;
											ptr12 = ref t;
										}
										if (ptr12.Equals(*Unsafe.Add<T>(ref second, index + (UIntPtr)((IntPtr)5))))
										{
											T ptr14;
											ref T ptr13 = (ptr14 = Unsafe.Add<T>(ref first, index + (UIntPtr)((IntPtr)6)));
											if (default(T) == null)
											{
												T t = ptr13;
												ptr14 = ref t;
											}
											if (ptr14.Equals(*Unsafe.Add<T>(ref second, index + (UIntPtr)((IntPtr)6))))
											{
												T ptr16;
												ref T ptr15 = (ptr16 = Unsafe.Add<T>(ref first, index + (UIntPtr)((IntPtr)7)));
												if (default(T) == null)
												{
													T t = ptr15;
													ptr16 = ref t;
												}
												if (ptr16.Equals(*Unsafe.Add<T>(ref second, index + (UIntPtr)((IntPtr)7))))
												{
													index += (UIntPtr)((IntPtr)8);
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
					ref T ptr17 = (ptr18 = Unsafe.Add<T>(ref first, index));
					if (default(T) == null)
					{
						T t = ptr17;
						ptr18 = ref t;
					}
					if (!ptr18.Equals(*Unsafe.Add<T>(ref second, index)))
					{
						return false;
					}
					T ptr20;
					ref T ptr19 = (ptr20 = Unsafe.Add<T>(ref first, index + (UIntPtr)((IntPtr)1)));
					if (default(T) == null)
					{
						T t = ptr19;
						ptr20 = ref t;
					}
					if (!ptr20.Equals(*Unsafe.Add<T>(ref second, index + (UIntPtr)((IntPtr)1))))
					{
						return false;
					}
					T ptr22;
					ref T ptr21 = (ptr22 = Unsafe.Add<T>(ref first, index + (UIntPtr)((IntPtr)2)));
					if (default(T) == null)
					{
						T t = ptr21;
						ptr22 = ref t;
					}
					if (!ptr22.Equals(*Unsafe.Add<T>(ref second, index + (UIntPtr)((IntPtr)2))))
					{
						return false;
					}
					T ptr24;
					ref T ptr23 = (ptr24 = Unsafe.Add<T>(ref first, index + (UIntPtr)((IntPtr)3)));
					if (default(T) == null)
					{
						T t = ptr23;
						ptr24 = ref t;
					}
					if (!ptr24.Equals(*Unsafe.Add<T>(ref second, index + (UIntPtr)((IntPtr)3))))
					{
						return false;
					}
					index += (UIntPtr)((IntPtr)4);
				}
				while (length > 0)
				{
					T ptr26;
					ref T ptr25 = (ptr26 = Unsafe.Add<T>(ref first, index));
					if (default(T) == null)
					{
						T t = ptr25;
						ptr26 = ref t;
					}
					if (!ptr26.Equals(*Unsafe.Add<T>(ref second, index)))
					{
						return false;
					}
					index += (UIntPtr)((IntPtr)1);
					length--;
				}
			}
			return true;
		}

		// Token: 0x06001969 RID: 6505 RVA: 0x00054090 File Offset: 0x00052290
		public unsafe static int SequenceCompareTo<[Nullable(0)] T>(ref T first, int firstLength, ref T second, int secondLength) where T : IComparable<T>
		{
			int minLength = firstLength;
			if (minLength > secondLength)
			{
				minLength = secondLength;
			}
			for (int i = 0; i < minLength; i++)
			{
				T ptr2;
				ref T ptr = (ptr2 = Unsafe.Add<T>(ref first, i));
				if (default(T) == null)
				{
					T t = ptr;
					ptr2 = ref t;
				}
				int result = ptr2.CompareTo(*Unsafe.Add<T>(ref second, i));
				if (result != 0)
				{
					return result;
				}
			}
			return firstLength.CompareTo(secondLength);
		}

		// Token: 0x02000478 RID: 1144
		[Nullable(0)]
		internal struct ComparerComparable<[Nullable(2)] T, [Nullable(0)] TComparer> : IComparable<T> where TComparer : IComparer<T>
		{
			// Token: 0x0600196A RID: 6506 RVA: 0x000540F5 File Offset: 0x000522F5
			public ComparerComparable(T value, TComparer comparer)
			{
				this._value = value;
				this._comparer = comparer;
			}

			// Token: 0x0600196B RID: 6507 RVA: 0x00054108 File Offset: 0x00052308
			[NullableContext(2)]
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public int CompareTo(T other)
			{
				TComparer comparer = this._comparer;
				return comparer.Compare(this._value, other);
			}

			// Token: 0x040010A9 RID: 4265
			private readonly T _value;

			// Token: 0x040010AA RID: 4266
			private readonly TComparer _comparer;
		}

		// Token: 0x02000479 RID: 1145
		[NullableContext(0)]
		private struct Reg64
		{
		}

		// Token: 0x0200047A RID: 1146
		[NullableContext(0)]
		private struct Reg32
		{
		}

		// Token: 0x0200047B RID: 1147
		[NullableContext(0)]
		private struct Reg16
		{
		}

		// Token: 0x0200047C RID: 1148
		[NullableContext(0)]
		public static class PerTypeValues<[Nullable(2)] T>
		{
			// Token: 0x0600196C RID: 6508 RVA: 0x00054130 File Offset: 0x00052330
			private static IntPtr MeasureArrayAdjustment()
			{
				T[] sampleArray = new T[1];
				return Unsafe.ByteOffset<T>(ILHelpers.ObjectAsRef<T>(sampleArray), ref sampleArray[0]);
			}

			// Token: 0x040010AB RID: 4267
			public static readonly bool IsReferenceOrContainsReferences = SpanHelpers.IsReferenceOrContainsReferencesCore(typeof(T));

			// Token: 0x040010AC RID: 4268
			[Nullable(1)]
			public static readonly T[] EmptyArray = ArrayEx.Empty<T>();

			// Token: 0x040010AD RID: 4269
			public static readonly IntPtr ArrayAdjustment = SpanHelpers.PerTypeValues<T>.MeasureArrayAdjustment();
		}
	}
}
