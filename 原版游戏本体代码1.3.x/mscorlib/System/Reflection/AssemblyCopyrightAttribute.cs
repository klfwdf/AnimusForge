using System;
using System.Runtime.InteropServices;

namespace System.Reflection
{
	// Token: 0x020005B4 RID: 1460
	[AttributeUsage(AttributeTargets.Assembly, Inherited = false)]
	[ComVisible(true)]
	[__DynamicallyInvokable]
	public sealed class AssemblyCopyrightAttribute : Attribute
	{
		// Token: 0x06004452 RID: 17490 RVA: 0x000FC414 File Offset: 0x000FA614
		[__DynamicallyInvokable]
		public AssemblyCopyrightAttribute(string copyright)
		{
			this.m_copyright = copyright;
		}

		// Token: 0x17000A0F RID: 2575
		// (get) Token: 0x06004453 RID: 17491 RVA: 0x000FC423 File Offset: 0x000FA623
		[__DynamicallyInvokable]
		public string Copyright
		{
			[__DynamicallyInvokable]
			get
			{
				return this.m_copyright;
			}
		}

		// Token: 0x04001BFE RID: 7166
		private string m_copyright;
	}
}
