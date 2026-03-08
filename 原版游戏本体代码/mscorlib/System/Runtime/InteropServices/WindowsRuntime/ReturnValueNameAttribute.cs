using System;

namespace System.Runtime.InteropServices.WindowsRuntime
{
	// Token: 0x020009CB RID: 2507
	[AttributeUsage(AttributeTargets.Delegate | AttributeTargets.ReturnValue, AllowMultiple = false, Inherited = false)]
	[__DynamicallyInvokable]
	public sealed class ReturnValueNameAttribute : Attribute
	{
		// Token: 0x060063CA RID: 25546 RVA: 0x001549ED File Offset: 0x00152BED
		[__DynamicallyInvokable]
		public ReturnValueNameAttribute(string name)
		{
			this.m_Name = name;
		}

		// Token: 0x1700113F RID: 4415
		// (get) Token: 0x060063CB RID: 25547 RVA: 0x001549FC File Offset: 0x00152BFC
		[__DynamicallyInvokable]
		public string Name
		{
			[__DynamicallyInvokable]
			get
			{
				return this.m_Name;
			}
		}

		// Token: 0x04002CE1 RID: 11489
		private string m_Name;
	}
}
