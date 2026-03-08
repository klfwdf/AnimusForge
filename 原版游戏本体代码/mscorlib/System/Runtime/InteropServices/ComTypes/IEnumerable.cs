using System;
using System.Collections;

namespace System.Runtime.InteropServices.ComTypes
{
	// Token: 0x02000A27 RID: 2599
	[Guid("496B0ABE-CDEE-11d3-88E8-00902754C43A")]
	internal interface IEnumerable
	{
		// Token: 0x0600661B RID: 26139
		[DispId(-4)]
		IEnumerator GetEnumerator();
	}
}
