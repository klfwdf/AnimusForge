using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors;

public class PlayerTrackCompanionBehavior : CampaignBehaviorBase
{
	private Dictionary<Hero, CampaignTime> _scatteredCompanions = new Dictionary<Hero, CampaignTime>();

	public override void RegisterEvents()
	{
		CampaignEvents.CharacterBecameFugitiveEvent.AddNonSerializedListener(this, HeroBecameFugitive);
		CampaignEvents.CompanionRemoved.AddNonSerializedListener(this, CompanionRemoved);
		CampaignEvents.SettlementEntered.AddNonSerializedListener(this, SettlementEntered);
		CampaignEvents.NewCompanionAdded.AddNonSerializedListener(this, CompanionAdded);
		CampaignEvents.HeroPrisonerReleased.AddNonSerializedListener(this, OnHeroPrisonerReleased);
		CampaignEvents.OnGameLoadFinishedEvent.AddNonSerializedListener(this, OnGameLoadFinished);
		CampaignEvents.MobilePartyCreated.AddNonSerializedListener(this, OnMobilePartyCreated);
		CampaignEvents.OnHeroTeleportationRequestedEvent.AddNonSerializedListener(this, OnHeroTeleportationRequested);
	}

	private void OnHeroTeleportationRequested(Hero hero, Settlement settlement, MobileParty party, TeleportHeroAction.TeleportationDetail detail)
	{
		if (hero.IsPlayerCompanion && party == MobileParty.MainParty && detail == TeleportHeroAction.TeleportationDetail.DelayedTeleportToParty && _scatteredCompanions.ContainsKey(hero))
		{
			_scatteredCompanions.Remove(hero);
		}
	}

	private void OnGameLoadFinished()
	{
		if (!MBSaveLoad.IsUpdatingGameVersion || !MBSaveLoad.LastLoadedGameVersion.IsOlderThan(ApplicationVersion.FromString("v1.3.0")))
		{
			return;
		}
		foreach (Hero item in _scatteredCompanions.Keys.ToList())
		{
			if (item.PartyBelongedTo != null || item.GovernorOf != null || Campaign.Current.IssueManager.IssueSolvingCompanionList.Contains(item))
			{
				_scatteredCompanions.Remove(item);
			}
		}
	}

	public override void SyncData(IDataStore dataStore)
	{
		dataStore.SyncData("ScatteredCompanions", ref _scatteredCompanions);
	}

	private void AddHeroToScatteredCompanions(Hero hero)
	{
		if (hero.IsPlayerCompanion)
		{
			if (!_scatteredCompanions.ContainsKey(hero))
			{
				_scatteredCompanions.Add(hero, CampaignTime.Now);
			}
			else
			{
				_scatteredCompanions[hero] = CampaignTime.Now;
			}
		}
	}

	private void HeroBecameFugitive(Hero hero, bool showNotification)
	{
		AddHeroToScatteredCompanions(hero);
	}

	private void OnHeroPrisonerReleased(Hero releasedHero, PartyBase party, IFaction capturerFaction, EndCaptivityDetail detail, bool showNotification)
	{
		AddHeroToScatteredCompanions(releasedHero);
	}

	private void SettlementEntered(MobileParty party, Settlement settlement, Hero hero)
	{
		if (party != MobileParty.MainParty)
		{
			return;
		}
		foreach (Hero item in _scatteredCompanions.Keys.ToMBList())
		{
			if (item.CurrentSettlement == settlement)
			{
				TextObject textObject = new TextObject("{=ahpSGaow}You hear that your companion {COMPANION.LINK}, who was separated from you after a battle, is currently in this settlement.");
				StringHelpers.SetCharacterProperties("COMPANION", item.CharacterObject, textObject);
				InformationManager.ShowInquiry(new InquiryData(new TextObject("{=dx0hmeH6}Tracking").ToString(), textObject.ToString(), isAffirmativeOptionShown: true, isNegativeOptionShown: false, new TextObject("{=yS7PvrTD}OK").ToString(), "", null, null));
				_scatteredCompanions.Remove(item);
			}
		}
	}

	private void CompanionAdded(Hero companion)
	{
		if (_scatteredCompanions.ContainsKey(companion))
		{
			_scatteredCompanions.Remove(companion);
		}
	}

	private void CompanionRemoved(Hero companion, RemoveCompanionAction.RemoveCompanionDetail detail)
	{
		if (_scatteredCompanions.ContainsKey(companion))
		{
			_scatteredCompanions.Remove(companion);
		}
	}

	private void OnMobilePartyCreated(MobileParty mobileParty)
	{
		if (mobileParty.LeaderHero != null && mobileParty.LeaderHero.IsPlayerCompanion && _scatteredCompanions.ContainsKey(mobileParty.LeaderHero))
		{
			_scatteredCompanions.Remove(mobileParty.LeaderHero);
		}
	}
}
