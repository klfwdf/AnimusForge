using System;

namespace System.Runtime.InteropServices
{
	// Token: 0x02000917 RID: 2327
	[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class, Inherited = false)]
	[ComVisible(true)]
	[__DynamicallyInvokable]
	public sealed class ClassInterfaceAttribute : Attribute
	{
		// Token: 0x06005FFC RID: 24572 RVA: 0x0014B74F File Offset: 0x0014994F
		[__DynamicallyInvokable]
		public ClassInterfaceAttribute(ClassInterfaceType classInterfaceType)
		{
			this._val = classInterfaceType;
		}

		// Token: 0x06005FFD RID: 24573 RVA: 0x0014B75E File Offset: 0x0014995E
		[__DynamicallyInvokable]
		public ClassInterfaceAttribute(short classInterfaceType)
		{
			this._val = (ClassInterfaceType)classInterfaceType;
		}

		// Token: 0x170010D4 RID: 4308
		// (get) Token: 0x06005FFE RID: 24574 RVA: 0x0014B76D File Offset: 0x0014996D
		[__DynamicallyInvokable]
		public ClassInterfaceType Value
		{
			[__DynamicallyInvokable]
			get
			{
				return this._val;
			}
		}

		// Token: 0x04002A71 RID: 10865
		internal ClassInterfaceType _val;
	}
}
