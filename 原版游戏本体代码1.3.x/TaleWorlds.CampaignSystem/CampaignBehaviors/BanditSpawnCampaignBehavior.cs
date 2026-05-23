using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors
{
	// Token: 0x020003D0 RID: 976
	public class BanditSpawnCampaignBehavior : CampaignBehaviorBase
	{
		// Token: 0x17000DFE RID: 3582
		// (get) Token: 0x06003A09 RID: 14857 RVA: 0x000EF1EF File Offset: 0x000ED3EF
		private float BanditSpawnRadiusAsDays
		{
			get
			{
				return 0.5f * Campaign.Current.EstimatedAverageBanditPartySpeed * (float)CampaignTime.HoursInDay;
			}
		}

		// Token: 0x17000DFF RID: 3583
		// (get) Token: 0x06003A0A RID: 14858 RVA: 0x000EF208 File Offset: 0x000ED408
		private float _radiusAroundPlayerPartySquared
		{
			get
			{
				return MobileParty.MainParty.SeeingRange * MobileParty.MainParty.SeeingRange;
			}
		}

		// Token: 0x17000E00 RID: 3584
		// (get) Token: 0x06003A0B RID: 14859 RVA: 0x000EF21F File Offset: 0x000ED41F
		private float _numberOfMinimumBanditPartiesInAHideoutToInfestIt
		{
			get
			{
				return (float)Campaign.Current.Models.BanditDensityModel.NumberOfMinimumBanditPartiesInAHideoutToInfestIt;
			}
		}

		// Token: 0x17000E01 RID: 3585
		// (get) Token: 0x06003A0C RID: 14860 RVA: 0x000EF236 File Offset: 0x000ED436
		private int _numberOfMaxBanditPartiesAroundEachHideout
		{
			get
			{
				return Campaign.Current.Models.BanditDensityModel.NumberOfMaximumBanditPartiesAroundEachHideout;
			}
		}

		// Token: 0x17000E02 RID: 3586
		// (get) Token: 0x06003A0D RID: 14861 RVA: 0x000EF24C File Offset: 0x000ED44C
		private int _numberOfMaxHideoutsAtEachBanditFaction
		{
			get
			{
				return Campaign.Current.Models.BanditDensityModel.NumberOfMaximumHideoutsAtEachBanditFaction;
			}
		}

		// Token: 0x17000E03 RID: 3587
		// (get) Token: 0x06003A0E RID: 14862 RVA: 0x000EF262 File Offset: 0x000ED462
		private int _numberOfInitialHideoutsAtEachBanditFaction
		{
			get
			{
				return Campaign.Current.Models.BanditDensityModel.NumberOfInitialHideoutsAtEachBanditFaction;
			}
		}

		// Token: 0x17000E04 RID: 3588
		// (get) Token: 0x06003A0F RID: 14863 RVA: 0x000EF278 File Offset: 0x000ED478
		private int _numberOfMaximumBanditPartiesInEachHideout
		{
			get
			{
				return Campaign.Current.Models.BanditDensityModel.NumberOfMaximumBanditPartiesInEachHideout;
			}
		}

		// Token: 0x17000E05 RID: 3589
		// (get) Token: 0x06003A10 RID: 14864 RVA: 0x000EF28E File Offset: 0x000ED48E
		private int _numberOfMaxBanditCountPerClanHideout
		{
			get
			{
				return this._numberOfMaxBanditPartiesAroundEachHideout + this._numberOfMaximumBanditPartiesInEachHideout;
			}
		}

		// Token: 0x06003A11 RID: 14865 RVA: 0x000EF2A0 File Offset: 0x000ED4A0
		public override void RegisterEvents()
		{
			CampaignEvents.MobilePartyCreated.AddNonSerializedListener(this, new Action<MobileParty>(this.MobilePartyCreated));
			CampaignEvents.MobilePartyDestroyed.AddNonSerializedListener(this, new Action<MobileParty, PartyBase>(this.MobilePartyDestroyed));
			CampaignEvents.SettlementEntered.AddNonSerializedListener(this, new Action<MobileParty, Settlement, Hero>(this.OnSettlementEntered));
			CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, new Action(this.DailyTick));
			CampaignEvents.HourlyTickClanEvent.AddNonSerializedListener(this, new Action<Clan>(this.HourlyTickClan));
			CampaignEvents.OnGameLoadedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(this.OnGameLoaded));
			CampaignEvents.OnHomeHideoutChangedEvent.AddNonSerializedListener(this, new Action<BanditPartyComponent, Hideout>(this.OnHomeHideoutChanged));
			CampaignEvents.OnNewGameCreatedPartialFollowUpEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter, int>(this.OnNewGameCreatedPartialFollowUp));
		}

		// Token: 0x06003A12 RID: 14866 RVA: 0x000EF368 File Offset: 0x000ED568
		private void MobilePartyDestroyed(MobileParty party, PartyBase destroyerParty)
		{
			if (party.IsBandit && party.ActualClan != null && (this.IsBanditFaction(party.ActualClan) || BanditSpawnCampaignBehavior.IsLooterFaction(party.ActualClan)))
			{
				int num = 0;
				this._banditCountsPerHideout.TryGetValue(party.HomeSettlement, out num);
				this._banditCountsPerHideout[party.HomeSettlement] = num - 1;
			}
		}

		// Token: 0x06003A13 RID: 14867 RVA: 0x000EF3CC File Offset: 0x000ED5CC
		private void MobilePartyCreated(MobileParty party)
		{
			if (party.IsBandit && party.ActualClan != null && (this.IsBanditFaction(party.ActualClan) || BanditSpawnCampaignBehavior.IsLooterFaction(party.ActualClan)))
			{
				int num = 0;
				this._banditCountsPerHideout.TryGetValue(party.HomeSettlement, out num);
				this._banditCountsPerHideout[party.HomeSettlement] = num + 1;
			}
		}

		// Token: 0x06003A14 RID: 14868 RVA: 0x000EF42E File Offset: 0x000ED62E
		private void OnGameLoaded(CampaignGameStarter starter)
		{
			this.CacheHideouts();
			this.CacheBanditCounts();
		}

		// Token: 0x06003A15 RID: 14869 RVA: 0x000EF43C File Offset: 0x000ED63C
		public override void SyncData(IDataStore dataStore)
		{
		}

		// Token: 0x06003A16 RID: 14870 RVA: 0x000EF43E File Offset: 0x000ED63E
		private void OnNewGameCreatedPartialFollowUp(CampaignGameStarter starter, int i)
		{
			if (i == 10)
			{
				this.CacheHideouts();
				if (this._numberOfInitialHideoutsAtEachBanditFaction > 0)
				{
					this.InitializeInitialHideouts();
					return;
				}
			}
			else if (i == 11)
			{
				this.SpawnBanditsAroundHideoutAtNewGame();
				this.SpawnLootersAtNewGame();
				this.CacheBanditCounts();
			}
		}

		// Token: 0x06003A17 RID: 14871 RVA: 0x000EF474 File Offset: 0x000ED674
		private void CacheHideouts()
		{
			foreach (Hideout hideout in Hideout.All)
			{
				List<Hideout> list;
				if (!this._hideouts.TryGetValue(hideout.Settlement.Culture, out list))
				{
					this._hideouts[hideout.Settlement.Culture] = new List<Hideout>();
				}
				this._hideouts[hideout.Settlement.Culture].Add(hideout);
			}
		}

		// Token: 0x06003A18 RID: 14872 RVA: 0x000EF510 File Offset: 0x000ED710
		private void CacheBanditCounts()
		{
			this._banditCountsPerHideout = new Dictionary<Settlement, int>();
			foreach (MobileParty mobileParty in MobileParty.AllBanditParties)
			{
				if (this.IsBanditFaction(mobileParty.ActualClan) || BanditSpawnCampaignBehavior.IsLooterFaction(mobileParty.ActualClan))
				{
					int num = 0;
					this._banditCountsPerHideout.TryGetValue(mobileParty.HomeSettlement, out num);
					this._banditCountsPerHideout[mobileParty.HomeSettlement] = num + 1;
				}
			}
		}

		// Token: 0x06003A19 RID: 14873 RVA: 0x000EF5AC File Offset: 0x000ED7AC
		public void InitializeInitialHideouts()
		{
			foreach (Clan clan in Clan.BanditFactions)
			{
				if (this.IsBanditFaction(clan))
				{
					this.SpawnHideoutsAndBanditsPartiallyOnNewGame(clan);
				}
			}
		}

		// Token: 0x06003A1A RID: 14874 RVA: 0x000EF604 File Offset: 0x000ED804
		private void SpawnHideoutsAndBanditsPartiallyOnNewGame(Clan banditClan)
		{
			for (int i = 0; i < this._numberOfInitialHideoutsAtEachBanditFaction; i++)
			{
				this.FillANewHideoutWithBandits(banditClan);
			}
		}

		// Token: 0x06003A1B RID: 14875 RVA: 0x000EF62C File Offset: 0x000ED82C
		public void OnSettlementEntered(MobileParty mobileParty, Settlement settlement, Hero hero)
		{
			this.CheckForSpawningBanditBoss(settlement, mobileParty);
			if (Campaign.Current.GameStarted && mobileParty != null && mobileParty.IsBandit && settlement.IsHideout)
			{
				if (!settlement.Hideout.IsSpotted && settlement.Hideout.IsInfested)
				{
					float lengthSquared = (MobileParty.MainParty.Position.ToVec2() - settlement.Position.ToVec2()).LengthSquared;
					float seeingRange = MobileParty.MainParty.SeeingRange;
					float num = seeingRange * seeingRange / lengthSquared;
					float partySpottingDifficulty = Campaign.Current.Models.MapVisibilityModel.GetPartySpottingDifficulty(MobileParty.MainParty, mobileParty);
					if (num / partySpottingDifficulty >= 1f)
					{
						settlement.Hideout.IsSpotted = true;
						settlement.Party.UpdateVisibilityAndInspected(MobileParty.MainParty.Position, 0f);
						CampaignEventDispatcher.Instance.OnHideoutSpotted(MobileParty.MainParty.Party, settlement.Party);
					}
				}
				int num2 = 0;
				foreach (ItemRosterElement itemRosterElement in mobileParty.ItemRoster)
				{
					int num3 = (itemRosterElement.EquipmentElement.Item.IsFood ? MBRandom.RoundRandomized((float)mobileParty.MemberRoster.TotalManCount * ((3f + 6f * MBRandom.RandomFloat) / (float)itemRosterElement.EquipmentElement.Item.Value)) : 0);
					if (itemRosterElement.Amount > num3)
					{
						int num4 = itemRosterElement.Amount - num3;
						num2 += num4 * itemRosterElement.EquipmentElement.Item.Value;
					}
				}
				if (num2 > 0)
				{
					if (mobileParty.IsPartyTradeActive)
					{
						mobileParty.PartyTradeGold += (int)(0.25f * (float)num2);
					}
					settlement.SettlementComponent.ChangeGold((int)(0.25f * (float)num2));
				}
			}
		}

		// Token: 0x06003A1C RID: 14876 RVA: 0x000EF834 File Offset: 0x000EDA34
		private void CheckForSpawningBanditBoss(Settlement settlement, MobileParty mobileParty)
		{
			if (settlement.IsHideout && settlement.Hideout.IsSpotted)
			{
				if (settlement.Parties.Any((MobileParty x) => x.IsBandit || x.IsBanditBossParty))
				{
					CultureObject culture = settlement.Culture;
					MobileParty mobileParty2 = settlement.Parties.FirstOrDefault((MobileParty x) => x.IsBanditBossParty);
					if (mobileParty2 == null)
					{
						this.AddBossParty(settlement, culture);
						return;
					}
					if (!mobileParty2.MemberRoster.Contains(culture.BanditBoss))
					{
						mobileParty2.MemberRoster.AddToCounts(culture.BanditBoss, 1, false, 0, 0, true, -1);
					}
				}
			}
		}

		// Token: 0x06003A1D RID: 14877 RVA: 0x000EF8F4 File Offset: 0x000EDAF4
		private void AddBossParty(Settlement settlement, CultureObject culture)
		{
			PartyTemplateObject banditBossPartyTemplate = culture.BanditBossPartyTemplate;
			if (banditBossPartyTemplate != null)
			{
				this.AddBanditToHideout(settlement.Hideout, banditBossPartyTemplate, true).Ai.DisableAi();
			}
		}

		// Token: 0x06003A1E RID: 14878 RVA: 0x000EF924 File Offset: 0x000EDB24
		public void DailyTick()
		{
			if (this._numberOfMaxHideoutsAtEachBanditFaction > 0)
			{
				this.AddNewHideouts();
			}
			foreach (MobileParty mobileParty in MobileParty.AllBanditParties)
			{
				if (mobileParty.IsPartyTradeActive)
				{
					mobileParty.PartyTradeGold = (int)((double)mobileParty.PartyTradeGold * 0.95 + (double)(50f * (float)mobileParty.Party.MemberRoster.TotalManCount * 0.05f));
					if (MBRandom.RandomFloat < 0.03f && mobileParty.MapEvent != null)
					{
						foreach (ItemObject itemObject in Items.All)
						{
							if (itemObject.IsFood)
							{
								int num = (BanditSpawnCampaignBehavior.IsLooterFaction(mobileParty.MapFaction) ? 8 : 16);
								int num2 = MBRandom.RoundRandomized((float)mobileParty.MemberRoster.TotalManCount * (1f / (float)itemObject.Value) * (float)num * MBRandom.RandomFloat * MBRandom.RandomFloat * MBRandom.RandomFloat * MBRandom.RandomFloat);
								if (num2 > 0)
								{
									mobileParty.ItemRoster.AddToCounts(itemObject, num2);
								}
							}
						}
					}
				}
			}
		}

		// Token: 0x06003A1F RID: 14879 RVA: 0x000EFAA0 File Offset: 0x000EDCA0
		private void HourlyTickClan(Clan clan)
		{
			if (Campaign.Current.IsNight && clan.IsBanditFaction)
			{
				if (BanditSpawnCampaignBehavior.IsLooterFaction(clan))
				{
					this.SpawnLooters(clan, 0.07f, false);
					return;
				}
				if (this.IsBanditFaction(clan))
				{
					this.SpawnBanditsAroundHideout(clan, 0.1f);
				}
			}
		}

		// Token: 0x06003A20 RID: 14880 RVA: 0x000EFAEC File Offset: 0x000EDCEC
		private void SpawnBanditsAroundHideout(Clan clan, float ratio)
		{
			int count = clan.WarPartyComponents.Count;
			int num = MBRandom.RoundRandomized((float)(this.GetInfestedHideoutCount(clan) * this._numberOfMaxBanditCountPerClanHideout - count) * ratio);
			for (int i = 0; i < num; i++)
			{
				this.SpawnBanditParty(clan);
			}
		}

		// Token: 0x06003A21 RID: 14881 RVA: 0x000EFB34 File Offset: 0x000EDD34
		private void SpawnLooters(Clan clan, float ratio, bool uniformDistribution)
		{
			int count = clan.WarPartyComponents.Count;
			int num = MBRandom.RoundRandomized((float)(this.GetCurrentLimitForLooters(clan) - count) * ratio);
			for (int i = 0; i < num; i++)
			{
				this.SpawnLooterParty(clan, uniformDistribution);
			}
		}

		// Token: 0x06003A22 RID: 14882 RVA: 0x000EFB74 File Offset: 0x000EDD74
		private void AddNewHideouts()
		{
			List<ValueTuple<ValueTuple<Clan, int>, float>> list = new List<ValueTuple<ValueTuple<Clan, int>, float>>();
			foreach (Clan clan in Clan.BanditFactions)
			{
				if (this.IsBanditFaction(clan))
				{
					int infestedHideoutCount = this.GetInfestedHideoutCount(clan);
					if (infestedHideoutCount < this._numberOfMaxHideoutsAtEachBanditFaction)
					{
						list.Add(new ValueTuple<ValueTuple<Clan, int>, float>(new ValueTuple<Clan, int>(clan, infestedHideoutCount), 1f - (float)infestedHideoutCount / (float)this._numberOfMaxHideoutsAtEachBanditFaction));
					}
				}
			}
			int num;
			ValueTuple<Clan, int> valueTuple = MBRandom.ChooseWeighted<ValueTuple<Clan, int>>(list, out num);
			Clan item = valueTuple.Item1;
			int item2 = valueTuple.Item2;
			if (item != null)
			{
				float num2 = (((float)item2 < (float)this._numberOfMaxHideoutsAtEachBanditFaction * 0.5f) ? (0.2f + (float)(this._numberOfMaxHideoutsAtEachBanditFaction - item2) * 0.1f) : (0.1f + 0.5f * MathF.Pow(1f - 0.25f * ((float)item2 - (float)this._numberOfMaxHideoutsAtEachBanditFaction * 0.5f), 3f)));
				if (MBRandom.RandomFloat < num2)
				{
					this.FillANewHideoutWithBandits(item);
				}
			}
		}

		// Token: 0x06003A23 RID: 14883 RVA: 0x000EFC8C File Offset: 0x000EDE8C
		private void FillANewHideoutWithBandits(Clan faction)
		{
			Hideout hideout = this.SelectANonInfestedHideoutOfSameCultureByWeight(faction);
			if (hideout != null)
			{
				int num = 0;
				while ((float)num < this._numberOfMinimumBanditPartiesInAHideoutToInfestIt)
				{
					this.AddBanditToHideout(hideout, null, false);
					num++;
				}
			}
		}

		// Token: 0x06003A24 RID: 14884 RVA: 0x000EFCC0 File Offset: 0x000EDEC0
		public MobileParty AddBanditToHideout(Hideout hideoutComponent, PartyTemplateObject overridenPartyTemplate = null, bool isBanditBossParty = false)
		{
			if (hideoutComponent.Owner.Settlement.Culture.IsBandit)
			{
				Clan clan = null;
				foreach (Clan clan2 in Clan.BanditFactions)
				{
					if (hideoutComponent.Owner.Settlement.Culture == clan2.Culture && (this.IsBanditFaction(clan2) || BanditSpawnCampaignBehavior.IsLooterFaction(clan2)))
					{
						clan = clan2;
					}
				}
				PartyTemplateObject pt = overridenPartyTemplate ?? clan.DefaultPartyTemplate;
				MobileParty mobileParty = BanditPartyComponent.CreateBanditParty(clan.StringId + "_1", clan, hideoutComponent, isBanditBossParty, pt, hideoutComponent.Owner.Settlement.GatePosition);
				this.InitializeBanditParty(mobileParty, clan);
				mobileParty.SetMoveGoToSettlement(hideoutComponent.Owner.Settlement, mobileParty.NavigationCapability, false);
				mobileParty.RecalculateShortTermBehavior();
				EnterSettlementAction.ApplyForParty(mobileParty, hideoutComponent.Owner.Settlement);
				return mobileParty;
			}
			return null;
		}

		// Token: 0x06003A25 RID: 14885 RVA: 0x000EFDC4 File Offset: 0x000EDFC4
		private Hideout SelectBanditHideout(Clan faction)
		{
			MBList<ValueTuple<Hideout, float>> mblist = new MBList<ValueTuple<Hideout, float>>();
			foreach (Hideout hideout in Hideout.All)
			{
				if (hideout.Settlement.Culture == faction.Culture && hideout.IsInfested)
				{
					mblist.Add(new ValueTuple<Hideout, float>(hideout, this.GetSpawnChanceInSettlement(hideout.Settlement)));
				}
			}
			if (mblist.Count != 0)
			{
				return MBRandom.ChooseWeighted<Hideout>(mblist);
			}
			return this.SelectAHideoutByCheckingCultureAndInfestedState(faction);
		}

		// Token: 0x06003A26 RID: 14886 RVA: 0x000EFE60 File Offset: 0x000EE060
		private float GetSpawnChanceInSettlement(Settlement settlement)
		{
			if (this._banditCountsPerHideout.ContainsKey(settlement) && this._banditCountsPerHideout[settlement] != 0)
			{
				return 1f / MathF.Pow((float)this._banditCountsPerHideout[settlement], 2f);
			}
			return 1f;
		}

		// Token: 0x06003A27 RID: 14887 RVA: 0x000EFEAC File Offset: 0x000EE0AC
		private void OnHomeHideoutChanged(BanditPartyComponent banditPartyComponent, Hideout oldHomeHideout)
		{
			int num = 0;
			this._banditCountsPerHideout.TryGetValue(oldHomeHideout.Settlement, out num);
			this._banditCountsPerHideout[oldHomeHideout.Settlement] = num - 1;
			num = 0;
			this._banditCountsPerHideout.TryGetValue(banditPartyComponent.HomeSettlement, out num);
			this._banditCountsPerHideout[banditPartyComponent.HomeSettlement] = num + 1;
		}

		// Token: 0x06003A28 RID: 14888 RVA: 0x000EFF10 File Offset: 0x000EE110
		private Hideout SelectAHideoutByCheckingCultureAndInfestedState(Clan faction)
		{
			List<Hideout> list = new List<Hideout>();
			bool flag = false;
			bool flag2 = false;
			foreach (Hideout hideout in Hideout.All)
			{
				bool flag3 = hideout.Settlement.Culture == faction.Culture;
				bool isInfested = hideout.IsInfested;
				if (!flag2 && flag3)
				{
					flag2 = true;
					list.Clear();
				}
				if (flag2 && !flag && isInfested)
				{
					flag = true;
					list.Clear();
				}
				if ((!flag2 || flag3) && (!flag || isInfested))
				{
					list.Add(hideout);
				}
			}
			return list.GetRandomElement<Hideout>();
		}

		// Token: 0x06003A29 RID: 14889 RVA: 0x000EFFD0 File Offset: 0x000EE1D0
		private Hideout SelectANonInfestedHideoutOfSameCultureByWeight(Clan faction)
		{
			float averageDistanceBetweenClosestTwoTownsWithNavigationType = Campaign.Current.GetAverageDistanceBetweenClosestTwoTownsWithNavigationType(MobileParty.NavigationType.Default);
			float num = averageDistanceBetweenClosestTwoTownsWithNavigationType * 0.33f * averageDistanceBetweenClosestTwoTownsWithNavigationType * 0.33f;
			List<ValueTuple<Hideout, float>> list = new List<ValueTuple<Hideout, float>>();
			foreach (Hideout hideout in Hideout.All)
			{
				if (!hideout.IsInfested && hideout.Settlement.Culture == faction.Culture)
				{
					int num2 = 1;
					if (hideout.Settlement.LastThreatTime.ElapsedDaysUntilNow > 1.5f)
					{
						float num3 = Campaign.MapDiagonalSquared;
						float num4 = Campaign.MapDiagonalSquared;
						foreach (Hideout hideout2 in Hideout.All)
						{
							if (hideout != hideout2 && hideout2.IsInfested)
							{
								float num5 = hideout.Settlement.Position.DistanceSquared(hideout2.Settlement.Position);
								if (hideout.Settlement.Culture == hideout2.Settlement.Culture && num5 < num3)
								{
									num3 = num5;
								}
								if (num5 < num4)
								{
									num4 = num5;
								}
							}
							num2 = (int)MathF.Max(averageDistanceBetweenClosestTwoTownsWithNavigationType * 0.015f, num3 / num + averageDistanceBetweenClosestTwoTownsWithNavigationType * 0.076f * (num4 / num));
						}
					}
					list.Add(new ValueTuple<Hideout, float>(hideout, (float)num2));
				}
			}
			return MBRandom.ChooseWeighted<Hideout>(list);
		}

		// Token: 0x06003A2A RID: 14890 RVA: 0x000F0188 File Offset: 0x000EE388
		public void SpawnBanditsAroundHideoutAtNewGame()
		{
			foreach (Clan clan in Clan.BanditFactions)
			{
				if (this.IsBanditFaction(clan))
				{
					this.SpawnBanditsAroundHideout(clan, MBRandom.RandomFloatRanged(0.5f, 0.75f));
				}
			}
		}

		// Token: 0x06003A2B RID: 14891 RVA: 0x000F01EC File Offset: 0x000EE3EC
		public void SpawnLootersAtNewGame()
		{
			foreach (Clan clan in Clan.BanditFactions)
			{
				if (BanditSpawnCampaignBehavior.IsLooterFaction(clan))
				{
					this.SpawnLooters(clan, MBRandom.RandomFloatRanged(0.5f, 0.75f), true);
				}
			}
		}

		// Token: 0x06003A2C RID: 14892 RVA: 0x000F0250 File Offset: 0x000EE450
		private void SpawnLooterParty(Clan selectedFaction, bool uniformDistribution)
		{
			Settlement settlement = this.SelectARandomSettlementForLooterParty(uniformDistribution);
			CampaignVec2 spawnPositionAroundSettlement = this.GetSpawnPositionAroundSettlement(selectedFaction, settlement);
			MobileParty mobileParty = BanditPartyComponent.CreateLooterParty(selectedFaction.StringId + "_1", selectedFaction, settlement, false, selectedFaction.DefaultPartyTemplate, spawnPositionAroundSettlement);
			this.InitializeBanditParty(mobileParty, selectedFaction);
			mobileParty.SetMovePatrolAroundPoint(mobileParty.Position, MobileParty.NavigationType.Default);
		}

		// Token: 0x06003A2D RID: 14893 RVA: 0x000F02A4 File Offset: 0x000EE4A4
		private void SpawnBanditParty(Clan selectedFaction)
		{
			Hideout hideout = this.SelectBanditHideout(selectedFaction);
			CampaignVec2 spawnPositionAroundSettlement = this.GetSpawnPositionAroundSettlement(selectedFaction, hideout.Settlement);
			MobileParty mobileParty = BanditPartyComponent.CreateBanditParty(selectedFaction.StringId + "_1", selectedFaction, hideout, false, selectedFaction.DefaultPartyTemplate, spawnPositionAroundSettlement);
			this.InitializeBanditParty(mobileParty, selectedFaction);
			mobileParty.SetMovePatrolAroundPoint(mobileParty.Position, mobileParty.NavigationCapability);
		}

		// Token: 0x06003A2E RID: 14894 RVA: 0x000F0301 File Offset: 0x000EE501
		private static bool IsLooterFaction(IFaction faction)
		{
			return !faction.Culture.CanHaveSettlement && !faction.HasNavalNavigationCapability && faction.StringId != "deserters";
		}

		// Token: 0x06003A2F RID: 14895 RVA: 0x000F032A File Offset: 0x000EE52A
		private float GetSpawnRadiusForClan(Clan selectedFaction)
		{
			return this.BanditSpawnRadiusAsDays * (BanditSpawnCampaignBehavior.IsLooterFaction(selectedFaction) ? 1.5f : 1f);
		}

		// Token: 0x06003A30 RID: 14896 RVA: 0x000F0348 File Offset: 0x000EE548
		private int GetInfestedHideoutCount(Clan banditFaction)
		{
			int num = 0;
			foreach (Hideout hideout in this._hideouts[banditFaction.Culture])
			{
				if (hideout.IsInfested && hideout.MapFaction == banditFaction)
				{
					num++;
				}
			}
			return num;
		}

		// Token: 0x06003A31 RID: 14897 RVA: 0x000F03B8 File Offset: 0x000EE5B8
		private int GetCurrentLimitForLooters(Clan clan)
		{
			return Math.Min(Hideout.All.Count((Hideout x) => x.IsInfested) * 7, Campaign.Current.Models.BanditDensityModel.GetMaxSupportedNumberOfLootersForClan(clan));
		}

		// Token: 0x06003A32 RID: 14898 RVA: 0x000F040C File Offset: 0x000EE60C
		private Settlement SelectARandomSettlementForLooterParty(bool uniformDistribution)
		{
			MBList<ValueTuple<Settlement, float>> mblist = new MBList<ValueTuple<Settlement, float>>();
			foreach (Settlement settlement in Settlement.All)
			{
				if (settlement.IsTown || settlement.IsVillage)
				{
					mblist.Add(new ValueTuple<Settlement, float>(settlement, this.GetSpawnChanceInSettlement(settlement)));
				}
			}
			return MBRandom.ChooseWeighted<Settlement>(mblist);
		}

		// Token: 0x06003A33 RID: 14899 RVA: 0x000F0488 File Offset: 0x000EE688
		private void GiveFoodToBanditParty(MobileParty banditParty)
		{
			int num = (BanditSpawnCampaignBehavior.IsLooterFaction(banditParty.MapFaction) ? 8 : 16);
			foreach (ItemObject itemObject in Items.All)
			{
				if (itemObject.IsFood)
				{
					int num2 = MBRandom.RoundRandomized((float)banditParty.MemberRoster.TotalManCount * (1f / (float)itemObject.Value) * (float)num * MBRandom.RandomFloat * MBRandom.RandomFloat * MBRandom.RandomFloat * MBRandom.RandomFloat);
					if (num2 > 0)
					{
						banditParty.ItemRoster.AddToCounts(itemObject, num2);
					}
				}
			}
		}

		// Token: 0x06003A34 RID: 14900 RVA: 0x000F053C File Offset: 0x000EE73C
		private CampaignVec2 GetSpawnPositionAroundSettlement(Clan clan, Settlement settlement)
		{
			CampaignVec2 campaignVec = NavigationHelper.FindPointAroundPosition(settlement.GatePosition, MobileParty.NavigationType.Default, this.GetSpawnRadiusForClan(clan), 0f, true, false);
			if (campaignVec.DistanceSquared(MobileParty.MainParty.Position) < this._radiusAroundPlayerPartySquared)
			{
				for (int i = 0; i < 15; i++)
				{
					CampaignVec2 campaignVec2 = NavigationHelper.FindReachablePointAroundPosition(campaignVec, MobileParty.NavigationType.Default, this.GetSpawnRadiusForClan(clan), 0f, false);
					if (NavigationHelper.IsPositionValidForNavigationType(campaignVec2, MobileParty.NavigationType.Default))
					{
						float num2;
						float num = DistanceHelper.FindClosestDistanceFromMobilePartyToPoint(MobileParty.MainParty, campaignVec2, MobileParty.NavigationType.Default, out num2);
						if (num * num > this._radiusAroundPlayerPartySquared)
						{
							campaignVec = campaignVec2;
							break;
						}
					}
				}
			}
			return campaignVec;
		}

		// Token: 0x06003A35 RID: 14901 RVA: 0x000F05C5 File Offset: 0x000EE7C5
		private bool IsBanditFaction(Clan clan)
		{
			return !clan.HasNavalNavigationCapability && clan.IsBanditFaction && clan.Culture.CanHaveSettlement;
		}

		// Token: 0x06003A36 RID: 14902 RVA: 0x000F05E4 File Offset: 0x000EE7E4
		private void InitializeBanditParty(MobileParty banditParty, Clan faction)
		{
			banditParty.Party.SetVisualAsDirty();
			banditParty.ActualClan = faction;
			banditParty.Aggressiveness = 1f - 0.2f * MBRandom.RandomFloat;
			BanditSpawnCampaignBehavior.CreatePartyTrade(banditParty);
			this.GiveFoodToBanditParty(banditParty);
		}

		// Token: 0x06003A37 RID: 14903 RVA: 0x000F061C File Offset: 0x000EE81C
		private static void CreatePartyTrade(MobileParty banditParty)
		{
			int initialGold = (int)(10f * (float)banditParty.Party.MemberRoster.TotalManCount * (0.5f + 1f * MBRandom.RandomFloat));
			banditParty.InitializePartyTrade(initialGold);
		}

		// Token: 0x04001207 RID: 4615
		private const float BanditStartGoldPerBandit = 10f;

		// Token: 0x04001208 RID: 4616
		private const float BanditLongTermGoldPerBandit = 50f;

		// Token: 0x04001209 RID: 4617
		private const float HideoutInfestCooldownAfterFightInDays = 1.5f;

		// Token: 0x0400120A RID: 4618
		private Dictionary<CultureObject, List<Hideout>> _hideouts = new Dictionary<CultureObject, List<Hideout>>();

		// Token: 0x0400120B RID: 4619
		private Dictionary<Settlement, int> _banditCountsPerHideout = new Dictionary<Settlement, int>();
	}
}
