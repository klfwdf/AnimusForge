using System;
using System.Collections;

namespace System.Security
{
	// Token: 0x020001DB RID: 475
	internal class PermissionSetEnumerator : IEnumerator
	{
		// Token: 0x1700033C RID: 828
		// (get) Token: 0x06001CBB RID: 7355 RVA: 0x000624E8 File Offset: 0x000606E8
		public object Current
		{
			get
			{
				return this.enm.Current;
			}
		}

		// Token: 0x06001CBC RID: 7356 RVA: 0x000624F5 File Offset: 0x000606F5
		public bool MoveNext()
		{
			return this.enm.MoveNext();
		}

		// Token: 0x06001CBD RID: 7357 RVA: 0x00062502 File Offset: 0x00060702
		public void Reset()
		{
			this.enm.Reset();
		}

		// Token: 0x06001CBE RID: 7358 RVA: 0x0006250F File Offset: 0x0006070F
		internal PermissionSetEnumerator(PermissionSet permSet)
		{
			this.enm = new PermissionSetEnumeratorInternal(permSet);
		}

		// Token: 0x04000A09 RID: 2569
		private PermissionSetEnumeratorInternal enm;
	}
}
