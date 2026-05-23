using System;

namespace System.Runtime.InteropServices
{
	// Token: 0x0200093B RID: 2363
	[AttributeUsage(AttributeTargets.Interface, Inherited = false)]
	[ComVisible(true)]
	[__DynamicallyInvokable]
	public sealed class ComEventInterfaceAttribute : Attribute
	{
		// Token: 0x06006052 RID: 24658 RVA: 0x0014BFB0 File Offset: 0x0014A1B0
		[__DynamicallyInvokable]
		public ComEventInterfaceAttribute(Type SourceInterface, Type EventProvider)
		{
			this._SourceInterface = SourceInterface;
			this._EventProvider = EventProvider;
		}

		// Token: 0x170010EA RID: 4330
		// (get) Token: 0x06006053 RID: 24659 RVA: 0x0014BFC6 File Offset: 0x0014A1C6
		[__DynamicallyInvokable]
		public Type SourceInterface
		{
			[__DynamicallyInvokable]
			get
			{
				return this._SourceInterface;
			}
		}

		// Token: 0x170010EB RID: 4331
		// (get) Token: 0x06006054 RID: 24660 RVA: 0x0014BFCE File Offset: 0x0014A1CE
		[__DynamicallyInvokable]
		public Type EventProvider
		{
			[__DynamicallyInvokable]
			get
			{
				return this._EventProvider;
			}
		}

		// Token: 0x04002B27 RID: 11047
		internal Type _SourceInterface;

		// Token: 0x04002B28 RID: 11048
		internal Type _EventProvider;
	}
}
