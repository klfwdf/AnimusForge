using System;
using System.Collections;
using System.Runtime.InteropServices;

namespace System.Security.Policy
{
	// Token: 0x02000348 RID: 840
	[ComVisible(true)]
	public sealed class ApplicationTrustEnumerator : IEnumerator
	{
		// Token: 0x060029C2 RID: 10690 RVA: 0x0009A659 File Offset: 0x00098859
		private ApplicationTrustEnumerator()
		{
		}

		// Token: 0x060029C3 RID: 10691 RVA: 0x0009A661 File Offset: 0x00098861
		[SecurityCritical]
		internal ApplicationTrustEnumerator(ApplicationTrustCollection trusts)
		{
			this.m_trusts = trusts;
			this.m_current = -1;
		}

		// Token: 0x1700057C RID: 1404
		// (get) Token: 0x060029C4 RID: 10692 RVA: 0x0009A677 File Offset: 0x00098877
		public ApplicationTrust Current
		{
			[SecuritySafeCritical]
			get
			{
				return this.m_trusts[this.m_current];
			}
		}

		// Token: 0x1700057D RID: 1405
		// (get) Token: 0x060029C5 RID: 10693 RVA: 0x0009A68A File Offset: 0x0009888A
		object IEnumerator.Current
		{
			[SecuritySafeCritical]
			get
			{
				return this.m_trusts[this.m_current];
			}
		}

		// Token: 0x060029C6 RID: 10694 RVA: 0x0009A69D File Offset: 0x0009889D
		[SecuritySafeCritical]
		public bool MoveNext()
		{
			if (this.m_current == this.m_trusts.Count - 1)
			{
				return false;
			}
			this.m_current++;
			return true;
		}

		// Token: 0x060029C7 RID: 10695 RVA: 0x0009A6C5 File Offset: 0x000988C5
		public void Reset()
		{
			this.m_current = -1;
		}

		// Token: 0x04001124 RID: 4388
		[SecurityCritical]
		private ApplicationTrustCollection m_trusts;

		// Token: 0x04001125 RID: 4389
		private int m_current;
	}
}
