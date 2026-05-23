using System;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace System.Security.Policy
{
	// Token: 0x02000363 RID: 867
	[ComVisible(true)]
	[Serializable]
	public class PolicyException : SystemException
	{
		// Token: 0x06002AD3 RID: 10963 RVA: 0x0009E94F File Offset: 0x0009CB4F
		public PolicyException()
			: base(Environment.GetResourceString("Policy_Default"))
		{
			base.HResult = -2146233322;
		}

		// Token: 0x06002AD4 RID: 10964 RVA: 0x0009E96C File Offset: 0x0009CB6C
		public PolicyException(string message)
			: base(message)
		{
			base.HResult = -2146233322;
		}

		// Token: 0x06002AD5 RID: 10965 RVA: 0x0009E980 File Offset: 0x0009CB80
		public PolicyException(string message, Exception exception)
			: base(message, exception)
		{
			base.HResult = -2146233322;
		}

		// Token: 0x06002AD6 RID: 10966 RVA: 0x0009E995 File Offset: 0x0009CB95
		protected PolicyException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}

		// Token: 0x06002AD7 RID: 10967 RVA: 0x0009E99F File Offset: 0x0009CB9F
		internal PolicyException(string message, int hresult)
			: base(message)
		{
			base.HResult = hresult;
		}

		// Token: 0x06002AD8 RID: 10968 RVA: 0x0009E9AF File Offset: 0x0009CBAF
		internal PolicyException(string message, int hresult, Exception exception)
			: base(message, exception)
		{
			base.HResult = hresult;
		}
	}
}
