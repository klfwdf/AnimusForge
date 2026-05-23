using System;
using System.Collections.Generic;
using Helpers;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors
{
	// Token: 0x02000438 RID: 1080
	public class RebellionsCampaignBehavior : CampaignBehaviorBase
	{
		// Token: 0x06004496 RID: 17558 RVA: 0x0014F4C6 File Offset: 0x0014D6C6
		public RebellionsCampaignBehavior()
		{
			this._rebelClansAndDaysPassedAfterCreation = new Dictionary<Clan, int>();
			this._cultureIconIdAndFrequencies = new Dictionary<CultureObject, Dictionary<int, int>>();
		}

		// Token: 0x06004497 RID: 17559 RVA: 0x0014F4EC File Offset: 0x0014D6EC
		public override void RegisterEvents()
		{
			CampaignEvents.DailyTickSettlementEvent.AddNonSerializedListener(this, new Action<Settlement>(this.DailyTickSettlement));
			CampaignEvents.DailyTickClanEvent.AddNonSerializedListener(this, new Action<Clan>(this.DailyTickClan));
			CampaignEvents.OnNewGameCreatedPartialFollowUpEndEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(this.OnNewGameCreatedPartialFollowUpEnd));
			CampaignEvents.OnGameLoadFinishedEvent.AddNonSerializedListener(this, new Action(this.OnGameLoaded));
			CampaignEvents.OnClanDestroyedEvent.AddNonSerializedListener(this, new Action<Clan>(this.OnClanDestroyed));
			CampaignEvents.OnSiegeEventStartedEvent.AddNonSerializedListener(this, new Action<SiegeEvent>(this.OnSiegeStarted));
		}

		// Token: 0x06004498 RID: 17560 RVA: 0x0014F583 File Offset: 0x0014D783
		private void OnSiegeStarted(SiegeEvent siegeEvent)
		{
			if (siegeEvent.BesiegedSettlement.IsTown)
			{
				this.CheckAndSetTownRebelliousState(siegeEvent.BesiegedSettlement);
			}
		}

		// Token: 0x06004499 RID: 17561 RVA: 0x0014F59E File Offset: 0x0014D79E
		public override void SyncData(IDataStore dataStore)
		{
			dataStore.SyncData<Dictionary<Clan, int>>("_rebelClansAndDaysPassedAfterCreation", ref this._rebelClansAndDaysPassedAfterCreation);
			dataStore.SyncData<Dictionary<CultureObject, Dictionary<int, int>>>("_iconIdAndFrequency", ref this._cultureIconIdAndFrequencies);
		}

		// Token: 0x0600449A RID: 17562 RVA: 0x0014F5C4 File Offset: 0x0014D7C4
		private void OnNewGameCreatedPartialFollowUpEnd(CampaignGameStarter starter)
		{
			this.InitializeIconIdAndFrequencies();
		}

		// Token: 0x0600449B RID: 17563 RVA: 0x0014F5CC File Offset: 0x0014D7CC
		private void OnGameLoaded()
		{
			this.InitializeIconIdAndFrequencies();
			if (MBSaveLoad.IsUpdatingGameVersion && MBSaveLoad.LastLoadedGameVersion < ApplicationVersion.FromString("e1.7.3.0", 0))
			{
				foreach (Settlement settlement in Settlement.All)
				{
					if (!settlement.IsTown && settlement.InRebelliousState)
					{
						settlement.Town.InRebelliousState = false;
						CampaignEventDispatcher.Instance.TownRebelliousStateChanged(settlement.Town, false);
					}
				}
			}
		}

		// Token: 0x0600449C RID: 17564 RVA: 0x0014F668 File Offset: 0x0014D868
		private void DailyTickSettlement(Settlement settlement)
		{
			if (this._rebellionEnabled && settlement.IsTown && settlement.Party.MapEvent == null && settlement.Party.SiegeEvent == null && !settlement.OwnerClan.IsRebelClan && Settlement.CurrentSettlement != settlement)
			{
				this.CheckAndSetTownRebelliousState(settlement);
				if (MBRandom.RandomFloat < 0.25f && RebellionsCampaignBehavior.CheckRebellionEvent(settlement))
				{
					this.StartRebellionEvent(settlement);
				}
			}
			if (settlement.IsTown && settlement.OwnerClan.IsRebelClan)
			{
				float num = MBMath.Map((float)(this._rebelClansAndDaysPassedAfterCreation[settlement.OwnerClan] - 1), 0f, 30f, (float)Campaign.Current.Models.SettlementLoyaltyModel.LoyaltyBoostAfterRebellionStartValue, 0f);
				settlement.Town.Loyalty += num;
			}
		}

		// Token: 0x0600449D RID: 17565 RVA: 0x0014F73C File Offset: 0x0014D93C
		private void CheckAndSetTownRebelliousState(Settlement settlement)
		{
			bool inRebelliousState = settlement.Town.InRebelliousState;
			settlement.Town.InRebelliousState = settlement.Town.Loyalty <= (float)Campaign.Current.Models.SettlementLoyaltyModel.RebelliousStateStartLoyaltyThreshold;
			if (inRebelliousState != settlement.Town.InRebelliousState)
			{
				CampaignEventDispatcher.Instance.TownRebelliousStateChanged(settlement.Town, settlement.Town.InRebelliousState);
			}
		}

		// Token: 0x0600449E RID: 17566 RVA: 0x0014F7AC File Offset: 0x0014D9AC
		private void OnClanDestroyed(Clan destroyedClan)
		{
			if (this._rebelClansAndDaysPassedAfterCreation.ContainsKey(destroyedClan))
			{
				this._rebelClansAndDaysPassedAfterCreation.Remove(destroyedClan);
			}
			if (destroyedClan.IsRebelClan)
			{
				for (int i = destroyedClan.Heroes.Count - 1; i >= 0; i--)
				{
					Hero hero = destroyedClan.Heroes[i];
					Campaign.Current.CampaignObjectManager.UnregisterDeadHero(hero);
				}
			}
		}

		// Token: 0x0600449F RID: 17567 RVA: 0x0014F814 File Offset: 0x0014DA14
		private void DailyTickClan(Clan clan)
		{
			if (this._rebelClansAndDaysPassedAfterCreation.ContainsKey(clan))
			{
				Dictionary<Clan, int> rebelClansAndDaysPassedAfterCreation = this._rebelClansAndDaysPassedAfterCreation;
				int num = rebelClansAndDaysPassedAfterCreation[clan];
				rebelClansAndDaysPassedAfterCreation[clan] = num + 1;
				if (this._rebelClansAndDaysPassedAfterCreation[clan] >= 30 && clan.Leader != null && clan.Settlements.Count > 0)
				{
					TextObject textObject = new TextObject("{=aKaGaOQx}{CLAN_LEADER.NAME}{.o} Clan", null);
					StringHelpers.SetCharacterProperties("CLAN_LEADER", clan.Leader.CharacterObject, textObject, false);
					clan.ChangeClanName(textObject, textObject);
					clan.IsRebelClan = false;
					this._rebelClansAndDaysPassedAfterCreation.Remove(clan);
					CampaignEventDispatcher.Instance.OnRebelliousClanDisbandedAtSettlement(clan.HomeSettlement, clan);
				}
			}
			if (clan.IsRebelClan && clan.Settlements.Count == 0 && clan.Heroes.Count > 0 && !clan.IsEliminated)
			{
				for (int i = clan.Heroes.Count - 1; i >= 0; i--)
				{
					Hero hero = clan.Heroes[i];
					if (hero.IsAlive)
					{
						if (hero.IsPrisoner && hero.PartyBelongedToAsPrisoner != null && hero.PartyBelongedToAsPrisoner != PartyBase.MainParty && hero.PartyBelongedToAsPrisoner.LeaderHero != null)
						{
							KillCharacterAction.ApplyByExecution(hero, hero.PartyBelongedToAsPrisoner.LeaderHero, true, true);
						}
						else if (hero.PartyBelongedTo == null)
						{
							KillCharacterAction.ApplyByRemove(hero, false, true);
						}
						else if (this._rebelClansAndDaysPassedAfterCreation[clan] > 90 && hero.PartyBelongedTo != null && hero.PartyBelongedTo.MapEvent == null)
						{
							KillCharacterAction.ApplyByRemove(hero, false, true);
						}
					}
				}
			}
		}

		// Token: 0x060044A0 RID: 17568 RVA: 0x0014F9BC File Offset: 0x0014DBBC
		private static bool CheckRebellionEvent(Settlement settlement)
		{
			if (settlement.Town.Loyalty <= (float)Campaign.Current.Models.SettlementLoyaltyModel.RebellionStartLoyaltyThreshold)
			{
				float militia = settlement.Militia;
				MobileParty garrisonParty = settlement.Town.GarrisonParty;
				float num = ((garrisonParty != null) ? garrisonParty.Party.CalculateCurrentStrength() : 0f);
				foreach (MobileParty mobileParty in settlement.Parties)
				{
					if (mobileParty.IsLordParty && DiplomacyHelper.IsSameFactionAndNotEliminated(mobileParty.MapFaction, settlement.MapFaction))
					{
						num += mobileParty.Party.CalculateCurrentStrength();
					}
				}
				return militia >= num * 1.4f;
			}
			return false;
		}

		// Token: 0x060044A1 RID: 17569 RVA: 0x0014FA90 File Offset: 0x0014DC90
		public void StartRebellionEvent(Settlement settlement)
		{
			Clan ownerClan = settlement.OwnerClan;
			this.CreateRebelPartyAndClan(settlement);
			this.ApplyRebellionConsequencesToSettlement(settlement);
			CampaignEventDispatcher.Instance.OnRebellionFinished(settlement, ownerClan);
			settlement.Town.FoodStocks = (float)settlement.Town.FoodStocksUpperLimit();
			settlement.Militia = 100f;
		}

		// Token: 0x060044A2 RID: 17570 RVA: 0x0014FAE0 File Offset: 0x0014DCE0
		private void ApplyRebellionConsequencesToSettlement(Settlement settlement)
		{
			Dictionary<TroopRosterElement, int> dictionary = new Dictionary<TroopRosterElement, int>();
			foreach (TroopRosterElement troopRosterElement in settlement.Town.GarrisonParty.MemberRoster.GetTroopRoster())
			{
				for (int i = 0; i < troopRosterElement.Number; i++)
				{
					if (MBRandom.RandomFloat < 0.5f)
					{
						if (dictionary.ContainsKey(troopRosterElement))
						{
							Dictionary<TroopRosterElement, int> dictionary2 = dictionary;
							TroopRosterElement key = troopRosterElement;
							int num = dictionary2[key];
							dictionary2[key] = num + 1;
						}
						else
						{
							dictionary.Add(troopRosterElement, 1);
						}
					}
				}
			}
			settlement.Town.GarrisonParty.MemberRoster.Clear();
			foreach (KeyValuePair<TroopRosterElement, int> keyValuePair in dictionary)
			{
				settlement.Town.GarrisonParty.AddPrisoner(keyValuePair.Key.Character, keyValuePair.Value);
			}
			settlement.Town.GarrisonParty.AddElementToMemberRoster(settlement.Culture.RangedMilitiaTroop, (int)(settlement.Militia * (MBRandom.RandomFloatRanged(-0.1f, 0.1f) + 0.6f)), false);
			settlement.Militia = 0f;
			if (settlement.MilitiaPartyComponent != null)
			{
				DestroyPartyAction.Apply(null, settlement.MilitiaPartyComponent.MobileParty);
			}
			settlement.Town.GarrisonParty.MemberRoster.AddToCounts(settlement.OwnerClan.Culture.BasicTroop, 50, false, 0, 0, true, -1);
			settlement.Town.GarrisonParty.MemberRoster.AddToCounts((settlement.OwnerClan.Culture.BasicTroop.UpgradeTargets.Length != 0) ? settlement.OwnerClan.Culture.BasicTroop.UpgradeTargets.GetRandomElement<CharacterObject>() : settlement.OwnerClan.Culture.BasicTroop, 25, false, 0, 0, true, -1);
			settlement.Town.Loyalty = 100f;
			settlement.Town.InRebelliousState = false;
		}

		// Token: 0x060044A3 RID: 17571 RVA: 0x0014FD04 File Offset: 0x0014DF04
		private void CreateRebelPartyAndClan(Settlement settlement)
		{
			MBReadOnlyList<CharacterObject> rebelliousHeroTemplates = settlement.Culture.RebelliousHeroTemplates;
			List<Hero> list = new List<Hero>
			{
				this.CreateRebelLeader(rebelliousHeroTemplates.GetRandomElement<CharacterObject>(), settlement),
				this.CreateRebelGovernor(rebelliousHeroTemplates.GetRandomElement<CharacterObject>(), settlement),
				this.CreateRebelSupporterHero(rebelliousHeroTemplates.GetRandomElement<CharacterObject>(), settlement),
				this.CreateRebelSupporterHero(rebelliousHeroTemplates.GetRandomElement<CharacterObject>(), settlement)
			};
			int clanIdForNewRebelClan = this.GetClanIdForNewRebelClan(settlement.Culture);
			Clan clan = Clan.CreateSettlementRebelClan(settlement, list[0], clanIdForNewRebelClan);
			clan.IsNoble = true;
			clan.AddRenown((float)MBRandom.RandomInt(200, 300), true);
			foreach (Hero hero in list)
			{
				hero.Clan = clan;
			}
			this._rebelClansAndDaysPassedAfterCreation.Add(clan, 1);
			foreach (Hero hero2 in list)
			{
				hero2.ChangeState(Hero.CharacterStates.Active);
			}
			MobileParty mobileParty = MobilePartyHelper.SpawnLordParty(list[0], settlement);
			MobilePartyHelper.SpawnLordParty(list[2], settlement);
			MobilePartyHelper.SpawnLordParty(list[3], settlement);
			IFaction mapFaction = settlement.MapFaction;
			DeclareWarAction.ApplyByRebellion(clan, mapFaction);
			foreach (Hero hero3 in list)
			{
				ChangeRelationAction.ApplyRelationChangeBetweenHeroes(mapFaction.Leader, hero3, MBRandom.RandomInt(-85, -75), true);
				foreach (Kingdom kingdom in Kingdom.All)
				{
					if (!kingdom.IsEliminated && kingdom.Culture != mapFaction.Culture)
					{
						int num = 0;
						foreach (Town town in kingdom.Fiefs)
						{
							num += (town.IsTown ? 2 : 1);
						}
						int num2 = (int)(MBRandom.RandomFloat * MBRandom.RandomFloat * 30f - (float)num);
						int value = ((kingdom.Culture == clan.Culture) ? (num2 + MBRandom.RandomInt(55, 65)) : num2);
						kingdom.Leader.SetPersonalRelation(hero3, value);
					}
				}
				foreach (Hero hero4 in list)
				{
					if (hero3 != hero4)
					{
						ChangeRelationAction.ApplyRelationChangeBetweenHeroes(hero3, hero4, MBRandom.RandomInt(5, 15), true);
					}
				}
			}
			ChangeOwnerOfSettlementAction.ApplyByRebellion(mobileParty.LeaderHero, settlement);
			ChangeGovernorAction.Apply(settlement.Town, list[1]);
			EnterSettlementAction.ApplyForParty(mobileParty, settlement);
			mobileParty.Ai.DisableForHours(5);
			list[0].ChangeHeroGold(50000);
			CampaignEventDispatcher.Instance.OnClanCreated(clan, false);
		}

		// Token: 0x060044A4 RID: 17572 RVA: 0x001500AC File Offset: 0x0014E2AC
		private Hero CreateRebelLeader(CharacterObject templateCharacter, Settlement settlement)
		{
			return this.CreateRebelHeroInternal(templateCharacter, settlement, new Dictionary<SkillObject, int>
			{
				{
					DefaultSkills.Steward,
					MBRandom.RandomInt(100, 175)
				},
				{
					DefaultSkills.Leadership,
					MBRandom.RandomInt(125, 175)
				},
				{
					DefaultSkills.OneHanded,
					MBRandom.RandomInt(125, 175)
				}
			});
		}

		// Token: 0x060044A5 RID: 17573 RVA: 0x0015010C File Offset: 0x0014E30C
		private Hero CreateRebelGovernor(CharacterObject templateCharacter, Settlement settlement)
		{
			return this.CreateRebelHeroInternal(templateCharacter, settlement, new Dictionary<SkillObject, int>
			{
				{
					DefaultSkills.Steward,
					MBRandom.RandomInt(125, 200)
				},
				{
					DefaultSkills.Leadership,
					MBRandom.RandomInt(100, 125)
				},
				{
					DefaultSkills.OneHanded,
					MBRandom.RandomInt(60, 90)
				}
			});
		}

		// Token: 0x060044A6 RID: 17574 RVA: 0x00150168 File Offset: 0x0014E368
		private Hero CreateRebelSupporterHero(CharacterObject templateCharacter, Settlement settlement)
		{
			return this.CreateRebelHeroInternal(templateCharacter, settlement, new Dictionary<SkillObject, int>
			{
				{
					DefaultSkills.Steward,
					MBRandom.RandomInt(100, 175)
				},
				{
					DefaultSkills.Leadership,
					MBRandom.RandomInt(100, 175)
				},
				{
					DefaultSkills.OneHanded,
					MBRandom.RandomInt(125, 175)
				}
			});
		}

		// Token: 0x060044A7 RID: 17575 RVA: 0x001501C8 File Offset: 0x0014E3C8
		private Hero CreateRebelHeroInternal(CharacterObject templateCharacter, Settlement settlement, Dictionary<SkillObject, int> startingSkills)
		{
			Hero hero = HeroCreator.CreateSpecialHero(templateCharacter, settlement, null, null, MBRandom.RandomInt(25, 40));
			foreach (KeyValuePair<SkillObject, int> keyValuePair in startingSkills)
			{
				hero.HeroDeveloper.SetInitialSkillLevel(keyValuePair.Key, keyValuePair.Value);
			}
			foreach (PerkObject perkObject in Campaign.Current.AllPerks)
			{
				if (hero.GetPerkValue(perkObject) && (float)hero.GetSkillValue(perkObject.Skill) < perkObject.RequiredSkillValue)
				{
					hero.SetPerkValueInternal(perkObject, false);
				}
			}
			return hero;
		}

		// Token: 0x060044A8 RID: 17576 RVA: 0x001502A8 File Offset: 0x0014E4A8
		private int GetClanIdForNewRebelClan(CultureObject culture)
		{
			int num = int.MaxValue;
			int num2 = int.MaxValue;
			Dictionary<int, int> dictionary;
			if (!this._cultureIconIdAndFrequencies.TryGetValue(culture, out dictionary))
			{
				dictionary = new Dictionary<int, int>();
				this._cultureIconIdAndFrequencies.Add(culture, dictionary);
			}
			if (culture.PossibleClanBannerIconsIDs != null)
			{
				MBList<int> mblist = culture.PossibleClanBannerIconsIDs.ToMBList<int>();
				mblist.Shuffle<int>();
				foreach (int num3 in mblist)
				{
					int num4;
					if (!dictionary.TryGetValue(num3, out num4))
					{
						num4 = 0;
						dictionary.Add(num3, num4);
					}
					if (num4 < num2)
					{
						num = num3;
						num2 = num4;
					}
				}
			}
			if (num == 2147483647)
			{
				foreach (KeyValuePair<CultureObject, Dictionary<int, int>> keyValuePair in this._cultureIconIdAndFrequencies)
				{
					foreach (KeyValuePair<int, int> keyValuePair2 in keyValuePair.Value)
					{
						if (keyValuePair2.Value < num2)
						{
							num = keyValuePair2.Key;
							num2 = keyValuePair2.Value;
						}
					}
				}
			}
			int num5 = num;
			int num6;
			if (this._cultureIconIdAndFrequencies[culture].TryGetValue(num5, out num6))
			{
				this._cultureIconIdAndFrequencies[culture][num5] = num6 + 1;
			}
			else
			{
				this._cultureIconIdAndFrequencies[culture].Add(num5, 1);
			}
			return num5;
		}

		// Token: 0x060044A9 RID: 17577 RVA: 0x00150440 File Offset: 0x0014E640
		private void InitializeIconIdAndFrequencies()
		{
			if (this._cultureIconIdAndFrequencies == null)
			{
				this._cultureIconIdAndFrequencies = new Dictionary<CultureObject, Dictionary<int, int>>();
			}
			foreach (Kingdom kingdom in Kingdom.All)
			{
				if (!this._cultureIconIdAndFrequencies.ContainsKey(kingdom.Culture))
				{
					this._cultureIconIdAndFrequencies.Add(kingdom.Culture, new Dictionary<int, int>());
				}
			}
			foreach (CultureObject key in MBObjectManager.Instance.GetObjectTypeList<CultureObject>())
			{
				if (!this._cultureIconIdAndFrequencies.ContainsKey(key))
				{
					this._cultureIconIdAndFrequencies.Add(key, new Dictionary<int, int>());
				}
			}
			foreach (CultureObject cultureObject in this._cultureIconIdAndFrequencies.Keys)
			{
				if (cultureObject.PossibleClanBannerIconsIDs != null)
				{
					foreach (int key2 in cultureObject.PossibleClanBannerIconsIDs)
					{
						if (!this._cultureIconIdAndFrequencies[cultureObject].ContainsKey(key2))
						{
							this._cultureIconIdAndFrequencies[cultureObject].Add(key2, 0);
						}
					}
				}
			}
		}

		// Token: 0x0400134B RID: 4939
		private const int UpdateClanAfterDays = 30;

		// Token: 0x0400134C RID: 4940
		private const int LoyaltyAfterRebellion = 100;

		// Token: 0x0400134D RID: 4941
		private const int InitialRelationPenalty = -80;

		// Token: 0x0400134E RID: 4942
		private const int InitialRelationBoostWithOtherFactions = 10;

		// Token: 0x0400134F RID: 4943
		private const int InitialRelationBoost = 60;

		// Token: 0x04001350 RID: 4944
		private const int InitialRelationBetweenRebelHeroes = 10;

		// Token: 0x04001351 RID: 4945
		private const int RebelClanStartingRenownMin = 200;

		// Token: 0x04001352 RID: 4946
		private const int RebelClanStartingRenownMax = 300;

		// Token: 0x04001353 RID: 4947
		private const int RebelHeroAgeMin = 25;

		// Token: 0x04001354 RID: 4948
		private const int RebelHeroAgeMax = 40;

		// Token: 0x04001355 RID: 4949
		private const float MilitiaGarrisonRatio = 1.4f;

		// Token: 0x04001356 RID: 4950
		private const float ThrowGarrisonTroopToPrisonPercentage = 0.5f;

		// Token: 0x04001357 RID: 4951
		private const float ThrowMilitiaTroopToGarrisonPercentage = 0.6f;

		// Token: 0x04001358 RID: 4952
		private const float DailyRebellionCheckChance = 0.25f;

		// Token: 0x04001359 RID: 4953
		private Dictionary<Clan, int> _rebelClansAndDaysPassedAfterCreation;

		// Token: 0x0400135A RID: 4954
		private Dictionary<CultureObject, Dictionary<int, int>> _cultureIconIdAndFrequencies;

		// Token: 0x0400135B RID: 4955
		private bool _rebellionEnabled = true;
	}
}
