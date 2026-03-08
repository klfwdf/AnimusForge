using System;

namespace TaleWorlds.Core
{
	// Token: 0x020000A0 RID: 160
	public class MBTypeMismatchException : MBException
	{
		// Token: 0x06000906 RID: 2310 RVA: 0x0001DB8F File Offset: 0x0001BD8F
		public MBTypeMismatchException(string exceptionString)
			: base("Type Does not match with the expected one. " + exceptionString)
		{
		}
	}
}
