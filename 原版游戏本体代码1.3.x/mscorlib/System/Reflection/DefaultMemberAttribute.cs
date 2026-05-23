using System;
using System.Runtime.InteropServices;

namespace System.Reflection
{
	// Token: 0x020005E0 RID: 1504
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface)]
	[ComVisible(true)]
	[__DynamicallyInvokable]
	[Serializable]
	public sealed class DefaultMemberAttribute : Attribute
	{
		// Token: 0x060045B3 RID: 17843 RVA: 0x001012A6 File Offset: 0x000FF4A6
		[__DynamicallyInvokable]
		public DefaultMemberAttribute(string memberName)
		{
			this.m_memberName = memberName;
		}

		// Token: 0x17000A5D RID: 2653
		// (get) Token: 0x060045B4 RID: 17844 RVA: 0x001012B5 File Offset: 0x000FF4B5
		[__DynamicallyInvokable]
		public string MemberName
		{
			[__DynamicallyInvokable]
			get
			{
				return this.m_memberName;
			}
		}

		// Token: 0x04001C94 RID: 7316
		private string m_memberName;
	}
}
