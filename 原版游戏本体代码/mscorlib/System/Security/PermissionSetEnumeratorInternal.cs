using System;
using System.Security.Util;

namespace System.Security
{
	// Token: 0x020001DC RID: 476
	internal struct PermissionSetEnumeratorInternal
	{
		// Token: 0x1700033D RID: 829
		// (get) Token: 0x06001CBF RID: 7359 RVA: 0x00062523 File Offset: 0x00060723
		public object Current
		{
			get
			{
				return this.enm.Current;
			}
		}

		// Token: 0x06001CC0 RID: 7360 RVA: 0x00062530 File Offset: 0x00060730
		internal PermissionSetEnumeratorInternal(PermissionSet permSet)
		{
			this.m_permSet = permSet;
			this.enm = new TokenBasedSetEnumerator(permSet.m_permSet);
		}

		// Token: 0x06001CC1 RID: 7361 RVA: 0x0006254A File Offset: 0x0006074A
		public int GetCurrentIndex()
		{
			return this.enm.Index;
		}

		// Token: 0x06001CC2 RID: 7362 RVA: 0x00062557 File Offset: 0x00060757
		public void Reset()
		{
			this.enm.Reset();
		}

		// Token: 0x06001CC3 RID: 7363 RVA: 0x00062564 File Offset: 0x00060764
		public bool MoveNext()
		{
			while (this.enm.MoveNext())
			{
				object current = this.enm.Current;
				IPermission permission = current as IPermission;
				if (permission != null)
				{
					this.enm.Current = permission;
					return true;
				}
				SecurityElement securityElement = current as SecurityElement;
				if (securityElement != null)
				{
					permission = this.m_permSet.CreatePermission(securityElement, this.enm.Index);
					if (permission != null)
					{
						this.enm.Current = permission;
						return true;
					}
				}
			}
			return false;
		}

		// Token: 0x04000A0A RID: 2570
		private PermissionSet m_permSet;

		// Token: 0x04000A0B RID: 2571
		private TokenBasedSetEnumerator enm;
	}
}
