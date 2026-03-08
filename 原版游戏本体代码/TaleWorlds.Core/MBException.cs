using System;
using System.Runtime.Serialization;

namespace TaleWorlds.Core
{
	// Token: 0x0200009F RID: 159
	public class MBException : ApplicationException
	{
		// Token: 0x06000902 RID: 2306 RVA: 0x0001DB6A File Offset: 0x0001BD6A
		public MBException(string message, Exception innerException)
			: base(message, innerException)
		{
		}

		// Token: 0x06000903 RID: 2307 RVA: 0x0001DB74 File Offset: 0x0001BD74
		public MBException(string message)
			: base(message)
		{
		}

		// Token: 0x06000904 RID: 2308 RVA: 0x0001DB7D File Offset: 0x0001BD7D
		public MBException()
		{
		}

		// Token: 0x06000905 RID: 2309 RVA: 0x0001DB85 File Offset: 0x0001BD85
		public MBException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}
	}
}
