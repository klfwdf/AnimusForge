using System;

namespace System.Runtime.CompilerServices
{
	// Token: 0x020008E2 RID: 2274
	[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
	[__DynamicallyInvokable]
	[Serializable]
	public sealed class ReferenceAssemblyAttribute : Attribute
	{
		// Token: 0x06005DDE RID: 24030 RVA: 0x00149ADE File Offset: 0x00147CDE
		[__DynamicallyInvokable]
		public ReferenceAssemblyAttribute()
		{
		}

		// Token: 0x06005DDF RID: 24031 RVA: 0x00149AE6 File Offset: 0x00147CE6
		[__DynamicallyInvokable]
		public ReferenceAssemblyAttribute(string description)
		{
			this._description = description;
		}

		// Token: 0x1700101F RID: 4127
		// (get) Token: 0x06005DE0 RID: 24032 RVA: 0x00149AF5 File Offset: 0x00147CF5
		[__DynamicallyInvokable]
		public string Description
		{
			[__DynamicallyInvokable]
			get
			{
				return this._description;
			}
		}

		// Token: 0x04002A3C RID: 10812
		private string _description;
	}
}
