using System;

namespace Mono.CompilerServices.SymbolWriter
{
	// Token: 0x0200034C RID: 844
	internal class SourceMethodImpl : IMethodDef
	{
		// Token: 0x060015BC RID: 5564 RVA: 0x000455F6 File Offset: 0x000437F6
		public SourceMethodImpl(string name, int token, int namespaceID)
		{
			this.name = name;
			this.token = token;
			this.namespaceID = namespaceID;
		}

		// Token: 0x1700057D RID: 1405
		// (get) Token: 0x060015BD RID: 5565 RVA: 0x00045613 File Offset: 0x00043813
		public string Name
		{
			get
			{
				return this.name;
			}
		}

		// Token: 0x1700057E RID: 1406
		// (get) Token: 0x060015BE RID: 5566 RVA: 0x0004561B File Offset: 0x0004381B
		public int NamespaceID
		{
			get
			{
				return this.namespaceID;
			}
		}

		// Token: 0x1700057F RID: 1407
		// (get) Token: 0x060015BF RID: 5567 RVA: 0x00045623 File Offset: 0x00043823
		public int Token
		{
			get
			{
				return this.token;
			}
		}

		// Token: 0x04000B1D RID: 2845
		private string name;

		// Token: 0x04000B1E RID: 2846
		private int token;

		// Token: 0x04000B1F RID: 2847
		private int namespaceID;
	}
}
