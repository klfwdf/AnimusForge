using System;
using Mono.Cecil.Metadata;

namespace Mono.Cecil
{
	// Token: 0x0200028C RID: 652
	internal sealed class PointerType : TypeSpecification
	{
		// Token: 0x17000424 RID: 1060
		// (get) Token: 0x0600102E RID: 4142 RVA: 0x00031A4D File Offset: 0x0002FC4D
		public override string Name
		{
			get
			{
				return base.Name + "*";
			}
		}

		// Token: 0x17000425 RID: 1061
		// (get) Token: 0x0600102F RID: 4143 RVA: 0x00031A5F File Offset: 0x0002FC5F
		public override string FullName
		{
			get
			{
				return base.FullName + "*";
			}
		}

		// Token: 0x17000426 RID: 1062
		// (get) Token: 0x06001030 RID: 4144 RVA: 0x0001B69F File Offset: 0x0001989F
		// (set) Token: 0x06001031 RID: 4145 RVA: 0x0001C627 File Offset: 0x0001A827
		public override bool IsValueType
		{
			get
			{
				return false;
			}
			set
			{
				throw new InvalidOperationException();
			}
		}

		// Token: 0x17000427 RID: 1063
		// (get) Token: 0x06001032 RID: 4146 RVA: 0x0001B6C7 File Offset: 0x000198C7
		public override bool IsPointer
		{
			get
			{
				return true;
			}
		}

		// Token: 0x06001033 RID: 4147 RVA: 0x00031A71 File Offset: 0x0002FC71
		public PointerType(TypeReference type)
			: base(type)
		{
			Mixin.CheckType(type);
			this.etype = Mono.Cecil.Metadata.ElementType.Ptr;
		}
	}
}
