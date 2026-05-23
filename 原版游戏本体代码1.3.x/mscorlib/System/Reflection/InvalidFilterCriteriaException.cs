using System;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace System.Reflection
{
	// Token: 0x020005EF RID: 1519
	[ComVisible(true)]
	[Serializable]
	public class InvalidFilterCriteriaException : ApplicationException
	{
		// Token: 0x06004661 RID: 18017 RVA: 0x001026F9 File Offset: 0x001008F9
		public InvalidFilterCriteriaException()
			: base(Environment.GetResourceString("Arg_InvalidFilterCriteriaException"))
		{
			base.SetErrorCode(-2146232831);
		}

		// Token: 0x06004662 RID: 18018 RVA: 0x00102716 File Offset: 0x00100916
		public InvalidFilterCriteriaException(string message)
			: base(message)
		{
			base.SetErrorCode(-2146232831);
		}

		// Token: 0x06004663 RID: 18019 RVA: 0x0010272A File Offset: 0x0010092A
		public InvalidFilterCriteriaException(string message, Exception inner)
			: base(message, inner)
		{
			base.SetErrorCode(-2146232831);
		}

		// Token: 0x06004664 RID: 18020 RVA: 0x0010273F File Offset: 0x0010093F
		protected InvalidFilterCriteriaException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}
	}
}
