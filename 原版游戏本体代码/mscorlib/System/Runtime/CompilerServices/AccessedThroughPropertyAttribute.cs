using System;
using System.Runtime.InteropServices;

namespace System.Runtime.CompilerServices
{
	// Token: 0x020008A2 RID: 2210
	[AttributeUsage(AttributeTargets.Field)]
	[ComVisible(true)]
	[__DynamicallyInvokable]
	public sealed class AccessedThroughPropertyAttribute : Attribute
	{
		// Token: 0x06005D78 RID: 23928 RVA: 0x001493D6 File Offset: 0x001475D6
		[__DynamicallyInvokable]
		public AccessedThroughPropertyAttribute(string propertyName)
		{
			this.propertyName = propertyName;
		}

		// Token: 0x1700100E RID: 4110
		// (get) Token: 0x06005D79 RID: 23929 RVA: 0x001493E5 File Offset: 0x001475E5
		[__DynamicallyInvokable]
		public string PropertyName
		{
			[__DynamicallyInvokable]
			get
			{
				return this.propertyName;
			}
		}

		// Token: 0x04002A10 RID: 10768
		private readonly string propertyName;
	}
}
