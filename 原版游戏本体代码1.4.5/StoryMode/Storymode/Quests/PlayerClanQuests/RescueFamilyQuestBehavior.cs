using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using SandBox.Missions.MissionLogics.Hideout;
using StoryMode.StoryModeObjects;
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
using TaleWorlds.CampaignSystem.SceneInformationPopupTypes;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ModuleManager;
using TaleWorlds.MountAndBlade;
using TaleWorlds.SaveSystem;
using TaleWorlds.SaveSystem.Load;

namespace StoryMode.Quests.PlayerClanQuests;

public class RescueFamilyQuestBehavior : CampaignBehaviorBase
{
	public class RescueFamilyQuest : StoryModeQuestBase
	{
		public class RebuildPlayerClanQuestBehaviorTypeDefiner : SaveableTypeDefiner
		{
			public RebuildPlayerClanQuestBehaviorTypeDefiner()
				: base(4140000)
			{
			}

			protected override void DefineClassTypes()
			{
				AddClassDefinition(typeof(RescueFamilyQuest), 1);
			}

			protected override void DefineEnumTypes()
			{
				AddEnumDefinition(typeof(RescueFamilyQuestStateEnum), 11);
			}
		}

		private enum RescueFamilyQuestStateEnum
		{
			None,
			ReunionTalkWithRadagosDone,
			HideoutTalkWithRadagosDone,
			HideoutBattleInProgress,
			ExecutionTalkWithGalterDone,
			ReunionTalkWithBrotherDone,
			GoodbyeTalkWithRadagosDone
		}

		private const int RaiderPartySize = 10;

		private const int RaiderPartyCount = 2;

		private const string RescueFamilyRaiderPartyStringId = "rescue_family_quest_raider_party_";

		private Hero _radagos;

		private Hero _hideoutBoss;

		private Settlement _targetSettlementForSiblings;

		[SaveableField(1)]
		private readonly Settlement _hideout;

		[SaveableField(7)]
		private readonly List<MobileParty> _raiderParties;

		[SaveableField(8)]
		private RescueFamilyQuestStateEnum _rescueFamilyQuestState;

		private TextObject _startQuestLogText
		{
			get
			{
				TextObject textObject = new TextObject("{=FyzsAZx8}{RADAGOS.LINK} said that he knows where your siblings are. He offered to attack together. He will wait for you at the hideout that he mentioned about near {SETTLEMENT_LINK}. You can see the hideout marked on the map.");
				StringHelpers.SetCharacterProperties("RADAGOS", _radagos.CharacterObject, textObject);
				Town town = SettlementHelper.FindNearestTownToSettlement(_hideout.SettlementComponent.Settlement, MobileParty.NavigationType.Default);
				textObject.SetTextVariable("SETTLEMENT_LINK", town.Settlement.EncyclopediaLinkWithName);
				return textObject;
			}
		}

		private TextObject _defeatedQuestLogText
		{
			get
			{
				TextObject textObject = new TextObject("{=Ga8mDgab}You've been defeated at {HIDEOUT_BOSS.LINK}'s hideout. You can attack again when you are ready.");
				StringHelpers.SetCharacterProperties("HIDEOUT_BOSS", _hideoutBoss.CharacterObject, textObject);
				return textObject;
			}
		}

		private TextObject _letGoRadagosEndQuestLogText
		{
			get
			{
				TextObject textObject = GameTexts.FindText("rescue_family_quest_let_go_radagos_quest_log");
				StringHelpers.SetCharacterProperties("RADAGOS", _radagos.CharacterObject, textObject);
				return textObject;
			}
		}

		private TextObject _executeRadagosEndQuestLogText
		{
			get
			{
				TextObject textObject = GameTexts.FindText("rescue_family_quest_execute_radagos_quest_log");
				StringHelpers.SetCharacterProperties("RADAGOS", _radagos.CharacterObject, textObject);
				return textObject;
			}
		}

		public override TextObject Title => new TextObject("{=HPNuqbSf}Rescue Your Family");

		public RescueFamilyQuest()
			: base("rescue_your_family_storymode_quest", null, CampaignTime.Never)
		{
			StoryModeManager.Current.MainStoryLine.FamilyRescued = true;
			_radagos = StoryModeHeroes.Radagos;
			_radagos.CharacterObject.SetTransferableInPartyScreen(isTransferable: false);
			_radagos.CharacterObject.SetTransferableInHideouts(isTransferable: false);
			_hideoutBoss = StoryModeHeroes.RadagosHenchman;
			_targetSettlementForSiblings = null;
			_hideout = SettlementHelper.FindNearestHideoutToMobileParty(MobileParty.MainParty, MobileParty.NavigationType.All, (Settlement s) => !s.IsSettlementBusy(this)).Settlement;
			_rescueFamilyQuestState = RescueFamilyQuestStateEnum.None;
			_raiderParties = new List<MobileParty>();
			InitializeHideout();
			AddTrackedObject(_hideout);
			SetDialogs();
			AddGameMenus();
		}

		[LoadInitializationCallback]
		private void OnLoad(MetaData metaData, ObjectLoadData objectLoadData)
		{
			if (objectLoadData.HasMember(2, objectLoadData.TypeDefinition.TypeLevel))
			{
				bool num = (bool)objectLoadData.GetMemberValueBySaveId(2, objectLoadData.TypeDefinition.TypeLevel);
				bool flag = (bool)objectLoadData.GetMemberValueBySaveId(3, objectLoadData.TypeDefinition.TypeLevel);
				bool flag2 = (bool)objectLoadData.GetMemberValueBySaveId(4, objectLoadData.TypeDefinition.TypeLevel);
				bool flag3 = (bool)objectLoadData.GetMemberValueBySaveId(5, objectLoadData.TypeDefinition.TypeLevel);
				if (num)
				{
					_rescueFamilyQuestState = RescueFamilyQuestStateEnum.ReunionTalkWithRadagosDone;
				}
				if (flag)
				{
					_rescueFamilyQuestState = RescueFamilyQuestStateEnum.HideoutTalkWithRadagosDone;
				}
				if (flag2)
				{
					_rescueFamilyQuestState = RescueFamilyQuestStateEnum.ReunionTalkWithBrotherDone;
				}
				if (flag3)
				{
					_rescueFamilyQuestState = RescueFamilyQuestStateEnum.GoodbyeTalkWithRadagosDone;
				}
			}
		}

		protected override void InitializeQuestOnGameLoad()
		{
			_radagos = StoryModeHeroes.Radagos;
			_radagos.CharacterObject.SetTransferableInPartyScreen(isTransferable: false);
			_radagos.CharacterObject.SetTransferableInHideouts(isTransferable: false);
			_hideoutBoss = StoryModeHeroes.RadagosHenchman;
			SetDialogs();
			AddGameMenus();
			SelectTargetSettlementForSiblings();
			if (MBSaveLoad.IsUpdatingGameVersion && MBSaveLoad.CurrentVersion.IsOlderThan(ApplicationVersion.FromString("v1.4.0")) && _rescueFamilyQuestState == RescueFamilyQuestStateEnum.HideoutTalkWithRadagosDone && PlayerEncounter.Battle?.MapEventSettlement == _hideout)
			{
				_rescueFamilyQuestState = RescueFamilyQuestStateEnum.HideoutBattleInProgress;
			}
		}

		public override void OnHeroCanHaveCampaignIssuesInfoIsRequested(Hero hero, ref bool result)
		{
			if (hero == StoryModeHeroes.Radagos && StoryModeManager.Current.MainStoryLine.TutorialPhase.IsCompleted && !StoryModeManager.Current.MainStoryLine.FamilyRescued)
			{
				result = false;
			}
		}

		protected override void OnCompleteWithSuccess()
		{
			StoryModeHeroes.ElderBrother.Clan = Clan.PlayerClan;
			StoryModeHeroes.LittleBrother.Clan = Clan.PlayerClan;
			StoryModeHeroes.ElderBrother.ChangeState(Hero.CharacterStates.Active);
			EnterSettlementAction.ApplyForCharacterOnly(StoryModeHeroes.ElderBrother, _targetSettlementForSiblings);
			if (StoryModeHeroes.LittleBrother.Age >= (float)Campaign.Current.Models.AgeModel.HeroComesOfAge)
			{
				StoryModeHeroes.LittleBrother.ChangeState(Hero.CharacterStates.Active);
				EnterSettlementAction.ApplyForCharacterOnly(StoryModeHeroes.LittleBrother, _targetSettlementForSiblings);
				StoryModeHelpers.SetPlayerSiblingsSkillsIfNeeded(StoryModeHeroes.LittleBrother);
			}
			else
			{
				StoryModeHeroes.LittleBrother.ChangeState(Hero.CharacterStates.NotSpawned);
			}
			StoryModeHeroes.ElderBrother.UpdateLastKnownClosestSettlement(_targetSettlementForSiblings);
			StoryModeHeroes.LittleBrother.UpdateLastKnownClosestSettlement(_targetSettlementForSiblings);
			TextObject textObject = new TextObject("{=PDlaPVIP}{PLAYER_LITTLE_BROTHER.NAME} is the little brother of {PLAYER.LINK}.");
			StringHelpers.SetCharacterProperties("PLAYER_LITTLE_BROTHER", StoryModeHeroes.LittleBrother.CharacterObject, textObject);
			StringHelpers.SetCharacterProperties("PLAYER", CharacterObject.PlayerCharacter, textObject);
			StoryModeHeroes.LittleBrother.EncyclopediaText = textObject;
			TextObject textObject2 = new TextObject("{=LcxfWLgd}{PLAYER_BROTHER.NAME} is the elder brother of {PLAYER.LINK}.");
			StringHelpers.SetCharacterProperties("PLAYER_BROTHER", StoryModeHeroes.ElderBrother.CharacterObject, textObject2);
			StringHelpers.SetCharacterProperties("PLAYER", CharacterObject.PlayerCharacter, textObject2);
			StoryModeHeroes.ElderBrother.EncyclopediaText = textObject2;
			ModuleInfo moduleInfo = ModuleHelper.GetModuleInfo("NavalDLC");
			if (moduleInfo == null || !moduleInfo.IsActive)
			{
				StoryModeHeroes.LittleSister.Clan = Clan.PlayerClan;
				if (StoryModeHeroes.LittleSister.Age >= (float)Campaign.Current.Models.AgeModel.HeroComesOfAge)
				{
					StoryModeHeroes.LittleSister.ChangeState(Hero.CharacterStates.Active);
					EnterSettlementAction.ApplyForCharacterOnly(StoryModeHeroes.LittleSister, _targetSettlementForSiblings);
					StoryModeHelpers.SetPlayerSiblingsSkillsIfNeeded(StoryModeHeroes.LittleSister);
				}
				else
				{
					StoryModeHeroes.LittleSister.ChangeState(Hero.CharacterStates.NotSpawned);
				}
				StoryModeHeroes.LittleSister.UpdateLastKnownClosestSettlement(_targetSettlementForSiblings);
				TextObject textObject3 = new TextObject("{=7XTkTi9B}{PLAYER_LITTLE_SISTER.NAME} is the little sister of {PLAYER.LINK}.");
				StringHelpers.SetCharacterProperties("PLAYER_LITTLE_SISTER", StoryModeHeroes.LittleSister.CharacterObject, textObject3);
				StringHelpers.SetCharacterProperties("PLAYER", CharacterObject.PlayerCharacter, textObject3);
				StoryModeHeroes.LittleSister.EncyclopediaText = textObject3;
			}
		}

		protected override void OnTimedOut()
		{
			base.OnTimedOut();
			KillCharacterAction.ApplyByRemove(StoryModeHeroes.LittleSister);
			KillCharacterAction.ApplyByRemove(StoryModeHeroes.LittleBrother);
			KillCharacterAction.ApplyByRemove(StoryModeHeroes.ElderBrother);
		}

		private void InitializeHideout()
		{
			CheckIfHideoutIsReady();
		}

		private void CheckIfHideoutIsReady()
		{
			if (!_hideout.Hideout.IsInfested)
			{
				for (int i = 0; i < 2; i++)
				{
					if (!_hideout.Hideout.IsInfested)
					{
						_raiderParties.Add(CreateRaiderParty(i, isBanditBossParty: false));
					}
				}
			}
			_hideout.Hideout.IsSpotted = true;
			_hideout.IsVisible = true;
		}

		private void AddRadagosHenchmanToHideout()
		{
			if (!_hideout.Parties.Any((MobileParty p) => p.IsBanditBossParty))
			{
				_raiderParties.Add(CreateRaiderParty(3, isBanditBossParty: true));
			}
			foreach (MobileParty party in _hideout.Parties)
			{
				if (!party.IsBanditBossParty)
				{
					continue;
				}
				if (party.MemberRoster.GetTroopRoster().Any((TroopRosterElement t) => t.Character == _hideout.Culture.BanditBoss))
				{
					TroopRosterElement troopRosterElement = party.MemberRoster.GetTroopRoster().First((TroopRosterElement t) => t.Character == _hideout.Culture.BanditBoss);
					party.MemberRoster.RemoveTroop(troopRosterElement.Character);
				}
				_hideoutBoss.ChangeState(Hero.CharacterStates.Active);
				if (_hideoutBoss.PartyBelongedTo == null)
				{
					party.MemberRoster.AddToCounts(_hideoutBoss.CharacterObject, 1, insertAtFront: true);
				}
				break;
			}
		}

		private MobileParty CreateRaiderParty(int number, bool isBanditBossParty)
		{
			Clan clan = _hideout.OwnerClan;
			if (clan.StringId.Equals("looters"))
			{
				clan = Clan.All.Where((Clan c) => c.IsBanditFaction && c.Culture == _hideout.Culture).GetRandomElementInefficiently();
			}
			MobileParty mobileParty = BanditPartyComponent.CreateBanditParty("rescue_family_quest_raider_party_" + number, clan, _hideout.Hideout, isBanditBossParty, null, _hideout.GatePosition);
			CharacterObject character = Campaign.Current.ObjectManager.GetObject<CharacterObject>(_hideout.Culture.StringId + "_bandit");
			mobileParty.MemberRoster.AddToCounts(character, 5);
			mobileParty.Party.SetCustomName(new TextObject("{=u1Pkt4HC}Raiders"));
			mobileParty.ActualClan = clan;
			mobileParty.Position = _hideout.Position;
			mobileParty.Party.SetVisualAsDirty();
			float num = mobileParty.Party.CalculateCurrentStrength();
			int initialGold = (int)(1f * MBRandom.RandomFloat * 20f * num + 50f);
			mobileParty.InitializePartyTrade(initialGold);
			mobileParty.SetMoveGoToSettlement(_hideout, MobileParty.NavigationType.Default, isTargetingThePort: false);
			mobileParty.Ai.SetDoNotMakeNewDecisions(doNotMakeNewDecisions: true);
			mobileParty.SetPartyUsedByQuest(isActivelyUsed: true);
			EnterSettlementAction.ApplyForParty(mobileParty, _hideout);
			return mobileParty;
		}

		private void SelectTargetSettlementForSiblings()
		{
			_targetSettlementForSiblings = SettlementHelper.FindNearestTownToMobileParty(MobileParty.MainParty, MobileParty.NavigationType.All, (Settlement s) => s.OwnerClan.MapFaction == Clan.PlayerClan.MapFaction)?.Settlement;
			if (_targetSettlementForSiblings == null)
			{
				_targetSettlementForSiblings = SettlementHelper.FindNearestTownToMobileParty(MobileParty.MainParty, MobileParty.NavigationType.All, (Settlement s) => !Clan.PlayerClan.MapFaction.IsAtWarWith(s.OwnerClan.MapFaction))?.Settlement;
			}
			if (_targetSettlementForSiblings == null)
			{
				_targetSettlementForSiblings = SettlementHelper.FindRandomSettlement((Settlement s) => s.IsTown);
			}
		}

		protected override void RegisterEvents()
		{
			CampaignEvents.OnSettlementLeftEvent.AddNonSerializedListener(this, OnSettlementLeft);
			CampaignEvents.GameMenuOpened.AddNonSerializedListener(this, OnGameMenuOpened);
			CampaignEvents.SettlementEntered.AddNonSerializedListener(this, OnSettlementEntered);
			CampaignEvents.HeroKilledEvent.AddNonSerializedListener(this, OnHeroKilled);
			CampaignEvents.IsSettlementBusyEvent.AddNonSerializedListener(this, IsSettlementBusy);
			CampaignEvents.MapEventStarted.AddNonSerializedListener(this, OnMapEventStarted);
			CampaignEvents.OnHideoutBattleCompletedEvent.AddNonSerializedListener(this, OnHideoutBattleCompleted);
			CampaignEvents.OnMissionStartedEvent.AddNonSerializedListener(this, OnMissionStarted);
		}

		private void IsSettlementBusy(Settlement settlement, object asker, ref int priority)
		{
			if (asker != this && settlement == _hideout)
			{
				priority = Math.Max(priority, 400);
			}
		}

		private void OnMapEventStarted(MapEvent mapEvent, PartyBase attackerParty, PartyBase defenderParty)
		{
			if (mapEvent.IsHideoutBattle && mapEvent.MapEventSettlement == _hideout && attackerParty == PartyBase.MainParty)
			{
				_rescueFamilyQuestState = RescueFamilyQuestStateEnum.HideoutBattleInProgress;
			}
		}

		private void OnHideoutBattleCompleted(BattleSideEnum winnerSide, HideoutEventComponent hideoutEventComponent, HideoutEventComponent.HideoutBattleEndState battleEndState)
		{
			Settlement mapEventSettlement = hideoutEventComponent.MapEvent.MapEventSettlement;
			if (mapEventSettlement != _hideout)
			{
				return;
			}
			MobileParty lastAttackerParty = mapEventSettlement.LastAttackerParty;
			if (lastAttackerParty == null || !lastAttackerParty.IsMainParty || _rescueFamilyQuestState != RescueFamilyQuestStateEnum.HideoutBattleInProgress)
			{
				return;
			}
			if ((uint)battleEndState > 2u && (uint)(battleEndState - 3) <= 1u)
			{
				CampaignMapConversation.OpenConversation(new ConversationCharacterData(CharacterObject.PlayerCharacter, null, noHorse: true, noWeapon: true), new ConversationCharacterData(StoryModeHeroes.RadagosHenchman.CharacterObject, null, noHorse: true, noWeapon: true));
				return;
			}
			if (!_hideoutBoss.IsHealthFull())
			{
				_hideoutBoss.Heal(_hideoutBoss.CharacterObject.MaxHitPoints());
			}
			AddLog(_defeatedQuestLogText);
			DisableHeroAction.Apply(_radagos);
			if (Hero.MainHero.IsPrisoner && _raiderParties.Contains(Hero.MainHero.PartyBelongedToAsPrisoner.MobileParty))
			{
				EndCaptivityAction.ApplyByPeace(Hero.MainHero);
				InformationManager.ShowInquiry(new InquiryData(new TextObject("{=FPhWhjq7}Defeated").ToString(), new TextObject("{=WN6aHR6m}You were defeated by the bandits in the hideout but you managed to escape. You need to wait a while before attacking again.").ToString(), isAffirmativeOptionShown: true, isNegativeOptionShown: false, new TextObject("{=yQtzabbe}Close").ToString(), null, null, null));
			}
			if (_hideout.Parties.Count == 0)
			{
				InitializeHideout();
			}
			_hideout.Hideout.SetNextPossibleAttackTime(StoryModeData.StorylineQuestHideoutHiddenDuration);
		}

		private void OnMissionStarted(IMission mission)
		{
			if (Settlement.CurrentSettlement != _hideout || PlayerEncounter.Current == null)
			{
				return;
			}
			Mission mission2 = (Mission)mission;
			HideoutAmbushMissionController missionBehavior = mission2.GetMissionBehavior<HideoutAmbushMissionController>();
			if (missionBehavior != null)
			{
				missionBehavior.SetOverriddenHideoutBossCharacterObject(_hideoutBoss.CharacterObject);
				return;
			}
			HideoutMissionController missionBehavior2 = mission2.GetMissionBehavior<HideoutMissionController>();
			if (missionBehavior2 != null)
			{
				missionBehavior2.SetOverriddenHideoutBossCharacterObject(_hideoutBoss.CharacterObject);
			}
			else
			{
				Debug.FailedAssert("Hideout boss can not be set!", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\StoryMode\\Quests\\PlayerClanQuests\\RescueFamilyQuestBehavior.cs", "OnMissionStarted", 542);
			}
		}

		private void OnHeroKilled(Hero victim, Hero killer, KillCharacterAction.KillCharacterActionDetail detail, bool showNotification = true)
		{
			if (killer == _radagos && victim == _hideoutBoss)
			{
				if (Campaign.Current.CurrentMenuContext != null)
				{
					Campaign.Current.CurrentMenuContext.SwitchToMenu("radagos_goodbye_menu");
				}
				else
				{
					GameMenu.ActivateGameMenu("radagos_goodbye_menu");
				}
			}
		}

		private void OnSettlementLeft(MobileParty party, Settlement settlement)
		{
			if (party.IsMainParty)
			{
				if (base.IsTrackEnabled && _rescueFamilyQuestState > RescueFamilyQuestStateEnum.None && !IsTracked(_hideout))
				{
					AddTrackedObject(_hideout);
				}
				if (settlement == _hideout && PartyBase.MainParty.MemberRoster.Contains(_radagos.CharacterObject))
				{
					PartyBase.MainParty.MemberRoster.RemoveTroop(_radagos.CharacterObject);
				}
			}
		}

		private void OnGameMenuOpened(MenuCallbackArgs args)
		{
			if (GameStateManager.Current?.ActiveState is MapState)
			{
				if (_rescueFamilyQuestState < RescueFamilyQuestStateEnum.HideoutTalkWithRadagosDone && Settlement.CurrentSettlement != null && Settlement.CurrentSettlement == _hideout)
				{
					CampaignMapConversation.OpenConversation(new ConversationCharacterData(CharacterObject.PlayerCharacter, null, noHorse: true, noWeapon: true), new ConversationCharacterData(StoryModeHeroes.Radagos.CharacterObject, null, noHorse: true, noWeapon: true));
				}
				else if (_rescueFamilyQuestState == RescueFamilyQuestStateEnum.GoodbyeTalkWithRadagosDone && args.MenuContext.GameMenu.StringId == "radagos_goodbye_menu")
				{
					GameMenu.ExitToLast();
					CompleteQuestWithSuccess();
				}
			}
		}

		private void OnSettlementEntered(MobileParty mobileParty, Settlement settlement, Hero hero)
		{
			if (_rescueFamilyQuestState < RescueFamilyQuestStateEnum.HideoutTalkWithRadagosDone || settlement != _hideout || mobileParty == null || !mobileParty.IsMainParty)
			{
				return;
			}
			if (!PartyBase.MainParty.MemberRoster.Contains(_radagos.CharacterObject))
			{
				if (_radagos.HeroState != Hero.CharacterStates.Active)
				{
					_radagos.ChangeState(Hero.CharacterStates.Active);
				}
				PartyBase.MainParty.MemberRoster.AddToCounts(_radagos.CharacterObject, 1);
			}
			AddRadagosHenchmanToHideout();
		}

		protected override void HourlyTick()
		{
			CheckIfHideoutIsReady();
		}

		protected override void SetDialogs()
		{
			Campaign.Current.ConversationManager.AddDialogFlow(DialogFlow.CreateDialogFlow("start", 160).NpcLine(new TextObject("{=1yi00v5w}{PLAYER.NAME}! Good to see you. Believe it or not, I mean that. I've been looking for you...[if:convo_calm_friendly][ib:normal2]")).Condition(radagos_reunion_conversation_condition)
				.PlayerLine(new TextObject("{=pCNSEPEP}You escaped? Where's my brother? What happened?"))
				.NpcLine(new TextObject("{=xknCpvcb}Calm down, now. I'll tell you everything.[ib:closed2][if:convo_grave]"))
				.NpcLine(GameTexts.FindText("rescue_family_quest_radagos_conversation_line_1"))
				.NpcLine(new TextObject("{=UpUqL368}What scum, eh? Even in this profession, double-crossing your comrades is frowned upon."))
				.NpcLine(new TextObject("{=bJjAqCxk}I escaped - one of his men, a little guiltier than the rest, cut my bonds when the others were sleeping - but I can't let a traitor live. So I decided to find you and offer you a deal.[if:convo_focused_voice][ib:hip]"))
				.NpcLine(new TextObject("{=PlpNTQqf}I know where {HIDEOUT_BOSS.LINK} is now. If you agree, we can attack together and save your kin."))
				.NpcLine(new TextObject("{=mmQRCHUM}But in return, I will have the pleasure of killing that bastard. So what do you say?[if:convo_snide_voice][ib:confident2]"))
				.PlayerLine(new TextObject("{=ypDmy5Rn}Uh, how can we possibly trust each other?"))
				.NpcLine(new TextObject("{=VbJvL8yB}Oh you can't trust me. But you need me, and I figure you have enough men that you could easily slit my throat pretty quickly if I lead you into a trap. And I don't need to trust you - you're my vehicle of revenge, not my partner.[if:convo_grave]"))
				.PlayerLine(new TextObject("{=ft6zzDrJ}I can live with that. Let's go."))
				.NpcLine(new TextObject("{=HT9hW29s}Splendid! But I have a few things to do. There is a hideout near this city. {HIDEOUT_BOSS.LINK} keeps your siblings there. I will join you right where the path leads up, just out of sight of their scouts.[if:convo_snide_voice][ib:hip]"))
				.PlayerLine(new TextObject("{=GicEcLx2}See you there then. But, remember, if this is a trap or something, that will cost you your life."))
				.NpcLine(new TextObject("{=8b4Ndfep}Oh of course. I have no doubts on that score.[if:convo_nonchalant]"))
				.Consequence(radagos_reunion_conversation_consequence)
				.CloseDialog(), this);
			Campaign.Current.ConversationManager.AddDialogFlow(DialogFlow.CreateDialogFlow("start", 160).NpcLine(new TextObject("{=rDuegB1L}You've finally arrived! I have a few things to say before we attack.[ib:confident2][if:convo_nonchalant]")).Condition(radagos_hideout_conversation_condition)
				.NpcLine(new TextObject("{=1T7p0O7B}We have to be clever. {HIDEOUT_BOSS.LINK} is a cunning fellow, in a low and base kind of way.[if:convo_normal]"))
				.PlayerLine(new TextObject("{=a29lmPLd}I defeated you before. I know how your gang operates. Less talking, more raiding. C'mon..."))
				.NpcLine(new TextObject("{=QbsDYITB}That you did, that you did. Lead on, then.[ib:closed2][if:convo_calm_friendly]"))
				.Consequence(radagos_hideout_conversation_consequence)
				.CloseDialog(), this);
			Campaign.Current.ConversationManager.AddDialogFlow(DialogFlow.CreateDialogFlow("start", 160).NpcLine(new TextObject("{=PiKISvfu}{PLAYER.NAME}! I knew you'd come. Great Heaven. Damn, {?PLAYER.GENDER}sister{?}brother{\\?}, nothing can stop you! I love you, {?PLAYER.GENDER}sister{?}brother{\\?}.[if:convo_calm_friendly][ib:aggressive2]")).Condition(brother_hideout_conversation_condition)
				.PlayerLine(new TextObject("{=DIKPGwj1}So glad to see you safe. Is everyone okay?"))
				.NpcLine(GameTexts.FindText("rescue_family_quest_brother_conversation_line_1"))
				.NpcLine(GameTexts.FindText("rescue_family_quest_brother_conversation_line_2"))
				.NpcLine(new TextObject("{=IC9Vg5MA}Meet me there later, when you're ready to tell me everything.[if:convo_normal][ib:normal2]"))
				.PlayerLine(new TextObject("{=LrItHItu}Okay brother, be careful. Take care."))
				.Consequence(delegate
				{
					Campaign.Current.ConversationManager.ConversationEndOneShot += brother_hideout_conversation_consequence;
				})
				.CloseDialog(), this);
			Campaign.Current.ConversationManager.AddDialogFlow(DialogFlow.CreateDialogFlow("start", 1000015).NpcLine(new TextObject("{=0I9siaQY}Bastards... You're the kin of my captives, right? I saw {RADAGOS.LINK} with you. You know he can't be trusted?[if:convo_confused_annoyed][ib:aggressive]")).Condition(bandit_hideout_boss_fight_start_on_condition)
				.PlayerLine(GameTexts.FindText("rescue_family_quest_galter_conversation_player_line_1"))
				.NpcLine(new TextObject("{=heoCaRIr}Nah... There's no more talking. Kill me or I kill you, that's how this ends.[ib:warrior][if:convo_bared_teeth]"))
				.NpcLine(new TextObject("{=2GeiKTlS}I'll do you the honor of duelling you, and my men will stand down if you win.[if:convo_predatory]"))
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
			Campaign.Current.ConversationManager.AddDialogFlow(DialogFlow.CreateDialogFlow("start", 1000015).NpcLine(new TextObject("{=G9iXmhGK}Look, we can still talk. I'll give you a pouch of silver.[ib:weary][if:convo_confused_voice]")).Condition(hideout_boss_prisoner_talk_condition)
				.PlayerLine(new TextObject("{=fM4eSVps}You said talking was a waste of time. You are {RADAGOS.NAME}'s property, now."))
				.Consequence(delegate
				{
					_rescueFamilyQuestState = RescueFamilyQuestStateEnum.ExecutionTalkWithGalterDone;
					hideout_boss_prisoner_talk_consequence();
				})
				.CloseDialog(), this);
			Campaign.Current.ConversationManager.AddDialogFlow(DialogFlow.CreateDialogFlow("start", 1000015).NpcLine(GameTexts.FindText("rescue_family_quest_radagos_goodbye_conversation_line_1")).Condition(goodbye_conversation_with_radagos_condition)
				.GetOutputToken(out var oState)
				.NpcLine(new TextObject("{=C79Xxm1b}Don't let your conscience bother you about letting me go, by the way. I won't get back into slaving. Burned too many bridges with my old colleagues, you might say. I'll find some other way to earn my keep - mercenary work, perhaps. Anyway, maybe our paths will cross again.[if:convo_empathic_voice]"))
				.BeginPlayerOptions()
				.PlayerOption(new TextObject("{=c1Q2irLi}Your men killed my parents. Did you really think you would not be punished?"))
				.NpcLine(new TextObject("{=W7hi7jS4}Eh, well, I dared to hope, I suppose. All right then, I'm not going to grovel to you, so get it over with.[ib:hip][if:convo_uncomfortable_voice]"))
				.BeginPlayerOptions()
				.PlayerOption(new TextObject("{=kz5PJbV1}I shall. For your many crimes, {RADAGOS.NAME}, your life is forfeit."))
				.Consequence(delegate
				{
					Campaign.Current.ConversationManager.ConversationEndOneShot += execute_radagos_consequence;
				})
				.CloseDialog()
				.PlayerOption(GameTexts.FindText("rescue_family_quest_radagos_goodbye_conversation_player_line_1"))
				.GotoDialogState(oState)
				.EndPlayerOptions()
				.PlayerOption(new TextObject("{=RefpTQpr}Maybe. Goodbye, {RADAGOS.NAME}..."))
				.Consequence(delegate
				{
					Campaign.Current.ConversationManager.ConversationEndOneShot += let_go_radagos_consequence;
				})
				.CloseDialog()
				.EndPlayerOptions()
				.CloseDialog(), this);
		}

		private bool radagos_reunion_conversation_condition()
		{
			StringHelpers.SetCharacterProperties("PLAYER", CharacterObject.PlayerCharacter);
			StringHelpers.SetCharacterProperties("HIDEOUT_BOSS", _hideoutBoss.CharacterObject);
			if (_rescueFamilyQuestState == RescueFamilyQuestStateEnum.None)
			{
				return Hero.OneToOneConversationHero == _radagos;
			}
			return false;
		}

		private void radagos_reunion_conversation_consequence()
		{
			_rescueFamilyQuestState = RescueFamilyQuestStateEnum.ReunionTalkWithRadagosDone;
			AddLog(_startQuestLogText);
		}

		private bool radagos_hideout_conversation_condition()
		{
			StringHelpers.SetCharacterProperties("HIDEOUT_BOSS", _hideoutBoss.CharacterObject);
			if (_rescueFamilyQuestState < RescueFamilyQuestStateEnum.HideoutTalkWithRadagosDone && Settlement.CurrentSettlement == _hideout)
			{
				return Hero.OneToOneConversationHero == _radagos;
			}
			return false;
		}

		private void radagos_hideout_conversation_consequence()
		{
			_rescueFamilyQuestState = RescueFamilyQuestStateEnum.HideoutTalkWithRadagosDone;
			if (!PartyBase.MainParty.MemberRoster.Contains(_radagos.CharacterObject))
			{
				if (_radagos.HeroState != Hero.CharacterStates.Active)
				{
					_radagos.ChangeState(Hero.CharacterStates.Active);
				}
				PartyBase.MainParty.MemberRoster.AddToCounts(_radagos.CharacterObject, 1);
			}
			AddRadagosHenchmanToHideout();
		}

		private bool brother_hideout_conversation_condition()
		{
			if (_rescueFamilyQuestState < RescueFamilyQuestStateEnum.ReunionTalkWithBrotherDone && Hero.OneToOneConversationHero == StoryModeHeroes.ElderBrother)
			{
				SelectTargetSettlementForSiblings();
				StringHelpers.SetCharacterProperties("PLAYER", CharacterObject.PlayerCharacter);
				StringHelpers.SetCharacterProperties("LITTLE_SISTER", StoryModeHeroes.LittleSister.CharacterObject);
				StringHelpers.SetCharacterProperties("LITTLE_BROTHER", StoryModeHeroes.LittleBrother.CharacterObject);
				MBTextManager.SetTextVariable("SETTLEMENT_LINK", _targetSettlementForSiblings.EncyclopediaLinkWithName);
				Campaign.Current.ConversationManager.ConversationEndOneShot += delegate
				{
					if (Campaign.Current.CurrentMenuContext != null)
					{
						Campaign.Current.CurrentMenuContext.SwitchToMenu("radagos_goodbye_menu");
					}
					else
					{
						GameMenu.ActivateGameMenu("radagos_goodbye_menu");
					}
				};
				return true;
			}
			return false;
		}

		private void brother_hideout_conversation_consequence()
		{
			_rescueFamilyQuestState = RescueFamilyQuestStateEnum.ReunionTalkWithBrotherDone;
		}

		private bool bandit_hideout_boss_fight_start_on_condition()
		{
			PartyBase encounteredParty = PlayerEncounter.EncounteredParty;
			if (encounteredParty == null || encounteredParty.IsMobile || encounteredParty.MapFaction == null || !encounteredParty.MapFaction.IsBanditFaction)
			{
				return false;
			}
			StringHelpers.SetCharacterProperties("RADAGOS", _radagos.CharacterObject);
			if (encounteredParty.IsSettlement && encounteredParty.Settlement.IsHideout && encounteredParty.Settlement == _hideout && Mission.Current != null && Hero.OneToOneConversationHero != null && Hero.OneToOneConversationHero == _hideoutBoss)
			{
				if (Mission.Current.GetMissionBehavior<HideoutAmbushMissionController>() == null)
				{
					return Mission.Current.GetMissionBehavior<HideoutMissionController>() != null;
				}
				return true;
			}
			return false;
		}

		private void bandit_hideout_start_duel_fight_on_consequence()
		{
			if (Mission.Current.GetMissionBehavior<HideoutAmbushMissionController>() != null)
			{
				Campaign.Current.ConversationManager.ConversationEndOneShot += HideoutAmbushMissionController.StartBossFightDuelMode;
			}
			else
			{
				Campaign.Current.ConversationManager.ConversationEndOneShot += HideoutMissionController.StartBossFightDuelMode;
			}
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
			if (Mission.Current.GetMissionBehavior<HideoutAmbushMissionController>() != null)
			{
				Campaign.Current.ConversationManager.ConversationEndOneShot += HideoutAmbushMissionController.StartBossFightBattleMode;
			}
			else
			{
				Campaign.Current.ConversationManager.ConversationEndOneShot += HideoutMissionController.StartBossFightBattleMode;
			}
		}

		private bool hideout_boss_prisoner_talk_condition()
		{
			StringHelpers.SetCharacterProperties("RADAGOS", _radagos.CharacterObject);
			return Hero.OneToOneConversationHero == _hideoutBoss;
		}

		private void hideout_boss_prisoner_talk_consequence()
		{
			MBInformationManager.ShowSceneNotification(HeroExecutionSceneNotificationData.CreateForInformingPlayer(_radagos, _hideoutBoss, SceneNotificationData.RelevantContextType.Map, OnGalterExecutionIsDone));
			Campaign.Current.ConversationManager.ConversationEndOneShot += delegate
			{
				if (Campaign.Current.CurrentMenuContext != null)
				{
					Campaign.Current.CurrentMenuContext.SwitchToMenu("radagos_goodbye_menu");
				}
				else
				{
					GameMenu.ActivateGameMenu("radagos_goodbye_menu");
				}
			};
		}

		private void OnGalterExecutionIsDone()
		{
			if (_rescueFamilyQuestState == RescueFamilyQuestStateEnum.ExecutionTalkWithGalterDone && !Campaign.Current.ConversationManager.IsConversationInProgress)
			{
				CampaignMapConversation.OpenConversation(new ConversationCharacterData(CharacterObject.PlayerCharacter, null, noHorse: true, noWeapon: true), new ConversationCharacterData(StoryModeHeroes.ElderBrother.CharacterObject, null, noHorse: true, noWeapon: true));
			}
		}

		private bool goodbye_conversation_with_radagos_condition()
		{
			if (_rescueFamilyQuestState == RescueFamilyQuestStateEnum.ReunionTalkWithBrotherDone && Hero.OneToOneConversationHero == _radagos)
			{
				StringHelpers.SetCharacterProperties("PLAYER", CharacterObject.PlayerCharacter);
				StringHelpers.SetCharacterProperties("RADAGOS", _radagos.CharacterObject);
				return true;
			}
			return false;
		}

		private void execute_radagos_consequence()
		{
			AddLog(_executeRadagosEndQuestLogText);
			MBInformationManager.ShowSceneNotification(HeroExecutionSceneNotificationData.CreateForInformingPlayer(Hero.MainHero, _radagos, SceneNotificationData.RelevantContextType.Map));
			_rescueFamilyQuestState = RescueFamilyQuestStateEnum.GoodbyeTalkWithRadagosDone;
		}

		private void let_go_radagos_consequence()
		{
			AddLog(_letGoRadagosEndQuestLogText);
			DisableHeroAction.Apply(_radagos);
			_rescueFamilyQuestState = RescueFamilyQuestStateEnum.GoodbyeTalkWithRadagosDone;
		}

		private void AddGameMenus()
		{
			TextObject textObject = new TextObject("{=kzgbBrYo}As you leave the hideout, {RADAGOS.LINK} comes to you and asks to talk.");
			StringHelpers.SetCharacterProperties("RADAGOS", _radagos.CharacterObject, textObject);
			AddGameMenu("radagos_goodbye_menu", textObject, radagos_goodbye_menu_on_init);
			AddGameMenuOption("radagos_goodbye_menu", "radagos_goodbye_menu_continue", new TextObject("{=DM6luo3c}Continue"), radagos_goodbye_menu_continue_on_condition, radagos_goodbye_menu_continue_on_consequence);
		}

		private void radagos_goodbye_menu_on_init(MenuCallbackArgs args)
		{
		}

		private bool radagos_goodbye_menu_continue_on_condition(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Continue;
			return true;
		}

		private void radagos_goodbye_menu_continue_on_consequence(MenuCallbackArgs args)
		{
			CampaignMapConversation.OpenConversation(new ConversationCharacterData(CharacterObject.PlayerCharacter, null, noHorse: true, noWeapon: true), new ConversationCharacterData(_radagos.CharacterObject, null, noHorse: true, noWeapon: true));
		}

		[GameMenuInitializationHandler("radagos_goodbye_menu")]
		private static void quest_game_menus_on_init_background(MenuCallbackArgs args)
		{
			args.MenuContext.SetBackgroundMeshName(SettlementHelper.FindNearestHideoutToMobileParty(MobileParty.MainParty, MobileParty.NavigationType.All).WaitMeshName);
		}

		internal static void AutoGeneratedStaticCollectObjectsRescueFamilyQuest(object o, List<object> collectedObjects)
		{
			((RescueFamilyQuest)o).AutoGeneratedInstanceCollectObjects(collectedObjects);
		}

		protected override void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects)
		{
			base.AutoGeneratedInstanceCollectObjects(collectedObjects);
			collectedObjects.Add(_hideout);
			collectedObjects.Add(_raiderParties);
		}

		internal static object AutoGeneratedGetMemberValue_hideout(object o)
		{
			return ((RescueFamilyQuest)o)._hideout;
		}

		internal static object AutoGeneratedGetMemberValue_raiderParties(object o)
		{
			return ((RescueFamilyQuest)o)._raiderParties;
		}

		internal static object AutoGeneratedGetMemberValue_rescueFamilyQuestState(object o)
		{
			return ((RescueFamilyQuest)o)._rescueFamilyQuestState;
		}
	}

	private bool _rescueFamilyQuestReadyToStart;

	internal RescueFamilyQuestBehavior()
	{
		_rescueFamilyQuestReadyToStart = false;
	}

	public override void RegisterEvents()
	{
		CampaignEvents.OnGameLoadedEvent.AddNonSerializedListener(this, OnGameLoadedEvent);
		CampaignEvents.SettlementEntered.AddNonSerializedListener(this, OnSettlementEntered);
		CampaignEvents.OnQuestCompletedEvent.AddNonSerializedListener(this, OnQuestCompleted);
		CampaignEvents.CanHaveCampaignIssuesEvent.AddNonSerializedListener(this, CanHaveCampaignIssuesInfoIsRequested);
		CampaignEvents.CanHeroDieEvent.AddNonSerializedListener(this, CanHeroDie);
	}

	public override void SyncData(IDataStore dataStore)
	{
		dataStore.SyncData("_rescueFamilyQuestReadyToStart", ref _rescueFamilyQuestReadyToStart);
	}

	private static void OnGameLoadedEvent(CampaignGameStarter campaignGameStarter)
	{
	}

	private void OnSettlementEntered(MobileParty party, Settlement settlement, Hero hero)
	{
		if (!_rescueFamilyQuestReadyToStart || party != MobileParty.MainParty || !settlement.IsTown || settlement.MapFaction.IsAtWarWith(Hero.MainHero.MapFaction) || !(GameStateManager.Current.ActiveState is MapState) || Campaign.Current.ConversationManager.IsConversationFlowActive)
		{
			return;
		}
		bool flag = false;
		foreach (QuestBase quest in Campaign.Current.QuestManager.Quests)
		{
			if (quest.QuestGiver?.CurrentSettlement == settlement)
			{
				flag = true;
				break;
			}
		}
		if (!flag)
		{
			new RescueFamilyQuest().StartQuest();
			_rescueFamilyQuestReadyToStart = false;
			StoryModeHeroes.Radagos.UpdateLastKnownClosestSettlement(Settlement.CurrentSettlement);
			CampaignMapConversation.OpenConversation(new ConversationCharacterData(CharacterObject.PlayerCharacter, null, noHorse: true, noWeapon: true), new ConversationCharacterData(StoryModeHeroes.Radagos.CharacterObject, null, noHorse: true, noWeapon: true));
		}
	}

	private void OnQuestCompleted(QuestBase quest, QuestBase.QuestCompleteDetails detail)
	{
		if (quest is RebuildPlayerClanQuest)
		{
			_rescueFamilyQuestReadyToStart = true;
		}
		else if (quest is RescueFamilyQuest)
		{
			_rescueFamilyQuestReadyToStart = false;
			StoryModeHeroes.Radagos.CharacterObject.SetTransferableInPartyScreen(isTransferable: true);
			StoryModeHeroes.Radagos.CharacterObject.SetTransferableInHideouts(isTransferable: true);
		}
	}

	private void CanHaveCampaignIssuesInfoIsRequested(Hero hero, ref bool result)
	{
		if (!StoryModeManager.Current.MainStoryLine.FamilyRescued && (hero == StoryModeHeroes.Radagos || hero == StoryModeHeroes.RadagosHenchman))
		{
			result = false;
		}
	}

	private void CanHeroDie(Hero hero, KillCharacterAction.KillCharacterActionDetail causeOfDeath, ref bool result)
	{
		if (hero == StoryModeHeroes.RadagosHenchman && (!StoryModeManager.Current.MainStoryLine.FamilyRescued || _rescueFamilyQuestReadyToStart || (Campaign.Current.QuestManager.IsThereActiveQuestWithType(typeof(RescueFamilyQuest)) && causeOfDeath != KillCharacterAction.KillCharacterActionDetail.Executed && causeOfDeath != KillCharacterAction.KillCharacterActionDetail.ExecutionAfterMapEvent)))
		{
			result = false;
		}
	}
}
