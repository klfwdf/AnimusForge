using System;
using System.Collections;

namespace System.Runtime.InteropServices
{
	// Token: 0x0200097E RID: 2430
	[Obsolete("Use System.Runtime.InteropServices.ComTypes.IEnumerable instead. http://go.microsoft.com/fwlink/?linkid=14202", false)]
	[Guid("496B0ABE-CDEE-11d3-88E8-00902754C43A")]
	internal interface UCOMIEnumerable
	{
		// Token: 0x06006288 RID: 25224
		[DispId(-4)]
		IEnumerator GetEnumerator();
	}
}
