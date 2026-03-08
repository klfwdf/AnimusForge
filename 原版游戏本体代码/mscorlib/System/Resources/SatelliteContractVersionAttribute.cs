using System;
using System.Runtime.InteropServices;

namespace System.Resources
{
	// Token: 0x0200039E RID: 926
	[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
	[ComVisible(true)]
	[__DynamicallyInvokable]
	public sealed class SatelliteContractVersionAttribute : Attribute
	{
		// Token: 0x06002D9D RID: 11677 RVA: 0x000AEB34 File Offset: 0x000ACD34
		[__DynamicallyInvokable]
		public SatelliteContractVersionAttribute(string version)
		{
			if (version == null)
			{
				throw new ArgumentNullException("version");
			}
			this._version = version;
		}

		// Token: 0x170005F0 RID: 1520
		// (get) Token: 0x06002D9E RID: 11678 RVA: 0x000AEB51 File Offset: 0x000ACD51
		[__DynamicallyInvokable]
		public string Version
		{
			[__DynamicallyInvokable]
			get
			{
				return this._version;
			}
		}

		// Token: 0x04001293 RID: 4755
		private string _version;
	}
}
