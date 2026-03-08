using System;
using System.Runtime.InteropServices;

namespace System.Diagnostics
{
	// Token: 0x020003E5 RID: 997
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
	[ComVisible(true)]
	[__DynamicallyInvokable]
	[Serializable]
	public sealed class ConditionalAttribute : Attribute
	{
		// Token: 0x060032FA RID: 13050 RVA: 0x000C4BA2 File Offset: 0x000C2DA2
		[__DynamicallyInvokable]
		public ConditionalAttribute(string conditionString)
		{
			this.m_conditionString = conditionString;
		}

		// Token: 0x17000770 RID: 1904
		// (get) Token: 0x060032FB RID: 13051 RVA: 0x000C4BB1 File Offset: 0x000C2DB1
		[__DynamicallyInvokable]
		public string ConditionString
		{
			[__DynamicallyInvokable]
			get
			{
				return this.m_conditionString;
			}
		}

		// Token: 0x040016A0 RID: 5792
		private string m_conditionString;
	}
}
