using System;
using Mono.Cecil.Metadata;

namespace Mono.Cecil
{
	// Token: 0x02000299 RID: 665
	internal sealed class SentinelType : TypeSpecification
	{
		// Token: 0x17000452 RID: 1106
		// (get) Token: 0x06001086 RID: 4230 RVA: 0x0001B69F File Offset: 0x0001989F
		// (set) Token: 0x06001087 RID: 4231 RVA: 0x0001C627 File Offset: 0x0001A827
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

		// Token: 0x17000453 RID: 1107
		// (get) Token: 0x06001088 RID: 4232 RVA: 0x0001B6C7 File Offset: 0x000198C7
		public override bool IsSentinel
		{
			get
			{
				return true;
			}
		}

		// Token: 0x06001089 RID: 4233 RVA: 0x00032237 File Offset: 0x00030437
		public SentinelType(TypeReference type)
			: base(type)
		{
			Mixin.CheckType(type);
			this.etype = Mono.Cecil.Metadata.ElementType.Sentinel;
		}
	}
}
