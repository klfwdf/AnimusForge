using System;
using Mono.Collections.Generic;

namespace Mono.Cecil.Cil
{
	// Token: 0x0200031F RID: 799
	internal interface ISymbolReader : IDisposable
	{
		// Token: 0x060014BE RID: 5310
		ISymbolWriterProvider GetWriterProvider();

		// Token: 0x060014BF RID: 5311
		bool ProcessDebugHeader(ImageDebugHeader header);

		// Token: 0x060014C0 RID: 5312
		MethodDebugInformation Read(MethodDefinition method);

		// Token: 0x060014C1 RID: 5313
		Collection<CustomDebugInformation> Read(ICustomDebugInformationProvider provider);
	}
}
