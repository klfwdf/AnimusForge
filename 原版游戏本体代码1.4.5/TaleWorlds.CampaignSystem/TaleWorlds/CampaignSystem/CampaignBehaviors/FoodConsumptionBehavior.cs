using System.Collections.Generic;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors;

public class FoodConsumptionBehavior : CampaignBehaviorBase
{
	private int _lastItemVersion = -1;

	public override void RegisterEvents()
	{
		CampaignEvents.OnNewGameCreatedPartialFollowUpEndEvent.AddNonSerializedListener(this, OnNewGameCreatedPartialFollowUpEnd);
		CampaignEvents.DailyTickPartyEvent.AddNonSerializedListener(this, DailyTickParty);
		CampaignEvents.TickEvent.AddNonSerializedListener(this, OnTick);
		CampaignEvents.PartyAttachedAnotherParty.AddNonSerializedListener(this, OnPartyAttachedParty);
	}

	private void OnNewGameCreatedPartialFollowUpEnd(CampaignGameStarter starter)
	{
		foreach (Settlement item in Settlement.All)
		{
			if (item.IsFortification)
			{
				item.Town.FoodStocks = item.Town.FoodStocksUpperLimit();
			}
		}
	}

	public override void SyncData(IDataStore dataStore)
	{
		dataStore.SyncData("_lastItemVersion", ref _lastItemVersion);
	}

	public void DailyTickParty(MobileParty party)
	{
		CheckAnimalBreeding(party);
		if (Campaign.Current.Models.MobilePartyFoodConsumptionModel.DoesPartyConsumeFood(party))
		{
			PartyConsumeFood(party);
		}
	}

	private void OnPartyAttachedParty(MobileParty mobileParty)
	{
		if (MobileParty.MainParty.Army == null || mobileParty.Army != MobileParty.MainParty.Army)
		{
			return;
		}
		if (mobileParty.Party.IsStarving)
		{
			PartyConsumeFood(mobileParty, starvingCheck: true);
			return;
		}
		if (MobileParty.MainParty.Army.LeaderParty.Party.IsStarving)
		{
			PartyConsumeFood(MobileParty.MainParty.Army.LeaderParty, starvingCheck: true);
		}
		foreach (MobileParty attachedParty in MobileParty.MainParty.Army.LeaderParty.AttachedParties)
		{
			if (attachedParty.Party.IsStarving && mobileParty != attachedParty)
			{
				PartyConsumeFood(attachedParty, starvingCheck: true);
			}
		}
	}

	public void OnTick(float dt)
	{
		if (PartyBase.MainParty.IsStarving)
		{
			int versionNo = PartyBase.MainParty.ItemRoster.VersionNo;
			if (_lastItemVersion != versionNo)
			{
				_lastItemVersion = versionNo;
				PartyConsumeFood(MobileParty.MainParty, starvingCheck: true);
			}
		}
	}

	private void PartyConsumeFood(MobileParty mobileParty, bool starvingCheck = false)
	{
		bool isStarving = mobileParty.Party.IsStarving;
		float foodChange = mobileParty.FoodChange;
		float num = ((foodChange < 0f) ? (0f - foodChange) : 0f);
		int num2 = ((mobileParty.Party.RemainingFoodPercentage >= 0) ? mobileParty.Party.RemainingFoodPercentage : 0);
		int num3 = MathF.Round(num * 100f);
		num2 -= num3;
		MakeFoodConsumption(mobileParty, ref num2);
		if (num2 < 0 && mobileParty.ItemRoster.TotalFood > 0 && SlaughterLivestock(mobileParty, num2))
		{
			MakeFoodConsumption(mobileParty, ref num2);
			if (mobileParty.IsMainParty)
			{
				MBInformationManager.AddQuickInformation(new TextObject("{=WTwafRTH}Your party has slaughtered some animals to eat."));
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
				foreach (MobileParty attachedParty in leaderParty.AttachedParties)
				{
					if (attachedParty != mobileParty && !attachedParty.Party.IsStarving && attachedParty.ItemRoster.TotalFood > 0)
					{
						float num6 = (float)attachedParty.ItemRoster.TotalFood / MathF.Abs(attachedParty.FoodChange);
						if (num6 > num4)
						{
							num4 = num6;
							mobileParty2 = attachedParty;
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
				foreach (ItemRosterElement item in mobileParty2.ItemRoster)
				{
					if (item.EquipmentElement.Item.IsFood && item.EquipmentElement.Item.Value < num7)
					{
						itemRosterElement = item;
						num7 = item.EquipmentElement.Item.Value;
						flag = true;
					}
				}
				if (!flag)
				{
					foreach (ItemRosterElement item2 in mobileParty2.ItemRoster)
					{
						if (item2.EquipmentElement.Item.HasHorseComponent && item2.EquipmentElement.Item.HorseComponent.IsLiveStock && item2.EquipmentElement.Item.Value < num7)
						{
							itemRosterElement = item2;
							num7 = item2.EquipmentElement.Item.Value;
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
					if (dictionary.TryGetValue(leaderHero2, out var value))
					{
						dictionary[leaderHero2] = value + num8;
					}
					else
					{
						dictionary.Add(leaderHero2, num8);
					}
				}
			}
			while (num2 < 0);
			foreach (KeyValuePair<Hero, float> item3 in dictionary)
			{
				CampaignEventDispatcher.Instance.OnHeroSharedFoodWithAnother(item3.Key, leaderHero, item3.Value);
			}
		}
		mobileParty.Party.RemainingFoodPercentage = num2;
		bool isStarving2 = mobileParty.Party.IsStarving;
		if ((int)Campaign.Current.Models.CampaignTimeModel.CampaignStartTime.ToDays != (int)CampaignTime.Now.ToDays)
		{
			if (isStarving && isStarving2)
			{
				int dailyStarvationMoralePenalty = Campaign.Current.Models.PartyMoraleModel.GetDailyStarvationMoralePenalty(mobileParty.Party);
				mobileParty.RecentEventsMorale += dailyStarvationMoralePenalty;
				if (mobileParty.IsMainParty)
				{
					MBTextManager.SetTextVariable("MORALE_PENALTY", -dailyStarvationMoralePenalty);
					MBInformationManager.AddQuickInformation(new TextObject("{=qhL5o55i}Your party is starving. You lose {MORALE_PENALTY} morale."));
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

	private void CheckAnimalBreeding(MobileParty party)
	{
		if (MBRandom.RandomFloat < DefaultPerks.Riding.Breeder.PrimaryBonus && !party.IsCurrentlyAtSea && party.HasPerk(DefaultPerks.Riding.Breeder) && (party.ItemRoster.NumberOfLivestockAnimals > 1 || party.ItemRoster.NumberOfPackAnimals > 1 || party.ItemRoster.NumberOfMounts > 1))
		{
			int num = party.ItemRoster.NumberOfLivestockAnimals + party.ItemRoster.NumberOfPackAnimals + party.ItemRoster.NumberOfMounts;
			ItemRosterElement randomElementWithPredicate = party.ItemRoster.GetRandomElementWithPredicate((ItemRosterElement x) => x.EquipmentElement.Item.HasHorseComponent);
			int num2 = MathF.Round(MathF.Max(1f, (float)num / 50f));
			party.ItemRoster.AddToCounts(randomElementWithPredicate.EquipmentElement.Item, num2);
			if (party.IsMainParty)
			{
				TextObject textObject = new TextObject("{=vl9bawa7}{COUNT} {?(COUNT > 1)}{PLURAL(ANIMAL_NAME)} are{?}{ANIMAL_NAME} is{\\?} added to your party.");
				textObject.SetTextVariable("COUNT", num2);
				textObject.SetTextVariable("ANIMAL_NAME", randomElementWithPredicate.EquipmentElement.Item.Name);
				InformationManager.DisplayMessage(new InformationMessage(textObject.ToString()));
			}
		}
	}

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
}
