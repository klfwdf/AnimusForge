using System;
using System.Runtime.Serialization;
using System.Security;

namespace System.Runtime.Remoting.Messaging
{
	// Token: 0x0200086C RID: 2156
	internal interface ISerializationRootObject
	{
		// Token: 0x06005BC7 RID: 23495
		[SecurityCritical]
		void RootSetObjectData(SerializationInfo info, StreamingContext ctx);
	}
}
