using System;

namespace Mono.Cecil
{
	// Token: 0x02000293 RID: 659
	internal abstract class Resource
	{
		// Token: 0x17000440 RID: 1088
		// (get) Token: 0x06001063 RID: 4195 RVA: 0x00031F65 File Offset: 0x00030165
		// (set) Token: 0x06001064 RID: 4196 RVA: 0x00031F6D File Offset: 0x0003016D
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

		// Token: 0x17000441 RID: 1089
		// (get) Token: 0x06001065 RID: 4197 RVA: 0x00031F76 File Offset: 0x00030176
		// (set) Token: 0x06001066 RID: 4198 RVA: 0x00031F7E File Offset: 0x0003017E
		public ManifestResourceAttributes Attributes
		{
			get
			{
				return (ManifestResourceAttributes)this.attributes;
			}
			set
			{
				this.attributes = (uint)value;
			}
		}

		// Token: 0x17000442 RID: 1090
		// (get) Token: 0x06001067 RID: 4199
		public abstract ResourceType ResourceType { get; }

		// Token: 0x17000443 RID: 1091
		// (get) Token: 0x06001068 RID: 4200 RVA: 0x00031F87 File Offset: 0x00030187
		// (set) Token: 0x06001069 RID: 4201 RVA: 0x00031F96 File Offset: 0x00030196
		public bool IsPublic
		{
			get
			{
				return this.attributes.GetMaskedAttributes(7U, 1U);
			}
			set
			{
				this.attributes = this.attributes.SetMaskedAttributes(7U, 1U, value);
			}
		}

		// Token: 0x17000444 RID: 1092
		// (get) Token: 0x0600106A RID: 4202 RVA: 0x00031FAC File Offset: 0x000301AC
		// (set) Token: 0x0600106B RID: 4203 RVA: 0x00031FBB File Offset: 0x000301BB
		public bool IsPrivate
		{
			get
			{
				return this.attributes.GetMaskedAttributes(7U, 2U);
			}
			set
			{
				this.attributes = this.attributes.SetMaskedAttributes(7U, 2U, value);
			}
		}

		// Token: 0x0600106C RID: 4204 RVA: 0x00031FD1 File Offset: 0x000301D1
		internal Resource(string name, ManifestResourceAttributes attributes)
		{
			this.name = name;
			this.attributes = (uint)attributes;
		}

		// Token: 0x04000574 RID: 1396
		private string name;

		// Token: 0x04000575 RID: 1397
		private uint attributes;
	}
}
