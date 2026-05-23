using System.Collections.Generic;
using System.Linq;
using Helpers;
using SandBox.Missions.MissionLogics.Hideout;
using StoryMode.GameComponents.CampaignBehaviors;
using StoryMode.StoryModeObjects;
using StoryMode.StoryModePhases;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.SaveSystem;

namespace StoryMode.Quests.TutorialPhase;

public class FindHideoutTutorialQuest : StoryModeQuestBase
{
	public enum HideoutBattleEndState
	{
		None,
		Retreated,
		Defeated,
		Victory
	}

	private const string RaiderPartyStringId = "radagos_raider_party_";

	private const string TutorialHideoutSceneName = "forest_hideout_003";

	private const int RaiderPartyCount = 2;

	private const int RaiderPartySize = 4;

	private const int MainPartyHealHitPointLimit = 50;

	private const int MaximumHealth = 100;

	private const int PlayerPartySizeMinLimitToAttack = 4;

	[SaveableField(1)]
	private readonly Settlement _hideout;

	[SaveableField(2)]
	private List<MobileParty> _raiderParties;

	private bool _foughtWithRadagos;

	private bool _dueledRadagos;

	[SaveableField(4)]
	private bool _talkedWithRadagos;

	[SaveableField(5)]
	private bool _talkedWithBrother;

	[SaveableField(6)]
	private HideoutBattleEndState _hideoutBattleEndState;

	private List<CharacterObject> _mainPartyTroopBackup;

	private static string _activeHideoutStringId;

	private TextObject _startQuestLog
	{
		get
		{
			TextObject textObject = new TextObject("{=gSBGpUBm}Find {RADAGOS.LINK}' hideout.");
			StringHelpers.SetCharacterProperties("RADAGOS", StoryModeHeroes.Radagos.CharacterObject, textObject);
			return textObject;
		}
	}

	public override TextObject Title
	{
		get
		{
			TextObject textObject = new TextObject("{=NvkWtb8f}Find the Hideout of {RADAGOS.NAME}' Gang and Defeat Them");
			StringHelpers.SetCharacterProperties("RADAGOS", StoryModeHeroes.Radagos.CharacterObject, textObject);
			return textObject;
		}
	}

	public FindHideoutTutorialQuest(Settlement hideout)
		: base("find_hideout_tutorial_quest", null, CampaignTime.Never)
	{
		_hideout = hideout;
		_activeHideoutStringId = _hideout.StringId;
		_hideout.Party.SetCustomName(new TextObject("{=9xaEPyNV}{RADAGOS.NAME}' Hideout"));
		StringHelpers.SetCharacterProperties("RADAGOS", StoryModeHeroes.Radagos.CharacterObject, _hideout.Name);
		_raiderParties = new List<MobileParty>();
		_foughtWithRadagos = false;
		_talkedWithRadagos = false;
		_talkedWithBrother = false;
		_hideoutBattleEndState = HideoutBattleEndState.None;
		InitializeHideout();
		AddTrackedObject(_hideout);
		SetDialogs();
		AddGameMenus();
		InitializeQuestOnCreation();
		AddLog(_startQuestLog);
		StoryMode.StoryModePhases.TutorialPhase.Instance.SetTutorialFocusSettlement(_hideout);
	}

	protected override void HourlyTick()
	{
	}

	protected override void RegisterEvents()
	{
		CampaignEvents.GameMenuOpened.AddNonSerializedListener(this, OnGameMenuOpened);
		CampaignEvents.MapEventEnded.AddNonSerializedListener(this, OnMapEventEnded);
		CampaignEvents.MobilePartyDestroyed.AddNonSerializedListener(this, OnMobilePartyDestroyed);
		CampaignEvents.OnGameLoadFinishedEvent.AddNonSerializedListener(this, OnGameLoadFinished);
	}

	public override void OnHeroCanDieInfoIsRequested(Hero hero, KillCharacterAction.KillCharacterActionDetail causeOfDeath, ref bool result)
	{
		if (hero == StoryModeHeroes.Radagos)
		{
			result = false;
		}
	}

	public override void OnHeroCanBeSelectedInInventoryInfoIsRequested(Hero hero, ref bool result)
	{
		if (hero == StoryModeHeroes.Radagos)
		{
			result = false;
		}
	}

	public override void OnHeroCanHaveCampaignIssuesInfoIsRequested(Hero hero, ref bool result)
	{
		if (hero == StoryModeHeroes.Radagos)
		{
			result = false;
		}
	}

	protected override void InitializeQuestOnGameLoad()
	{
		_activeHideoutStringId = _hideout.StringId;
		_hideout.Party.SetCustomName(new TextObject("{=9xaEPyNV}{RADAGOS.NAME}' Hideout"));
		StringHelpers.SetCharacterProperties("RADAGOS", StoryModeHeroes.Radagos.CharacterObject, _hideout.Name);
		SetDialogs();
		AddGameMenus();
		if (_raiderParties.Count <= 2)
		{
			return;
		}
		for (int num = _raiderParties.Count - 1; num >= 0; num--)
		{
			if (_raiderParties[num].MapEvent == null && !_raiderParties[num].IsActive)
			{
				_raiderParties.Remove(_raiderParties[num]);
			}
		}
		int num2 = _raiderParties.Count - 1;
		while (num2 >= 0)
		{
			if (!_raiderParties[num2].IsBanditBossParty && _raiderParties[num2].MapEvent == null)
			{
				if (!_raiderParties[num2].IsActive)
				{
					_raiderParties.Remove(_raiderParties[num2]);
				}
				else
				{
					DestroyPartyAction.Apply(null, _raiderParties[num2]);
				}
			}
			if (_raiderParties.Count > 2)
			{
				num2--;
				continue;
			}
			break;
		}
	}

	protected override void OnStartQuest()
	{
		if (GameStateManager.Current.ActiveState is MapState mapState)
		{
			mapState.Handler.StartCameraAnimation(_hideout.GatePosition, 1f);
		}
	}

	private void InitializeHideout()
	{
		_hideout.Hideout.IsSpotted = true;
		_hideout.IsVisible = true;
		if (!_hideout.Hideout.IsInfested)
		{
			for (int i = 0; i < 2; i++)
			{
				if (_hideout.Hideout.IsInfested)
				{
					break;
				}
				_raiderParties.Add(CreateRaiderParty(_raiderParties.Count + 1, isBanditBossParty: false));
			}
		}
		if (!_hideout.Parties.Any((MobileParty p) => p.IsBanditBossParty))
		{
			_raiderParties.Add(CreateRaiderParty(_raiderParties.Count + 1, isBanditBossParty: true));
		}
		foreach (MobileParty party in _hideout.Parties)
		{
			if (party.IsBanditBossParty)
			{
				int totalRegulars = party.MemberRoster.TotalRegulars;
				party.MemberRoster.Clear();
				if (StoryModeHeroes.Radagos.HeroState != Hero.CharacterStates.Active)
				{
					StoryModeHeroes.Radagos.ChangeState(Hero.CharacterStates.Active);
				}
				party.MemberRoster.AddToCounts(StoryModeHeroes.Radagos.CharacterObject, 1);
				CharacterObject character = Campaign.Current.ObjectManager.GetObject<CharacterObject>("storymode_quest_raider");
				party.MemberRoster.AddToCounts(character, totalRegulars);
				StoryModeHeroes.Radagos.Heal(100);
				break;
			}
		}
	}

	private MobileParty CreateRaiderParty(int number, bool isBanditBossParty)
	{
		MobileParty mobileParty = BanditPartyComponent.CreateBanditParty("radagos_raider_party_" + number, _hideout.OwnerClan, _hideout.Hideout, isBanditBossParty, null, _hideout.GatePosition);
		CharacterObject character = Campaign.Current.ObjectManager.GetObject<CharacterObject>("storymode_quest_raider");
		mobileParty.MemberRoster.AddToCounts(character, 4);
		mobileParty.Party.SetCustomName(new TextObject("{=u1Pkt4HC}Raiders"));
		mobileParty.ActualClan = _hideout.OwnerClan;
		mobileParty.Position = _hideout.Position;
		mobileParty.Party.SetCustomOwner(StoryModeHeroes.Radagos);
		mobileParty.Party.SetVisualAsDirty();
		EnterSettlementAction.ApplyForParty(mobileParty, _hideout);
		float num = mobileParty.Party.CalculateCurrentStrength();
		int initialGold = (int)(1f * MBRandom.RandomFloat * 20f * num + 50f);
		mobileParty.InitializePartyTrade(initialGold);
		mobileParty.SetMoveGoToSettlement(_hideout, MobileParty.NavigationType.Default, isTargetingThePort: false);
		EnterSettlementAction.ApplyForParty(mobileParty, _hideout);
		mobileParty.SetPartyUsedByQuest(isActivelyUsed: true);
		return mobileParty;
	}

	protected override void SetDialogs()
	{
		StringHelpers.SetCharacterProperties("TACTEOS", StoryModeHeroes.Tacitus.CharacterObject);
		Campaign.Current.ConversationManager.AddDialogFlow(DialogFlow.CreateDialogFlow("start", 1000015).NpcLine(new TextObject("{=R3CnF55p}So... Who's this that comes through my place of business, killing my employees?[if:convo_confused_voice][ib:warrior2]")).Condition(bandit_hideout_boss_fight_start_on_condition)
			.PlayerLine(new TextObject("{=itRoeaJf}We heard you took our little brother and sister. Where are they?"))
			.NpcLine(GameTexts.FindText("find_hideout_quest_radagos_conversation_line_1"))
			.NpcLine(new TextObject("{=wWLnZ6G4}Since your hunt for your kin is fruitless, how about you clear off and save your own lives? Either that or I force you to lick up all the blood you've spilled here with your tongues. Or... You and I could settle this, one on one.[if:convo_angry_voice]"))
			.BeginPlayerOptions()
			.PlayerOption(new TextObject("{=ImLQNYWC}Very well - I'll duel you."))
			.Consequence(bandit_hideout_start_duel_fight_on_consequence)
			.CloseDialog()
			.PlayerOption(new TextObject("{=MMv3hsmI}I don't duel slavers. Men, attack!"))
			.ClickableCondition(bandit_hideout_continue_battle_on_clickable_condition)
			.Consequence(bandit_hideout_continue_battle_on_consequence)
			.CloseDialog()
			.EndPlayerOptions()
			.CloseDialog(), this);
		Campaign.Current.ConversationManager.AddDialogFlow(DialogFlow.CreateDialogFlow("start", 1000010).NpcLine(new TextObject("{=ZhZ7MCeh}Well. I recognize defeat when I see it. If I'm going to be your captive, let me introduce myself. I'm Radagos.[ib:weary2][if:convo_uncomfortable_voice]")).Condition(radagos_meeting_conversation_condition)
			.NpcLine(new TextObject("{=w0CUaEU7}You haven't cut my throat yet, which was a wise move. I'm sure I can find a way to be worth more to you alive than dead.[if:convo_calm_friendly]"))
			.PlayerLine(new TextObject("{=vDRRsed8}You'd better help us get our brother and sister back, or you'll swing from a tree."))
			.NpcLine(GameTexts.FindText("find_hideout_quest_radagos_conversation_line_2"))
			.NpcLine(GameTexts.FindText("find_hideout_quest_radagos_conversation_line_3"))
			.NpcLine(new TextObject("{=FWSwngVX}Shall we get on the road? Remember - if I drop dead of exhaustion, or drown in some river, that's it for your little dears. I don't expect a cozy palanquin, now, but you'd best not make it too hard a trip for me.[if:convo_uncomfortable_voice]"))
			.Consequence(radagos_meeting_conversation_consequence)
			.CloseDialog(), this);
		Campaign.Current.ConversationManager.AddDialogFlow(DialogFlow.CreateDialogFlow("start", 1000020).NpcLine(new TextObject("{=qp2zYfua}I was hoping to find more treasure here, but I think business wasn't going too well for {RADAGOS.NAME} and his gang.[ib:closed2][if:convo_pondering]")).Condition(brother_farewell_conversation_condition)
			.NpcLine(new TextObject("{=J4qetbZb}I found this strange looking metal piece though. It doesn't look too valuable, but it could be the artifact {TACTEOS.NAME} was talking about. Maybe we can sell it to one of the noble clans for a hefty price.[if:convo_astonished]"))
			.PlayerLine(new TextObject("{=OffNcRby}All right then. Let's get on the road."))
			.NpcLine(GameTexts.FindText("find_hideout_quest_brother_conversation_line_1"))
			.NpcLine(GameTexts.FindText("find_hideout_quest_brother_conversation_line_2"))
			.NpcLine(new TextObject("{=fp6QBO7l}I'll need to take these men with us. {RADAGOS.NAME} is a slippery one. I don't want him getting away.[if:convo_confused_voice]"))
			.PlayerLine(new TextObject("{=RJ9NbuYr}So you want me to raise the money to ransom the little ones?"))
			.NpcLine(new TextObject("{=4OUnPjZc}Indeed. You'll have to find a way to do that. Maybe this bronze thing can help.[if:convo_empathic_voice]"))
			.NpcLine(new TextObject("{=5soUEFEJ}{TACTEOS.NAME} said it could be worth a fortune to the right person, if you manage not to get killed. If he's telling the truth, you must be careful. Never reveal that you have it. Try to understand its value, and how it can be sold.[if:convo_pondering]"))
			.NpcLine(new TextObject("{=jPKIN2r4}One more thing. When you are talking to nobles and other people of importance, make sure you present yourself as someone from a distant but distinguished family.[if:convo_thinking]"))
			.NpcLine(new TextObject("{=GVMGXfxS}You can use our family name if you like or make up a new one. You will have a better chance of obtaining an audience with nobles and it'll be easier for me to find you by asking around.[if:convo_normal]"))
			.Consequence(SelectClanName)
			.NpcLine(GameTexts.FindText("find_hideout_quest_brother_conversation_line_3"))
			.CloseDialog(), this);
	}

	private bool bandit_hideout_boss_fight_start_on_condition()
	{
		PartyBase encounteredParty = PlayerEncounter.EncounteredParty;
		if (encounteredParty == null || encounteredParty.IsMobile || encounteredParty.MapFaction == null || !encounteredParty.MapFaction.IsBanditFaction)
		{
			return false;
		}
		if (!_foughtWithRadagos && encounteredParty.IsSettlement && encounteredParty.Settlement.IsHideout && encounteredParty.Settlement == _hideout && Mission.Current != null && Mission.Current.GetMissionBehavior<HideoutMissionController>() != null && Hero.OneToOneConversationHero != null)
		{
			return Hero.OneToOneConversationHero == StoryModeHeroes.Radagos;
		}
		return false;
	}

	private void bandit_hideout_start_duel_fight_on_consequence()
	{
		Campaign.Current.ConversationManager.ConversationEndOneShot += HideoutMissionController.StartBossFightDuelMode;
		_dueledRadagos = true;
		_foughtWithRadagos = true;
	}

	private bool bandit_hideout_continue_battle_on_clickable_condition(out TextObject explanation)
	{
		bool flag = false;
		foreach (Agent activeAgent in Mission.Current.PlayerTeam.ActiveAgents)
		{
			if (!activeAgent.IsMount && activeAgent.Character != CharacterObject.PlayerCharacter)
			{
				flag = true;
				break;
			}
		}
		explanation = TextObject.GetEmpty();
		if (!flag)
		{
			explanation = new TextObject("{=F9HxO1iS}You don't have any men.");
		}
		return flag;
	}

	private void bandit_hideout_continue_battle_on_consequence()
	{
		Campaign.Current.ConversationManager.ConversationEndOneShot += HideoutMissionController.StartBossFightBattleMode;
		_foughtWithRadagos = true;
	}

	private bool radagos_meeting_conversation_condition()
	{
		if (_foughtWithRadagos)
		{
			return Hero.OneToOneConversationHero == StoryModeHeroes.Radagos;
		}
		return false;
	}

	private void radagos_meeting_conversation_consequence()
	{
		StoryModeHeroes.Radagos.SetHasMet();
		_ = StoryModeHeroes.Radagos.PartyBelongedTo;
		DisableHeroAction.Apply(StoryModeHeroes.Radagos);
		_talkedWithRadagos = true;
		Campaign.Current.ConversationManager.ConversationEndOneShot += OpenBrotherConversationMenu;
	}

	private void OpenBrotherConversationMenu()
	{
		GameMenu.ActivateGameMenu("brother_chest_menu");
	}

	private bool brother_farewell_conversation_condition()
	{
		StringHelpers.SetCharacterProperties("RADAGOS", StoryModeHeroes.Radagos.CharacterObject);
		StringHelpers.SetCharacterProperties("TACTEOS", StoryModeHeroes.Tacitus.CharacterObject);
		StringHelpers.SetCharacterProperties("LITTLE_BROTHER", StoryModeHeroes.LittleBrother.CharacterObject);
		StringHelpers.SetCharacterProperties("LITTLE_SISTER", StoryModeHeroes.LittleSister.CharacterObject);
		if (Hero.OneToOneConversationHero == StoryModeHeroes.ElderBrother)
		{
			return _talkedWithRadagos;
		}
		return false;
	}

	private void SelectClanName()
	{
		InformationManager.ShowTextInquiry(new TextInquiryData(new TextObject("{=JJiKk4ow}Select your family name: ").ToString(), string.Empty, isAffirmativeOptionShown: true, isNegativeOptionShown: false, GameTexts.FindText("str_done").ToString(), null, OnChangeClanNameDone, null, shouldInputBeObfuscated: false, FactionHelper.IsClanNameApplicable, "", Clan.PlayerClan.Name.ToString()));
	}

	private void OnChangeClanNameDone(string newClanName)
	{
		TextObject textObject = GameTexts.FindText("str_generic_clan_name");
		textObject.SetTextVariable("CLAN_NAME", new TextObject(newClanName));
		Clan.PlayerClan.ChangeClanName(textObject, textObject);
		Game.Current.GameStateManager.PushState(Game.Current.GameStateManager.CreateState<BannerEditorState>());
	}

	private bool OpenBannerSelectionScreen()
	{
		return true;
	}

	private void OnGameMenuOpened(MenuCallbackArgs menuCallbackArgs)
	{
		StoryModeHeroes.Radagos.Heal(StoryModeHeroes.Radagos.MaxHitPoints);
		if (Settlement.CurrentSettlement != null && Settlement.CurrentSettlement == _hideout && _hideoutBattleEndState == HideoutBattleEndState.None && menuCallbackArgs.MenuContext.GameMenu.StringId != "radagos_hideout" && menuCallbackArgs.MenuContext.GameMenu.StringId != "brother_chest_menu")
		{
			GameMenu.SwitchToMenu("radagos_hideout");
		}
		if (_hideoutBattleEndState == HideoutBattleEndState.Victory && _talkedWithRadagos && menuCallbackArgs.MenuContext.GameMenu.StringId != "brother_chest_menu")
		{
			Campaign.Current.GameMenuManager.SetNextMenu("brother_chest_menu");
		}
		else
		{
			if (_hideoutBattleEndState != HideoutBattleEndState.Defeated && _hideoutBattleEndState != HideoutBattleEndState.Retreated)
			{
				return;
			}
			foreach (MobileParty party in _hideout.Parties)
			{
				foreach (TroopRosterElement item in party.MemberRoster.GetTroopRoster())
				{
					if (item.Character.IsHero)
					{
						item.Character.HeroObject.Heal(50 - item.Character.HeroObject.HitPoints);
						continue;
					}
					int elementWoundedNumber = party.MemberRoster.GetElementWoundedNumber(party.MemberRoster.FindIndexOfTroop(item.Character));
					if (elementWoundedNumber > 0)
					{
						party.MemberRoster.AddToCounts(item.Character, 0, insertAtFront: false, -elementWoundedNumber);
					}
				}
				if (!party.IsBanditBossParty && party.MemberRoster.TotalManCount < 4)
				{
					int totalManCount = party.MemberRoster.TotalManCount;
					CharacterObject character = Campaign.Current.ObjectManager.GetObject<CharacterObject>("storymode_quest_raider");
					party.MemberRoster.AddToCounts(character, 4 - totalManCount);
				}
				if (MBSaveLoad.LastLoadedGameVersion < ApplicationVersion.FromString("v1.1.1") && party.IsBanditBossParty && party.MemberRoster.GetTroopCount(StoryModeHeroes.Radagos.CharacterObject) <= 0)
				{
					if (StoryModeHeroes.Radagos.HeroState != Hero.CharacterStates.Active)
					{
						StoryModeHeroes.Radagos.ChangeState(Hero.CharacterStates.Active);
					}
					party.MemberRoster.AddToCounts(StoryModeHeroes.Radagos.CharacterObject, 1);
				}
			}
			if (Hero.MainHero.IsPrisoner)
			{
				EndCaptivityAction.ApplyByPeace(Hero.MainHero);
				Hero elderBrother = StoryModeHeroes.ElderBrother;
				if (elderBrother.PartyBelongedToAsPrisoner != null)
				{
					EndCaptivityAction.ApplyByPeace(elderBrother);
				}
				if (!elderBrother.IsActive)
				{
					elderBrother.ChangeState(Hero.CharacterStates.Active);
				}
				if (elderBrother.PartyBelongedTo == null)
				{
					AddHeroToPartyAction.Apply(elderBrother, MobileParty.MainParty, showNotification: false);
				}
			}
			if (_hideoutBattleEndState == HideoutBattleEndState.Defeated || _hideoutBattleEndState == HideoutBattleEndState.Retreated)
			{
				TextObject textObject = new TextObject("{=Zq9qXcCk}You are defeated by the {RADAGOS.NAME}' Party, but your brother saved you. It doesn't look like they're going anywhere, though, so you should attack again once you're ready. You must have at least {NUMBER} members in your party. If you don't, go back to {QUEST_VILLAGE} and recruit some more troops.");
				StringHelpers.SetCharacterProperties("RADAGOS", StoryModeHeroes.Radagos.CharacterObject, textObject);
				textObject.SetTextVariable("NUMBER", 4);
				textObject.SetTextVariable("QUEST_VILLAGE", Settlement.Find("village_ES3_2").Name);
				InformationManager.ShowInquiry(new InquiryData(((_hideoutBattleEndState == HideoutBattleEndState.Defeated) ? new TextObject("{=FPhWhjq7}Defeated") : new TextObject("{=w6Wa3lSL}Retreated")).ToString(), textObject.ToString(), isAffirmativeOptionShown: true, isNegativeOptionShown: false, new TextObject("{=yS7PvrTD}OK").ToString(), null, delegate
				{
					_hideout.Hideout.IsSpotted = true;
					_hideout.IsVisible = true;
				}, null));
			}
			if (menuCallbackArgs.MenuContext.GameMenu.StringId == "radagos_hideout" && _hideoutBattleEndState == HideoutBattleEndState.Retreated)
			{
				PlayerEncounter.Finish();
			}
			if (_hideoutBattleEndState == HideoutBattleEndState.Defeated || _hideoutBattleEndState == HideoutBattleEndState.Retreated)
			{
				if (Hero.MainHero.HitPoints < 50)
				{
					Hero.MainHero.Heal(50 - Hero.MainHero.HitPoints);
				}
				Hero elderBrother2 = StoryModeHeroes.ElderBrother;
				if (elderBrother2.HitPoints < 50)
				{
					elderBrother2.Heal(50 - elderBrother2.HitPoints);
				}
				if (elderBrother2.PartyBelongedToAsPrisoner != null)
				{
					EndCaptivityAction.ApplyByPeace(elderBrother2);
				}
				if (elderBrother2.PartyBelongedTo == null)
				{
					PartyBase.MainParty.MemberRoster.AddToCounts(elderBrother2.CharacterObject, 1);
				}
			}
			_hideoutBattleEndState = HideoutBattleEndState.None;
			_foughtWithRadagos = false;
			foreach (MobileParty party2 in _hideout.Parties)
			{
				foreach (TroopRosterElement item2 in party2.PrisonRoster.GetTroopRoster())
				{
					if (_mainPartyTroopBackup.Contains(item2.Character))
					{
						int index = party2.PrisonRoster.FindIndexOfTroop(item2.Character);
						int elementWoundedNumber2 = party2.PrisonRoster.GetElementWoundedNumber(index);
						int num = party2.PrisonRoster.GetTroopCount(item2.Character) - elementWoundedNumber2;
						if (num > 0)
						{
							party2.PrisonRoster.AddToCounts(item2.Character, -num);
							PartyBase.MainParty.MemberRoster.AddToCounts(item2.Character, num);
						}
					}
				}
			}
			_mainPartyTroopBackup?.Clear();
		}
	}

	private void OnMapEventEnded(MapEvent mapEvent)
	{
		if (_hideoutBattleEndState == HideoutBattleEndState.Victory && !_talkedWithRadagos)
		{
			CampaignMission.OpenConversationMission(new ConversationCharacterData(CharacterObject.PlayerCharacter, null, noHorse: true), new ConversationCharacterData(StoryModeHeroes.Radagos.CharacterObject, null, noHorse: true, noWeapon: true));
		}
	}

	private void OnMobilePartyDestroyed(MobileParty mobileParty, PartyBase destroyerParty)
	{
		if (_raiderParties.Contains(mobileParty))
		{
			_raiderParties.Remove(mobileParty);
		}
	}

	private void OnGameLoadFinished()
	{
		for (int num = _hideout.Parties.Count - 1; num >= 0; num--)
		{
			MobileParty mobileParty = _hideout.Parties[num];
			if (mobileParty.IsBandit && mobileParty.MapEvent == null)
			{
				while (mobileParty.MemberRoster.TotalManCount > 4)
				{
					foreach (TroopRosterElement item in mobileParty.MemberRoster.GetTroopRoster())
					{
						if (!item.Character.IsHero)
						{
							mobileParty.MemberRoster.RemoveTroop(item.Character);
						}
						if (mobileParty.MemberRoster.TotalManCount <= 4)
						{
							break;
						}
					}
				}
			}
		}
		while (_hideout.Party.MemberRoster.TotalManCount > 4 && _hideout.Party.MapEvent == null)
		{
			foreach (TroopRosterElement item2 in _hideout.Party.MemberRoster.GetTroopRoster())
			{
				if (!item2.Character.IsHero)
				{
					_hideout.Party.MemberRoster.RemoveTroop(item2.Character);
				}
				if (_hideout.Party.MemberRoster.TotalManCount <= 4)
				{
					break;
				}
			}
		}
	}

	private void AddGameMenus()
	{
		StringHelpers.SetCharacterProperties("TACTEOS", StoryModeHeroes.Tacitus.CharacterObject);
		StringHelpers.SetCharacterProperties("RADAGOS", StoryModeHeroes.Radagos.CharacterObject);
		AddGameMenu("radagos_hideout", new TextObject("{=z8LQn2Uh}You have arrived at the hideout."), radagos_hideout_menu_on_init);
		AddGameMenuOption("radagos_hideout", "enter_hideout", new TextObject("{=zxMOqlhs}Attack"), enter_radagos_hideout_condition, enter_radagos_hideout_on_consequence);
		AddGameMenuOption("radagos_hideout", "leave_hideout", new TextObject("{=3sRdGQou}Leave"), leave_radagos_hideout_condition, leave_radagos_hideout_on_consequence, Isleave: true);
		AddGameMenu("brother_chest_menu", new TextObject("{=bhQ6Jbom}You come across a chest with an old piece of bronze in it. It's so battered and corroded that it could have been anything from a cup to a crown. This must be the chest {TACTEOS.NAME} mentioned to you, that had something to do with 'Neretzes' Folly'."), brother_chest_menu_on_init);
		AddGameMenuOption("brother_chest_menu", "brother_chest_menu_continue", new TextObject("{=DM6luo3c}Continue"), brother_chest_menu_on_condition, brother_chest_menu_on_consequence);
	}

	private void brother_chest_menu_on_init(MenuCallbackArgs menuCallbackArgs)
	{
		if (_talkedWithBrother)
		{
			_hideoutBattleEndState = HideoutBattleEndState.None;
			PlayerEncounter.Finish();
			CompleteQuestWithSuccess();
		}
	}

	private bool brother_chest_menu_on_condition(MenuCallbackArgs menuCallbackArgs)
	{
		menuCallbackArgs.optionLeaveType = GameMenuOption.LeaveType.Continue;
		return base.IsOngoing;
	}

	private void brother_chest_menu_on_consequence(MenuCallbackArgs menuCallbackArgs)
	{
		_talkedWithBrother = true;
		CampaignMission.OpenConversationMission(new ConversationCharacterData(CharacterObject.PlayerCharacter, null, noHorse: true), new ConversationCharacterData(StoryModeHeroes.ElderBrother.CharacterObject, null, noHorse: true, noWeapon: true));
	}

	private void radagos_hideout_menu_on_init(MenuCallbackArgs menuCallbackArgs)
	{
		menuCallbackArgs.MenuTitle = new TextObject("{=8OIwHZF1}Hideout");
		StringHelpers.SetCharacterProperties("RADAGOS", StoryModeHeroes.Radagos.CharacterObject);
		if (PlayerEncounter.Current == null)
		{
			return;
		}
		MapEvent playerMapEvent = MapEvent.PlayerMapEvent;
		if (playerMapEvent != null)
		{
			if (playerMapEvent.WinningSide == playerMapEvent.PlayerSide)
			{
				if (_dueledRadagos)
				{
					Campaign.Current.CampaignBehaviorManager.GetBehavior<AchievementsCampaignBehavior>()?.OnRadagosDuelWon();
				}
				_hideoutBattleEndState = HideoutBattleEndState.Victory;
			}
			else if (playerMapEvent.WinningSide == BattleSideEnum.None)
			{
				_hideoutBattleEndState = HideoutBattleEndState.Retreated;
			}
			else
			{
				_hideoutBattleEndState = HideoutBattleEndState.Defeated;
			}
			_dueledRadagos = false;
		}
		if (_hideoutBattleEndState != HideoutBattleEndState.None)
		{
			PlayerEncounter.Update();
		}
	}

	private bool enter_radagos_hideout_condition(MenuCallbackArgs menuCallbackArgs)
	{
		menuCallbackArgs.optionLeaveType = GameMenuOption.LeaveType.Mission;
		if (MobileParty.MainParty.MemberRoster.TotalManCount < 4)
		{
			menuCallbackArgs.IsEnabled = false;
			menuCallbackArgs.Tooltip = new TextObject("{=kaZ1XtDX}You are not strong enough to attack. Recruit more troops from the village.");
		}
		if (base.IsOngoing)
		{
			return _hideoutBattleEndState == HideoutBattleEndState.None;
		}
		return false;
	}

	private void enter_radagos_hideout_on_consequence(MenuCallbackArgs menuCallbackArgs)
	{
		_hideoutBattleEndState = HideoutBattleEndState.None;
		_mainPartyTroopBackup = new List<CharacterObject>();
		foreach (TroopRosterElement item in PartyBase.MainParty.MemberRoster.GetTroopRoster())
		{
			if (!item.Character.IsHero)
			{
				_mainPartyTroopBackup.Add(item.Character);
			}
		}
		if (!_hideout.Hideout.IsInfested || _hideout.Parties.Count < 3)
		{
			InitializeHideout();
		}
		foreach (MobileParty party in _hideout.Parties)
		{
			if (party.IsBanditBossParty && party.MemberRoster.Contains(party.Party.Culture.BanditBoss))
			{
				party.MemberRoster.RemoveTroop(party.Party.Culture.BanditBoss);
			}
		}
		if (PlayerEncounter.Battle == null)
		{
			PlayerEncounter.StartBattle();
			PlayerEncounter.Update();
		}
		CampaignMission.OpenHideoutBattleMission("forest_hideout_003", null, isTutorial: true);
	}

	private bool leave_radagos_hideout_condition(MenuCallbackArgs menuCallbackArgs)
	{
		menuCallbackArgs.optionLeaveType = GameMenuOption.LeaveType.Leave;
		return base.IsOngoing;
	}

	private void leave_radagos_hideout_on_consequence(MenuCallbackArgs menuCallbackArgs)
	{
		_hideoutBattleEndState = HideoutBattleEndState.None;
		PlayerEncounter.Finish();
	}

	[GameMenuInitializationHandler("radagos_hideout")]
	[GameMenuInitializationHandler("brother_chest_menu")]
	private static void quest_game_menus_on_init_background(MenuCallbackArgs args)
	{
		Settlement settlement = Settlement.Find(_activeHideoutStringId);
		args.MenuContext.SetBackgroundMeshName(settlement.Hideout.WaitMeshName);
	}

	protected override void OnCompleteWithSuccess()
	{
		_hideout.Party.SetCustomName(null);
		_hideout.Party.SetVisualAsDirty();
		StoryModeHeroes.Radagos.Heal(100);
		StoryModeManager.Current.MainStoryLine.CompleteTutorialPhase(isSkipped: false);
	}

	internal static void AutoGeneratedStaticCollectObjectsFindHideoutTutorialQuest(object o, List<object> collectedObjects)
	{
		((FindHideoutTutorialQuest)o).AutoGeneratedInstanceCollectObjects(collectedObjects);
	}

	protected override void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects)
	{
		base.AutoGeneratedInstanceCollectObjects(collectedObjects);
		collectedObjects.Add(_hideout);
		collectedObjects.Add(_raiderParties);
	}

	internal static object AutoGeneratedGetMemberValue_hideout(object o)
	{
		return ((FindHideoutTutorialQuest)o)._hideout;
	}

	internal static object AutoGeneratedGetMemberValue_raiderParties(object o)
	{
		return ((FindHideoutTutorialQuest)o)._raiderParties;
	}

	internal static object AutoGeneratedGetMemberValue_talkedWithRadagos(object o)
	{
		return ((FindHideoutTutorialQuest)o)._talkedWithRadagos;
	}

	internal static object AutoGeneratedGetMemberValue_talkedWithBrother(object o)
	{
		return ((FindHideoutTutorialQuest)o)._talkedWithBrother;
	}

	internal static object AutoGeneratedGetMemberValue_hideoutBattleEndState(object o)
	{
		return ((FindHideoutTutorialQuest)o)._hideoutBattleEndState;
	}
}
