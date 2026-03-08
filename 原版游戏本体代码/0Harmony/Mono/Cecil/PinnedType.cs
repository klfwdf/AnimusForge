using System;
using Mono.Cecil.Metadata;

namespace Mono.Cecil
{
	// Token: 0x02000289 RID: 649
	internal sealed class PinnedType : TypeSpecification
	{
		// Token: 0x17000410 RID: 1040
		// (get) Token: 0x06001005 RID: 4101 RVA: 0x0001B69F File Offset: 0x0001989F
		// (set) Token: 0x06001006 RID: 4102 RVA: 0x0001C627 File Offset: 0x0001A827
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

		// Token: 0x17000411 RID: 1041
		// (get) Token: 0x06001007 RID: 4103 RVA: 0x0001B6C7 File Offset: 0x000198C7
		public override bool IsPinned
		{
			get
			{
				return true;
			}
		}

		// Token: 0x06001008 RID: 4104 RVA: 0x00031745 File Offset: 0x0002F945
		public PinnedType(TypeReference type)
			: base(type)
		{
			Mixin.CheckType(type);
			this.etype = Mono.Cecil.Metadata.ElementType.Pinned;
		}
	}
}
