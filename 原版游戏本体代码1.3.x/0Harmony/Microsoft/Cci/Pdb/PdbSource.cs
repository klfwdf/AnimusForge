using System;

namespace Microsoft.Cci.Pdb
{
	// Token: 0x02000443 RID: 1091
	internal class PdbSource
	{
		// Token: 0x060017BA RID: 6074 RVA: 0x0004AD0D File Offset: 0x00048F0D
		internal PdbSource(string name, Guid doctype, Guid language, Guid vendor, Guid checksumAlgorithm, byte[] checksum)
		{
			this.name = name;
			this.doctype = doctype;
			this.language = language;
			this.vendor = vendor;
			this.checksumAlgorithm = checksumAlgorithm;
			this.checksum = checksum;
		}

		// Token: 0x04001040 RID: 4160
		internal string name;

		// Token: 0x04001041 RID: 4161
		internal Guid doctype;

		// Token: 0x04001042 RID: 4162
		internal Guid language;

		// Token: 0x04001043 RID: 4163
		internal Guid vendor;

		// Token: 0x04001044 RID: 4164
		internal Guid checksumAlgorithm;

		// Token: 0x04001045 RID: 4165
		internal byte[] checksum;
	}
}
