using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors
{
	// Token: 0x020003EF RID: 1007
	public class FoodConsumptionBehavior : CampaignBehaviorBase
	{
		// Token: 0x06003EBD RID: 16061 RVA: 0x0011950C File Offset: 0x0011770C
		public override void RegisterEvents()
		{
			CampaignEvents.OnNewGameCreatedPartialFollowUpEndEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(this.OnNewGameCreatedPartialFollowUpEnd));
			CampaignEvents.DailyTickPartyEvent.AddNonSerializedListener(this, new Action<MobileParty>(this.DailyTickParty));
			CampaignEvents.TickEvent.AddNonSerializedListener(this, new Action<float>(this.OnTick));
			CampaignEvents.PartyAttachedAnotherParty.AddNonSerializedListener(this, new Action<MobileParty>(this.OnPartyAttachedParty));
		}

		// Token: 0x06003EBE RID: 16062 RVA: 0x00119578 File Offset: 0x00117778
		private void OnNewGameCreatedPartialFollowUpEnd(CampaignGameStarter starter)
		{
			foreach (Settlement settlement in Settlement.All)
			{
				if (settlement.IsFortification)
				{
					settlement.Town.FoodStocks = (float)settlement.Town.FoodStocksUpperLimit();
				}
			}
		}

		// Token: 0x06003EBF RID: 16063 RVA: 0x001195E4 File Offset: 0x001177E4
		public override void SyncData(IDataStore dataStore)
		{
			dataStore.SyncData<int>("_lastItemVersion", ref this._lastItemVersion);
		}

		// Token: 0x06003EC0 RID: 16064 RVA: 0x001195F8 File Offset: 0x001177F8
		public void DailyTickParty(MobileParty party)
		{
			this.CheckAnimalBreeding(party);
			if (Campaign.Current.Models.MobilePartyFoodConsumptionModel.DoesPartyConsumeFood(party))
			{
				this.PartyConsumeFood(party, false);
			}
		}

		// Token: 0x06003EC1 RID: 16065 RVA: 0x00119620 File Offset: 0x00117820
		private void OnPartyAttachedParty(MobileParty mobileParty)
		{
			if (MobileParty.MainParty.Army != null && mobileParty.Army == MobileParty.MainParty.Army)
			{
				if (mobileParty.Party.IsStarving)
				{
					this.PartyConsumeFood(mobileParty, true);
					return;
				}
				if (MobileParty.MainParty.Army.LeaderParty.Party.IsStarving)
				{
					this.PartyConsumeFood(MobileParty.MainParty.Army.LeaderParty, true);
				}
				foreach (MobileParty mobileParty2 in MobileParty.MainParty.Army.LeaderParty.AttachedParties)
				{
					if (mobileParty2.Party.IsStarving && mobileParty != mobileParty2)
					{
						this.PartyConsumeFood(mobileParty2, true);
					}
				}
			}
		}

		// Token: 0x06003EC2 RID: 16066 RVA: 0x00119700 File Offset: 0x00117900
		public void OnTick(float dt)
		{
			if (PartyBase.MainParty.IsStarving)
			{
				int versionNo = PartyBase.MainParty.ItemRoster.VersionNo;
				if (this._lastItemVersion != versionNo)
				{
					this._lastItemVersion = versionNo;
					this.PartyConsumeFood(MobileParty.MainParty, true);
				}
			}
		}

		// Token: 0x06003EC3 RID: 16067 RVA: 0x00119748 File Offset: 0x00117948
		private void PartyConsumeFood(MobileParty mobileParty, bool starvingCheck = false)
		{
			bool isStarving = mobileParty.Party.IsStarving;
			float foodChange = mobileParty.FoodChange;
			float num = ((foodChange < 0f) ? (-foodChange) : 0f);
			int num2 = ((mobileParty.Party.RemainingFoodPercentage < 0) ? 0 : mobileParty.Party.RemainingFoodPercentage);
			int num3 = MathF.Round(num * 100f);
			num2 -= num3;
			this.MakeFoodConsumption(mobileParty, ref num2);
			if (num2 < 0 && mobileParty.ItemRoster.TotalFood > 0 && this.SlaughterLivestock(mobileParty, num2))
			{
				this.MakeFoodConsumption(mobileParty, ref num2);
				if (mobileParty.IsMainParty)
				{
					MBInformationManager.AddQuickInformation(new TextObject("{=WTwafRTH}Your party has slaughtered some animals to eat.", null), 0, null, null, "");
				}
			}
			if (num2 < 0 && mobileParty.Army != null && (mobileParty.AttachedTo == mobileParty.Army.LeaderParty || mobileParty.Army.LeaderParty == mobileParty))
			{
				Dictionary<Hero, float> dictionary = new Dictionary<Hero, float>();
				Hero leaderHero = mobileParty.LeaderHero;
				do
				{
					MobileParty mobileParty2 = null;
					float num4 = 1f;
					MobileParty leaderParty = mobileParty.Army.LeaderParty;
					if (leaderParty != mobileParty && !leaderParty.Party.IsStarving && leaderParty.ItemRoster.TotalFood > 0)
					{
						float num5 = (float)leaderParty.ItemRoster.TotalFood / MathF.Abs(leaderParty.FoodChange);
						if (num5 > num4)
						{
							num4 = num5;
							mobileParty2 = leaderParty;
						}
					}
					foreach (MobileParty mobileParty3 in leaderParty.AttachedParties)
					{
						if (mobileParty3 != mobileParty && !mobileParty3.Party.IsStarving && mobileParty3.ItemRoster.TotalFood > 0)
						{
							float num6 = (float)mobileParty3.ItemRoster.TotalFood / MathF.Abs(mobileParty3.FoodChange);
							if (num6 > num4)
							{
								num4 = num6;
								mobileParty2 = mobileParty3;
							}
						}
					}
					ItemRosterElement itemRosterElement = default(ItemRosterElement);
					if (mobileParty2 == null)
					{
						break;
					}
					int num7 = 10000;
					bool flag = false;
					foreach (ItemRosterElement itemRosterElement2 in mobileParty2.ItemRoster)
					{
						if (itemRosterElement2.EquipmentElement.Item.IsFood && itemRosterElement2.EquipmentElement.Item.Value < num7)
						{
							itemRosterElement = itemRosterElement2;
							num7 = itemRosterElement2.EquipmentElement.Item.Value;
							flag = true;
						}
					}
					if (!flag)
					{
						foreach (ItemRosterElement itemRosterElement3 in mobileParty2.ItemRoster)
						{
							if (itemRosterElement3.EquipmentElement.Item.HasHorseComponent && itemRosterElement3.EquipmentElement.Item.HorseComponent.IsLiveStock && itemRosterElement3.EquipmentElement.Item.Value < num7)
							{
								itemRosterElement = itemRosterElement3;
								num7 = itemRosterElement3.EquipmentElement.Item.Value;
								flag = true;
							}
						}
					}
					if (!flag)
					{
						break;
					}
					mobileParty2.ItemRoster.AddToCounts(itemRosterElement.EquipmentElement, -1);
					num2 += 100;
					if (itemRosterElement.EquipmentElement.Item.HasHorseComponent && itemRosterElement.EquipmentElement.Item.HorseComponent.IsLiveStock)
					{
						int meatCount = itemRosterElement.EquipmentElement.Item.HorseComponent.MeatCount;
						mobileParty2.ItemRoster.AddToCounts(DefaultItems.Meat, meatCount - 1);
					}
					Hero leaderHero2 = mobileParty2.LeaderHero;
					if (leaderHero != null && leaderHero2 != null)
					{
						float num8 = 0.2f;
						GainKingdomInfluenceAction.ApplyForGivingFood(leaderHero2, leaderHero, num8);
						float num9;
						if (dictionary.TryGetValue(leaderHero2, out num9))
						{
							dictionary[leaderHero2] = num9 + num8;
						}
						else
						{
							dictionary.Add(leaderHero2, num8);
						}
					}
				}
				while (num2 < 0);
				foreach (KeyValuePair<Hero, float> keyValuePair in dictionary)
				{
					CampaignEventDispatcher.Instance.OnHeroSharedFoodWithAnother(keyValuePair.Key, leaderHero, keyValuePair.Value);
				}
			}
			mobileParty.Party.RemainingFoodPercentage = num2;
			bool isStarving2 = mobileParty.Party.IsStarving;
			if ((int)Campaign.Current.Models.CampaignTimeModel.CampaignStartTime.ToDays != (int)CampaignTime.Now.ToDays)
			{
				if (isStarving && isStarving2)
				{
					int dailyStarvationMoralePenalty = Campaign.Current.Models.PartyMoraleModel.GetDailyStarvationMoralePenalty(mobileParty.Party);
					mobileParty.RecentEventsMorale += (float)dailyStarvationMoralePenalty;
					if (mobileParty.IsMainParty)
					{
						MBTextManager.SetTextVariable("MORALE_PENALTY", -dailyStarvationMoralePenalty);
						MBInformationManager.AddQuickInformation(new TextObject("{=qhL5o55i}Your party is starving. You lose {MORALE_PENALTY} morale.", null), 0, null, null, "");
						CampaignEventDispatcher.Instance.OnMainPartyStarving();
						if ((int)CampaignTime.Now.ToDays % 3 == 0 && mobileParty.MemberRoster.TotalManCount > 1)
						{
							TraitLevelingHelper.OnPartyStarved();
						}
					}
				}
				if (mobileParty.MemberRoster.TotalManCount > 1)
				{
					SkillLevelingManager.OnFoodConsumed(mobileParty, isStarving2);
					if (!isStarving && !isStarving2 && mobileParty.IsMainParty && mobileParty.Morale >= 90f && mobileParty.MemberRoster.TotalRegulars >= 20 && (int)CampaignTime.Now.ToDays % 10 == 0)
					{
						TraitLevelingHelper.OnPartyTreatedWell();
					}
				}
			}
			CampaignEventDispatcher.Instance.OnPartyConsumedFood(mobileParty);
		}

		// Token: 0x06003EC4 RID: 16068 RVA: 0x00119CE8 File Offset: 0x00117EE8
		private bool SlaughterLivestock(MobileParty party, int partyRemainingFoodPercentage)
		{
			int num = 0;
			ItemRoster itemRoster = party.ItemRoster;
			int num2 = itemRoster.Count - 1;
			while (num2 >= 0 && num * 100 < -partyRemainingFoodPercentage)
			{
				ItemObject itemAtIndex = itemRoster.GetItemAtIndex(num2);
				HorseComponent horseComponent = itemAtIndex.HorseComponent;
				if (horseComponent != null && horseComponent.IsLiveStock)
				{
					while (num * 100 < -partyRemainingFoodPercentage)
					{
						itemRoster.AddToCounts(itemAtIndex, -1);
						num += itemAtIndex.HorseComponent.MeatCount;
						if (itemRoster.FindIndexOfItem(itemAtIndex) == -1)
						{
							break;
						}
					}
				}
				num2--;
			}
			if (num > 0)
			{
				itemRoster.AddToCounts(DefaultItems.Meat, num);
				return true;
			}
			return false;
		}

		// Token: 0x06003EC5 RID: 16069 RVA: 0x00119D74 File Offset: 0x00117F74
		private void CheckAnimalBreeding(MobileParty party)
		{
			if (MBRandom.RandomFloat < DefaultPerks.Riding.Breeder.PrimaryBonus && !party.IsCurrentlyAtSea && party.HasPerk(DefaultPerks.Riding.Breeder, false) && (party.ItemRoster.NumberOfLivestockAnimals > 1 || party.ItemRoster.NumberOfPackAnimals > 1 || party.ItemRoster.NumberOfMounts > 1))
			{
				int num = party.ItemRoster.NumberOfLivestockAnimals + party.ItemRoster.NumberOfPackAnimals + party.ItemRoster.NumberOfMounts;
				ItemRosterElement randomElementWithPredicate = party.ItemRoster.GetRandomElementWithPredicate((ItemRosterElement x) => x.EquipmentElement.Item.HasHorseComponent);
				int num2 = MathF.Round(MathF.Max(1f, (float)num / 50f));
				party.ItemRoster.AddToCounts(randomElementWithPredicate.EquipmentElement.Item, num2);
				if (party.IsMainParty)
				{
					TextObject textObject = new TextObject("{=vl9bawa7}{COUNT} {?(COUNT > 1)}{PLURAL(ANIMAL_NAME)} are{?}{ANIMAL_NAME} is{\\?} added to your party.", null);
					textObject.SetTextVariable("COUNT", num2);
					textObject.SetTextVariable("ANIMAL_NAME", randomElementWithPredicate.EquipmentElement.Item.Name);
					InformationManager.DisplayMessage(new InformationMessage(textObject.ToString()));
				}
			}
		}

		// Token: 0x06003EC6 RID: 16070 RVA: 0x00119EB0 File Offset: 0x001180B0
		private void MakeFoodConsumption(MobileParty party, ref int partyRemainingFoodPercentage)
		{
			ItemRoster itemRoster = party.ItemRoster;
			int num = 0;
			for (int i = 0; i < itemRoster.Count; i++)
			{
				if (itemRoster.GetItemAtIndex(i).IsFood)
				{
					num++;
				}
			}
			bool flag = false;
			while (num > 0 && partyRemainingFoodPercentage < 0)
			{
				int num2 = MBRandom.RandomInt(num);
				bool flag2 = false;
				int num3 = 0;
				int num4 = itemRoster.Count - 1;
				while (num4 >= 0 && !flag2)
				{
					if (itemRoster.GetItemAtIndex(num4).IsFood)
					{
						int elementNumber = itemRoster.GetElementNumber(num4);
						if (elementNumber > 0)
						{
							num3++;
							if (num2 < num3)
							{
								itemRoster.AddToCounts(itemRoster.GetItemAtIndex(num4), -1);
								partyRemainingFoodPercentage += 100;
								if (elementNumber == 1)
								{
									num--;
								}
								flag2 = true;
								flag = true;
							}
						}
					}
					num4--;
				}
				if (flag)
				{
					party.Party.OnConsumedFood();
				}
			}
		}

		// Token: 0x040012B3 RID: 4787
		private int _lastItemVersion = -1;
	}
}
