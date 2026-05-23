using System;
using System.Runtime.Serialization;
using System.Security;

namespace System.Runtime.Remoting.Messaging
{
	// Token: 0x0200087D RID: 2173
	internal class ObjRefSurrogate : ISerializationSurrogate
	{
		// Token: 0x06005C5B RID: 23643 RVA: 0x00143A1A File Offset: 0x00141C1A
		[SecurityCritical]
		public virtual void GetObjectData(object obj, SerializationInfo info, StreamingContext context)
		{
			if (obj == null)
			{
				throw new ArgumentNullException("obj");
			}
			if (info == null)
			{
				throw new ArgumentNullException("info");
			}
			((ObjRef)obj).GetObjectData(info, context);
			info.AddValue("fIsMarshalled", 0);
		}

		// Token: 0x06005C5C RID: 23644 RVA: 0x00143A51 File Offset: 0x00141C51
		[SecurityCritical]
		public virtual object SetObjectData(object obj, SerializationInfo info, StreamingContext context, ISurrogateSelector selector)
		{
			throw new NotSupportedException(Environment.GetResourceString("NotSupported_PopulateData"));
		}
	}
}
