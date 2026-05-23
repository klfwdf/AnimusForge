using System;
using System.Reflection;
using System.Runtime.InteropServices;

namespace System
{
	// Token: 0x02000092 RID: 146
	[ComVisible(true)]
	public class AssemblyLoadEventArgs : EventArgs
	{
		// Token: 0x170000C4 RID: 196
		// (get) Token: 0x06000776 RID: 1910 RVA: 0x0001A014 File Offset: 0x00018214
		public Assembly LoadedAssembly
		{
			get
			{
				return this._LoadedAssembly;
			}
		}

		// Token: 0x06000777 RID: 1911 RVA: 0x0001A01C File Offset: 0x0001821C
		public AssemblyLoadEventArgs(Assembly loadedAssembly)
		{
			this._LoadedAssembly = loadedAssembly;
		}

		// Token: 0x0400037C RID: 892
		private Assembly _LoadedAssembly;
	}
}
