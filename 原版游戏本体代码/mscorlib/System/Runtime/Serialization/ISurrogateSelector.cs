using System;
using System.Runtime.InteropServices;
using System.Security;

namespace System.Runtime.Serialization
{
	// Token: 0x02000737 RID: 1847
	[ComVisible(true)]
	public interface ISurrogateSelector
	{
		// Token: 0x060051C4 RID: 20932
		[SecurityCritical]
		void ChainSelector(ISurrogateSelector selector);

		// Token: 0x060051C5 RID: 20933
		[SecurityCritical]
		ISerializationSurrogate GetSurrogate(Type type, StreamingContext context, out ISurrogateSelector selector);

		// Token: 0x060051C6 RID: 20934
		[SecurityCritical]
		ISurrogateSelector GetNextSelector();
	}
}
