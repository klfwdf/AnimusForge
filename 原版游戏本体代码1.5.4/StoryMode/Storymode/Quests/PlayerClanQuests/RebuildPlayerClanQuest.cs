using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Localization;
using TaleWorlds.SaveSystem;

namespace StoryMode.Quests.PlayerClanQuests;

public class RebuildPlayerClanQuest : StoryModeQuestBase
{
	private const int GoldGoal = 2000;

	private const int ClanTierRenownGoal = 50;

	private const int RenownReward = 25;

	private const int HiredCompanionGoal = 1;

	private bool _finishQuest;

	[SaveableField(1)]
	private JournalLog _goldGoalLog;

	[SaveableField(2)]
	private JournalLog _partySizeGoalLog;

	[SaveableField(3)]
	private JournalLog _clanTierGoalLog;

	[SaveableField(4)]
	private JournalLog _hireCompanionGoalLog;

	private static int _partySizeGoal => Campaign.Current.Models.BanditDensityModel.GetMinimumTroopCountForHideoutMission(MobileParty.MainParty, isAssault: false);

	private TextObject _startQuestLogText => new TextObject("{=IITkXnnU}Calradia is a land full of peril - but also opportunities. To face the challenges that await, you will need to build up your clan.{newline}Your brother told you that there are many ways to go about this but that none forego coin. Trade would be one means to this end, fighting and selling off captured bandits in town another. Whatever path you choose to pursue, travelling alone would make you easy pickings for whomever came across your trail.{newline}You know that you can recruit men to follow you from the notables of villages and towns, though they may ask you for a favor or two of their own before they allow you access to their more valued fighters.{newline}Naturally, you may also find more unique characters in the taverns of Calradia. However, these tend to favor more established clans.");

	private TextObject _goldGoalLogText => new TextObject("{=bXYFXLgg}Increase your denars by 1000");

	private TextObject _partySizeGoalLogText
	{
		get
		{
			TextObject textObject = new TextObject("{=b6hQWKHe}Grow your party to {PARTY_SIZE} men");
			textObject.SetTextVariable("PARTY_SIZE", _partySizeGoal);
			return textObject;
		}
	}

	private TextObject _clanTierGoalLogText => new TextObject("{=RbXiEdXk}Reach Clan Tier 1");

	private TextObject _hireCompanionGoalLogText => new TextObject("{=e8Tjf8Ph}Hire 1 Companion");

	private TextObject _successLogText => new TextObject("{=eJX7rhch}You have successfully rebuilt your clan.");

	public override TextObject Title => new TextObject("{=bESRdcRo}Establish Your Clan");

	public RebuildPlayerClanQuest()
		: base("rebuild_player_clan_storymode_quest", null, CampaignTime.Never)
	{
		_finishQuest = false;
		SetDialogs();
	}

	protected override void InitializeQuestOnGameLoad()
	{
		SetDialogs();
	}

	protected override void RegisterEvents()
	{
		CampaignEvents.HeroOrPartyTradedGold.AddNonSerializedListener(this, HeroOrPartyTradedGold);
		CampaignEvents.SettlementEntered.AddNonSerializedListener(this, OnSettlementEntered);
		CampaignEvents.OnSettlementLeftEvent.AddNonSerializedListener(this, OnSettlementLeft);
		CampaignEvents.MapEventEnded.AddNonSerializedListener(this, OnMapEventEnded);
		CampaignEvents.OnTroopRecruitedEvent.AddNonSerializedListener(this, OnTroopRecruited);
		CampaignEvents.RenownGained.AddNonSerializedListener(this, OnRenownGained);
		CampaignEvents.NewCompanionAdded.AddNonSerializedListener(this, OnNewCompanionAdded);
	}

	protected override void OnStartQuest()
	{
		AddLog(_startQuestLogText, hideInformation: true);
		_goldGoalLog = AddDiscreteLog(_goldGoalLogText, new TextObject("{=hYgmzZJX}Denars"), Hero.MainHero.Gold, 2000, null, hideInformation: true);
		_partySizeGoalLog = AddDiscreteLog(_partySizeGoalLogText, new TextObject("{=DO4PE3Oo}Current Party Size"), 1, _partySizeGoal, null, hideInformation: true);
		_clanTierGoalLog = AddDiscreteLog(_clanTierGoalLogText, new TextObject("{=aZxHIra4}Renown"), (int)Clan.PlayerClan.Renown, 50, null, hideInformation: true);
		_hireCompanionGoalLog = AddDiscreteLog(_hireCompanionGoalLogText, new TextObject("{=VLD5416o}Companion Hired"), 0, 1, null, hideInformation: true);
	}

	protected override void OnCompleteWithSuccess()
	{
		GainRenownAction.Apply(Hero.MainHero, 25f);
		AddLog(_successLogText);
	}

	protected override void SetDialogs()
	{
	}

	private void HeroOrPartyTradedGold((Hero, PartyBase) giver, (Hero, PartyBase) recipient, (int, string) goldAmount, bool showNotification)
	{
		UpdateProgresses();
	}

	protected override void HourlyTick()
	{
		UpdateProgresses();
	}

	private void OnSettlementEntered(MobileParty party, Settlement settlement, Hero hero)
	{
		UpdateProgresses();
	}

	private void OnSettlementLeft(MobileParty party, Settlement settlement)
	{
		UpdateProgresses();
	}

	private void OnMapEventEnded(MapEvent mapEvent)
	{
		UpdateProgresses();
	}

	private void OnTroopRecruited(Hero recruiterHero, Settlement recruitmentSettlement, Hero recruitmentSource, CharacterObject troop, int amount)
	{
		UpdateProgresses();
	}

	private void OnRenownGained(Hero hero, int gainedRenown, bool doNotNotify)
	{
		UpdateProgresses();
	}

	private void OnNewCompanionAdded(Hero newCompanion)
	{
		UpdateProgresses();
	}

	private void UpdateProgresses()
	{
		_goldGoalLog.UpdateCurrentProgress((Hero.MainHero.Gold > 2000) ? 2000 : Hero.MainHero.Gold);
		_partySizeGoalLog.UpdateCurrentProgress((PartyBase.MainParty.MemberRoster.TotalManCount > _partySizeGoal) ? _partySizeGoal : PartyBase.MainParty.MemberRoster.TotalManCount);
		_clanTierGoalLog.UpdateCurrentProgress((Clan.PlayerClan.Renown > 50f) ? 50 : ((int)Clan.PlayerClan.Renown));
		_hireCompanionGoalLog.UpdateCurrentProgress((Clan.PlayerClan.Companions.Count > 1) ? 1 : Clan.PlayerClan.Companions.Count);
		if (_goldGoalLog.CurrentProgress >= 2000 && _partySizeGoalLog.CurrentProgress >= _partySizeGoal && _clanTierGoalLog.CurrentProgress >= 50 && _hireCompanionGoalLog.CurrentProgress >= 1 && !_finishQuest)
		{
			_finishQuest = true;
			CompleteQuestWithSuccess();
		}
	}

	internal static void AutoGeneratedStaticCollectObjectsRebuildPlayerClanQuest(object o, List<object> collectedObjects)
	{
		((RebuildPlayerClanQuest)o).AutoGeneratedInstanceCollectObjects(collectedObjects);
	}

	protected override void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects)
	{
		base.AutoGeneratedInstanceCollectObjects(collectedObjects);
		collectedObjects.Add(_goldGoalLog);
		collectedObjects.Add(_partySizeGoalLog);
		collectedObjects.Add(_clanTierGoalLog);
		collectedObjects.Add(_hireCompanionGoalLog);
	}

	internal static object AutoGeneratedGetMemberValue_goldGoalLog(object o)
	{
		return ((RebuildPlayerClanQuest)o)._goldGoalLog;
	}

	internal static object AutoGeneratedGetMemberValue_partySizeGoalLog(object o)
	{
		return ((RebuildPlayerClanQuest)o)._partySizeGoalLog;
	}

	internal static object AutoGeneratedGetMemberValue_clanTierGoalLog(object o)
	{
		return ((RebuildPlayerClanQuest)o)._clanTierGoalLog;
	}

	internal static object AutoGeneratedGetMemberValue_hireCompanionGoalLog(object o)
	{
		return ((RebuildPlayerClanQuest)o)._hireCompanionGoalLog;
	}
}
