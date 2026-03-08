using System;

namespace System.Diagnostics.CodeAnalysis
{
	// Token: 0x020004C8 RID: 1224
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Interface | AttributeTargets.Parameter | AttributeTargets.ReturnValue | AttributeTargets.GenericParameter, Inherited = false)]
	internal sealed class DynamicallyAccessedMembersAttribute : Attribute
	{
		// Token: 0x06001B43 RID: 6979 RVA: 0x0005850E File Offset: 0x0005670E
		public DynamicallyAccessedMembersAttribute(DynamicallyAccessedMemberTypes memberTypes)
		{
			this.MemberTypes = memberTypes;
		}

		// Token: 0x170005EC RID: 1516
		// (get) Token: 0x06001B44 RID: 6980 RVA: 0x0005851D File Offset: 0x0005671D
		public DynamicallyAccessedMemberTypes MemberTypes { get; }
	}
}
