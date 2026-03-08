using System;

namespace TaleWorlds.Core
{
	// Token: 0x020000A6 RID: 166
	public class MBNotNullParameterException : MBException
	{
		// Token: 0x0600090D RID: 2317 RVA: 0x0001DC0E File Offset: 0x0001BE0E
		public MBNotNullParameterException(string parameterName)
			: base("The parameter must be null : " + parameterName)
		{
		}
	}
}
