using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.BarterSystem.Barterables;
using TaleWorlds.CampaignSystem.MapNotificationTypes;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors;

public class RansomOfferCampaignBehavior : CampaignBehaviorBase
{
	private const float RansomOfferInitialChance = 0.2f;

	private const float RansomOfferChanceAfterRefusal = 0.12f;

	private const float RansomOfferChanceForPrisonersKeptByAI = 0.1f;

	private const float MapNotificationAutoDeclineDurationInDays = 2f;

	private const int AmountOfGoldLeftAfterRansom = 1000;

	private static TextObject RansomOfferDescriptionText = new TextObject("{=ZqJ92UN4}A courier with a ransom offer for the freedom of {CAPTIVE_HERO.NAME} has arrived.");

	private static TextObject RansomPanelDescriptionNpcHeldPrisonerText = new TextObject("{=4fXpOe4N}A courier arrives from the {CLAN_NAME}. They hold {CAPTIVE_HERO.NAME} and are demanding {GOLD_AMOUNT}{GOLD_ICON} in ransom.");

	private static TextObject RansomPanelDescriptionPlayerHeldPrisonerText = new TextObject("{=PutoRsWp}A courier arrives from the {CLAN_NAME}. They offer you {GOLD_AMOUNT}{GOLD_ICON} in ransom if you will free {CAPTIVE_HERO.NAME}.");

	private List<Hero> _heroesWithDeclinedRansomOffers = new List<Hero>();

	private Hero _currentRansomHero;

	private Hero _currentRansomPayer;

	private CampaignTime _currentRansomOfferDate;

	private static TextObject RansomPanelTitleText => new TextObject("{=ho5EndaV}Decision");

	private static TextObject RansomPanelAffirmativeText => new TextObject("{=Y94H6XnK}Accept");

	private static TextObject RansomPanelNegativeText => new TextObject("{=cOgmdp9e}Decline");

	public override void RegisterEvents()
	{
		CampaignEvents.DailyTickHeroEvent.AddNonSerializedListener(this, DailyTickHero);
		CampaignEvents.OnRansomOfferedToPlayerEvent.AddNonSerializedListener(this, OnRansomOffered);
		CampaignEvents.HeroKilledEvent.AddNonSerializedListener(this, OnHeroKilled);
		CampaignEvents.HeroPrisonerReleased.AddNonSerializedListener(this, OnHeroPrisonerReleased);
		CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, HourlyTick);
		CampaignEvents.PrisonersChangeInSettlement.AddNonSerializedListener(this, OnPrisonersChangeInSettlement);
		CampaignEvents.HeroPrisonerTaken.AddNonSerializedListener(this, OnHeroPrisonerTaken);
	}

	private void OnHeroPrisonerTaken(PartyBase party, Hero hero)
	{
		HandleDeclineRansomOffer(hero);
	}

	private void DailyTickHero(Hero hero)
	{
		if (hero.IsPrisoner && hero.Clan != null && hero.PartyBelongedToAsPrisoner != null && hero.PartyBelongedToAsPrisoner.MapFaction != null && !hero.PartyBelongedToAsPrisoner.MapFaction.IsBanditFaction && hero != Hero.MainHero && hero.Clan.AliveLords.Count > 1 && hero.MapFaction != null)
		{
			ConsiderRansomPrisoner(hero);
		}
	}

	private void ConsiderRansomPrisoner(Hero hero)
	{
		Clan captorClanOfPrisoner = GetCaptorClanOfPrisoner(hero);
		if (captorClanOfPrisoner == null)
		{
			return;
		}
		Hero hero2 = ((hero.Clan.Leader != hero) ? hero.Clan.Leader : hero.Clan.AliveLords.Where((Hero t) => t != hero.Clan.Leader).GetRandomElementInefficiently());
		if (hero2 == Hero.MainHero && hero2.IsPrisoner)
		{
			return;
		}
		if (captorClanOfPrisoner == Clan.PlayerClan || hero.Clan == Clan.PlayerClan)
		{
			if (_currentRansomHero != null || MobileParty.MainParty.IsInRaftState)
			{
				return;
			}
			float num = ((!_heroesWithDeclinedRansomOffers.Contains(hero)) ? 0.2f : 0.12f);
			if (MBRandom.RandomFloat < num)
			{
				float num2 = (float)new SetPrisonerFreeBarterable(hero, captorClanOfPrisoner.Leader, hero.PartyBelongedToAsPrisoner, hero2).GetUnitValueForFaction(hero.Clan) * 1.1f;
				if (num2 > 1E-05f && (float)(hero2.Gold + 1000) >= num2)
				{
					SetCurrentRansomHero(hero, hero2);
					StringHelpers.SetCharacterProperties("CAPTIVE_HERO", hero.CharacterObject, RansomOfferDescriptionText);
					Campaign.Current.CampaignInformationManager.NewMapNoticeAdded(new RansomOfferMapNotification(hero, RansomOfferDescriptionText));
				}
			}
		}
		else if (MBRandom.RandomFloat < 0.1f)
		{
			SetPrisonerFreeBarterable setPrisonerFreeBarterable = new SetPrisonerFreeBarterable(hero, captorClanOfPrisoner.Leader, hero.PartyBelongedToAsPrisoner, hero2);
			if (setPrisonerFreeBarterable.GetValueForFaction(captorClanOfPrisoner) + setPrisonerFreeBarterable.GetValueForFaction(hero.Clan) > 0)
			{
				Campaign.Current.BarterManager.ExecuteAiBarter(captorClanOfPrisoner, hero.Clan, captorClanOfPrisoner.Leader, hero2, setPrisonerFreeBarterable);
			}
		}
	}

	private Clan GetCaptorClanOfPrisoner(Hero hero)
	{
		Clan clan = null;
		if (hero.PartyBelongedToAsPrisoner.IsMobile)
		{
			if ((hero.PartyBelongedToAsPrisoner.MobileParty.IsMilitia || hero.PartyBelongedToAsPrisoner.MobileParty.IsGarrison || hero.PartyBelongedToAsPrisoner.MobileParty.IsCaravan || hero.PartyBelongedToAsPrisoner.MobileParty.IsVillager) && hero.PartyBelongedToAsPrisoner.Owner != null)
			{
				if (hero.PartyBelongedToAsPrisoner.Owner.IsNotable)
				{
					return hero.PartyBelongedToAsPrisoner.Owner.CurrentSettlement.OwnerClan;
				}
				return hero.PartyBelongedToAsPrisoner.Owner.Clan;
			}
			if (hero.PartyBelongedToAsPrisoner.MobileParty.IsPatrolParty)
			{
				return hero.PartyBelongedToAsPrisoner.MobileParty.HomeSettlement.OwnerClan;
			}
			return hero.PartyBelongedToAsPrisoner.MobileParty.ActualClan;
		}
		return hero.PartyBelongedToAsPrisoner.Settlement.OwnerClan;
	}

	public void SetCurrentRansomHero(Hero hero, Hero ransomPayer = null)
	{
		_currentRansomHero = hero;
		_currentRansomPayer = ransomPayer;
		_currentRansomOfferDate = ((hero != null) ? CampaignTime.Now : CampaignTime.Never);
	}

	private void OnRansomOffered(Hero captiveHero)
	{
		Clan captorClanOfPrisoner = GetCaptorClanOfPrisoner(captiveHero);
		Clan clan = ((captiveHero.Clan == Clan.PlayerClan) ? captorClanOfPrisoner : captiveHero.Clan);
		Hero ransomPayer = ((captiveHero.Clan.Leader != captiveHero) ? captiveHero.Clan.Leader : captiveHero.Clan.AliveLords.Where((Hero t) => t != captiveHero.Clan.Leader).GetRandomElementInefficiently());
		int ransomPrice = (int)((float)new SetPrisonerFreeBarterable(captiveHero, captorClanOfPrisoner.Leader, captiveHero.PartyBelongedToAsPrisoner, ransomPayer).GetUnitValueForFaction(captiveHero.Clan) * 1.1f);
		TextObject textObject = ((captorClanOfPrisoner == Clan.PlayerClan) ? RansomPanelDescriptionPlayerHeldPrisonerText : RansomPanelDescriptionNpcHeldPrisonerText);
		textObject.SetTextVariable("CLAN_NAME", clan.Name);
		textObject.SetTextVariable("GOLD_AMOUNT", ransomPrice);
		textObject.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"6\">");
		StringHelpers.SetCharacterProperties("CAPTIVE_HERO", captiveHero.CharacterObject, textObject);
		Campaign.Current.TimeControlMode = CampaignTimeControlMode.Stop;
		InformationManager.ShowInquiry(new InquiryData(RansomPanelTitleText.ToString(), textObject.ToString(), isAffirmativeOptionShown: true, isNegativeOptionShown: true, RansomPanelAffirmativeText.ToString(), RansomPanelNegativeText.ToString(), delegate
		{
			AcceptRansomOffer(ransomPrice);
		}, DeclineRansomOffer, "", 0f, null, () => IsAffirmativeOptionEnabled(ransomPayer, ransomPrice)), pauseGameActiveState: true);
	}

	private (bool, string) IsAffirmativeOptionEnabled(Hero ransomPayer, int ransomPrice)
	{
		if (ransomPayer == Hero.MainHero && ransomPayer.Gold < ransomPrice)
		{
			return (false, "{=d0kbtGYn}You don't have enough gold.");
		}
		return (true, string.Empty);
	}

	private void AcceptRansomOffer(int ransomPrice)
	{
		if (_heroesWithDeclinedRansomOffers.Contains(_currentRansomHero))
		{
			_heroesWithDeclinedRansomOffers.Remove(_currentRansomHero);
		}
		if (_currentRansomPayer.Gold < ransomPrice + 1000 && _currentRansomPayer != Hero.MainHero)
		{
			_currentRansomPayer.Gold = ransomPrice + 1000;
		}
		GiveGoldAction.ApplyBetweenCharacters(_currentRansomPayer, GetCaptorClanOfPrisoner(_currentRansomHero).Leader, ransomPrice);
		EndCaptivityAction.ApplyByRansom(_currentRansomHero, _currentRansomHero.Clan.Leader);
		Campaign.Current.CampaignBehaviorManager.GetBehavior<IStatisticsCampaignBehavior>()?.OnPlayerAcceptedRansomOffer(ransomPrice);
	}

	private void DeclineRansomOffer()
	{
		if (_currentRansomHero.IsPrisoner && _currentRansomHero.IsAlive && !_heroesWithDeclinedRansomOffers.Contains(_currentRansomHero))
		{
			_heroesWithDeclinedRansomOffers.Add(_currentRansomHero);
		}
		SetCurrentRansomHero(null);
	}

	private void OnHeroKilled(Hero victim, Hero killer, KillCharacterAction.KillCharacterActionDetail detail, bool showNotification = true)
	{
		HandleDeclineRansomOffer(victim);
	}

	private void HandleDeclineRansomOffer(Hero victim)
	{
		if (_currentRansomHero != null && (victim == _currentRansomHero || victim == Hero.MainHero))
		{
			CampaignEventDispatcher.Instance.OnRansomOfferCancelled(_currentRansomHero);
			DeclineRansomOffer();
		}
	}

	private void OnPrisonersChangeInSettlement(Settlement settlement, FlattenedTroopRoster roster, Hero prisoner, bool takenFromDungeon)
	{
		if (takenFromDungeon || _currentRansomHero == null)
		{
			return;
		}
		if (prisoner == _currentRansomHero)
		{
			CampaignEventDispatcher.Instance.OnRansomOfferCancelled(_currentRansomHero);
			DeclineRansomOffer();
		}
		else
		{
			if (roster == null)
			{
				return;
			}
			foreach (FlattenedTroopRosterElement item in roster)
			{
				if (item.Troop.IsHero && item.Troop.HeroObject == _currentRansomHero)
				{
					CampaignEventDispatcher.Instance.OnRansomOfferCancelled(_currentRansomHero);
					DeclineRansomOffer();
					break;
				}
			}
		}
	}

	private void OnHeroPrisonerReleased(Hero prisoner, PartyBase party, IFaction capturerFaction, EndCaptivityDetail detail, bool showNotification)
	{
		HandleDeclineRansomOffer(prisoner);
	}

	private void HourlyTick()
	{
		if (_currentRansomHero != null && _currentRansomOfferDate.ElapsedDaysUntilNow >= 2f)
		{
			CampaignEventDispatcher.Instance.OnRansomOfferCancelled(_currentRansomHero);
			DeclineRansomOffer();
		}
	}

	public override void SyncData(IDataStore dataStore)
	{
		dataStore.SyncData("_heroesWithDeclinedRansomOffers", ref _heroesWithDeclinedRansomOffers);
		dataStore.SyncData("_currentRansomHero", ref _currentRansomHero);
		dataStore.SyncData("_currentRansomPayer", ref _currentRansomPayer);
		dataStore.SyncData("_currentRansomOfferDate", ref _currentRansomOfferDate);
	}
}
