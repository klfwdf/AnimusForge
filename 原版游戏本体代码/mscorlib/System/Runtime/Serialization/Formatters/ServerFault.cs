using System;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Metadata;

namespace System.Runtime.Serialization.Formatters
{
	// Token: 0x02000767 RID: 1895
	[SoapType(Embedded = true)]
	[ComVisible(true)]
	[Serializable]
	public sealed class ServerFault
	{
		// Token: 0x0600532E RID: 21294 RVA: 0x00123F91 File Offset: 0x00122191
		internal ServerFault(Exception exception)
		{
			this.exception = exception;
		}

		// Token: 0x0600532F RID: 21295 RVA: 0x00123FA0 File Offset: 0x001221A0
		public ServerFault(string exceptionType, string message, string stackTrace)
		{
			this.exceptionType = exceptionType;
			this.message = message;
			this.stackTrace = stackTrace;
		}

		// Token: 0x17000DCA RID: 3530
		// (get) Token: 0x06005330 RID: 21296 RVA: 0x00123FBD File Offset: 0x001221BD
		// (set) Token: 0x06005331 RID: 21297 RVA: 0x00123FC5 File Offset: 0x001221C5
		public string ExceptionType
		{
			get
			{
				return this.exceptionType;
			}
			set
			{
				this.exceptionType = value;
			}
		}

		// Token: 0x17000DCB RID: 3531
		// (get) Token: 0x06005332 RID: 21298 RVA: 0x00123FCE File Offset: 0x001221CE
		// (set) Token: 0x06005333 RID: 21299 RVA: 0x00123FD6 File Offset: 0x001221D6
		public string ExceptionMessage
		{
			get
			{
				return this.message;
			}
			set
			{
				this.message = value;
			}
		}

		// Token: 0x17000DCC RID: 3532
		// (get) Token: 0x06005334 RID: 21300 RVA: 0x00123FDF File Offset: 0x001221DF
		// (set) Token: 0x06005335 RID: 21301 RVA: 0x00123FE7 File Offset: 0x001221E7
		public string StackTrace
		{
			get
			{
				return this.stackTrace;
			}
			set
			{
				this.stackTrace = value;
			}
		}

		// Token: 0x17000DCD RID: 3533
		// (get) Token: 0x06005336 RID: 21302 RVA: 0x00123FF0 File Offset: 0x001221F0
		internal Exception Exception
		{
			get
			{
				return this.exception;
			}
		}

		// Token: 0x040024E1 RID: 9441
		private string exceptionType;

		// Token: 0x040024E2 RID: 9442
		private string message;

		// Token: 0x040024E3 RID: 9443
		private string stackTrace;

		// Token: 0x040024E4 RID: 9444
		private Exception exception;
	}
}
