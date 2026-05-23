using System;

namespace System.Runtime.InteropServices
{
	// Token: 0x02000933 RID: 2355
	[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Method, AllowMultiple = false)]
	[ComVisible(false)]
	[__DynamicallyInvokable]
	public sealed class DefaultDllImportSearchPathsAttribute : Attribute
	{
		// Token: 0x06006038 RID: 24632 RVA: 0x0014BBF0 File Offset: 0x00149DF0
		[__DynamicallyInvokable]
		public DefaultDllImportSearchPathsAttribute(DllImportSearchPath paths)
		{
			this._paths = paths;
		}

		// Token: 0x170010E1 RID: 4321
		// (get) Token: 0x06006039 RID: 24633 RVA: 0x0014BBFF File Offset: 0x00149DFF
		[__DynamicallyInvokable]
		public DllImportSearchPath Paths
		{
			[__DynamicallyInvokable]
			get
			{
				return this._paths;
			}
		}

		// Token: 0x04002B12 RID: 11026
		internal DllImportSearchPath _paths;
	}
}
