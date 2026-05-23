using System;
using System.Runtime.InteropServices;

namespace System.Collections
{
	// Token: 0x020004A1 RID: 1185
	[Obsolete("Please use IEqualityComparer instead.")]
	[ComVisible(true)]
	public interface IHashCodeProvider
	{
		// Token: 0x060038C1 RID: 14529
		int GetHashCode(object obj);
	}
}
