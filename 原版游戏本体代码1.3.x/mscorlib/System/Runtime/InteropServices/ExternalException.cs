using System;
using System.Globalization;
using System.Runtime.Serialization;

namespace System.Runtime.InteropServices
{
	// Token: 0x02000946 RID: 2374
	[ComVisible(true)]
	[Serializable]
	public class ExternalException : SystemException
	{
		// Token: 0x06006079 RID: 24697 RVA: 0x0014C284 File Offset: 0x0014A484
		public ExternalException()
			: base(Environment.GetResourceString("Arg_ExternalException"))
		{
			base.SetErrorCode(-2147467259);
		}

		// Token: 0x0600607A RID: 24698 RVA: 0x0014C2A1 File Offset: 0x0014A4A1
		public ExternalException(string message)
			: base(message)
		{
			base.SetErrorCode(-2147467259);
		}

		// Token: 0x0600607B RID: 24699 RVA: 0x0014C2B5 File Offset: 0x0014A4B5
		public ExternalException(string message, Exception inner)
			: base(message, inner)
		{
			base.SetErrorCode(-2147467259);
		}

		// Token: 0x0600607C RID: 24700 RVA: 0x0014C2CA File Offset: 0x0014A4CA
		public ExternalException(string message, int errorCode)
			: base(message)
		{
			base.SetErrorCode(errorCode);
		}

		// Token: 0x0600607D RID: 24701 RVA: 0x0014C2DA File Offset: 0x0014A4DA
		protected ExternalException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}

		// Token: 0x170010F8 RID: 4344
		// (get) Token: 0x0600607E RID: 24702 RVA: 0x0014C2E4 File Offset: 0x0014A4E4
		public virtual int ErrorCode
		{
			get
			{
				return base.HResult;
			}
		}

		// Token: 0x0600607F RID: 24703 RVA: 0x0014C2EC File Offset: 0x0014A4EC
		public override string ToString()
		{
			string message = this.Message;
			string str = base.GetType().ToString();
			string text = str + " (0x" + base.HResult.ToString("X8", CultureInfo.InvariantCulture) + ")";
			if (!string.IsNullOrEmpty(message))
			{
				text = text + ": " + message;
			}
			Exception innerException = base.InnerException;
			if (innerException != null)
			{
				text = text + " ---> " + innerException.ToString();
			}
			if (this.StackTrace != null)
			{
				text = text + Environment.NewLine + this.StackTrace;
			}
			return text;
		}
	}
}
