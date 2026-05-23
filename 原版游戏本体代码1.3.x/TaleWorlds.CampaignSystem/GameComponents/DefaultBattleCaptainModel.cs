using System;
using System.Collections.Generic;
using Helpers;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.GameComponents
{
	// Token: 0x020000F4 RID: 244
	public class DefaultBattleCaptainModel : BattleCaptainModel
	{
		// Token: 0x06001646 RID: 5702 RVA: 0x00065AC4 File Offset: 0x00063CC4
		public override float GetCaptainRatingForTroopUsages(Hero hero, TroopUsageFlags flag, out List<PerkObject> compatiblePerks)
		{
			float num = 0f;
			compatiblePerks = new List<PerkObject>();
			foreach (PerkObject perkObject in PerkHelper.GetCaptainPerksForTroopUsages(flag))
			{
				if (hero.GetPerkValue(perkObject))
				{
					num += perkObject.RequiredSkillValue;
					compatiblePerks.Add(perkObject);
				}
			}
			num /= 1650f;
			return num;
		}
	}
}
