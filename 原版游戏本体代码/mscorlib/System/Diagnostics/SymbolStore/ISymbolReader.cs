using System;
using System.Runtime.InteropServices;

namespace System.Diagnostics.SymbolStore
{
	// Token: 0x02000401 RID: 1025
	[ComVisible(true)]
	public interface ISymbolReader
	{
		// Token: 0x060033B3 RID: 13235
		ISymbolDocument GetDocument(string url, Guid language, Guid languageVendor, Guid documentType);

		// Token: 0x060033B4 RID: 13236
		ISymbolDocument[] GetDocuments();

		// Token: 0x17000794 RID: 1940
		// (get) Token: 0x060033B5 RID: 13237
		SymbolToken UserEntryPoint { get; }

		// Token: 0x060033B6 RID: 13238
		ISymbolMethod GetMethod(SymbolToken method);

		// Token: 0x060033B7 RID: 13239
		ISymbolMethod GetMethod(SymbolToken method, int version);

		// Token: 0x060033B8 RID: 13240
		ISymbolVariable[] GetVariables(SymbolToken parent);

		// Token: 0x060033B9 RID: 13241
		ISymbolVariable[] GetGlobalVariables();

		// Token: 0x060033BA RID: 13242
		ISymbolMethod GetMethodFromDocumentPosition(ISymbolDocument document, int line, int column);

		// Token: 0x060033BB RID: 13243
		byte[] GetSymAttribute(SymbolToken parent, string name);

		// Token: 0x060033BC RID: 13244
		ISymbolNamespace[] GetNamespaces();
	}
}
