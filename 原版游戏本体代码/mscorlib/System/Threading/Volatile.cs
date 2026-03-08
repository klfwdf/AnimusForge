using System;
using System.Runtime.ConstrainedExecution;
using System.Security;

namespace System.Threading
{
	// Token: 0x02000532 RID: 1330
	[__DynamicallyInvokable]
	public static class Volatile
	{
		// Token: 0x06003E64 RID: 15972 RVA: 0x000E8A78 File Offset: 0x000E6C78
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		[__DynamicallyInvokable]
		public static bool Read(ref bool location)
		{
			bool result = location;
			Thread.MemoryBarrier();
			return result;
		}

		// Token: 0x06003E65 RID: 15973 RVA: 0x000E8A90 File Offset: 0x000E6C90
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public static sbyte Read(ref sbyte location)
		{
			sbyte result = location;
			Thread.MemoryBarrier();
			return result;
		}

		// Token: 0x06003E66 RID: 15974 RVA: 0x000E8AA8 File Offset: 0x000E6CA8
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		[__DynamicallyInvokable]
		public static byte Read(ref byte location)
		{
			byte result = location;
			Thread.MemoryBarrier();
			return result;
		}

		// Token: 0x06003E67 RID: 15975 RVA: 0x000E8AC0 File Offset: 0x000E6CC0
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		[__DynamicallyInvokable]
		public static short Read(ref short location)
		{
			short result = location;
			Thread.MemoryBarrier();
			return result;
		}

		// Token: 0x06003E68 RID: 15976 RVA: 0x000E8AD8 File Offset: 0x000E6CD8
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public static ushort Read(ref ushort location)
		{
			ushort result = location;
			Thread.MemoryBarrier();
			return result;
		}

		// Token: 0x06003E69 RID: 15977 RVA: 0x000E8AF0 File Offset: 0x000E6CF0
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		[__DynamicallyInvokable]
		public static int Read(ref int location)
		{
			int result = location;
			Thread.MemoryBarrier();
			return result;
		}

		// Token: 0x06003E6A RID: 15978 RVA: 0x000E8B08 File Offset: 0x000E6D08
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public static uint Read(ref uint location)
		{
			uint result = location;
			Thread.MemoryBarrier();
			return result;
		}

		// Token: 0x06003E6B RID: 15979 RVA: 0x000E8B1E File Offset: 0x000E6D1E
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		[__DynamicallyInvokable]
		public static long Read(ref long location)
		{
			return Interlocked.CompareExchange(ref location, 0L, 0L);
		}

		// Token: 0x06003E6C RID: 15980 RVA: 0x000E8B2C File Offset: 0x000E6D2C
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		[CLSCompliant(false)]
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		public unsafe static ulong Read(ref ulong location)
		{
			fixed (ulong* ptr = &location)
			{
				ulong* location2 = ptr;
				return (ulong)Interlocked.CompareExchange(ref *(long*)location2, 0L, 0L);
			}
		}

		// Token: 0x06003E6D RID: 15981 RVA: 0x000E8B48 File Offset: 0x000E6D48
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		public static IntPtr Read(ref IntPtr location)
		{
			IntPtr result = location;
			Thread.MemoryBarrier();
			return result;
		}

		// Token: 0x06003E6E RID: 15982 RVA: 0x000E8B60 File Offset: 0x000E6D60
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		[CLSCompliant(false)]
		public static UIntPtr Read(ref UIntPtr location)
		{
			UIntPtr result = location;
			Thread.MemoryBarrier();
			return result;
		}

		// Token: 0x06003E6F RID: 15983 RVA: 0x000E8B78 File Offset: 0x000E6D78
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		[__DynamicallyInvokable]
		public static float Read(ref float location)
		{
			float result = location;
			Thread.MemoryBarrier();
			return result;
		}

		// Token: 0x06003E70 RID: 15984 RVA: 0x000E8B8E File Offset: 0x000E6D8E
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		[__DynamicallyInvokable]
		public static double Read(ref double location)
		{
			return Interlocked.CompareExchange(ref location, 0.0, 0.0);
		}

		// Token: 0x06003E71 RID: 15985 RVA: 0x000E8BA8 File Offset: 0x000E6DA8
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		public static T Read<T>(ref T location) where T : class
		{
			T result = location;
			Thread.MemoryBarrier();
			return result;
		}

		// Token: 0x06003E72 RID: 15986 RVA: 0x000E8BC2 File Offset: 0x000E6DC2
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		[__DynamicallyInvokable]
		public static void Write(ref bool location, bool value)
		{
			Thread.MemoryBarrier();
			location = value;
		}

		// Token: 0x06003E73 RID: 15987 RVA: 0x000E8BCC File Offset: 0x000E6DCC
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public static void Write(ref sbyte location, sbyte value)
		{
			Thread.MemoryBarrier();
			location = value;
		}

		// Token: 0x06003E74 RID: 15988 RVA: 0x000E8BD6 File Offset: 0x000E6DD6
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		[__DynamicallyInvokable]
		public static void Write(ref byte location, byte value)
		{
			Thread.MemoryBarrier();
			location = value;
		}

		// Token: 0x06003E75 RID: 15989 RVA: 0x000E8BE0 File Offset: 0x000E6DE0
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		[__DynamicallyInvokable]
		public static void Write(ref short location, short value)
		{
			Thread.MemoryBarrier();
			location = value;
		}

		// Token: 0x06003E76 RID: 15990 RVA: 0x000E8BEA File Offset: 0x000E6DEA
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public static void Write(ref ushort location, ushort value)
		{
			Thread.MemoryBarrier();
			location = value;
		}

		// Token: 0x06003E77 RID: 15991 RVA: 0x000E8BF4 File Offset: 0x000E6DF4
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		[__DynamicallyInvokable]
		public static void Write(ref int location, int value)
		{
			Thread.MemoryBarrier();
			location = value;
		}

		// Token: 0x06003E78 RID: 15992 RVA: 0x000E8BFE File Offset: 0x000E6DFE
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		[CLSCompliant(false)]
		[__DynamicallyInvokable]
		public static void Write(ref uint location, uint value)
		{
			Thread.MemoryBarrier();
			location = value;
		}

		// Token: 0x06003E79 RID: 15993 RVA: 0x000E8C08 File Offset: 0x000E6E08
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		[__DynamicallyInvokable]
		public static void Write(ref long location, long value)
		{
			Interlocked.Exchange(ref location, value);
		}

		// Token: 0x06003E7A RID: 15994 RVA: 0x000E8C14 File Offset: 0x000E6E14
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		[CLSCompliant(false)]
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		public unsafe static void Write(ref ulong location, ulong value)
		{
			fixed (ulong* ptr = &location)
			{
				ulong* location2 = ptr;
				Interlocked.Exchange(ref *(long*)location2, (long)value);
			}
		}

		// Token: 0x06003E7B RID: 15995 RVA: 0x000E8C31 File Offset: 0x000E6E31
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		public static void Write(ref IntPtr location, IntPtr value)
		{
			Thread.MemoryBarrier();
			location = value;
		}

		// Token: 0x06003E7C RID: 15996 RVA: 0x000E8C3B File Offset: 0x000E6E3B
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		[CLSCompliant(false)]
		public static void Write(ref UIntPtr location, UIntPtr value)
		{
			Thread.MemoryBarrier();
			location = value;
		}

		// Token: 0x06003E7D RID: 15997 RVA: 0x000E8C45 File Offset: 0x000E6E45
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		[__DynamicallyInvokable]
		public static void Write(ref float location, float value)
		{
			Thread.MemoryBarrier();
			location = value;
		}

		// Token: 0x06003E7E RID: 15998 RVA: 0x000E8C4F File Offset: 0x000E6E4F
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		[__DynamicallyInvokable]
		public static void Write(ref double location, double value)
		{
			Interlocked.Exchange(ref location, value);
		}

		// Token: 0x06003E7F RID: 15999 RVA: 0x000E8C59 File Offset: 0x000E6E59
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		public static void Write<T>(ref T location, T value) where T : class
		{
			Thread.MemoryBarrier();
			location = value;
		}
	}
}
