using System;

namespace System.Runtime.InteropServices
{
	// Token: 0x02000918 RID: 2328
	[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Interface | AttributeTargets.Delegate, Inherited = false)]
	[ComVisible(true)]
	[__DynamicallyInvokable]
	public sealed class ComVisibleAttribute : Attribute
	{
		// Token: 0x06005FFF RID: 24575 RVA: 0x0014B775 File Offset: 0x00149975
		[__DynamicallyInvokable]
		public ComVisibleAttribute(bool visibility)
		{
			this._val = visibility;
		}

		// Token: 0x170010D5 RID: 4309
		// (get) Token: 0x06006000 RID: 24576 RVA: 0x0014B784 File Offset: 0x00149984
		[__DynamicallyInvokable]
		public bool Value
		{
			[__DynamicallyInvokable]
			get
			{
				return this._val;
			}
		}

		// Token: 0x04002A72 RID: 10866
		internal bool _val;
	}
}
