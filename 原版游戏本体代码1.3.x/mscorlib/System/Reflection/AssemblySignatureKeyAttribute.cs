using System;

namespace System.Reflection
{
	// Token: 0x020005C5 RID: 1477
	[AttributeUsage(AttributeTargets.Assembly, Inherited = false, AllowMultiple = false)]
	[__DynamicallyInvokable]
	public sealed class AssemblySignatureKeyAttribute : Attribute
	{
		// Token: 0x06004479 RID: 17529 RVA: 0x000FC5ED File Offset: 0x000FA7ED
		[__DynamicallyInvokable]
		public AssemblySignatureKeyAttribute(string publicKey, string countersignature)
		{
			this._publicKey = publicKey;
			this._countersignature = countersignature;
		}

		// Token: 0x17000A22 RID: 2594
		// (get) Token: 0x0600447A RID: 17530 RVA: 0x000FC603 File Offset: 0x000FA803
		[__DynamicallyInvokable]
		public string PublicKey
		{
			[__DynamicallyInvokable]
			get
			{
				return this._publicKey;
			}
		}

		// Token: 0x17000A23 RID: 2595
		// (get) Token: 0x0600447B RID: 17531 RVA: 0x000FC60B File Offset: 0x000FA80B
		[__DynamicallyInvokable]
		public string Countersignature
		{
			[__DynamicallyInvokable]
			get
			{
				return this._countersignature;
			}
		}

		// Token: 0x04001C10 RID: 7184
		private string _publicKey;

		// Token: 0x04001C11 RID: 7185
		private string _countersignature;
	}
}
