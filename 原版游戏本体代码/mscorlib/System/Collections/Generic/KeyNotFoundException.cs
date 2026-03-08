using System;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace System.Collections.Generic
{
	// Token: 0x020004DA RID: 1242
	[ComVisible(true)]
	[__DynamicallyInvokable]
	[Serializable]
	public class KeyNotFoundException : SystemException, ISerializable
	{
		// Token: 0x06003AE4 RID: 15076 RVA: 0x000DFAE0 File Offset: 0x000DDCE0
		[__DynamicallyInvokable]
		public KeyNotFoundException()
			: base(Environment.GetResourceString("Arg_KeyNotFound"))
		{
			base.SetErrorCode(-2146232969);
		}

		// Token: 0x06003AE5 RID: 15077 RVA: 0x000DFAFD File Offset: 0x000DDCFD
		[__DynamicallyInvokable]
		public KeyNotFoundException(string message)
			: base(message)
		{
			base.SetErrorCode(-2146232969);
		}

		// Token: 0x06003AE6 RID: 15078 RVA: 0x000DFB11 File Offset: 0x000DDD11
		[__DynamicallyInvokable]
		public KeyNotFoundException(string message, Exception innerException)
			: base(message, innerException)
		{
			base.SetErrorCode(-2146232969);
		}

		// Token: 0x06003AE7 RID: 15079 RVA: 0x000DFB26 File Offset: 0x000DDD26
		protected KeyNotFoundException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}
	}
}
