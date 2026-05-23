using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors
{
	// Token: 0x02000426 RID: 1062
	public class PartyHealCampaignBehavior : CampaignBehaviorBase
	{
		// Token: 0x06004308 RID: 17160 RVA: 0x001441FC File Offset: 0x001423FC
		public override void RegisterEvents()
		{
			CampaignEvents.HourlyTickClanEvent.AddNonSerializedListener(this, new Action<Clan>(this.OnClanHourlyTick));
			CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, new Action(this.OnHourlyTick));
			CampaignEvents.MobilePartyDestroyed.AddNonSerializedListener(this, new Action<MobileParty, PartyBase>(this.OnMobilePartyDestroyed));
			CampaignEvents.MapEventEnded.AddNonSerializedListener(this, new Action<MapEvent>(this.OnMapEventEnded));
			CampaignEvents.OnQuarterDailyPartyTick.AddNonSerializedListener(this, new Action<MobileParty>(this.OnQuarterDailyPartyTick));
			CampaignEvents.OnPlayerBattleEndEvent.AddNonSerializedListener(this, new Action<MapEvent>(this.OnPlayerBattleEnd));
			CampaignEvents.DailyTickSettlementEvent.AddNonSerializedListener(this, new Action<Settlement>(this.OnDailyTickSettlement));
		}

		// Token: 0x06004309 RID: 17161 RVA: 0x001442AC File Offset: 0x001424AC
		private void OnMobilePartyDestroyed(MobileParty mobileParty, PartyBase destroyerParty)
		{
			if (this._overflowedHealingForRegulars.ContainsKey(mobileParty.Party))
			{
				this._overflowedHealingForRegulars.Remove(mobileParty.Party);
				if (this._overflowedHealingForHeroes.ContainsKey(mobileParty.Party))
				{
					this._overflowedHealingForHeroes.Remove(mobileParty.Party);
				}
				if (this._overflowedHealingForPrisonerRegulars.ContainsKey(mobileParty.Party))
				{
					this._overflowedHealingForPrisonerRegulars.Remove(mobileParty.Party);
				}
				if (this._overflowedHealingForPrisonerHeroes.ContainsKey(mobileParty.Party))
				{
					this._overflowedHealingForPrisonerHeroes.Remove(mobileParty.Party);
				}
			}
		}

		// Token: 0x0600430A RID: 17162 RVA: 0x00144350 File Offset: 0x00142550
		public void OnMapEventEnded(MapEvent mapEvent)
		{
			if (!mapEvent.IsPlayerMapEvent)
			{
				this.OnBattleEndCheckPerkEffects(mapEvent);
			}
		}

		// Token: 0x0600430B RID: 17163 RVA: 0x00144361 File Offset: 0x00142561
		private void OnPlayerBattleEnd(MapEvent mapEvent)
		{
			this.OnBattleEndCheckPerkEffects(mapEvent);
		}

		// Token: 0x0600430C RID: 17164 RVA: 0x0014436C File Offset: 0x0014256C
		private void OnBattleEndCheckPerkEffects(MapEvent mapEvent)
		{
			if (mapEvent.HasWinner)
			{
				foreach (PartyBase partyBase in mapEvent.InvolvedParties)
				{
					if (partyBase.MemberRoster.TotalHeroes > 0)
					{
						foreach (TroopRosterElement troopRosterElement in partyBase.MemberRoster.GetTroopRoster())
						{
							if (troopRosterElement.Character.IsHero)
							{
								Hero heroObject = troopRosterElement.Character.HeroObject;
								int roundedResultNumber = Campaign.Current.Models.PartyHealingModel.GetBattleEndHealingAmount(partyBase, heroObject).RoundedResultNumber;
								if (roundedResultNumber > 0)
								{
									heroObject.Heal(roundedResultNumber, false);
								}
							}
						}
					}
				}
			}
		}

		// Token: 0x0600430D RID: 17165 RVA: 0x0014445C File Offset: 0x0014265C
		public override void SyncData(IDataStore dataStore)
		{
			dataStore.SyncData<Dictionary<PartyBase, float>>("_overflowedHealingForRegulars", ref this._overflowedHealingForRegulars);
			dataStore.SyncData<Dictionary<PartyBase, float>>("_overflowedHealingForHeroes", ref this._overflowedHealingForHeroes);
			dataStore.SyncData<Dictionary<PartyBase, float>>("_overflowedHealingForPrisonerRegulars", ref this._overflowedHealingForPrisonerRegulars);
			dataStore.SyncData<Dictionary<PartyBase, float>>("_overflowedHealingForPrisonerHeroes", ref this._overflowedHealingForPrisonerHeroes);
		}

		// Token: 0x0600430E RID: 17166 RVA: 0x001444B1 File Offset: 0x001426B1
		private void OnHourlyTick()
		{
			this.TryHealOrWoundParty(MobileParty.MainParty.Party, (float)CampaignTime.HoursInDay);
		}

		// Token: 0x0600430F RID: 17167 RVA: 0x001444CC File Offset: 0x001426CC
		private void OnClanHourlyTick(Clan clan)
		{
			if (!clan.IsBanditFaction)
			{
				foreach (Hero hero in clan.Heroes)
				{
					float f = 0f;
					bool flag = hero.PartyBelongedTo == null && hero.PartyBelongedToAsPrisoner == null;
					bool flag2 = hero.HeroState == Hero.CharacterStates.Dead || hero.HeroState == Hero.CharacterStates.NotSpawned || hero.HeroState == Hero.CharacterStates.Disabled;
					if (flag && !flag2)
					{
						f = Campaign.Current.Models.PartyHealingModel.GetDailyHealingHpForHeroes(null, false, false).ResultNumber / (float)CampaignTime.HoursInDay;
					}
					int a = MBRandom.RoundRandomized(f);
					if (!hero.IsHealthFull())
					{
						int num = MathF.Min(a, hero.MaxHitPoints - hero.HitPoints);
						hero.HitPoints += num;
					}
				}
			}
		}

		// Token: 0x06004310 RID: 17168 RVA: 0x001445CC File Offset: 0x001427CC
		private void OnQuarterDailyPartyTick(MobileParty mobileParty)
		{
			if (!mobileParty.IsMainParty)
			{
				this.TryHealOrWoundParty(mobileParty.Party, 4f);
			}
		}

		// Token: 0x06004311 RID: 17169 RVA: 0x001445E7 File Offset: 0x001427E7
		private void OnDailyTickSettlement(Settlement settlement)
		{
			this.TryHealOrWoundParty(settlement.Party, 1f);
		}

		// Token: 0x06004312 RID: 17170 RVA: 0x001445FA File Offset: 0x001427FA
		private void TryHealOrWoundParty(PartyBase partyBase, float healFrequencyPerDay)
		{
			if (partyBase.IsActive && partyBase.MapEvent == null)
			{
				this.TryToHealOrWoundMembers(partyBase, healFrequencyPerDay);
				this.TryToHealOrWoundPrisoners(partyBase, healFrequencyPerDay);
			}
		}

		// Token: 0x06004313 RID: 17171 RVA: 0x0014461C File Offset: 0x0014281C
		private void TryToHealOrWoundPrisoners(PartyBase partyBase, float healFrequencyPerDay)
		{
			float num;
			if (!this._overflowedHealingForPrisonerHeroes.TryGetValue(partyBase, out num))
			{
				this._overflowedHealingForPrisonerHeroes.Add(partyBase, 0f);
			}
			float num2;
			if (!this._overflowedHealingForPrisonerRegulars.TryGetValue(partyBase, out num2))
			{
				this._overflowedHealingForPrisonerRegulars.Add(partyBase, 0f);
			}
			float num3 = Campaign.Current.Models.PartyHealingModel.GetDailyHealingHpForHeroes(partyBase, true, false).ResultNumber / healFrequencyPerDay;
			float num4 = Campaign.Current.Models.PartyHealingModel.GetDailyHealingForRegulars(partyBase, true, false).ResultNumber / healFrequencyPerDay;
			num += num3;
			num2 += num4;
			if ((int)num != 0)
			{
				this.ManageHealingOfPrisonerHeroes(partyBase, ref num);
			}
			if ((int)num2 != 0)
			{
				this.ManageHealingOfPrisonerRegulars(partyBase, ref num2);
			}
			this._overflowedHealingForPrisonerHeroes[partyBase] = num;
			this._overflowedHealingForPrisonerRegulars[partyBase] = num2;
		}

		// Token: 0x06004314 RID: 17172 RVA: 0x001446F0 File Offset: 0x001428F0
		private void TryToHealOrWoundMembers(PartyBase partyBase, float healFrequencyPerDay)
		{
			float num;
			if (!this._overflowedHealingForHeroes.TryGetValue(partyBase, out num))
			{
				this._overflowedHealingForHeroes.Add(partyBase, 0f);
			}
			float num2;
			if (!this._overflowedHealingForRegulars.TryGetValue(partyBase, out num2))
			{
				this._overflowedHealingForRegulars.Add(partyBase, 0f);
			}
			float num3 = partyBase.HealingRateForMemberHeroes / healFrequencyPerDay;
			float num4 = partyBase.HealingRateForMemberRegulars / healFrequencyPerDay;
			num += num3;
			num2 += num4;
			if (num >= 1f)
			{
				PartyHealCampaignBehavior.HealMemberHeroes(partyBase, ref num);
			}
			else if (num <= -1f)
			{
				PartyHealCampaignBehavior.ReduceHpMemberHeroes(partyBase, ref num);
			}
			if (num2 >= 1f)
			{
				PartyHealCampaignBehavior.HealMemberRegulars(partyBase, ref num2);
			}
			else if (num2 <= -1f)
			{
				PartyHealCampaignBehavior.ReduceHpMemberRegulars(partyBase, ref num2);
			}
			this._overflowedHealingForHeroes[partyBase] = num;
			this._overflowedHealingForRegulars[partyBase] = num2;
		}

		// Token: 0x06004315 RID: 17173 RVA: 0x001447B8 File Offset: 0x001429B8
		private void ManageHealingOfPrisonerRegulars(PartyBase partyBase, ref float prisonerRegularsHealingValue)
		{
			TroopRoster prisonRoster = partyBase.PrisonRoster;
			if (prisonRoster.TotalWoundedRegulars == 0)
			{
				prisonerRegularsHealingValue = 0f;
				return;
			}
			int num = MathF.Floor(prisonerRegularsHealingValue);
			prisonerRegularsHealingValue -= (float)num;
			int num2 = MBRandom.RandomInt(prisonRoster.Count);
			int num3 = 0;
			while (num3 < prisonRoster.Count && num > 0)
			{
				int index = (num2 + num3) % prisonRoster.Count;
				if (prisonRoster.GetCharacterAtIndex(index).IsRegular && prisonRoster.GetElementWoundedNumber(index) > 0)
				{
					int num4 = MathF.Min(num, prisonRoster.GetElementWoundedNumber(index));
					if (num4 > 0)
					{
						prisonRoster.AddToCountsAtIndex(index, 0, -num4, 0, true);
						num -= num4;
					}
				}
				num3++;
			}
		}

		// Token: 0x06004316 RID: 17174 RVA: 0x0014485C File Offset: 0x00142A5C
		private void ManageHealingOfPrisonerHeroes(PartyBase partyBase, ref float prisonerHeroesHealingValue)
		{
			int num = MathF.Floor(prisonerHeroesHealingValue);
			prisonerHeroesHealingValue -= (float)num;
			TroopRoster prisonRoster = partyBase.PrisonRoster;
			if (prisonRoster.TotalHeroes > 0)
			{
				for (int i = 0; i < prisonRoster.Count; i++)
				{
					Hero heroObject = prisonRoster.GetCharacterAtIndex(i).HeroObject;
					if (heroObject != null && heroObject.HitPoints < heroObject.WoundedHealthLimit)
					{
						int healAmount = Math.Min(num, heroObject.WoundedHealthLimit - heroObject.HitPoints);
						heroObject.Heal(healAmount, false);
					}
				}
			}
		}

		// Token: 0x06004317 RID: 17175 RVA: 0x001448D8 File Offset: 0x00142AD8
		private static void HealMemberHeroes(PartyBase partyBase, ref float heroesHealingValue)
		{
			int num = MathF.Floor(heroesHealingValue);
			heroesHealingValue -= (float)num;
			TroopRoster memberRoster = partyBase.MemberRoster;
			if (memberRoster.TotalHeroes > 0)
			{
				for (int i = 0; i < memberRoster.Count; i++)
				{
					Hero heroObject = memberRoster.GetCharacterAtIndex(i).HeroObject;
					if (heroObject != null && !heroObject.IsHealthFull())
					{
						heroObject.Heal(num, true);
					}
				}
			}
		}

		// Token: 0x06004318 RID: 17176 RVA: 0x00144938 File Offset: 0x00142B38
		private static void ReduceHpMemberHeroes(PartyBase partyBase, ref float heroesHealingValue)
		{
			int a = MathF.Ceiling(heroesHealingValue);
			heroesHealingValue = -(-heroesHealingValue % 1f);
			for (int i = 0; i < partyBase.MemberRoster.Count; i++)
			{
				Hero heroObject = partyBase.MemberRoster.GetCharacterAtIndex(i).HeroObject;
				if (heroObject != null && heroObject.HitPoints > 0)
				{
					int num = MathF.Min(a, heroObject.HitPoints);
					heroObject.HitPoints += num;
				}
			}
		}

		// Token: 0x06004319 RID: 17177 RVA: 0x001449A8 File Offset: 0x00142BA8
		private static void HealMemberRegulars(PartyBase partyBase, ref float regularsHealingValue)
		{
			TroopRoster memberRoster = partyBase.MemberRoster;
			if (memberRoster.TotalWoundedRegulars == 0)
			{
				regularsHealingValue = 0f;
				return;
			}
			int num = MathF.Floor(regularsHealingValue);
			regularsHealingValue -= (float)num;
			int num2 = 0;
			float num3 = 0f;
			int num4 = MBRandom.RandomInt(memberRoster.Count);
			int num5 = 0;
			while (num5 < memberRoster.Count && num > 0)
			{
				int index = (num4 + num5) % memberRoster.Count;
				CharacterObject characterAtIndex = memberRoster.GetCharacterAtIndex(index);
				if (characterAtIndex.IsRegular)
				{
					int num6 = MathF.Min(num, memberRoster.GetElementWoundedNumber(index));
					if (num6 > 0)
					{
						memberRoster.AddToCountsAtIndex(index, 0, -num6, 0, true);
						num -= num6;
						num2 += num6;
						num3 += (float)(characterAtIndex.Tier * num6);
					}
				}
				num5++;
			}
			if (num2 > 0)
			{
				SkillLevelingManager.OnRegularTroopHealedWhileWaiting(partyBase.MobileParty, num2, num3 / (float)num2);
			}
		}

		// Token: 0x0600431A RID: 17178 RVA: 0x00144A7C File Offset: 0x00142C7C
		private static void ReduceHpMemberRegulars(PartyBase partyBase, ref float regularsHealingValue)
		{
			TroopRoster memberRoster = partyBase.MemberRoster;
			if (memberRoster.TotalRegulars - memberRoster.TotalWoundedRegulars == 0)
			{
				regularsHealingValue = 0f;
				return;
			}
			int num = MathF.Floor(-regularsHealingValue);
			regularsHealingValue = -(-regularsHealingValue % 1f);
			int num2 = MBRandom.RandomInt(memberRoster.Count);
			int num3 = 0;
			while (num3 < memberRoster.Count && num > 0)
			{
				int index = (num2 + num3) % memberRoster.Count;
				if (memberRoster.GetCharacterAtIndex(index).IsRegular)
				{
					int num4 = MathF.Min(memberRoster.GetElementNumber(index) - memberRoster.GetElementWoundedNumber(index), num);
					if (num4 > 0)
					{
						memberRoster.AddToCountsAtIndex(index, 0, num4, 0, true);
						num -= num4;
					}
				}
				num3++;
			}
		}

		// Token: 0x04001314 RID: 4884
		private Dictionary<PartyBase, float> _overflowedHealingForRegulars = new Dictionary<PartyBase, float>();

		// Token: 0x04001315 RID: 4885
		private Dictionary<PartyBase, float> _overflowedHealingForHeroes = new Dictionary<PartyBase, float>();

		// Token: 0x04001316 RID: 4886
		private Dictionary<PartyBase, float> _overflowedHealingForPrisonerRegulars = new Dictionary<PartyBase, float>();

		// Token: 0x04001317 RID: 4887
		private Dictionary<PartyBase, float> _overflowedHealingForPrisonerHeroes = new Dictionary<PartyBase, float>();
	}
}
