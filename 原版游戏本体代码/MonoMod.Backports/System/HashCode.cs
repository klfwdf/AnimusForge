using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace System
{
	// Token: 0x02000011 RID: 17
	[NullableContext(1)]
	[Nullable(0)]
	public struct HashCode
	{
		// Token: 0x06000019 RID: 25 RVA: 0x00002254 File Offset: 0x00000454
		private unsafe static uint GenerateGlobalSeed()
		{
			Span<byte> destination = new Span<byte>(stackalloc byte[(UIntPtr)4], 4);
			RandomNumberGenerator randomNumberGenerator = RandomNumberGenerator.Create();
			try
			{
				byte[] array = new byte[destination.Length];
				randomNumberGenerator.GetBytes(array);
				array.AsSpan<byte>().CopyTo(destination);
			}
			finally
			{
				RandomNumberGenerator randomNumberGenerator2 = randomNumberGenerator;
				if (randomNumberGenerator2 != null)
				{
					((IDisposable)randomNumberGenerator2).Dispose();
				}
			}
			return Unsafe.ReadUnaligned<uint>(destination[0]);
		}

		// Token: 0x0600001A RID: 26 RVA: 0x000022C4 File Offset: 0x000004C4
		public static int Combine<[Nullable(2)] T1>(T1 value1)
		{
			ref T1 ptr = ref value1;
			T1 t = default(T1);
			uint num;
			if (t == null)
			{
				t = value1;
				ptr = ref t;
				if (t == null)
				{
					num = 0U;
					goto IL_31;
				}
			}
			num = (uint)ptr.GetHashCode();
			IL_31:
			uint queuedValue = num;
			return (int)HashCode.MixFinal(HashCode.QueueRound(HashCode.MixEmptyState() + 4U, queuedValue));
		}

		// Token: 0x0600001B RID: 27 RVA: 0x00002318 File Offset: 0x00000518
		public static int Combine<[Nullable(2)] T1, [Nullable(2)] T2>(T1 value1, T2 value2)
		{
			ref T1 ptr = ref value1;
			T1 t = default(T1);
			uint num;
			if (t == null)
			{
				t = value1;
				ptr = ref t;
				if (t == null)
				{
					num = 0U;
					goto IL_31;
				}
			}
			num = (uint)ptr.GetHashCode();
			IL_31:
			uint queuedValue = num;
			ref T2 ptr2 = ref value2;
			T2 t2 = default(T2);
			uint num2;
			if (t2 == null)
			{
				t2 = value2;
				ptr2 = ref t2;
				if (t2 == null)
				{
					num2 = 0U;
					goto IL_63;
				}
			}
			num2 = (uint)ptr2.GetHashCode();
			IL_63:
			uint queuedValue2 = num2;
			return (int)HashCode.MixFinal(HashCode.QueueRound(HashCode.QueueRound(HashCode.MixEmptyState() + 8U, queuedValue), queuedValue2));
		}

		// Token: 0x0600001C RID: 28 RVA: 0x000023A4 File Offset: 0x000005A4
		public static int Combine<[Nullable(2)] T1, [Nullable(2)] T2, [Nullable(2)] T3>(T1 value1, T2 value2, T3 value3)
		{
			ref T1 ptr = ref value1;
			T1 t = default(T1);
			uint num;
			if (t == null)
			{
				t = value1;
				ptr = ref t;
				if (t == null)
				{
					num = 0U;
					goto IL_31;
				}
			}
			num = (uint)ptr.GetHashCode();
			IL_31:
			uint queuedValue = num;
			ref T2 ptr2 = ref value2;
			T2 t2 = default(T2);
			uint num2;
			if (t2 == null)
			{
				t2 = value2;
				ptr2 = ref t2;
				if (t2 == null)
				{
					num2 = 0U;
					goto IL_66;
				}
			}
			num2 = (uint)ptr2.GetHashCode();
			IL_66:
			uint queuedValue2 = num2;
			ref T3 ptr3 = ref value3;
			T3 t3 = default(T3);
			uint num3;
			if (t3 == null)
			{
				t3 = value3;
				ptr3 = ref t3;
				if (t3 == null)
				{
					num3 = 0U;
					goto IL_9B;
				}
			}
			num3 = (uint)ptr3.GetHashCode();
			IL_9B:
			uint queuedValue3 = num3;
			return (int)HashCode.MixFinal(HashCode.QueueRound(HashCode.QueueRound(HashCode.QueueRound(HashCode.MixEmptyState() + 12U, queuedValue), queuedValue2), queuedValue3));
		}

		// Token: 0x0600001D RID: 29 RVA: 0x0000246C File Offset: 0x0000066C
		public static int Combine<[Nullable(2)] T1, [Nullable(2)] T2, [Nullable(2)] T3, [Nullable(2)] T4>(T1 value1, T2 value2, T3 value3, T4 value4)
		{
			ref T1 ptr = ref value1;
			T1 t = default(T1);
			uint num;
			if (t == null)
			{
				t = value1;
				ptr = ref t;
				if (t == null)
				{
					num = 0U;
					goto IL_34;
				}
			}
			num = (uint)ptr.GetHashCode();
			IL_34:
			uint input = num;
			ref T2 ptr2 = ref value2;
			T2 t2 = default(T2);
			uint num2;
			if (t2 == null)
			{
				t2 = value2;
				ptr2 = ref t2;
				if (t2 == null)
				{
					num2 = 0U;
					goto IL_69;
				}
			}
			num2 = (uint)ptr2.GetHashCode();
			IL_69:
			uint input2 = num2;
			ref T3 ptr3 = ref value3;
			T3 t3 = default(T3);
			uint num3;
			if (t3 == null)
			{
				t3 = value3;
				ptr3 = ref t3;
				if (t3 == null)
				{
					num3 = 0U;
					goto IL_9E;
				}
			}
			num3 = (uint)ptr3.GetHashCode();
			IL_9E:
			uint input3 = num3;
			ref T4 ptr4 = ref value4;
			T4 t4 = default(T4);
			uint num4;
			if (t4 == null)
			{
				t4 = value4;
				ptr4 = ref t4;
				if (t4 == null)
				{
					num4 = 0U;
					goto IL_D3;
				}
			}
			num4 = (uint)ptr4.GetHashCode();
			IL_D3:
			uint input4 = num4;
			uint num5;
			uint num6;
			uint num7;
			uint num8;
			HashCode.Initialize(out num5, out num6, out num7, out num8);
			num5 = HashCode.Round(num5, input);
			num6 = HashCode.Round(num6, input2);
			num7 = HashCode.Round(num7, input3);
			num8 = HashCode.Round(num8, input4);
			return (int)HashCode.MixFinal(HashCode.MixState(num5, num6, num7, num8) + 16U);
		}

		// Token: 0x0600001E RID: 30 RVA: 0x00002598 File Offset: 0x00000798
		public static int Combine<[Nullable(2)] T1, [Nullable(2)] T2, [Nullable(2)] T3, [Nullable(2)] T4, [Nullable(2)] T5>(T1 value1, T2 value2, T3 value3, T4 value4, T5 value5)
		{
			ref T1 ptr = ref value1;
			T1 t = default(T1);
			uint num;
			if (t == null)
			{
				t = value1;
				ptr = ref t;
				if (t == null)
				{
					num = 0U;
					goto IL_34;
				}
			}
			num = (uint)ptr.GetHashCode();
			IL_34:
			uint input = num;
			ref T2 ptr2 = ref value2;
			T2 t2 = default(T2);
			uint num2;
			if (t2 == null)
			{
				t2 = value2;
				ptr2 = ref t2;
				if (t2 == null)
				{
					num2 = 0U;
					goto IL_69;
				}
			}
			num2 = (uint)ptr2.GetHashCode();
			IL_69:
			uint input2 = num2;
			ref T3 ptr3 = ref value3;
			T3 t3 = default(T3);
			uint num3;
			if (t3 == null)
			{
				t3 = value3;
				ptr3 = ref t3;
				if (t3 == null)
				{
					num3 = 0U;
					goto IL_9E;
				}
			}
			num3 = (uint)ptr3.GetHashCode();
			IL_9E:
			uint input3 = num3;
			ref T4 ptr4 = ref value4;
			T4 t4 = default(T4);
			uint num4;
			if (t4 == null)
			{
				t4 = value4;
				ptr4 = ref t4;
				if (t4 == null)
				{
					num4 = 0U;
					goto IL_D3;
				}
			}
			num4 = (uint)ptr4.GetHashCode();
			IL_D3:
			uint input4 = num4;
			ref T5 ptr5 = ref value5;
			T5 t5 = default(T5);
			uint num5;
			if (t5 == null)
			{
				t5 = value5;
				ptr5 = ref t5;
				if (t5 == null)
				{
					num5 = 0U;
					goto IL_108;
				}
			}
			num5 = (uint)ptr5.GetHashCode();
			IL_108:
			uint queuedValue = num5;
			uint num6;
			uint num7;
			uint num8;
			uint num9;
			HashCode.Initialize(out num6, out num7, out num8, out num9);
			num6 = HashCode.Round(num6, input);
			num7 = HashCode.Round(num7, input2);
			num8 = HashCode.Round(num8, input3);
			num9 = HashCode.Round(num9, input4);
			return (int)HashCode.MixFinal(HashCode.QueueRound(HashCode.MixState(num6, num7, num8, num9) + 20U, queuedValue));
		}

		// Token: 0x0600001F RID: 31 RVA: 0x00002700 File Offset: 0x00000900
		public static int Combine<[Nullable(2)] T1, [Nullable(2)] T2, [Nullable(2)] T3, [Nullable(2)] T4, [Nullable(2)] T5, [Nullable(2)] T6>(T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6)
		{
			ref T1 ptr = ref value1;
			T1 t = default(T1);
			uint num;
			if (t == null)
			{
				t = value1;
				ptr = ref t;
				if (t == null)
				{
					num = 0U;
					goto IL_34;
				}
			}
			num = (uint)ptr.GetHashCode();
			IL_34:
			uint input = num;
			ref T2 ptr2 = ref value2;
			T2 t2 = default(T2);
			uint num2;
			if (t2 == null)
			{
				t2 = value2;
				ptr2 = ref t2;
				if (t2 == null)
				{
					num2 = 0U;
					goto IL_69;
				}
			}
			num2 = (uint)ptr2.GetHashCode();
			IL_69:
			uint input2 = num2;
			ref T3 ptr3 = ref value3;
			T3 t3 = default(T3);
			uint num3;
			if (t3 == null)
			{
				t3 = value3;
				ptr3 = ref t3;
				if (t3 == null)
				{
					num3 = 0U;
					goto IL_9E;
				}
			}
			num3 = (uint)ptr3.GetHashCode();
			IL_9E:
			uint input3 = num3;
			ref T4 ptr4 = ref value4;
			T4 t4 = default(T4);
			uint num4;
			if (t4 == null)
			{
				t4 = value4;
				ptr4 = ref t4;
				if (t4 == null)
				{
					num4 = 0U;
					goto IL_D3;
				}
			}
			num4 = (uint)ptr4.GetHashCode();
			IL_D3:
			uint input4 = num4;
			ref T5 ptr5 = ref value5;
			T5 t5 = default(T5);
			uint num5;
			if (t5 == null)
			{
				t5 = value5;
				ptr5 = ref t5;
				if (t5 == null)
				{
					num5 = 0U;
					goto IL_108;
				}
			}
			num5 = (uint)ptr5.GetHashCode();
			IL_108:
			uint queuedValue = num5;
			ref T6 ptr6 = ref value6;
			T6 t6 = default(T6);
			uint num6;
			if (t6 == null)
			{
				t6 = value6;
				ptr6 = ref t6;
				if (t6 == null)
				{
					num6 = 0U;
					goto IL_13E;
				}
			}
			num6 = (uint)ptr6.GetHashCode();
			IL_13E:
			uint queuedValue2 = num6;
			uint num7;
			uint num8;
			uint num9;
			uint num10;
			HashCode.Initialize(out num7, out num8, out num9, out num10);
			num7 = HashCode.Round(num7, input);
			num8 = HashCode.Round(num8, input2);
			num9 = HashCode.Round(num9, input3);
			num10 = HashCode.Round(num10, input4);
			return (int)HashCode.MixFinal(HashCode.QueueRound(HashCode.QueueRound(HashCode.MixState(num7, num8, num9, num10) + 24U, queuedValue), queuedValue2));
		}

		// Token: 0x06000020 RID: 32 RVA: 0x000028A8 File Offset: 0x00000AA8
		public static int Combine<[Nullable(2)] T1, [Nullable(2)] T2, [Nullable(2)] T3, [Nullable(2)] T4, [Nullable(2)] T5, [Nullable(2)] T6, [Nullable(2)] T7>(T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7)
		{
			ref T1 ptr = ref value1;
			T1 t = default(T1);
			uint num;
			if (t == null)
			{
				t = value1;
				ptr = ref t;
				if (t == null)
				{
					num = 0U;
					goto IL_34;
				}
			}
			num = (uint)ptr.GetHashCode();
			IL_34:
			uint input = num;
			ref T2 ptr2 = ref value2;
			T2 t2 = default(T2);
			uint num2;
			if (t2 == null)
			{
				t2 = value2;
				ptr2 = ref t2;
				if (t2 == null)
				{
					num2 = 0U;
					goto IL_69;
				}
			}
			num2 = (uint)ptr2.GetHashCode();
			IL_69:
			uint input2 = num2;
			ref T3 ptr3 = ref value3;
			T3 t3 = default(T3);
			uint num3;
			if (t3 == null)
			{
				t3 = value3;
				ptr3 = ref t3;
				if (t3 == null)
				{
					num3 = 0U;
					goto IL_9E;
				}
			}
			num3 = (uint)ptr3.GetHashCode();
			IL_9E:
			uint input3 = num3;
			ref T4 ptr4 = ref value4;
			T4 t4 = default(T4);
			uint num4;
			if (t4 == null)
			{
				t4 = value4;
				ptr4 = ref t4;
				if (t4 == null)
				{
					num4 = 0U;
					goto IL_D3;
				}
			}
			num4 = (uint)ptr4.GetHashCode();
			IL_D3:
			uint input4 = num4;
			ref T5 ptr5 = ref value5;
			T5 t5 = default(T5);
			uint num5;
			if (t5 == null)
			{
				t5 = value5;
				ptr5 = ref t5;
				if (t5 == null)
				{
					num5 = 0U;
					goto IL_108;
				}
			}
			num5 = (uint)ptr5.GetHashCode();
			IL_108:
			uint queuedValue = num5;
			ref T6 ptr6 = ref value6;
			T6 t6 = default(T6);
			uint num6;
			if (t6 == null)
			{
				t6 = value6;
				ptr6 = ref t6;
				if (t6 == null)
				{
					num6 = 0U;
					goto IL_13E;
				}
			}
			num6 = (uint)ptr6.GetHashCode();
			IL_13E:
			uint queuedValue2 = num6;
			ref T7 ptr7 = ref value7;
			T7 t7 = default(T7);
			uint num7;
			if (t7 == null)
			{
				t7 = value7;
				ptr7 = ref t7;
				if (t7 == null)
				{
					num7 = 0U;
					goto IL_174;
				}
			}
			num7 = (uint)ptr7.GetHashCode();
			IL_174:
			uint queuedValue3 = num7;
			uint num8;
			uint num9;
			uint num10;
			uint num11;
			HashCode.Initialize(out num8, out num9, out num10, out num11);
			num8 = HashCode.Round(num8, input);
			num9 = HashCode.Round(num9, input2);
			num10 = HashCode.Round(num10, input3);
			num11 = HashCode.Round(num11, input4);
			return (int)HashCode.MixFinal(HashCode.QueueRound(HashCode.QueueRound(HashCode.QueueRound(HashCode.MixState(num8, num9, num10, num11) + 28U, queuedValue), queuedValue2), queuedValue3));
		}

		// Token: 0x06000021 RID: 33 RVA: 0x00002A8C File Offset: 0x00000C8C
		public static int Combine<[Nullable(2)] T1, [Nullable(2)] T2, [Nullable(2)] T3, [Nullable(2)] T4, [Nullable(2)] T5, [Nullable(2)] T6, [Nullable(2)] T7, [Nullable(2)] T8>(T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7, T8 value8)
		{
			ref T1 ptr = ref value1;
			T1 t = default(T1);
			uint num;
			if (t == null)
			{
				t = value1;
				ptr = ref t;
				if (t == null)
				{
					num = 0U;
					goto IL_34;
				}
			}
			num = (uint)ptr.GetHashCode();
			IL_34:
			uint input = num;
			ref T2 ptr2 = ref value2;
			T2 t2 = default(T2);
			uint num2;
			if (t2 == null)
			{
				t2 = value2;
				ptr2 = ref t2;
				if (t2 == null)
				{
					num2 = 0U;
					goto IL_69;
				}
			}
			num2 = (uint)ptr2.GetHashCode();
			IL_69:
			uint input2 = num2;
			ref T3 ptr3 = ref value3;
			T3 t3 = default(T3);
			uint num3;
			if (t3 == null)
			{
				t3 = value3;
				ptr3 = ref t3;
				if (t3 == null)
				{
					num3 = 0U;
					goto IL_9E;
				}
			}
			num3 = (uint)ptr3.GetHashCode();
			IL_9E:
			uint input3 = num3;
			ref T4 ptr4 = ref value4;
			T4 t4 = default(T4);
			uint num4;
			if (t4 == null)
			{
				t4 = value4;
				ptr4 = ref t4;
				if (t4 == null)
				{
					num4 = 0U;
					goto IL_D3;
				}
			}
			num4 = (uint)ptr4.GetHashCode();
			IL_D3:
			uint input4 = num4;
			ref T5 ptr5 = ref value5;
			T5 t5 = default(T5);
			uint num5;
			if (t5 == null)
			{
				t5 = value5;
				ptr5 = ref t5;
				if (t5 == null)
				{
					num5 = 0U;
					goto IL_108;
				}
			}
			num5 = (uint)ptr5.GetHashCode();
			IL_108:
			uint input5 = num5;
			ref T6 ptr6 = ref value6;
			T6 t6 = default(T6);
			uint num6;
			if (t6 == null)
			{
				t6 = value6;
				ptr6 = ref t6;
				if (t6 == null)
				{
					num6 = 0U;
					goto IL_13E;
				}
			}
			num6 = (uint)ptr6.GetHashCode();
			IL_13E:
			uint input6 = num6;
			ref T7 ptr7 = ref value7;
			T7 t7 = default(T7);
			uint num7;
			if (t7 == null)
			{
				t7 = value7;
				ptr7 = ref t7;
				if (t7 == null)
				{
					num7 = 0U;
					goto IL_174;
				}
			}
			num7 = (uint)ptr7.GetHashCode();
			IL_174:
			uint input7 = num7;
			ref T8 ptr8 = ref value8;
			T8 t8 = default(T8);
			uint num8;
			if (t8 == null)
			{
				t8 = value8;
				ptr8 = ref t8;
				if (t8 == null)
				{
					num8 = 0U;
					goto IL_1AA;
				}
			}
			num8 = (uint)ptr8.GetHashCode();
			IL_1AA:
			uint input8 = num8;
			uint num9;
			uint num10;
			uint num11;
			uint num12;
			HashCode.Initialize(out num9, out num10, out num11, out num12);
			num9 = HashCode.Round(num9, input);
			num10 = HashCode.Round(num10, input2);
			num11 = HashCode.Round(num11, input3);
			num12 = HashCode.Round(num12, input4);
			num9 = HashCode.Round(num9, input5);
			num10 = HashCode.Round(num10, input6);
			num11 = HashCode.Round(num11, input7);
			num12 = HashCode.Round(num12, input8);
			return (int)HashCode.MixFinal(HashCode.MixState(num9, num10, num11, num12) + 32U);
		}

		// Token: 0x06000022 RID: 34 RVA: 0x00002CBB File Offset: 0x00000EBB
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void Initialize(out uint v1, out uint v2, out uint v3, out uint v4)
		{
			v1 = HashCode.s_seed + 2654435761U + 2246822519U;
			v2 = HashCode.s_seed + 2246822519U;
			v3 = HashCode.s_seed;
			v4 = HashCode.s_seed - 2654435761U;
		}

		// Token: 0x06000023 RID: 35 RVA: 0x00002CF1 File Offset: 0x00000EF1
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static uint Round(uint hash, uint input)
		{
			return BitOperations.RotateLeft(hash + input * 2246822519U, 13) * 2654435761U;
		}

		// Token: 0x06000024 RID: 36 RVA: 0x00002D09 File Offset: 0x00000F09
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static uint QueueRound(uint hash, uint queuedValue)
		{
			return BitOperations.RotateLeft(hash + queuedValue * 3266489917U, 17) * 668265263U;
		}

		// Token: 0x06000025 RID: 37 RVA: 0x00002D21 File Offset: 0x00000F21
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static uint MixState(uint v1, uint v2, uint v3, uint v4)
		{
			return BitOperations.RotateLeft(v1, 1) + BitOperations.RotateLeft(v2, 7) + BitOperations.RotateLeft(v3, 12) + BitOperations.RotateLeft(v4, 18);
		}

		// Token: 0x06000026 RID: 38 RVA: 0x00002D44 File Offset: 0x00000F44
		private static uint MixEmptyState()
		{
			return HashCode.s_seed + 374761393U;
		}

		// Token: 0x06000027 RID: 39 RVA: 0x00002D51 File Offset: 0x00000F51
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static uint MixFinal(uint hash)
		{
			hash ^= hash >> 15;
			hash *= 2246822519U;
			hash ^= hash >> 13;
			hash *= 3266489917U;
			hash ^= hash >> 16;
			return hash;
		}

		// Token: 0x06000028 RID: 40 RVA: 0x00002D80 File Offset: 0x00000F80
		public void Add<[Nullable(2)] T>(T value)
		{
			ref T ptr = ref value;
			T t = default(T);
			int value2;
			if (t == null)
			{
				t = value;
				ptr = ref t;
				if (t == null)
				{
					value2 = 0;
					goto IL_32;
				}
			}
			value2 = ptr.GetHashCode();
			IL_32:
			this.Add(value2);
		}

		// Token: 0x06000029 RID: 41 RVA: 0x00002DC4 File Offset: 0x00000FC4
		public void Add<[Nullable(2)] T>(T value, [Nullable(new byte[] { 2, 1 })] IEqualityComparer<T> comparer)
		{
			this.Add((value == null) ? 0 : ((comparer != null) ? comparer.GetHashCode(value) : value.GetHashCode()));
		}

		// Token: 0x0600002A RID: 42 RVA: 0x00002DF0 File Offset: 0x00000FF0
		[NullableContext(0)]
		public void AddBytes(ReadOnlySpan<byte> value)
		{
			ref byte ptr = ref MemoryMarshal.GetReference<byte>(value);
			ref byte ptr2 = ref Unsafe.Add<byte>(ref ptr, value.Length);
			while (Unsafe.ByteOffset<byte>(ref ptr, ref ptr2) >= (IntPtr)4)
			{
				this.Add(Unsafe.ReadUnaligned<int>(ref ptr));
				ptr = Unsafe.Add<byte>(ref ptr, 4);
			}
			while (Unsafe.IsAddressLessThan<byte>(ref ptr, ref ptr2))
			{
				this.Add((int)ptr);
				ptr = Unsafe.Add<byte>(ref ptr, 1);
			}
		}

		// Token: 0x0600002B RID: 43 RVA: 0x00002E50 File Offset: 0x00001050
		private void Add(int value)
		{
			uint length = this._length;
			this._length = length + 1U;
			uint num = length;
			uint num2 = num % 4U;
			if (num2 == 0U)
			{
				this._queue1 = (uint)value;
				return;
			}
			if (num2 == 1U)
			{
				this._queue2 = (uint)value;
				return;
			}
			if (num2 == 2U)
			{
				this._queue3 = (uint)value;
				return;
			}
			if (num == 3U)
			{
				HashCode.Initialize(out this._v1, out this._v2, out this._v3, out this._v4);
			}
			this._v1 = HashCode.Round(this._v1, this._queue1);
			this._v2 = HashCode.Round(this._v2, this._queue2);
			this._v3 = HashCode.Round(this._v3, this._queue3);
			this._v4 = HashCode.Round(this._v4, (uint)value);
		}

		// Token: 0x0600002C RID: 44 RVA: 0x00002F10 File Offset: 0x00001110
		public int ToHashCode()
		{
			uint length = this._length;
			uint num = length % 4U;
			uint num2 = ((length < 4U) ? HashCode.MixEmptyState() : HashCode.MixState(this._v1, this._v2, this._v3, this._v4));
			num2 += length * 4U;
			if (num > 0U)
			{
				num2 = HashCode.QueueRound(num2, this._queue1);
				if (num > 1U)
				{
					num2 = HashCode.QueueRound(num2, this._queue2);
					if (num > 2U)
					{
						num2 = HashCode.QueueRound(num2, this._queue3);
					}
				}
			}
			return (int)HashCode.MixFinal(num2);
		}

		// Token: 0x0600002D RID: 45 RVA: 0x00002F92 File Offset: 0x00001192
		[Obsolete("HashCode is a mutable struct and should not be compared with other HashCodes. Use ToHashCode to retrieve the computed hash code.", true)]
		[EditorBrowsable(EditorBrowsableState.Never)]
		public override int GetHashCode()
		{
			throw new NotSupportedException("GetHashCode on HashCode is not supported");
		}

		// Token: 0x0600002E RID: 46 RVA: 0x00002F9E File Offset: 0x0000119E
		[NullableContext(2)]
		[Obsolete("HashCode is a mutable struct and should not be compared with other HashCodes.", true)]
		[EditorBrowsable(EditorBrowsableState.Never)]
		public override bool Equals(object obj)
		{
			throw new NotSupportedException("Equals on HashCode is not supported");
		}

		// Token: 0x04000017 RID: 23
		private static readonly uint s_seed = HashCode.GenerateGlobalSeed();

		// Token: 0x04000018 RID: 24
		private const uint Prime1 = 2654435761U;

		// Token: 0x04000019 RID: 25
		private const uint Prime2 = 2246822519U;

		// Token: 0x0400001A RID: 26
		private const uint Prime3 = 3266489917U;

		// Token: 0x0400001B RID: 27
		private const uint Prime4 = 668265263U;

		// Token: 0x0400001C RID: 28
		private const uint Prime5 = 374761393U;

		// Token: 0x0400001D RID: 29
		private uint _v1;

		// Token: 0x0400001E RID: 30
		private uint _v2;

		// Token: 0x0400001F RID: 31
		private uint _v3;

		// Token: 0x04000020 RID: 32
		private uint _v4;

		// Token: 0x04000021 RID: 33
		private uint _queue1;

		// Token: 0x04000022 RID: 34
		private uint _queue2;

		// Token: 0x04000023 RID: 35
		private uint _queue3;

		// Token: 0x04000024 RID: 36
		private uint _length;
	}
}
