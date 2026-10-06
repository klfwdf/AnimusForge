using System;
using System.Collections.Generic;
using Helpers;
using NavalDLC.Storyline.Quests;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace NavalDLC.Storyline.CampaignBehaviors;

public class NavalStorylineThirdActSecondQuestBehavior : CampaignBehaviorBase
{
	private const string _questConversationMenuId = "naval_storyline_act_3_quest_2_conversation_menu";

	private bool _isQuestAcceptedThroughMission;

	public override void RegisterEvents()
	{
		if (!NavalStorylineData.IsNavalStorylineCanceled())
		{
			CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
			NavalDLCEvents.OnNavalStorylineCanceledEvent.AddNonSerializedListener(this, OnNavalStorylineCanceled);
		}
	}

	private void OnNavalStorylineCanceled(NavalStorylineData.StorylineCancelDetail detail)
	{
		CampaignEventDispatcher.Instance.RemoveListeners(this);
	}

	private void OnSessionLaunched(CampaignGameStarter starter)
	{
		AddGameMenus(starter);
		AddDialogs(starter);
	}

	private void AddGameMenus(CampaignGameStarter starter)
	{
		starter.AddGameMenu("naval_storyline_act_3_quest_2_conversation_menu", string.Empty, naval_storyline_act_3_quest_2_conversation_menu_on_init);
	}

	private void naval_storyline_act_3_quest_2_conversation_menu_on_init(MenuCallbackArgs args)
	{
		if (_isQuestAcceptedThroughMission && Mission.Current == null)
		{
			StartQuest();
			_isQuestAcceptedThroughMission = false;
		}
	}

	private void AddDialogs(CampaignGameStarter starter)
	{
		TextObject textObject = new TextObject("{=TlgUi5Sh}[if:convo_merry][ib:confident2]{PLAYER.NAME}... Word spreads fast among sailors. We seem to have made a bit of a name for ourselves with that victory off of Hvalvik. I have someone for you to meet.");
		textObject.SetCharacterProperties("PLAYER", CharacterObject.PlayerCharacter);
		TextObject npcText = new TextObject("{=AGY68GQE}[ib:confident][if:convo_approving]So… You are the captain who thrashed those so-called Sea Hounds up north. I have a proposal that I hope would be of interest.");
		TextObject textObject2 = new TextObject("{=pUZTxrEy}[ib:closed]I am Lahar, of Quyaz, on the Jade Sea. I am here because one of the great families of our city has been having some troubles. The head of one branch, the lady Fahda, has quarreled over her inheritance with her uncles. The elders of the town backed the uncles, so she took to the sea with her retainers and vowed to ravage their shipping.");
		textObject2.SetTextVariable("SETTLEMENT_LINK", NavalStorylineData.Act3Quest2TargetSettlement.EncyclopediaLinkWithName);
		TextObject text = new TextObject("{=MM0mXw6o}How formidable a foe is this Fahda?");
		TextObject textObject3 = new TextObject("{=x3EgmkF8}[if:convo_pondering]The lady is good at her craft. Fahda has been sailing since she was a child. She always wears a sailor’s cap, and underneath she is as bald as an egg. She persuaded her late father to take her to sea, so the story goes, by cutting off all of her long shining hair lest it catch in the rigging. She has taken several Quyazi ships, and I would be reluctant to fight her alone.");
		textObject3.SetTextVariable("SETTLEMENT_LINK", NavalStorylineData.Act3Quest2TargetSettlement.EncyclopediaLinkWithName);
		TextObject text2 = new TextObject("{=s7CSGwZ5}What does this have to do with our quarrel with the Sea Hounds?");
		TextObject npcText2 = new TextObject("{=JBOE2x1a}[ib:demure][if:convo_evil_smile]The lady Fahda has reportedly joined up with these Sea Hounds, as pirates often band together. She has been prowling about the Gulf of Charas, taking Quyazi vessels. You wish to continue hunting Sea Hounds, do you not? Those would be good waters in which to hunt, and if you are going there, I would like to come with you and lend my assistance.");
		TextObject npcText3 = new TextObject("{=pUZPt8Po}[ib:closed][if:convo_thinking]Fahda also traffics in captives with the Sea Hounds. She may have bought or held your sister at some point, or if not, at least she may be able to tell us more about the Sea Hounds' trade in slaves.");
		TextObject text3 = new TextObject("{=TUmPKK8P}Lahar - what will we gain by helping you catch her?");
		TextObject npcText4 = new TextObject("{=fbKlKR0v}If you wish to weaken these Sea Hounds, you may want to strike at their allies first. And of course the elders of Quyaz will be most happy to pay a handsome reward, of which you and Gunnar would receive your fair share.");
		TextObject text4 = new TextObject("{=jo3s90PF}What will you bring on our hunt?");
		TextObject npcText5 = new TextObject("{=w9ar5Ldc}[ib:hip][if:convo_approving]I have my loyal crew and a swift liburna, outfitted with a ram, which I think you might put to good purpose. It would be especially useful if we encounter any slow but powerful ships that would be costly to take by boarding.");
		TextObject text5 = new TextObject("{=jSaUTBbW}I am ready to set out.");
		TextObject text6 = new TextObject("{=ZUAvYPpg}That sounds promising, but I am not yet ready to depart.");
		TextObject npcText6 = new TextObject("{=8T2uf1ay}Can I tell Lahar that we are ready to sail? The tide and winds are with us, and it would be a pity if someone else were to hunt down Fahda and claim the bounty.");
		TextObject text7 = new TextObject("{=hcm7PZLK}Order the men to their ships. We sail at once.");
		TextObject text8 = new TextObject("{=vxLowgvR}I am not quite ready. Let us pray that the good winds last a little longer.");
		TextObject npcText7 = new TextObject("{=OSZozYIR}Talk with Gunnar when you're ready to depart.");
		string token;
		string token2;
		string token3;
		string token4;
		string token5;
		string token6;
		string token7;
		DialogFlow dialogFlow = DialogFlow.CreateDialogFlow("start", 1200).GenerateToken(out token).GenerateToken(out token2)
			.GenerateToken(out token3)
			.GenerateToken(out token4)
			.GenerateToken(out token5)
			.GenerateToken(out token6)
			.GenerateToken(out token7)
			.NpcLine(textObject, IsGunnar, IsMainHero)
			.Condition(MultiAgentConversationCondition)
			.NpcLine(npcText, IsLahar, IsMainHero)
			.NpcLine(textObject2, IsLahar, IsMainHero)
			.GotoDialogState(token)
			.BeginPlayerOptions(token)
			.PlayerOption(text, IsLahar)
			.NpcLine(textObject3, IsLahar, IsMainHero)
			.GotoDialogState(token3)
			.PlayerOption(text2, IsLahar)
			.NpcLine(npcText2, IsLahar, IsMainHero)
			.GotoDialogState(token2)
			.EndPlayerOptions()
			.BeginPlayerOptions(token2)
			.PlayerOption(text, IsLahar)
			.NpcLine(textObject3, IsLahar, IsMainHero)
			.NpcLine(npcText3, IsGunnar, IsMainHero)
			.GotoDialogState(token4)
			.EndPlayerOptions()
			.BeginPlayerOptions(token3)
			.PlayerOption(text2, IsLahar)
			.NpcLine(npcText2, IsLahar, IsMainHero)
			.NpcLine(npcText3, IsGunnar, IsMainHero)
			.GotoDialogState(token4)
			.EndPlayerOptions()
			.BeginPlayerOptions(token4)
			.PlayerOption(text3, IsLahar)
			.NpcLine(npcText4, IsLahar, IsMainHero)
			.GotoDialogState(token6)
			.PlayerOption(text4, IsLahar)
			.NpcLine(npcText5, IsLahar, IsMainHero)
			.GotoDialogState(token5)
			.EndPlayerOptions()
			.BeginPlayerOptions(token5)
			.PlayerOption(text3, IsLahar)
			.NpcLine(npcText4, IsLahar, IsMainHero)
			.GotoDialogState(token7)
			.EndPlayerOptions()
			.BeginPlayerOptions(token6)
			.PlayerOption(text4, IsLahar)
			.NpcLine(npcText5, IsLahar, IsMainHero)
			.GotoDialogState(token7)
			.EndPlayerOptions()
			.BeginPlayerOptions(token7)
			.PlayerOption(text5, IsGunnar)
			.Consequence(delegate
			{
				Campaign.Current.ConversationManager.ConversationEndOneShot += delegate
				{
					NavalStorylineData.OnPlayerAcceptsQuest(QuestAccepted, NavalStorylineData.OnPlayerPostponedQuestStart);
				};
			})
			.CloseDialog()
			.PlayerOption(text6, IsGunnar)
			.Consequence(delegate
			{
				Campaign.Current.ConversationManager.ConversationEndOneShot += NavalStorylineData.OnPlayerPostponedQuestStart;
			})
			.CloseDialog()
			.PlayerOption("{=aEKNUI45}This war on the Sea Hounds is too risky. There must be another way to get my sister back.")
			.GotoDialogState("gunnar_ransom_sister")
			.EndPlayerOptions();
		DialogFlow dialogFlow2 = DialogFlow.CreateDialogFlow("start", 1200).NpcLine(npcText6, IsGunnar).Condition(() => NavalStorylineData.Lahar.HasMet && IsAct3Quest2ReadyToStart(NavalStorylineData.Gunnar))
			.BeginPlayerOptions()
			.PlayerOption(text7, IsGunnar)
			.Consequence(delegate
			{
				Campaign.Current.ConversationManager.ConversationEndOneShot += delegate
				{
					NavalStorylineData.OnPlayerAcceptsQuest(QuestAccepted, NavalStorylineData.OnPlayerPostponedQuestStart);
				};
			})
			.CloseDialog()
			.PlayerOption(text8, IsGunnar)
			.Consequence(delegate
			{
				Campaign.Current.ConversationManager.ConversationEndOneShot += NavalStorylineData.OnPlayerPostponedQuestStart;
			})
			.CloseDialog()
			.PlayerOption("{=aEKNUI45}This war on the Sea Hounds is too risky. There must be another way to get my sister back.")
			.GotoDialogState("gunnar_ransom_sister")
			.EndPlayerOptions();
		DialogFlow dialogFlow3 = DialogFlow.CreateDialogFlow("start", 1200).NpcLine(npcText7, IsLahar, IsMainHero).Condition(() => NavalStorylineData.Lahar.HasMet && IsAct3Quest2ReadyToStart(NavalStorylineData.Lahar))
			.CloseDialog();
		Campaign.Current.ConversationManager.AddDialogFlow(dialogFlow);
		Campaign.Current.ConversationManager.AddDialogFlow(dialogFlow2);
		Campaign.Current.ConversationManager.AddDialogFlow(dialogFlow3);
	}

	private bool MultiAgentConversationCondition()
	{
		StringHelpers.SetCharacterProperties("PLAYER", CharacterObject.PlayerCharacter);
		if (IsAct3Quest2ReadyToStart(NavalStorylineData.Gunnar) && Mission.Current != null && !NavalStorylineData.Lahar.HasMet)
		{
			NavalStorylineData.Lahar.SetHasMet();
			Agent agent = null;
			foreach (Agent nearbyAgent in Mission.Current.GetNearbyAgents(Agent.Main.Position.AsVec2, 100f, new MBList<Agent>()))
			{
				if (nearbyAgent.Character == NavalStorylineData.Gunnar.CharacterObject)
				{
					agent = nearbyAgent;
					break;
				}
			}
			if (agent != null)
			{
				Agent agent2 = SpawnLahar(agent);
				agent2.SetLookAgent(Agent.Main);
				Campaign.Current.ConversationManager.AddConversationAgents(new List<Agent> { agent2 }, setActionsInstantly: true);
			}
			return true;
		}
		return false;
	}

	private bool IsAct3Quest2ReadyToStart(Hero conversationHero)
	{
		if (NavalStorylineData.IsStorylineActivationPossible() && NavalStorylineData.HasCompletedLast(NavalStorylineData.NavalStorylineStage.Act3Quest1) && Hero.OneToOneConversationHero == conversationHero && Settlement.CurrentSettlement == NavalStorylineData.HomeSettlement && !Campaign.Current.QuestManager.IsThereActiveQuestWithType(typeof(SailToTheGulfOfCharasQuest)))
		{
			return !Campaign.Current.QuestManager.IsThereActiveQuestWithType(typeof(HuntDownTheEmiraAlFahdaAndTheCorsairsQuest));
		}
		return false;
	}

	private bool IsGunnar(IAgent agent)
	{
		return agent.Character == NavalStorylineData.Gunnar.CharacterObject;
	}

	private bool IsLahar(IAgent agent)
	{
		return agent.Character == NavalStorylineData.Lahar.CharacterObject;
	}

	private bool IsMainHero(IAgent agent)
	{
		return agent.Character == CharacterObject.PlayerCharacter;
	}

	private Agent SpawnLahar(Agent gunnar)
	{
		AgentBuildData agentBuildData = new AgentBuildData(NavalStorylineData.Lahar.CharacterObject);
		agentBuildData.TroopOrigin(new SimpleAgentOrigin(agentBuildData.AgentCharacter));
		Vec3 position = gunnar.Position - Agent.Main.Position;
		position.RotateAboutZ(0.34906584f);
		position += Agent.Main.Position;
		int num = 250;
		while (true)
		{
			UIntPtr? uIntPtr = Mission.Current?.Scene?.GetNavigationMeshForPosition(in position);
			UIntPtr zero = UIntPtr.Zero;
			if (!uIntPtr.HasValue || (uIntPtr.HasValue && !(uIntPtr.GetValueOrDefault() == zero)) || num == 0)
			{
				break;
			}
			if (MBRandom.RandomFloat > 0.5f)
			{
				position.RotateAboutZ(System.MathF.PI / 180f * (float)MBRandom.RandomInt(20, 45));
			}
			else
			{
				position.RotateAboutZ(System.MathF.PI / 180f * (float)MBRandom.RandomInt(-45, -20));
			}
			num--;
		}
		if (num == 0)
		{
			Debug.FailedAssert("Couldn't find a valid position for Lahar around Gunnar", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\NavalDLC\\Storyline\\CampaignBehaviors\\NavalStorylineThirdActSecondQuestBehavior.cs", "SpawnLahar", 284);
			position = Mission.Current.GetRandomPositionAroundPoint(gunnar.Position, 1f, 3f, nearFirst: true);
		}
		agentBuildData.InitialPosition(in position);
		agentBuildData.InitialDirection(-Agent.Main.LookDirection.AsVec2.Normalized());
		agentBuildData.NoHorses(noHorses: true);
		agentBuildData.CivilianEquipment(civilianEquipment: true);
		return Mission.Current.SpawnAgent(agentBuildData);
	}

	private void QuestAccepted()
	{
		_isQuestAcceptedThroughMission = true;
		GameMenu.ActivateGameMenu("naval_storyline_act_3_quest_2_conversation_menu");
		Mission.Current.EndMission();
	}

	private void StartQuest()
	{
		if (!Campaign.Current.QuestManager.IsThereActiveQuestWithType(typeof(SailToTheGulfOfCharasQuest)))
		{
			CampaignVec2 campaignVec = new CampaignVec2(new Vec2(194.4578f, 359.8387f), isOnLand: false);
			if (!NavigationHelper.IsPositionValidForNavigationType(campaignVec, MobileParty.NavigationType.Naval))
			{
				campaignVec = NavigationHelper.FindReachablePointAroundPosition(campaignVec, MobileParty.NavigationType.Naval, 10f);
			}
			new SailToTheGulfOfCharasQuest("naval_storyline_act3_quest2_1", NavalStorylineData.Gunnar, campaignVec).StartQuest();
		}
	}

	public override void SyncData(IDataStore dataStore)
	{
	}
}
