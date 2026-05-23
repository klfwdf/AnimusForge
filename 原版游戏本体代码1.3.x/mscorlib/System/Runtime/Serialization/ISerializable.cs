using System;
using System.Runtime.InteropServices;
using System.Security;

namespace System.Runtime.Serialization
{
	// Token: 0x02000735 RID: 1845
	[ComVisible(true)]
	public interface ISerializable
	{
		// Token: 0x060051C1 RID: 20929
		[SecurityCritical]
		void GetObjectData(SerializationInfo info, StreamingContext context);
	}
}
