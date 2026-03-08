using System;
using System.Runtime.InteropServices;

namespace System.Diagnostics
{
	// Token: 0x020003EE RID: 1006
	[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true)]
	[ComVisible(true)]
	[__DynamicallyInvokable]
	public sealed class DebuggerTypeProxyAttribute : Attribute
	{
		// Token: 0x06003313 RID: 13075 RVA: 0x000C4D44 File Offset: 0x000C2F44
		[__DynamicallyInvokable]
		public DebuggerTypeProxyAttribute(Type type)
		{
			if (type == null)
			{
				throw new ArgumentNullException("type");
			}
			this.typeName = type.AssemblyQualifiedName;
		}

		// Token: 0x06003314 RID: 13076 RVA: 0x000C4D6C File Offset: 0x000C2F6C
		[__DynamicallyInvokable]
		public DebuggerTypeProxyAttribute(string typeName)
		{
			this.typeName = typeName;
		}

		// Token: 0x17000776 RID: 1910
		// (get) Token: 0x06003315 RID: 13077 RVA: 0x000C4D7B File Offset: 0x000C2F7B
		[__DynamicallyInvokable]
		public string ProxyTypeName
		{
			[__DynamicallyInvokable]
			get
			{
				return this.typeName;
			}
		}

		// Token: 0x17000777 RID: 1911
		// (get) Token: 0x06003317 RID: 13079 RVA: 0x000C4DAC File Offset: 0x000C2FAC
		// (set) Token: 0x06003316 RID: 13078 RVA: 0x000C4D83 File Offset: 0x000C2F83
		[__DynamicallyInvokable]
		public Type Target
		{
			[__DynamicallyInvokable]
			get
			{
				return this.target;
			}
			[__DynamicallyInvokable]
			set
			{
				if (value == null)
				{
					throw new ArgumentNullException("value");
				}
				this.targetName = value.AssemblyQualifiedName;
				this.target = value;
			}
		}

		// Token: 0x17000778 RID: 1912
		// (get) Token: 0x06003318 RID: 13080 RVA: 0x000C4DB4 File Offset: 0x000C2FB4
		// (set) Token: 0x06003319 RID: 13081 RVA: 0x000C4DBC File Offset: 0x000C2FBC
		[__DynamicallyInvokable]
		public string TargetTypeName
		{
			[__DynamicallyInvokable]
			get
			{
				return this.targetName;
			}
			[__DynamicallyInvokable]
			set
			{
				this.targetName = value;
			}
		}

		// Token: 0x040016A9 RID: 5801
		private string typeName;

		// Token: 0x040016AA RID: 5802
		private string targetName;

		// Token: 0x040016AB RID: 5803
		private Type target;
	}
}
