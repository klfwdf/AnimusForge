using System;
using System.Runtime.InteropServices;

namespace System.Reflection
{
	// Token: 0x020005B9 RID: 1465
	[AttributeUsage(AttributeTargets.Assembly, Inherited = false)]
	[ComVisible(true)]
	[__DynamicallyInvokable]
	public sealed class AssemblyTitleAttribute : Attribute
	{
		// Token: 0x0600445C RID: 17500 RVA: 0x000FC487 File Offset: 0x000FA687
		[__DynamicallyInvokable]
		public AssemblyTitleAttribute(string title)
		{
			this.m_title = title;
		}

		// Token: 0x17000A14 RID: 2580
		// (get) Token: 0x0600445D RID: 17501 RVA: 0x000FC496 File Offset: 0x000FA696
		[__DynamicallyInvokable]
		public string Title
		{
			[__DynamicallyInvokable]
			get
			{
				return this.m_title;
			}
		}

		// Token: 0x04001C03 RID: 7171
		private string m_title;
	}
}
