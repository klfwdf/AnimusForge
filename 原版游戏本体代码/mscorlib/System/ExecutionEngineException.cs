using System;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace System
{
	// Token: 0x02000084 RID: 132
	[Obsolete("This type previously indicated an unspecified fatal error in the runtime. The runtime no longer raises this exception so this type is obsolete.")]
	[ComVisible(true)]
	[Serializable]
	public sealed class ExecutionEngineException : SystemException
	{
		// Token: 0x060006D4 RID: 1748 RVA: 0x000179E6 File Offset: 0x00015BE6
		public ExecutionEngineException()
			: base(Environment.GetResourceString("Arg_ExecutionEngineException"))
		{
			base.SetErrorCode(-2146233082);
		}

		// Token: 0x060006D5 RID: 1749 RVA: 0x00017A03 File Offset: 0x00015C03
		public ExecutionEngineException(string message)
			: base(message)
		{
			base.SetErrorCode(-2146233082);
		}

		// Token: 0x060006D6 RID: 1750 RVA: 0x00017A17 File Offset: 0x00015C17
		public ExecutionEngineException(string message, Exception innerException)
			: base(message, innerException)
		{
			base.SetErrorCode(-2146233082);
		}

		// Token: 0x060006D7 RID: 1751 RVA: 0x00017A2C File Offset: 0x00015C2C
		internal ExecutionEngineException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}
	}
}
