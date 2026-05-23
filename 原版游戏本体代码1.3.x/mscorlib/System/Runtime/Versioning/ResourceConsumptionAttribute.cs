using System;
using System.Diagnostics;

namespace System.Runtime.Versioning
{
	// Token: 0x0200071F RID: 1823
	[AttributeUsage(AttributeTargets.Constructor | AttributeTargets.Method | AttributeTargets.Property, Inherited = false)]
	[Conditional("RESOURCE_ANNOTATION_WORK")]
	public sealed class ResourceConsumptionAttribute : Attribute
	{
		// Token: 0x06005157 RID: 20823 RVA: 0x0011EDA7 File Offset: 0x0011CFA7
		public ResourceConsumptionAttribute(ResourceScope resourceScope)
		{
			this._resourceScope = resourceScope;
			this._consumptionScope = this._resourceScope;
		}

		// Token: 0x06005158 RID: 20824 RVA: 0x0011EDC2 File Offset: 0x0011CFC2
		public ResourceConsumptionAttribute(ResourceScope resourceScope, ResourceScope consumptionScope)
		{
			this._resourceScope = resourceScope;
			this._consumptionScope = consumptionScope;
		}

		// Token: 0x17000D6A RID: 3434
		// (get) Token: 0x06005159 RID: 20825 RVA: 0x0011EDD8 File Offset: 0x0011CFD8
		public ResourceScope ResourceScope
		{
			get
			{
				return this._resourceScope;
			}
		}

		// Token: 0x17000D6B RID: 3435
		// (get) Token: 0x0600515A RID: 20826 RVA: 0x0011EDE0 File Offset: 0x0011CFE0
		public ResourceScope ConsumptionScope
		{
			get
			{
				return this._consumptionScope;
			}
		}

		// Token: 0x04002410 RID: 9232
		private ResourceScope _consumptionScope;

		// Token: 0x04002411 RID: 9233
		private ResourceScope _resourceScope;
	}
}
