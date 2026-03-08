using System;
using System.IO;

namespace Microsoft.Cci.Pdb
{
	// Token: 0x02000435 RID: 1077
	internal class PdbException : IOException
	{
		// Token: 0x06001789 RID: 6025 RVA: 0x00048F3D File Offset: 0x0004713D
		internal PdbException(string format, params object[] args)
			: base(string.Format(format, args))
		{
		}
	}
}
