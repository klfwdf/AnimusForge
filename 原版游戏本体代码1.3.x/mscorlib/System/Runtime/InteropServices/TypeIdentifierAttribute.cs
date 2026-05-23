using System;

namespace System.Runtime.InteropServices
{
	// Token: 0x02000910 RID: 2320
	[AttributeUsage(AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Interface | AttributeTargets.Delegate, AllowMultiple = false, Inherited = false)]
	[ComVisible(false)]
	[__DynamicallyInvokable]
	public sealed class TypeIdentifierAttribute : Attribute
	{
		// Token: 0x06005FF0 RID: 24560 RVA: 0x0014B6C5 File Offset: 0x001498C5
		[__DynamicallyInvokable]
		public TypeIdentifierAttribute()
		{
		}

		// Token: 0x06005FF1 RID: 24561 RVA: 0x0014B6CD File Offset: 0x001498CD
		[__DynamicallyInvokable]
		public TypeIdentifierAttribute(string scope, string identifier)
		{
			this.Scope_ = scope;
			this.Identifier_ = identifier;
		}

		// Token: 0x170010CF RID: 4303
		// (get) Token: 0x06005FF2 RID: 24562 RVA: 0x0014B6E3 File Offset: 0x001498E3
		[__DynamicallyInvokable]
		public string Scope
		{
			[__DynamicallyInvokable]
			get
			{
				return this.Scope_;
			}
		}

		// Token: 0x170010D0 RID: 4304
		// (get) Token: 0x06005FF3 RID: 24563 RVA: 0x0014B6EB File Offset: 0x001498EB
		[__DynamicallyInvokable]
		public string Identifier
		{
			[__DynamicallyInvokable]
			get
			{
				return this.Identifier_;
			}
		}

		// Token: 0x04002A63 RID: 10851
		internal string Scope_;

		// Token: 0x04002A64 RID: 10852
		internal string Identifier_;
	}
}
