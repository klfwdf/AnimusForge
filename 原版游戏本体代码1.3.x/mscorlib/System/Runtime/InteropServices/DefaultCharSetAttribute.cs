using System;

namespace System.Runtime.InteropServices
{
	// Token: 0x0200093F RID: 2367
	[AttributeUsage(AttributeTargets.Module, Inherited = false)]
	[ComVisible(true)]
	[__DynamicallyInvokable]
	public sealed class DefaultCharSetAttribute : Attribute
	{
		// Token: 0x0600605F RID: 24671 RVA: 0x0014C058 File Offset: 0x0014A258
		[__DynamicallyInvokable]
		public DefaultCharSetAttribute(CharSet charSet)
		{
			this._CharSet = charSet;
		}

		// Token: 0x170010F3 RID: 4339
		// (get) Token: 0x06006060 RID: 24672 RVA: 0x0014C067 File Offset: 0x0014A267
		[__DynamicallyInvokable]
		public CharSet CharSet
		{
			[__DynamicallyInvokable]
			get
			{
				return this._CharSet;
			}
		}

		// Token: 0x04002B31 RID: 11057
		internal CharSet _CharSet;
	}
}
