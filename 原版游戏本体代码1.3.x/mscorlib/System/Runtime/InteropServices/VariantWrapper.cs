using System;

namespace System.Runtime.InteropServices
{
	// Token: 0x02000961 RID: 2401
	[__DynamicallyInvokable]
	[Serializable]
	public sealed class VariantWrapper
	{
		// Token: 0x06006222 RID: 25122 RVA: 0x0014F80E File Offset: 0x0014DA0E
		[__DynamicallyInvokable]
		public VariantWrapper(object obj)
		{
			this.m_WrappedObject = obj;
		}

		// Token: 0x17001115 RID: 4373
		// (get) Token: 0x06006223 RID: 25123 RVA: 0x0014F81D File Offset: 0x0014DA1D
		[__DynamicallyInvokable]
		public object WrappedObject
		{
			[__DynamicallyInvokable]
			get
			{
				return this.m_WrappedObject;
			}
		}

		// Token: 0x04002B91 RID: 11153
		private object m_WrappedObject;
	}
}
