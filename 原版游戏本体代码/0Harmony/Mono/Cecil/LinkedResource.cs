using System;

namespace Mono.Cecil
{
	// Token: 0x0200025A RID: 602
	internal sealed class LinkedResource : Resource
	{
		// Token: 0x17000320 RID: 800
		// (get) Token: 0x06000D9E RID: 3486 RVA: 0x0002D635 File Offset: 0x0002B835
		public byte[] Hash
		{
			get
			{
				return this.hash;
			}
		}

		// Token: 0x17000321 RID: 801
		// (get) Token: 0x06000D9F RID: 3487 RVA: 0x0002D63D File Offset: 0x0002B83D
		// (set) Token: 0x06000DA0 RID: 3488 RVA: 0x0002D645 File Offset: 0x0002B845
		public string File
		{
			get
			{
				return this.file;
			}
			set
			{
				this.file = value;
			}
		}

		// Token: 0x17000322 RID: 802
		// (get) Token: 0x06000DA1 RID: 3489 RVA: 0x0001B69F File Offset: 0x0001989F
		public override ResourceType ResourceType
		{
			get
			{
				return ResourceType.Linked;
			}
		}

		// Token: 0x06000DA2 RID: 3490 RVA: 0x0001EBDE File Offset: 0x0001CDDE
		public LinkedResource(string name, ManifestResourceAttributes flags)
			: base(name, flags)
		{
		}

		// Token: 0x06000DA3 RID: 3491 RVA: 0x0002D64E File Offset: 0x0002B84E
		public LinkedResource(string name, ManifestResourceAttributes flags, string file)
			: base(name, flags)
		{
			this.file = file;
		}

		// Token: 0x04000403 RID: 1027
		internal byte[] hash;

		// Token: 0x04000404 RID: 1028
		private string file;
	}
}
