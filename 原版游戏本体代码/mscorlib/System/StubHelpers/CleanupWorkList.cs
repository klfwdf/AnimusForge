using System;
using System.Collections.Generic;
using System.Runtime.ConstrainedExecution;
using System.Security;

namespace System.StubHelpers
{
	// Token: 0x020005AB RID: 1451
	[ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
	[SecurityCritical]
	internal sealed class CleanupWorkList
	{
		// Token: 0x0600432F RID: 17199 RVA: 0x000FA3C4 File Offset: 0x000F85C4
		public void Add(CleanupWorkListElement elem)
		{
			this.m_list.Add(elem);
		}

		// Token: 0x06004330 RID: 17200 RVA: 0x000FA3D4 File Offset: 0x000F85D4
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		public void Destroy()
		{
			for (int i = this.m_list.Count - 1; i >= 0; i--)
			{
				if (this.m_list[i].m_owned)
				{
					StubHelpers.SafeHandleRelease(this.m_list[i].m_handle);
				}
			}
		}

		// Token: 0x04001BEE RID: 7150
		private List<CleanupWorkListElement> m_list = new List<CleanupWorkListElement>();
	}
}
