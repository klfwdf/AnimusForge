using System;

namespace System.Runtime.InteropServices
{
	// Token: 0x0200093A RID: 2362
	[AttributeUsage(AttributeTargets.Interface, Inherited = false)]
	[ComVisible(true)]
	[__DynamicallyInvokable]
	public sealed class CoClassAttribute : Attribute
	{
		// Token: 0x06006050 RID: 24656 RVA: 0x0014BF99 File Offset: 0x0014A199
		[__DynamicallyInvokable]
		public CoClassAttribute(Type coClass)
		{
			this._CoClass = coClass;
		}

		// Token: 0x170010E9 RID: 4329
		// (get) Token: 0x06006051 RID: 24657 RVA: 0x0014BFA8 File Offset: 0x0014A1A8
		[__DynamicallyInvokable]
		public Type CoClass
		{
			[__DynamicallyInvokable]
			get
			{
				return this._CoClass;
			}
		}

		// Token: 0x04002B26 RID: 11046
		internal Type _CoClass;
	}
}
