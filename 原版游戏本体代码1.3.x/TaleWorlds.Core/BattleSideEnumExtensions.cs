using System;

namespace TaleWorlds.Core
{
	// Token: 0x0200001F RID: 31
	public static class BattleSideEnumExtensions
	{
		// Token: 0x0600018B RID: 395 RVA: 0x0000699C File Offset: 0x00004B9C
		public static bool IsValid(this BattleSideEnum battleSide)
		{
			return battleSide >= BattleSideEnum.Defender && battleSide < BattleSideEnum.NumSides;
		}
	}
}
