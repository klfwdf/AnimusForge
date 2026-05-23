using System;
using System.Runtime.InteropServices;

namespace System.Diagnostics.SymbolStore
{
	// Token: 0x020003FC RID: 1020
	[ComVisible(true)]
	public interface ISymbolBinder1
	{
		// Token: 0x06003399 RID: 13209
		ISymbolReader GetReader(IntPtr importer, string filename, string searchPath);
	}
}
