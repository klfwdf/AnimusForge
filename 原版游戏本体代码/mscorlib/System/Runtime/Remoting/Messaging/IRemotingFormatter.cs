using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace System.Runtime.Remoting.Messaging
{
	// Token: 0x0200085D RID: 2141
	[ComVisible(true)]
	public interface IRemotingFormatter : IFormatter
	{
		// Token: 0x06005A97 RID: 23191
		object Deserialize(Stream serializationStream, HeaderHandler handler);

		// Token: 0x06005A98 RID: 23192
		void Serialize(Stream serializationStream, object graph, Header[] headers);
	}
}
