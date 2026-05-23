using System;
using System.Collections.ObjectModel;
using System.Reflection;

namespace System.Runtime.InteropServices.WindowsRuntime
{
	// Token: 0x020009F8 RID: 2552
	[ComVisible(false)]
	public class NamespaceResolveEventArgs : EventArgs
	{
		// Token: 0x17001159 RID: 4441
		// (get) Token: 0x060064E5 RID: 25829 RVA: 0x00157CFC File Offset: 0x00155EFC
		public string NamespaceName
		{
			get
			{
				return this._NamespaceName;
			}
		}

		// Token: 0x1700115A RID: 4442
		// (get) Token: 0x060064E6 RID: 25830 RVA: 0x00157D04 File Offset: 0x00155F04
		public Assembly RequestingAssembly
		{
			get
			{
				return this._RequestingAssembly;
			}
		}

		// Token: 0x1700115B RID: 4443
		// (get) Token: 0x060064E7 RID: 25831 RVA: 0x00157D0C File Offset: 0x00155F0C
		public Collection<Assembly> ResolvedAssemblies
		{
			get
			{
				return this._ResolvedAssemblies;
			}
		}

		// Token: 0x060064E8 RID: 25832 RVA: 0x00157D14 File Offset: 0x00155F14
		public NamespaceResolveEventArgs(string namespaceName, Assembly requestingAssembly)
		{
			this._NamespaceName = namespaceName;
			this._RequestingAssembly = requestingAssembly;
			this._ResolvedAssemblies = new Collection<Assembly>();
		}

		// Token: 0x04002D2D RID: 11565
		private string _NamespaceName;

		// Token: 0x04002D2E RID: 11566
		private Assembly _RequestingAssembly;

		// Token: 0x04002D2F RID: 11567
		private Collection<Assembly> _ResolvedAssemblies;
	}
}
