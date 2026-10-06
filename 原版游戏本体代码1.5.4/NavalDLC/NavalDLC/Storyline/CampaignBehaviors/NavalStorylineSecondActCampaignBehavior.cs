using NavalDLC.Storyline.Quests;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace NavalDLC.Storyline.CampaignBehaviors;

public class NavalStorylineSecondActCampaignBehavior : CampaignBehaviorBase
{
	private const string _questConversationMenuId = "naval_storyline_act_2_conversation_menu";

	private bool _isQuestAcceptedThroughMission;

	private bool _isIntroGiven;

	private bool _isOption1Selected;

	private bool _isOption2Selected;

	private bool _isOption3Selected;

	public override void RegisterEvents()
	{
		if (!NavalStorylineData.IsNavalStorylineCanceled())
		{
			NavalDLCEvents.OnNavalStorylineCanceledEvent.AddNonSerializedListener(this, OnNavalStorylineCanceled);
			CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
		}
	}

	private void OnSessionLaunched(CampaignGameStarter campaignGameSystemStarter)
	{
		AddDialogs();
		AddGameMenus(campaignGameSystemStarter);
	}

	private void AddGameMenus(CampaignGameStarter starter)
	{
		starter.AddGameMenu("naval_storyline_act_2_conversation_menu", string.Empty, naval_storyline_act_2_conversation_menu_on_init);
	}

	private void naval_storyline_act_2_conversation_menu_on_init(MenuCallbackArgs args)
	{
		if (_isQuestAcceptedThroughMission && Mission.Current == null)
		{
			StartQuest();
			_isQuestAcceptedThroughMission = false;
		}
	}

	private void AddDialogs()
	{
		AddGunnarDialogFlow();
	}

	private void OnNavalStorylineCanceled(NavalStorylineData.StorylineCancelDetail detail)
	{
		CampaignEventDispatcher.Instance.RemoveListeners(this);
	}

	private void AddGunnarDialogFlow()
	{
		Campaign.Current.ConversationManager.AddDialogFlow(DialogFlow.CreateDialogFlow("start", 1200).NpcLine("{=LI75U7wB}[ib:closed2]Well, I sent off my letter, and I spoke to some men from my homeland who have just arrived. They are sick of Sea Hound raids, and ready to join us in our hunt. I am afraid we cannot take your own companions, however. There's not enough room on the ship, and my men aren't willing to trust their lives to any other vessel in these northern seas.").Condition(() => IsAct2ReadyToStart(NavalStorylineData.Gunnar) && !_isIntroGiven)
			.GetOutputToken(out var oState)
			.NpcLine("{=sKumwPNF}I recommend you make ready to sail again soon, but we have a bit of time.")
			.BeginPlayerOptions()
			.PlayerOption("{=Vk0n2AHp}Who are these allies? And to whom were you writing your letter?")
			.ClickableCondition(option_1_clickable_condition)
			.Consequence(delegate
			{
				_isOption1Selected = true;
			})
			.NpcLine("{=jzVnKuMC}[ib:confident]The ones with us now are farmers from the island of Beinland, near my village at Lagshofn. They are good men, and well-motivated. That coast has suffered greatly at the hands of the Sea Hounds. But they are not warriors who live for battle.")
			.NpcLine("{=suM1ocUh}[ib:normal]The letter, though, was addressed to one of my very old friends, Bjolgur of Agilting. When he left the rebellion he chose neither peace nor banditry. He chose, instead, to join the Skolderbroda, the Shield Brothers. Rather than fight for a king, he said, he would fight for whoever pays him. A king may turn out to be unworthy of his warriors' valor, but gold is never unworthy.")
			.NpcLine("{=Z7FadHk0}[ib:closed]I do not pretend that I see eye-to-eye with Bjolgur on all things. But he, like me, prefers to match his skill against other warriors, and he has kin in Beinland. He will not come immediately, as he must take permission from his brotherhood, but when he does come we will be very glad of his help.")
			.GotoDialogState(oState)
			.PlayerOption("{=kAFsfSda}Can you tell me more about your past with the Sea Hounds?")
			.ClickableCondition(option_2_clickable_condition)
			.Consequence(delegate
			{
				_isOption2Selected = true;
			})
			.NpcLine("{=n6VFFFoU}[ib:confident]I suppose I have time for an old war story or two, if you're truly of a mind to hear…")
			.NpcLine("{=GW2Qa6Iq}As I told you, we fought together against old king Volbjorn. Many of us were from Beinland and other parts of the Nordvyg where even the jarls tread lightly. A man who called himself our 'king' - well, we weren't having any of that. We didn't call ourselves the Sea Hounds back then, but we flew the dogs-head banner, to show our loyalty to each other and to our cause.")
			.NpcLine("{=YkjnLIGV}[ib:normal]We fought, but we lost, in a great battle in Hvalvik Bay. I and a hundred other captives were led before Volbjorn, and the king ordered his men to take our heads. A dozen or so were killed before me, so I had time to think up something to say. As they held me down to receive the blow, I told Volbjorn that it was a good thing I'd eaten not long ago, so I could let him know directly from the High Hall what sort of feast they'd prepared for him there.")
			.NpcLine("{=ImiGnP0a}[ib:hip2]I thought he'd order me cut open, or flayed, but instead Volbjorn laughed and told me that he liked my mettle. He'd spare my life, and the lives of the rest of my comrades, so long as I swore an oath not to take up arms against him again. Volbjorn had made his point with a dozen headless bodies at his feet, and men paying the land-tax or serving in his armies were of more value to him than corpses. If I swore, a hundred of my comrades could return to their families. So I swore.")
			.NpcLine("{=hnSqEnxu}[ib:closed]Some of our number never showed up at the battle, however. I can imagine how it went for them after that - mocked in taverns or in scraps of songs sung by children.")
			.NpcLine("{=ZOV8GaTO}The north is not forgiving of those who throw away their good name. Their old life, where they were feared rather than scorned, must have seemed much sweeter in comparison. And of course they conceived a great resentment against those of us who faced death and showed them up as cowards. These are the ones who became the Sea Hounds, who aimed to steal the wealth and glory that they failed to win in battle.")
			.GotoDialogState(oState)
			.PlayerOption("{=oKRiNpUR}Why did Purig turn on you? ")
			.ClickableCondition(option_3_clickable_condition)
			.Consequence(delegate
			{
				_isOption3Selected = true;
			})
			.NpcLine("{=IV6FiJW4}[ib:normal2][if:convo_grave]I do not know for sure. If I had ever knowingly given him insult, I never would have sailed on his ship.")
			.NpcLine("{=1Aq78OZl}[if:convo_normal]A true warrior, to my mind, knows his own mettle. He does not need others to remind of his honor. He knows that sometimes in war you suffer bad luck - your ship arrived late, you were carried away in the rush of a rout, you were defeated by a king with forces far greater than your own.")
			.NpcLine("{=L4EokSgE}[ib:confident2]Other men, on the other hand, crave glory. They must hear the cheers of townsfolk and wear gilded armor given to them by kings. They must be envied by other men. And if they aren't, they may hear a little voice in your head telling them that they can steal this glory, that they can slay the weak and take their wealth and buy the respect that they crave.")
			.NpcLine("{=l3DKrfv0}That voice spoke to Purig, he gave into it, and it twisted his heart into something truly dark. He is a husk of a man filled only with ambition and wounded pride, all concealed beneath a fair face and a friendly laugh.")
			.GotoDialogState(oState)
			.PlayerOption("{=sOtCi0WH}So, that's all I have to ask. What would be our next move?")
			.Condition(() => _isOption1Selected && _isOption2Selected && _isOption3Selected)
			.NpcLine("{=oMRN2H6T}[ib:hip]Well… I do not think we will have to go very far to start our hunt. A pair of ships have been patrolling off our coast, robbing passing fishermen, and I'd wager one of my eyes to a bale of herring that they're Sea Hounds. They scatter like minnows whenever a real warship sets out, but I think that my men's little vessel would be prey much to their liking. Hopefully, the fight will come to us.")
			.Consequence(delegate
			{
				_isIntroGiven = true;
			})
			.BeginPlayerOptions()
			.PlayerOption("{=qhmolZly}Let's sail out, see if they're Sea Hounds, and sink or take them if they are.")
			.NpcLine("{=eLCcTeAX}Right. To the ship, then!")
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
			.PlayerOption("{=R7KiYpab}I have things to do on shore.")
			.Consequence(delegate
			{
				Campaign.Current.ConversationManager.ConversationEndOneShot += NavalStorylineData.OnPlayerPostponedQuestStart;
			})
			.NpcLine("{=Bss21RWb}Very well. Come back here when you're ready.")
			.CloseDialog()
			.EndPlayerOptions()
			.CloseDialog()
			.EndPlayerOptions()
			.CloseDialog());
		Campaign.Current.ConversationManager.AddDialogFlow(DialogFlow.CreateDialogFlow("start", 1200).NpcLine("{=7NQsM70B}[ib:hip]Are you ready to deal with those two Sea Hound vessels?").Condition(() => IsAct2ReadyToStart(NavalStorylineData.Gunnar) && _isIntroGiven)
			.BeginPlayerOptions()
			.PlayerOption("{=NzMX0s21}I am ready.")
			.NpcLine("{=eLCcTeAX}Right. To the ship, then!")
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
			.PlayerOption("{=8MFLb4X6}I still have things to do on shore.")
			.Consequence(delegate
			{
				Campaign.Current.ConversationManager.ConversationEndOneShot += NavalStorylineData.OnPlayerPostponedQuestStart;
			})
			.NpcLine("{=Bss21RWb}Very well. Come back here when you're ready.")
			.CloseDialog()
			.EndPlayerOptions()
			.CloseDialog());
	}

	private bool option_1_clickable_condition(out TextObject explanation)
	{
		explanation = TextObject.GetEmpty();
		return !_isOption1Selected;
	}

	private bool option_2_clickable_condition(out TextObject explanation)
	{
		explanation = TextObject.GetEmpty();
		return !_isOption2Selected;
	}

	private bool option_3_clickable_condition(out TextObject explanation)
	{
		explanation = TextObject.GetEmpty();
		return !_isOption3Selected;
	}

	private bool IsAct2ReadyToStart(Hero conversationHero)
	{
		if (NavalStorylineData.IsStorylineActivationPossible() && NavalStorylineData.HasCompletedLast(NavalStorylineData.NavalStorylineStage.Act1) && Hero.OneToOneConversationHero == conversationHero && conversationHero.HasMet && Settlement.CurrentSettlement == NavalStorylineData.HomeSettlement)
		{
			return !Campaign.Current.QuestManager.IsThereActiveQuestWithType(typeof(DefeatThePiratesQuest));
		}
		return false;
	}

	private void StartQuest()
	{
		new DefeatThePiratesQuest("naval_storyline_defeat_the_pirates_quest", NavalStorylineData.Gunnar).StartQuest();
	}

	private void QuestAccepted()
	{
		_isQuestAcceptedThroughMission = true;
		OpenQuestMenu();
		Mission.Current.EndMission();
	}

	private void OpenQuestMenu()
	{
		GameMenu.ActivateGameMenu("naval_storyline_act_2_conversation_menu");
	}

	public override void SyncData(IDataStore dataStore)
	{
		dataStore.SyncData("_isIntroGiven", ref _isIntroGiven);
	}
}
