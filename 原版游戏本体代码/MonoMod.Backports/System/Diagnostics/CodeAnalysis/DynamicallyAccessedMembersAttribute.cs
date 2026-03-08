using System;

namespace System.Diagnostics.CodeAnalysis
{
	// Token: 0x02000053 RID: 83
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Interface | AttributeTargets.Parameter | AttributeTargets.ReturnValue | AttributeTargets.GenericParameter, Inherited = false)]
	public sealed class DynamicallyAccessedMembersAttribute : Attribute
	{
		// Token: 0x060002B8 RID: 696 RVA: 0x0000CC8A File Offset: 0x0000AE8A
		public DynamicallyAccessedMembersAttribute(DynamicallyAccessedMemberTypes memberTypes)
		{
			this.MemberTypes = memberTypes;
		}

		// Token: 0x17000041 RID: 65
		// (get) Token: 0x060002B9 RID: 697 RVA: 0x0000CC99 File Offset: 0x0000AE99
		public DynamicallyAccessedMemberTypes MemberTypes { get; }
	}
}
