using System;
using System.IO;

namespace Microsoft.Cci.Pdb
{
	// Token: 0x02000434 RID: 1076
	internal class PdbDebugException : IOException
	{
		// Token: 0x06001788 RID: 6024 RVA: 0x00048F3D File Offset: 0x0004713D
		internal PdbDebugException(string format, params object[] args)
			: base(string.Format(format, args))
		{
		}
	}
}
