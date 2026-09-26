using System;
using HarmonyLib;

namespace AnimusForge;

// Startup patch and service composition stays on the Bannerlord main-thread entry point.
internal static class StartupPatchComposition
{
	internal static void Register()
	{
		Logger.LogTrace("SubModule", "====== Game root screen is about to show, loading module data ======");
		try
		{
			Logger.LogTrace("SubModule", ">>> Applying Harmony patches...");
			Harmony harmony = new Harmony("com.AnimusForge.spy");
			try
			{
				PatchClassProcessor patchClassProcessor2 = harmony.CreateClassProcessor(typeof(Patch_TriggerMassiveHook));
				patchClassProcessor2.Patch();
			}
			catch (Exception ex2)
			{
				Logger.LogTrace("SubModule", ">>> Patch_TriggerMassiveHook failed: " + ex2.Message);
			}
			try
			{
				PatchClassProcessor patchClassProcessor3 = harmony.CreateClassProcessor(typeof(Patch_GlobalUI_Click));
				patchClassProcessor3.Patch();
			}
			catch (Exception ex3)
			{
				Logger.LogTrace("SubModule", ">>> Patch_GlobalUI_Click failed: " + ex3.Message);
			}
			try
			{
				AiErrorAnalysisInquiry.EnsurePatched(harmony);
			}
			catch (Exception ex3a)
			{
				Logger.LogTrace("SubModule", ">>> AI error analysis inquiry patch failed: " + ex3a.Message);
			}
			try
			{
				PatchClassProcessor patchClassProcessor4 = harmony.CreateClassProcessor(typeof(Patch_PlayerEncounter_Start));
				patchClassProcessor4.Patch();
			}
			catch (Exception ex4)
			{
				Logger.LogTrace("SubModule", ">>> Patch_PlayerEncounter_Start failed: " + ex4.Message);
			}
			try
			{
				PatchClassProcessor patchClassProcessor5 = harmony.CreateClassProcessor(typeof(Patch_GameMenu_ActivateGameMenu));
				patchClassProcessor5.Patch();
			}
			catch (Exception ex5)
			{
				Logger.LogTrace("SubModule", ">>> Patch_GameMenu_ActivateGameMenu failed: " + ex5.Message);
			}
			try
			{
				harmony.CreateClassProcessor(typeof(Patch_MenuHelper_EncounterAttackConsequence_RaidVillageRestore)).Patch();
			}
			catch (Exception ex5r)
			{
				Logger.LogTrace("SubModule", ">>> Raid village encounter restore patch failed: " + ex5r.Message);
			}
			try
			{
				harmony.CreateClassProcessor(typeof(Patch_NpcSurrender_SkipCapturedLordConversation)).Patch();
				harmony.CreateClassProcessor(typeof(Patch_NpcSurrender_SkipFreeOrCapturePrisonerHeroConversation)).Patch();
			}
			catch (Exception ex5n)
			{
				Logger.LogTrace("SubModule", ">>> NPC surrender hero capture conversation skip patches failed: " + ex5n.Message);
			}
			try
			{
				harmony.CreateClassProcessor(typeof(Patch_PrisonBreakRescue_RecordSuccess)).Patch();
			}
			catch (Exception ex5p)
			{
				Logger.LogTrace("SubModule", ">>> Prison break rescue record patch failed: " + ex5p.Message);
			}
			try
			{
				SiegeAftermathPatchBootstrap.Apply(harmony);
			}
			catch (Exception ex5a)
			{
				Logger.LogTrace("SubModule", ">>> Siege aftermath GCCZ bridge patches failed: " + ex5a.Message);
			}
			try
			{
				PatchClassProcessor patchClassProcessor6 = harmony.CreateClassProcessor(typeof(Patch_Meeting_SuppressDeclareWarAction));
				patchClassProcessor6.Patch();
			}
			catch (Exception ex6)
			{
				Logger.LogTrace("SubModule", ">>> Patch_Meeting_SuppressDeclareWarAction failed: " + ex6.Message);
			}
			try
			{
				PermanentAllianceGuard.RegisterHarmonyPatches(harmony);
			}
			catch (Exception permanentAllianceEx)
			{
				Logger.LogTrace("SubModule", ">>> Permanent alliance guard patches failed: " + permanentAllianceEx.Message);
			}
			try
			{
				PatchClassProcessor vassalageDeclareWarPatch = harmony.CreateClassProcessor(typeof(Patch_Vassalage_DeclareWarAction));
				vassalageDeclareWarPatch.Patch();
			}
			catch (Exception ex6a)
			{
				Logger.LogTrace("SubModule", ">>> Patch_Vassalage_DeclareWarAction failed: " + ex6a.Message);
			}
			try
			{
				PatchClassProcessor vassalageMakePeacePatch = harmony.CreateClassProcessor(typeof(Patch_Vassalage_MakePeaceAction));
				vassalageMakePeacePatch.Patch();
			}
			catch (Exception ex6b)
			{
				Logger.LogTrace("SubModule", ">>> Patch_Vassalage_MakePeaceAction failed: " + ex6b.Message);
			}
			try
			{
				NpcTributeVassalageBehavior.RegisterHarmonyPatches(harmony);
			}
			catch (Exception ex6b2)
			{
				Logger.LogTrace("SubModule", ">>> NpcTributeVassalage patches failed: " + ex6b2.Message);
			}
			try
			{
				AnimusForgeVassalageUiSprites.EnsurePatched(harmony);
			}
			catch (Exception ex6c)
			{
				Logger.LogTrace("SubModule", ">>> Vassalage UI sprite bootstrap failed: " + ex6c.Message);
			}
			try
			{
				AnimusForgeWeeklyReportUiSprites.EnsurePatched(harmony);
			}
			catch (Exception ex6d)
			{
				Logger.LogTrace("SubModule", ">>> Weekly report UI sprite bootstrap failed: " + ex6d.Message);
			}
			try
			{
				AnimusForgePlayerNotorietyUiSprites.EnsurePatched(harmony);
			}
			catch (Exception ex6e)
			{
				Logger.LogTrace("SubModule", ">>> Player notoriety UI sprite bootstrap failed: " + ex6e.Message);
			}
			try
			{
				AnimusForgePlayerRpForgeUiSprites.EnsurePatched(harmony);
			}
			catch (Exception ex6f)
			{
				Logger.LogTrace("SubModule", ">>> Player RP forge UI sprite bootstrap failed: " + ex6f.Message);
			}
			try
			{
				PatchClassProcessor patchClassProcessor7 = harmony.CreateClassProcessor(typeof(Patch_Meeting_SuppressChangeRelationAction));
				patchClassProcessor7.Patch();
			}
			catch (Exception ex7)
			{
				Logger.LogTrace("SubModule", ">>> Patch_Meeting_SuppressChangeRelationAction failed: " + ex7.Message);
			}
			try
			{
				PatchClassProcessor patchClassProcessor8 = harmony.CreateClassProcessor(typeof(Patch_Meeting_SuppressEncounterHostileAction));
				patchClassProcessor8.Patch();
			}
			catch (Exception ex8)
			{
				Logger.LogTrace("SubModule", ">>> Patch_Meeting_SuppressEncounterHostileAction failed: " + ex8.Message);
			}
			try
			{
				PlayerEncounterPropertySafePatch.EnsurePatched();
			}
			catch (Exception ex8a)
			{
				Logger.LogTrace("SubModule", ">>> PlayerEncounterPropertySafePatch init failed: " + ex8a.Message);
			}
			try
			{
				AgentVictoryRetreatNullTeamSafePatch.EnsurePatched();
			}
			catch (Exception ex8aa)
			{
				Logger.LogTrace("SubModule", ">>> AgentVictoryRetreatNullTeamSafePatch init failed: " + ex8aa.Message);
			}
			try
			{
				BattleObserverInspectionPrisonerSafePatch.EnsurePatched();
			}
			catch (Exception ex8aaa)
			{
				Logger.LogTrace("SubModule", ">>> BattleObserverInspectionPrisonerSafePatch init failed: " + ex8aaa.Message);
			}
			try
			{
				KingdomDecisionCleanupSafePatch.EnsurePatched(harmony);
			}
			catch (Exception ex8ab)
			{
				Logger.LogTrace("SubModule", ">>> KingdomDecisionCleanupSafePatch init failed: " + ex8ab.Message);
			}
			try
			{
				WorkshopDailyTickSafetyPatch.EnsurePatched(harmony);
			}
			catch (Exception ex8ac)
			{
				Logger.LogTrace("SubModule", ">>> WorkshopDailyTickSafetyPatch init failed: " + ex8ac.Message);
			}
			try
			{
				MapSceneTerrainTypeSafePatch.EnsurePatched(harmony);
			}
			catch (Exception ex8ad)
			{
				Logger.LogTrace("SubModule", ">>> MapSceneTerrainTypeSafePatch init failed: " + ex8ad.Message);
			}
			try
			{
				HeroClosestSettlementSafePatch.EnsurePatched(harmony);
			}
			catch (Exception ex8ae)
			{
				Logger.LogTrace("SubModule", ">>> HeroClosestSettlementSafePatch init failed: " + ex8ae.Message);
			}
			try
			{
				AnimusForgeMobilePartyAiSafetyPatch.EnsurePatched(harmony);
			}
			catch (Exception ex8af)
			{
				Logger.LogTrace("SubModule", ">>> AnimusForgeMobilePartyAiSafetyPatch init failed: " + ex8af.Message);
			}
			try
			{
				WorldMapGovernorExpeditionNativeLifecyclePatch.EnsurePatched(harmony);
			}
			catch (Exception ex8af2)
			{
				Logger.LogTrace("SubModule", ">>> Governor expedition native lifecycle patch failed: " + ex8af2.Message);
			}
			try
			{
				WorldMapOrderedArmySurvivalPatch.EnsurePatched(harmony);
			}
			catch (Exception ex8af3)
			{
				Logger.LogTrace("SubModule", ">>> Ordered army survival patch failed: " + ex8af3.Message);
			}
			try
			{
				CampaignTickDiagnosticsPatch.EnsurePatched(harmony);
			}
			catch (Exception ex8ag)
			{
				Logger.LogTrace("SubModule", ">>> CampaignTickDiagnosticsPatch init failed: " + ex8ag.Message);
			}
			try
			{
				Patch_Conversation_Start_Intercept.ManualPatch(harmony);
			}
			catch (Exception ex8b)
			{
				Logger.LogTrace("SubModule", ">>> Manual conversation start intercept patch failed: " + ex8b.Message);
			}
			try
			{
				PatchClassProcessor shoutTextInputFocusPatch = harmony.CreateClassProcessor(typeof(ShoutTextInputFocusChangePatch));
				shoutTextInputFocusPatch.Patch();
			}
			catch (Exception ex8c)
			{
				Logger.LogTrace("SubModule", ">>> ShoutTextInputFocusChangePatch failed: " + ex8c.Message);
			}
			try
			{
				Patch_ConversationManager_OpenMapConversation.ManualPatch(harmony);
			}
			catch (Exception ex9)
			{
				Logger.LogTrace("SubModule", ">>> Manual OpenMapConversation patch failed: " + ex9.Message);
			}
			try
			{
				Patch_ConversationManager_SetupAndStartMapConversation.ManualPatch(harmony);
			}
			catch (Exception ex10)
			{
				Logger.LogTrace("SubModule", ">>> Manual SetupAndStartMapConversation patch failed: " + ex10.Message);
			}
			try
			{
				ConversationVMCapturePatch.EnsurePatched();
			}
			catch (Exception ex10a)
			{
				Logger.LogTrace("SubModule", ">>> Conversation VM capture patch failed: " + ex10a.Message);
			}
			try
			{
				NativeConversationAnswerAreaController.EnsurePatched();
			}
			catch (Exception ex10b)
			{
				Logger.LogTrace("SubModule", ">>> Native conversation answer area patch failed: " + ex10b.Message);
			}
			try
			{
				PassageUsePointSafePatch.EnsurePatched();
			}
			catch (Exception ex11)
			{
				Logger.LogTrace("SubModule", ">>> PassageUsePointSafePatch init failed: " + ex11.Message);
			}
			try
			{
				SceneTauntWieldBlockPatch.EnsurePatched();
			}
			catch (Exception ex12)
			{
				Logger.LogTrace("SubModule", ">>> SceneTauntWieldBlockPatch init failed: " + ex12.Message);
			}
			try
			{
				SceneTauntMissionDifficultyPatch.EnsurePatched();
			}
			catch (Exception ex13)
			{
				Logger.LogTrace("SubModule", ">>> SceneTauntMissionDifficultyPatch init failed: " + ex13.Message);
			}
			try
			{
				SceneTauntNativeConversationBlockPatch.EnsurePatched();
			}
			catch (Exception ex14)
			{
				Logger.LogTrace("SubModule", ">>> SceneTauntNativeConversationBlockPatch init failed: " + ex14.Message);
			}
			try
			{
				SceneTauntLeaveMissionBlockPatch.EnsurePatched();
			}
			catch (Exception ex15)
			{
				Logger.LogTrace("SubModule", ">>> SceneTauntLeaveMissionBlockPatch init failed: " + ex15.Message);
			}
			try
			{
				SceneTauntFightAutoEndDelayPatch.EnsurePatched();
			}
			catch (Exception ex16)
			{
				Logger.LogTrace("SubModule", ">>> SceneTauntFightAutoEndDelayPatch init failed: " + ex16.Message);
			}
			try
			{
				BannerlordExceptionSentinel.Initialize(harmony);
			}
			catch (Exception ex17)
			{
				Logger.LogTrace("SubModule", ">>> BannerlordExceptionSentinel init failed: " + ex17.Message);
			}
			try
			{
				CraftingOrderLoadSafetyPatch.EnsurePatched(harmony);
			}
			catch (Exception ex17a)
			{
				Logger.LogTrace("SubModule", ">>> CraftingOrderLoadSafetyPatch init failed: " + ex17a.Message);
			}
			try
			{
				McmDropdownRuntimeRefresh.EnsurePatched();
			}
			catch (Exception ex18a)
			{
				Logger.LogTrace("SubModule", ">>> McmDropdownRuntimeRefresh init failed: " + ex18a.Message);
			}
			try
			{
				EncyclopediaHeroPersonaPatch.EnsurePatched(harmony);
			}
			catch (Exception ex18aa)
			{
				Logger.LogTrace("SubModule", ">>> EncyclopediaHeroPersonaPatch init failed: " + ex18aa.Message);
			}
			try
			{
				EncyclopediaTownRuleMemoryPatch.EnsurePatched(harmony);
			}
			catch (Exception ex18aab)
			{
				Logger.LogTrace("SubModule", ">>> EncyclopediaTownRuleMemoryPatch init failed: " + ex18aab.Message);
			}
			try
			{
				EncyclopediaKingdomStabilityPatch.EnsurePatched(harmony);
			}
			catch (Exception ex18aaa)
			{
				Logger.LogTrace("SubModule", ">>> EncyclopediaKingdomStabilityPatch init failed: " + ex18aaa.Message);
			}
			try
			{
				PlayerNotorietyCharacterDeveloperPatch.EnsurePatched(harmony);
			}
			catch (Exception ex18ab)
			{
				Logger.LogTrace("SubModule", ">>> PlayerNotorietyCharacterDeveloperPatch init failed: " + ex18ab.Message);
			}
			try
			{
				harmony.CreateClassProcessor(typeof(Patch_PlayerKingdomNameChange_RecordMaterials)).Patch();
			}
			catch (Exception ex18ac)
			{
				Logger.LogTrace("SubModule", ">>> Player kingdom rename material patch init failed: " + ex18ac.Message);
			}
			try
			{
				TroopInspectionBehavior.RegisterHarmonyPatches(harmony);
			}
			catch (Exception ex18b)
			{
				Logger.LogTrace("SubModule", ">>> TroopInspection patches init failed: " + ex18b.Message);
			}
			try
			{
				CastleAftermathConstructionSpeedPatchRegistrar.Register(harmony);
			}
			catch (Exception ex18bCastleConstruction)
			{
				Logger.LogTrace("SubModule", ">>> Castle aftermath construction patches init failed: " + ex18bCastleConstruction.Message);
			}
			try
			{
				SettlementEntryTroopSelectionBehavior.RegisterHarmonyPatches(harmony);
			}
			catch (Exception ex18ba)
			{
				Logger.LogTrace("SubModule", ">>> SETS patches init failed: " + ex18ba.Message);
			}
			try
			{
				MilitaryExerciseBehavior.RegisterHarmonyPatches(harmony);
			}
			catch (Exception ex18c)
			{
				Logger.LogTrace("SubModule", ">>> MilitaryExercise patches init failed: " + ex18c.Message);
			}
			try
			{
				DuelBehavior.RegisterHarmonyPatches(harmony);
			}
			catch (Exception ex18d0)
			{
				Logger.LogTrace("SubModule", ">>> DuelBehavior patches init failed: " + ex18d0.Message);
			}
			try
			{
				FourberieDuelCompatibility.EnsurePatched(harmony);
			}
			catch (Exception ex18d0Fourberie)
			{
				Logger.LogTrace("SubModule", ">>> Fourberie duel compatibility init failed: " + ex18d0Fourberie.Message);
			}
			try
			{
				CourierDeliveryBehavior.RegisterHarmonyPatches(harmony);
			}
			catch (Exception ex18d)
			{
				Logger.LogTrace("SubModule", ">>> CourierDelivery patches init failed: " + ex18d.Message);
			}
			try
			{
				WorldDiplomacyBehavior.RegisterHarmonyPatches(harmony);
			}
			catch (Exception worldDiplomacyPatchEx)
			{
				Logger.LogTrace("SubModule", ">>> WorldDiplomacy patches init failed: " + worldDiplomacyPatchEx.Message);
			}
			try
			{
				RewardSystemBehavior.RegisterHarmonyPatches(harmony);
			}
			catch (Exception ex18e)
			{
				Logger.LogTrace("SubModule", ">>> RewardSystem patches init failed: " + ex18e.Message);
			}
			try
			{
				NobleGatheringBehavior.RegisterHarmonyPatches(harmony);
			}
			catch (Exception ex18f)
			{
				Logger.LogTrace("SubModule", ">>> NobleGathering patches init failed: " + ex18f.Message);
			}
			try
			{
				SexualConceptionBehavior.RegisterHarmonyPatches(harmony);
			}
			catch (Exception ex18g)
			{
				Logger.LogTrace("SubModule", ">>> SexualConception patches init failed: " + ex18g.Message);
			}
			Logger.LogTrace("SubModule", ">>> Harmony patches applied.");
		}
		catch (Exception ex18)
		{
			Logger.LogTrace("SubModule", ">>> Harmony patch bootstrap failed: " + ex18);
		}
		AIConfigHandler.ReloadConfig();
		try
		{
			TtsEngine.Instance.Initialize();
			Logger.LogTrace("SubModule", ">>> Online TTS engine initialized.");
		}
		catch (Exception ex19)
		{
			Logger.LogTrace("SubModule", ">>> TTS engine initialization failed (non-fatal): " + ex19.Message);
		}
		try
		{
			CompatibilityAudit.RunStartupAudit();
		}
		catch (Exception ex20)
		{
			Logger.LogCompatibilityAudit("CompatAudit", "Startup compatibility audit failed: " + ex20.Message);
		}
	}
}
