using System;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace System
{
	// Token: 0x020000BF RID: 191
	[ComVisible(true)]
	[Serializable]
	public class TypeUnloadedException : SystemException
	{
		// Token: 0x06000AFF RID: 2815 RVA: 0x00022BC6 File Offset: 0x00020DC6
		public TypeUnloadedException()
			: base(Environment.GetResourceString("Arg_TypeUnloadedException"))
		{
			base.SetErrorCode(-2146234349);
		}

		// Token: 0x06000B00 RID: 2816 RVA: 0x00022BE3 File Offset: 0x00020DE3
		public TypeUnloadedException(string message)
			: base(message)
		{
			base.SetErrorCode(-2146234349);
		}

		// Token: 0x06000B01 RID: 2817 RVA: 0x00022BF7 File Offset: 0x00020DF7
		public TypeUnloadedException(string message, Exception innerException)
			: base(message, innerException)
		{
			base.SetErrorCode(-2146234349);
		}

		// Token: 0x06000B02 RID: 2818 RVA: 0x00022C0C File Offset: 0x00020E0C
		protected TypeUnloadedException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}
	}
}
