using System;

namespace System.Runtime.InteropServices.WindowsRuntime
{
	// Token: 0x020009C6 RID: 2502
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = false, Inherited = false)]
	[__DynamicallyInvokable]
	public sealed class DefaultInterfaceAttribute : Attribute
	{
		// Token: 0x060063BF RID: 25535 RVA: 0x00154969 File Offset: 0x00152B69
		[__DynamicallyInvokable]
		public DefaultInterfaceAttribute(Type defaultInterface)
		{
			this.m_defaultInterface = defaultInterface;
		}

		// Token: 0x17001139 RID: 4409
		// (get) Token: 0x060063C0 RID: 25536 RVA: 0x00154978 File Offset: 0x00152B78
		[__DynamicallyInvokable]
		public Type DefaultInterface
		{
			[__DynamicallyInvokable]
			get
			{
				return this.m_defaultInterface;
			}
		}

		// Token: 0x04002CDB RID: 11483
		private Type m_defaultInterface;
	}
}
