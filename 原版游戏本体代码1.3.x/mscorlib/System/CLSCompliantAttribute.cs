using System;
using System.Runtime.InteropServices;

namespace System
{
	// Token: 0x020000BE RID: 190
	[AttributeUsage(AttributeTargets.All, Inherited = true, AllowMultiple = false)]
	[ComVisible(true)]
	[__DynamicallyInvokable]
	[Serializable]
	public sealed class CLSCompliantAttribute : Attribute
	{
		// Token: 0x06000AFD RID: 2813 RVA: 0x00022BAF File Offset: 0x00020DAF
		[__DynamicallyInvokable]
		public CLSCompliantAttribute(bool isCompliant)
		{
			this.m_compliant = isCompliant;
		}

		// Token: 0x17000156 RID: 342
		// (get) Token: 0x06000AFE RID: 2814 RVA: 0x00022BBE File Offset: 0x00020DBE
		[__DynamicallyInvokable]
		public bool IsCompliant
		{
			[__DynamicallyInvokable]
			get
			{
				return this.m_compliant;
			}
		}

		// Token: 0x0400045F RID: 1119
		private bool m_compliant;
	}
}
