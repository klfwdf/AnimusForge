using System.Collections.Generic;
using NavalDLC.CampaignBehaviors;
using NavalDLC.Map;
using NavalDLC.Storyline;
using NavalDLC.Storyline.CampaignBehaviors;
using NavalDLC.Storyline.MissionControllers;
using NavalDLC.Storyline.Quests;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.Core;
using TaleWorlds.SaveSystem;

namespace NavalDLC;

public class SaveableNavalDLCTypeDefiner : SaveableTypeDefiner
{
	public SaveableNavalDLCTypeDefiner()
		: base(520000)
	{
	}

	protected override void DefineClassTypes()
	{
		AddClassDefinition(typeof(DefeatTheCaptorsQuest), 2);
		AddClassDefinition(typeof(SpeakToTheSailorsQuest), 9);
		AddClassDefinition(typeof(SailToTheGulfOfCharasQuest), 10);
		AddClassDefinition(typeof(HuntDownTheEmiraAlFahdaAndTheCorsairsQuest), 11);
		AddClassDefinition(typeof(ReturnToBaseQuest), 12);
		AddClassDefinition(typeof(SetSailAndEscortTheFortuneSeekersQuest), 13);
		AddClassDefinition(typeof(SetSailAndMeetTheFortuneSeekersInTargetSettlementQuest), 14);
		AddClassDefinition(typeof(GoToSkatriaIslandsQuest), 15);
		AddClassDefinition(typeof(CaptureTheImperialMerchantPrusas), 16);
		AddClassDefinition(typeof(InquireAtOstican), 17);
		AddClassDefinition(typeof(DefeatThePiratesQuest), 18);
		AddClassDefinition(typeof(FreeTheSeaHoundsCaptivesQuest), 19);
		AddClassDefinition(typeof(NavalOrderOfBattleCampaignBehavior.NavalOrderOfBattleFormationData), 20);
		AddClassDefinition(typeof(FishingPartyComponent), 21);
		AddClassDefinition(typeof(StormManager), 22);
		AddClassDefinition(typeof(Storm), 23);
		AddClassDefinition(typeof(SpeakToGunnarAndSisterQuest), 24);
		AddClassDefinition(typeof(ScourgeoftheSeasQuest), 25);
	}

	protected override void DefineContainerDefinitions()
	{
		ConstructContainerDefinition(typeof(List<NavalOrderOfBattleCampaignBehavior.NavalOrderOfBattleFormationData>));
		ConstructContainerDefinition(typeof(List<Storm>));
		ConstructContainerDefinition(typeof(Storm.PreviousData[]));
		ConstructContainerDefinition(typeof(Dictionary<Ship, List<ShipUpgradePiece>>));
	}

	protected override void DefineStructTypes()
	{
		AddStructDefinition(typeof(Storm.PreviousData), 1);
	}

	protected override void DefineEnumTypes()
	{
		AddEnumDefinition(typeof(NavalStorylineData.NavalStorylineStage), 1000);
		AddEnumDefinition(typeof(Storm.StormTypes), 1001);
		AddEnumDefinition(typeof(FreeTheSeaHoundsCaptivesQuest.FreeTheSeaHoundsCaptivesQuestState), 1002);
		AddEnumDefinition(typeof(Quest5SetPieceBattleMissionController.BossFightOutComeEnum), 1003);
		AddEnumDefinition(typeof(Quest5SetPieceBattleMissionController.Quest5SetPieceBattleMissionState), 1004);
		AddEnumDefinition(typeof(NavalStorylineThirdActFifthQuestBehaviour.NavalStorylineFinalQuestState), 1005);
		AddEnumDefinition(typeof(NavalStorylineData.NavalStorylineCheckpoint), 1006);
	}
}
