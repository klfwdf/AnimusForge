using System;
using System.Runtime.InteropServices;

namespace System.Diagnostics
{
	// Token: 0x020003EF RID: 1007
	[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Delegate, AllowMultiple = true)]
	[ComVisible(true)]
	[__DynamicallyInvokable]
	public sealed class DebuggerDisplayAttribute : Attribute
	{
		// Token: 0x0600331A RID: 13082 RVA: 0x000C4DC5 File Offset: 0x000C2FC5
		[__DynamicallyInvokable]
		public DebuggerDisplayAttribute(string value)
		{
			if (value == null)
			{
				this.value = "";
			}
			else
			{
				this.value = value;
			}
			this.name = "";
			this.type = "";
		}

		// Token: 0x17000779 RID: 1913
		// (get) Token: 0x0600331B RID: 13083 RVA: 0x000C4DFA File Offset: 0x000C2FFA
		[__DynamicallyInvokable]
		public string Value
		{
			[__DynamicallyInvokable]
			get
			{
				return this.value;
			}
		}

		// Token: 0x1700077A RID: 1914
		// (get) Token: 0x0600331C RID: 13084 RVA: 0x000C4E02 File Offset: 0x000C3002
		// (set) Token: 0x0600331D RID: 13085 RVA: 0x000C4E0A File Offset: 0x000C300A
		[__DynamicallyInvokable]
		public string Name
		{
			[__DynamicallyInvokable]
			get
			{
				return this.name;
			}
			[__DynamicallyInvokable]
			set
			{
				this.name = value;
			}
		}

		// Token: 0x1700077B RID: 1915
		// (get) Token: 0x0600331E RID: 13086 RVA: 0x000C4E13 File Offset: 0x000C3013
		// (set) Token: 0x0600331F RID: 13087 RVA: 0x000C4E1B File Offset: 0x000C301B
		[__DynamicallyInvokable]
		public string Type
		{
			[__DynamicallyInvokable]
			get
			{
				return this.type;
			}
			[__DynamicallyInvokable]
			set
			{
				this.type = value;
			}
		}

		// Token: 0x1700077C RID: 1916
		// (get) Token: 0x06003321 RID: 13089 RVA: 0x000C4E4D File Offset: 0x000C304D
		// (set) Token: 0x06003320 RID: 13088 RVA: 0x000C4E24 File Offset: 0x000C3024
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

		// Token: 0x1700077D RID: 1917
		// (get) Token: 0x06003322 RID: 13090 RVA: 0x000C4E55 File Offset: 0x000C3055
		// (set) Token: 0x06003323 RID: 13091 RVA: 0x000C4E5D File Offset: 0x000C305D
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

		// Token: 0x040016AC RID: 5804
		private string name;

		// Token: 0x040016AD RID: 5805
		private string value;

		// Token: 0x040016AE RID: 5806
		private string type;

		// Token: 0x040016AF RID: 5807
		private string targetName;

		// Token: 0x040016B0 RID: 5808
		private Type target;
	}
}
