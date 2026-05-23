using System;

namespace TaleWorlds.Core
{
	// Token: 0x020000A4 RID: 164
	public class MBInvalidParameterException : MBException
	{
		// Token: 0x0600090B RID: 2315 RVA: 0x0001DBE8 File Offset: 0x0001BDE8
		public MBInvalidParameterException(string parameterName)
			: base("The parameter must be valid : " + parameterName)
		{
		}
	}
}
