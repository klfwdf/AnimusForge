using System;

namespace Microsoft.Cci.Pdb
{
	// Token: 0x0200043F RID: 1087
	internal class PdbLines
	{
		// Token: 0x060017B0 RID: 6064 RVA: 0x0004A948 File Offset: 0x00048B48
		internal PdbLines(PdbSource file, uint count)
		{
			this.file = file;
			this.lines = new PdbLine[count];
		}

		// Token: 0x04001031 RID: 4145
		internal PdbSource file;

		// Token: 0x04001032 RID: 4146
		internal PdbLine[] lines;
	}
}
