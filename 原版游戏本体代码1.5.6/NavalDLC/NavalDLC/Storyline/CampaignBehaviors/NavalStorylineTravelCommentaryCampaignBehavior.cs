using System.Linq;
using NavalDLC.Map;
using NavalDLC.Storyline.Quests;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace NavalDLC.Storyline.CampaignBehaviors;

public class NavalStorylineTravelCommentaryCampaignBehavior : CampaignBehaviorBase
{
	private CampaignVec2 RansLaundryLocation = new CampaignVec2(new Vec2(321.2f, 697.4f), isOnLand: false);

	private CampaignVec2 FinisterreLocation = new CampaignVec2(new Vec2(202.24f, 621.91f), isOnLand: false);

	private CampaignVec2 RookBirdLocation = new CampaignVec2(new Vec2(205f, 230f), isOnLand: false);

	private CampaignVec2 TrandEstuaryLocation = new CampaignVec2(new Vec2(196.84f, 528.48f), isOnLand: false);

	private CampaignVec2 GulfOfCharasLocation = new CampaignVec2(new Vec2(217f, 372f), isOnLand: false);

	private CampaignVec2 GarantorCastleLocation = new CampaignVec2(new Vec2(359.33f, 304.45f), isOnLand: false);

	private CampaignVec2 MarzoporLocation = new CampaignVec2(new Vec2(543.53f, 641.23f), isOnLand: false);

	private CampaignVec2 GalendLocation = new CampaignVec2(new Vec2(195f, 437f), isOnLand: false);

	private CampaignVec2 AngrafjordLocation = new CampaignVec2(new Vec2(260f, 770f), isOnLand: false);

	private CampaignVec2 ByalicLocation = new CampaignVec2(new Vec2(555f, 724.72f), isOnLand: false);

	private NavalStorylineCampaignBehavior _storylineBehavior;

	private NavalStorylineQuestBase _activeQuest;

	private CampaignTime _finisterCooldown = CampaignTime.Zero;

	private CampaignTime _byalicCooldown = CampaignTime.Zero;

	private CampaignTime _rookCooldown = CampaignTime.Zero;

	private CampaignTime _ransLaundryCooldown = CampaignTime.Zero;

	private CampaignTime _trandEstuaryCooldown = CampaignTime.Zero;

	private CampaignTime _charasCooldown = CampaignTime.Zero;

	private CampaignTime _garantorCastleCooldown = CampaignTime.Zero;

	private CampaignTime _mazoporCooldown = CampaignTime.Zero;

	private CampaignTime _galendCooldown = CampaignTime.Zero;

	private bool DidShowAct2Commentary;

	private bool DidShowAct3Q1Commentary1;

	private bool DidShowAct3Q1Commentary2;

	private bool DidShowAct3Q2Commentary;

	private bool DidShowAct3Q3Commentary;

	private bool DidShowAct3Q4Commentary;

	private bool DidShowAct3Q5Commentary;

	private CampaignTime _nextGoToPortEventTime = CampaignTime.Zero;

	private bool CanShowLowFoodCommentary;

	private bool CanShowLowShipHpCommentary;

	private bool CanShowLowTroopsAndShipsCommentary;

	private bool CanShowStormCommentary;

	private bool CanShowNearOsticanCommentary;

	private float ImportantLocationLargeRadius => 15f;

	private float CommentarySettlementArrivalRadius => 20f;

	private float SkatriaRadius => 25f;

	private float ByalicRadius => 50f;

	private float GarantorRadius => 10f;

	private NavalStorylineCampaignBehavior StorylineBehavior
	{
		get
		{
			if (_storylineBehavior == null)
			{
				_storylineBehavior = Campaign.Current.GetCampaignBehavior<NavalStorylineCampaignBehavior>();
			}
			return _storylineBehavior;
		}
	}

	private bool IsStorylineActive => StorylineBehavior?.IsNavalStorylineActive() ?? false;

	private NavalStorylineData.NavalStorylineStage CurrentStage => StorylineBehavior?.GetNavalStorylineStage() ?? NavalStorylineData.NavalStorylineStage.None;

	public override void RegisterEvents()
	{
		CampaignEvents.QuarterHourlyTickEvent.AddNonSerializedListener(this, QuarterlyHourlyTick);
		CampaignEvents.OnQuestStartedEvent.AddNonSerializedListener(this, OnQuestStarted);
		CampaignEvents.OnQuestCompletedEvent.AddNonSerializedListener(this, OnQuestCompleted);
		CampaignEvents.OnGameLoadFinishedEvent.AddNonSerializedListener(this, OnGameLoadFinished);
	}

	private void OnQuestStarted(QuestBase quest)
	{
		if (quest is NavalStorylineQuestBase activeQuest)
		{
			CanShowLowFoodCommentary = true;
			CanShowLowTroopsAndShipsCommentary = true;
			CanShowLowShipHpCommentary = true;
			CanShowStormCommentary = true;
			_activeQuest = activeQuest;
		}
		else
		{
			if (!(quest is ReturnToBaseQuest))
			{
				return;
			}
			CanShowStormCommentary = true;
			if (CurrentStage >= NavalStorylineData.NavalStorylineStage.Act3Quest4)
			{
				return;
			}
			if (CurrentStage == NavalStorylineData.NavalStorylineStage.None)
			{
				AddNotification(new TextObject("{=2epUWf2j}Let's row into harbor and tie her up."), MBInformationManager.NotificationPriority.High);
			}
			else if (CurrentStage == NavalStorylineData.NavalStorylineStage.Act1)
			{
				AddNotification(new TextObject("{=cXk7SjQD}Right. Let's get her docked."), MBInformationManager.NotificationPriority.High);
			}
			else if (CurrentStage == NavalStorylineData.NavalStorylineStage.Act2)
			{
				AddNotification(new TextObject("{=shqI8YFE}Time to go ashore."), MBInformationManager.NotificationPriority.High);
			}
			else if (CurrentStage == NavalStorylineData.NavalStorylineStage.Act3Quest1 || CurrentStage == NavalStorylineData.NavalStorylineStage.Act3SpeakToSailors || CurrentStage == NavalStorylineData.NavalStorylineStage.Act3Quest2)
			{
				CanShowNearOsticanCommentary = true;
				if (MBRandom.RandomFloat < 0.5f)
				{
					AddNotification(new TextObject("{=MbyjzdVW}Let's hope for a fair wind back to Ostican."), MBInformationManager.NotificationPriority.High);
				}
				else
				{
					AddNotification(new TextObject("{=AS1hHmHa}And now back to Ostican…"), MBInformationManager.NotificationPriority.High);
				}
			}
		}
	}

	private void OnQuestCompleted(QuestBase quest, QuestBase.QuestCompleteDetails details)
	{
		if (quest is NavalStorylineQuestBase)
		{
			CanShowLowFoodCommentary = false;
			CanShowLowTroopsAndShipsCommentary = false;
			CanShowLowShipHpCommentary = false;
			CanShowStormCommentary = false;
			if (_activeQuest != null && !_activeQuest.IsOngoing)
			{
				_activeQuest = null;
			}
			if (quest is DefeatThePiratesQuest)
			{
				DidShowAct2Commentary = false;
			}
			else if (quest is SetSailAndMeetTheFortuneSeekersInTargetSettlementQuest)
			{
				DidShowAct3Q1Commentary1 = false;
			}
			else if (quest is SetSailAndEscortTheFortuneSeekersQuest)
			{
				DidShowAct3Q1Commentary2 = false;
			}
			else if (quest is HuntDownTheEmiraAlFahdaAndTheCorsairsQuest)
			{
				DidShowAct3Q2Commentary = false;
			}
			else if (quest is SpeakToTheSailorsQuest)
			{
				DidShowAct3Q3Commentary = false;
			}
			else if (quest is CaptureTheImperialMerchantPrusas)
			{
				DidShowAct3Q4Commentary = false;
			}
			else if (quest is FreeTheSeaHoundsCaptivesQuest)
			{
				DidShowAct3Q5Commentary = false;
			}
		}
		else if (quest is ReturnToBaseQuest)
		{
			CanShowNearOsticanCommentary = false;
		}
	}

	private void OnGameLoadFinished()
	{
		foreach (QuestBase quest in Campaign.Current.QuestManager.Quests)
		{
			if (quest.IsOngoing)
			{
				OnQuestStarted(quest);
			}
		}
	}

	private void QuarterlyHourlyTick()
	{
		if (!CanComment())
		{
			return;
		}
		bool flag = false;
		if (!DidShowAct2Commentary && _activeQuest != null && _activeQuest.Stage == NavalStorylineData.NavalStorylineStage.Act2 && _activeQuest is DefeatThePiratesQuest defeatThePiratesQuest && defeatThePiratesQuest.IsPiratePartyVisible())
		{
			AddNotification(new TextObject("{=PHPX0Hhe}That ship out there, raising sail to intercept us. That's got to be one of them."), MBInformationManager.NotificationPriority.Highest);
			DidShowAct2Commentary = true;
		}
		else if (!DidShowAct3Q1Commentary1 && _activeQuest != null && _activeQuest.Stage == NavalStorylineData.NavalStorylineStage.Act3Quest1 && _activeQuest is SetSailAndMeetTheFortuneSeekersInTargetSettlementQuest && IsNearSettlement(NavalStorylineData.Act3Quest1TargetSettlement))
		{
			AddNotification(new TextObject("{=Td3JWbx9}We are nearing Hvalvik. Keep an eye out for sails. A pity I can't set my feet on the soil of dear old Beinland this time, but I don't want to miss that merchantman."), MBInformationManager.NotificationPriority.Highest);
			DidShowAct3Q1Commentary1 = true;
		}
		else if (!DidShowAct3Q1Commentary2 && _activeQuest != null && _activeQuest.Stage == NavalStorylineData.NavalStorylineStage.Act3Quest1 && _activeQuest is SetSailAndEscortTheFortuneSeekersQuest setSailAndEscortTheFortuneSeekersQuest && setSailAndEscortTheFortuneSeekersQuest.AreEnemiesNearby())
		{
			AddNotification(new TextObject("{=5RgeG4bw}There's more of those devils! They seem to really want this prize."), MBInformationManager.NotificationPriority.Highest);
			DidShowAct3Q1Commentary2 = true;
		}
		else if (!DidShowAct3Q2Commentary && _activeQuest != null && _activeQuest.Stage == NavalStorylineData.NavalStorylineStage.Act3Quest2 && _activeQuest is HuntDownTheEmiraAlFahdaAndTheCorsairsQuest huntDownTheEmiraAlFahdaAndTheCorsairsQuest && huntDownTheEmiraAlFahdaAndTheCorsairsQuest.IsFahdaVisible())
		{
			AddNotification(new TextObject("{=GgYW2rJX}Hard to make things out in this rough sea, but she's got to be around here somewhere."), MBInformationManager.NotificationPriority.Highest);
			DidShowAct3Q2Commentary = true;
		}
		else if (!DidShowAct3Q3Commentary && _activeQuest != null && _activeQuest.Stage == NavalStorylineData.NavalStorylineStage.Act3SpeakToSailors && IsNearSettlement(NavalStorylineData.Act3Quest3TargetSettlement))
		{
			AddNotification(new TextObject("{=3bmPcXJA}We should be docking at Omor shortly. Not sure if we'll have time, but there's a place by the harbor that makes a fine herring pie…"), MBInformationManager.NotificationPriority.Highest);
			DidShowAct3Q3Commentary = true;
		}
		else if (!DidShowAct3Q4Commentary && _activeQuest != null && _activeQuest.Stage == NavalStorylineData.NavalStorylineStage.Act3Quest4 && _activeQuest is CaptureTheImperialMerchantPrusas captureTheImperialMerchantPrusas && captureTheImperialMerchantPrusas.IsCrusasVisible())
		{
			AddNotification(new TextObject("{=9C9hnWKO}The lookout has spotted sails. That may be Crusas's fleet."), MBInformationManager.NotificationPriority.Highest);
			DidShowAct3Q4Commentary = true;
		}
		else if (!DidShowAct3Q5Commentary && _activeQuest != null && _activeQuest.Stage == NavalStorylineData.NavalStorylineStage.Act3Quest5 && IsNearLocation(AngrafjordLocation, ImportantLocationLargeRadius))
		{
			AddNotification(new TextObject("{=7rCbyb0F}This part of Beinland, here in the far north, is called the Iskap. I can see why Purig made his base here. There's little reason for most ships to come this way, unless they're chasing whale or walrus. Even if you're an old Beinlander like me, the wind that whips past the promontory will put a chill in your bones."), MBInformationManager.NotificationPriority.Highest);
			DidShowAct3Q5Commentary = true;
		}
		else if (_finisterCooldown.IsPast && IsNearLocation(FinisterreLocation, ImportantLocationLargeRadius) && (_activeQuest == null || _activeQuest.Stage != NavalStorylineData.NavalStorylineStage.Act3Quest1))
		{
			AddNotification(new TextObject("{=jzIhvbYe}That cliff jutting out into the sea over there, that's the Revelkap. The Battanians will tell you it's the end of the earth, and the spirits of the dead take flight here for the west. Sometimes you can hear a howling that will set your hair on edge, though I'd say it's just the wind passing through the sea-caves at its foot."), MBInformationManager.NotificationPriority.High);
			_finisterCooldown = CampaignTime.Never;
		}
		else if (_byalicCooldown.IsPast && IsNearLocation(ByalicLocation, ByalicRadius))
		{
			AddNotification(new TextObject("{=gS2lr1Zq}We're well into the Byalic now. I know these waters well. When I was a boy, my father would bring me along on his trading voyages to the Sturgian lands. They'd stuff me between the bales of ivory and wrap me in a cloak, and I'd sit there looking out at the waves or playing with tafl-men. I don't think we have business here now, though. We've come too far east. Perhaps we should turn around?"), MBInformationManager.NotificationPriority.High);
			_byalicCooldown = CampaignTime.Never;
		}
		else if (_ransLaundryCooldown.IsPast && IsNearLocation(RansLaundryLocation, ImportantLocationLargeRadius))
		{
			AddNotification(new TextObject("{=m3bg8ahN}That passage between the isles off the coast and the mainland - we call that Ran's Laundry. The currents come through here fast and create whirlpools. The old lady of the sea washing out her nets, d'you think? Getting rid of fish-chewed bits of sailor in preparation for the next catch."), MBInformationManager.NotificationPriority.High);
			_ransLaundryCooldown = CampaignTime.Never;
		}
		else if (_trandEstuaryCooldown.IsPast && IsNearLocation(TrandEstuaryLocation, ImportantLocationLargeRadius))
		{
			AddNotification(new TextObject("{=1oD3fc8a}We're coming up the mouth of the Trand. You can see the water change color - that rich soil washing miles out to sea. Our forefathers knew it as the gateway to the lands round Pravend, the finest raiding a warrior could want. Then the Vlandians came and decided not to bother with cramped, cold ships - they'd sit in a castle and raid the lands around them. Except they call it \"ruling,\" not raiding."), MBInformationManager.NotificationPriority.High);
			_trandEstuaryCooldown = CampaignTime.Never;
		}
		else if (_charasCooldown.IsPast && IsNearLocation(GulfOfCharasLocation, SkatriaRadius) && (_activeQuest == null || _activeQuest.Stage != NavalStorylineData.NavalStorylineStage.Act3Quest2) && CurrentStage != NavalStorylineData.NavalStorylineStage.Act3Quest2)
		{
			AddNotification(new TextObject("{=7XyEmKQO}We're in the Gulf of Charas now. The fishermen here used to have this way of catching tuna - herd them into a maze of nets, into a smaller and smaller area, until they could just spear them from their boats and drag them out. Worked like dogs for a few days then ate like kings for months. Can't do that these days, though. Too many pirates about. Fishermen must be like fish, always moving to survive."), MBInformationManager.NotificationPriority.High);
			_charasCooldown = CampaignTime.Never;
		}
		else if (_garantorCastleCooldown.IsPast && IsNearLocation(GarantorCastleLocation, GarantorRadius))
		{
			AddNotification(new TextObject("{=L7A9aFlB}We're coming up here on the Gates of Garantor, the passage to the Perassic Sea. Treacherous waters, these, all the more so if you're at the helm of an overladen drakkar and you can't slow down because there's three vengeful sambuks on your tail! Ah, I could tell you some stories. I need to ask, though - are you going to the Perassic? Seems a bit out of our way."), MBInformationManager.NotificationPriority.High);
			_garantorCastleCooldown = CampaignTime.Never;
		}
		else if (_mazoporCooldown.IsPast && IsNearLocation(MarzoporLocation, ImportantLocationLargeRadius))
		{
			AddNotification(new TextObject("{=8FeBLAYc}This here is the estuary of Mazopor, taking us from the Byalic to Lake Laconis. We'd sail down here in peacetime if we wished to offer our services to the Sturgian boyars or the imperial governor at Diathma. In wartime, well... When you've got a lofty dromon bearing down on you, it's nice to have a bit more searoom to evade her than an estuary can give you."), MBInformationManager.NotificationPriority.High);
			_mazoporCooldown = CampaignTime.Never;
		}
		else if (_galendCooldown.IsPast && IsNearLocation(GalendLocation, ImportantLocationLargeRadius))
		{
			AddNotification(new TextObject("{=IWNneauS}That's Galend over there, and beyond that the Biscan coast. I respect the Biscaners. Their sailors are as brave, and as hungry for wealth, as any Nord. They'll go far into the western ocean in pursuit of whales. They can be very cagey about their voyages, though, when you press them for details. Sometimes I wonder if they've found something out there."), MBInformationManager.NotificationPriority.High);
			_galendCooldown = CampaignTime.Never;
		}
		else if (_rookCooldown.IsPast && MobileParty.MainParty.Position.X < RookBirdLocation.X && MobileParty.MainParty.Position.Y < RookBirdLocation.Y)
		{
			AddNotification(new TextObject("{=bvgLDvvS}Are you trying to take us to the Isle of Bounty, then, in the far south seas? Where you can walk the beach and gather emeralds like they were shells? Tempting, but also know that, in those waters, a great rook bird the size of a mountain might swoop down and pluck our ship right out of the waves. Let's think about turning around."), MBInformationManager.NotificationPriority.High);
			_rookCooldown = CampaignTime.Never;
		}
		else if (CanShowNearOsticanCommentary && IsNearSettlement(NavalStorylineData.HomeSettlement))
		{
			AddNotification(new TextObject("{=PmFbxbKk}Ostican in sight. Make ready to bring her in."), MBInformationManager.NotificationPriority.Medium);
			CanShowNearOsticanCommentary = false;
		}
		else if (_nextGoToPortEventTime.IsPast)
		{
			if (CanShowLowFoodCommentary && PartyBase.MainParty.IsStarving)
			{
				AddNotification(new TextObject("{=zmHH8l1p}Our food is running low. We should resupply in a nearby port."), MBInformationManager.NotificationPriority.Medium);
				CanShowLowFoodCommentary = false;
			}
			else if (CanShowLowTroopsAndShipsCommentary && IsLowOnTroopsOrShips())
			{
				AddNotification(new TextObject("{=TujGZTu3}We have taken too many losses. Let's stop in a nearby port and gather reinforcements."), MBInformationManager.NotificationPriority.Medium);
				CanShowLowTroopsAndShipsCommentary = false;
			}
			else if (CanShowLowShipHpCommentary && MobileParty.MainParty.Ships.Average((Ship ship) => ship.HitPoints / ship.MaxHitPoints) < 0.6f)
			{
				AddNotification(new TextObject("{=Q7vglid0}Our ships are damaged. Let's stop in a nearby port to repair."), MBInformationManager.NotificationPriority.Medium);
			}
			else if (CanShowStormCommentary && IsMainPartyInStorm() && (_activeQuest == null || _activeQuest.Stage != NavalStorylineData.NavalStorylineStage.Act3Quest2))
			{
				flag = true;
			}
		}
		else if (CanShowStormCommentary && IsMainPartyInStorm() && (_activeQuest == null || _activeQuest.Stage != NavalStorylineData.NavalStorylineStage.Act3Quest2))
		{
			flag = true;
		}
		if (flag)
		{
			AddNotification(new TextObject("{=JdKcm9LY}Steer clear of these storms, if you can. The winds and waves can damage our vessels."), MBInformationManager.NotificationPriority.Medium);
			CanShowStormCommentary = false;
		}
	}

	private bool IsLowOnTroopsOrShips()
	{
		if (!(MobileParty.MainParty.PartySizeRatio < 0.5f))
		{
			if (MobileParty.MainParty.IsNavalStorylineQuestParty(out var partyData) && partyData != null && partyData.IsQuestParty)
			{
				return partyData.Template.ShipHulls.Sum((ShipTemplateStack x) => x.MaxValue) > MobileParty.MainParty.Ships.Count;
			}
			return false;
		}
		return true;
	}

	public override void SyncData(IDataStore dataStore)
	{
		dataStore.SyncData("_galendCooldown", ref _galendCooldown);
		dataStore.SyncData("_mazoporCooldown", ref _mazoporCooldown);
		dataStore.SyncData("_garantorCastleCooldown", ref _garantorCastleCooldown);
		dataStore.SyncData("_charasCooldown", ref _charasCooldown);
		dataStore.SyncData("_trandEstuaryCooldown", ref _trandEstuaryCooldown);
		dataStore.SyncData("_ransLaundryCooldown", ref _ransLaundryCooldown);
		dataStore.SyncData("_finisterCooldown", ref _finisterCooldown);
		dataStore.SyncData("_byalicCooldown", ref _byalicCooldown);
		dataStore.SyncData("_rookCooldown", ref _rookCooldown);
	}

	private bool CanComment()
	{
		return MobileParty.MainParty.IsActive && MobileParty.MainParty.IsCurrentlyAtSea && IsStorylineActive && !MobileParty.MainParty.IsInNavalAutoTravel && MobileParty.MainParty.MapEvent == null && MobileParty.MainParty.SiegeEvent == null;
	}

	private void AddNotification(TextObject text, MBInformationManager.NotificationPriority priority)
	{
		CampaignInformationManager.AddDialogLine(text, NavalStorylineData.Gunnar.CharacterObject, null, 0, priority);
		_nextGoToPortEventTime = CampaignTime.HoursFromNow(6f);
	}

	private static bool IsNearLocation(CampaignVec2 location, float radius)
	{
		return MobileParty.MainParty.Position.DistanceSquared(location) <= radius * radius;
	}

	private bool IsNearSettlement(Settlement settlement)
	{
		float num = MobileParty.MainParty.Position.DistanceSquared(settlement.PortPosition);
		if (num <= CommentarySettlementArrivalRadius * CommentarySettlementArrivalRadius * 4f && MobileParty.MainParty.TargetSettlement == settlement)
		{
			return true;
		}
		if (num <= CommentarySettlementArrivalRadius * CommentarySettlementArrivalRadius)
		{
			return true;
		}
		return false;
	}

	private bool IsMainPartyInStorm()
	{
		foreach (Storm spawnedStorm in NavalDLCManager.Instance.StormManager.SpawnedStorms)
		{
			if (spawnedStorm.CurrentPosition.Distance(MobileParty.MainParty.Position.ToVec2()) < spawnedStorm.EffectRadius * 0.9f)
			{
				return true;
			}
		}
		return false;
	}
}
