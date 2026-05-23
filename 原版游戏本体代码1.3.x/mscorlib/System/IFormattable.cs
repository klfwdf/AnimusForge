using System;
using System.Runtime.InteropServices;

namespace System
{
	// Token: 0x020000F0 RID: 240
	[ComVisible(true)]
	[__DynamicallyInvokable]
	public interface IFormattable
	{
		// Token: 0x06000F00 RID: 3840
		[__DynamicallyInvokable]
		string ToString(string format, IFormatProvider formatProvider);
	}
}
