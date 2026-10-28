using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using NavalDLC.Missions;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;
using TaleWorlds.SaveSystem;

namespace NavalDLC.Storyline.Quests;

public class SetSailAndEscortTheFortuneSeekersQuest : NavalStorylineQuestBase
{
	private const string MerchantCharacterStringId = "vlandian_fortune_seekers";

	private const string Act3Quest1CaravanPartyTemplateStringId = "storyline_act3_quest_1_caravan_party_template";

	private const string Act3Quest1GenericPartyTemplateStringId = "storyline_act3_quest_1_generic_party_template";

	private const string Act3Quest1SpecialPartyTemplateStringId = "storyline_act3_quest_1_special_party_template";

	private const int TargetSettlementArrivalRadius = 10;

	private const float MapEventInvulnerabilityDurationInHours = 8f;

	public const string PlayerPartySailPatternId = "generated_square__h4_09";

	public const string MerchantPartySailPatternId = "generated_square_l1_h4_04";

	public const string SeaHoundsPartySailPatternId = "generated_square_l1_h4_10";

	private static readonly Dictionary<string, string> MerchantShipUpgradePieces = new Dictionary<string, string> { { "sail", "sails_lvl2" } };

	private static readonly Dictionary<string, string> RegularBanditShipUpgradePieces = new Dictionary<string, string>
	{
		{ "sail", "sails_lvl2" },
		{ "side", "side_northern_shields_lvl1" }
	};

	private static readonly Dictionary<string, string> SpecialBanditShipUpgradePieces = new Dictionary<string, string>
	{
		{ "sail", "sails_lvl2" },
		{ "side", "side_northern_shields_lvl1" }
	};

	private CharacterObject _merchantCharacter;

	[SaveableField(1)]
	private bool _isMerchantPartyWaitingForEscort;

	[SaveableField(2)]
	private bool _isMerchantPartySaved;

	[SaveableField(3)]
	private bool _isAfterFightDialogDone;

	[SaveableField(4)]
	private bool _specialBattleWon;

	[SaveableField(5)]
	private MobileParty _merchantParty;

	[SaveableField(6)]
	private MobileParty _initialBanditParty;

	[SaveableField(7)]
	private MobileParty _secondBanditParty;

	[SaveableField(8)]
	private MobileParty _specialBanditParty;

	[SaveableField(9)]
	private Settlement _targetSettlement;

	[SaveableField(10)]
	private bool _willProgressStoryline;

	[SaveableField(11)]
	private bool _hasMetMerchantParty;

	private List<Vec2> _banditSpawnPositions;

	public override bool WillProgressStoryline => _willProgressStoryline;

	public override NavalStorylineData.NavalStorylineStage Stage => NavalStorylineData.NavalStorylineStage.Act3Quest1;

	public bool HasMetMerchants => _hasMetMerchantParty;

	public bool HasSavedMerchants => _isMerchantPartySaved;

	public bool IsConversationHeroTheMerchant => CharacterObject.OneToOneConversationCharacter == _merchantCharacter;

	private TextObject QuestSecondPhaseStartLog
	{
		get
		{
			TextObject textObject = new TextObject("{=ycq46riU}Escort the Vlandian merchants the rest of the way to {SETTLEMENT_LINK}.");
			textObject.SetTextVariable("SETTLEMENT_LINK", NavalStorylineData.HomeSettlement.EncyclopediaLinkWithName);
			return textObject;
		}
	}

	protected override string MainPartyTemplateStringId => "storyline_act3_quest_1_main_party_template";

	private TextObject MerchantPartyArrivedToHomeSettlementNotification
	{
		get
		{
			TextObject textObject = new TextObject("{=7ZFbP4TO}You have successfully escorted the Vlandian merchants to {SETTLEMENT_LINK}.");
			textObject.SetTextVariable("SETTLEMENT_LINK", NavalStorylineData.HomeSettlement.EncyclopediaLinkWithName);
			return textObject;
		}
	}

	private TextObject FailLogText => new TextObject("{=F0bGPXyz}You failed to defend the Vlandian merchants.");

	public override TextObject Title => new TextObject("{=ntIGLPdc}Escort the Vlandian Merchants");

	private TextObject _descriptionLogText => new TextObject("{=ik68yVRc}Guard a Vlandian merchant ship sailing home from Beinland.");

	private TextObject _allyDefeatedText => new TextObject("{=9sfcVI0Q}Your allies were defeated. You will have to try again.");

	public SetSailAndEscortTheFortuneSeekersQuest(string questId, Hero questGiver, Settlement targetSettlement)
		: base(questId, questGiver, CampaignTime.Never, 0)
	{
		_willProgressStoryline = false;
		_targetSettlement = targetSettlement;
		SetMerchantCharacterReference();
		AddLog(_descriptionLogText);
	}

	protected override void SetDialogs()
	{
		AddMerchantDialogue();
	}

	protected override void InitializeQuestOnGameLoadInternal()
	{
		SetMerchantCharacterReference();
		AddGameMenus();
		SetDialogs();
		SetBanditSpawnPositions();
		if (_merchantParty != null && _merchantParty.IsActive)
		{
			NavalDLCHelpers.SetCustomSailPatternOfPartyShips(_merchantParty, "generated_square_l1_h4_04");
		}
		MobileParty activeBanditParty = GetActiveBanditParty();
		if (activeBanditParty != null && activeBanditParty.IsActive)
		{
			NavalDLCHelpers.SetCustomSailPatternOfPartyShips(activeBanditParty, "generated_square_l1_h4_10");
		}
		if (MobileParty.MainParty.IsActive)
		{
			NavalDLCHelpers.SetCustomSailPatternOfPartyShips(MobileParty.MainParty, "generated_square__h4_09");
		}
	}

	private void SetMerchantCharacterReference()
	{
		_merchantCharacter = MBObjectManager.Instance.GetObject<CharacterObject>("vlandian_fortune_seekers");
	}

	protected override void OnStartQuestInternal()
	{
		AddGameMenus();
		SetDialogs();
		SpawnMerchantParty();
		SetBanditSpawnPositions();
		CampaignVec2 banditSpawnPosition = GetBanditSpawnPosition(0);
		_initialBanditParty = SpawnBanditParty("set_sail_and_escort_generic_party_1", Campaign.Current.ObjectManager.GetObject<PartyTemplateObject>("storyline_act3_quest_1_generic_party_template"), isSpecialParty: false, banditSpawnPosition);
		_willProgressStoryline = true;
	}

	private void SetBanditSpawnPositions()
	{
		_banditSpawnPositions = new List<Vec2>
		{
			new Vec2(200f, 655f),
			new Vec2(202f, 615f),
			new Vec2(210f, 595f)
		};
	}

	private CampaignVec2 GetBanditSpawnPosition(int index)
	{
		Vec2 pos = _banditSpawnPositions[index];
		return NavigationHelper.FindReachablePointAroundPosition(new CampaignVec2(pos, isOnLand: false), MobileParty.NavigationType.Naval, 5f);
	}

	protected override void IsNavalQuestPartyInternal(PartyBase party, NavalStorylinePartyData data)
	{
		if (_initialBanditParty?.Party == party || _secondBanditParty?.Party == party)
		{
			PartyTemplateObject templateObject = Campaign.Current.ObjectManager.GetObject<PartyTemplateObject>("storyline_act3_quest_1_generic_party_template");
			data.PartySize = (int)NavalDLCHelpers.GetMaxPartySizeLimitFromTemplate(templateObject).ResultNumber;
			data.IsQuestParty = true;
		}
		else if (_merchantParty?.Party == party)
		{
			PartyTemplateObject templateObject2 = Campaign.Current.ObjectManager.GetObject<PartyTemplateObject>("storyline_act3_quest_1_caravan_party_template");
			data.PartySize = (int)NavalDLCHelpers.GetMaxPartySizeLimitFromTemplate(templateObject2).ResultNumber;
			data.IsQuestParty = true;
		}
		else if (_specialBanditParty?.Party == party)
		{
			PartyTemplateObject templateObject3 = Campaign.Current.ObjectManager.GetObject<PartyTemplateObject>("storyline_act3_quest_1_special_party_template");
			data.PartySize = (int)NavalDLCHelpers.GetMaxPartySizeLimitFromTemplate(templateObject3).ResultNumber;
			data.IsQuestParty = true;
		}
	}

	private void AddMerchantDialogue()
	{
		Campaign.Current.ConversationManager.AddDialogFlow(DialogFlow.CreateDialogFlow("start").NpcLine("{=6QkMVCgz}Ahoy! It's good to have you with us. We've seen sails, and I reckon that there are still pirates about.").Condition(() => _hasMetMerchantParty && !_isMerchantPartySaved && CharacterObject.OneToOneConversationCharacter == _merchantCharacter)
			.CloseDialog(), this);
		Campaign.Current.ConversationManager.AddDialogFlow(DialogFlow.CreateDialogFlow("start").NpcLine("{=acz9UxsD}Thank the Heavens. And thank you. Those Sea Hound vessels would have torn us to pieces. You came just in time.").Condition(() => _isMerchantPartySaved && !_isAfterFightDialogDone && CharacterObject.OneToOneConversationCharacter == _merchantCharacter)
			.NpcLine("{=CowdyMzB}[ib:confident3]We would still wish to show you our gratitude. I took a collection among the men whose lives you saved today. We wish to offer you a barrel of oil and a bundle of ivory. These are the rewards of our labor over the past months, but they would mean nothing to us if our ship were seized by pirates.")
			.Consequence(delegate
			{
				AddLog(QuestSecondPhaseStartLog);
				_isAfterFightDialogDone = true;
			})
			.BeginPlayerOptions()
			.PlayerOption("{=e69pk8m2}I accept your gift. Let us return to Ostican.")
			.Consequence(AcceptGifts)
			.CloseDialog()
			.PlayerOption("{=sacjGtbK}You risked much for those goods. Keep them.")
			.Consequence(RejectGifts)
			.CloseDialog()
			.EndPlayerOptions()
			.CloseDialog(), this);
		Campaign.Current.ConversationManager.AddDialogFlow(DialogFlow.CreateDialogFlow("start").NpcLine("{=acz9UxsD}Thank the Heavens. And thank you. Those Sea Hound vessels would have torn us to pieces. You came just in time.").Condition(() => _isMerchantPartySaved && _isAfterFightDialogDone && CharacterObject.OneToOneConversationCharacter == _merchantCharacter)
			.CloseDialog(), this);
	}

	public void OnMerchantsMet()
	{
		_hasMetMerchantParty = true;
		DirectMerchantPartyToBase();
	}

	private void AcceptGifts()
	{
		ItemRosterElement itemRosterElement = new ItemRosterElement(Items.All.GetRandomElementWithPredicate((ItemObject x) => x.IsTradeGood && x.ItemCategory == DefaultItemCategories.Oil), 1);
		PartyBase.MainParty.ItemRoster.AddToCounts(itemRosterElement.EquipmentElement, itemRosterElement.Amount);
		ItemRosterElement itemRosterElement2 = new ItemRosterElement(Items.All.GetRandomElementWithPredicate((ItemObject x) => x.IsTradeGood && x.ItemCategory == NavalItemCategories.WalrusTusk), 1);
		PartyBase.MainParty.ItemRoster.AddToCounts(itemRosterElement2.EquipmentElement, itemRosterElement2.Amount);
	}

	private void RejectGifts()
	{
		TraitLevelingHelper.OnIssueSolvedThroughQuest(Hero.MainHero, new Tuple<TraitObject, int>[1]
		{
			new Tuple<TraitObject, int>(DefaultTraits.Generosity, 50)
		});
	}

	protected override void HourlyTick()
	{
		if (_merchantParty == null || !_merchantParty.IsActive || !base.IsOngoing)
		{
			return;
		}
		if (_merchantParty.MapEvent == null)
		{
			float getEncounterJoiningRadius = Campaign.Current.Models.EncounterModel.GetEncounterJoiningRadius;
			if (!_hasMetMerchantParty && _merchantParty.Position.DistanceSquared(MobileParty.MainParty.Position) <= getEncounterJoiningRadius * getEncounterJoiningRadius)
			{
				EncounterManager.StartPartyEncounter(MobileParty.MainParty.Party, _merchantParty.Party);
			}
			if (!_isMerchantPartySaved && GetActiveBanditParty() != null && _merchantParty.Position.DistanceSquared(GetActiveBanditParty().Position) <= getEncounterJoiningRadius * getEncounterJoiningRadius)
			{
				MBInformationManager.AddQuickInformation(new TextObject("{=cjkHktxl}The merchant party is under attack."), 0, null, null, "event:/ui/notification/quest_update");
				EncounterManager.StartPartyEncounter(GetActiveBanditParty().Party, _merchantParty.Party);
				return;
			}
			if (_merchantParty.Position.DistanceSquared(NavalStorylineData.HomeSettlement.PortPosition) <= 100f)
			{
				MBInformationManager.AddQuickInformation(MerchantPartyArrivedToHomeSettlementNotification);
				CompleteQuestWithSuccess();
				return;
			}
			UtilizePartyEscortBehavior(_merchantParty, MobileParty.MainParty, ref _isMerchantPartyWaitingForEscort, 7f, 11f, DirectMerchantPartyToBase);
			MobileParty activeBanditParty = GetActiveBanditParty();
			if (activeBanditParty != null && PlayerCaptivity.CaptorParty != activeBanditParty.Party)
			{
				if (!IsTracked(activeBanditParty) && activeBanditParty.Position.Distance(MobileParty.MainParty.Position) < MobileParty.MainParty.SeeingRange)
				{
					AddTrackedObject(activeBanditParty);
				}
				SetPartyAiAction.GetActionForEngagingParty(activeBanditParty, _merchantParty, MobileParty.NavigationType.Naval, isFromPort: false);
				activeBanditParty.Ai.SetDoNotMakeNewDecisions(doNotMakeNewDecisions: true);
			}
			AdjustMerchantPartySpeed();
		}
		else if (_merchantParty.MapEvent.IsInvulnerable && _merchantParty.MapEvent.BattleStartTime.ElapsedHoursUntilNow > 8f)
		{
			_merchantParty.MapEvent.IsInvulnerable = false;
		}
	}

	private MobileParty GetActiveBanditParty()
	{
		return _initialBanditParty ?? _secondBanditParty ?? _specialBanditParty;
	}

	private void DirectMerchantPartyToBase()
	{
		SetPartyAiAction.GetActionForVisitingSettlement(_merchantParty, NavalStorylineData.HomeSettlement, MobileParty.NavigationType.Naval, isFromPort: false, isTargetingPort: true);
	}

	protected override void RegisterEventsInternal()
	{
		CampaignEvents.MapEventEnded.AddNonSerializedListener(this, MapEventEnded);
		CampaignEvents.MapEventStarted.AddNonSerializedListener(this, MapEventStarted);
		CampaignEvents.GameMenuOpened.AddNonSerializedListener(this, OnGameMenuOpened);
		CampaignEvents.OnMissionEndedEvent.AddNonSerializedListener(this, OnMissionEnded);
	}

	private void OnGameMenuOpened(MenuCallbackArgs args)
	{
		if (NavalStorylineData.IsNavalStoryLineActive() && PlayerEncounter.Current != null && PlayerEncounter.EncounteredParty != null && PlayerEncounter.EncounteredParty.IsNavalStorylineQuestParty())
		{
			string obj = args.MenuContext?.GameMenu?.StringId;
			if (obj == "naval_storyline_encounter_meeting")
			{
				if (PlayerEncounter.EncounteredParty == _merchantParty.Party)
				{
					if (PlayerEncounter.MeetingDone)
					{
						PlayerEncounter.LeaveEncounter = true;
					}
				}
				else
				{
					PlayerEncounter.SetMeetingDone();
				}
			}
			if (obj == "naval_storyline_encounter" && GetActiveBanditParty() != null && (_initialBanditParty?.Party == PlayerEncounter.EncounteredParty || _secondBanditParty?.Party == PlayerEncounter.EncounteredParty) && (PlayerEncounter.EncounteredBattle == null || !PlayerEncounter.EncounteredBattle.HasWinner))
			{
				MapEvent encounteredBattle = PlayerEncounter.EncounteredBattle;
				if (encounteredBattle == null || !encounteredBattle.InvolvedParties.Contains(_merchantParty.Party))
				{
					MBTextManager.SetTextVariable("ENCOUNTER_TEXT", new TextObject("{=Iu7TkxZo}“A ship! A ship!” calls out one of your lookouts. You can see it too - a square sail, outlined against the steel-gray northern sky. One the Sea Hounds has spotted you, and thinks to make you its prey."));
				}
				else
				{
					MBTextManager.SetTextVariable("ENCOUNTER_TEXT", new TextObject("{=XfqPvVDc}“A ship! A ship!” calls out one of your lookouts. You can see it too - a square sail, outlined against the steel-gray northern sky. One of the Sea Hounds stalking the merchant seems to be closing in on its prey."));
				}
			}
		}
		if (args.MenuContext?.GameMenu?.StringId == "naval_storyline_encounter" && GetActiveBanditParty() != null && PlayerEncounter.Current != null && (PlayerEncounter.EncounteredBattle.InvolvedParties.Contains(_specialBanditParty?.Party) || PlayerEncounter.EncounteredMobileParty == _specialBanditParty))
		{
			GameMenu.SwitchToMenu("naval_storyline_act3_quest1_setpiece_menu");
		}
	}

	private void OnMissionEnded(IMission mission)
	{
		if (Mission.Current.IsNavalBattle && PlayerEncounter.Current != null && PlayerEncounter.EncounteredParty != null && _specialBanditParty?.Party == PlayerEncounter.EncounteredParty && PlayerEncounter.Battle != null && PlayerEncounter.Battle.BattleState == BattleState.DefenderVictory)
		{
			_specialBattleWon = true;
			_isMerchantPartySaved = true;
		}
	}

	private void SpawnMerchantParty()
	{
		Clan clan = new Clan();
		clan.StringId = Campaign.Current.CampaignObjectManager.FindNextUniqueStringId<Clan>("naval_storyline_vlandian_merchant_clan");
		clan.ChangeClanName(new TextObject("{=FjwRsf1C}Vlandia"), new TextObject("{=FjwRsf1C}Vlandia"));
		clan.Culture = MBObjectManager.Instance.GetObject<CultureObject>("vlandia");
		clan.Banner = Banner.CreateRandomClanBanner();
		clan.Color = 4287441178u;
		clan.Color2 = 4294426438u;
		clan.Banner.ChangePrimaryColor(4287441178u);
		clan.Banner.ChangeBackgroundColor(4287441178u, 4287441178u);
		clan.Banner.ChangeIconColors(4294426438u);
		TextObject name = new TextObject("{=FyfpoKvX}Vlandian Merchants");
		CampaignVec2 portPosition = _targetSettlement.PortPosition;
		PartyTemplateObject partyTemplate = Campaign.Current.ObjectManager.GetObject<PartyTemplateObject>("storyline_act3_quest_1_caravan_party_template");
		_merchantParty = CustomPartyComponent.CreateCustomPartyWithPartyTemplate(portPosition, 0.1f, NavalStorylineData.HomeSettlement, name, clan, partyTemplate, null, "camel", "camel_saddle_b", MobileParty.MainParty.Speed * 1.5f);
		NavalDLCHelpers.AddUpgradePiecesToPartyShips(_merchantParty, MerchantShipUpgradePieces);
		NavalDLCHelpers.SetCustomSailPatternOfPartyShips(_merchantParty, "generated_square_l1_h4_04");
		foreach (Ship ship in _merchantParty.Ships)
		{
			ship.IsInvulnerable = true;
		}
		_merchantParty.MemberRoster.AddToCounts(_merchantCharacter, 1);
		_merchantParty.ItemRoster.AddToCounts(DefaultItems.Grain, 40);
		_merchantParty.IgnoreByOtherPartiesTill(base.QuestDueTime);
		SetPartyAiAction.GetActionForEngagingParty(_merchantParty, MobileParty.MainParty, MobileParty.NavigationType.Naval, isFromPort: false);
		_merchantParty.Ai.SetDoNotMakeNewDecisions(doNotMakeNewDecisions: true);
		_merchantParty.SetPartyUsedByQuest(isActivelyUsed: true);
		AddTrackedObject(_merchantParty);
	}

	private void AdjustMerchantPartySpeed()
	{
		if (!_hasMetMerchantParty)
		{
			return;
		}
		MobileParty activeBanditParty = GetActiveBanditParty();
		MobileParty mobileParty = MobileParty.MainParty;
		if (!mobileParty.IsActive || activeBanditParty == null || !activeBanditParty.IsActive)
		{
			return;
		}
		float num = Campaign.Current.Models.EncounterModel.GetEncounterJoiningRadius * 2.5f;
		if (activeBanditParty.Position.DistanceSquared(_merchantParty.Position) <= num * num)
		{
			mobileParty = activeBanditParty;
		}
		float referencePartySpeed = GetReferencePartySpeed(mobileParty);
		float speed = _merchantParty.Speed;
		CustomPartyComponent customPartyComponent = _merchantParty.PartyComponent as CustomPartyComponent;
		while (referencePartySpeed < speed || ShouldMerchantPartyCatchUpWithParty(mobileParty, referencePartySpeed, speed))
		{
			referencePartySpeed = GetReferencePartySpeed(mobileParty);
			if (speed > referencePartySpeed || referencePartySpeed.ApproximatelyEqualsTo(speed))
			{
				customPartyComponent.SetBaseSpeed(customPartyComponent.BaseSpeed - 0.05f);
			}
			else if (ShouldMerchantPartyCatchUpWithParty(mobileParty, referencePartySpeed, speed))
			{
				customPartyComponent.SetBaseSpeed(customPartyComponent.BaseSpeed + 0.05f);
			}
			speed = _merchantParty.Speed;
		}
	}

	private bool ShouldMerchantPartyCatchUpWithParty(MobileParty referenceParty, float cachedReferencePartySpeed, float cachedMerchantPartySpeed)
	{
		if (referenceParty.IsMainParty && cachedMerchantPartySpeed <= 5.5f)
		{
			return TaleWorlds.Library.MathF.Abs(cachedMerchantPartySpeed - cachedReferencePartySpeed) > 0.7f;
		}
		return false;
	}

	private float GetReferencePartySpeed(MobileParty referenceParty)
	{
		float num = 1f;
		if (referenceParty.IsActive)
		{
			num = referenceParty.Speed;
			if (referenceParty == GetActiveBanditParty())
			{
				num -= 0.5f;
			}
		}
		return num;
	}

	private MobileParty SpawnBanditParty(string stringId, PartyTemplateObject partyTemplate, bool isSpecialParty, CampaignVec2 banditPartyPosition)
	{
		Hideout hideout = SettlementHelper.FindNearestHideoutToMobileParty(MobileParty.MainParty, MobileParty.NavigationType.All, (Settlement x) => x.IsActive);
		Clan clan = Clan.All.FirstOrDefault((Clan x) => x.StringId == "northern_pirates");
		MobileParty mobileParty = BanditPartyComponent.CreateBanditParty(stringId, clan, hideout.Settlement.Hideout, isBossParty: false, partyTemplate, banditPartyPosition);
		mobileParty.Party.SetCustomName(new TextObject("{=SKC3FeGR}Sea Hounds"));
		mobileParty.SetPartyUsedByQuest(isActivelyUsed: true);
		mobileParty.SetLandNavigationAccess(access: false);
		foreach (Ship ship in mobileParty.Ships)
		{
			ship.IsInvulnerable = true;
			if (isSpecialParty)
			{
				ship.IsTradeable = false;
				ship.IsUsedByQuest = true;
			}
		}
		NavalDLCHelpers.AddUpgradePiecesToPartyShips(mobileParty, isSpecialParty ? SpecialBanditShipUpgradePieces : RegularBanditShipUpgradePieces);
		NavalDLCHelpers.SetCustomSailPatternOfPartyShips(mobileParty, "generated_square_l1_h4_10");
		mobileParty.IgnoreByOtherPartiesTill(base.QuestDueTime);
		mobileParty.Ai.SetDoNotMakeNewDecisions(doNotMakeNewDecisions: true);
		mobileParty.Party.SetCustomBanner(NavalStorylineData.CorsairBanner);
		mobileParty.InitializePartyTrade(QuestHelper.CalculateInitialGoldForBanditQuestParty(mobileParty));
		return mobileParty;
	}

	private void MapEventStarted(MapEvent mapEvent, PartyBase attackerParty, PartyBase defenderParty)
	{
		if (attackerParty.IsNavalStorylineQuestParty())
		{
			foreach (Ship ship in attackerParty.Ships)
			{
				ship.IsInvulnerable = false;
			}
		}
		if (defenderParty.IsNavalStorylineQuestParty())
		{
			foreach (Ship ship2 in defenderParty.Ships)
			{
				ship2.IsInvulnerable = false;
			}
		}
		if (defenderParty.MobileParty == _merchantParty && attackerParty.MobileParty == GetActiveBanditParty())
		{
			mapEvent.IsInvulnerable = true;
		}
	}

	private void MapEventEnded(MapEvent mapEvent)
	{
		if (!_isMerchantPartySaved && mapEvent.WinningSide != BattleSideEnum.None && mapEvent.DefeatedSide != BattleSideEnum.None)
		{
			MapEventSide mapEventSide = mapEvent.GetMapEventSide(mapEvent.WinningSide);
			MapEventSide mapEventSide2 = mapEvent.GetMapEventSide(mapEvent.DefeatedSide);
			MobileParty banditParty = GetActiveBanditParty();
			if (mapEventSide2.Parties.Any((MapEventParty t) => t.Party == _merchantParty.Party) && !mapEventSide2.IsMainPartyAmongParties())
			{
				OnMerchantPartyDestroyed();
			}
			else if (mapEventSide2.Parties.Any((MapEventParty t) => t.Party == banditParty?.Party))
			{
				if (mapEventSide.IsMainPartyAmongParties())
				{
					if (_merchantParty.IsActive)
					{
						OnBanditPartyDestroyed();
						if (_merchantParty.MemberRoster.TotalHealthyCount == 0 && mapEvent.InvolvedParties.Contains(_merchantParty.Party))
						{
							_merchantParty.MemberRoster.Clear();
							_merchantParty.MemberRoster.AddToCounts(_merchantCharacter, 11);
						}
					}
					else
					{
						OnMerchantPartyDestroyed();
					}
				}
				else
				{
					OnMerchantSurvivedWithoutHelp();
				}
			}
			if (banditParty != null && banditParty.IsActive && mapEvent.InvolvedParties.Contains(banditParty.Party) && (banditParty.NavigationCapability & MobileParty.NavigationType.Naval) == MobileParty.NavigationType.Naval)
			{
				banditParty.SetMovePatrolAroundSettlement(NavalStorylineData.HomeSettlement, MobileParty.NavigationType.Naval, isTargetingPort: true);
			}
		}
		if (_merchantParty != null && _merchantParty.IsActive && mapEvent.InvolvedParties.Contains(_merchantParty.Party) && !_isMerchantPartySaved && _merchantParty.MemberRoster.TotalHealthyCount > 0)
		{
			DirectMerchantPartyToBase();
		}
	}

	private void OnBanditPartyDestroyed()
	{
		if (GetActiveBanditParty() == _initialBanditParty || GetActiveBanditParty() == _secondBanditParty)
		{
			CampaignVec2 banditSpawnPosition = GetBanditSpawnPosition(2);
			_specialBanditParty = SpawnBanditParty("set_sail_and_escort_special_party", Campaign.Current.ObjectManager.GetObject<PartyTemplateObject>("storyline_act3_quest_1_special_party_template"), isSpecialParty: true, banditSpawnPosition);
			_specialBanditParty.IsInfoHidden = true;
			_initialBanditParty = null;
			_secondBanditParty = null;
		}
	}

	private void OpenConversationWithMerchants()
	{
		ConversationCharacterData playerCharacterData = new ConversationCharacterData(CharacterObject.PlayerCharacter, PartyBase.MainParty, noHorse: true);
		ConversationCharacterData conversationPartnerData = new ConversationCharacterData(_merchantCharacter, _merchantParty.Party, noHorse: true);
		CampaignMission.OpenConversationMission(playerCharacterData, conversationPartnerData);
	}

	private void OnMerchantPartyDestroyed()
	{
		ShowAllyDefeatedPopUp();
	}

	private void OnMerchantSurvivedWithoutHelp()
	{
		CancelQuest();
	}

	private void CancelQuest(TextObject logText = null)
	{
		CompleteQuestWithCancel(logText);
		NavalStorylineData.DeactivateNavalStoryline();
	}

	protected override void OnFinalizeInternal()
	{
		MobileParty activeBanditParty = GetActiveBanditParty();
		if (activeBanditParty != null && activeBanditParty.IsActive)
		{
			DestroyPartyAction.Apply(null, activeBanditParty);
		}
		if (_merchantParty.IsActive)
		{
			if (_merchantParty.MapEventSide != null)
			{
				_merchantParty.MapEventSide = null;
			}
			DestroyPartyAction.ApplyForDisbanding(_merchantParty, NavalStorylineData.HomeSettlement);
		}
		if (_merchantParty?.ActualClan != null)
		{
			DestroyClanAction.Apply(_merchantParty.ActualClan);
		}
	}

	private void ShowAllyDefeatedPopUp()
	{
		TextObject textObject = new TextObject("{=cH3Kpkwg}Ally Defeated");
		InformationManager.ShowInquiry(new InquiryData(affirmativeText: new TextObject("{=DM6luo3c}Continue").ToString(), titleText: textObject.ToString(), text: _allyDefeatedText.ToString(), isAffirmativeOptionShown: true, isNegativeOptionShown: false, negativeText: null, affirmativeAction: OnAllyDefeatedPopUpClosed, negativeAction: null), pauseGameActiveState: true);
	}

	private void OnAllyDefeatedPopUpClosed()
	{
		CancelQuest(_allyDefeatedText);
	}

	public static void UtilizePartyEscortBehavior(MobileParty escortedParty, MobileParty escortParty, ref bool isWaitingForEscortParty, float innerRadius, float outerRadius, MobilePartyHelper.ResumePartyEscortBehaviorDelegate onPartyEscortBehaviorResumed, bool showDebugSpheres = false)
	{
		if (!isWaitingForEscortParty)
		{
			if (escortParty.Position.DistanceSquared(escortedParty.Position) >= outerRadius * outerRadius)
			{
				escortedParty.SetMoveGoToPoint(escortedParty.Position, MobileParty.NavigationType.All);
				escortedParty.Ai.CheckPartyNeedsUpdate();
				isWaitingForEscortParty = true;
			}
		}
		else if (escortParty.Position.DistanceSquared(escortedParty.Position) <= innerRadius * innerRadius)
		{
			onPartyEscortBehaviorResumed();
			escortedParty.Ai.CheckPartyNeedsUpdate();
			isWaitingForEscortParty = false;
		}
	}

	private void AddGameMenus()
	{
		AddGameMenu("naval_storyline_act3_quest1_setpiece_menu", new TextObject("{=tcfyZUb8}A brief squall cuts visibility to a few bowshots, and when it clears, you see that two Sea Hound vessels have snuck up upon the merchant’s ship and are in hot pursuit. They are much faster, so unless you can close and defeat them or draw them off, it is likely that your ally will be taken."), naval_storyline_act_3_quest_1_setpiece_menu_on_init, GameMenu.MenuOverlayType.Encounter);
		AddGameMenuOption("naval_storyline_act3_quest1_setpiece_menu", "naval_storyline_act3_quest1_setpiece_attack", new TextObject("{=DM6luo3c}Continue"), naval_storyline_act3_quest1_setpiece_attack_condition, naval_storyline_act3_quest1_setpiece_attack_consequence);
		AddGameMenu("set_piece_retry_menu", new TextObject("{=etH1IHNZ}You manage to put some distance between you and your enemies, and you have a moment to consider how to proceed."), set_piece_retry_menu_on_init);
		AddGameMenuOption("set_piece_retry_menu", "try_again_option", new TextObject("{=YHMDy3lQ}Try again"), set_piece_retry_menu_try_again_on_condition, encounter_menu_try_again_on_consequence);
		AddGameMenuOption("set_piece_retry_menu", "leave_option", new TextObject("{=3sRdGQou}Leave"), leave_on_condition, leave_on_consequence, Isleave: true);
	}

	private void naval_storyline_act_3_quest_1_setpiece_menu_on_init(MenuCallbackArgs args)
	{
		args.MenuContext.SetBackgroundMeshName("encounter_naval");
		NavalStorylineData.OnCheckpointReached(NavalStorylineData.NavalStorylineCheckpoint.Act3Quest1SetPieceEncounterMenu);
	}

	private bool naval_storyline_act3_quest1_setpiece_attack_condition(MenuCallbackArgs args)
	{
		args.optionLeaveType = GameMenuOption.LeaveType.Continue;
		return true;
	}

	private void naval_storyline_act3_quest1_setpiece_attack_consequence(MenuCallbackArgs args)
	{
		StartBattle();
	}

	private void set_piece_retry_menu_on_init(MenuCallbackArgs args)
	{
		args.MenuContext.SetBackgroundMeshName("encounter_naval");
		if (_specialBattleWon)
		{
			DestroyPartyAction.Apply(null, _specialBanditParty);
			_merchantParty.Ai.SetDoNotMakeNewDecisions(doNotMakeNewDecisions: true);
			DirectMerchantPartyToBase();
			PlayerEncounter.Finish();
			OpenConversationWithMerchants();
			_specialBanditParty = null;
			NavalStorylineData.OnCheckpointReached(NavalStorylineData.NavalStorylineCheckpoint.Act3Quest1SetPieceSucceeded);
		}
	}

	private bool set_piece_retry_menu_try_again_on_condition(MenuCallbackArgs args)
	{
		args.optionLeaveType = GameMenuOption.LeaveType.Mission;
		return true;
	}

	private void encounter_menu_try_again_on_consequence(MenuCallbackArgs args)
	{
		StartBattle();
	}

	private bool leave_on_condition(MenuCallbackArgs args)
	{
		args.optionLeaveType = GameMenuOption.LeaveType.Leave;
		return true;
	}

	private void leave_on_consequence(MenuCallbackArgs args)
	{
		CancelQuest(FailLogText);
	}

	private void StartBattle()
	{
		_specialBattleWon = false;
		if (Hero.MainHero.IsWounded)
		{
			Hero.MainHero.Heal(Hero.MainHero.WoundedHealthLimit - Hero.MainHero.HitPoints + 1);
		}
		PlayerEncounter.Finish();
		PlayerEncounter.Start();
		PlayerEncounter.Current.SetupFields(_specialBanditParty.Party, PartyBase.MainParty);
		PlayerEncounter.StartBattle();
		_merchantParty.MapEventSide = PlayerEncounter.Battle.GetMapEventSide(PlayerEncounter.Battle.PlayerSide);
		MissionInitializerRecord navalMissionInitializerTemplate = NavalStorylineData.GetNavalMissionInitializerTemplate("naval_storyline_act_3_quest_1");
		TerrainType faceTerrainType = Campaign.Current.MapSceneWrapper.GetFaceTerrainType(MobileParty.MainParty.CurrentNavigationFace);
		navalMissionInitializerTemplate.TerrainType = (int)faceTerrainType;
		navalMissionInitializerTemplate.NeedsRandomTerrain = false;
		navalMissionInitializerTemplate.PlayingInCampaignMode = true;
		navalMissionInitializerTemplate.RandomTerrainSeed = MBRandom.RandomInt(10000);
		navalMissionInitializerTemplate.AtmosphereOnCampaign = Campaign.Current.Models.MapWeatherModel.GetAtmosphereModel(MobileParty.MainParty.Position);
		navalMissionInitializerTemplate.SceneHasMapPatch = false;
		navalMissionInitializerTemplate.AtmosphereOnCampaign.NauticalInfo.UsesNavalSimulatedWater = 1;
		PartyTemplateObject template = Campaign.Current.ObjectManager.GetObject<PartyTemplateObject>("storyline_act3_quest_1_caravan_party_template");
		new MBList<Ship>(NavalDLCHelpers.GetSetPieceBattleShips(base.Template, PartyBase.MainParty));
		new MBList<Ship>(NavalDLCHelpers.GetSetPieceBattleShips(template, _merchantParty.Party));
		new MBList<Ship>(_specialBanditParty.Ships);
		NavalMissions.OpenHelpingAnAllySetPieceBattleMission(navalMissionInitializerTemplate, _merchantParty, _specialBanditParty);
		GameMenu.ActivateGameMenu("set_piece_retry_menu");
	}

	public bool AreEnemiesNearby()
	{
		if (_specialBanditParty != null && _specialBanditParty.IsActive)
		{
			return _specialBanditParty.IsVisible;
		}
		return false;
	}

	protected override void OnCompleteWithSuccessInternal()
	{
		GainRenownAction.Apply(Hero.MainHero, 5f);
	}
}
