using System;
using System.Runtime.InteropServices;

namespace System.Reflection
{
	// Token: 0x020005F3 RID: 1523
	[ComVisible(true)]
	[__DynamicallyInvokable]
	public class ManifestResourceInfo
	{
		// Token: 0x06004675 RID: 18037 RVA: 0x001027CF File Offset: 0x001009CF
		[__DynamicallyInvokable]
		public ManifestResourceInfo(Assembly containingAssembly, string containingFileName, ResourceLocation resourceLocation)
		{
			this._containingAssembly = containingAssembly;
			this._containingFileName = containingFileName;
			this._resourceLocation = resourceLocation;
		}

		// Token: 0x17000A9A RID: 2714
		// (get) Token: 0x06004676 RID: 18038 RVA: 0x001027EC File Offset: 0x001009EC
		[__DynamicallyInvokable]
		public virtual Assembly ReferencedAssembly
		{
			[__DynamicallyInvokable]
			get
			{
				return this._containingAssembly;
			}
		}

		// Token: 0x17000A9B RID: 2715
		// (get) Token: 0x06004677 RID: 18039 RVA: 0x001027F4 File Offset: 0x001009F4
		[__DynamicallyInvokable]
		public virtual string FileName
		{
			[__DynamicallyInvokable]
			get
			{
				return this._containingFileName;
			}
		}

		// Token: 0x17000A9C RID: 2716
		// (get) Token: 0x06004678 RID: 18040 RVA: 0x001027FC File Offset: 0x001009FC
		[__DynamicallyInvokable]
		public virtual ResourceLocation ResourceLocation
		{
			[__DynamicallyInvokable]
			get
			{
				return this._resourceLocation;
			}
		}

		// Token: 0x04001CD9 RID: 7385
		private Assembly _containingAssembly;

		// Token: 0x04001CDA RID: 7386
		private string _containingFileName;

		// Token: 0x04001CDB RID: 7387
		private ResourceLocation _resourceLocation;
	}
}
