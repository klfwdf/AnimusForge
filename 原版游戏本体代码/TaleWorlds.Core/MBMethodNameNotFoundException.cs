using System;

namespace TaleWorlds.Core
{
	// Token: 0x020000A3 RID: 163
	public class MBMethodNameNotFoundException : MBException
	{
		// Token: 0x0600090A RID: 2314 RVA: 0x0001DBD5 File Offset: 0x0001BDD5
		public MBMethodNameNotFoundException(string methodName)
			: base("Unable to find method " + methodName)
		{
		}
	}
}
