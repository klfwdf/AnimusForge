using System;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;

namespace System
{
	// Token: 0x02000156 RID: 342
	[ComVisible(true)]
	[Serializable]
	public class UnhandledExceptionEventArgs : EventArgs
	{
		// Token: 0x06001566 RID: 5478 RVA: 0x0003EC21 File Offset: 0x0003CE21
		public UnhandledExceptionEventArgs(object exception, bool isTerminating)
		{
			this._Exception = exception;
			this._IsTerminating = isTerminating;
		}

		// Token: 0x17000260 RID: 608
		// (get) Token: 0x06001567 RID: 5479 RVA: 0x0003EC37 File Offset: 0x0003CE37
		public object ExceptionObject
		{
			[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
			get
			{
				return this._Exception;
			}
		}

		// Token: 0x17000261 RID: 609
		// (get) Token: 0x06001568 RID: 5480 RVA: 0x0003EC3F File Offset: 0x0003CE3F
		public bool IsTerminating
		{
			[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
			get
			{
				return this._IsTerminating;
			}
		}

		// Token: 0x04000710 RID: 1808
		private object _Exception;

		// Token: 0x04000711 RID: 1809
		private bool _IsTerminating;
	}
}
