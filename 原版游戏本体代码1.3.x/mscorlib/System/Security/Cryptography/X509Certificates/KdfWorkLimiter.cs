using System;

namespace System.Security.Cryptography.X509Certificates
{
	// Token: 0x020002BB RID: 699
	internal static class KdfWorkLimiter
	{
		// Token: 0x060024F9 RID: 9465 RVA: 0x00085C34 File Offset: 0x00083E34
		internal static void SetIterationLimit(ulong workLimit)
		{
			KdfWorkLimiter.t_State = new KdfWorkLimiter.State
			{
				RemainingAllowedWork = workLimit
			};
		}

		// Token: 0x060024FA RID: 9466 RVA: 0x00085C54 File Offset: 0x00083E54
		internal static bool WasWorkLimitExceeded()
		{
			return KdfWorkLimiter.t_State.WorkLimitWasExceeded;
		}

		// Token: 0x060024FB RID: 9467 RVA: 0x00085C60 File Offset: 0x00083E60
		internal static void ResetIterationLimit()
		{
			KdfWorkLimiter.t_State = null;
		}

		// Token: 0x060024FC RID: 9468 RVA: 0x00085C68 File Offset: 0x00083E68
		internal static void RecordIterations(int workCount)
		{
			KdfWorkLimiter.RecordIterations((long)workCount);
		}

		// Token: 0x060024FD RID: 9469 RVA: 0x00085C74 File Offset: 0x00083E74
		internal static void RecordIterations(long workCount)
		{
			KdfWorkLimiter.State state = KdfWorkLimiter.t_State;
			bool flag = false;
			checked
			{
				try
				{
					if (!state.WorkLimitWasExceeded)
					{
						state.RemainingAllowedWork -= (ulong)workCount;
						flag = true;
					}
				}
				finally
				{
					if (!flag)
					{
						state.RemainingAllowedWork = 0UL;
						state.WorkLimitWasExceeded = true;
						throw new CryptographicException();
					}
				}
			}
		}

		// Token: 0x04000DCE RID: 3534
		[ThreadStatic]
		private static KdfWorkLimiter.State t_State;

		// Token: 0x02000B50 RID: 2896
		private sealed class State
		{
			// Token: 0x040033DF RID: 13279
			internal ulong RemainingAllowedWork;

			// Token: 0x040033E0 RID: 13280
			internal bool WorkLimitWasExceeded;
		}
	}
}
