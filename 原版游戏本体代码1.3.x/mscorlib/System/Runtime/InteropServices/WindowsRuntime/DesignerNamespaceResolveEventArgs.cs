using System;
using System.Collections.ObjectModel;

namespace System.Runtime.InteropServices.WindowsRuntime
{
	// Token: 0x020009F9 RID: 2553
	[ComVisible(false)]
	public class DesignerNamespaceResolveEventArgs : EventArgs
	{
		// Token: 0x1700115C RID: 4444
		// (get) Token: 0x060064E9 RID: 25833 RVA: 0x00157D35 File Offset: 0x00155F35
		public string NamespaceName
		{
			get
			{
				return this._NamespaceName;
			}
		}

		// Token: 0x1700115D RID: 4445
		// (get) Token: 0x060064EA RID: 25834 RVA: 0x00157D3D File Offset: 0x00155F3D
		public Collection<string> ResolvedAssemblyFiles
		{
			get
			{
				return this._ResolvedAssemblyFiles;
			}
		}

		// Token: 0x060064EB RID: 25835 RVA: 0x00157D45 File Offset: 0x00155F45
		public DesignerNamespaceResolveEventArgs(string namespaceName)
		{
			this._NamespaceName = namespaceName;
			this._ResolvedAssemblyFiles = new Collection<string>();
		}

		// Token: 0x04002D30 RID: 11568
		private string _NamespaceName;

		// Token: 0x04002D31 RID: 11569
		private Collection<string> _ResolvedAssemblyFiles;
	}
}
