using System.Collections.Generic;
using System.Linq;
using Helpers;
using StoryMode.StoryModeObjects;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Localization;
using TaleWorlds.SaveSystem;

namespace StoryMode.Quests.FirstPhase;

public class MeetWithArzagosQuest : StoryModeQuestBase
{
	[SaveableField(1)]
	private bool _metAntiImperialMentor;

	private TextObject _startQuestLog
	{
		get
		{
			TextObject textObject = new TextObject("{=5K4wvz3w}Find and meet {HERO.LINK} to learn more about Neretzes' Banner. He is currently in {SETTLEMENT}.");
			StringHelpers.SetCharacterProperties("HERO", StoryModeHeroes.AntiImperialMentor.CharacterObject, textObject);
			textObject.SetTextVariable("SETTLEMENT", StoryModeHeroes.AntiImperialMentor.CurrentSettlement.EncyclopediaLinkWithName);
			return textObject;
		}
	}

	private TextObject _endQuestLog
	{
		get
		{
			TextObject textObject = new TextObject("{=qMUfOtyk}You talked with {HERO.LINK}.");
			StringHelpers.SetCharacterProperties("HERO", StoryModeHeroes.AntiImperialMentor.CharacterObject, textObject);
			return textObject;
		}
	}

	public override TextObject Title
	{
		get
		{
			TextObject textObject = new TextObject("{=Y6SqyQwn}Meet with {HERO.NAME}");
			StringHelpers.SetCharacterProperties("HERO", StoryModeHeroes.AntiImperialMentor.CharacterObject, textObject);
			return textObject;
		}
	}

	public override bool IsRemainingTimeHidden => false;

	public MeetWithArzagosQuest(Settlement settlement)
		: base("meet_with_arzagos_story_mode_quest", null, StoryModeManager.Current.MainStoryLine.FirstPhase.FirstPhaseEndTime)
	{
		_metAntiImperialMentor = false;
		SetDialogs();
		HeroHelper.SpawnHeroForTheFirstTime(StoryModeHeroes.AntiImperialMentor, settlement);
		AddTrackedObject(settlement);
		AddTrackedObject(StoryModeHeroes.AntiImperialMentor);
		AddLog(_startQuestLog);
	}

	protected override void InitializeQuestOnGameLoad()
	{
		SetDialogs();
	}

	protected override void HourlyTick()
	{
	}

	protected override void SetDialogs()
	{
		Campaign.Current.ConversationManager.AddDialogFlow(DialogFlow.CreateDialogFlow("lord_start", 110).NpcLine(new TextObject("{=unOLk4PY}So. Who are you, and what brings you to me?")).Condition(() => Hero.OneToOneConversationHero != null && Hero.OneToOneConversationHero == StoryModeHeroes.AntiImperialMentor && !_metAntiImperialMentor)
			.PlayerLine(new TextObject("{=tfm5hcks}I believe I have a piece of the Dragon Banner of Calradios."))
			.NpcLine(new TextObject("{=uvbCyLiR}Is that true? Well, that is interesting.[ib:normal][if:convo_astonished]"))
			.NpcLine(new TextObject("{=pOuGX9j0}You may have one piece of the banner, but it's of little use in itself. You'll have to find the other parts. But once you can bring together the pieces, you'll have something of tremendous value.[if:convo_pondering]"))
			.PlayerLine(new TextObject("{=t71lPdyb}How so?"))
			.NpcLine(new TextObject("{=SmVwMrUM}The banner of Calradios is part of a legend. It was said to be carried by Calradios the Great, who first led his people to this land, to conquer and despoil. The legend says that no army led by a true son of Calradios shall be defeated in battle.[ib:confident2][if:convo_thinking]"))
			.NpcLine(new TextObject("{=cNwejsNl}Convenient legend, eh? Of course the Calradians have been defeated many times, but I guess those were not 'true sons.' Still, you could say it represents the strength and endurance of this empire.[ib:normal][if:convo_focused_happy]"))
			.PlayerLine(new TextObject("{=FBp2ranI}So, can you help me find a buyer for it?"))
			.NpcLine(new TextObject("{=3G64Ej64}A buyer? I can help you do far more than that.[if:convo_astonished]"))
			.PlayerLine(new TextObject("{=MnmblprY}So, where can I find the other pieces?"))
			.NpcLine(new TextObject("{=Fgta5mF6}Before I answer, you and I need to know more about each other. I don't know what you know about me.  I was a citizen of the Empire. I was a commander in the imperial armies. But I am not imperial.[ib:confident][if:convo_thinking]"))
			.NpcLine(new TextObject("{=R5kLv5kg}I am what they call Palaic. Palaic is a language that is no longer spoken, except by a few old people. Even the word 'Palaic' is imperial. We are a people who have forgotten who we are.[if:convo_focused_voice]"))
			.NpcLine(new TextObject("{=cfTiiEEM}The Empire has a genius for destruction - the destruction of languages, traditions, gods. It takes our fortresses, slaughters our men, and turns our children into its own children.[if:convo_focused_voice]"))
			.NpcLine(new TextObject("{=qoA4UPly}Nothing can bring the Palaic people back. They are now imperial. But it is an insult to our name, to our gods, to our memory, that the state which destroyed our shrines and fortresses should last and thrive.[if:convo_empathic_voice]"))
			.NpcLine(new TextObject("{=rMem50oz}I have vowed that this Empire shall not survive this civil war, if I can do anything to stop it. And believe me, if I had that banner, there is very much something I could do.[if:convo_angry_voice]"))
			.Consequence(ActivateAssembleTheBannerQuest)
			.BeginPlayerOptions()
			.PlayerOption(new TextObject("{=tkXKef0Z}I too would see the empire destroyed."))
			.NpcLine(new TextObject("{=4RaspRbe}Good. Then I will tell you what I know. I heard about one other piece.[if:convo_calm_friendly]"))
			.NpcLine(new TextObject("{=4WZ9zJbF}I do not know where the other pieces are, you may need to keep searching for them.[if:convo_confused_normal]"))
			.NpcLine(new TextObject("{=kIDbW8fP}When you have recovered all pieces, return to me and I'll help you put them to use.[if:convo_calm_friendly]"))
			.Consequence(delegate
			{
				Campaign.Current.ConversationManager.ConversationEndOneShot += base.CompleteQuestWithSuccess;
			})
			.CloseDialog()
			.PlayerOption(new TextObject("{=gdgbaMOP}I am not sure I share your views."))
			.Consequence(delegate
			{
				_metAntiImperialMentor = true;
				Hero.OneToOneConversationHero.SetHasMet();
			})
			.NpcLine(new TextObject("{=7ULaG8aT}Then you can come back when you made your mind up.[ib:closed][if:convo_insulted]"))
			.CloseDialog()
			.EndPlayerOptions()
			.CloseDialog(), this);
		Campaign.Current.ConversationManager.AddDialogFlow(DialogFlow.CreateDialogFlow("lord_start", 110).NpcLine(new TextObject("{=bHveKDUI}So have you made up your mind now?[ib:closed][if:convo_nonchalant]")).Condition(() => Hero.OneToOneConversationHero != null && Hero.OneToOneConversationHero == StoryModeHeroes.AntiImperialMentor && _metAntiImperialMentor)
			.BeginPlayerOptions()
			.PlayerOption(new TextObject("{=upyNhwZ9}Yes, I intend to use the banner to help destroy the empire."))
			.NpcLine(new TextObject("{=TEgoba7R}Good. Then I will tell you what I know. I heard about one other piece.[ib:confident2][if:convo_calm_friendly]"))
			.NpcLine(new TextObject("{=ijyROgb4}I do not know where the other pieces are, you may need to keep searching for them.[if:convo_thinking]"))
			.NpcLine(new TextObject("{=kIDbW8fP}When you have recovered all pieces, return to me and I'll help you put them to use.[if:convo_calm_friendly]"))
			.Consequence(delegate
			{
				Campaign.Current.ConversationManager.ConversationEndOneShot += base.CompleteQuestWithSuccess;
			})
			.CloseDialog()
			.PlayerOption(new TextObject("{=ibm9EEPa}No, I need more time to decide."))
			.NpcLine(new TextObject("{=7ULaG8aT}Then you can come back when you made your mind up.[ib:closed][if:convo_insulted]"))
			.CloseDialog()
			.EndPlayerOptions()
			.CloseDialog(), this);
	}

	private void ActivateAssembleTheBannerQuest()
	{
		if (!Campaign.Current.QuestManager.Quests.Any((QuestBase q) => q is AssembleTheBannerQuest))
		{
			new AssembleTheBannerQuest().StartQuest();
		}
	}

	protected override void OnCompleteWithSuccess()
	{
		base.OnCompleteWithSuccess();
		AddLog(_endQuestLog);
	}

	internal static void AutoGeneratedStaticCollectObjectsMeetWithArzagosQuest(object o, List<object> collectedObjects)
	{
		((MeetWithArzagosQuest)o).AutoGeneratedInstanceCollectObjects(collectedObjects);
	}

	protected override void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects)
	{
		base.AutoGeneratedInstanceCollectObjects(collectedObjects);
	}

	internal static object AutoGeneratedGetMemberValue_metAntiImperialMentor(object o)
	{
		return ((MeetWithArzagosQuest)o)._metAntiImperialMentor;
	}
}
