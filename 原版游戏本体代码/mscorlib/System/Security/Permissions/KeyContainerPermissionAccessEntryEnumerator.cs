using System;
using System.Collections;
using System.Runtime.InteropServices;

namespace System.Security.Permissions
{
	// Token: 0x02000317 RID: 791
	[ComVisible(true)]
	[Serializable]
	public sealed class KeyContainerPermissionAccessEntryEnumerator : IEnumerator
	{
		// Token: 0x060027DB RID: 10203 RVA: 0x00090EA2 File Offset: 0x0008F0A2
		private KeyContainerPermissionAccessEntryEnumerator()
		{
		}

		// Token: 0x060027DC RID: 10204 RVA: 0x00090EAA File Offset: 0x0008F0AA
		internal KeyContainerPermissionAccessEntryEnumerator(KeyContainerPermissionAccessEntryCollection entries)
		{
			this.m_entries = entries;
			this.m_current = -1;
		}

		// Token: 0x1700051A RID: 1306
		// (get) Token: 0x060027DD RID: 10205 RVA: 0x00090EC0 File Offset: 0x0008F0C0
		public KeyContainerPermissionAccessEntry Current
		{
			get
			{
				return this.m_entries[this.m_current];
			}
		}

		// Token: 0x1700051B RID: 1307
		// (get) Token: 0x060027DE RID: 10206 RVA: 0x00090ED3 File Offset: 0x0008F0D3
		object IEnumerator.Current
		{
			get
			{
				return this.m_entries[this.m_current];
			}
		}

		// Token: 0x060027DF RID: 10207 RVA: 0x00090EE6 File Offset: 0x0008F0E6
		public bool MoveNext()
		{
			if (this.m_current == this.m_entries.Count - 1)
			{
				return false;
			}
			this.m_current++;
			return true;
		}

		// Token: 0x060027E0 RID: 10208 RVA: 0x00090F0E File Offset: 0x0008F10E
		public void Reset()
		{
			this.m_current = -1;
		}

		// Token: 0x04000F72 RID: 3954
		private KeyContainerPermissionAccessEntryCollection m_entries;

		// Token: 0x04000F73 RID: 3955
		private int m_current;
	}
}
