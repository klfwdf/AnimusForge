using System;

namespace Mono.Cecil.Cil
{
	// Token: 0x02000312 RID: 786
	internal sealed class ImportTarget
	{
		// Token: 0x17000531 RID: 1329
		// (get) Token: 0x0600146C RID: 5228 RVA: 0x00041024 File Offset: 0x0003F224
		// (set) Token: 0x0600146D RID: 5229 RVA: 0x0004102C File Offset: 0x0003F22C
		public string Namespace
		{
			get
			{
				return this.@namespace;
			}
			set
			{
				this.@namespace = value;
			}
		}

		// Token: 0x17000532 RID: 1330
		// (get) Token: 0x0600146E RID: 5230 RVA: 0x00041035 File Offset: 0x0003F235
		// (set) Token: 0x0600146F RID: 5231 RVA: 0x0004103D File Offset: 0x0003F23D
		public TypeReference Type
		{
			get
			{
				return this.type;
			}
			set
			{
				this.type = value;
			}
		}

		// Token: 0x17000533 RID: 1331
		// (get) Token: 0x06001470 RID: 5232 RVA: 0x00041046 File Offset: 0x0003F246
		// (set) Token: 0x06001471 RID: 5233 RVA: 0x0004104E File Offset: 0x0003F24E
		public AssemblyNameReference AssemblyReference
		{
			get
			{
				return this.reference;
			}
			set
			{
				this.reference = value;
			}
		}

		// Token: 0x17000534 RID: 1332
		// (get) Token: 0x06001472 RID: 5234 RVA: 0x00041057 File Offset: 0x0003F257
		// (set) Token: 0x06001473 RID: 5235 RVA: 0x0004105F File Offset: 0x0003F25F
		public string Alias
		{
			get
			{
				return this.alias;
			}
			set
			{
				this.alias = value;
			}
		}

		// Token: 0x17000535 RID: 1333
		// (get) Token: 0x06001474 RID: 5236 RVA: 0x00041068 File Offset: 0x0003F268
		// (set) Token: 0x06001475 RID: 5237 RVA: 0x00041070 File Offset: 0x0003F270
		public ImportTargetKind Kind
		{
			get
			{
				return this.kind;
			}
			set
			{
				this.kind = value;
			}
		}

		// Token: 0x06001476 RID: 5238 RVA: 0x00041079 File Offset: 0x0003F279
		public ImportTarget(ImportTargetKind kind)
		{
			this.kind = kind;
		}

		// Token: 0x04000A37 RID: 2615
		internal ImportTargetKind kind;

		// Token: 0x04000A38 RID: 2616
		internal string @namespace;

		// Token: 0x04000A39 RID: 2617
		internal TypeReference type;

		// Token: 0x04000A3A RID: 2618
		internal AssemblyNameReference reference;

		// Token: 0x04000A3B RID: 2619
		internal string alias;
	}
}
