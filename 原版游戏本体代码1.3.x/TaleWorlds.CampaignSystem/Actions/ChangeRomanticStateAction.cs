using System;

namespace TaleWorlds.CampaignSystem.Actions
{
	// Token: 0x0200049F RID: 1183
	public static class ChangeRomanticStateAction
	{
		// Token: 0x06004978 RID: 18808 RVA: 0x00171F9C File Offset: 0x0017019C
		private static void ApplyInternal(Hero hero1, Hero hero2, Romance.RomanceLevelEnum toWhat)
		{
			Romance.SetRomanticState(hero1, hero2, toWhat);
			CampaignEventDispatcher.Instance.OnRomanticStateChanged(hero1, hero2, toWhat);
		}

		// Token: 0x06004979 RID: 18809 RVA: 0x00171FB3 File Offset: 0x001701B3
		public static void Apply(Hero person1, Hero person2, Romance.RomanceLevelEnum toWhat)
		{
			ChangeRomanticStateAction.ApplyInternal(person1, person2, toWhat);
		}
	}
}
