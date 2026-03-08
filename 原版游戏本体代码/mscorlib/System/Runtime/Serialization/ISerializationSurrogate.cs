using System;
using System.Runtime.InteropServices;
using System.Security;

namespace System.Runtime.Serialization
{
	// Token: 0x02000736 RID: 1846
	[ComVisible(true)]
	public interface ISerializationSurrogate
	{
		// Token: 0x060051C2 RID: 20930
		[SecurityCritical]
		void GetObjectData(object obj, SerializationInfo info, StreamingContext context);

		// Token: 0x060051C3 RID: 20931
		[SecurityCritical]
		object SetObjectData(object obj, SerializationInfo info, StreamingContext context, ISurrogateSelector selector);
	}
}
