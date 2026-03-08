using System;
using System.Runtime.Serialization;

namespace TaleWorlds.Library
{
	// Token: 0x02000097 RID: 151
	public class TWException : ApplicationException
	{
		// Token: 0x0600056B RID: 1387 RVA: 0x00013546 File Offset: 0x00011746
		public TWException(string message, Exception innerException)
			: base(message, innerException)
		{
		}

		// Token: 0x0600056C RID: 1388 RVA: 0x00013550 File Offset: 0x00011750
		public TWException(string message)
			: base(message)
		{
		}

		// Token: 0x0600056D RID: 1389 RVA: 0x00013559 File Offset: 0x00011759
		public TWException()
		{
		}

		// Token: 0x0600056E RID: 1390 RVA: 0x00013561 File Offset: 0x00011761
		public TWException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}
	}
}
