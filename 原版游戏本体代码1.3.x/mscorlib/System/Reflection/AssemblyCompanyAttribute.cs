using System;
using System.Runtime.InteropServices;

namespace System.Reflection
{
	// Token: 0x020005B7 RID: 1463
	[AttributeUsage(AttributeTargets.Assembly, Inherited = false)]
	[ComVisible(true)]
	[__DynamicallyInvokable]
	public sealed class AssemblyCompanyAttribute : Attribute
	{
		// Token: 0x06004458 RID: 17496 RVA: 0x000FC459 File Offset: 0x000FA659
		[__DynamicallyInvokable]
		public AssemblyCompanyAttribute(string company)
		{
			this.m_company = company;
		}

		// Token: 0x17000A12 RID: 2578
		// (get) Token: 0x06004459 RID: 17497 RVA: 0x000FC468 File Offset: 0x000FA668
		[__DynamicallyInvokable]
		public string Company
		{
			[__DynamicallyInvokable]
			get
			{
				return this.m_company;
			}
		}

		// Token: 0x04001C01 RID: 7169
		private string m_company;
	}
}
