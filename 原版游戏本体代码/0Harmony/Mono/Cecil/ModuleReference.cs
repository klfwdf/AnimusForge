using System;

namespace Mono.Cecil
{
	// Token: 0x02000283 RID: 643
	internal class ModuleReference : IMetadataScope, IMetadataTokenProvider
	{
		// Token: 0x170003F9 RID: 1017
		// (get) Token: 0x06000FCE RID: 4046 RVA: 0x00031354 File Offset: 0x0002F554
		// (set) Token: 0x06000FCF RID: 4047 RVA: 0x0003135C File Offset: 0x0002F55C
		public string Name
		{
			get
			{
				return this.name;
			}
			set
			{
				this.name = value;
			}
		}

		// Token: 0x170003FA RID: 1018
		// (get) Token: 0x06000FD0 RID: 4048 RVA: 0x0001B6C7 File Offset: 0x000198C7
		public virtual MetadataScopeType MetadataScopeType
		{
			get
			{
				return MetadataScopeType.ModuleReference;
			}
		}

		// Token: 0x170003FB RID: 1019
		// (get) Token: 0x06000FD1 RID: 4049 RVA: 0x00031365 File Offset: 0x0002F565
		// (set) Token: 0x06000FD2 RID: 4050 RVA: 0x0003136D File Offset: 0x0002F56D
		public MetadataToken MetadataToken
		{
			get
			{
				return this.token;
			}
			set
			{
				this.token = value;
			}
		}

		// Token: 0x06000FD3 RID: 4051 RVA: 0x00031376 File Offset: 0x0002F576
		internal ModuleReference()
		{
			this.token = new MetadataToken(TokenType.ModuleRef);
		}

		// Token: 0x06000FD4 RID: 4052 RVA: 0x0003138E File Offset: 0x0002F58E
		public ModuleReference(string name)
			: this()
		{
			this.name = name;
		}

		// Token: 0x06000FD5 RID: 4053 RVA: 0x00031354 File Offset: 0x0002F554
		public override string ToString()
		{
			return this.name;
		}

		// Token: 0x0400050D RID: 1293
		private string name;

		// Token: 0x0400050E RID: 1294
		internal MetadataToken token;
	}
}
