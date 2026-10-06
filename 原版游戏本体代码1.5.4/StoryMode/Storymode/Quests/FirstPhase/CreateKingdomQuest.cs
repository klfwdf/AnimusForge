using System.Collections.Generic;
using System.Linq;
using Helpers;
using StoryMode.StoryModeObjects;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.SaveSystem;

namespace StoryMode.Quests.FirstPhase;

public class CreateKingdomQuest : StoryModeQuestBase
{
	[SaveableField(1)]
	private readonly bool _isImperial;

	private const int PartySizeRequirement = 100;

	private const int SettlementCountRequirement = 1;

	[SaveableField(2)]
	private bool _hasPlayerCreatedKingdom;

	[SaveableField(9)]
	private JournalLog _leftKingdomLog;

	[SaveableField(10)]
	private Kingdom _playerCreatedKingdom;

	[SaveableField(4)]
	private readonly JournalLog _clanTierRequirementLog;

	[SaveableField(5)]
	private readonly JournalLog _partySizeRequirementLog;

	[SaveableField(6)]
	private readonly JournalLog _settlementOwnershipRequirementLog;

	[SaveableField(7)]
	private readonly JournalLog _clanIndependenceRequirementLog;

	private TextObject _onQuestStartedImperialLogText
	{
		get
		{
			TextObject textObject = new TextObject("{=N5Qg5ick}You told {MENTOR.LINK} that you will create your own imperial faction. You can do that by speaking to one of your governors once you fulfill the requirements. {?MENTOR.GENDER}She{?}He{\\?} expects to talk to you once you succeed.");
			StringHelpers.SetCharacterProperties("MENTOR", StoryModeHeroes.ImperialMentor.CharacterObject, textObject);
			return textObject;
		}
	}

	private TextObject _onQuestStartedAntiImperialLogText
	{
		get
		{
			TextObject textObject = new TextObject("{=AxKDQJ4G}You told {MENTOR.LINK} that you will create your own kingdom to defeat the Empire. You can do that by speaking to one of your governors once you fulfill the requirements. {?MENTOR.GENDER}She{?}He{\\?} expects to talk to you once you succeed.");
			StringHelpers.SetCharacterProperties("MENTOR", StoryModeHeroes.AntiImperialMentor.CharacterObject, textObject);
			return textObject;
		}
	}

	private TextObject _imperialKingdomCreatedLogText
	{
		get
		{
			TextObject textObject = new TextObject("{=UnjgFmnE}Heeding the advice of {MENTOR.LINK}, you have created an imperial faction. You can tell {?MENTOR.GENDER}her{?}him{\\?} that you will support your own kingdom.");
			StringHelpers.SetCharacterProperties("MENTOR", StoryModeHeroes.ImperialMentor.CharacterObject, textObject);
			return textObject;
		}
	}

	private TextObject _antiImperialKingdomCreatedLogText
	{
		get
		{
			TextObject textObject = new TextObject("{=BekWpXmR}Heeding the advice of {MENTOR.LINK}, you have created a kingdom to oppose the Empire. You can tell {?MENTOR.GENDER}her{?}him{\\?} that you will support your own kingdom.");
			StringHelpers.SetCharacterProperties("MENTOR", StoryModeHeroes.AntiImperialMentor.CharacterObject, textObject);
			return textObject;
		}
	}

	private TextObject _leftKingdomAfterCreatingLogText => new TextObject("{=nNavD2NO}You left the kingdom you have created. You can only support kingdoms that you are a part of.");

	private TextObject _clanTierRequirementLogText
	{
		get
		{
			TextObject textObject = new TextObject("{=QxeKZ3nE}Reach Clan Tier {CLAN_TIER}");
			textObject.SetTextVariable("CLAN_TIER", Campaign.Current.Models.KingdomCreationModel.MinimumClanTierToCreateKingdom);
			return textObject;
		}
	}

	private TextObject _partySizeRequirementLogText
	{
		get
		{
			TextObject textObject = new TextObject("{=NzQq2qp1}Gather {PARTY_SIZE} Troops");
			textObject.SetTextVariable("PARTY_SIZE", 100);
			return textObject;
		}
	}

	private TextObject _settlementOwnershipRequirementLogText
	{
		get
		{
			TextObject textObject = new TextObject("{=Bo66bfTh}Own {?IS_IMPERIAL}an Imperial Settlement{?}a Settlement{\\?} ");
			textObject.SetTextVariable("IS_IMPERIAL", _isImperial ? 1 : 0);
			return textObject;
		}
	}

	private TextObject _clanIndependenceRequirementLogText => new TextObject("{=a0ZKBj6P}Be an independent clan");

	private TextObject _questFailedLogText => new TextObject("{=tVlZTOst}You have chosen a different path.");

	public override TextObject Title
	{
		get
		{
			TextObject textObject = new TextObject("{=HhFHRs7N}Create {?IS_IMPERIAL}an Imperial Faction{?}a Non-Imperial Kingdom{\\?}");
			textObject.SetTextVariable("IS_IMPERIAL", _isImperial ? 1 : 0);
			return textObject;
		}
	}

	public override bool IsRemainingTimeHidden => false;

	public CreateKingdomQuest(Hero questGiver)
		: base("main_storyline_create_kingdom_quest_" + ((StoryModeHeroes.ImperialMentor == questGiver) ? "1" : "0"), questGiver, StoryModeManager.Current.MainStoryLine.FirstPhase.FirstPhaseEndTime)
	{
		_isImperial = StoryModeHeroes.ImperialMentor == questGiver;
		SetDialogs();
		if (_isImperial)
		{
			AddLog(_onQuestStartedImperialLogText);
		}
		else
		{
			AddLog(_onQuestStartedAntiImperialLogText);
		}
		int minimumClanTierToCreateKingdom = Campaign.Current.Models.KingdomCreationModel.MinimumClanTierToCreateKingdom;
		_clanTierRequirementLog = AddDiscreteLog(_clanTierRequirementLogText, new TextObject("{=tTLvo8sM}Clan Tier"), (int)MathF.Clamp(Clan.PlayerClan.Tier, 0f, minimumClanTierToCreateKingdom), minimumClanTierToCreateKingdom);
		_partySizeRequirementLog = AddDiscreteLog(_partySizeRequirementLogText, new TextObject("{=aClquusd}Troop Count"), (int)MathF.Clamp(MobileParty.MainParty.MemberRoster.TotalManCount - MobileParty.MainParty.MemberRoster.TotalWounded, 0f, 100f), 100);
		_clanIndependenceRequirementLog = AddDiscreteLog(_clanIndependenceRequirementLogText, new TextObject("{=qa0o7xaj}Clan Independence"), (Clan.PlayerClan.Kingdom == null) ? 1 : 0, 1);
		int currentProgress = (int)MathF.Clamp(_isImperial ? Clan.PlayerClan.Settlements.Count((Settlement t) => t.IsFortification && t.Culture == StoryModeData.ImperialCulture) : Clan.PlayerClan.Settlements.Count((Settlement t) => t.IsFortification), 0f, 1f);
		_settlementOwnershipRequirementLog = AddDiscreteLog(_settlementOwnershipRequirementLogText, new TextObject("{=gL3WCqM5}Settlement Count"), currentProgress, 1);
		InitializeQuestOnCreation();
		CheckPlayerClanDiplomaticState(Clan.PlayerClan.Kingdom);
	}

	protected override void SetDialogs()
	{
		DiscussDialogFlow = GetMentorDialogueFlow();
	}

	protected override void InitializeQuestOnGameLoad()
	{
		SetDialogs();
	}

	protected override void HourlyTick()
	{
	}

	private DialogFlow GetMentorDialogueFlow()
	{
		return DialogFlow.CreateDialogFlow("quest_discuss", 300).NpcLine("{=kbyqtszZ}I'm listening..").Condition(() => Hero.OneToOneConversationHero == base.QuestGiver)
			.PlayerLine("{=wErSpkjy}I'm still working on it.")
			.CloseDialog();
	}

	private void OnClanTierIncreased(Clan clan, bool showNotification)
	{
		if (!_hasPlayerCreatedKingdom && clan == Clan.PlayerClan)
		{
			UpdateQuestTaskStage(_clanTierRequirementLog, (int)MathF.Clamp(Clan.PlayerClan.Tier, 0f, Campaign.Current.Models.KingdomCreationModel.MinimumClanTierToCreateKingdom));
		}
	}

	private void OnClanChangedKingdom(Clan clan, Kingdom oldKingdom, Kingdom newKingdom, ChangeKingdomAction.ChangeKingdomActionDetail detail, bool showNotification)
	{
		if (clan == Clan.PlayerClan)
		{
			CheckPlayerClanDiplomaticState(newKingdom);
		}
	}

	private void CheckPlayerClanDiplomaticState(Kingdom newKingdom)
	{
		if (newKingdom == null)
		{
			if (_hasPlayerCreatedKingdom)
			{
				_leftKingdomLog = AddLog(_leftKingdomAfterCreatingLogText);
				_hasPlayerCreatedKingdom = false;
			}
			UpdateQuestTaskStage(_clanIndependenceRequirementLog, 1);
		}
		else if (newKingdom.RulingClan == Clan.PlayerClan)
		{
			_playerCreatedKingdom = newKingdom;
			if (StoryModeData.IsKingdomImperial(newKingdom))
			{
				if (_isImperial)
				{
					_hasPlayerCreatedKingdom = true;
					if (_leftKingdomLog != null)
					{
						RemoveLog(_leftKingdomLog);
					}
					else
					{
						AddLog(_imperialKingdomCreatedLogText);
					}
				}
				else
				{
					UpdateQuestTaskStage(_clanIndependenceRequirementLog, 0);
				}
			}
			else if (!_isImperial)
			{
				_hasPlayerCreatedKingdom = true;
				if (_leftKingdomLog != null)
				{
					RemoveLog(_leftKingdomLog);
				}
				else
				{
					AddLog(_antiImperialKingdomCreatedLogText);
				}
			}
			else
			{
				UpdateQuestTaskStage(_clanIndependenceRequirementLog, 0);
			}
		}
		else if (_playerCreatedKingdom == newKingdom && _isImperial == StoryModeData.IsKingdomImperial(newKingdom))
		{
			RemoveLog(_leftKingdomLog);
		}
	}

	private void OnSettlementOwnerChanged(Settlement settlement, bool openToClaim, Hero newOwner, Hero oldOwner, Hero capturerHero, ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail detail)
	{
		if (_hasPlayerCreatedKingdom || (newOwner != Hero.MainHero && oldOwner != Hero.MainHero))
		{
			return;
		}
		int num = -1;
		if (_isImperial && settlement.Culture == StoryModeData.ImperialCulture)
		{
			num = Clan.PlayerClan.Settlements.Count((Settlement t) => t.IsFortification && t.Culture == StoryModeData.ImperialCulture);
		}
		else if (!_isImperial)
		{
			num = Clan.PlayerClan.Settlements.Count((Settlement t) => t.IsFortification);
		}
		if (num != -1)
		{
			UpdateQuestTaskStage(_settlementOwnershipRequirementLog, (int)MathF.Clamp(num, 0f, 1f));
		}
	}

	private void OnPartySizeChanged(PartyBase party)
	{
		if (!_hasPlayerCreatedKingdom && party == PartyBase.MainParty)
		{
			int currentProgress = (int)MathF.Clamp(MobileParty.MainParty.MemberRoster.TotalManCount - MobileParty.MainParty.MemberRoster.TotalWounded, 0f, 100f);
			UpdateQuestTaskStage(_partySizeRequirementLog, currentProgress);
		}
	}

	private void MainStoryLineChosen(MainStoryLineSide chosenSide)
	{
		if (_hasPlayerCreatedKingdom && ((chosenSide == MainStoryLineSide.CreateImperialKingdom && _isImperial) || (chosenSide == MainStoryLineSide.CreateAntiImperialKingdom && !_isImperial)))
		{
			CompleteQuestWithSuccess();
		}
		else
		{
			CompleteQuestWithCancel(_questFailedLogText);
		}
	}

	protected override void RegisterEvents()
	{
		CampaignEvents.ClanTierIncrease.AddNonSerializedListener(this, OnClanTierIncreased);
		CampaignEvents.OnSettlementOwnerChangedEvent.AddNonSerializedListener(this, OnSettlementOwnerChanged);
		CampaignEvents.OnClanChangedKingdomEvent.AddNonSerializedListener(this, OnClanChangedKingdom);
		CampaignEvents.OnPartySizeChangedEvent.AddNonSerializedListener(this, OnPartySizeChanged);
		StoryModeEvents.OnMainStoryLineSideChosenEvent.AddNonSerializedListener(this, MainStoryLineChosen);
	}

	internal static void AutoGeneratedStaticCollectObjectsCreateKingdomQuest(object o, List<object> collectedObjects)
	{
		((CreateKingdomQuest)o).AutoGeneratedInstanceCollectObjects(collectedObjects);
	}

	protected override void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects)
	{
		base.AutoGeneratedInstanceCollectObjects(collectedObjects);
		collectedObjects.Add(_leftKingdomLog);
		collectedObjects.Add(_playerCreatedKingdom);
		collectedObjects.Add(_clanTierRequirementLog);
		collectedObjects.Add(_partySizeRequirementLog);
		collectedObjects.Add(_settlementOwnershipRequirementLog);
		collectedObjects.Add(_clanIndependenceRequirementLog);
	}

	internal static object AutoGeneratedGetMemberValue_isImperial(object o)
	{
		return ((CreateKingdomQuest)o)._isImperial;
	}

	internal static object AutoGeneratedGetMemberValue_hasPlayerCreatedKingdom(object o)
	{
		return ((CreateKingdomQuest)o)._hasPlayerCreatedKingdom;
	}

	internal static object AutoGeneratedGetMemberValue_leftKingdomLog(object o)
	{
		return ((CreateKingdomQuest)o)._leftKingdomLog;
	}

	internal static object AutoGeneratedGetMemberValue_playerCreatedKingdom(object o)
	{
		return ((CreateKingdomQuest)o)._playerCreatedKingdom;
	}

	internal static object AutoGeneratedGetMemberValue_clanTierRequirementLog(object o)
	{
		return ((CreateKingdomQuest)o)._clanTierRequirementLog;
	}

	internal static object AutoGeneratedGetMemberValue_partySizeRequirementLog(object o)
	{
		return ((CreateKingdomQuest)o)._partySizeRequirementLog;
	}

	internal static object AutoGeneratedGetMemberValue_settlementOwnershipRequirementLog(object o)
	{
		return ((CreateKingdomQuest)o)._settlementOwnershipRequirementLog;
	}

	internal static object AutoGeneratedGetMemberValue_clanIndependenceRequirementLog(object o)
	{
		return ((CreateKingdomQuest)o)._clanIndependenceRequirementLog;
	}
}
