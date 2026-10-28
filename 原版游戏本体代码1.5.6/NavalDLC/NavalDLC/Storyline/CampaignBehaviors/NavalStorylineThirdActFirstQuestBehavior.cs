using Helpers;
using NavalDLC.Storyline.Quests;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.MountAndBlade;

namespace NavalDLC.Storyline.CampaignBehaviors;

public class NavalStorylineThirdActFirstQuestBehavior : CampaignBehaviorBase
{
	private const string _questConversationMenuId = "naval_storyline_act_3_quest_1_conversation_menu";

	private bool _isIntroGiven;

	private bool _isQuestAcceptedThroughMission;

	private SetSailAndEscortTheFortuneSeekersQuest _cachedQuest;

	private IFaction _merchantsFaction;

	private static SetSailAndEscortTheFortuneSeekersQuest Instance
	{
		get
		{
			NavalStorylineThirdActFirstQuestBehavior campaignBehavior = Campaign.Current.GetCampaignBehavior<NavalStorylineThirdActFirstQuestBehavior>();
			if (campaignBehavior._cachedQuest != null && campaignBehavior._cachedQuest.IsOngoing)
			{
				return campaignBehavior._cachedQuest;
			}
			foreach (QuestBase quest in Campaign.Current.QuestManager.Quests)
			{
				if (quest is SetSailAndEscortTheFortuneSeekersQuest cachedQuest)
				{
					campaignBehavior._cachedQuest = cachedQuest;
					return campaignBehavior._cachedQuest;
				}
			}
			return null;
		}
	}

	public override void RegisterEvents()
	{
		if (!NavalStorylineData.IsNavalStorylineCanceled())
		{
			CampaignEvents.OnAfterSessionLaunchedEvent.AddNonSerializedListener(this, OnAfterSessionLaunched);
			NavalDLCEvents.OnNavalStorylineCanceledEvent.AddNonSerializedListener(this, OnNavalStorylineCanceled);
			CampaignEvents.OnQuestStartedEvent.AddNonSerializedListener(this, OnQuestStarted);
		}
	}

	private void OnQuestStarted(QuestBase quest)
	{
		if (quest is SetSailAndEscortTheFortuneSeekersQuest)
		{
			_merchantsFaction = NavalStorylineData.HomeSettlement.OwnerClan;
		}
	}

	private void OnNavalStorylineCanceled(NavalStorylineData.StorylineCancelDetail detail)
	{
		CampaignEventDispatcher.Instance.RemoveListeners(this);
	}

	private void OnAfterSessionLaunched(CampaignGameStarter campaignGameStarter)
	{
		AddGunnarInitialDialogFlow();
		AddMerchantsDialogueFlow(campaignGameStarter);
		AddGameMenus(campaignGameStarter);
	}

	private void AddGameMenus(CampaignGameStarter gameStarter)
	{
		gameStarter.AddGameMenu("naval_storyline_act_3_quest_1_conversation_menu", string.Empty, naval_storyline_act_3_quest_1_conversation_menu_on_init);
	}

	private void naval_storyline_act_3_quest_1_conversation_menu_on_init(MenuCallbackArgs args)
	{
		if (_isQuestAcceptedThroughMission && Mission.Current == null)
		{
			OnPlayerAgreedToHelp();
			_isQuestAcceptedThroughMission = false;
		}
	}

	private void AddGunnarInitialDialogFlow()
	{
		Campaign.Current.ConversationManager.AddDialogFlow(DialogFlow.CreateDialogFlow("start", 1200).NpcLine("{=HTEIIesY}[ib:closed]Greetings. Listen… When we sailed with Purig, I was hoping that he would help me fight the Sea Hounds near Hvalvik. His betrayal of course has cost us time, but I think that plan is still a good one.").Condition(() => IsQuest1ReadyToStart() && !NavalStorylineData.IsTutorialSkipped() && !_isIntroGiven)
			.NpcLine("{=zYEWPvl2}[ib:confident]That captive we took, Hralgar, said that the Sea Hounds expect to find rich pickings near Beinland. I think I know what he is talking about. Every year, a Vlandian merchant ship travels to the far north, bearing hunters and other fortune-seekers. It should be returning south around this time. These men have spent the last months gathering walrus ivory, fur and whale oil, all of which are quite valuable in the southlands.")
			.NpcLine("{=Tn5mFdcU}[ib:hip]Such a prize would be a great boon to the Sea Hounds. I propose that we deny it to them. We can sail to Hvalvik, meet this merchant, and escort them south, sinking or taking any Sea Hounds we encounter.")
			.NpcLine("{=DRRUMKFN}Our longship is ready. If you can join me, then we should set out as soon as you are ready.")
			.NpcLine("{=xngacVnQ}[ib:normal]One thing - it is hard to revictual at sea, so do make sure we have plenty of supplies with us to go to Hvalvik and back. Twenty loads of grain and meat, or the equivalent, should be sufficient for our voyage.")
			.Consequence(delegate
			{
				_isIntroGiven = true;
			})
			.BeginPlayerOptions()
			.PlayerOption("{=SdwdyDGN}I am ready to sail.")
			.NpcLine("{=bhUo9L89}[ib:hip][if:convo_approving]Splendid. The tide and winds are with us. Let us go forth!")
			.Consequence(delegate
			{
				if (Mission.Current == null)
				{
					Campaign.Current.ConversationManager.ConversationEndOneShot += OnPlayerAgreedToHelp;
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
			.PlayerOption("{=k07wzat8}I am not ready yet.")
			.Consequence(delegate
			{
				Campaign.Current.ConversationManager.ConversationEndOneShot += NavalStorylineData.OnPlayerPostponedQuestStart;
			})
			.NpcLine("{=mw07yfTt}[ib:closed]Very well. We can wait here for a bit longer for you.")
			.CloseDialog()
			.PlayerOption("{=aEKNUI45}This war on the Sea Hounds is too risky. There must be another way to get my sister back.")
			.GotoDialogState("gunnar_ransom_sister")
			.EndPlayerOptions());
		Campaign.Current.ConversationManager.AddDialogFlow(DialogFlow.CreateDialogFlow("start", 1200).NpcLine("{=NSHm5s2u}{PLAYER.NAME}... It's good to see you again! Have you reconsidered joining me in my little feud? I cannot promise you that we will find your sister, but I believe the odds have increased.").Condition(delegate
		{
			StringHelpers.SetCharacterProperties("PLAYER", Hero.MainHero.CharacterObject);
			return IsQuest1ReadyToStart() && NavalStorylineData.IsTutorialSkipped() && !_isIntroGiven;
		})
			.NpcLine("{=XDr67yKI}[ib:closed]When last we met, I was intending to sail with my old friend Purig. Well, I always fancied myself a good judge of character, but I suppose fond memories of my warlike youth went to my head like ale. Purig betrayed me. Like so many of my comrades from those days, he turned Sea Hound. I escaped his clutches however, and returned here. I know a great deal more about their operations.")
			.NpcLine("{=zYguiNhG}[ib:confident]Anyway, I had originally wanted to join up with a merchant returning to Vlandia from Hvalvik, and I think that plan is still a good one. His company has spent the last months hunting and whaling in the far north, and their ship is laden with valuables. I am certain that the Sea Hounds will be unable to resist such a tempting target.")
			.Consequence(delegate
			{
				_isIntroGiven = true;
			})
			.BeginPlayerOptions()
			.PlayerOption("{=SdwdyDGN}I am ready to sail.")
			.NpcLine("{=bhUo9L89}[ib:hip]Splendid. The tide and winds are with us. Let us go forth!")
			.Consequence(delegate
			{
				if (Mission.Current == null)
				{
					Campaign.Current.ConversationManager.ConversationEndOneShot += OnPlayerAgreedToHelp;
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
			.PlayerOption("{=k07wzat8}I am not ready yet.")
			.Consequence(delegate
			{
				Campaign.Current.ConversationManager.ConversationEndOneShot += NavalStorylineData.OnPlayerPostponedQuestStart;
			})
			.NpcLine("{=mw07yfTt}[ib:closed2]Very well. We can wait here for a bit longer for you.")
			.CloseDialog()
			.PlayerOption("{=aEKNUI45}This war on the Sea Hounds is too risky. There must be another way to get my sister back.")
			.GotoDialogState("gunnar_ransom_sister")
			.EndPlayerOptions());
		Campaign.Current.ConversationManager.AddDialogFlow(DialogFlow.CreateDialogFlow("start", 1500).NpcLine("{=yJIP3tpk}[ib:closed2]Are we ready to sail to Hvalvik to escort those Vlandian merchants? They will wait as long as they can, but they cannot wait forever.").Condition(() => IsQuest1ReadyToStart() && _isIntroGiven)
			.BeginPlayerOptions()
			.PlayerOption("{=qcYkbX2a}Let us sail.")
			.Consequence(delegate
			{
				if (Mission.Current == null)
				{
					Campaign.Current.ConversationManager.ConversationEndOneShot += OnPlayerAgreedToHelp;
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
			.PlayerOption("{=yCTF6YvP}I still need more time.")
			.Consequence(delegate
			{
				Campaign.Current.ConversationManager.ConversationEndOneShot += NavalStorylineData.OnPlayerPostponedQuestStart;
			})
			.CloseDialog()
			.PlayerOption("{=aEKNUI45}This war on the Sea Hounds is too risky. There must be another way to get my sister back.")
			.GotoDialogState("gunnar_ransom_sister")
			.EndPlayerOptions());
	}

	private void AddMerchantsDialogueFlow(CampaignGameStarter campaignGameStarter)
	{
		campaignGameStarter.AddDialogLine("merchant_meeting_dialogue", "start", "merchant_meeting_player_options_1", "{=lV2EbD7b}Ahoy! Who are you, and what's your purpose?", merchant_meeting_dialogue_on_condition, null, 50000);
		campaignGameStarter.AddPlayerLine("merchant_meeting_dialogue_player_options1_1", "merchant_meeting_player_options_1", "merchant_meeting_npc_answer", "{=zjDk0evO}We're here to escort you, if you'll have us.", null, null);
		campaignGameStarter.AddPlayerLine("merchant_meeting_dialogue_player_options1_2", "merchant_meeting_player_options_1", "merchant_meeting_npc_answer", "{=1EkgbhaB}We're here making war upon the Sea Hounds, a pirate confederation.", null, null);
		campaignGameStarter.AddDialogLine("merchant_meeting_npc_answer_line", "merchant_meeting_npc_answer", "merchant_meeting_player_options_2", "{=MlLDWXuR}[if:convo_grateful]If that's the case then we're glad to have you around. Back in Hvalvik port, we heard rumors of these pirates, and we were none too pleased that we had to venture out alone like this. Tell me then, are you asking anything for your services?", null, null);
		campaignGameStarter.AddPlayerLine("merchant_meeting_dialogue_player_options2_1", "merchant_meeting_player_options_2", "merchant_meeting_npc_answer_2", "{=ZFONiAA3}A small share of your cargo would be customary.", null, null);
		campaignGameStarter.AddPlayerLine("merchant_meeting_dialogue_player_options2_2", "merchant_meeting_player_options_2", "merchant_meeting_npc_answer_2", "{=ens8bc7I}Merely a chance to fight those slaving bastards.", null, null);
		campaignGameStarter.AddDialogLine("merchant_meeting_npc_answer_2_line", "merchant_meeting_npc_answer_2", "close_window", "{=tH5wQo81}[ib:hip]Very well. Should we arrive safely, we will happily show our gratitude with a contribution to your cause. The wind is brisk and the waves are choppy, so try not to venture too far away… May the Heavens protect us from pirates and the perils of the sea.", null, delegate
		{
			Campaign.Current.ConversationManager.ConversationEndOneShot += OnMerchantConversationEnded;
		});
	}

	private bool merchant_meeting_dialogue_on_condition()
	{
		if (Instance != null && !Instance.HasMetMerchants && !Instance.HasSavedMerchants)
		{
			return Instance.IsConversationHeroTheMerchant;
		}
		return false;
	}

	private void OnMerchantConversationEnded()
	{
		Instance.OnMerchantsMet();
		PlayerEncounter.Finish();
	}

	private bool IsQuest1ReadyToStart()
	{
		if (NavalStorylineData.IsStorylineActivationPossible() && NavalStorylineData.HasCompletedLast(NavalStorylineData.NavalStorylineStage.Act2) && Settlement.CurrentSettlement == NavalStorylineData.HomeSettlement && Hero.OneToOneConversationHero == NavalStorylineData.Gunnar && !Campaign.Current.QuestManager.IsThereActiveQuestWithType(typeof(SetSailAndMeetTheFortuneSeekersInTargetSettlementQuest)))
		{
			return !Campaign.Current.QuestManager.IsThereActiveQuestWithType(typeof(SetSailAndEscortTheFortuneSeekersQuest));
		}
		return false;
	}

	private void QuestAccepted()
	{
		_isQuestAcceptedThroughMission = true;
		GameMenu.ActivateGameMenu("naval_storyline_act_3_quest_1_conversation_menu");
		Mission.Current.EndMission();
	}

	private void OnPlayerAgreedToHelp()
	{
		new SetSailAndMeetTheFortuneSeekersInTargetSettlementQuest("naval_storyline_act3_quest1_1", NavalStorylineData.Gunnar, NavalStorylineData.Act3Quest1TargetSettlement).StartQuest();
	}

	public override void SyncData(IDataStore dataStore)
	{
		dataStore.SyncData("_isIntroGiven", ref _isIntroGiven);
		dataStore.SyncData("_merchantsFaction", ref _merchantsFaction);
	}
}
