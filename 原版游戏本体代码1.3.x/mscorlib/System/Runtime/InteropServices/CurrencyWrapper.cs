using System;

namespace System.Runtime.InteropServices
{
	// Token: 0x0200095D RID: 2397
	[ComVisible(true)]
	[__DynamicallyInvokable]
	[Serializable]
	public sealed class CurrencyWrapper
	{
		// Token: 0x06006217 RID: 25111 RVA: 0x0014F71E File Offset: 0x0014D91E
		[__DynamicallyInvokable]
		public CurrencyWrapper(decimal obj)
		{
			this.m_WrappedObject = obj;
		}

		// Token: 0x06006218 RID: 25112 RVA: 0x0014F72D File Offset: 0x0014D92D
		[__DynamicallyInvokable]
		public CurrencyWrapper(object obj)
		{
			if (!(obj is decimal))
			{
				throw new ArgumentException(Environment.GetResourceString("Arg_MustBeDecimal"), "obj");
			}
			this.m_WrappedObject = (decimal)obj;
		}

		// Token: 0x17001111 RID: 4369
		// (get) Token: 0x06006219 RID: 25113 RVA: 0x0014F75E File Offset: 0x0014D95E
		[__DynamicallyInvokable]
		public decimal WrappedObject
		{
			[__DynamicallyInvokable]
			get
			{
				return this.m_WrappedObject;
			}
		}

		// Token: 0x04002B8D RID: 11149
		private decimal m_WrappedObject;
	}
}
