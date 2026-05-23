using System;

namespace System.Runtime.InteropServices
{
	// Token: 0x02000915 RID: 2325
	[AttributeUsage(AttributeTargets.Class, Inherited = false)]
	[ComVisible(true)]
	[__DynamicallyInvokable]
	public sealed class ComDefaultInterfaceAttribute : Attribute
	{
		// Token: 0x06005FFA RID: 24570 RVA: 0x0014B738 File Offset: 0x00149938
		[__DynamicallyInvokable]
		public ComDefaultInterfaceAttribute(Type defaultInterface)
		{
			this._val = defaultInterface;
		}

		// Token: 0x170010D3 RID: 4307
		// (get) Token: 0x06005FFB RID: 24571 RVA: 0x0014B747 File Offset: 0x00149947
		[__DynamicallyInvokable]
		public Type Value
		{
			[__DynamicallyInvokable]
			get
			{
				return this._val;
			}
		}

		// Token: 0x04002A6C RID: 10860
		internal Type _val;
	}
}
