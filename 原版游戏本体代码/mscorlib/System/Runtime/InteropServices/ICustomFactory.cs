using System;

namespace System.Runtime.InteropServices
{
	// Token: 0x02000965 RID: 2405
	[ComVisible(true)]
	public interface ICustomFactory
	{
		// Token: 0x06006227 RID: 25127
		MarshalByRefObject CreateInstance(Type serverType);
	}
}
