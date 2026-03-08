using System;

namespace Mono.Cecil
{
	// Token: 0x0200025F RID: 607
	internal sealed class SafeArrayMarshalInfo : MarshalInfo
	{
		// Token: 0x1700032C RID: 812
		// (get) Token: 0x06000DB9 RID: 3513 RVA: 0x0002D738 File Offset: 0x0002B938
		// (set) Token: 0x06000DBA RID: 3514 RVA: 0x0002D740 File Offset: 0x0002B940
		public VariantType ElementType
		{
			get
			{
				return this.element_type;
			}
			set
			{
				this.element_type = value;
			}
		}

		// Token: 0x06000DBB RID: 3515 RVA: 0x0002D749 File Offset: 0x0002B949
		public SafeArrayMarshalInfo()
			: base(NativeType.SafeArray)
		{
			this.element_type = VariantType.None;
		}

		// Token: 0x04000412 RID: 1042
		internal VariantType element_type;
	}
}
