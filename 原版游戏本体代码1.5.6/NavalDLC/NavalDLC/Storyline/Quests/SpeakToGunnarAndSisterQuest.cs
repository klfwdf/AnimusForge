using System.Linq;
using Helpers;
using NavalDLC.Storyline.MissionControllers;
using SandBox.Conversation.MissionLogics;
using StoryMode.StoryModeObjects;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;
using TaleWorlds.SaveSystem;

namespace NavalDLC.Storyline.Quests;

public class SpeakToGunnarAndSisterQuest : QuestBase
{
	private const string GunnarsLongshipStringId = "northern_medium_ship";

	private const string Tier3NordInfantryStringId = "nord_spear_warrior";

	private const string Tier4NordInfantryStringId = "nord_vargr";

	private const int Tier3NordInfantryCount = 10;

	private const int Tier4NordInfantryCount = 10;

	[SaveableField(1)]
	private Quest5SetPieceBattleMissionController.BossFightOutComeEnum _bossFightOutcome;

	private TextObject _startLog
	{
		get
		{
			TextObject textObject = new TextObject("{=vhqRTs5p}Look for {GUNNAR.NAME} and your sister in Ostican harbor.");
			textObject.SetCharacterProperties("GUNNAR", NavalStorylineData.Gunnar.CharacterObject);
			return textObject;
		}
	}

	public override TextObject Title => new TextObject("{=9VzikXB0}Speak to Gunnar and Your Sister");

	public override bool IsRemainingTimeHidden => true;

	public override string SpecialQuestType => "NavalStoryline";

	public SpeakToGunnarAndSisterQuest(Quest5SetPieceBattleMissionController.BossFightOutComeEnum bossFightOutcome)
		: base("naval_storyline_act3_quest5_end", NavalStorylineData.Gunnar, CampaignTime.Never, 0)
	{
		_bossFightOutcome = bossFightOutcome;
	}

	protected override void OnStartQuest()
	{
		InitializeDialogues();
		AddLog(_startLog);
		StoryModeHeroes.LittleSister.HitPoints = StoryModeHeroes.LittleSister.MaxHitPoints;
	}

	protected override void SetDialogs()
	{
	}

	protected override void InitializeQuestOnGameLoad()
	{
		if (_bossFightOutcome == Quest5SetPieceBattleMissionController.BossFightOutComeEnum.None || _bossFightOutcome == Quest5SetPieceBattleMissionController.BossFightOutComeEnum.PlayerDefeatedWaitingForConversation)
		{
			_bossFightOutcome = Quest5SetPieceBattleMissionController.BossFightOutComeEnum.PlayerRefusedTheDuel;
		}
		InitializeDialogues();
	}

	protected override void OnCompleteWithSuccess()
	{
		MakeGunnarNotable();
		NavalDLCHelpers.AddSisterToClan();
	}

	private void InitializeDialogues()
	{
		StringHelpers.SetCharacterProperties("PLAYER", CharacterObject.PlayerCharacter);
		DecideGunnarDialogue();
		DialogFlow dialogFlow = DialogFlow.CreateDialogFlow("start", 1500).NpcLine("{=!}{GUNNAR_FINAL_DIALOG_LINE_1}").Condition(() => Campaign.Current.QuestManager.IsThereActiveQuestWithType(typeof(SpeakToGunnarAndSisterQuest)) && Hero.OneToOneConversationHero == NavalStorylineData.Gunnar && Settlement.CurrentSettlement == NavalStorylineData.HomeSettlement)
			.Consequence(delegate
			{
				Mission.Current.GetMissionBehavior<MissionConversationLogic>()?.DisableStartConversation(isDisabled: true);
			})
			.NpcLine("{=!}{GUNNAR_FINAL_DIALOG_LINE_2}")
			.NpcLine("{=xxxjoDxM}My men, though... I've had a word with them, and some of them have been quite impressed by your leadership. They want to follow you, if you'll have them. And as I mentioned, they prefer to sail on our ship here, the Wave-Steed, so I guess that's yours too, if you'll have it. She'll carry you well, especially in the rough seas of the north.")
			.BeginPlayerOptions()
			.PlayerOption("{=qatVcvrX}I welcome your ship and crew.")
			.Consequence(OnPlayerWelcomedGunnarsCrew)
			.GotoDialogState("gunnar_final_dialog_token_1")
			.PlayerOption("{=FaZ1dSuh}I am honored, but I cannot take on your companions.")
			.GotoDialogState("gunnar_final_dialog_token_1")
			.EndPlayerOptions()
			.NpcLine("{=!}{GUNNAR_FINAL_DIALOG_LINE_3}", null, null, "gunnar_final_dialog_token_1")
			.BeginPlayerOptions()
			.PlayerOption("{=uh2W7Jh3}Farewell. Perhaps I will take you up on your reputation.")
			.GotoDialogState("gunnar_final_dialog_token_2")
			.PlayerOption("{=C94hXQp3}Farewell, and good hunting.")
			.GotoDialogState("gunnar_final_dialog_token_2")
			.EndPlayerOptions()
			.NpcLine("{=Vcr7BYxJ}Farewell, {PLAYER.NAME}.", null, null, "gunnar_final_dialog_token_2")
			.CloseDialog();
		DialogFlow dialogFlow2 = DialogFlow.CreateDialogFlow("start", 1200).NpcLine("{=L3NhSRHr}{PLAYER.NAME}... It's good to be free, and back on land. Things have changed so much though. Men follow you, and jump to their feet to obey your orders, and speak of your deeds...").Condition(delegate
		{
			int num;
			if (Hero.OneToOneConversationHero == StoryModeHeroes.LittleSister)
			{
				num = (Campaign.Current.QuestManager.IsThereActiveQuestWithType(typeof(SpeakToGunnarAndSisterQuest)) ? 1 : 0);
				if (num != 0)
				{
					StringHelpers.SetCharacterProperties("PLAYER", CharacterObject.PlayerCharacter);
					StringHelpers.SetCharacterProperties("BROTHER", StoryModeHeroes.ElderBrother.CharacterObject);
					StringHelpers.SetCharacterProperties("SISTER", StoryModeHeroes.LittleSister.CharacterObject);
					MBTextManager.SetTextVariable("CLAN_NAME", Clan.PlayerClan.Name);
				}
			}
			else
			{
				num = 0;
			}
			return (byte)num != 0;
		})
			.NpcLine("{=bqNHSlsb}One moment I am a slave and the next I seem to be some sort of noble lady... I need some time to rest. I will seek out our brother {BROTHER.NAME}.")
			.BeginPlayerOptions()
			.PlayerOption("{=VNEiqDzI}Of course, {SISTER.NAME}. Join {BROTHER.NAME}, and take all the time you need.")
			.GotoDialogState("sister_end_conversation_token")
			.PlayerOption("{=cESGiaPI}Things have indeed changed. Rest now, but remember that you are of the {CLAN_NAME}, and you must learn to command respect.")
			.GotoDialogState("sister_end_conversation_token")
			.EndPlayerOptions()
			.NpcLine("{=WFFv3fyb}Thank you again, {PLAYER.NAME}. I will pray nightly to Heaven for your safety.", null, null, "sister_end_conversation_token")
			.Consequence(SisterFinalConversationConsequence)
			.CloseDialog();
		Campaign.Current.ConversationManager.AddDialogFlow(dialogFlow);
		Campaign.Current.ConversationManager.AddDialogFlow(dialogFlow2);
	}

	private void DecideGunnarDialogue()
	{
		TextObject text;
		TextObject text2;
		if (_bossFightOutcome == Quest5SetPieceBattleMissionController.BossFightOutComeEnum.PlayerRefusedTheDuel)
		{
			text = new TextObject("{=JoBwweim}Well, {PLAYER.NAME}... Your sister is free, thank the gods. You gave Purig the death he deserved. None will mourn him.");
			text2 = new TextObject("{=bTCuEZW9}As for the Sea Hounds, I hear, they've mostly scattered. It's time for me to return to my home in Beinland. I've settled what I wish to settle, and all this rowing and ramming and climbing and jostling and fighting is hard on my old bones.");
		}
		else if (_bossFightOutcome == Quest5SetPieceBattleMissionController.BossFightOutComeEnum.PlayerAcceptedAndWonTheDuel)
		{
			text = new TextObject("{=AmwwLMvJ}Well, {PLAYER.NAME}... Your sister is free, thank the gods. You gave Purig a far more honorable death than he deserved. Men will speak well of you.");
			text2 = new TextObject("{=bTCuEZW9}As for the Sea Hounds, I hear, they've mostly scattered. It's time for me to return to my home in Beinland. I've settled what I wish to settle, and all this rowing and ramming and climbing and jostling and fighting is hard on my old bones.");
		}
		else if (_bossFightOutcome == Quest5SetPieceBattleMissionController.BossFightOutComeEnum.PlayerAcceptedTheDuelLostItAndLetPurigGo)
		{
			text = new TextObject("{=4rXR7jR9}Well, {PLAYER.NAME}... Your sister is free, thank the gods. Purig may have gotten away, but I doubt the Sea Hounds will be troubling us much more.");
			text2 = new TextObject("{=GqHo4JE2}It was an honorable thing, to duel him, and I am glad you kept your word to him, though he did not deserve it. For my part, though, I owe him nothing. I will continue to hunt him, and as it is much easier for him to evade a large group than a single hunter, I will do so alone.");
		}
		else
		{
			text = new TextObject("{=qGZZRhKj}Well, {PLAYER.NAME}... Your sister is free, thank the gods.  Purig is dead, and none will mourn him. I might that wish his death could have come some other way, but I will not dwell on it.");
			text2 = new TextObject("{=aJ8bK4oo}The Sea Hounds, I hear, they've mostly scattered. It's time for me to return to my home in Beinland. I've settled what I wish to settle, and all this rowing and ramming and climbing and jostling and fighting is hard on my old bones.");
		}
		TextObject text3 = ((_bossFightOutcome != Quest5SetPieceBattleMissionController.BossFightOutComeEnum.PlayerAcceptedTheDuelLostItAndLetPurigGo) ? new TextObject("{=IGnbxJHn}You should come see me in my village, Lagshofn, in Beinland. It's not much, not for a {?PLAYER.GENDER}warrior{?}man{\\?} like you, who's no doubt seen all the wonders of the Empire and the lands beyond, but we can pass a summer's night on the beach and drink to our deeds.") : new TextObject("{=1PPiv2ns}I suspect Purig will try to travel as far from these parts as possible. Perhaps deep into the south, or to the east... Perhaps I will take years to find him, or perhaps my old age will finally catch up to me on the road or on the seas. I do not know if we will meet again."));
		MBTextManager.SetTextVariable("GUNNAR_FINAL_DIALOG_LINE_1", text);
		MBTextManager.SetTextVariable("GUNNAR_FINAL_DIALOG_LINE_2", text2);
		MBTextManager.SetTextVariable("GUNNAR_FINAL_DIALOG_LINE_3", text3);
	}

	private void MakeGunnarNotable()
	{
		Village village = Village.All.FirstOrDefault((Village x) => x.Settlement.StringId == "village_N1_2");
		if (village != null)
		{
			TeleportHeroAction.ApplyImmediateTeleportToSettlement(NavalStorylineData.Gunnar, village.Settlement);
		}
	}

	private void OnPlayerWelcomedGunnarsCrew()
	{
		Ship ship = new Ship(MBObjectManager.Instance.GetObject<ShipHull>("northern_medium_ship"));
		ship.SetName(new TextObject("{=EUAsSTeT}Wave-Steed"));
		ChangeShipOwnerAction.ApplyByLooting(PartyBase.MainParty, ship);
		CharacterObject character = MBObjectManager.Instance.GetObject<CharacterObject>("nord_spear_warrior");
		MobileParty.MainParty.MemberRoster.AddToCounts(character, 10);
		CharacterObject character2 = MBObjectManager.Instance.GetObject<CharacterObject>("nord_vargr");
		MobileParty.MainParty.MemberRoster.AddToCounts(character2, 10);
		if (!MobileParty.MainParty.Anchor.IsValid && Settlement.CurrentSettlement != null && Settlement.CurrentSettlement.HasPort)
		{
			MobileParty.MainParty.Anchor.Settlement = Settlement.CurrentSettlement;
		}
		TextObject textObject = new TextObject("{=06sIBlHR}{NUMBER} troops and {SHIP_NAME} were added to your party.");
		textObject.SetTextVariable("NUMBER", 20);
		textObject.SetTextVariable("SHIP_NAME", ship.Name);
		InformationManager.DisplayMessage(new InformationMessage(textObject.ToString(), new Color(0f, 1f, 0f)));
	}

	private void SisterFinalConversationConsequence()
	{
		CompleteQuestWithSuccess();
		Mission.Current.GetMissionBehavior<MissionConversationLogic>()?.DisableStartConversation(isDisabled: false);
		Campaign.Current.ConversationManager.ConversationEndOneShot += delegate
		{
			CampaignMission.Current.EndMission();
		};
	}
}
