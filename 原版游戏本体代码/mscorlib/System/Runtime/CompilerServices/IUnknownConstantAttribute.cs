using System;
using System.Runtime.InteropServices;

namespace System.Runtime.CompilerServices
{
	// Token: 0x020008FD RID: 2301
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Parameter, Inherited = false)]
	[ComVisible(true)]
	[Serializable]
	public sealed class IUnknownConstantAttribute : CustomConstantAttribute
	{
		// Token: 0x17001031 RID: 4145
		// (get) Token: 0x06005E54 RID: 24148 RVA: 0x0014B5E3 File Offset: 0x001497E3
		public override object Value
		{
			get
			{
				return new UnknownWrapper(null);
			}
		}
	}
}
