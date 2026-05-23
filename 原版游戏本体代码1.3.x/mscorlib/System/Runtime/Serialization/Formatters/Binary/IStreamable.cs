using System;
using System.Security;

namespace System.Runtime.Serialization.Formatters.Binary
{
	// Token: 0x02000780 RID: 1920
	internal interface IStreamable
	{
		// Token: 0x060053C1 RID: 21441
		[SecurityCritical]
		void Read(__BinaryParser input);

		// Token: 0x060053C2 RID: 21442
		void Write(__BinaryWriter sout);
	}
}
