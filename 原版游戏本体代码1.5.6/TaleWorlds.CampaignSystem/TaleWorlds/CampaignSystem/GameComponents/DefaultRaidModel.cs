using System.Collections.Generic;
using Helpers;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.GameComponents;

public class DefaultRaidModel : RaidModel
{
	private MBReadOnlyList<(ItemObject, float)> _commonLootItems;

	private MBReadOnlyList<(ItemObject, float)> CommonLootItemSpawnChances
	{
		get
		{
			if (_commonLootItems == null)
			{
				List<(ItemObject, float)> list = new List<(ItemObject, float)>
				{
					(DefaultItems.Hides, 1f),
					(DefaultItems.HardWood, 1f),
					(DefaultItems.Tools, 1f),
					(DefaultItems.Grain, 1f),
					(Campaign.Current.ObjectManager.GetObject<ItemObject>("linen"), 1f),
					(Campaign.Current.ObjectManager.GetObject<ItemObject>("sheep"), 1f),
					(Campaign.Current.ObjectManager.GetObject<ItemObject>("mule"), 1f),
					(Campaign.Current.ObjectManager.GetObject<ItemObject>("pottery"), 1f)
				};
				for (int num = list.Count - 1; num >= 0; num--)
				{
					ItemObject item = list[num].Item1;
					float item2 = 100f / ((float)item.Value + 1f);
					list[num] = (item, item2);
				}
				_commonLootItems = new MBReadOnlyList<(ItemObject, float)>(list);
			}
			return _commonLootItems;
		}
	}

	public override int GoldRewardForEachLostHearth => 4;

	public override ExplainedNumber CalculateHitDamage(MapEventSide attackerSide, float settlementHitPoints)
	{
		float num = (MathF.Sqrt(attackerSide.TroopCount) + 5f) / 900f;
		ExplainedNumber stat = new ExplainedNumber(num * (float)CampaignTime.DeltaTime.ToHours);
		foreach (MapEventParty party in attackerSide.Parties)
		{
			PerkHelper.AddPerkBonusForParty(DefaultPerks.Roguery.NoRestForTheWicked, party.Party.MobileParty, isPrimaryBonus: false, ref stat);
		}
		return stat;
	}

	public override float GetRaidLootMultiplier(PartyBase receivingParty)
	{
		float num = 1f;
		MobileParty mobileParty = receivingParty.MobileParty;
		Hero hero = mobileParty?.Army?.LeaderParty?.LeaderHero ?? mobileParty?.LeaderHero;
		if (hero != null)
		{
			num += TraitEffectHelper.GetTraitEffectBonus(hero, DefaultPersonalityTraitEffects.MercyRaidLootEffect);
		}
		return num;
	}

	public override MBReadOnlyList<(ItemObject, float)> GetCommonLootItemScores()
	{
		return CommonLootItemSpawnChances;
	}
}
