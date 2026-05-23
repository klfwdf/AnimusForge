using System;
using System.Diagnostics;

namespace System.Runtime.Versioning
{
	// Token: 0x02000720 RID: 1824
	[AttributeUsage(AttributeTargets.Constructor | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field, Inherited = false)]
	[Conditional("RESOURCE_ANNOTATION_WORK")]
	public sealed class ResourceExposureAttribute : Attribute
	{
		// Token: 0x0600515B RID: 20827 RVA: 0x0011EDE8 File Offset: 0x0011CFE8
		public ResourceExposureAttribute(ResourceScope exposureLevel)
		{
			this._resourceExposureLevel = exposureLevel;
		}

		// Token: 0x17000D6C RID: 3436
		// (get) Token: 0x0600515C RID: 20828 RVA: 0x0011EDF7 File Offset: 0x0011CFF7
		public ResourceScope ResourceExposureLevel
		{
			get
			{
				return this._resourceExposureLevel;
			}
		}

		// Token: 0x04002412 RID: 9234
		private ResourceScope _resourceExposureLevel;
	}
}
