using System;
using System.Collections;

namespace System.Security
{
	// Token: 0x020001E7 RID: 487
	internal sealed class ReadOnlyPermissionSetEnumerator : IEnumerator
	{
		// Token: 0x06001D84 RID: 7556 RVA: 0x00066DC0 File Offset: 0x00064FC0
		internal ReadOnlyPermissionSetEnumerator(IEnumerator permissionSetEnumerator)
		{
			this.m_permissionSetEnumerator = permissionSetEnumerator;
		}

		// Token: 0x17000347 RID: 839
		// (get) Token: 0x06001D85 RID: 7557 RVA: 0x00066DD0 File Offset: 0x00064FD0
		public object Current
		{
			get
			{
				IPermission permission = this.m_permissionSetEnumerator.Current as IPermission;
				if (permission == null)
				{
					return null;
				}
				return permission.Copy();
			}
		}

		// Token: 0x06001D86 RID: 7558 RVA: 0x00066DF9 File Offset: 0x00064FF9
		public bool MoveNext()
		{
			return this.m_permissionSetEnumerator.MoveNext();
		}

		// Token: 0x06001D87 RID: 7559 RVA: 0x00066E06 File Offset: 0x00065006
		public void Reset()
		{
			this.m_permissionSetEnumerator.Reset();
		}

		// Token: 0x04000A49 RID: 2633
		private IEnumerator m_permissionSetEnumerator;
	}
}
