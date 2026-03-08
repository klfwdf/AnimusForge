using System;

namespace System.Runtime.InteropServices.ComTypes
{
	// Token: 0x02000A28 RID: 2600
	[Guid("496B0ABF-CDEE-11d3-88E8-00902754C43A")]
	internal interface IEnumerator
	{
		// Token: 0x0600661C RID: 26140
		bool MoveNext();

		// Token: 0x1700118A RID: 4490
		// (get) Token: 0x0600661D RID: 26141
		object Current { get; }

		// Token: 0x0600661E RID: 26142
		void Reset();
	}
}
