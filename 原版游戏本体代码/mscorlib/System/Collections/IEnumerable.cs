using System;
using System.Runtime.InteropServices;

namespace System.Collections
{
	// Token: 0x0200049E RID: 1182
	[Guid("496B0ABE-CDEE-11d3-88E8-00902754C43A")]
	[ComVisible(true)]
	[__DynamicallyInvokable]
	public interface IEnumerable
	{
		// Token: 0x060038BB RID: 14523
		[DispId(-4)]
		[__DynamicallyInvokable]
		IEnumerator GetEnumerator();
	}
}
