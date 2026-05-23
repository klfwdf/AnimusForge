using System;

namespace System.Runtime.CompilerServices
{
	// Token: 0x020008E1 RID: 2273
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Interface | AttributeTargets.Delegate, Inherited = false, AllowMultiple = false)]
	[__DynamicallyInvokable]
	public sealed class TypeForwardedFromAttribute : Attribute
	{
		// Token: 0x06005DDB RID: 24027 RVA: 0x00149AAC File Offset: 0x00147CAC
		private TypeForwardedFromAttribute()
		{
		}

		// Token: 0x06005DDC RID: 24028 RVA: 0x00149AB4 File Offset: 0x00147CB4
		[__DynamicallyInvokable]
		public TypeForwardedFromAttribute(string assemblyFullName)
		{
			if (string.IsNullOrEmpty(assemblyFullName))
			{
				throw new ArgumentNullException("assemblyFullName");
			}
			this.assemblyFullName = assemblyFullName;
		}

		// Token: 0x1700101E RID: 4126
		// (get) Token: 0x06005DDD RID: 24029 RVA: 0x00149AD6 File Offset: 0x00147CD6
		[__DynamicallyInvokable]
		public string AssemblyFullName
		{
			[__DynamicallyInvokable]
			get
			{
				return this.assemblyFullName;
			}
		}

		// Token: 0x04002A3B RID: 10811
		private string assemblyFullName;
	}
}
