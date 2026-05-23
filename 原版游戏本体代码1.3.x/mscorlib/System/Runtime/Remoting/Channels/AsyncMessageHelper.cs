using System;
using System.Reflection;

namespace System.Runtime.Remoting.Channels
{
	// Token: 0x02000833 RID: 2099
	internal static class AsyncMessageHelper
	{
		// Token: 0x060059BD RID: 22973 RVA: 0x0013C670 File Offset: 0x0013A870
		internal static void GetOutArgs(ParameterInfo[] syncParams, object[] syncArgs, object[] endArgs)
		{
			int num = 0;
			for (int i = 0; i < syncParams.Length; i++)
			{
				if (syncParams[i].IsOut || syncParams[i].ParameterType.IsByRef)
				{
					endArgs[num++] = syncArgs[i];
				}
			}
		}
	}
}
