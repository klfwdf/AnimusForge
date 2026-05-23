using System;

namespace TaleWorlds.Library
{
	// Token: 0x02000030 RID: 48
	public static class UiStringHelper
	{
		// Token: 0x060001A1 RID: 417 RVA: 0x00006C6C File Offset: 0x00004E6C
		public static bool IsStringNoneOrEmptyForUi(string str)
		{
			return string.IsNullOrEmpty(str) || str == "none";
		}
	}
}
