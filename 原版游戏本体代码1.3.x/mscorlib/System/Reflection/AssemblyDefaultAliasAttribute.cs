using System;
using System.Runtime.InteropServices;

namespace System.Reflection
{
	// Token: 0x020005BB RID: 1467
	[AttributeUsage(AttributeTargets.Assembly, Inherited = false)]
	[ComVisible(true)]
	[__DynamicallyInvokable]
	public sealed class AssemblyDefaultAliasAttribute : Attribute
	{
		// Token: 0x06004460 RID: 17504 RVA: 0x000FC4B5 File Offset: 0x000FA6B5
		[__DynamicallyInvokable]
		public AssemblyDefaultAliasAttribute(string defaultAlias)
		{
			this.m_defaultAlias = defaultAlias;
		}

		// Token: 0x17000A16 RID: 2582
		// (get) Token: 0x06004461 RID: 17505 RVA: 0x000FC4C4 File Offset: 0x000FA6C4
		[__DynamicallyInvokable]
		public string DefaultAlias
		{
			[__DynamicallyInvokable]
			get
			{
				return this.m_defaultAlias;
			}
		}

		// Token: 0x04001C05 RID: 7173
		private string m_defaultAlias;
	}
}
