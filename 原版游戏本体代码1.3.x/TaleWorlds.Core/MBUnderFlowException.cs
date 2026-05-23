using System;

namespace TaleWorlds.Core
{
	// Token: 0x020000A1 RID: 161
	public class MBUnderFlowException : MBException
	{
		// Token: 0x06000907 RID: 2311 RVA: 0x0001DBA2 File Offset: 0x0001BDA2
		public MBUnderFlowException()
			: base("The given value is less than the expected value.")
		{
		}

		// Token: 0x06000908 RID: 2312 RVA: 0x0001DBAF File Offset: 0x0001BDAF
		public MBUnderFlowException(string parameterName)
			: base("The given value is less than the expected value : " + parameterName)
		{
		}
	}
}
