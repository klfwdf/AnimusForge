using System;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace System.Resources
{
	// Token: 0x02000391 RID: 913
	[ComVisible(true)]
	[__DynamicallyInvokable]
	[Serializable]
	public class MissingManifestResourceException : SystemException
	{
		// Token: 0x06002D04 RID: 11524 RVA: 0x000AA233 File Offset: 0x000A8433
		[__DynamicallyInvokable]
		public MissingManifestResourceException()
			: base(Environment.GetResourceString("Arg_MissingManifestResourceException"))
		{
			base.SetErrorCode(-2146233038);
		}

		// Token: 0x06002D05 RID: 11525 RVA: 0x000AA250 File Offset: 0x000A8450
		[__DynamicallyInvokable]
		public MissingManifestResourceException(string message)
			: base(message)
		{
			base.SetErrorCode(-2146233038);
		}

		// Token: 0x06002D06 RID: 11526 RVA: 0x000AA264 File Offset: 0x000A8464
		[__DynamicallyInvokable]
		public MissingManifestResourceException(string message, Exception inner)
			: base(message, inner)
		{
			base.SetErrorCode(-2146233038);
		}

		// Token: 0x06002D07 RID: 11527 RVA: 0x000AA279 File Offset: 0x000A8479
		protected MissingManifestResourceException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}
	}
}
