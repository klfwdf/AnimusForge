using System;

namespace TaleWorlds.Core
{
	// Token: 0x020000A2 RID: 162
	public class MBOutOfRangeException : MBException
	{
		// Token: 0x06000909 RID: 2313 RVA: 0x0001DBC2 File Offset: 0x0001BDC2
		public MBOutOfRangeException(string parameterName)
			: base("The given value is out of range : " + parameterName)
		{
		}
	}
}
