using System;
using System.Runtime.InteropServices;

namespace System.Reflection
{
	// Token: 0x020005BE RID: 1470
	[AttributeUsage(AttributeTargets.Assembly, Inherited = false)]
	[ComVisible(true)]
	[__DynamicallyInvokable]
	public sealed class AssemblyCultureAttribute : Attribute
	{
		// Token: 0x06004466 RID: 17510 RVA: 0x000FC508 File Offset: 0x000FA708
		[__DynamicallyInvokable]
		public AssemblyCultureAttribute(string culture)
		{
			this.m_culture = culture;
		}

		// Token: 0x17000A19 RID: 2585
		// (get) Token: 0x06004467 RID: 17511 RVA: 0x000FC517 File Offset: 0x000FA717
		[__DynamicallyInvokable]
		public string Culture
		{
			[__DynamicallyInvokable]
			get
			{
				return this.m_culture;
			}
		}

		// Token: 0x04001C08 RID: 7176
		private string m_culture;
	}
}
