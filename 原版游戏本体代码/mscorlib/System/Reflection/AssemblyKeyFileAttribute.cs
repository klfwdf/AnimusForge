using System;
using System.Runtime.InteropServices;

namespace System.Reflection
{
	// Token: 0x020005C0 RID: 1472
	[AttributeUsage(AttributeTargets.Assembly, Inherited = false)]
	[ComVisible(true)]
	[__DynamicallyInvokable]
	public sealed class AssemblyKeyFileAttribute : Attribute
	{
		// Token: 0x0600446A RID: 17514 RVA: 0x000FC536 File Offset: 0x000FA736
		[__DynamicallyInvokable]
		public AssemblyKeyFileAttribute(string keyFile)
		{
			this.m_keyFile = keyFile;
		}

		// Token: 0x17000A1B RID: 2587
		// (get) Token: 0x0600446B RID: 17515 RVA: 0x000FC545 File Offset: 0x000FA745
		[__DynamicallyInvokable]
		public string KeyFile
		{
			[__DynamicallyInvokable]
			get
			{
				return this.m_keyFile;
			}
		}

		// Token: 0x04001C0A RID: 7178
		private string m_keyFile;
	}
}
