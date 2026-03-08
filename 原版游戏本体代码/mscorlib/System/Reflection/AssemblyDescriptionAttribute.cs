using System;
using System.Runtime.InteropServices;

namespace System.Reflection
{
	// Token: 0x020005B8 RID: 1464
	[AttributeUsage(AttributeTargets.Assembly, Inherited = false)]
	[ComVisible(true)]
	[__DynamicallyInvokable]
	public sealed class AssemblyDescriptionAttribute : Attribute
	{
		// Token: 0x0600445A RID: 17498 RVA: 0x000FC470 File Offset: 0x000FA670
		[__DynamicallyInvokable]
		public AssemblyDescriptionAttribute(string description)
		{
			this.m_description = description;
		}

		// Token: 0x17000A13 RID: 2579
		// (get) Token: 0x0600445B RID: 17499 RVA: 0x000FC47F File Offset: 0x000FA67F
		[__DynamicallyInvokable]
		public string Description
		{
			[__DynamicallyInvokable]
			get
			{
				return this.m_description;
			}
		}

		// Token: 0x04001C02 RID: 7170
		private string m_description;
	}
}
