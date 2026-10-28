using NavalDLC.Storyline.Quests;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace NavalDLC.Storyline.CampaignBehaviors;

public class NavalStorylineThirdActThirdQuestBehavior : CampaignBehaviorBase
{
	private const string _questConversationMenuId = "naval_storyline_act_3_quest_3_conversation_menu";

	private bool _isQuestAcceptedThroughMission;

	private bool _isIntroGiven;

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

	public override void SyncData(IDataStore dataStore)
	{
		dataStore.SyncData("_isIntroGiven", ref _isIntroGiven);
	}

	private void OnSessionLaunched(CampaignGameStarter campaignGameSystemStarter)
	{
		AddDialogs();
		AddGameMenus(campaignGameSystemStarter);
	}

	private void AddGameMenus(CampaignGameStarter starter)
	{
		starter.AddGameMenu("naval_storyline_act_3_quest_3_conversation_menu", string.Empty, naval_storyline_act_3_quest_3_conversation_menu_on_init);
	}

	private void naval_storyline_act_3_quest_3_conversation_menu_on_init(MenuCallbackArgs args)
	{
		if (_isQuestAcceptedThroughMission && Mission.Current == null)
		{
			StartQuest();
			_isQuestAcceptedThroughMission = false;
		}
	}

	private void AddDialogs()
	{
		AddGunnarInitialDialogFlow();
	}

	private void AddGunnarInitialDialogFlow()
	{
		Campaign.Current.ConversationManager.AddDialogFlow(DialogFlow.CreateDialogFlow("start", 1500).NpcLine("{=0xymiaMQ}{PLAYER.NAME}... So… I have been making inquiries into what Fahda told us, about these Vlandian pirates in Purig's employ and their plan to steal the Sturgian silver. Several large warships have been sighted patrolling near {SETTLEMENT_LINK}. I suspect that these are the Vlandians.").Condition(delegate
		{
			MBTextManager.SetTextVariable("SETTLEMENT_LINK", NavalStorylineData.Act3Quest3TargetSettlement.EncyclopediaLinkWithName);
			return NavalStorylineData.IsStorylineActivationPossible() && NavalStorylineData.HasCompletedLast(NavalStorylineData.NavalStorylineStage.Act3Quest2) && Settlement.CurrentSettlement == NavalStorylineData.HomeSettlement && Hero.OneToOneConversationHero == NavalStorylineData.Gunnar && !Campaign.Current.QuestManager.IsThereActiveQuestWithType(typeof(SpeakToTheSailorsQuest)) && !_isIntroGiven;
		})
			.NpcLine("{=Jjm2hpCl}{SETTLEMENT_LINK} is linked to the Byalic Sea by a wide estuary. It would be easy for the pirates to sit there, like spiders in a web, and wait until the Sturgians despair of losing all their commerce and try to run the blockade. Then the Vlandians will snap up the ships and their treasure.")
			.Condition(delegate
			{
				MBTextManager.SetTextVariable("SETTLEMENT_LINK", NavalStorylineData.Act3Quest3TargetSettlement.EncyclopediaLinkWithName);
				return true;
			})
			.NpcLine("{=jFhkURpP}I'm sure Purig could wreak a great deal of wickedness with this silver in his hands, and I would very much like to foil this plan of his.")
			.Consequence(delegate
			{
				_isIntroGiven = true;
			})
			.BeginPlayerOptions()
			.PlayerOption("{=el44RZG4}Let us set out, then.")
			.Consequence(delegate
			{
				if (Mission.Current == null)
				{
					Campaign.Current.ConversationManager.ConversationEndOneShot += StartQuest;
				}
				else
				{
					Campaign.Current.ConversationManager.ConversationEndOneShot += delegate
					{
						NavalStorylineData.OnPlayerAcceptsQuest(QuestAccepted, NavalStorylineData.OnPlayerPostponedQuestStart);
					};
				}
			})
			.CloseDialog()
			.PlayerOption("{=a0j86F9C}I need a bit more time.")
			.Consequence(delegate
			{
				Campaign.Current.ConversationManager.ConversationEndOneShot += NavalStorylineData.OnPlayerPostponedQuestStart;
			})
			.CloseDialog()
			.PlayerOption("{=aEKNUI45}This war on the Sea Hounds is too risky. There must be another way to get my sister back.")
			.GotoDialogState("gunnar_ransom_sister")
			.EndPlayerOptions());
		Campaign.Current.ConversationManager.AddDialogFlow(DialogFlow.CreateDialogFlow("start", 1500).NpcLine("{=LnqHcu5S}Are we ready to sail for {SETTLEMENT_LINK}? The tide and winds are right.").Condition(delegate
		{
			MBTextManager.SetTextVariable("SETTLEMENT_LINK", NavalStorylineData.Act3Quest3TargetSettlement.EncyclopediaLinkWithName);
			return NavalStorylineData.IsStorylineActivationPossible() && NavalStorylineData.HasCompletedLast(NavalStorylineData.NavalStorylineStage.Act3Quest2) && Settlement.CurrentSettlement == NavalStorylineData.HomeSettlement && Hero.OneToOneConversationHero == NavalStorylineData.Gunnar && !Campaign.Current.QuestManager.IsThereActiveQuestWithType(typeof(SpeakToTheSailorsQuest)) && _isIntroGiven;
		})
			.BeginPlayerOptions()
			.PlayerOption("{=EjnrlsjX}Get the men to their ships. We sail at once.")
			.Consequence(delegate
			{
				if (Mission.Current == null)
				{
					Campaign.Current.ConversationManager.ConversationEndOneShot += StartQuest;
				}
				else
				{
					Campaign.Current.ConversationManager.ConversationEndOneShot += delegate
					{
						NavalStorylineData.OnPlayerAcceptsQuest(QuestAccepted, NavalStorylineData.OnPlayerPostponedQuestStart);
					};
				}
			})
			.CloseDialog()
			.PlayerOption("{=Ebk8s9s1}I am not yet ready.")
			.Consequence(delegate
			{
				Campaign.Current.ConversationManager.ConversationEndOneShot += NavalStorylineData.OnPlayerPostponedQuestStart;
			})
			.CloseDialog()
			.PlayerOption("{=aEKNUI45}This war on the Sea Hounds is too risky. There must be another way to get my sister back.")
			.GotoDialogState("gunnar_ransom_sister")
			.EndPlayerOptions());
	}

	private void QuestAccepted()
	{
		_isQuestAcceptedThroughMission = true;
		OpenQuestMenu();
		Mission.Current.EndMission();
	}

	private void OpenQuestMenu()
	{
		GameMenu.ActivateGameMenu("naval_storyline_act_3_quest_3_conversation_menu");
	}

	private void StartQuest()
	{
		new SpeakToTheSailorsQuest("speak_to_the_sailors_quest", NavalStorylineData.Act3Quest3TargetSettlement).StartQuest();
	}
}
