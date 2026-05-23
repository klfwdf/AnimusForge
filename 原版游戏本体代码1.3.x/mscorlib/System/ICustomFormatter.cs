using System;
using System.Runtime.InteropServices;

namespace System
{
	// Token: 0x020000ED RID: 237
	[ComVisible(true)]
	[__DynamicallyInvokable]
	public interface ICustomFormatter
	{
		// Token: 0x06000EFD RID: 3837
		[__DynamicallyInvokable]
		string Format(string format, object arg, IFormatProvider formatProvider);
	}
}
