using System;
using System.Runtime.InteropServices;

namespace System.Reflection
{
	// Token: 0x020005BC RID: 1468
	[AttributeUsage(AttributeTargets.Assembly, Inherited = false)]
	[ComVisible(true)]
	[__DynamicallyInvokable]
	public sealed class AssemblyInformationalVersionAttribute : Attribute
	{
		// Token: 0x06004462 RID: 17506 RVA: 0x000FC4CC File Offset: 0x000FA6CC
		[__DynamicallyInvokable]
		public AssemblyInformationalVersionAttribute(string informationalVersion)
		{
			this.m_informationalVersion = informationalVersion;
		}

		// Token: 0x17000A17 RID: 2583
		// (get) Token: 0x06004463 RID: 17507 RVA: 0x000FC4DB File Offset: 0x000FA6DB
		[__DynamicallyInvokable]
		public string InformationalVersion
		{
			[__DynamicallyInvokable]
			get
			{
				return this.m_informationalVersion;
			}
		}

		// Token: 0x04001C06 RID: 7174
		private string m_informationalVersion;
	}
}
