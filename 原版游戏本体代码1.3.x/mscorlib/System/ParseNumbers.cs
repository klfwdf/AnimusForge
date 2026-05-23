using System;
using System.Runtime.CompilerServices;
using System.Security;

namespace System
{
	// Token: 0x02000121 RID: 289
	internal static class ParseNumbers
	{
		// Token: 0x060010E4 RID: 4324 RVA: 0x00032DE8 File Offset: 0x00030FE8
		[SecuritySafeCritical]
		public static long StringToLong(string s, int radix, int flags)
		{
			return ParseNumbers.StringToLong(s, radix, flags, null);
		}

		// Token: 0x060010E5 RID: 4325
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		public unsafe static extern long StringToLong(string s, int radix, int flags, int* currPos);

		// Token: 0x060010E6 RID: 4326 RVA: 0x00032DF4 File Offset: 0x00030FF4
		[SecuritySafeCritical]
		public unsafe static long StringToLong(string s, int radix, int flags, ref int currPos)
		{
			fixed (int* ptr = &currPos)
			{
				int* currPos2 = ptr;
				return ParseNumbers.StringToLong(s, radix, flags, currPos2);
			}
		}

		// Token: 0x060010E7 RID: 4327 RVA: 0x00032E0F File Offset: 0x0003100F
		[SecuritySafeCritical]
		public static int StringToInt(string s, int radix, int flags)
		{
			return ParseNumbers.StringToInt(s, radix, flags, null);
		}

		// Token: 0x060010E8 RID: 4328
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		public unsafe static extern int StringToInt(string s, int radix, int flags, int* currPos);

		// Token: 0x060010E9 RID: 4329 RVA: 0x00032E1C File Offset: 0x0003101C
		[SecuritySafeCritical]
		public unsafe static int StringToInt(string s, int radix, int flags, ref int currPos)
		{
			fixed (int* ptr = &currPos)
			{
				int* currPos2 = ptr;
				return ParseNumbers.StringToInt(s, radix, flags, currPos2);
			}
		}

		// Token: 0x060010EA RID: 4330
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		public static extern string IntToString(int l, int radix, int width, char paddingChar, int flags);

		// Token: 0x060010EB RID: 4331
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		public static extern string LongToString(long l, int radix, int width, char paddingChar, int flags);

		// Token: 0x040005DB RID: 1499
		internal const int PrintAsI1 = 64;

		// Token: 0x040005DC RID: 1500
		internal const int PrintAsI2 = 128;

		// Token: 0x040005DD RID: 1501
		internal const int PrintAsI4 = 256;

		// Token: 0x040005DE RID: 1502
		internal const int TreatAsUnsigned = 512;

		// Token: 0x040005DF RID: 1503
		internal const int TreatAsI1 = 1024;

		// Token: 0x040005E0 RID: 1504
		internal const int TreatAsI2 = 2048;

		// Token: 0x040005E1 RID: 1505
		internal const int IsTight = 4096;

		// Token: 0x040005E2 RID: 1506
		internal const int NoSpace = 8192;
	}
}
