using System;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace System.IO
{
	// Token: 0x02000180 RID: 384
	[ComVisible(true)]
	[Serializable]
	public class DriveNotFoundException : IOException
	{
		// Token: 0x06001795 RID: 6037 RVA: 0x0004BB09 File Offset: 0x00049D09
		public DriveNotFoundException()
			: base(Environment.GetResourceString("Arg_DriveNotFoundException"))
		{
			base.SetErrorCode(-2147024893);
		}

		// Token: 0x06001796 RID: 6038 RVA: 0x0004BB26 File Offset: 0x00049D26
		public DriveNotFoundException(string message)
			: base(message)
		{
			base.SetErrorCode(-2147024893);
		}

		// Token: 0x06001797 RID: 6039 RVA: 0x0004BB3A File Offset: 0x00049D3A
		public DriveNotFoundException(string message, Exception innerException)
			: base(message, innerException)
		{
			base.SetErrorCode(-2147024893);
		}

		// Token: 0x06001798 RID: 6040 RVA: 0x0004BB4F File Offset: 0x00049D4F
		protected DriveNotFoundException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}
	}
}
