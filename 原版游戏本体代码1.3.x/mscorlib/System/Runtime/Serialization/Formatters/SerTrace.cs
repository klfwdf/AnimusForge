using System;
using System.Diagnostics;

namespace System.Runtime.Serialization.Formatters
{
	// Token: 0x02000764 RID: 1892
	internal static class SerTrace
	{
		// Token: 0x06005313 RID: 21267 RVA: 0x00123D12 File Offset: 0x00121F12
		[Conditional("_LOGGING")]
		internal static void InfoLog(params object[] messages)
		{
		}

		// Token: 0x06005314 RID: 21268 RVA: 0x00123D14 File Offset: 0x00121F14
		[Conditional("SER_LOGGING")]
		internal static void Log(params object[] messages)
		{
			if (!(messages[0] is string))
			{
				messages[0] = messages[0].GetType().Name + " ";
				return;
			}
			int num = 0;
			object obj = messages[0];
			messages[num] = ((obj != null) ? obj.ToString() : null) + " ";
		}
	}
}
