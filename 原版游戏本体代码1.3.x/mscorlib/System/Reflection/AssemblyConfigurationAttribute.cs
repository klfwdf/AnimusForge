using System;
using System.Runtime.InteropServices;

namespace System.Reflection
{
	// Token: 0x020005BA RID: 1466
	[AttributeUsage(AttributeTargets.Assembly, Inherited = false)]
	[ComVisible(true)]
	[__DynamicallyInvokable]
	public sealed class AssemblyConfigurationAttribute : Attribute
	{
		// Token: 0x0600445E RID: 17502 RVA: 0x000FC49E File Offset: 0x000FA69E
		[__DynamicallyInvokable]
		public AssemblyConfigurationAttribute(string configuration)
		{
			this.m_configuration = configuration;
		}

		// Token: 0x17000A15 RID: 2581
		// (get) Token: 0x0600445F RID: 17503 RVA: 0x000FC4AD File Offset: 0x000FA6AD
		[__DynamicallyInvokable]
		public string Configuration
		{
			[__DynamicallyInvokable]
			get
			{
				return this.m_configuration;
			}
		}

		// Token: 0x04001C04 RID: 7172
		private string m_configuration;
	}
}
