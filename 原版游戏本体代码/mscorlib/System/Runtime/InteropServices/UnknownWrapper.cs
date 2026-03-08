using System;

namespace System.Runtime.InteropServices
{
	// Token: 0x02000960 RID: 2400
	[ComVisible(true)]
	[__DynamicallyInvokable]
	[Serializable]
	public sealed class UnknownWrapper
	{
		// Token: 0x06006220 RID: 25120 RVA: 0x0014F7F7 File Offset: 0x0014D9F7
		[__DynamicallyInvokable]
		public UnknownWrapper(object obj)
		{
			this.m_WrappedObject = obj;
		}

		// Token: 0x17001114 RID: 4372
		// (get) Token: 0x06006221 RID: 25121 RVA: 0x0014F806 File Offset: 0x0014DA06
		[__DynamicallyInvokable]
		public object WrappedObject
		{
			[__DynamicallyInvokable]
			get
			{
				return this.m_WrappedObject;
			}
		}

		// Token: 0x04002B90 RID: 11152
		private object m_WrappedObject;
	}
}
