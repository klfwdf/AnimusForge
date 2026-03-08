using System;

namespace TaleWorlds.Core
{
	// Token: 0x020000D4 RID: 212
	public static class TeamSideEnumExtensions
	{
		// Token: 0x06000B23 RID: 2851 RVA: 0x00024370 File Offset: 0x00022570
		public static bool IsValid(this TeamSideEnum teamSide)
		{
			return teamSide >= TeamSideEnum.PlayerTeam && teamSide < TeamSideEnum.NumSides;
		}
	}
}
