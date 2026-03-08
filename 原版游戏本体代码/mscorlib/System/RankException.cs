using System;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace System
{
	// Token: 0x02000127 RID: 295
	[ComVisible(true)]
	[__DynamicallyInvokable]
	[Serializable]
	public class RankException : SystemException
	{
		// Token: 0x06001102 RID: 4354 RVA: 0x00033289 File Offset: 0x00031489
		[__DynamicallyInvokable]
		public RankException()
			: base(Environment.GetResourceString("Arg_RankException"))
		{
			base.SetErrorCode(-2146233065);
		}

		// Token: 0x06001103 RID: 4355 RVA: 0x000332A6 File Offset: 0x000314A6
		[__DynamicallyInvokable]
		public RankException(string message)
			: base(message)
		{
			base.SetErrorCode(-2146233065);
		}

		// Token: 0x06001104 RID: 4356 RVA: 0x000332BA File Offset: 0x000314BA
		[__DynamicallyInvokable]
		public RankException(string message, Exception innerException)
			: base(message, innerException)
		{
			base.SetErrorCode(-2146233065);
		}

		// Token: 0x06001105 RID: 4357 RVA: 0x000332CF File Offset: 0x000314CF
		protected RankException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}
	}
}
