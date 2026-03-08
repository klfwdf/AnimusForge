using System;

namespace System.Runtime.InteropServices.WindowsRuntime
{
	// Token: 0x020009FE RID: 2558
	internal class IStringableHelper
	{
		// Token: 0x060064F2 RID: 25842 RVA: 0x00157DA8 File Offset: 0x00155FA8
		internal static string ToString(object obj)
		{
			IStringable stringable = obj as IStringable;
			if (stringable != null)
			{
				return stringable.ToString();
			}
			return obj.ToString();
		}
	}
}
