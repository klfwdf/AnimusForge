using System;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace System.IO
{
	// Token: 0x02000197 RID: 407
	[ComVisible(true)]
	[__DynamicallyInvokable]
	[Serializable]
	public class IOException : SystemException
	{
		// Token: 0x060018D1 RID: 6353 RVA: 0x0005112E File Offset: 0x0004F32E
		[__DynamicallyInvokable]
		public IOException()
			: base(Environment.GetResourceString("Arg_IOException"))
		{
			base.SetErrorCode(-2146232800);
		}

		// Token: 0x060018D2 RID: 6354 RVA: 0x0005114B File Offset: 0x0004F34B
		[__DynamicallyInvokable]
		public IOException(string message)
			: base(message)
		{
			base.SetErrorCode(-2146232800);
		}

		// Token: 0x060018D3 RID: 6355 RVA: 0x0005115F File Offset: 0x0004F35F
		[__DynamicallyInvokable]
		public IOException(string message, int hresult)
			: base(message)
		{
			base.SetErrorCode(hresult);
		}

		// Token: 0x060018D4 RID: 6356 RVA: 0x0005116F File Offset: 0x0004F36F
		internal IOException(string message, int hresult, string maybeFullPath)
			: base(message)
		{
			base.SetErrorCode(hresult);
			this._maybeFullPath = maybeFullPath;
		}

		// Token: 0x060018D5 RID: 6357 RVA: 0x00051186 File Offset: 0x0004F386
		[__DynamicallyInvokable]
		public IOException(string message, Exception innerException)
			: base(message, innerException)
		{
			base.SetErrorCode(-2146232800);
		}

		// Token: 0x060018D6 RID: 6358 RVA: 0x0005119B File Offset: 0x0004F39B
		protected IOException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}

		// Token: 0x040008B3 RID: 2227
		[NonSerialized]
		private string _maybeFullPath;
	}
}
