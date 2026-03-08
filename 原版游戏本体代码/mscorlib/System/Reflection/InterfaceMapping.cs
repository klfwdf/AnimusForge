using System;
using System.Runtime.InteropServices;

namespace System.Reflection
{
	// Token: 0x020005EE RID: 1518
	[ComVisible(true)]
	[__DynamicallyInvokable]
	public struct InterfaceMapping
	{
		// Token: 0x04001CD0 RID: 7376
		[ComVisible(true)]
		[__DynamicallyInvokable]
		public Type TargetType;

		// Token: 0x04001CD1 RID: 7377
		[ComVisible(true)]
		[__DynamicallyInvokable]
		public Type InterfaceType;

		// Token: 0x04001CD2 RID: 7378
		[ComVisible(true)]
		[__DynamicallyInvokable]
		public MethodInfo[] TargetMethods;

		// Token: 0x04001CD3 RID: 7379
		[ComVisible(true)]
		[__DynamicallyInvokable]
		public MethodInfo[] InterfaceMethods;
	}
}
