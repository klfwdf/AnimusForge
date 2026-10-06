using System;
using System.Collections.Generic;
using Helpers;
using NavalDLC.CharacterDevelopment;
using NavalDLC.Map;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Map;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Library;

namespace NavalDLC.CampaignBehaviors;

public class NavalStormriderCampaignBehaviour : CampaignBehaviorBase
{
	private Dictionary<MobileParty, CampaignTime> _partiesEnteredStorm = new Dictionary<MobileParty, CampaignTime>();

	private CampaignTime _playerLastStormEnterTime = CampaignTime.Never;

	public override void RegisterEvents()
	{
		CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, OnHourlyTick);
		CampaignEvents.TickEvent.AddNonSerializedListener(this, TickEvent);
		CampaignEvents.MobilePartyDestroyed.AddNonSerializedListener(this, OnMobilePartyDestroyed);
	}

	private void TickEvent(float deltaTime)
	{
		Hero perkOwnerHero = null;
		if ((!(_playerLastStormEnterTime == CampaignTime.Never) && !(_playerLastStormEnterTime.ElapsedDaysUntilNow > 1f)) || !MobileParty.MainParty.HasPerk(NavalPerks.Shipmaster.Stormrider, out perkOwnerHero))
		{
			return;
		}
		foreach (Storm spawnedStorm in NavalDLCManager.Instance.StormManager.SpawnedStorms)
		{
			if (MobileParty.MainParty.Position.DistanceSquared(spawnedStorm.CurrentPosition) <= spawnedStorm.EffectRadius * spawnedStorm.EffectRadius)
			{
				_playerLastStormEnterTime = CampaignTime.Now;
				int amount = TaleWorlds.Library.MathF.Round(NavalPerks.Shipmaster.Stormrider.PrimaryBonus);
				AddXpToTroops(MobileParty.MainParty, amount);
			}
		}
	}

	private void OnHourlyTick()
	{
		foreach (Storm spawnedStorm in NavalDLCManager.Instance.StormManager.SpawnedStorms)
		{
			LocatableSearchData<MobileParty> data = MobileParty.StartFindingLocatablesAroundPosition(spawnedStorm.CurrentPosition, spawnedStorm.EffectRadius);
			MobileParty mobileParty = MobileParty.FindNextLocatable(ref data);
			while (mobileParty != null)
			{
				if (mobileParty == MobileParty.MainParty)
				{
					mobileParty = MobileParty.FindNextLocatable(ref data);
					continue;
				}
				if (mobileParty.IsCurrentlyAtSea && mobileParty.MapEvent == null && (!_partiesEnteredStorm.ContainsKey(mobileParty) || _partiesEnteredStorm[mobileParty].ElapsedDaysUntilNow > 1f))
				{
					OnPartyEnteredStorm(mobileParty);
				}
				mobileParty = MobileParty.FindNextLocatable(ref data);
			}
		}
	}

	private void OnPartyEnteredStorm(MobileParty party)
	{
		Hero perkOwnerHero = null;
		if (party.HasPerk(NavalPerks.Shipmaster.Stormrider, out perkOwnerHero))
		{
			_partiesEnteredStorm[party] = CampaignTime.Now;
			int amount = TaleWorlds.Library.MathF.Round(NavalPerks.Shipmaster.Stormrider.PrimaryBonus);
			AddXpToTroops(party, amount);
		}
	}

	private static void AddXpToTroops(MobileParty party, int amount)
	{
		TroopRoster memberRoster = party.MemberRoster;
		for (int i = 0; i < memberRoster.Count; i++)
		{
			TroopRosterElement elementCopyAtIndex = memberRoster.GetElementCopyAtIndex(i);
			if (!elementCopyAtIndex.Character.IsHero && MobilePartyHelper.CanTroopGainXp(party.Party, elementCopyAtIndex.Character, out var gainableMaxXp))
			{
				int xpAmount = Math.Min(gainableMaxXp, amount);
				memberRoster.AddXpToTroopAtIndex(i, xpAmount);
			}
		}
	}

	private void OnMobilePartyDestroyed(MobileParty mobileParty, PartyBase party)
	{
		_partiesEnteredStorm.Remove(mobileParty);
	}

	private void DoCleanUp()
	{
		List<MobileParty> list = new List<MobileParty>();
		foreach (KeyValuePair<MobileParty, CampaignTime> item in _partiesEnteredStorm)
		{
			if (item.Value.ElapsedDaysUntilNow > 1f)
			{
				list.Add(item.Key);
			}
		}
		foreach (MobileParty item2 in list)
		{
			_partiesEnteredStorm.Remove(item2);
		}
	}

	public override void SyncData(IDataStore dataStore)
	{
		DoCleanUp();
		dataStore.SyncData("_partiesEnteredStorm", ref _partiesEnteredStorm);
		dataStore.SyncData("_playerLastStormEnterTime", ref _playerLastStormEnterTime);
	}
}
