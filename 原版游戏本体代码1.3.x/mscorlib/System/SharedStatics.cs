using System;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Security;
using System.Security.Util;
using System.Threading;

namespace System
{
	// Token: 0x0200013E RID: 318
	internal sealed class SharedStatics
	{
		// Token: 0x0600130E RID: 4878 RVA: 0x000382D5 File Offset: 0x000364D5
		private SharedStatics()
		{
		}

		// Token: 0x170001FF RID: 511
		// (get) Token: 0x0600130F RID: 4879 RVA: 0x000382E0 File Offset: 0x000364E0
		public static string Remoting_Identity_IDGuid
		{
			[SecuritySafeCritical]
			get
			{
				if (SharedStatics._sharedStatics._Remoting_Identity_IDGuid == null)
				{
					bool flag = false;
					RuntimeHelpers.PrepareConstrainedRegions();
					try
					{
						Monitor.Enter(SharedStatics._sharedStatics, ref flag);
						if (SharedStatics._sharedStatics._Remoting_Identity_IDGuid == null)
						{
							SharedStatics._sharedStatics._Remoting_Identity_IDGuid = Guid.NewGuid().ToString().Replace('-', '_');
						}
					}
					finally
					{
						if (flag)
						{
							Monitor.Exit(SharedStatics._sharedStatics);
						}
					}
				}
				return SharedStatics._sharedStatics._Remoting_Identity_IDGuid;
			}
		}

		// Token: 0x06001310 RID: 4880 RVA: 0x00038370 File Offset: 0x00036570
		[SecuritySafeCritical]
		public static Tokenizer.StringMaker GetSharedStringMaker()
		{
			Tokenizer.StringMaker stringMaker = null;
			bool flag = false;
			RuntimeHelpers.PrepareConstrainedRegions();
			try
			{
				Monitor.Enter(SharedStatics._sharedStatics, ref flag);
				if (SharedStatics._sharedStatics._maker != null)
				{
					stringMaker = SharedStatics._sharedStatics._maker;
					SharedStatics._sharedStatics._maker = null;
				}
			}
			finally
			{
				if (flag)
				{
					Monitor.Exit(SharedStatics._sharedStatics);
				}
			}
			if (stringMaker == null)
			{
				stringMaker = new Tokenizer.StringMaker();
			}
			return stringMaker;
		}

		// Token: 0x06001311 RID: 4881 RVA: 0x000383E0 File Offset: 0x000365E0
		[SecuritySafeCritical]
		public static void ReleaseSharedStringMaker(ref Tokenizer.StringMaker maker)
		{
			bool flag = false;
			RuntimeHelpers.PrepareConstrainedRegions();
			try
			{
				Monitor.Enter(SharedStatics._sharedStatics, ref flag);
				SharedStatics._sharedStatics._maker = maker;
				maker = null;
			}
			finally
			{
				if (flag)
				{
					Monitor.Exit(SharedStatics._sharedStatics);
				}
			}
		}

		// Token: 0x06001312 RID: 4882 RVA: 0x00038430 File Offset: 0x00036630
		internal static int Remoting_Identity_GetNextSeqNum()
		{
			return Interlocked.Increment(ref SharedStatics._sharedStatics._Remoting_Identity_IDSeqNum);
		}

		// Token: 0x06001313 RID: 4883 RVA: 0x00038441 File Offset: 0x00036641
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		internal static long AddMemoryFailPointReservation(long size)
		{
			return Interlocked.Add(ref SharedStatics._sharedStatics._memFailPointReservedMemory, size);
		}

		// Token: 0x17000200 RID: 512
		// (get) Token: 0x06001314 RID: 4884 RVA: 0x00038453 File Offset: 0x00036653
		internal static ulong MemoryFailPointReservedMemory
		{
			get
			{
				return (ulong)Volatile.Read(ref SharedStatics._sharedStatics._memFailPointReservedMemory);
			}
		}

		// Token: 0x04000683 RID: 1667
		private static SharedStatics _sharedStatics;

		// Token: 0x04000684 RID: 1668
		private volatile string _Remoting_Identity_IDGuid;

		// Token: 0x04000685 RID: 1669
		private Tokenizer.StringMaker _maker;

		// Token: 0x04000686 RID: 1670
		private int _Remoting_Identity_IDSeqNum;

		// Token: 0x04000687 RID: 1671
		private long _memFailPointReservedMemory;
	}
}
