using System;
using System.Runtime.InteropServices;

namespace System.Diagnostics.SymbolStore
{
	// Token: 0x020003FE RID: 1022
	[ComVisible(true)]
	public interface ISymbolDocumentWriter
	{
		// Token: 0x060033A4 RID: 13220
		void SetSource(byte[] source);

		// Token: 0x060033A5 RID: 13221
		void SetCheckSum(Guid algorithmId, byte[] checkSum);
	}
}
