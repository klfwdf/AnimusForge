using System;

namespace System.Reflection
{
	// Token: 0x020005C4 RID: 1476
	[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true, Inherited = false)]
	[__DynamicallyInvokable]
	public sealed class AssemblyMetadataAttribute : Attribute
	{
		// Token: 0x06004476 RID: 17526 RVA: 0x000FC5C7 File Offset: 0x000FA7C7
		[__DynamicallyInvokable]
		public AssemblyMetadataAttribute(string key, string value)
		{
			this.m_key = key;
			this.m_value = value;
		}

		// Token: 0x17000A20 RID: 2592
		// (get) Token: 0x06004477 RID: 17527 RVA: 0x000FC5DD File Offset: 0x000FA7DD
		[__DynamicallyInvokable]
		public string Key
		{
			[__DynamicallyInvokable]
			get
			{
				return this.m_key;
			}
		}

		// Token: 0x17000A21 RID: 2593
		// (get) Token: 0x06004478 RID: 17528 RVA: 0x000FC5E5 File Offset: 0x000FA7E5
		[__DynamicallyInvokable]
		public string Value
		{
			[__DynamicallyInvokable]
			get
			{
				return this.m_value;
			}
		}

		// Token: 0x04001C0E RID: 7182
		private string m_key;

		// Token: 0x04001C0F RID: 7183
		private string m_value;
	}
}
