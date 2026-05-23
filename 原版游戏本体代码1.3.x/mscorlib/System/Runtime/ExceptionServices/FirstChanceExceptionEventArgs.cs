using System;
using System.Runtime.ConstrainedExecution;

namespace System.Runtime.ExceptionServices
{
	// Token: 0x020007A9 RID: 1961
	public class FirstChanceExceptionEventArgs : EventArgs
	{
		// Token: 0x06005502 RID: 21762 RVA: 0x0012E2FB File Offset: 0x0012C4FB
		public FirstChanceExceptionEventArgs(Exception exception)
		{
			this.m_Exception = exception;
		}

		// Token: 0x17000DEF RID: 3567
		// (get) Token: 0x06005503 RID: 21763 RVA: 0x0012E30A File Offset: 0x0012C50A
		public Exception Exception
		{
			[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
			get
			{
				return this.m_Exception;
			}
		}

		// Token: 0x04002720 RID: 10016
		private Exception m_Exception;
	}
}
