using System;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace System
{
	// Token: 0x02000083 RID: 131
	[ComVisible(true)]
	[__DynamicallyInvokable]
	[Serializable]
	public sealed class DataMisalignedException : SystemException
	{
		// Token: 0x060006D0 RID: 1744 RVA: 0x00017996 File Offset: 0x00015B96
		[__DynamicallyInvokable]
		public DataMisalignedException()
			: base(Environment.GetResourceString("Arg_DataMisalignedException"))
		{
			base.SetErrorCode(-2146233023);
		}

		// Token: 0x060006D1 RID: 1745 RVA: 0x000179B3 File Offset: 0x00015BB3
		[__DynamicallyInvokable]
		public DataMisalignedException(string message)
			: base(message)
		{
			base.SetErrorCode(-2146233023);
		}

		// Token: 0x060006D2 RID: 1746 RVA: 0x000179C7 File Offset: 0x00015BC7
		[__DynamicallyInvokable]
		public DataMisalignedException(string message, Exception innerException)
			: base(message, innerException)
		{
			base.SetErrorCode(-2146233023);
		}

		// Token: 0x060006D3 RID: 1747 RVA: 0x000179DC File Offset: 0x00015BDC
		internal DataMisalignedException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}
	}
}
