using System;

namespace System.Runtime.InteropServices
{
	// Token: 0x02000912 RID: 2322
	[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Event, Inherited = false)]
	[ComVisible(true)]
	[__DynamicallyInvokable]
	public sealed class DispIdAttribute : Attribute
	{
		// Token: 0x06005FF5 RID: 24565 RVA: 0x0014B6FB File Offset: 0x001498FB
		[__DynamicallyInvokable]
		public DispIdAttribute(int dispId)
		{
			this._val = dispId;
		}

		// Token: 0x170010D1 RID: 4305
		// (get) Token: 0x06005FF6 RID: 24566 RVA: 0x0014B70A File Offset: 0x0014990A
		[__DynamicallyInvokable]
		public int Value
		{
			[__DynamicallyInvokable]
			get
			{
				return this._val;
			}
		}

		// Token: 0x04002A65 RID: 10853
		internal int _val;
	}
}
