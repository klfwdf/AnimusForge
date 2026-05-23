using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using StoryMode.StoryModeObjects;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;
using TaleWorlds.SaveSystem;

namespace StoryMode.Quests.SecondPhase.ConspiracyQuests;

public class DisruptSupplyLinesConspiracyQuest : ConspiracyQuestBase
{
	private const int NumberOfSettlementsToVisit = 6;

	private const int SpawnCaravanWaitDaysAfterQuestStarted = 5;

	[SaveableField(1)]
	private readonly Settlement[] _caravanTargetSettlements;

	[SaveableField(2)]
	private MobileParty _questCaravanMobileParty;

	[SaveableField(3)]
	private readonly CampaignTime _questStartTime;

	public override TextObject Title => new TextObject("{=y150haHv}Disrupt Supply Lines");

	public override TextObject SideNotificationText
	{
		get
		{
			TextObject textObject = new TextObject("{=IPP6MKfy}{MENTOR.LINK} notified you about a weapons caravan that will supply conspirators with weapons and armour.");
			StringHelpers.SetCharacterProperties("MENTOR", base.Mentor.CharacterObject, textObject);
			return textObject;
		}
	}

	public override TextObject StartMessageLogFromMentor
	{
		get
		{
			TextObject textObject = new TextObject("{=01Y1DAqA}{MENTOR.LINK} has sent you a message: As you may know, I receive reports from my spies in marketplaces around here. There is a merchant who I have been following - I know he is connected with {OTHER_MENTOR.LINK}. Now, I hear he has bought up a large supply of weapons and armor in {QUEST_FROM_SETTLEMENT_NAME}, and plans to travel to {QUEST_TO_SETTLEMENT_NAME}. From there it will move onward. I expect that {OTHER_MENTOR.LINK} is arming {?OTHER_MENTOR.GENDER}her{?}his{\\?} allies in the gangs in that area. If the caravan delivers its load, then I expect we will soon find some of our friends stabbed to death in the streets by hired thugs, and the rest of our friends too frightened to acknowledge us. I need you to track it down and destroy it. Try to intercept it on the first leg of its journey, before it gets to {QUEST_TO_SETTLEMENT_NAME}. If you fail, find out the next town to which it is going. It may take some time to find it, and when you do, it will be well guarded. But I trust in your perseverance, your skill and your understanding of how important this is. Good hunting.");
			StringHelpers.SetCharacterProperties("OTHER_MENTOR", StoryModeManager.Current.MainStoryLine.IsOnImperialQuestLine ? StoryModeHeroes.AntiImperialMentor.CharacterObject : StoryModeHeroes.ImperialMentor.CharacterObject, textObject);
			StringHelpers.SetCharacterProperties("MENTOR", base.Mentor.CharacterObject, textObject);
			textObject.SetTextVariable("QUEST_FROM_SETTLEMENT_NAME", QuestFromSettlement.EncyclopediaLinkWithName);
			textObject.SetTextVariable("QUEST_TO_SETTLEMENT_NAME", QuestToSettlement.EncyclopediaLinkWithName);
			return textObject;
		}
	}

	public override TextObject StartLog
	{
		get
		{
			TextObject textObject = new TextObject("{=ZKdBlAmp}An arms caravan to resupply the conspirators will be soon on its way.{newline}{MENTOR.LINK}'s message:{newline}\"Our spies have learned about an arms caravan that is attempting to bring the conspirators high quality weapons and armor. We know that it will set out on its route from {QUEST_FROM_SETTLEMENT_NAME} to {QUEST_TO_SETTLEMENT_NAME} after {SPAWN_DAYS} days. We will find out and notify you about the new routes that it takes as it progresses.\"");
			StringHelpers.SetCharacterProperties("MENTOR", base.Mentor.CharacterObject, textObject);
			textObject.SetTextVariable("QUEST_FROM_SETTLEMENT_NAME", QuestFromSettlement.EncyclopediaLinkWithName);
			textObject.SetTextVariable("QUEST_TO_SETTLEMENT_NAME", QuestToSettlement.EncyclopediaLinkWithName);
			textObject.SetTextVariable("SPAWN_DAYS", 5);
			return textObject;
		}
	}

	public override float ConspiracyStrengthDecreaseAmount => 75f;

	private TextObject PlayerDefeatedCaravanLog
	{
		get
		{
			TextObject textObject = new TextObject("{=Db63Pe03}You have defeated the caravan and acquired its supplies. {OTHER_MENTOR.LINK}'s allies will not have their weapons. This will give us time and resources to prepare.");
			StringHelpers.SetCharacterProperties("MENTOR", base.Mentor.CharacterObject, textObject);
			StringHelpers.SetCharacterProperties("OTHER_MENTOR", StoryModeManager.Current.MainStoryLine.IsOnImperialQuestLine ? StoryModeHeroes.AntiImperialMentor.CharacterObject : StoryModeHeroes.ImperialMentor.CharacterObject, textObject);
			return textObject;
		}
	}

	private TextObject MainHeroFailedToDisrupt => new TextObject("{=9aRqqx3U}The caravan has delivered its supplies to the conspirators. A stronger adversary awaits us...");

	private TextObject MainHeroLostCombat => new TextObject("{=bT9yspaQ}You have lost the battle against the conspiracy's caravan. A stronger adversary awaits us...");

	private Settlement QuestFromSettlement => _caravanTargetSettlements[0];

	private Settlement QuestToSettlement => _caravanTargetSettlements[_caravanTargetSettlements.Length - 1];

	public MobileParty ConspiracyCaravan => _questCaravanMobileParty;

	public int CaravanPartySize => 70 + 70 * (int)GetQuestDifficultyMultiplier();

	public DisruptSupplyLinesConspiracyQuest(string questId, Hero questGiver)
		: base(questId, questGiver)
	{
		_questStartTime = CampaignTime.Now;
		List<Settlement> list = new List<Settlement> { GetQuestFromSettlement() };
		for (int i = 1; i <= 6; i++)
		{
			list.Add(GetNextSettlement(list));
		}
		_caravanTargetSettlements = new Settlement[list.Count];
		for (int j = 0; j < list.Count; j++)
		{
			_caravanTargetSettlements[j] = list[j];
		}
		AddTrackedObject(QuestFromSettlement);
	}

	private Settlement GetQuestFromSettlement()
	{
		Settlement centralSettlement = StoryModeHeroes.ImperialMentor.HomeSettlement;
		Settlement settlement = SettlementHelper.FindRandomSettlement((Settlement s) => s.IsTown && s.MapFaction != Clan.PlayerClan.MapFaction && Campaign.Current.Models.MapDistanceModel.PathExistBetweenPoints(s.GatePosition, centralSettlement.GatePosition, MobileParty.NavigationType.Default) && ((!StoryModeManager.Current.MainStoryLine.IsOnImperialQuestLine) ? (!StoryModeData.IsKingdomImperial(s.OwnerClan.Kingdom)) : StoryModeData.IsKingdomImperial(s.OwnerClan.Kingdom)));
		if (settlement == null)
		{
			settlement = SettlementHelper.FindRandomSettlement((Settlement s) => s.IsTown && Campaign.Current.Models.MapDistanceModel.PathExistBetweenPoints(s.GatePosition, centralSettlement.GatePosition, MobileParty.NavigationType.Default) && ((!StoryModeManager.Current.MainStoryLine.IsOnImperialQuestLine) ? (!StoryModeData.IsKingdomImperial(s.OwnerClan.Kingdom)) : StoryModeData.IsKingdomImperial(s.OwnerClan.Kingdom)));
		}
		if (settlement == null)
		{
			settlement = SettlementHelper.FindRandomSettlement((Settlement s) => s.IsTown && Campaign.Current.Models.MapDistanceModel.PathExistBetweenPoints(s.GatePosition, centralSettlement.GatePosition, MobileParty.NavigationType.Default));
		}
		return settlement;
	}

	private Settlement GetNextSettlement(List<Settlement> caravanTargetSettlements)
	{
		Settlement centralSettlement = StoryModeHeroes.ImperialMentor.HomeSettlement;
		Settlement settlement = caravanTargetSettlements.Last();
		Settlement settlement2 = SettlementHelper.FindNearestTownToSettlement(settlement, MobileParty.NavigationType.Default, (Settlement s) => !caravanTargetSettlements.Contains(s) && s.MapFaction != Clan.PlayerClan.MapFaction && Campaign.Current.Models.MapDistanceModel.PathExistBetweenPoints(s.GatePosition, centralSettlement.GatePosition, MobileParty.NavigationType.Default) && (StoryModeManager.Current.MainStoryLine.IsOnImperialQuestLine ? StoryModeData.IsKingdomImperial(s.OwnerClan.Kingdom) : (!StoryModeData.IsKingdomImperial(s.OwnerClan.Kingdom))) && Campaign.Current.Models.MapDistanceModel.GetDistance(settlement, s, isFromPort: false, isTargetingPort: false, MobileParty.NavigationType.Default) > 100f && Campaign.Current.Models.MapDistanceModel.GetDistance(settlement, s, isFromPort: false, isTargetingPort: false, MobileParty.NavigationType.Default) < 500f)?.Settlement;
		if (settlement2 == null)
		{
			settlement2 = SettlementHelper.FindRandomSettlement((Settlement s) => !caravanTargetSettlements.Contains(s) && s.IsTown && s.MapFaction != Clan.PlayerClan.MapFaction && Campaign.Current.Models.MapDistanceModel.PathExistBetweenPoints(s.GatePosition, centralSettlement.GatePosition, MobileParty.NavigationType.Default) && Campaign.Current.Models.MapDistanceModel.GetDistance(settlement, s, isFromPort: false, isTargetingPort: false, MobileParty.NavigationType.Default) > 100f && Campaign.Current.Models.MapDistanceModel.GetDistance(settlement, s, isFromPort: false, isTargetingPort: false, MobileParty.NavigationType.Default) < 500f);
		}
		if (settlement2 == null)
		{
			settlement2 = SettlementHelper.FindRandomSettlement((Settlement s) => !caravanTargetSettlements.Contains(s) && s.IsTown && Campaign.Current.Models.MapDistanceModel.PathExistBetweenPoints(s.GatePosition, centralSettlement.GatePosition, MobileParty.NavigationType.Default) && Campaign.Current.Models.MapDistanceModel.GetDistance(settlement, s, isFromPort: false, isTargetingPort: false, MobileParty.NavigationType.Default) > 100f && Campaign.Current.Models.MapDistanceModel.GetDistance(settlement, s, isFromPort: false, isTargetingPort: false, MobileParty.NavigationType.Default) < 500f);
		}
		return settlement2;
	}

	protected override void InitializeQuestOnGameLoad()
	{
		SetDialogs();
	}

	protected override void HourlyTick()
	{
	}

	protected override void OnTimedOut()
	{
		MobileParty questCaravanMobileParty = _questCaravanMobileParty;
		if (questCaravanMobileParty != null && questCaravanMobileParty.IsActive)
		{
			DestroyPartyAction.Apply(null, _questCaravanMobileParty);
		}
	}

	protected override void RegisterEvents()
	{
		CampaignEvents.SettlementEntered.AddNonSerializedListener(this, OnSettlementEntered);
		CampaignEvents.OnSettlementLeftEvent.AddNonSerializedListener(this, OnSettlementLeft);
		CampaignEvents.MapEventEnded.AddNonSerializedListener(this, OnMapEventEnded);
	}

	private void OnSettlementEntered(MobileParty party, Settlement settlement, Hero hero)
	{
		if (_questCaravanMobileParty == null || _questCaravanMobileParty != party)
		{
			return;
		}
		if (settlement == QuestToSettlement)
		{
			DestroyPartyAction.Apply(null, _questCaravanMobileParty);
			FailedToDisrupt();
			return;
		}
		int num = Array.IndexOf(_caravanTargetSettlements, settlement) + 1;
		SetPartyAiAction.GetActionForVisitingSettlement(_questCaravanMobileParty, _caravanTargetSettlements[num], MobileParty.NavigationType.Default, isFromPort: false, isTargetingPort: false);
		if (IsTracked(settlement))
		{
			RemoveTrackedObject(settlement);
		}
		AddTrackedObject(_caravanTargetSettlements[num]);
	}

	private void OnSettlementLeft(MobileParty party, Settlement settlement)
	{
		if (_questCaravanMobileParty != null && _questCaravanMobileParty == party)
		{
			AddLogForSettlementVisit(settlement);
		}
	}

	private void OnMapEventEnded(MapEvent mapEvent)
	{
		if (!mapEvent.IsPlayerMapEvent || _questCaravanMobileParty == null || !mapEvent.InvolvedParties.Contains(_questCaravanMobileParty.Party))
		{
			return;
		}
		if (mapEvent.WinningSide == mapEvent.PlayerSide)
		{
			if (_questCaravanMobileParty.Party.NumberOfHealthyMembers > 0 && _questCaravanMobileParty.IsActive)
			{
				DestroyPartyAction.Apply(null, _questCaravanMobileParty);
			}
			BattleWon();
		}
		else if (mapEvent.WinningSide != BattleSideEnum.None)
		{
			if (_questCaravanMobileParty.IsActive)
			{
				DestroyPartyAction.Apply(null, _questCaravanMobileParty);
			}
			BattleLost();
		}
	}

	protected override void DailyTick()
	{
		if (_questCaravanMobileParty == null && _questStartTime.ElapsedDaysUntilNow >= 5f)
		{
			CreateQuestCaravanParty();
			SetDialogs();
		}
	}

	private void AddLogForSettlementVisit(Settlement settlement)
	{
		TextObject textObject = new TextObject("{=SVcr0EJM}Caravan is moving on to {TO_SETTLEMENT_LINK} from {FROM_SETTLEMENT_LINK}.");
		int num = Array.IndexOf(_caravanTargetSettlements, settlement) + 1;
		textObject.SetTextVariable("FROM_SETTLEMENT_LINK", settlement.EncyclopediaLinkWithName);
		textObject.SetTextVariable("TO_SETTLEMENT_LINK", _caravanTargetSettlements[num].EncyclopediaLinkWithName);
		Campaign.Current.CampaignInformationManager.NewMapNoticeAdded(new ConspiracyQuestMapNotification(this, textObject));
		AddLog(textObject);
	}

	private void CreateQuestCaravanParty()
	{
		PartyTemplateObject partyTemplateObject = (StoryModeManager.Current.MainStoryLine.IsOnImperialQuestLine ? Campaign.Current.ObjectManager.GetObject<PartyTemplateObject>("conspiracy_anti_imperial_special_raider_party_template") : Campaign.Current.ObjectManager.GetObject<PartyTemplateObject>("conspiracy_imperial_special_raider_party_template"));
		Hero owner = (StoryModeManager.Current.MainStoryLine.IsOnImperialQuestLine ? StoryModeHeroes.AntiImperialMentor : StoryModeHeroes.ImperialMentor);
		GetAdditionalVisualsForParty(QuestFromSettlement.Culture, out var mountStringId, out var harnessStringId);
		string[] source = new string[5] { "aserai", "battania", "khuzait", "sturgia", "vlandia" };
		Clan clan = null;
		foreach (Clan item in Clan.All)
		{
			if (!item.IsEliminated && !item.IsBanditFaction && !item.IsMinorFaction && ((StoryModeManager.Current.MainStoryLine.IsOnAntiImperialQuestLine && item.Culture.StringId == "empire") || (StoryModeManager.Current.MainStoryLine.IsOnImperialQuestLine && source.Contains(item.Culture.StringId))))
			{
				clan = item;
				break;
			}
		}
		_questCaravanMobileParty = CustomPartyComponent.CreateCustomPartyWithPartyTemplate(QuestFromSettlement.GatePosition, 0f, QuestFromSettlement, new TextObject("{=eVzg5Mtl}Conspiracy Caravan"), clan, partyTemplateObject, owner, mountStringId, harnessStringId, 4f, avoidHostileActions: true);
		_questCaravanMobileParty.Aggressiveness = 0f;
		_questCaravanMobileParty.MemberRoster.Clear();
		_questCaravanMobileParty.ItemRoster.AddToCounts(MBObjectManager.Instance.GetObject<ItemObject>("fish"), 20);
		_questCaravanMobileParty.ItemRoster.AddToCounts(MBObjectManager.Instance.GetObject<ItemObject>("grain"), 40);
		_questCaravanMobileParty.ItemRoster.AddToCounts(MBObjectManager.Instance.GetObject<ItemObject>("butter"), 20);
		DistributeConspiracyRaiderTroopsByLevel(partyTemplateObject, _questCaravanMobileParty.Party, CaravanPartySize);
		_questCaravanMobileParty.IgnoreByOtherPartiesTill(base.QuestDueTime);
		_questCaravanMobileParty.SetPartyUsedByQuest(isActivelyUsed: true);
		SetPartyAiAction.GetActionForVisitingSettlement(_questCaravanMobileParty, _caravanTargetSettlements[1], MobileParty.NavigationType.Default, isFromPort: false, isTargetingPort: false);
		_questCaravanMobileParty.Ai.SetDoNotMakeNewDecisions(doNotMakeNewDecisions: true);
		AddTrackedObject(_questCaravanMobileParty);
		_questCaravanMobileParty.IgnoreByOtherPartiesTill(CampaignTime.DaysFromNow(21f));
		AddLogForSettlementVisit(QuestFromSettlement);
	}

	private void GetAdditionalVisualsForParty(CultureObject culture, out string mountStringId, out string harnessStringId)
	{
		if (culture.StringId == "aserai" || culture.StringId == "khuzait")
		{
			mountStringId = "camel";
			harnessStringId = ((MBRandom.RandomFloat > 0.5f) ? "camel_saddle_a" : "camel_saddle_b");
		}
		else
		{
			mountStringId = "mule";
			harnessStringId = ((MBRandom.RandomFloat > 0.5f) ? "mule_load_a" : ((MBRandom.RandomFloat > 0.5f) ? "mule_load_b" : "mule_load_c"));
		}
	}

	private float GetQuestDifficultyMultiplier()
	{
		return MBMath.ClampFloat((0f + (float)Clan.PlayerClan.Fiefs.Count * 0.1f + Clan.PlayerClan.CurrentTotalStrength * 0.0008f + Clan.PlayerClan.Renown * 1.5E-05f + (float)Clan.PlayerClan.AliveLords.Count * 0.002f + (float)Clan.PlayerClan.Companions.Count * 0.01f + (float)Clan.PlayerClan.SupporterNotables.Count * 0.001f + (float)Hero.MainHero.OwnedCaravans.Count * 0.01f + (float)PartyBase.MainParty.NumberOfAllMembers * 0.002f + (float)CharacterObject.PlayerCharacter.Level * 0.002f) * 0.975f + MBRandom.RandomFloat * 0.025f, 0.1f, 1f);
	}

	private void BattleWon()
	{
		AddLog(PlayerDefeatedCaravanLog);
		CompleteQuestWithSuccess();
	}

	private void BattleLost()
	{
		AddLog(MainHeroLostCombat);
		CompleteQuestWithFail();
	}

	private void FailedToDisrupt()
	{
		AddLog(MainHeroFailedToDisrupt);
		CompleteQuestWithFail();
	}

	protected override void SetDialogs()
	{
		Campaign.Current.ConversationManager.AddDialogFlow(DialogFlow.CreateDialogFlow("start", 1000015).NpcLine(new TextObject("{=ch9f3A1e}Greetings, {?PLAYER.GENDER}madam{?}sir{\\?}. Why did you stop our caravan? I trust you are not robbing us.")).Condition(conversation_with_caravan_master_condition)
			.BeginPlayerOptions()
			.PlayerOption(new TextObject("{=Xx94UrYe}I might be. What are you carrying? Honest goods, or weapons? How about you let us have a look."))
			.NpcLine(new TextObject("{=LXGXxKqw}Ah... Well, I suppose we can drop the charade. [ib:hip2][if:convo_nonchalant]I know who sent you, and I suppose you know who sent me. Certainly, you can see my wares, and then you can feel their sharp end in your belly."))
			.CloseDialog()
			.PlayerOption(new TextObject("{=cEaXehHy}I was just checking on something. You can move along."))
			.Consequence(cancel_encounter_consequence)
			.CloseDialog()
			.EndPlayerOptions()
			.CloseDialog(), this);
	}

	private bool conversation_with_caravan_master_condition()
	{
		if (_questCaravanMobileParty != null)
		{
			return ConversationHelper.GetConversationCharacterPartyLeader(_questCaravanMobileParty.Party) == CharacterObject.OneToOneConversationCharacter;
		}
		return false;
	}

	private void cancel_encounter_consequence()
	{
		if (PlayerEncounter.Current != null)
		{
			PlayerEncounter.LeaveEncounter = true;
		}
	}

	internal static void AutoGeneratedStaticCollectObjectsDisruptSupplyLinesConspiracyQuest(object o, List<object> collectedObjects)
	{
		((DisruptSupplyLinesConspiracyQuest)o).AutoGeneratedInstanceCollectObjects(collectedObjects);
	}

	protected override void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects)
	{
		base.AutoGeneratedInstanceCollectObjects(collectedObjects);
		collectedObjects.Add(_caravanTargetSettlements);
		collectedObjects.Add(_questCaravanMobileParty);
		CampaignTime.AutoGeneratedStaticCollectObjectsCampaignTime(_questStartTime, collectedObjects);
	}

	internal static object AutoGeneratedGetMemberValue_caravanTargetSettlements(object o)
	{
		return ((DisruptSupplyLinesConspiracyQuest)o)._caravanTargetSettlements;
	}

	internal static object AutoGeneratedGetMemberValue_questCaravanMobileParty(object o)
	{
		return ((DisruptSupplyLinesConspiracyQuest)o)._questCaravanMobileParty;
	}

	internal static object AutoGeneratedGetMemberValue_questStartTime(object o)
	{
		return ((DisruptSupplyLinesConspiracyQuest)o)._questStartTime;
	}
}
