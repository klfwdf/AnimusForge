using System.Collections.Generic;
using StoryMode.Quests.FirstPhase;
using StoryMode.Quests.PlayerClanQuests;
using StoryMode.Quests.QuestTasks;
using StoryMode.Quests.SecondPhase;
using StoryMode.Quests.SecondPhase.ConspiracyQuests;
using StoryMode.Quests.TutorialPhase;
using StoryMode.StoryModePhases;
using TaleWorlds.CampaignSystem;
using TaleWorlds.ObjectSystem;
using TaleWorlds.SaveSystem;

namespace StoryMode;

public class SaveableStoryModeTypeDefiner : SaveableTypeDefiner
{
	public SaveableStoryModeTypeDefiner()
		: base(320000)
	{
	}

	protected override void DefineClassTypes()
	{
		AddClassDefinition(typeof(CampaignStoryMode), 1);
		AddClassDefinition(typeof(StoryModeManager), 2);
		AddClassDefinition(typeof(MainStoryLine), 3);
		AddClassDefinition(typeof(TrainingField), 4);
		AddClassDefinition(typeof(TrainingFieldEncounter), 5);
		AddClassDefinition(typeof(PurchaseItemTutorialQuestTask), 6);
		AddClassDefinition(typeof(RecruitTroopTutorialQuestTask), 7);
		AddClassDefinition(typeof(TutorialPhase), 8);
		AddClassDefinition(typeof(FirstPhase), 9);
		AddClassDefinition(typeof(SecondPhase), 10);
		AddClassDefinition(typeof(ThirdPhase), 11);
		AddClassDefinition(typeof(ConspiracyQuestBase), 12);
		AddClassDefinition(typeof(ConspiracyQuestMapNotification), 13);
		AddClassDefinition(typeof(ConspiracyBaseOfOperationsDiscoveredConspiracyQuest), 14);
		AddClassDefinition(typeof(DestroyRaidersConspiracyQuest), 15);
		AddClassDefinition(typeof(DisruptSupplyLinesConspiracyQuest), 17);
		AddClassDefinition(typeof(TravelToVillageTutorialQuest), 694001);
		AddClassDefinition(typeof(TalkToTheHeadmanTutorialQuest), 693001);
		AddClassDefinition(typeof(PurchaseGrainTutorialQuest), 691001);
		AddClassDefinition(typeof(RecruitTroopsTutorialQuest), 692001);
		AddClassDefinition(typeof(LocateAndRescueTravellerTutorialQuest), 688001);
		AddClassDefinition(typeof(FindHideoutTutorialQuest), 686001);
		AddClassDefinition(typeof(BannerInvestigationQuest), 684001);
		AddClassDefinition(typeof(AssembleTheBannerQuest), 683001);
		AddClassDefinition(typeof(MeetWithIstianaQuest), 690001);
		AddClassDefinition(typeof(MeetWithArzagosQuest), 689001);
		AddClassDefinition(typeof(IstianasBannerPieceQuest), 687001);
		AddClassDefinition(typeof(ArzagosBannerPieceQuest), 681001);
		AddClassDefinition(typeof(SupportKingdomQuest), 680001);
		AddClassDefinition(typeof(CreateKingdomQuest), 580001);
		AddClassDefinition(typeof(ConspiracyProgressQuest), 695001);
		AddClassDefinition(typeof(RebuildPlayerClanQuest), 3780001);
		AddClassDefinition(typeof(VillagersInNeed), 18);
	}

	protected override void DefineStructTypes()
	{
	}

	protected override void DefineEnumTypes()
	{
		AddEnumDefinition(typeof(MainStoryLineSide), 2001);
		AddEnumDefinition(typeof(TutorialQuestPhase), 2002);
		AddEnumDefinition(typeof(FindHideoutTutorialQuest.HideoutBattleEndState), 686010);
		AddEnumDefinition(typeof(IstianasBannerPieceQuest.HideoutBattleEndState), 687010);
		AddEnumDefinition(typeof(ArzagosBannerPieceQuest.HideoutBattleEndState), 681010);
	}

	protected override void DefineInterfaceTypes()
	{
	}

	protected override void DefineRootClassTypes()
	{
	}

	protected override void DefineGenericClassDefinitions()
	{
	}

	protected override void DefineGenericStructDefinitions()
	{
	}

	protected override void DefineContainerDefinitions()
	{
		ConstructContainerDefinition(typeof(List<TrainingField>));
		ConstructContainerDefinition(typeof(Dictionary<string, TrainingField>));
		ConstructContainerDefinition(typeof(Dictionary<MBGUID, TrainingField>));
		ConstructContainerDefinition(typeof(Dictionary<int, CampaignTime>));
	}
}
