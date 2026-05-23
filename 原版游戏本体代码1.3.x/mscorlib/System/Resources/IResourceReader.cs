using System;
using System.Collections;
using System.Runtime.InteropServices;

namespace System.Resources
{
	// Token: 0x0200038E RID: 910
	[ComVisible(true)]
	public interface IResourceReader : IEnumerable, IDisposable
	{
		// Token: 0x06002CEF RID: 11503
		void Close();

		// Token: 0x06002CF0 RID: 11504
		IDictionaryEnumerator GetEnumerator();
	}
}
