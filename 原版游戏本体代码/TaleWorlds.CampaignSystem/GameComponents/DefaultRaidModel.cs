using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.GameComponents
{
	// Token: 0x02000143 RID: 323
	public class DefaultRaidModel : RaidModel
	{
		// Token: 0x17000695 RID: 1685
		// (get) Token: 0x0600197E RID: 6526 RVA: 0x0007F468 File Offset: 0x0007D668
		private MBReadOnlyList<ValueTuple<ItemObject, float>> CommonLootItemSpawnChances
		{
			get
			{
				if (this._commonLootItems == null)
				{
					List<ValueTuple<ItemObject, float>> list = new List<ValueTuple<ItemObject, float>>
					{
						new ValueTuple<ItemObject, float>(DefaultItems.Hides, 1f),
						new ValueTuple<ItemObject, float>(DefaultItems.HardWood, 1f),
						new ValueTuple<ItemObject, float>(DefaultItems.Tools, 1f),
						new ValueTuple<ItemObject, float>(DefaultItems.Grain, 1f),
						new ValueTuple<ItemObject, float>(Campaign.Current.ObjectManager.GetObject<ItemObject>("linen"), 1f),
						new ValueTuple<ItemObject, float>(Campaign.Current.ObjectManager.GetObject<ItemObject>("sheep"), 1f),
						new ValueTuple<ItemObject, float>(Campaign.Current.ObjectManager.GetObject<ItemObject>("mule"), 1f),
						new ValueTuple<ItemObject, float>(Campaign.Current.ObjectManager.GetObject<ItemObject>("pottery"), 1f)
					};
					for (int i = list.Count - 1; i >= 0; i--)
					{
						ItemObject item = list[i].Item1;
						float item2 = 100f / ((float)item.Value + 1f);
						list[i] = new ValueTuple<ItemObject, float>(item, item2);
					}
					this._commonLootItems = new MBReadOnlyList<ValueTuple<ItemObject, float>>(list);
				}
				return this._commonLootItems;
			}
		}

		// Token: 0x0600197F RID: 6527 RVA: 0x0007F5C0 File Offset: 0x0007D7C0
		public override ExplainedNumber CalculateHitDamage(MapEventSide attackerSide, float settlementHitPoints)
		{
			float num = (MathF.Sqrt((float)attackerSide.TroopCount) + 5f) / 900f;
			ExplainedNumber result = new ExplainedNumber(num * (float)CampaignTime.DeltaTime.ToHours, false, null);
			foreach (MapEventParty mapEventParty in attackerSide.Parties)
			{
				MobileParty mobileParty = mapEventParty.Party.MobileParty;
				if (((mobileParty != null) ? mobileParty.LeaderHero : null) != null && mapEventParty.Party.MobileParty.LeaderHero.GetPerkValue(DefaultPerks.Roguery.NoRestForTheWicked))
				{
					result.AddFactor(DefaultPerks.Roguery.NoRestForTheWicked.SecondaryBonus, null);
				}
			}
			return result;
		}

		// Token: 0x06001980 RID: 6528 RVA: 0x0007F68C File Offset: 0x0007D88C
		public override MBReadOnlyList<ValueTuple<ItemObject, float>> GetCommonLootItemScores()
		{
			return this.CommonLootItemSpawnChances;
		}

		// Token: 0x17000696 RID: 1686
		// (get) Token: 0x06001981 RID: 6529 RVA: 0x0007F694 File Offset: 0x0007D894
		public override int GoldRewardForEachLostHearth
		{
			get
			{
				return 4;
			}
		}

		// Token: 0x0400086B RID: 2155
		private MBReadOnlyList<ValueTuple<ItemObject, float>> _commonLootItems;
	}
}
