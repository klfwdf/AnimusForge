using System;

namespace System.Runtime.CompilerServices
{
	// Token: 0x020008B9 RID: 2233
	[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true, Inherited = false)]
	[__DynamicallyInvokable]
	public sealed class InternalsVisibleToAttribute : Attribute
	{
		// Token: 0x06005DAE RID: 23982 RVA: 0x00149890 File Offset: 0x00147A90
		[__DynamicallyInvokable]
		public InternalsVisibleToAttribute(string assemblyName)
		{
			this._assemblyName = assemblyName;
		}

		// Token: 0x17001016 RID: 4118
		// (get) Token: 0x06005DAF RID: 23983 RVA: 0x001498A6 File Offset: 0x00147AA6
		[__DynamicallyInvokable]
		public string AssemblyName
		{
			[__DynamicallyInvokable]
			get
			{
				return this._assemblyName;
			}
		}

		// Token: 0x17001017 RID: 4119
		// (get) Token: 0x06005DB0 RID: 23984 RVA: 0x001498AE File Offset: 0x00147AAE
		// (set) Token: 0x06005DB1 RID: 23985 RVA: 0x001498B6 File Offset: 0x00147AB6
		public bool AllInternalsVisible
		{
			get
			{
				return this._allInternalsVisible;
			}
			set
			{
				this._allInternalsVisible = value;
			}
		}

		// Token: 0x04002A19 RID: 10777
		private string _assemblyName;

		// Token: 0x04002A1A RID: 10778
		private bool _allInternalsVisible = true;
	}
}
