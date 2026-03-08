using System;

namespace System.Runtime.InteropServices
{
	// Token: 0x02000914 RID: 2324
	[AttributeUsage(AttributeTargets.Interface, Inherited = false)]
	[ComVisible(true)]
	[__DynamicallyInvokable]
	public sealed class InterfaceTypeAttribute : Attribute
	{
		// Token: 0x06005FF7 RID: 24567 RVA: 0x0014B712 File Offset: 0x00149912
		[__DynamicallyInvokable]
		public InterfaceTypeAttribute(ComInterfaceType interfaceType)
		{
			this._val = interfaceType;
		}

		// Token: 0x06005FF8 RID: 24568 RVA: 0x0014B721 File Offset: 0x00149921
		[__DynamicallyInvokable]
		public InterfaceTypeAttribute(short interfaceType)
		{
			this._val = (ComInterfaceType)interfaceType;
		}

		// Token: 0x170010D2 RID: 4306
		// (get) Token: 0x06005FF9 RID: 24569 RVA: 0x0014B730 File Offset: 0x00149930
		[__DynamicallyInvokable]
		public ComInterfaceType Value
		{
			[__DynamicallyInvokable]
			get
			{
				return this._val;
			}
		}

		// Token: 0x04002A6B RID: 10859
		internal ComInterfaceType _val;
	}
}
