using System;

namespace System.Runtime.CompilerServices
{
	// Token: 0x020008E3 RID: 2275
	[AttributeUsage(AttributeTargets.Assembly, Inherited = false, AllowMultiple = false)]
	[__DynamicallyInvokable]
	[Serializable]
	public sealed class RuntimeCompatibilityAttribute : Attribute
	{
		// Token: 0x06005DE1 RID: 24033 RVA: 0x00149AFD File Offset: 0x00147CFD
		[__DynamicallyInvokable]
		public RuntimeCompatibilityAttribute()
		{
		}

		// Token: 0x17001020 RID: 4128
		// (get) Token: 0x06005DE2 RID: 24034 RVA: 0x00149B05 File Offset: 0x00147D05
		// (set) Token: 0x06005DE3 RID: 24035 RVA: 0x00149B0D File Offset: 0x00147D0D
		[__DynamicallyInvokable]
		public bool WrapNonExceptionThrows
		{
			[__DynamicallyInvokable]
			get
			{
				return this.m_wrapNonExceptionThrows;
			}
			[__DynamicallyInvokable]
			set
			{
				this.m_wrapNonExceptionThrows = value;
			}
		}

		// Token: 0x04002A3D RID: 10813
		private bool m_wrapNonExceptionThrows;
	}
}
