using System;

namespace Mono.CompilerServices.SymbolWriter
{
	// Token: 0x0200032F RID: 815
	internal class MonoSymbolFileException : Exception
	{
		// Token: 0x060014E4 RID: 5348 RVA: 0x0000EB0A File Offset: 0x0000CD0A
		public MonoSymbolFileException()
		{
		}

		// Token: 0x060014E5 RID: 5349 RVA: 0x00041D54 File Offset: 0x0003FF54
		public MonoSymbolFileException(string message, params object[] args)
			: base(string.Format(message, args))
		{
		}

		// Token: 0x060014E6 RID: 5350 RVA: 0x0000EB1B File Offset: 0x0000CD1B
		public MonoSymbolFileException(string message, Exception innerException)
			: base(message, innerException)
		{
		}
	}
}
