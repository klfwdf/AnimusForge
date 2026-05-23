using System;
using System.Collections.Generic;
using Helpers;
using StoryMode.Missions;
using StoryMode.StoryModePhases;
using Storymode.Missions;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;
using TaleWorlds.SaveSystem;

namespace StoryMode.Quests.TutorialPhase;

public class VillagersInNeed : StoryModeQuestBase
{
	public const string StealthEquipmentId = "stealth_tutorial_set_player";

	public const string VillaSceneId = "villa_singular_c";

	public const string HeadmanId = "tutorial_npc_captive_headman";

	private const string VillagerId = "tutorial_npc_questgiver_villager";

	[SaveableField(1)]
	private bool _talkedToVillagers;

	[SaveableField(2)]
	private bool _failedTheMission;

	private bool _startVillaMission;

	private bool _isHeadmanFollowing;

	private bool _rescuedHeadman;

	private static int SettlementBusyPriority => 400;

	public override TextObject Title => new TextObject("{=Cv2W7aFu}Villagers in Need");

	private TextObject _startQuestLogTutorialNotSkipped
	{
		get
		{
			TextObject textObject = new TextObject("{=sbX4fQ0R}A boy came to your camp and told you some of Radagos' men returned to {VILLAGE_LINK} and took the headman hostage.");
			textObject.SetTextVariable("VILLAGE_LINK", _village.EncyclopediaLinkWithName);
			return textObject;
		}
	}

	private TextObject _startQuestLogTutorialSkipped
	{
		get
		{
			TextObject textObject = new TextObject("{=Iu7tpHsO}A boy came to your camp and told you the villagers of {VILLAGE_LINK} need your help rescuing their headman from a group of bandits.");
			textObject.SetTextVariable("VILLAGE_LINK", _village.EncyclopediaLinkWithName);
			return textObject;
		}
	}

	private Settlement _village => Settlement.Find("village_ES3_2");

	public CharacterObject Headman => MBObjectManager.Instance.GetObject<CharacterObject>("tutorial_npc_captive_headman");

	private CharacterObject _villager => MBObjectManager.Instance.GetObject<CharacterObject>("tutorial_npc_questgiver_villager");

	public VillagersInNeed()
		: base("talk_to_villagers_in_village_quest", null, CampaignTime.Never)
	{
		AddTrackedObject(_village);
		SetDialogs();
		AddGameMenus();
		InitializeQuestOnCreation();
	}

	protected override void InitializeQuestOnGameLoad()
	{
		SetDialogs();
	}

	protected override void OnStartQuest()
	{
		AddLog(StoryMode.StoryModePhases.TutorialPhase.Instance.IsSkipped ? _startQuestLogTutorialSkipped : _startQuestLogTutorialNotSkipped);
		Hero.AllAliveHeroes.GetRandomElementWithPredicate((Hero t) => t.Occupation == Occupation.Headman && t.Culture == _village.Culture);
	}

	protected override void RegisterEvents()
	{
		CampaignEvents.SettlementEntered.AddNonSerializedListener(this, OnSettlementEntered);
		CampaignEvents.GameMenuOpened.AddNonSerializedListener(this, GameMenuOpened);
		CampaignEvents.OnGameLoadFinishedEvent.AddNonSerializedListener(this, OnGameLoadFinished);
		CampaignEvents.OnMissionEndedEvent.AddNonSerializedListener(this, OnMissionEnded);
		CampaignEvents.IsSettlementBusyEvent.AddNonSerializedListener(this, IsSettlementBusy);
	}

	private void IsSettlementBusy(Settlement settlement, object asker, ref int priority)
	{
		if (settlement == _village && asker != this)
		{
			priority = Math.Max(priority, SettlementBusyPriority);
		}
	}

	private void OnMissionEnded(IMission mission)
	{
		_isHeadmanFollowing = false;
	}

	private void GameMenuOpened(MenuCallbackArgs args)
	{
		if (Settlement.CurrentSettlement == _village)
		{
			if (args.MenuContext.GameMenu.StringId == "village" && _startVillaMission)
			{
				StartVillaMission();
			}
			if (args.MenuContext.GameMenu.StringId == "village" && _rescuedHeadman)
			{
				OpenConversationWithHeadman();
			}
		}
	}

	private void OnGameLoadFinished()
	{
		AddGameMenus();
	}

	private void AddGameMenus()
	{
		AddGameMenuOption("village", "talk_to_villager", new TextObject("{=Q5jUW8Oa}Talk to the villager"), village_talk_to_villager_on_condition, village_talk_to_villager_on_consequence, Isleave: false, 4);
	}

	private void village_talk_to_villager_on_consequence(MenuCallbackArgs args)
	{
		OpenConversationWithVillager();
	}

	private bool village_talk_to_villager_on_condition(MenuCallbackArgs args)
	{
		args.OptionQuestData = GameMenuOption.IssueQuestFlags.ActiveStoryQuest;
		args.optionLeaveType = GameMenuOption.LeaveType.Conversation;
		if (Hero.MainHero.IsWounded)
		{
			args.IsEnabled = false;
			args.Tooltip = new TextObject("{=yNMrF2QF}You are wounded");
		}
		if (Settlement.CurrentSettlement == _village)
		{
			return _talkedToVillagers;
		}
		return false;
	}

	private void OnSettlementEntered(MobileParty party, Settlement settlement, Hero hero)
	{
		if (!_talkedToVillagers && settlement == _village && party == MobileParty.MainParty)
		{
			OpenConversationWithVillager();
		}
	}

	private void OpenConversationWithVillager()
	{
		CampaignMission.OpenConversationMission(new ConversationCharacterData(CharacterObject.PlayerCharacter, null, noHorse: true), new ConversationCharacterData(_villager, null, noHorse: true, noWeapon: true));
	}

	private void OpenConversationWithHeadman()
	{
		CampaignMission.OpenConversationMission(new ConversationCharacterData(CharacterObject.PlayerCharacter, null, noHorse: true), new ConversationCharacterData(Headman, null, noHorse: true, noWeapon: true));
	}

	protected override void SetDialogs()
	{
		Campaign.Current.ConversationManager.AddDialogFlow(DialogFlow.CreateDialogFlow("start", 1000010).NpcLine(new TextObject("{=!}{VILLAGER_DIALOGUE_1}")).Condition(talk_to_villagers_on_condition)
			.NpcLine(new TextObject("{=!}{VILLAGER_DIALOGUE_2}"))
			.NpcLine(new TextObject("{=vxYaxWwC}They've holed up in a ruined villa a short distance from here, and say that if we try to rescue the headman they'll cut his throat then and there. But surely you could save him? You could sneak in there and get him out?"))
			.GenerateToken(out var token)
			.BeginPlayerOptions()
			.PlayerOption(new TextObject("{=3sI7nPbF}Hmm. I guess it's mostly a matter of waiting until their back is turned, then moving quickly from cover to cover."), null, null, token)
			.PlayerOption(new TextObject("{=mFTTWH03}I shall pass through them unseen, cloaked in silence and shadow."), null, null, token)
			.EndPlayerOptions()
			.NpcLine(new TextObject("{=n5fELaJd}We can give you some things that might help you. We have some special, softer boots, that our hunters use when they go out at night - when you walk, you'll barely make any noise at all. And some darkened clothes. We have some that would fit you."), null, null, token)
			.NpcLine(new TextObject("{=3ee3I8WX}Also, this dagger... It's probably safest just to get to the headman as stealthily as you can, and then sneak back out. But if there's just no getting around one of them, you can take this and come up behind him, and that would make a lot less noise than a straight-out fight."))
			.GenerateToken(out var token2)
			.BeginPlayerOptions()
			.PlayerOption(new TextObject("{=HpYpZEt3}Right. I'll don those hunting clothes and see what I can do."), null, null, token2)
			.PlayerOption(new TextObject("{=q4bjsoKj}Let this dagger be the hand of the angel of death."), null, null, token2)
			.EndPlayerOptions()
			.NpcLine(new TextObject("{=z9prKkWu}Thank you, my {?PLAYER.GENDER}lady{?}lord{\\?}. I'll take you right now to the villa. This is a good time - they took some wine when they raided us, and I doubt they'll be on their guard."), null, null, token2)
			.Consequence(talk_to_villagers_not_skipped_on_consequence)
			.BeginPlayerOptions()
			.PlayerOption(new TextObject("{=glarczej}Lead on."))
			.Consequence(delegate
			{
				_startVillaMission = true;
			})
			.CloseDialog()
			.PlayerOption(new TextObject("{=nhSLTzHk}I have to take care of something else, first."))
			.CloseDialog()
			.EndPlayerOptions(), this);
		Campaign.Current.ConversationManager.AddDialogFlow(DialogFlow.CreateDialogFlow("start", 1000010).NpcLine(new TextObject("{=AZ30Q0nM}Heaven bless you, {?PLAYER.GENDER}madame{?}sir{\\?}. Shall I take you to where the bandits are holding the headman?")).Condition(talk_to_villagers_later_on_condition)
			.BeginPlayerOptions()
			.PlayerOption(new TextObject("{=glarczej}Lead on."))
			.Consequence(delegate
			{
				_startVillaMission = true;
			})
			.CloseDialog()
			.PlayerOption(new TextObject("{=nhSLTzHk}I have to take care of something else, first."))
			.CloseDialog()
			.EndPlayerOptions(), this);
		Campaign.Current.ConversationManager.AddDialogFlow(DialogFlow.CreateDialogFlow("start", 1000010).NpcLine(new TextObject("{=IGdpap9P}We saw what happened, {?PLAYER.GENDER}madame{?}sir{\\?}, but we think that drunken lot have all gone to sleep again. Maybe you could try again, {?PLAYER.GENDER}madame{?}sir{\\?}? We would be forever in your debt.")).Condition(talk_to_villagers_failed_on_condition)
			.BeginPlayerOptions()
			.PlayerOption(new TextObject("{=glarczej}Lead on."))
			.Consequence(delegate
			{
				_startVillaMission = true;
				_failedTheMission = false;
			})
			.NpcLine(new TextObject("{=76acv5m2}Whatever happens, we're forever in your debt."))
			.CloseDialog()
			.PlayerOption(new TextObject("{=nhSLTzHk}I have to take care of something else, first."))
			.NpcLine(new TextObject("{=krsbwYax}Come find us here in Tevea when you're ready, {?PLAYER.GENDER}madame{?}sir{\\?}."))
			.CloseDialog()
			.EndPlayerOptions(), this);
		Campaign.Current.ConversationManager.AddDialogFlow(DialogFlow.CreateDialogFlow("start", 1000010).NpcLine(new TextObject("{=7aAFEx7e}You're not one of them! Who are you? What's happening?")).Condition(talk_to_headman_in_villa_skipped_on_condition)
			.BeginPlayerOptions()
			.PlayerOption(new TextObject("{=bcZaWOZM}I'll find a way out. Follow me as soon as it’s safe."))
			.Consequence(talk_to_headman_in_villa_on_consequence)
			.CloseDialog()
			.PlayerOption(new TextObject("{=nfMWzDbw}Be silent! I shall clear a path past them."))
			.Consequence(talk_to_headman_in_villa_on_consequence)
			.CloseDialog()
			.EndPlayerOptions(), this);
		Campaign.Current.ConversationManager.AddDialogFlow(DialogFlow.CreateDialogFlow("start", 1000010).NpcLine(new TextObject("{=dykrJl5v}{PLAYER.NAME}! What's happening?")).Condition(talk_to_headman_in_villa_not_skipped_on_condition)
			.BeginPlayerOptions()
			.PlayerOption(new TextObject("{=bcZaWOZM}I'll find a way out. Follow me as soon as it’s safe."))
			.Consequence(talk_to_headman_in_villa_on_consequence)
			.CloseDialog()
			.PlayerOption(new TextObject("{=nfMWzDbw}Be silent! I shall clear a path past them."))
			.Consequence(talk_to_headman_in_villa_on_consequence)
			.CloseDialog()
			.EndPlayerOptions(), this);
		Campaign.Current.ConversationManager.AddDialogFlow(DialogFlow.CreateDialogFlow("start", 1000010).NpcLine(new TextObject("{=bN3zJKz5}As soon as you find an escape route, I will follow.")).Condition(talk_to_headman_in_villa_after_talking_on_condition)
			.CloseDialog(), this);
		Campaign.Current.ConversationManager.AddDialogFlow(DialogFlow.CreateDialogFlow("start", 1000010).NpcLine(new TextObject("{=!}{HEADMAN_DIALOGUE_1}")).Condition(talk_to_headman_after_rescue_on_condition)
			.GenerateToken(out var token3)
			.BeginPlayerOptions()
			.PlayerOption(new TextObject("{=xJUJmrTb}I'm always glad to help honest folk like yourselves."), null, null, token3)
			.PlayerOption(new TextObject("{=y3gl2ada}Perhaps you would like to express a more tangible form of gratitude."), null, null, token3)
			.EndPlayerOptions()
			.NpcLine(new TextObject("{=!}{HEADMAN_DIALOGUE_2}"), null, null, token3)
			.GenerateToken(out var token4)
			.BeginPlayerOptions()
			.PlayerOption(new TextObject("{=gLVaQeAL}I'll take it. I need whatever I can get."), null, null, token4)
			.Consequence(delegate
			{
				Campaign.Current.ConversationManager.ConversationEndOneShot += TakeRewards;
			})
			.PlayerOption(new TextObject("{=xj5dlLXa}Keep your money, my good man. You've lost too much already."), null, null, token4)
			.EndPlayerOptions()
			.NpcLine(new TextObject("{=x3vZ8iQC}Then thank you again. We here in Tevea won't forget what you've done for us."), null, null, token4)
			.Consequence(base.CompleteQuestWithSuccess)
			.CloseDialog(), this);
	}

	protected override void OnCompleteWithSuccess()
	{
		TextObject textObject = new TextObject("{=LcPHw4m2}You rescued the headman from the bandits, and returned him to the village of {VILLAGE_LINK}.");
		textObject.SetTextVariable("VILLAGE_LINK", _village.EncyclopediaLinkWithName);
		AddLog(textObject);
		MBEquipmentRoster mBEquipmentRoster = MBObjectManager.Instance.GetObject<MBEquipmentRoster>("stealth_tutorial_set_player");
		for (int i = 0; i < 12; i++)
		{
			if (!mBEquipmentRoster.DefaultEquipment[i].IsEmpty)
			{
				MobileParty.MainParty.ItemRoster.AddToCounts(mBEquipmentRoster.DefaultEquipment[i], 1);
			}
		}
	}

	private void talk_to_headman_in_villa_on_consequence()
	{
		Mission.Current.GetMissionBehavior<SneakIntoTheVillaMissionController>().OnAfterTalkingToPrisoner();
		_isHeadmanFollowing = true;
	}

	private bool talk_to_headman_in_villa_on_condition()
	{
		int num;
		if (CharacterObject.OneToOneConversationCharacter == Headman && _talkedToVillagers && !_isHeadmanFollowing)
		{
			num = ((!_rescuedHeadman) ? 1 : 0);
			if (num != 0)
			{
				StringHelpers.SetCharacterProperties("PLAYER", CharacterObject.PlayerCharacter);
			}
		}
		else
		{
			num = 0;
		}
		return (byte)num != 0;
	}

	private bool talk_to_headman_in_villa_after_talking_on_condition()
	{
		int num;
		if (CharacterObject.OneToOneConversationCharacter == Headman && _talkedToVillagers && _isHeadmanFollowing)
		{
			num = ((!_rescuedHeadman) ? 1 : 0);
			if (num != 0)
			{
				StringHelpers.SetCharacterProperties("PLAYER", CharacterObject.PlayerCharacter);
			}
		}
		else
		{
			num = 0;
		}
		return (byte)num != 0;
	}

	private bool talk_to_headman_in_villa_skipped_on_condition()
	{
		if (talk_to_headman_in_villa_on_condition())
		{
			return StoryMode.StoryModePhases.TutorialPhase.Instance.IsSkipped;
		}
		return false;
	}

	private bool talk_to_headman_in_villa_not_skipped_on_condition()
	{
		if (talk_to_headman_in_villa_on_condition())
		{
			return !StoryMode.StoryModePhases.TutorialPhase.Instance.IsSkipped;
		}
		return false;
	}

	private void TakeRewards()
	{
		GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, 100);
	}

	private bool talk_to_headman_after_rescue_on_condition()
	{
		int num;
		if (CharacterObject.OneToOneConversationCharacter == Headman && _talkedToVillagers)
		{
			num = (_rescuedHeadman ? 1 : 0);
			if (num != 0)
			{
				if (StoryMode.StoryModePhases.TutorialPhase.Instance.IsSkipped)
				{
					MBTextManager.SetTextVariable("HEADMAN_DIALOGUE_1", new TextObject("{=sbqpaU64}Thank you, {?PLAYER.GENDER}madame{?}sir{\\?}! You saved my life, and put the fear of Heaven into those villains, I'm sure!  We can handle things from here. There's just a few of them left, and we won't let them take us unaware again. With all our hearts, thank you."));
				}
				else
				{
					MBTextManager.SetTextVariable("HEADMAN_DIALOGUE_1", new TextObject("{=L5KshziU}{PLAYER.NAME}... Once again, you've helped us fend off those villains. We can handle things from here, I'm sure. There's just a few of them left, and we won't let them take us unaware again. Thank you. With all our hearts, thank you."));
				}
				if (StoryMode.StoryModePhases.TutorialPhase.Instance.IsSkipped)
				{
					MBTextManager.SetTextVariable("HEADMAN_DIALOGUE_2", new TextObject("{=9IHnjuXb}We'd heard that you're trying to find your family. Our heart goes out to you, {?PLAYER.GENDER}madame{?}sir{\\?}. That dagger and those hunting clothes - please take them. We pray they can be of use. And I have 100 denars that I'd been saving, but I want you to have it. If it helps you at all, I'd be glad."));
				}
				else
				{
					MBTextManager.SetTextVariable("HEADMAN_DIALOGUE_2", new TextObject("{=LRkKxcmX}We know you've got a long road ahead of you, trying to find your family. That dagger and those hunting clothes - please take them. We pray they can be of use. And I have 100 denars that I'd been saving, but I want you to have it. If it helps you at all to find your poor brother and sister, I'd be glad."));
				}
				StringHelpers.SetCharacterProperties("PLAYER", CharacterObject.PlayerCharacter);
			}
		}
		else
		{
			num = 0;
		}
		return (byte)num != 0;
	}

	private void talk_to_villagers_not_skipped_on_consequence()
	{
		TextObject text = new TextObject("{=4ezrToWI}You agreed to help the villagers and try to save their headman from a nearby villa.");
		AddLog(text);
		_talkedToVillagers = true;
	}

	private void StartVillaMission()
	{
		StoryModeMissions.OpenSneakIntoTheVillaMission("villa_singular_c", CampaignTime.Now);
		_startVillaMission = false;
	}

	private bool talk_to_villagers_later_on_condition()
	{
		int num;
		if (Mission.Current != null && CharacterObject.OneToOneConversationCharacter != null && Settlement.CurrentSettlement == _village && !_failedTheMission && _talkedToVillagers)
		{
			num = ((CharacterObject.OneToOneConversationCharacter == _villager) ? 1 : 0);
			if (num != 0)
			{
				StringHelpers.SetCharacterProperties("PLAYER", CharacterObject.PlayerCharacter);
			}
		}
		else
		{
			num = 0;
		}
		return (byte)num != 0;
	}

	private bool talk_to_villagers_failed_on_condition()
	{
		int num;
		if (Mission.Current != null && CharacterObject.OneToOneConversationCharacter != null && Settlement.CurrentSettlement == _village && _talkedToVillagers && _failedTheMission)
		{
			num = ((CharacterObject.OneToOneConversationCharacter == _villager) ? 1 : 0);
			if (num != 0)
			{
				StringHelpers.SetCharacterProperties("PLAYER", CharacterObject.PlayerCharacter);
			}
		}
		else
		{
			num = 0;
		}
		return (byte)num != 0;
	}

	private bool talk_to_villagers_on_condition()
	{
		int num;
		if (Mission.Current != null && CharacterObject.OneToOneConversationCharacter != null && Settlement.CurrentSettlement == _village && !_talkedToVillagers)
		{
			num = ((CharacterObject.OneToOneConversationCharacter == _villager) ? 1 : 0);
			if (num != 0)
			{
				if (StoryMode.StoryModePhases.TutorialPhase.Instance.IsSkipped)
				{
					MBTextManager.SetTextVariable("VILLAGER_DIALOGUE_1", new TextObject("{=toszqdCj}Thank Heaven our lad found you! Please, {?PLAYER.GENDER}madame{?}sir{\\?}, we'd heard about that terrible affair at the inn, and that a couple of warriors were planning to track those killers down. Are you one of them? We thought you could help us."));
				}
				else
				{
					MBTextManager.SetTextVariable("VILLAGER_DIALOGUE_1", new TextObject("{=PkoWqYPD}Thank Heaven our lad found you! Please, {?PLAYER.GENDER}madame{?}sir{\\?}, you've done so much for us, but we beg you not to forsake us now."));
				}
				if (StoryMode.StoryModePhases.TutorialPhase.Instance.IsSkipped)
				{
					MBTextManager.SetTextVariable("VILLAGER_DIALOGUE_2", new TextObject("{=55avOQ4k}Listen, {?PLAYER.GENDER}madame{?}sir{\\?}... It seems like a small group of bandits broke off from the main group, and now they have our headman. They're demanding a ransom - a half-dozen horses and ten sacks of grain. After all their theft and villainy we have no horses at all, sirs, and the grain would leave us nothing to plant!"));
				}
				else
				{
					MBTextManager.SetTextVariable("VILLAGER_DIALOGUE_2", new TextObject("{=datALLCZ}When our lads came back, and said you'd led them to victory over Radagos and his gang, we thought the danger had passed. But it looks like we rejoiced too soon. A few desperate bandits got away, and now they have our headman. They're demanding a ransom - a half-dozen horses and ten sacks of grain. After all their theft and villainy we have no horses at all, sirs, and the grain would leave us nothing to plant!"));
				}
				StringHelpers.SetCharacterProperties("PLAYER", CharacterObject.PlayerCharacter);
			}
		}
		else
		{
			num = 0;
		}
		return (byte)num != 0;
	}

	public void OnRescueMissionFailed()
	{
		_failedTheMission = true;
	}

	public void OnHeadmanRescued()
	{
		_rescuedHeadman = true;
	}

	internal static void AutoGeneratedStaticCollectObjectsVillagersInNeed(object o, List<object> collectedObjects)
	{
		((VillagersInNeed)o).AutoGeneratedInstanceCollectObjects(collectedObjects);
	}

	protected override void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects)
	{
		base.AutoGeneratedInstanceCollectObjects(collectedObjects);
	}

	internal static object AutoGeneratedGetMemberValue_talkedToVillagers(object o)
	{
		return ((VillagersInNeed)o)._talkedToVillagers;
	}

	internal static object AutoGeneratedGetMemberValue_failedTheMission(object o)
	{
		return ((VillagersInNeed)o)._failedTheMission;
	}
}
