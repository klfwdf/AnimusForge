using System;
using System.Runtime.InteropServices;

namespace System.Runtime.CompilerServices
{
	// Token: 0x020008FC RID: 2300
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Parameter, Inherited = false)]
	[ComVisible(true)]
	[Serializable]
	public sealed class IDispatchConstantAttribute : CustomConstantAttribute
	{
		// Token: 0x17001030 RID: 4144
		// (get) Token: 0x06005E52 RID: 24146 RVA: 0x0014B5D3 File Offset: 0x001497D3
		public override object Value
		{
			get
			{
				return new DispatchWrapper(null);
			}
		}
	}
}
