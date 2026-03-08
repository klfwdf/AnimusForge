using System;
using System.Collections.Generic;

namespace System.Diagnostics.Tracing
{
	// Token: 0x0200042D RID: 1069
	internal class EtwSession
	{
		// Token: 0x0600355B RID: 13659 RVA: 0x000CEFBC File Offset: 0x000CD1BC
		public static EtwSession GetEtwSession(int etwSessionId, bool bCreateIfNeeded = false)
		{
			if (etwSessionId < 0)
			{
				return null;
			}
			EtwSession etwSession;
			foreach (WeakReference<EtwSession> weakReference in EtwSession.s_etwSessions)
			{
				if (weakReference.TryGetTarget(out etwSession) && etwSession.m_etwSessionId == etwSessionId)
				{
					return etwSession;
				}
			}
			if (!bCreateIfNeeded)
			{
				return null;
			}
			if (EtwSession.s_etwSessions == null)
			{
				EtwSession.s_etwSessions = new List<WeakReference<EtwSession>>();
			}
			etwSession = new EtwSession(etwSessionId);
			EtwSession.s_etwSessions.Add(new WeakReference<EtwSession>(etwSession));
			if (EtwSession.s_etwSessions.Count > 16)
			{
				EtwSession.TrimGlobalList();
			}
			return etwSession;
		}

		// Token: 0x0600355C RID: 13660 RVA: 0x000CF068 File Offset: 0x000CD268
		public static void RemoveEtwSession(EtwSession etwSession)
		{
			if (EtwSession.s_etwSessions == null || etwSession == null)
			{
				return;
			}
			EtwSession.s_etwSessions.RemoveAll(delegate(WeakReference<EtwSession> wrEtwSession)
			{
				EtwSession etwSession2;
				return wrEtwSession.TryGetTarget(out etwSession2) && etwSession2.m_etwSessionId == etwSession.m_etwSessionId;
			});
			if (EtwSession.s_etwSessions.Count > 16)
			{
				EtwSession.TrimGlobalList();
			}
		}

		// Token: 0x0600355D RID: 13661 RVA: 0x000CF0BC File Offset: 0x000CD2BC
		private static void TrimGlobalList()
		{
			if (EtwSession.s_etwSessions == null)
			{
				return;
			}
			EtwSession.s_etwSessions.RemoveAll(delegate(WeakReference<EtwSession> wrEtwSession)
			{
				EtwSession etwSession;
				return !wrEtwSession.TryGetTarget(out etwSession);
			});
		}

		// Token: 0x0600355E RID: 13662 RVA: 0x000CF0F0 File Offset: 0x000CD2F0
		private EtwSession(int etwSessionId)
		{
			this.m_etwSessionId = etwSessionId;
		}

		// Token: 0x040017BB RID: 6075
		public readonly int m_etwSessionId;

		// Token: 0x040017BC RID: 6076
		public ActivityFilter m_activityFilter;

		// Token: 0x040017BD RID: 6077
		private static List<WeakReference<EtwSession>> s_etwSessions = new List<WeakReference<EtwSession>>();

		// Token: 0x040017BE RID: 6078
		private const int s_thrSessionCount = 16;
	}
}
