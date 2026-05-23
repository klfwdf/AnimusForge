using System;
using System.Globalization;
using System.Runtime.Serialization;
using System.Security;
using Microsoft.Win32;

namespace System.Runtime.InteropServices
{
	// Token: 0x02000944 RID: 2372
	[ComVisible(true)]
	[__DynamicallyInvokable]
	[Serializable]
	public class COMException : ExternalException
	{
		// Token: 0x06006065 RID: 24677 RVA: 0x0014C09D File Offset: 0x0014A29D
		[__DynamicallyInvokable]
		public COMException()
			: base(Environment.GetResourceString("Arg_COMException"))
		{
			base.SetErrorCode(-2147467259);
		}

		// Token: 0x06006066 RID: 24678 RVA: 0x0014C0BA File Offset: 0x0014A2BA
		[__DynamicallyInvokable]
		public COMException(string message)
			: base(message)
		{
			base.SetErrorCode(-2147467259);
		}

		// Token: 0x06006067 RID: 24679 RVA: 0x0014C0CE File Offset: 0x0014A2CE
		[__DynamicallyInvokable]
		public COMException(string message, Exception inner)
			: base(message, inner)
		{
			base.SetErrorCode(-2147467259);
		}

		// Token: 0x06006068 RID: 24680 RVA: 0x0014C0E3 File Offset: 0x0014A2E3
		[__DynamicallyInvokable]
		public COMException(string message, int errorCode)
			: base(message)
		{
			base.SetErrorCode(errorCode);
		}

		// Token: 0x06006069 RID: 24681 RVA: 0x0014C0F3 File Offset: 0x0014A2F3
		[SecuritySafeCritical]
		internal COMException(int hresult)
			: base(Win32Native.GetMessage(hresult))
		{
			base.SetErrorCode(hresult);
		}

		// Token: 0x0600606A RID: 24682 RVA: 0x0014C108 File Offset: 0x0014A308
		internal COMException(string message, int hresult, Exception inner)
			: base(message, inner)
		{
			base.SetErrorCode(hresult);
		}

		// Token: 0x0600606B RID: 24683 RVA: 0x0014C119 File Offset: 0x0014A319
		protected COMException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}

		// Token: 0x0600606C RID: 24684 RVA: 0x0014C124 File Offset: 0x0014A324
		public override string ToString()
		{
			string message = this.Message;
			string str = base.GetType().ToString();
			string text = str + " (0x" + base.HResult.ToString("X8", CultureInfo.InvariantCulture) + ")";
			if (message != null && message.Length > 0)
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
