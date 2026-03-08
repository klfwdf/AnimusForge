using System;
using Mono.Cecil.Metadata;

namespace Mono.Cecil
{
	// Token: 0x02000291 RID: 657
	internal sealed class ByReferenceType : TypeSpecification
	{
		// Token: 0x1700043C RID: 1084
		// (get) Token: 0x0600105D RID: 4189 RVA: 0x00031F2A File Offset: 0x0003012A
		public override string Name
		{
			get
			{
				return base.Name + "&";
			}
		}

		// Token: 0x1700043D RID: 1085
		// (get) Token: 0x0600105E RID: 4190 RVA: 0x00031F3C File Offset: 0x0003013C
		public override string FullName
		{
			get
			{
				return base.FullName + "&";
			}
		}

		// Token: 0x1700043E RID: 1086
		// (get) Token: 0x0600105F RID: 4191 RVA: 0x0001B69F File Offset: 0x0001989F
		// (set) Token: 0x06001060 RID: 4192 RVA: 0x0001C627 File Offset: 0x0001A827
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

		// Token: 0x1700043F RID: 1087
		// (get) Token: 0x06001061 RID: 4193 RVA: 0x0001B6C7 File Offset: 0x000198C7
		public override bool IsByReference
		{
			get
			{
				return true;
			}
		}

		// Token: 0x06001062 RID: 4194 RVA: 0x00031F4E File Offset: 0x0003014E
		public ByReferenceType(TypeReference type)
			: base(type)
		{
			Mixin.CheckType(type);
			this.etype = Mono.Cecil.Metadata.ElementType.ByRef;
		}
	}
}
