using System.Collections.Generic;
using System.Linq;
using Helpers;
using StoryMode.StoryModeObjects;
using StoryMode.StoryModePhases;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.SaveSystem;

namespace StoryMode.Quests.FirstPhase;

public class AssembleTheBannerQuest : StoryModeQuestBase
{
	[SaveableField(1)]
	private JournalLog _startLog;

	[SaveableField(2)]
	private bool _talkedWithImperialMentor;

	[SaveableField(3)]
	private bool _talkedWithAntiImperialMentor;

	private TextObject _startQuestLog => new TextObject("{=OS8YjyE5}You should collect all of the pieces of the Dragon Banner before deciding your path.");

	private TextObject _allPiecesCollectedQuestLog
	{
		get
		{
			TextObject textObject = new TextObject("{=eV8R0SKp}Now you can decide what to do with the {DRAGON_BANNER}.");
			textObject.SetTextVariable("DRAGON_BANNER", StoryModeManager.Current.MainStoryLine.DragonBanner.Name);
			StringHelpers.SetCharacterProperties("IMPERIAL_MENTOR", StoryModeHeroes.ImperialMentor.CharacterObject, textObject);
			StringHelpers.SetCharacterProperties("ANTI_IMPERIAL_MENTOR", StoryModeHeroes.AntiImperialMentor.CharacterObject, textObject);
			return textObject;
		}
	}

	private TextObject _talkedWithImperialMentorButNotWithAntiImperialMentorQuestLog
	{
		get
		{
			TextObject textObject = new TextObject("{=yNcBDr9j}You talked with {IMPERIAL_MENTOR.LINK}. Now, you may want to talk with {ANTI_IMPERIAL_MENTOR.LINK} and take {?ANTI_IMPERIAL_MENTOR.GENDER}her{?}his{\\?} opinions too. {?ANTI_IMPERIAL_MENTOR.GENDER}She{?}He{\\?} is currently in {SETTLEMENT_LINK}.");
			StringHelpers.SetCharacterProperties("IMPERIAL_MENTOR", StoryModeHeroes.ImperialMentor.CharacterObject, textObject);
			StringHelpers.SetCharacterProperties("ANTI_IMPERIAL_MENTOR", StoryModeHeroes.AntiImperialMentor.CharacterObject, textObject);
			textObject.SetTextVariable("SETTLEMENT_LINK", StoryModeHeroes.AntiImperialMentor.CurrentSettlement.EncyclopediaLinkWithName);
			return textObject;
		}
	}

	private TextObject _talkedWithImperialMentorQuestLog
	{
		get
		{
			TextObject textObject = new TextObject("{=RwlDeE9t}You talked with {IMPERIAL_MENTOR.LINK} too. Now you should make a decision.");
			StringHelpers.SetCharacterProperties("IMPERIAL_MENTOR", StoryModeHeroes.ImperialMentor.CharacterObject, textObject);
			return textObject;
		}
	}

	private TextObject _talkedWithAntiImperialMentorButNotWithImperialMentorQuestLog
	{
		get
		{
			TextObject textObject = new TextObject("{=yub8ZSFP}You talked with {ANTI_IMPERIAL_MENTOR.LINK}. Now, you may want to talk with {IMPERIAL_MENTOR.LINK} and take {?IMPERIAL_MENTOR.GENDER}her{?}his{\\?} opinions too. {?IMPERIAL_MENTOR.GENDER}She{?}He{\\?} is currently in {SETTLEMENT_LINK}.");
			StringHelpers.SetCharacterProperties("ANTI_IMPERIAL_MENTOR", StoryModeHeroes.AntiImperialMentor.CharacterObject, textObject);
			StringHelpers.SetCharacterProperties("IMPERIAL_MENTOR", StoryModeHeroes.ImperialMentor.CharacterObject, textObject);
			textObject.SetTextVariable("SETTLEMENT_LINK", StoryModeHeroes.ImperialMentor.CurrentSettlement.EncyclopediaLinkWithName);
			return textObject;
		}
	}

	private TextObject _talkedWithAntiImperialMentorQuestLog
	{
		get
		{
			TextObject textObject = new TextObject("{=rfkKxdxp}You talked with {ANTI_IMPERIAL_MENTOR.LINK} too. Now you should make a decision.");
			StringHelpers.SetCharacterProperties("ANTI_IMPERIAL_MENTOR", StoryModeHeroes.AntiImperialMentor.CharacterObject, textObject);
			return textObject;
		}
	}

	private TextObject _endQuestLog => new TextObject("{=eNJBjYG8}You successfully assembled the Dragon Banner of Calradios.");

	public override TextObject Title => new TextObject("{=y84UnOQX}Assemble the Dragon Banner");

	public override bool IsRemainingTimeHidden => false;

	public AssembleTheBannerQuest()
		: base("assemble_the_banner_story_mode_quest", null, StoryModeManager.Current.MainStoryLine.FirstPhase.FirstPhaseEndTime)
	{
		_talkedWithImperialMentor = false;
		_talkedWithAntiImperialMentor = false;
	}

	protected override void InitializeQuestOnGameLoad()
	{
		SetDialogs();
	}

	protected override void HourlyTick()
	{
	}

	protected override void RegisterEvents()
	{
		StoryModeEvents.OnBannerPieceCollectedEvent.AddNonSerializedListener(this, OnBannerPieceCollected);
		CampaignEvents.OnQuestCompletedEvent.AddNonSerializedListener(this, OnQuestCompleted);
	}

	protected override void OnStartQuest()
	{
		SetDialogs();
		_startLog = AddDiscreteLog(_startQuestLog, new TextObject("{=xL3WGYsw}Collected Pieces"), StoryMode.StoryModePhases.FirstPhase.Instance.CollectedBannerPieceCount, 3);
	}

	protected override void OnCompleteWithSuccess()
	{
		AddLog(_endQuestLog);
	}

	private void OnBannerPieceCollected()
	{
		_startLog.UpdateCurrentProgress(StoryMode.StoryModePhases.FirstPhase.Instance.CollectedBannerPieceCount);
		if (StoryMode.StoryModePhases.FirstPhase.Instance.AllPiecesCollected)
		{
			AddLog(_allPiecesCollectedQuestLog);
			AddTrackedObject(StoryModeHeroes.ImperialMentor.CurrentSettlement);
			AddTrackedObject(StoryModeHeroes.AntiImperialMentor.CurrentSettlement);
			AddTrackedObject(StoryModeHeroes.ImperialMentor);
			AddTrackedObject(StoryModeHeroes.AntiImperialMentor);
			StoryModeManager.Current.MainStoryLine.FirstPhase?.MergeDragonBanner();
		}
	}

	private void OnQuestCompleted(QuestBase quest, QuestCompleteDetails detail)
	{
		if (quest is CreateKingdomQuest || quest is SupportKingdomQuest)
		{
			if (IsTracked(StoryModeHeroes.AntiImperialMentor.CurrentSettlement))
			{
				RemoveTrackedObject(StoryModeHeroes.AntiImperialMentor.CurrentSettlement);
			}
			if (IsTracked(StoryModeHeroes.ImperialMentor.CurrentSettlement))
			{
				RemoveTrackedObject(StoryModeHeroes.ImperialMentor.CurrentSettlement);
			}
			if (IsTracked(StoryModeHeroes.AntiImperialMentor))
			{
				RemoveTrackedObject(StoryModeHeroes.AntiImperialMentor);
			}
			if (IsTracked(StoryModeHeroes.ImperialMentor))
			{
				RemoveTrackedObject(StoryModeHeroes.ImperialMentor);
			}
			CompleteQuestWithSuccess();
		}
	}

	public override void OnFailed()
	{
		base.OnFailed();
		RemoveRemainingBannerPieces();
	}

	public override void OnCanceled()
	{
		base.OnCanceled();
		RemoveRemainingBannerPieces();
	}

	protected override void OnTimedOut()
	{
		base.OnTimedOut();
		RemoveRemainingBannerPieces();
	}

	private void RemoveRemainingBannerPieces()
	{
		ItemObject itemObject = Campaign.Current.ObjectManager.GetObject<ItemObject>("dragon_banner_center");
		ItemObject itemObject2 = Campaign.Current.ObjectManager.GetObject<ItemObject>("dragon_banner_dragonhead");
		ItemObject itemObject3 = Campaign.Current.ObjectManager.GetObject<ItemObject>("dragon_banner_handle");
		foreach (ItemRosterElement item in MobileParty.MainParty.ItemRoster)
		{
			if (item.EquipmentElement.Item == itemObject || item.EquipmentElement.Item == itemObject2 || item.EquipmentElement.Item == itemObject3)
			{
				MobileParty.MainParty.ItemRoster.Remove(item);
			}
		}
	}

	protected override void SetDialogs()
	{
		Campaign.Current.ConversationManager.AddDialogFlow(GetImperialMentorEndQuestDialog(), this);
		Campaign.Current.ConversationManager.AddDialogFlow(GetAntiImperialMentorEndQuestDialog(), this);
		Campaign.Current.ConversationManager.AddDialogFlow(DialogFlow.CreateDialogFlow("lord_start", 150).NpcLine(new TextObject("{=AHDQffXv}Have you assembled the banner?")).Condition(AssembleBannerConditionDialogCondition)
			.PlayerLine(new TextObject("{=2h7IlBmv}Not yet, I'm working on it..."))
			.Consequence(delegate
			{
				if (PlayerEncounter.Current != null)
				{
					PlayerEncounter.LeaveEncounter = true;
				}
			})
			.CloseDialog(), this);
	}

	private bool AssembleBannerConditionDialogCondition()
	{
		if ((Hero.OneToOneConversationHero == StoryModeHeroes.ImperialMentor || Hero.OneToOneConversationHero == StoryModeHeroes.AntiImperialMentor) && !StoryMode.StoryModePhases.FirstPhase.Instance.AllPiecesCollected)
		{
			if ((Hero.OneToOneConversationHero == StoryModeHeroes.ImperialMentor && Campaign.Current.QuestManager.Quests.Any((QuestBase q) => !q.IsFinalized && q is MeetWithIstianaQuest)) || (Hero.OneToOneConversationHero == StoryModeHeroes.AntiImperialMentor && Campaign.Current.QuestManager.Quests.Any((QuestBase q) => !q.IsFinalized && q is MeetWithArzagosQuest)))
			{
				return false;
			}
			return true;
		}
		return false;
	}

	private DialogFlow GetAntiImperialMentorEndQuestDialog()
	{
		string oState;
		return DialogFlow.CreateDialogFlow("hero_main_options", 150).BeginPlayerOptions().PlayerSpecialOption(new TextObject("{=r8ZLabb0}I have gathered all pieces of the Dragon Banner. What now?"))
			.Condition(() => Hero.OneToOneConversationHero == StoryModeHeroes.AntiImperialMentor && StoryMode.StoryModePhases.FirstPhase.Instance.AllPiecesCollected && !_talkedWithAntiImperialMentor)
			.NpcLine(new TextObject("{=5j6qvGAF}Excellent work! When you unfurl this banner, and men see what they thought was lost, it will make a powerful impression.[ib:normal2][if:convo_astonished]"))
			.Consequence(GetAntiImperialQuests)
			.NpcLine(new TextObject("{=MOVWOyeh}Clearly you have been chosen by Heaven for a great purpose. I see the makings of a new legend here... Allow me to call you 'Bannerlord.'[ib:normal][if:convo_relaxed_happy]"))
			.NpcLine(new TextObject("{=o791xRtb}Right then, to the business of bringing down this cursed Empire. As I see it, you have two options...[ib:confident2][if:convo_pondering]"))
			.GetOutputToken(out oState)
			.NpcLine(new TextObject("{=c6pDNXbb}You can create your own kingdom or support an existing one...[if:convo_normal]"))
			.BeginPlayerOptions()
			.PlayerOption(new TextObject("{=0pilmavQ}How can I create my own kingdom?"))
			.NpcLine(new TextObject("{=frk7T3ue}It will not be easy, but I can explain in detail..."))
			.NpcLine(new TextObject("{=yCzcfKNM}Firstly, your clan must be independent. You cannot be pledged to an existing realm."))
			.NpcLine(new TextObject("{=tJQ5oajd}Next, your clan must have won for itself considerable renown, or no one will follow you."))
			.NpcLine(new TextObject("{=MJd5agS2}I would recommend that you gather a fairly large army, as you may soon be at war with more powerful and established realms."))
			.NpcLine(new TextObject("{=6YhGGJ7a}Finally, you need a capital for your realm. It can be any settlement you own, so long as they do not speak the imperial tongue. I will not help you create another Empire."))
			.NpcLine(new TextObject("{=fprOWs1E}Now, when you are ready to declare your new kingdom, instruct the governor of your capital to have a proclamation read out throughout your lands."))
			.NpcLine(new TextObject("{=Q2obAF4E}So! You have much to do. I will await news of your success. Return to me when you wish to declare your ownership of the banner to the world."))
			.GotoDialogState(oState)
			.PlayerOption(new TextObject("{=mtiaY2Pa}How can I support an existing kingdom?"))
			.NpcLine(new TextObject("{=oKknZdXn}You should join the kingdom that you wish to support by talking to the leader. None will bring back the Palaic people, but the final victory of any one of those would be suitable vengeance."))
			.NpcLine(new TextObject("{=dPb2Vph3}My informants will tell me once you pledged your support...[ib:normal2][if:convo_nonchalant]"))
			.GotoDialogState(oState)
			.PlayerOption(new TextObject("{=6LQUuQhV}Thank you for your precious help."))
			.CloseDialog()
			.EndPlayerOptions()
			.CloseDialog();
	}

	private void GetAntiImperialQuests()
	{
		_talkedWithAntiImperialMentor = true;
		if (!_talkedWithImperialMentor)
		{
			AddLog(_talkedWithAntiImperialMentorButNotWithImperialMentorQuestLog);
		}
		else
		{
			AddLog(_talkedWithAntiImperialMentorQuestLog);
		}
		if (IsTracked(StoryModeHeroes.AntiImperialMentor.CurrentSettlement))
		{
			RemoveTrackedObject(StoryModeHeroes.AntiImperialMentor.CurrentSettlement);
		}
		new CreateKingdomQuest(StoryModeHeroes.AntiImperialMentor).StartQuest();
		new SupportKingdomQuest(StoryModeHeroes.AntiImperialMentor).StartQuest();
	}

	private DialogFlow GetImperialMentorEndQuestDialog()
	{
		string oState;
		return DialogFlow.CreateDialogFlow("hero_main_options", 150).BeginPlayerOptions().PlayerSpecialOption(new TextObject("{=r8ZLabb0}I have gathered all pieces of the Dragon Banner. What now?"))
			.Condition(() => Hero.OneToOneConversationHero == StoryModeHeroes.ImperialMentor && StoryMode.StoryModePhases.FirstPhase.Instance.AllPiecesCollected && !_talkedWithImperialMentor)
			.NpcLine(new TextObject("{=UjyZ7GFk}Impressive, most impressive. Well, things will get interesting now.[ib:normal2][if:convo_astonished]"))
			.Consequence(GetImperialQuests)
			.NpcLine(new TextObject("{=9E6faNBg}I will need to embroider a proper legend about you. Divine omens at your birth, that kind of thing. For now, we can call you 'Bannerlord,' who brings down the wrath of Heaven on the impudent barbarians.[ib:confident2][if:convo_relaxed_happy]"))
			.NpcLine(new TextObject("{=CnXA7oyE}Now, there are two paths that lie ahead of you, my child!"))
			.GetOutputToken(out oState)
			.NpcLine(new TextObject("{=1GgTNRNl}You can make your own claim to the rulership of the Empire and try to win the civil war, or support an existing claimant...[if:convo_normal]"))
			.BeginPlayerOptions()
			.PlayerOption(new TextObject("{=Dgdopl1b}How can I create my own imperial kingdom?"))
			.NpcLine(new TextObject("{=NdkqUnXb}To have a chance as an imperial contender, you must fullfil some conditions.[if:convo_empathic_voice]"))
			.NpcLine(new TextObject("{=yCzcfKNM}Firstly, your clan must be independent. You cannot be pledged to an existing realm."))
			.NpcLine(new TextObject("{=LLJ0oB8i}Next, your clan's renown must have spread far and wide, or no one will take you seriously."))
			.NpcLine(new TextObject("{=3XbTo6O7}Also, of course, I recommend that you have as large an army as you can gather."))
			.NpcLine(new TextObject("{=Cl4xi6Be}Finally, you need a capital. Any settlement will do, so long as the inhabitants speak the imperial language.[if:convo_focused_voice]"))
			.NpcLine(new TextObject("{=fprOWs1E}Now, when you are ready to declare your new kingdom, instruct the governor of your capital to have a proclamation read out throughout your lands."))
			.NpcLine(new TextObject("{=tkJD40hE}Well, that should keep you busy for a while. Come back when you are ready."))
			.GotoDialogState(oState)
			.PlayerOption(new TextObject("{=tRzjuX0E}How can I support an existing imperial claimant?"))
			.NpcLine(new TextObject("{=oL9BdThD}Choose one and pledge allegiance. When this civil war began, I was a bit torn... Rhagaea was the cleverest ruler, Garios probably the best fighter, and Lucon seemed to have the best grasp of our laws and traditions. But you can make up your own mind."))
			.NpcLine(new TextObject("{=eaxOH9mb}My little birds will tell me once you pledge your support...[if:convo_nonchalant]"))
			.GotoDialogState(oState)
			.PlayerOption(new TextObject("{=6LQUuQhV}Thank you for your precious help."))
			.CloseDialog()
			.EndPlayerOptions()
			.CloseDialog();
	}

	private void GetImperialQuests()
	{
		_talkedWithImperialMentor = true;
		if (!_talkedWithAntiImperialMentor)
		{
			AddLog(_talkedWithImperialMentorButNotWithAntiImperialMentorQuestLog);
		}
		else
		{
			AddLog(_talkedWithImperialMentorQuestLog);
		}
		if (IsTracked(StoryModeHeroes.ImperialMentor.CurrentSettlement))
		{
			RemoveTrackedObject(StoryModeHeroes.ImperialMentor.CurrentSettlement);
		}
		new CreateKingdomQuest(StoryModeHeroes.ImperialMentor).StartQuest();
		new SupportKingdomQuest(StoryModeHeroes.ImperialMentor).StartQuest();
	}

	internal static void AutoGeneratedStaticCollectObjectsAssembleTheBannerQuest(object o, List<object> collectedObjects)
	{
		((AssembleTheBannerQuest)o).AutoGeneratedInstanceCollectObjects(collectedObjects);
	}

	protected override void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects)
	{
		base.AutoGeneratedInstanceCollectObjects(collectedObjects);
		collectedObjects.Add(_startLog);
	}

	internal static object AutoGeneratedGetMemberValue_startLog(object o)
	{
		return ((AssembleTheBannerQuest)o)._startLog;
	}

	internal static object AutoGeneratedGetMemberValue_talkedWithImperialMentor(object o)
	{
		return ((AssembleTheBannerQuest)o)._talkedWithImperialMentor;
	}

	internal static object AutoGeneratedGetMemberValue_talkedWithAntiImperialMentor(object o)
	{
		return ((AssembleTheBannerQuest)o)._talkedWithAntiImperialMentor;
	}
}
