using System;
using System.Runtime.InteropServices;

namespace System.Reflection
{
	// Token: 0x020005C6 RID: 1478
	[AttributeUsage(AttributeTargets.Assembly, Inherited = false)]
	[ComVisible(true)]
	[__DynamicallyInvokable]
	public sealed class AssemblyKeyNameAttribute : Attribute
	{
		// Token: 0x0600447C RID: 17532 RVA: 0x000FC613 File Offset: 0x000FA813
		[__DynamicallyInvokable]
		public AssemblyKeyNameAttribute(string keyName)
		{
			this.m_keyName = keyName;
		}

		// Token: 0x17000A24 RID: 2596
		// (get) Token: 0x0600447D RID: 17533 RVA: 0x000FC622 File Offset: 0x000FA822
		[__DynamicallyInvokable]
		public string KeyName
		{
			[__DynamicallyInvokable]
			get
			{
				return this.m_keyName;
			}
		}

		// Token: 0x04001C12 RID: 7186
		private string m_keyName;
	}
}
