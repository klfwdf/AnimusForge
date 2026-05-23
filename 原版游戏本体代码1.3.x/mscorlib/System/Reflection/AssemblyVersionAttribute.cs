using System;
using System.Runtime.InteropServices;

namespace System.Reflection
{
	// Token: 0x020005BF RID: 1471
	[AttributeUsage(AttributeTargets.Assembly, Inherited = false)]
	[ComVisible(true)]
	[__DynamicallyInvokable]
	public sealed class AssemblyVersionAttribute : Attribute
	{
		// Token: 0x06004468 RID: 17512 RVA: 0x000FC51F File Offset: 0x000FA71F
		[__DynamicallyInvokable]
		public AssemblyVersionAttribute(string version)
		{
			this.m_version = version;
		}

		// Token: 0x17000A1A RID: 2586
		// (get) Token: 0x06004469 RID: 17513 RVA: 0x000FC52E File Offset: 0x000FA72E
		[__DynamicallyInvokable]
		public string Version
		{
			[__DynamicallyInvokable]
			get
			{
				return this.m_version;
			}
		}

		// Token: 0x04001C09 RID: 7177
		private string m_version;
	}
}
