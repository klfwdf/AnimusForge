using System;

namespace Mono.Cecil
{
	// Token: 0x02000288 RID: 648
	internal abstract class ParameterReference : IMetadataTokenProvider
	{
		// Token: 0x1700040C RID: 1036
		// (get) Token: 0x06000FFB RID: 4091 RVA: 0x000316D6 File Offset: 0x0002F8D6
		// (set) Token: 0x06000FFC RID: 4092 RVA: 0x000316DE File Offset: 0x0002F8DE
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

		// Token: 0x1700040D RID: 1037
		// (get) Token: 0x06000FFD RID: 4093 RVA: 0x000316E7 File Offset: 0x0002F8E7
		public int Index
		{
			get
			{
				return this.index;
			}
		}

		// Token: 0x1700040E RID: 1038
		// (get) Token: 0x06000FFE RID: 4094 RVA: 0x000316EF File Offset: 0x0002F8EF
		// (set) Token: 0x06000FFF RID: 4095 RVA: 0x000316F7 File Offset: 0x0002F8F7
		public TypeReference ParameterType
		{
			get
			{
				return this.parameter_type;
			}
			set
			{
				this.parameter_type = value;
			}
		}

		// Token: 0x1700040F RID: 1039
		// (get) Token: 0x06001000 RID: 4096 RVA: 0x00031700 File Offset: 0x0002F900
		// (set) Token: 0x06001001 RID: 4097 RVA: 0x00031708 File Offset: 0x0002F908
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

		// Token: 0x06001002 RID: 4098 RVA: 0x00031711 File Offset: 0x0002F911
		internal ParameterReference(string name, TypeReference parameterType)
		{
			if (parameterType == null)
			{
				throw new ArgumentNullException("parameterType");
			}
			this.name = name ?? string.Empty;
			this.parameter_type = parameterType;
		}

		// Token: 0x06001003 RID: 4099 RVA: 0x000316D6 File Offset: 0x0002F8D6
		public override string ToString()
		{
			return this.name;
		}

		// Token: 0x06001004 RID: 4100
		public abstract ParameterDefinition Resolve();

		// Token: 0x04000545 RID: 1349
		private string name;

		// Token: 0x04000546 RID: 1350
		internal int index = -1;

		// Token: 0x04000547 RID: 1351
		protected TypeReference parameter_type;

		// Token: 0x04000548 RID: 1352
		internal MetadataToken token;
	}
}
