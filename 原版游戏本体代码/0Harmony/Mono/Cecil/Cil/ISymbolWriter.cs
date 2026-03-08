using System;

namespace Mono.Cecil.Cil
{
	// Token: 0x02000326 RID: 806
	internal interface ISymbolWriter : IDisposable
	{
		// Token: 0x060014D1 RID: 5329
		ISymbolReaderProvider GetReaderProvider();

		// Token: 0x060014D2 RID: 5330
		ImageDebugHeader GetDebugHeader();

		// Token: 0x060014D3 RID: 5331
		void Write(MethodDebugInformation info);

		// Token: 0x060014D4 RID: 5332
		void Write();

		// Token: 0x060014D5 RID: 5333
		void Write(ICustomDebugInformationProvider provider);
	}
}
