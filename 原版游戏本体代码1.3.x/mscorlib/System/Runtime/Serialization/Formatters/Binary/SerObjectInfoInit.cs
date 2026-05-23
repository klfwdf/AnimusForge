using System;
using System.Collections;

namespace System.Runtime.Serialization.Formatters.Binary
{
	// Token: 0x020007A3 RID: 1955
	internal sealed class SerObjectInfoInit
	{
		// Token: 0x04002706 RID: 9990
		internal Hashtable seenBeforeTable = new Hashtable();

		// Token: 0x04002707 RID: 9991
		internal int objectInfoIdCount = 1;

		// Token: 0x04002708 RID: 9992
		internal SerStack oiPool = new SerStack("SerObjectInfo Pool");
	}
}
