using System.Collections.Generic;
using Helpers;
using StoryMode.StoryModeObjects;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.SceneInformationPopupTypes;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.SaveSystem;

namespace StoryMode.Quests.FirstPhase;

public class SupportKingdomQuest : StoryModeQuestBase
{
	[SaveableField(1)]
	private bool _isImperial;

	public override TextObject Title
	{
		get
		{
			TextObject textObject = new TextObject("{=XtC0hXhr}Support {?IS_IMPERIAL}an Imperial Faction{?}a Non-Imperial Kingdom{\\?}");
			textObject.SetTextVariable("IS_IMPERIAL", _isImperial ? 1 : 0);
			return textObject;
		}
	}

	private TextObject _onQuestStartedImperialLogText
	{
		get
		{
			TextObject textObject = new TextObject("{=TZZX9kWf}{MENTOR.LINK} suggested that you should support an imperial faction by offering them the Dragon Banner.");
			StringHelpers.SetCharacterProperties("MENTOR", StoryModeHeroes.ImperialMentor.CharacterObject, textObject);
			return textObject;
		}
	}

	private TextObject _onQuestStartedAntiImperialLogText
	{
		get
		{
			TextObject textObject = new TextObject("{=4d5SP6B6}{MENTOR.LINK} suggested that you should support an anti-imperial kingdom by offering them the Dragon Banner.");
			StringHelpers.SetCharacterProperties("MENTOR", StoryModeHeroes.AntiImperialMentor.CharacterObject, textObject);
			return textObject;
		}
	}

	private TextObject _onImperialKingdomSupportedLogText
	{
		get
		{
			TextObject textObject = new TextObject("{=atUTLABh}You have chosen to support the {KINGDOM} by presenting them the Dragon Banner, taking the advice of {MENTOR.LINK}.");
			StringHelpers.SetCharacterProperties("MENTOR", StoryModeHeroes.ImperialMentor.CharacterObject, textObject);
			textObject.SetTextVariable("KINGDOM", Clan.PlayerClan.Kingdom.EncyclopediaLinkWithName);
			return textObject;
		}
	}

	private TextObject _onAntiImperialKingdomSupportedLogText
	{
		get
		{
			TextObject textObject = new TextObject("{=atUTLABh}You have chosen to support the {KINGDOM} by presenting them the Dragon Banner, taking the advice of {MENTOR.LINK}.");
			StringHelpers.SetCharacterProperties("MENTOR", StoryModeHeroes.AntiImperialMentor.CharacterObject, textObject);
			textObject.SetTextVariable("KINGDOM", Clan.PlayerClan.Kingdom.EncyclopediaLinkWithName);
			return textObject;
		}
	}

	private TextObject _onPlayerRuledKingdomSupportedLogText => new TextObject("{=kqj1Wp0f}You have decided to keep the Dragon Banner within the kingdom you are ruling.");

	private TextObject _questFailedLogText => new TextObject("{=tVlZTOst}You have chosen a different path.");

	public override bool IsRemainingTimeHidden => false;

	public SupportKingdomQuest(Hero questGiver)
		: base("main_storyline_support_kingdom_quest_" + ((StoryModeHeroes.ImperialMentor == questGiver) ? "1" : "0"), questGiver, StoryModeManager.Current.MainStoryLine.FirstPhase.FirstPhaseEndTime)
	{
		_isImperial = StoryModeHeroes.ImperialMentor == questGiver;
		SetDialogs();
		if (_isImperial)
		{
			AddLog(_onQuestStartedImperialLogText);
			Campaign.Current.ConversationManager.AddDialogFlow(GetImperialKingDialogueFlow(), this);
			Campaign.Current.ConversationManager.AddDialogFlow(GetImperialMentorDialogueFlow(), this);
		}
		else
		{
			AddLog(_onQuestStartedAntiImperialLogText);
			Campaign.Current.ConversationManager.AddDialogFlow(GetAntiImperialKingDialogueFlow(), this);
			Campaign.Current.ConversationManager.AddDialogFlow(GetAntiImperialMentorDialogueFlow(), this);
		}
		InitializeQuestOnCreation();
	}

	protected override void SetDialogs()
	{
		DiscussDialogFlow = DialogFlow.CreateDialogFlow("quest_discuss").NpcLine(new TextObject("{=9tpTkKdY}Tell me which path you choose when you've made progress.")).Condition(() => Hero.OneToOneConversationHero == base.QuestGiver)
			.CloseDialog();
	}

	private DialogFlow GetImperialKingDialogueFlow()
	{
		return DialogFlow.CreateDialogFlow("hero_main_options", 300).BeginPlayerOptions().PlayerSpecialOption(new TextObject("{=Ke7f4XSC}I present you with the Dragon Banner of Calradios."))
			.ClickableCondition(CheckConditionToSupportKingdom)
			.Condition(() => Hero.OneToOneConversationHero.Clan != null && Hero.OneToOneConversationHero.Clan.Kingdom != null && Hero.OneToOneConversationHero.Clan.Kingdom.Leader == Hero.OneToOneConversationHero && StoryModeData.IsKingdomImperial(Hero.OneToOneConversationHero.Clan.Kingdom))
			.NpcLine("{=PQgzfHLk}Well now. I had heard rumors that you had obtained this great artifact.[if:convo_nonchalant]")
			.NpcLine("{=ULn7iWlz}It will be a powerful tool in our hands. People will believe that the Heavens intend us to restore the Empire of Calradia.[if:convo_pondering]")
			.NpcLine("{=S1yCTPrL}This is one of the most valuable services anyone has ever done for me. I am very grateful.[if:convo_grateful]")
			.Consequence(delegate
			{
				OnKingdomSupported(Hero.OneToOneConversationHero.Clan.Kingdom, isImperial: true);
				if (PlayerEncounter.Current != null)
				{
					PlayerEncounter.LeaveEncounter = true;
				}
				TextObject textObject = new TextObject("{=IL4FcHXv}You've pledged your allegiance to the {KINGDOM_NAME}!");
				textObject.SetTextVariable("KINGDOM_NAME", Hero.OneToOneConversationHero.Clan.Kingdom.Name);
				MBInformationManager.AddQuickInformation(textObject);
			})
			.CloseDialog()
			.EndPlayerOptions()
			.CloseDialog();
	}

	private DialogFlow GetAntiImperialKingDialogueFlow()
	{
		return DialogFlow.CreateDialogFlow("hero_main_options", 300).BeginPlayerOptions().PlayerSpecialOption(new TextObject("{=Ke7f4XSC}I present you with the Dragon Banner of Calradios."))
			.ClickableCondition(CheckConditionToSupportKingdom)
			.Condition(() => Hero.OneToOneConversationHero.Clan != null && Hero.OneToOneConversationHero.Clan.Kingdom != null && Hero.OneToOneConversationHero.Clan.Kingdom.Leader == Hero.OneToOneConversationHero && !StoryModeData.IsKingdomImperial(Hero.OneToOneConversationHero.Clan.Kingdom))
			.NpcLine("{=PQgzfHLk}Well now. I had heard rumors that you had obtained this great artifact.[if:convo_nonchalant]")
			.NpcLine("{=4olAbDTq}It will be a powerful tool in our hands. People will believe that the Heavens have transferred dominion over Calradia from the Empire to us.[if:convo_pondering]")
			.NpcLine("{=S1yCTPrL}This is one of the most valuable services anyone has ever done for me. I am very grateful.[if:convo_grateful]")
			.Consequence(delegate
			{
				OnKingdomSupported(Hero.OneToOneConversationHero.Clan.Kingdom, isImperial: false);
				if (PlayerEncounter.Current != null)
				{
					PlayerEncounter.LeaveEncounter = true;
				}
				TextObject textObject = new TextObject("{=IL4FcHXv}You've pledged your allegiance to the {KINGDOM_NAME}!");
				textObject.SetTextVariable("KINGDOM_NAME", Hero.OneToOneConversationHero.Clan.Kingdom.Name);
				MBInformationManager.AddQuickInformation(textObject);
			})
			.CloseDialog()
			.EndPlayerOptions()
			.CloseDialog();
	}

	private DialogFlow GetImperialMentorDialogueFlow()
	{
		return DialogFlow.CreateDialogFlow("hero_main_options", 300).BeginPlayerOptions().PlayerSpecialOption(new TextObject("{=O2BAcMNO}As the legitimate {?PLAYER.GENDER}Empress{?}Emperor{\\?} of Calradia, I am ready to declare my ownership of the Dragon Banner."))
			.Condition(() => base.IsOngoing && Hero.OneToOneConversationHero == StoryModeHeroes.ImperialMentor)
			.NpcLine("{=ATduKfHu}This will make a great impression. It will attract allies, but also probably make you new enemies. Are you sure you're ready?[if:convo_undecided_closed]")
			.BeginPlayerOptions()
			.PlayerOption("{=n8pmVHNn}Yes, I am ready.")
			.ClickableCondition(CheckPlayerCanDeclareBannerOwnershipClickableCondition)
			.NpcLine("{=gL241Hoz}Very nice. Superstitious twaddle, of course, but people will believe you. Very well, oh heir to Calradios, go forth![if:convo_nonchalant]")
			.Consequence(delegate
			{
				OnKingdomSupported(Clan.PlayerClan.Kingdom, isImperial: true);
			})
			.CloseDialog()
			.PlayerOption("{=fRMIoPUK}Give me more time.")
			.NpcLine("{=KH07mJ5k}Very well, come back when you are ready.")
			.EndPlayerOptions()
			.CloseDialog()
			.PlayerOption("{=eYXLYgsC}I still am not sure what I will do with it.")
			.Condition(() => base.IsOngoing && Hero.OneToOneConversationHero == StoryModeHeroes.ImperialMentor)
			.NpcLine("{=UCoOMWaj}As I said before, there's a case for all of the claimants. When this war began, I thought Rhagaea understood best how to rule, Garios was the strongest warrior, and Lucon had the firmest grasp of our traditions.[if:convo_empathic_voice]")
			.NpcLine("{=uFsMzAuR}Speak to whichever one you choose, or come back to me if you wish to claim the banner for yourself.[if:convo_normal]")
			.CloseDialog()
			.EndPlayerOptions()
			.NpcLine("{=Z54ZrDG9}Until next time, then.")
			.CloseDialog();
	}

	private DialogFlow GetAntiImperialMentorDialogueFlow()
	{
		return DialogFlow.CreateDialogFlow("hero_main_options", 300).BeginPlayerOptions().PlayerSpecialOption(new TextObject("{=N5jJtZyr}As the Empire's nemesis, I am ready to declare my ownership of the Dragon Banner."))
			.Condition(() => base.IsOngoing && Hero.OneToOneConversationHero == StoryModeHeroes.AntiImperialMentor)
			.NpcLine("{=BXMKgTXl}This will make a great impression. It will attract allies, but also probably make you new enemies. Are you sure you're ready?[if:convo_astonished]")
			.BeginPlayerOptions()
			.PlayerOption("{=ALWqXMiP}Yes, I am sure.")
			.ClickableCondition(CheckPlayerCanDeclareBannerOwnershipClickableCondition)
			.NpcLine("{=exoZygYL}Very well. The Dragon Banner in your hands proclaims you the avenger of the Empire's crimes and its successor. Now go forth and claim your destiny![if:convo_calm_friendly]")
			.Consequence(delegate
			{
				OnKingdomSupported(Clan.PlayerClan.Kingdom, isImperial: false);
			})
			.CloseDialog()
			.PlayerOption("{=fRMIoPUK}Give me more time.")
			.NpcLine("{=YgoxFJSz}Very well, come back when you are ready.[if:convo_nonchalant]")
			.EndPlayerOptions()
			.CloseDialog()
			.PlayerOption("{=tzsZTcWd}I wonder which kingdom should I support..")
			.Condition(() => base.IsOngoing && Hero.OneToOneConversationHero == StoryModeHeroes.AntiImperialMentor)
			.NpcLine("{=1v6aYpDx}You must choose, but choose wisely. Or you can claim it yourself. I have no preference.")
			.GotoDialogState("hero_main_options")
			.EndPlayerOptions()
			.NpcLine("{=Z54ZrDG9}Until next time, then.")
			.CloseDialog();
	}

	private bool IsPlayerTheRulerOfAKingdom()
	{
		int num;
		if (Clan.PlayerClan.Kingdom != null && Clan.PlayerClan.Kingdom.Leader == Hero.MainHero)
		{
			num = ((StoryModeData.IsKingdomImperial(Clan.PlayerClan.Kingdom) == _isImperial) ? 1 : 0);
			if (num != 0)
			{
				MBTextManager.SetTextVariable("FACTION", Clan.PlayerClan.Kingdom.Name);
			}
		}
		else
		{
			num = 0;
		}
		return (byte)num != 0;
	}

	private bool CheckPlayerCanDeclareBannerOwnershipClickableCondition(out TextObject explanation)
	{
		if (IsPlayerTheRulerOfAKingdom())
		{
			explanation = null;
			return true;
		}
		explanation = (_isImperial ? new TextObject("{=mziMNKm2}You should be ruling a kingdom of the imperial culture.") : new TextObject("{=HCA9xOOo}You should be ruling a kingdom of non-imperial culture."));
		return false;
	}

	private bool CheckConditionToSupportKingdom(out TextObject explanation)
	{
		explanation = new TextObject("{=qNR8WKcX}You should join a kingdom before supporting it with the Dragon Banner.");
		if (Clan.PlayerClan.Kingdom == null || Clan.PlayerClan.Kingdom != Hero.OneToOneConversationHero.Clan.Kingdom)
		{
			return false;
		}
		return true;
	}

	private void OnKingdomSupported(Kingdom kingdom, bool isImperial)
	{
		if (isImperial)
		{
			if (kingdom.RulingClan == Clan.PlayerClan)
			{
				AddLog(_onPlayerRuledKingdomSupportedLogText);
				StoryModeManager.Current.MainStoryLine.SetStoryLineSide(MainStoryLineSide.CreateImperialKingdom);
				MBInformationManager.ShowSceneNotification(new DeclareDragonBannerSceneNotificationItem(playerWantsToRestore: true));
			}
			else
			{
				AddLog(_onImperialKingdomSupportedLogText);
				StoryModeManager.Current.MainStoryLine.SetStoryLineSide(MainStoryLineSide.SupportImperialKingdom);
				MBInformationManager.ShowSceneNotification(new PledgeAllegianceSceneNotificationItem(Hero.MainHero, playerWantsToRestore: true));
			}
		}
		else if (kingdom.RulingClan == Clan.PlayerClan)
		{
			AddLog(_onPlayerRuledKingdomSupportedLogText);
			StoryModeManager.Current.MainStoryLine.SetStoryLineSide(MainStoryLineSide.CreateAntiImperialKingdom);
			MBInformationManager.ShowSceneNotification(new DeclareDragonBannerSceneNotificationItem(playerWantsToRestore: false));
		}
		else
		{
			AddLog(_onAntiImperialKingdomSupportedLogText);
			StoryModeManager.Current.MainStoryLine.SetStoryLineSide(MainStoryLineSide.SupportAntiImperialKingdom);
			MBInformationManager.ShowSceneNotification(new PledgeAllegianceSceneNotificationItem(Hero.MainHero, playerWantsToRestore: false));
		}
		CompleteQuestWithSuccess();
	}

	private void MainStoryLineChosen(MainStoryLineSide chosenSide)
	{
		if ((_isImperial && chosenSide != MainStoryLineSide.SupportImperialKingdom && chosenSide != MainStoryLineSide.CreateImperialKingdom) || (!_isImperial && chosenSide != MainStoryLineSide.SupportAntiImperialKingdom && chosenSide != MainStoryLineSide.CreateAntiImperialKingdom))
		{
			CompleteQuestWithCancel(_questFailedLogText);
		}
	}

	protected override void HourlyTick()
	{
	}

	protected override void RegisterEvents()
	{
		StoryModeEvents.OnMainStoryLineSideChosenEvent.AddNonSerializedListener(this, MainStoryLineChosen);
	}

	protected override void InitializeQuestOnGameLoad()
	{
		SetDialogs();
		if (_isImperial)
		{
			Campaign.Current.ConversationManager.AddDialogFlow(GetImperialKingDialogueFlow(), this);
			Campaign.Current.ConversationManager.AddDialogFlow(GetImperialMentorDialogueFlow(), this);
		}
		else
		{
			Campaign.Current.ConversationManager.AddDialogFlow(GetAntiImperialKingDialogueFlow(), this);
			Campaign.Current.ConversationManager.AddDialogFlow(GetAntiImperialMentorDialogueFlow(), this);
		}
	}

	internal static void AutoGeneratedStaticCollectObjectsSupportKingdomQuest(object o, List<object> collectedObjects)
	{
		((SupportKingdomQuest)o).AutoGeneratedInstanceCollectObjects(collectedObjects);
	}

	protected override void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects)
	{
		base.AutoGeneratedInstanceCollectObjects(collectedObjects);
	}

	internal static object AutoGeneratedGetMemberValue_isImperial(object o)
	{
		return ((SupportKingdomQuest)o)._isImperial;
	}
}
