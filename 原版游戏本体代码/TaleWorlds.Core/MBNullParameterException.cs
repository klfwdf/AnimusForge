using System;

namespace TaleWorlds.Core
{
	// Token: 0x020000A5 RID: 165
	public class MBNullParameterException : MBException
	{
		// Token: 0x0600090C RID: 2316 RVA: 0x0001DBFB File Offset: 0x0001BDFB
		public MBNullParameterException(string parameterName)
			: base("The parameter cannot be null : " + parameterName)
		{
		}
	}
}
