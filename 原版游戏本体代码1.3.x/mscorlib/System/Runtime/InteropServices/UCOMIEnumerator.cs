using System;

namespace System.Runtime.InteropServices
{
	// Token: 0x0200097F RID: 2431
	[Obsolete("Use System.Runtime.InteropServices.ComTypes.IEnumerator instead. http://go.microsoft.com/fwlink/?linkid=14202", false)]
	[Guid("496B0ABF-CDEE-11d3-88E8-00902754C43A")]
	internal interface UCOMIEnumerator
	{
		// Token: 0x06006289 RID: 25225
		bool MoveNext();

		// Token: 0x17001116 RID: 4374
		// (get) Token: 0x0600628A RID: 25226
		object Current { get; }

		// Token: 0x0600628B RID: 25227
		void Reset();
	}
}
