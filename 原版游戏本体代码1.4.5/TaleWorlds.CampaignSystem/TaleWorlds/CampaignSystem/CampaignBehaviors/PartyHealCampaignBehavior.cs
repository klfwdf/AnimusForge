using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors;

public class PartyHealCampaignBehavior : CampaignBehaviorBase
{
	private Dictionary<PartyBase, float> _overflowedHealingForRegulars = new Dictionary<PartyBase, float>();

	private Dictionary<PartyBase, float> _overflowedHealingForHeroes = new Dictionary<PartyBase, float>();

	private Dictionary<PartyBase, float> _overflowedHealingForPrisonerRegulars = new Dictionary<PartyBase, float>();

	private Dictionary<PartyBase, float> _overflowedHealingForPrisonerHeroes = new Dictionary<PartyBase, float>();

	public override void RegisterEvents()
	{
		CampaignEvents.HourlyTickClanEvent.AddNonSerializedListener(this, OnClanHourlyTick);
		CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, OnHourlyTick);
		CampaignEvents.MobilePartyDestroyed.AddNonSerializedListener(this, OnMobilePartyDestroyed);
		CampaignEvents.MapEventEnded.AddNonSerializedListener(this, OnMapEventEnded);
		CampaignEvents.OnQuarterDailyPartyTick.AddNonSerializedListener(this, OnQuarterDailyPartyTick);
		CampaignEvents.OnPlayerBattleEndEvent.AddNonSerializedListener(this, OnPlayerBattleEnd);
		CampaignEvents.DailyTickSettlementEvent.AddNonSerializedListener(this, OnDailyTickSettlement);
	}

	private void OnMobilePartyDestroyed(MobileParty mobileParty, PartyBase destroyerParty)
	{
		if (_overflowedHealingForRegulars.ContainsKey(mobileParty.Party))
		{
			_overflowedHealingForRegulars.Remove(mobileParty.Party);
			if (_overflowedHealingForHeroes.ContainsKey(mobileParty.Party))
			{
				_overflowedHealingForHeroes.Remove(mobileParty.Party);
			}
			if (_overflowedHealingForPrisonerRegulars.ContainsKey(mobileParty.Party))
			{
				_overflowedHealingForPrisonerRegulars.Remove(mobileParty.Party);
			}
			if (_overflowedHealingForPrisonerHeroes.ContainsKey(mobileParty.Party))
			{
				_overflowedHealingForPrisonerHeroes.Remove(mobileParty.Party);
			}
		}
	}

	public void OnMapEventEnded(MapEvent mapEvent)
	{
		if (!mapEvent.IsPlayerMapEvent)
		{
			OnBattleEndCheckPerkEffects(mapEvent);
		}
	}

	private void OnPlayerBattleEnd(MapEvent mapEvent)
	{
		OnBattleEndCheckPerkEffects(mapEvent);
	}

	private void OnBattleEndCheckPerkEffects(MapEvent mapEvent)
	{
		if (!mapEvent.HasWinner)
		{
			return;
		}
		foreach (PartyBase involvedParty in mapEvent.InvolvedParties)
		{
			if (involvedParty.MemberRoster.TotalHeroes <= 0)
			{
				continue;
			}
			foreach (TroopRosterElement item in involvedParty.MemberRoster.GetTroopRoster())
			{
				if (item.Character.IsHero)
				{
					Hero heroObject = item.Character.HeroObject;
					int roundedResultNumber = Campaign.Current.Models.PartyHealingModel.GetBattleEndHealingAmount(involvedParty, heroObject).RoundedResultNumber;
					if (roundedResultNumber > 0)
					{
						heroObject.Heal(roundedResultNumber);
					}
				}
			}
		}
	}

	public override void SyncData(IDataStore dataStore)
	{
		dataStore.SyncData("_overflowedHealingForRegulars", ref _overflowedHealingForRegulars);
		dataStore.SyncData("_overflowedHealingForHeroes", ref _overflowedHealingForHeroes);
		dataStore.SyncData("_overflowedHealingForPrisonerRegulars", ref _overflowedHealingForPrisonerRegulars);
		dataStore.SyncData("_overflowedHealingForPrisonerHeroes", ref _overflowedHealingForPrisonerHeroes);
	}

	private void OnHourlyTick()
	{
		TryHealOrWoundParty(MobileParty.MainParty.Party, CampaignTime.HoursInDay);
	}

	private void OnClanHourlyTick(Clan clan)
	{
		if (clan.IsBanditFaction)
		{
			return;
		}
		foreach (Hero hero in clan.Heroes)
		{
			float f = 0f;
			bool num = hero.PartyBelongedTo == null && hero.PartyBelongedToAsPrisoner == null;
			bool flag = hero.HeroState == Hero.CharacterStates.Dead || hero.HeroState == Hero.CharacterStates.NotSpawned || hero.HeroState == Hero.CharacterStates.Disabled;
			if (num && !flag)
			{
				f = Campaign.Current.Models.PartyHealingModel.GetDailyHealingHpForHeroes(null, isPrisoners: false).ResultNumber / (float)CampaignTime.HoursInDay;
			}
			int a = MBRandom.RoundRandomized(f);
			if (!hero.IsHealthFull())
			{
				int num2 = TaleWorlds.Library.MathF.Min(a, hero.MaxHitPoints - hero.HitPoints);
				hero.HitPoints += num2;
			}
		}
	}

	private void OnQuarterDailyPartyTick(MobileParty mobileParty)
	{
		if (!mobileParty.IsMainParty)
		{
			TryHealOrWoundParty(mobileParty.Party, 4f);
		}
	}

	private void OnDailyTickSettlement(Settlement settlement)
	{
		TryHealOrWoundParty(settlement.Party, 1f);
	}

	private void TryHealOrWoundParty(PartyBase partyBase, float healFrequencyPerDay)
	{
		if (partyBase.IsActive && partyBase.MapEvent == null)
		{
			TryToHealOrWoundMembers(partyBase, healFrequencyPerDay);
			TryToHealOrWoundPrisoners(partyBase, healFrequencyPerDay);
		}
	}

	private void TryToHealOrWoundPrisoners(PartyBase partyBase, float healFrequencyPerDay)
	{
		if (!_overflowedHealingForPrisonerHeroes.TryGetValue(partyBase, out var value))
		{
			_overflowedHealingForPrisonerHeroes.Add(partyBase, 0f);
		}
		if (!_overflowedHealingForPrisonerRegulars.TryGetValue(partyBase, out var value2))
		{
			_overflowedHealingForPrisonerRegulars.Add(partyBase, 0f);
		}
		float num = Campaign.Current.Models.PartyHealingModel.GetDailyHealingHpForHeroes(partyBase, isPrisoners: true).ResultNumber / healFrequencyPerDay;
		float num2 = Campaign.Current.Models.PartyHealingModel.GetDailyHealingForRegulars(partyBase, isPrisoner: true).ResultNumber / healFrequencyPerDay;
		value += num;
		value2 += num2;
		if ((int)value != 0)
		{
			ManageHealingOfPrisonerHeroes(partyBase, ref value);
		}
		if ((int)value2 != 0)
		{
			ManageHealingOfPrisonerRegulars(partyBase, ref value2);
		}
		_overflowedHealingForPrisonerHeroes[partyBase] = value;
		_overflowedHealingForPrisonerRegulars[partyBase] = value2;
	}

	private void TryToHealOrWoundMembers(PartyBase partyBase, float healFrequencyPerDay)
	{
		if (!_overflowedHealingForHeroes.TryGetValue(partyBase, out var value))
		{
			_overflowedHealingForHeroes.Add(partyBase, 0f);
		}
		if (!_overflowedHealingForRegulars.TryGetValue(partyBase, out var value2))
		{
			_overflowedHealingForRegulars.Add(partyBase, 0f);
		}
		float num = partyBase.HealingRateForMemberHeroes / healFrequencyPerDay;
		float num2 = partyBase.HealingRateForMemberRegulars / healFrequencyPerDay;
		value += num;
		value2 += num2;
		if (value >= 1f)
		{
			HealMemberHeroes(partyBase, ref value);
		}
		else if (value <= -1f)
		{
			ReduceHpMemberHeroes(partyBase, ref value);
		}
		if (value2 >= 1f)
		{
			HealMemberRegulars(partyBase, ref value2);
		}
		else if (value2 <= -1f)
		{
			ReduceHpMemberRegulars(partyBase, ref value2);
		}
		_overflowedHealingForHeroes[partyBase] = value;
		_overflowedHealingForRegulars[partyBase] = value2;
	}

	private void ManageHealingOfPrisonerRegulars(PartyBase partyBase, ref float prisonerRegularsHealingValue)
	{
		TroopRoster prisonRoster = partyBase.PrisonRoster;
		if (prisonRoster.TotalWoundedRegulars == 0)
		{
			prisonerRegularsHealingValue = 0f;
			return;
		}
		int num = TaleWorlds.Library.MathF.Floor(prisonerRegularsHealingValue);
		prisonerRegularsHealingValue -= num;
		int num2 = MBRandom.RandomInt(prisonRoster.Count);
		for (int i = 0; i < prisonRoster.Count; i++)
		{
			if (num <= 0)
			{
				break;
			}
			int index = (num2 + i) % prisonRoster.Count;
			if (prisonRoster.GetCharacterAtIndex(index).IsRegular && prisonRoster.GetElementWoundedNumber(index) > 0)
			{
				int num3 = TaleWorlds.Library.MathF.Min(num, prisonRoster.GetElementWoundedNumber(index));
				if (num3 > 0)
				{
					prisonRoster.AddToCountsAtIndex(index, 0, -num3);
					num -= num3;
				}
			}
		}
	}

	private void ManageHealingOfPrisonerHeroes(PartyBase partyBase, ref float prisonerHeroesHealingValue)
	{
		int num = TaleWorlds.Library.MathF.Floor(prisonerHeroesHealingValue);
		prisonerHeroesHealingValue -= num;
		TroopRoster prisonRoster = partyBase.PrisonRoster;
		if (prisonRoster.TotalHeroes <= 0)
		{
			return;
		}
		for (int i = 0; i < prisonRoster.Count; i++)
		{
			Hero heroObject = prisonRoster.GetCharacterAtIndex(i).HeroObject;
			if (heroObject != null && heroObject.HitPoints < heroObject.WoundedHealthLimit)
			{
				int healAmount = Math.Min(num, heroObject.WoundedHealthLimit - heroObject.HitPoints);
				heroObject.Heal(healAmount);
			}
		}
	}

	private static void HealMemberHeroes(PartyBase partyBase, ref float heroesHealingValue)
	{
		int num = TaleWorlds.Library.MathF.Floor(heroesHealingValue);
		heroesHealingValue -= num;
		TroopRoster memberRoster = partyBase.MemberRoster;
		if (memberRoster.TotalHeroes <= 0)
		{
			return;
		}
		for (int i = 0; i < memberRoster.Count; i++)
		{
			Hero heroObject = memberRoster.GetCharacterAtIndex(i).HeroObject;
			if (heroObject != null && !heroObject.IsHealthFull())
			{
				heroObject.Heal(num, addXp: true);
			}
		}
	}

	private static void ReduceHpMemberHeroes(PartyBase partyBase, ref float heroesHealingValue)
	{
		int a = TaleWorlds.Library.MathF.Ceiling(heroesHealingValue);
		heroesHealingValue = 0f - (0f - heroesHealingValue) % 1f;
		for (int i = 0; i < partyBase.MemberRoster.Count; i++)
		{
			Hero heroObject = partyBase.MemberRoster.GetCharacterAtIndex(i).HeroObject;
			if (heroObject != null && heroObject.HitPoints > 0)
			{
				int num = TaleWorlds.Library.MathF.Min(a, heroObject.HitPoints);
				heroObject.HitPoints += num;
			}
		}
	}

	private static void HealMemberRegulars(PartyBase partyBase, ref float regularsHealingValue)
	{
		TroopRoster memberRoster = partyBase.MemberRoster;
		if (memberRoster.TotalWoundedRegulars == 0)
		{
			regularsHealingValue = 0f;
			return;
		}
		int num = TaleWorlds.Library.MathF.Floor(regularsHealingValue);
		regularsHealingValue -= num;
		int num2 = 0;
		float num3 = 0f;
		int num4 = MBRandom.RandomInt(memberRoster.Count);
		for (int i = 0; i < memberRoster.Count; i++)
		{
			if (num <= 0)
			{
				break;
			}
			int index = (num4 + i) % memberRoster.Count;
			CharacterObject characterAtIndex = memberRoster.GetCharacterAtIndex(index);
			if (characterAtIndex.IsRegular)
			{
				int num5 = TaleWorlds.Library.MathF.Min(num, memberRoster.GetElementWoundedNumber(index));
				if (num5 > 0)
				{
					memberRoster.AddToCountsAtIndex(index, 0, -num5);
					num -= num5;
					num2 += num5;
					num3 += (float)(characterAtIndex.Tier * num5);
				}
			}
		}
		if (num2 > 0)
		{
			SkillLevelingManager.OnRegularTroopHealedWhileWaiting(partyBase.MobileParty, num2, num3 / (float)num2);
		}
	}

	private static void ReduceHpMemberRegulars(PartyBase partyBase, ref float regularsHealingValue)
	{
		TroopRoster memberRoster = partyBase.MemberRoster;
		if (memberRoster.TotalRegulars - memberRoster.TotalWoundedRegulars == 0)
		{
			regularsHealingValue = 0f;
			return;
		}
		int num = TaleWorlds.Library.MathF.Floor(0f - regularsHealingValue);
		regularsHealingValue = 0f - (0f - regularsHealingValue) % 1f;
		int num2 = MBRandom.RandomInt(memberRoster.Count);
		for (int i = 0; i < memberRoster.Count; i++)
		{
			if (num <= 0)
			{
				break;
			}
			int index = (num2 + i) % memberRoster.Count;
			if (memberRoster.GetCharacterAtIndex(index).IsRegular)
			{
				int num3 = TaleWorlds.Library.MathF.Min(memberRoster.GetElementNumber(index) - memberRoster.GetElementWoundedNumber(index), num);
				if (num3 > 0)
				{
					memberRoster.AddToCountsAtIndex(index, 0, num3);
					num -= num3;
				}
			}
		}
	}
}
