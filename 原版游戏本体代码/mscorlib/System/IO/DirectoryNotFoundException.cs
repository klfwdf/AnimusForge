using System;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace System.IO
{
	// Token: 0x0200017D RID: 381
	[ComVisible(true)]
	[__DynamicallyInvokable]
	[Serializable]
	public class DirectoryNotFoundException : IOException
	{
		// Token: 0x06001782 RID: 6018 RVA: 0x0004B653 File Offset: 0x00049853
		[__DynamicallyInvokable]
		public DirectoryNotFoundException()
			: base(Environment.GetResourceString("Arg_DirectoryNotFoundException"))
		{
			base.SetErrorCode(-2147024893);
		}

		// Token: 0x06001783 RID: 6019 RVA: 0x0004B670 File Offset: 0x00049870
		[__DynamicallyInvokable]
		public DirectoryNotFoundException(string message)
			: base(message)
		{
			base.SetErrorCode(-2147024893);
		}

		// Token: 0x06001784 RID: 6020 RVA: 0x0004B684 File Offset: 0x00049884
		[__DynamicallyInvokable]
		public DirectoryNotFoundException(string message, Exception innerException)
			: base(message, innerException)
		{
			base.SetErrorCode(-2147024893);
		}

		// Token: 0x06001785 RID: 6021 RVA: 0x0004B699 File Offset: 0x00049899
		protected DirectoryNotFoundException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}
	}
}
