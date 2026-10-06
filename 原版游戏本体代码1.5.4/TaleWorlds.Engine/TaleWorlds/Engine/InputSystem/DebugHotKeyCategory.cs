using TaleWorlds.InputSystem;

namespace TaleWorlds.Engine.InputSystem;

public class DebugHotKeyCategory : GameKeyContext
{
	public const string CategoryId = "Debug";

	public const string LeftMouseButton = "LeftMouseButton";

	public const string RightMouseButton = "RightMouseButton";

	public const string SelectAll = "SelectAll";

	public const string Redo = "Redo";

	public const string Undo = "Undo";

	public const string Copy = "Copy";

	public const string Duplicate = "Duplicate";

	public const string Score = "Score";

	public const string SetOriginToZero = "SetOriginToZero";

	public const string TestEngineCrash = "TestEngineCrash";

	public const string AgentHotkeySwitchRender = "AgentHotkeySwitchRender";

	public const string AgentHotkeySwitchFaceAnimationDebug = "AgentHotkeySwitchFaceAnimationDebug";

	public const string AgentHotkeyCheckCollisionCapsule = "AgentHotkeyCheckCollisionCapsule";

	public const string EngineInterfaceHotkeyWireframe = "EngineInterfaceHotkeyWireframe";

	public const string EngineInterfaceHotkeyWireframe2 = "EngineInterfaceHotkeyWireframe2";

	public const string EngineInterfaceHotkeyTakeScreenShot = "EngineInterfaceHotkeyTakeScreenShot";

	public const string EditingManagerHotkeyCrashReporting = "EditingManagerHotkeyCrashReporting";

	public const string EditingManagerHotkeyEmergencySceneSaving = "EditingManagerHotkeyEmergencySceneSaving";

	public const string EditingManagerHotkeyAssertTestEntityOperations = "EditingManagerHotkeyAssertTestEntityOperations";

	public const string EditingManagerHotkeyUpdateSceneDialog = "EditingManagerHotkeyUpdateSceneDialog";

	public const string EditingManagerHotkeySetTriadToWorld = "EditingManagerHotkeySetTriadToWorld";

	public const string EditingManagerHotkeySetTriadToLocal = "EditingManagerHotkeySetTriadToLocal";

	public const string EditingManagerHotkeySetTriadToScreen = "EditingManagerHotkeySetTriadToScreen";

	public const string EditingManagerHotkeyCameraSmoothMode = "EditingManagerHotkeyCameraSmoothMode";

	public const string EditingManagerHotkeyDisplayNormalsOfSelectedEntities = "EditingManagerHotkeyDisplayNormalsOfSelectedEntities";

	public const string EditingManagerHotkeyChoosePhysicsMaterial = "EditingManagerHotkeyChoosePhysicsMaterial";

	public const string EditingManagerHotkeySwitchObjectsLockedForSelection = "EditingManagerHotkeySwitchObjectsLockedForSelection";

	public const string ApplicationHotkeyAnimationReload = "ApplicationHotkeyAnimationReload";

	public const string ApplicationHotkeyIncreasePingDelay = "ApplicationHotkeyIncreasePingDelay";

	public const string ApplicationHotkeyIncreaseLossRatio = "ApplicationHotkeyIncreaseLossRatio";

	public const string ApplicationHotkeySaveAllContentFilesWithType = "ApplicationHotkeySaveAllContentFilesWithType";

	public const string MissionHotkeySwitchAnimationDebugSystem = "MissionHotkeySwitchAnimationDebugSystem";

	public const string MissionHotkeyAssignMainAgentToDebugAgent = "MissionHotkeyAssignMainAgentToDebugAgent";

	public const string MissionHotkeyUseProgrammerSound = "MissionHotkeyUseProgrammerSound";

	public const string MissionHotkeySetDebugPathStartPos = "MissionHotkeySetDebugPathStartPos";

	public const string MissionHotkeySetDebugPathEndPos = "MissionHotkeySetDebugPathEndPos";

	public const string MissionHotkeyRenderCombatCollisionCapsules = "MissionHotkeyRenderCombatCollisionCapsules";

	public const string ModelviewerHotkeyApplyUpwardsForce = "ModelviewerHotkeyApplyUpwardsForce";

	public const string ModelviewerHotkeyApplyDownwardsForce = "ModelviewerHotkeyApplyDownwardsForce";

	public const string NavigationMeshBuilderHotkeyMakeFourLastVerticesFace = "NavigationMeshBuilderHotkeyMakeFourLastVerticesFace";

	public const string CameraControllerHotkeyMoveForward = "CameraControllerHotkeyMoveForward";

	public const string CameraControllerHotkeyMoveBackward = "CameraControllerHotkeyMoveBackward";

	public const string CameraControllerHotkeyMoveLeft = "CameraControllerHotkeyMoveLeft";

	public const string CameraControllerHotkeyMoveRight = "CameraControllerHotkeyMoveRight";

	public const string CameraControllerHotkeyMoveUpward = "CameraControllerHotkeyMoveUpward";

	public const string CameraControllerHotkeyMoveDownward = "CameraControllerHotkeyMoveDownward";

	public const string CameraControllerHotkeyPenCamera = "CameraControllerHotkeyPenCamera";

	public const string ClothSimulationHotkeyResetAllMeshes = "ClothSimulationHotkeyResetAllMeshes";

	public const string EngineInterfaceHotkeySwitchForwardPhysicxDebugMode = "EngineInterfaceHotkeySwitchForwardPhysicxDebugMode";

	public const string EngineInterfaceHotkeySwitchBackwardPhysicxDebugMode = "EngineInterfaceHotkeySwitchBackwardPhysicxDebugMode";

	public const string EngineInterfaceHotkeyShowPhysicsDebugInfo = "EngineInterfaceHotkeyShowPhysicsDebugInfo";

	public const string EngineInterfaceHotkeyShowProfileModes = "EngineInterfaceHotkeyShowProfileModes";

	public const string EngineInterfaceHotkeyShowDebugInfo = "EngineInterfaceHotkeyShowDebugInfo";

	public const string EngineInterfaceHotkeyDecreaseByTenDrawOneByOneIndex = "EngineInterfaceHotkeyDecreaseByTenDrawOneByOneIndex";

	public const string EngineInterfaceHotkeyIncreaseByTenDrawOneByOneIndex = "EngineInterfaceHotkeyIncreaseByTenDrawOneByOneIndex";

	public const string EngineInterfaceHotkeyDecreaseDrawOneByOneIndex = "EngineInterfaceHotkeyDecreaseDrawOneByOneIndex";

	public const string EngineInterfaceHotkeyIncreaseDrawOneByOneIndex = "EngineInterfaceHotkeyIncreaseDrawOneByOneIndex";

	public const string EngineInterfaceHotkeyForceSetDrawOneByOneIndexMinusone = "EngineInterfaceHotkeyForceSetDrawOneByOneIndexMinusone";

	public const string EngineInterfaceHotkeySetDrawOneByOneIndexMinusone = "EngineInterfaceHotkeySetDrawOneByOneIndexMinusone";

	public const string EngineInterfaceHotkeyReleaseUnusedMemory = "EngineInterfaceHotkeyReleaseUnusedMemory";

	public const string EngineInterfaceHotkeyChangeShaderVisualizationMode = "EngineInterfaceHotkeyChangeShaderVisualizationMode";

	public const string EngineInterfaceHotkeyOnlyRenderDeferredQuad = "EngineInterfaceHotkeyOnlyRenderDeferredQuad";

	public const string EngineInterfaceHotkeyOnlyRenderNonDeferredMeshes = "EngineInterfaceHotkeyOnlyRenderNonDeferredMeshes";

	public const string EngineInterfaceHotkeyChangeAnimationDebugMode = "EngineInterfaceHotkeyChangeAnimationDebugMode";

	public const string EngineInterfaceHotkeyTestAssertReport = "EngineInterfaceHotkeyTestAssertReport";

	public const string EngineInterfaceHotkeyTestCreateBugReportTask = "EngineInterfaceHotkeyTestCreateBugReportTask";

	public const string EngineInterfaceHotkeySlowmotion = "EngineInterfaceHotkeySlowmotion";

	public const string EngineInterfaceHotkeyRecompileShader = "EngineInterfaceHotkeyRecompileShader";

	public const string EngineInterfaceHotkeyToggleConsole = "EngineInterfaceHotkeyToggleConsole";

	public const string EngineInterfaceHotkeyShowConsoleManager = "EngineInterfaceHotkeyShowConsoleManager";

	public const string EngineInterfaceHotkeyShowDebugTools = "EngineInterfaceHotkeyShowDebugTools";

	public const string SceneHotkeyIncreaseEnforcedSkyboxIndex = "SceneHotkeyIncreaseEnforcedSkyboxIndex";

	public const string SceneHotkeyDecreaseEnforcedSkyboxIndex = "SceneHotkeyDecreaseEnforcedSkyboxIndex";

	public const string SceneHotkeyCheckBoundingBoxCorrectness = "SceneHotkeyCheckBoundingBoxCorrectness";

	public const string SceneHotkeyShowNavigationMeshIds = "SceneHotkeyShowNavigationMeshIds";

	public const string SceneHotkeyShowNavigationMeshIdsXray = "SceneHotkeyShowNavigationMeshIdsXray";

	public const string SceneHotkeyShowNavigationMeshIslands = "SceneHotkeyShowNavigationMeshIslands";

	public const string SceneHotkeySetNewCharacterDetailModifier = "SceneHotkeySetNewCharacterDetailModifier";

	public const string SceneHotkeyShowTerrainMaterials = "SceneHotkeyShowTerrainMaterials";

	public const string SceneViewHotkeyTakeHighQualityScreenshot = "SceneViewHotkeyTakeHighQualityScreenshot";

	public const string SoundManagerHotkeyReloadSounds = "SoundManagerHotkeyReloadSounds";

	public const string ReplayEditorHotkeyRenderSounds = "ReplayEditorHotkeyRenderSounds";

	public const string FrameMoveTaskHotkeyUseTelemetryProfiler = "FrameMoveTaskHotkeyUseTelemetryProfiler";

	public const string SkeletonHotkeyActivateDisableAnimationFpsOptimization = "SkeletonHotkeyActivateDisableAnimationFpsOptimization";

	public const string SkeletonHotkeyDisactiveDisableAnimationFpsOptimization = "SkeletonHotkeyDisactiveDisableAnimationFpsOptimization";

	public const string LibraryHotkeyDisableCommitChanges = "LibraryHotkeyDisableCommitChanges";

	public const string Numpad0 = "Numpad0";

	public const string Numpad1 = "Numpad1";

	public const string Numpad3 = "Numpad3";

	public const string Numpad5 = "Numpad5";

	public const string Numpad7 = "Numpad7";

	public const string Numpad9 = "Numpad9";

	public const string D0 = "D0";

	public const string D1 = "D1";

	public const string D2 = "D2";

	public const string D3 = "D3";

	public const string D4 = "D4";

	public const string D5 = "D5";

	public const string D6 = "D6";

	public const string D7 = "D7";

	public const string D8 = "D8";

	public const string D9 = "D9";

	public const string F1 = "F1";

	public const string F2 = "F2";

	public const string F3 = "F3";

	public const string F4 = "F4";

	public const string F5 = "F5";

	public const string F6 = "F6";

	public const string F7 = "F7";

	public const string F8 = "F8";

	public const string F9 = "F9";

	public const string F10 = "F10";

	public const string F11 = "F11";

	public const string Y = "Y";

	public const string A = "A";

	public const string F = "F";

	public const string B = "B";

	public const string N = "N";

	public const string C = "C";

	public const string E = "E";

	public const string J = "J";

	public const string Q = "Q";

	public const string H = "H";

	public const string W = "W";

	public const string S = "S";

	public const string U = "U";

	public const string T = "T";

	public const string K = "K";

	public const string M = "M";

	public const string G = "G";

	public const string D = "D";

	public const string Space = "Space";

	public const string UpArrow = "UpArrow";

	public const string LeftArrow = "LeftArrow";

	public const string DownArrow = "DownArrow";

	public const string RightArrow = "RightArrow";

	public const string NumpadArrowForward = "NumpadArrowForward";

	public const string NumpadArrowBackward = "NumpadArrowBackward";

	public const string NumpadArrowLeft = "NumpadArrowLeft";

	public const string NumpadArrowRight = "NumpadArrowRight";

	public const string SwapToEnemy = "SwapToEnemy";

	public const string ChangeEnemyTeam = "ChangeEnemyTeam";

	public const string Paste = "Paste";

	public const string Cut = "Cut";

	public const string Refresh = "Refresh";

	public const string EnterEditMode = "EnterEditMode";

	public const string FixSkeletons = "FixSkeletons";

	public const string Reset = "Reset";

	public const string AnimationTestControllerHotkeyUseWeaponTesting = "AnimationTestControllerHotkeyUseWeaponTesting";

	public const string BaseBattleMissionControllerHotkeyBecomePlayer = "BaseBattleMissionControllerHotkeyBecomePlayer";

	public const string BaseBattleMissionControllerHotkeyDrawNavMeshLines = "BaseBattleMissionControllerHotkeyDrawNavMeshLines";

	public const string ModuleHotkeyOpenDebug = "ModuleHotkeyOpenDebug";

	public const string FormationTestMissionControllerHotkeyChargeSide = "FormationTestMissionControllerHotkeyChargeSide";

	public const string FormationTestMissionControllerHotkeyToggleSide = "FormationTestMissionControllerHotkeyToggleSide";

	public const string FormationTestMissionControllerHotkeyToggleFactionBackward = "FormationTestMissionControllerHotkeyToggleFactionBackward";

	public const string FormationTestMissionControllerHotkeyToggleFactionForward = "FormationTestMissionControllerHotkeyToggleFactionForward";

	public const string FormationTestMissionControllerHotkeyToggleTroopForward = "FormationTestMissionControllerHotkeyToggleTroopForward";

	public const string FormationTestMissionControllerHotkeyToggleTroopBackward = "FormationTestMissionControllerHotkeyToggleTroopBackward";

	public const string FormationTestMissionControllerHotkeyIncreaseSpawnCount = "FormationTestMissionControllerHotkeyIncreaseSpawnCount";

	public const string FormationTestMissionControllerHotkeyDecreaseSpawnCount = "FormationTestMissionControllerHotkeyDecreaseSpawnCount";

	public const string FormationTestMissionControllerHotkeySpawnCustom = "FormationTestMissionControllerHotkeySpawnCustom";

	public const string FormationTestMissionControllerHotkeyOrderLooseAndInfantryFormation = "FormationTestMissionControllerHotkeyOrderLooseAndInfantryFormation";

	public const string FormationTestMissionControllerHotkeyOrderScatterAndRangedFormation = "FormationTestMissionControllerHotkeyOrderScatterAndRangedFormation";

	public const string FormationTestMissionControllerHotkeyOrderSkeinAndCavalryFormation = "FormationTestMissionControllerHotkeyOrderSkeinAndCavalryFormation";

	public const string FormationTestMissionControllerHotkeyOrderLineAndHorseArcherFormation = "FormationTestMissionControllerHotkeyOrderLineAndHorseArcherFormation";

	public const string FormationTestMissionControllerHotkeyOrderCircle = "FormationTestMissionControllerHotkeyOrderCircle";

	public const string FormationTestMissionControllerHotkeyOrderColumn = "FormationTestMissionControllerHotkeyOrderColumn";

	public const string FormationTestMissionControllerHotkeyOrderShieldWall = "FormationTestMissionControllerHotkeyOrderShieldWall";

	public const string FormationTestMissionControllerHotkeyOrderSquare = "FormationTestMissionControllerHotkeyOrderSquare";

	public const string AiTestMissionControllerHotkeySpawnFormation = "AiTestMissionControllerHotkeySpawnFormation";

	public const string TabbedPanelHotkeyDecreaseSelectedIndex = "TabbedPanelHotkeyDecreaseSelectedIndex";

	public const string TabbedPanelHotkeyIncreaseSelectedIndex = "TabbedPanelHotkeyIncreaseSelectedIndex";

	public const string MissionSingleplayerUiHandlerHotkeyUpdateItems = "MissionSingleplayerUiHandlerHotkeyUpdateItems";

	public const string MissionSingleplayerUiHandlerHotkeyJoinEnemyTeam = "MissionSingleplayerUiHandlerHotkeyJoinEnemyTeam";

	public const string SiegeDeploymentViewHotkeyTeleportMainAgent = "SiegeDeploymentViewHotkeyTeleportMainAgent";

	public const string SiegeDeploymentViewHotkeyFinishDeployment = "SiegeDeploymentViewHotkeyFinishDeployment";

	public const string CraftingScreenHotkeyEnableRuler = "CraftingScreenHotkeyEnableRuler";

	public const string CraftingScreenHotkeyEnableRulerPoint1 = "CraftingScreenHotkeyEnableRulerPoint1";

	public const string CraftingScreenHotkeyEnableRulerPoint2 = "CraftingScreenHotkeyEnableRulerPoint2";

	public const string CraftingScreenHotkeySwitchSelectedPieceMovement = "CraftingScreenHotkeySwitchSelectedPieceMovement";

	public const string CraftingScreenHotkeySetSelectedVariableIndexZero = "CraftingScreenHotkeySetSelectedVariableIndexZero";

	public const string CraftingScreenHotkeySetSelectedVariableIndexOne = "CraftingScreenHotkeySetSelectedVariableIndexOne";

	public const string CraftingScreenHotkeySetSelectedVariableIndexTwo = "CraftingScreenHotkeySetSelectedVariableIndexTwo";

	public const string CraftingScreenHotkeySelectPieceZero = "CraftingScreenHotkeySelectPieceZero";

	public const string CraftingScreenHotkeySelectPieceOne = "CraftingScreenHotkeySelectPieceOne";

	public const string CraftingScreenHotkeySelectPieceTwo = "CraftingScreenHotkeySelectPieceTwo";

	public const string CraftingScreenHotkeySelectPieceThree = "CraftingScreenHotkeySelectPieceThree";

	public const string MbFaceGeneratorScreenHotkeyCamDebugAndAdjustEnabled = "MbFaceGeneratorScreenHotkeyCamDebugAndAdjustEnabled";

	public const string MbFaceGeneratorScreenHotkeyNumpad0 = "MbFaceGeneratorScreenHotkeyNumpad0";

	public const string MbFaceGeneratorScreenHotkeyNumpad1 = "MbFaceGeneratorScreenHotkeyNumpad1";

	public const string MbFaceGeneratorScreenHotkeyNumpad2 = "MbFaceGeneratorScreenHotkeyNumpad2";

	public const string MbFaceGeneratorScreenHotkeyNumpad3 = "MbFaceGeneratorScreenHotkeyNumpad3";

	public const string MbFaceGeneratorScreenHotkeyNumpad4 = "MbFaceGeneratorScreenHotkeyNumpad4";

	public const string MbFaceGeneratorScreenHotkeyNumpad5 = "MbFaceGeneratorScreenHotkeyNumpad5";

	public const string MbFaceGeneratorScreenHotkeyNumpad6 = "MbFaceGeneratorScreenHotkeyNumpad6";

	public const string MbFaceGeneratorScreenHotkeyNumpad7 = "MbFaceGeneratorScreenHotkeyNumpad7";

	public const string MbFaceGeneratorScreenHotkeyNumpad8 = "MbFaceGeneratorScreenHotkeyNumpad8";

	public const string MbFaceGeneratorScreenHotkeyNumpad9 = "MbFaceGeneratorScreenHotkeyNumpad9";

	public const string MbFaceGeneratorScreenHotkeyResetFaceToDefault = "MbFaceGeneratorScreenHotkeyResetFaceToDefault";

	public const string MbFaceGeneratorScreenHotkeySetFaceKeyMax = "MbFaceGeneratorScreenHotkeySetFaceKeyMax";

	public const string MbFaceGeneratorScreenHotkeySetFaceKeyMin = "MbFaceGeneratorScreenHotkeySetFaceKeyMin";

	public const string MbFaceGeneratorScreenHotkeySetCurFaceKeyToMax = "MbFaceGeneratorScreenHotkeySetCurFaceKeyToMax";

	public const string MbFaceGeneratorScreenHotkeySetCurFaceKeyToMin = "MbFaceGeneratorScreenHotkeySetCurFaceKeyToMin";

	public const string SoftwareOcclusionCheckerHotkeySaveOcclusionImage = "SoftwareOcclusionCheckerHotkeySaveOcclusionImage";

	public const string MapScreenHotkeySwitchCampaignTrueSight = "MapScreenHotkeySwitchCampaignTrueSight";

	public const string MapScreenPrintMultiLineText = "MapScreenPrintMultiLineText";

	public const string MapScreenHotkeyShowPos = "MapScreenHotkeyShowPos";

	public const string MapScreenHotkeyOpenEncyclopedia = "MapScreenHotkeyOpenEncyclopedia";

	public const string ReplayCaptureLogicHotkeyRenderWithScreenshot = "ReplayCaptureLogicHotkeyRenderWithScreenshot";

	public const string MissionScreenHotkeyFixCamera = "MissionScreenHotkeyFixCamera";

	public const string MissionScreenHotkeyIncrementArtificialLag = "MissionScreenHotkeyIncrementArtificialLag";

	public const string MissionScreenHotkeyIncrementArtificialLoss = "MissionScreenHotkeyIncrementArtificialLoss";

	public const string MissionScreenHotkeyResetDebugVariables = "MissionScreenHotkeyResetDebugVariables";

	public const string MissionScreenHotkeySwitchCameraSmooth = "MissionScreenHotkeySwitchCameraSmooth";

	public const string MissionScreenHotkeyIncreaseFirstFormationWidth = "MissionScreenHotkeyIncreaseFirstFormationWidth";

	public const string MissionScreenHotkeyDecreaseFirstFormationWidth = "MissionScreenHotkeyDecreaseFirstFormationWidth";

	public const string MissionScreenHotkeyExtendedDebugKey = "MissionScreenHotkeyExtendedDebugKey";

	public const string MissionScreenHotkeyShowDebug = "MissionScreenHotkeyShowDebug";

	public const string MissionScreenHotkeyIncreaseTotalUploadLimit = "MissionScreenHotkeyIncreaseTotalUploadLimit";

	public const string MissionScreenIncreaseTotalUploadLimit = "MissionScreenIncreaseTotalUploadLimit";

	public const string MissionScreenHotkeyDecreaseRulerDistanceFromPivot = "MissionScreenHotkeyDecreaseRulerDistanceFromPivot";

	public const string MissionScreenHotkeyIncreaseRulerDistanceFromPivot = "MissionScreenHotkeyIncreaseRulerDistanceFromPivot";

	public const string DebugAgentTeleportMissionControllerHotkeyTeleportMainAgent = "DebugAgentTeleportMissionControllerHotkeyTeleportMainAgent";

	public const string DebugAgentTeleportMissionControllerHotkeyDisableScriptedMovement = "DebugAgentTeleportMissionControllerHotkeyDisableScriptedMovement";

	public const string MissionDebugHandlerHotkeyKillAI = "MissionDebugHandlerHotkeyKillAI";

	public const string MissionDebugHandlerHotkeyKillAttacker = "MissionDebugHandlerHotkeyKillAttacker";

	public const string MissionDebugHandlerHotkeyKillDefender = "MissionDebugHandlerHotkeyKillDefender";

	public const string MissionDebugHandlerHotkeyKillMainAgent = "MissionDebugHandlerHotkeyKillMainAgent";

	public const string MissionDebugHandlerHotkeyAttackingAiAgent = "MissionDebugHandlerHotkeyAttackingAiAgent";

	public const string MissionDebugHandlerHotkeyDefendingAiAgent = "MissionDebugHandlerHotkeyDefendingAiAgent";

	public const string MissionDebugHandlerHotkeyNormalAiAgent = "MissionDebugHandlerHotkeyNormalAiAgent";

	public const string MissionDebugHandlerHotkeyAiAgentSideZero = "MissionDebugHandlerHotkeyAiAgentSideZero";

	public const string MissionDebugHandlerHotkeyAiAgentSideOne = "MissionDebugHandlerHotkeyAiAgentSideOne";

	public const string MissionDebugHandlerHotkeyAiAgentSideTwo = "MissionDebugHandlerHotkeyAiAgentSideTwo";

	public const string MissionDebugHandlerHotkeyAiAgentSideThree = "MissionDebugHandlerHotkeyAiAgentSideThree";

	public const string MissionDebugHandlerHotkeyColorEnemyTeam = "MissionDebugHandlerHotkeyColorEnemyTeam";

	public const string MissionDebugHandlerHotkeyOpenMissionDebug = "MissionDebugHandlerHotkeyOpenMissionDebug";

	public const string UsableMachineAiBaseHotkeyShowMachineUsers = "UsableMachineAiBaseHotkeyShowMachineUsers";

	public const string UsableMachineAiBaseHotkeyRetreatScriptActive = "UsableMachineAiBaseHotkeyRetreatScriptActive";

	public const string UsableMachineAiBaseHotkeyRetreatScriptPassive = "UsableMachineAiBaseHotkeyRetreatScriptPassive";

	public const string CustomCameraMissionViewHotkeyIncreaseCustomCameraIndex = "CustomCameraMissionViewHotkeyIncreaseCustomCameraIndex";

	public const string DebugSiegeBehaviorHotkeyAimAtBallistas = "DebugSiegeBehaviorHotkeyAimAtBallistas";

	public const string DebugSiegeBehaviorHotkeyAimAtMangonels = "DebugSiegeBehaviorHotkeyAimAtMangonels";

	public const string DebugSiegeBehaviorHotkeyAimAtBattlements = "DebugSiegeBehaviorHotkeyAimAtBattlements";

	public const string DebugSiegeBehaviorHotkeyAimAtNone = "DebugSiegeBehaviorHotkeyAimAtNone";

	public const string DebugSiegeBehaviorHotkeyAimAtNone2 = "DebugSiegeBehaviorHotkeyAimAtNone2";

	public const string DebugSiegeBehaviorHotkeyTargetDebugActive = "DebugSiegeBehaviorHotkeyTargetDebugActive";

	public const string DebugSiegeBehaviorHotkeyTargetDebugDisactive = "DebugSiegeBehaviorHotkeyTargetDebugDisactive";

	public const string DebugSiegeBehaviorHotkeyAimAtRam = "DebugSiegeBehaviorHotkeyAimAtRam";

	public const string DebugSiegeBehaviorHotkeyAimAtSt = "DebugSiegeBehaviorHotkeyAimAtSt";

	public const string DebugSiegeBehaviorHotkeyAimAtBallistas2 = "DebugSiegeBehaviorHotkeyAimAtBallistas2";

	public const string DebugSiegeBehaviorHotkeyAimAtMangonels2 = "DebugSiegeBehaviorHotkeyAimAtMangonels2";

	public const string DebugNetworkEventStatisticsHotkeyClear = "DebugNetworkEventStatisticsHotkeyClear";

	public const string DebugNetworkEventStatisticsHotkeyDumpDataAndClear = "DebugNetworkEventStatisticsHotkeyDumpDataAndClear";

	public const string DebugNetworkEventStatisticsHotkeyDumpData = "DebugNetworkEventStatisticsHotkeyDumpData";

	public const string DebugNetworkEventStatisticsHotkeyClearReplicationData = "DebugNetworkEventStatisticsHotkeyClearReplicationData";

	public const string DebugNetworkEventStatisticsHotkeyDumpReplicationData = "DebugNetworkEventStatisticsHotkeyDumpReplicationData";

	public const string DebugNetworkEventStatisticsHotkeyDumpAndClearReplicationData = "DebugNetworkEventStatisticsHotkeyDumpAndClearReplicationData";

	public const string DebugNetworkEventStatisticsHotkeyToggleActive = "DebugNetworkEventStatisticsHotkeyToggleActive";

	public const string AiSelectDebugAgent1 = "AiSelectDebugAgent1";

	public const string AiSelectDebugAgent2 = "AiSelectDebugAgent2";

	public const string AiClearDebugAgents = "AiClearDebugAgents";

	public const string DebugCustomBattlePredefinedSettings1 = "DebugCustomBattlePredefinedSettings1";

	public const string CraftingScreenResetVariable = "CraftingScreenResetVariable";

	public const string DisableParallelSettlementPositionUpdate = "DisableParallelSettlementPositionUpdate";

	public const string OpenUIEditor = "OpenUIEditor";

	public const string ToggleUI = "ToggleUI";

	public const string LeaveWhileInConversation = "LeaveWhileInConversation";

	public const string ShowHighlightsSummary = "ShowHighlightsSummary";

	public const string ResetMusicParameters = "ResetMusicParameters";

	public const string UIExtendedDebugKey = "UIExtendedDebugKey";

	public const string FaceGeneratorExtendedDebugKey = "FaceGeneratorExtendedDebugKey";

	public const string FormationTestMissionExtendedDebugKey = "FormationTestMissionExtendedDebugKey";

	public const string FormationTestMissionExtendedDebugKey2 = "FormationTestMissionExtendedDebugKey2";

	public DebugHotKeyCategory()
		: base("Debug", 0, GameKeyContextType.AuxiliaryNotSerialized)
	{
		RegisterDebugHotkey("CraftingScreenResetVariable", InputKey.Home, HotKey.Modifiers.None);
		RegisterDebugHotkey("LeftMouseButton", InputKey.LeftMouseButton, HotKey.Modifiers.None);
		RegisterDebugHotkey("RightMouseButton", InputKey.RightMouseButton, HotKey.Modifiers.None);
		RegisterDebugHotkey("Score", InputKey.O, HotKey.Modifiers.Control);
		RegisterDebugHotkey("Copy", InputKey.C, HotKey.Modifiers.Control);
		RegisterDebugHotkey("Duplicate", InputKey.D, HotKey.Modifiers.Control);
		RegisterDebugHotkey("Numpad0", InputKey.Numpad0, HotKey.Modifiers.None);
		RegisterDebugHotkey("Numpad1", InputKey.Numpad1, HotKey.Modifiers.None);
		RegisterDebugHotkey("NumpadArrowBackward", InputKey.Numpad2, HotKey.Modifiers.None);
		RegisterDebugHotkey("Numpad3", InputKey.Numpad3, HotKey.Modifiers.None);
		RegisterDebugHotkey("NumpadArrowLeft", InputKey.Numpad4, HotKey.Modifiers.None);
		RegisterDebugHotkey("Numpad5", InputKey.Numpad5, HotKey.Modifiers.None);
		RegisterDebugHotkey("NumpadArrowRight", InputKey.Numpad6, HotKey.Modifiers.None);
		RegisterDebugHotkey("Numpad7", InputKey.Numpad7, HotKey.Modifiers.None);
		RegisterDebugHotkey("NumpadArrowForward", InputKey.Numpad8, HotKey.Modifiers.None);
		RegisterDebugHotkey("Numpad9", InputKey.Numpad9, HotKey.Modifiers.None);
		RegisterDebugHotkey("Reset", InputKey.R, HotKey.Modifiers.None);
		RegisterDebugHotkey("SiegeDeploymentViewHotkeyFinishDeployment", InputKey.P, HotKey.Modifiers.None);
		RegisterDebugHotkey("MapScreenHotkeyShowPos", InputKey.T, HotKey.Modifiers.None);
		RegisterDebugHotkey("Y", InputKey.Y, HotKey.Modifiers.None);
		RegisterDebugHotkey("E", InputKey.E, HotKey.Modifiers.None);
		RegisterDebugHotkey("J", InputKey.J, HotKey.Modifiers.None);
		RegisterDebugHotkey("H", InputKey.H, HotKey.Modifiers.None);
		RegisterDebugHotkey("F1", InputKey.F1, HotKey.Modifiers.None);
		RegisterDebugHotkey("F2", InputKey.F2, HotKey.Modifiers.None);
		RegisterDebugHotkey("F3", InputKey.F3, HotKey.Modifiers.None);
		RegisterDebugHotkey("F4", InputKey.F4, HotKey.Modifiers.None);
		RegisterDebugHotkey("F5", InputKey.F5, HotKey.Modifiers.None);
		RegisterDebugHotkey("F6", InputKey.F6, HotKey.Modifiers.None);
		RegisterDebugHotkey("F7", InputKey.F7, HotKey.Modifiers.None);
		RegisterDebugHotkey("F8", InputKey.F8, HotKey.Modifiers.None);
		RegisterDebugHotkey("F9", InputKey.F9, HotKey.Modifiers.None);
		RegisterDebugHotkey("F10", InputKey.F10, HotKey.Modifiers.None);
		RegisterDebugHotkey("F11", InputKey.F11, HotKey.Modifiers.None);
		RegisterDebugHotkey("MissionScreenHotkeyShowDebug", InputKey.F12, HotKey.Modifiers.None);
		RegisterDebugHotkey("ToggleUI", InputKey.F10, HotKey.Modifiers.Alt | HotKey.Modifiers.Control);
		RegisterDebugHotkey("MissionScreenHotkeyExtendedDebugKey", InputKey.K, HotKey.Modifiers.None);
		RegisterDebugHotkey("DebugAgentTeleportMissionControllerHotkeyDisableScriptedMovement", InputKey.Home, HotKey.Modifiers.None);
		RegisterDebugHotkey("EngineInterfaceHotkeyTakeScreenShot", InputKey.Insert, HotKey.Modifiers.None);
		RegisterDebugHotkey("MissionScreenIncreaseTotalUploadLimit", InputKey.PageDown, HotKey.Modifiers.None);
		RegisterDebugHotkey("MissionScreenHotkeyIncreaseTotalUploadLimit", InputKey.PageUp, HotKey.Modifiers.None);
		RegisterDebugHotkey("MissionScreenHotkeyDecreaseRulerDistanceFromPivot", InputKey.PageDown, HotKey.Modifiers.None);
		RegisterDebugHotkey("MissionScreenHotkeyIncreaseRulerDistanceFromPivot", InputKey.PageUp, HotKey.Modifiers.None);
		RegisterDebugHotkey("MissionHotkeySetDebugPathEndPos", InputKey.X, HotKey.Modifiers.None);
		RegisterDebugHotkey("MissionHotkeyRenderCombatCollisionCapsules", InputKey.V, HotKey.Modifiers.None);
		RegisterDebugHotkey("T", InputKey.T, HotKey.Modifiers.None);
		RegisterDebugHotkey("K", InputKey.K, HotKey.Modifiers.None);
		RegisterDebugHotkey("U", InputKey.U, HotKey.Modifiers.None);
		RegisterDebugHotkey("MissionHotkeySetDebugPathStartPos", InputKey.Z, HotKey.Modifiers.None);
		RegisterDebugHotkey("A", InputKey.A, HotKey.Modifiers.None);
		RegisterDebugHotkey("B", InputKey.B, HotKey.Modifiers.None);
		RegisterDebugHotkey("C", InputKey.C, HotKey.Modifiers.None);
		RegisterDebugHotkey("AiTestMissionControllerHotkeySpawnFormation", InputKey.M, HotKey.Modifiers.None);
		RegisterDebugHotkey("N", InputKey.N, HotKey.Modifiers.None);
		RegisterDebugHotkey("Q", InputKey.Q, HotKey.Modifiers.None);
		RegisterDebugHotkey("Space", InputKey.Space, HotKey.Modifiers.None);
		RegisterDebugHotkey("F", InputKey.F, HotKey.Modifiers.None);
		RegisterDebugHotkey("W", InputKey.W, HotKey.Modifiers.None);
		RegisterDebugHotkey("S", InputKey.S, HotKey.Modifiers.None);
		RegisterDebugHotkey("D", InputKey.D, HotKey.Modifiers.None);
		RegisterDebugHotkey("UpArrow", InputKey.Up, HotKey.Modifiers.None);
		RegisterDebugHotkey("LeftArrow", InputKey.Left, HotKey.Modifiers.None);
		RegisterDebugHotkey("RightArrow", InputKey.Right, HotKey.Modifiers.None);
		RegisterDebugHotkey("DownArrow", InputKey.Down, HotKey.Modifiers.None);
		RegisterDebugHotkey("D0", InputKey.D0, HotKey.Modifiers.None);
		RegisterDebugHotkey("D1", InputKey.D1, HotKey.Modifiers.None);
		RegisterDebugHotkey("D2", InputKey.D2, HotKey.Modifiers.None);
		RegisterDebugHotkey("D3", InputKey.D3, HotKey.Modifiers.None);
		RegisterDebugHotkey("D4", InputKey.D4, HotKey.Modifiers.None);
		RegisterDebugHotkey("D5", InputKey.D5, HotKey.Modifiers.None);
		RegisterDebugHotkey("D6", InputKey.D6, HotKey.Modifiers.None);
		RegisterDebugHotkey("D7", InputKey.D7, HotKey.Modifiers.None);
		RegisterDebugHotkey("D8", InputKey.D8, HotKey.Modifiers.None);
		RegisterDebugHotkey("D9", InputKey.D9, HotKey.Modifiers.None);
		RegisterDebugHotkey("ChangeEnemyTeam", InputKey.NumpadSlash, HotKey.Modifiers.Control);
		RegisterDebugHotkey("AnimationTestControllerHotkeyUseWeaponTesting", InputKey.L, HotKey.Modifiers.None);
		RegisterDebugHotkey("SwapToEnemy", InputKey.E, HotKey.Modifiers.Shift | HotKey.Modifiers.Control);
		RegisterDebugHotkey("BaseBattleMissionControllerHotkeyBecomePlayer", InputKey.P, HotKey.Modifiers.Shift | HotKey.Modifiers.Control);
		RegisterDebugHotkey("BaseBattleMissionControllerHotkeyDrawNavMeshLines", InputKey.N, HotKey.Modifiers.Shift);
		RegisterDebugHotkey("ModuleHotkeyOpenDebug", InputKey.D, HotKey.Modifiers.Control);
		RegisterDebugHotkey("TabbedPanelHotkeyDecreaseSelectedIndex", InputKey.Tab, HotKey.Modifiers.Shift | HotKey.Modifiers.Control);
		RegisterDebugHotkey("TabbedPanelHotkeyIncreaseSelectedIndex", InputKey.Tab, HotKey.Modifiers.Control);
		RegisterDebugHotkey("Paste", InputKey.V, HotKey.Modifiers.Control);
		RegisterDebugHotkey("Cut", InputKey.X, HotKey.Modifiers.Control);
		RegisterDebugHotkey("MissionSingleplayerUiHandlerHotkeyUpdateItems", InputKey.U, HotKey.Modifiers.Control);
		RegisterDebugHotkey("MissionSingleplayerUiHandlerHotkeyJoinEnemyTeam", InputKey.Numpad9, HotKey.Modifiers.Control);
		RegisterDebugHotkey("MapScreenHotkeySwitchCampaignTrueSight", InputKey.T, HotKey.Modifiers.Alt | HotKey.Modifiers.Control);
		RegisterDebugHotkey("SiegeDeploymentViewHotkeyTeleportMainAgent", InputKey.LeftMouseButton, HotKey.Modifiers.Control);
		RegisterDebugHotkey("SoftwareOcclusionCheckerHotkeySaveOcclusionImage", InputKey.NumpadSlash, HotKey.Modifiers.None);
		RegisterDebugHotkey("UIExtendedDebugKey", InputKey.G, HotKey.Modifiers.None);
		RegisterDebugHotkey("MapScreenHotkeyOpenEncyclopedia", InputKey.E, HotKey.Modifiers.None);
		RegisterDebugHotkey("MbFaceGeneratorScreenHotkeyCamDebugAndAdjustEnabled", InputKey.L, HotKey.Modifiers.Control);
		RegisterDebugHotkey("FaceGeneratorExtendedDebugKey", InputKey.F, HotKey.Modifiers.None);
		RegisterDebugHotkey("MbFaceGeneratorScreenHotkeyNumpad0", InputKey.Numpad0, HotKey.Modifiers.None);
		RegisterDebugHotkey("MbFaceGeneratorScreenHotkeyNumpad1", InputKey.Numpad1, HotKey.Modifiers.None);
		RegisterDebugHotkey("MbFaceGeneratorScreenHotkeyNumpad2", InputKey.Numpad2, HotKey.Modifiers.None);
		RegisterDebugHotkey("MbFaceGeneratorScreenHotkeyNumpad3", InputKey.Numpad3, HotKey.Modifiers.None);
		RegisterDebugHotkey("MbFaceGeneratorScreenHotkeyNumpad4", InputKey.Numpad4, HotKey.Modifiers.None);
		RegisterDebugHotkey("MbFaceGeneratorScreenHotkeyNumpad5", InputKey.Numpad5, HotKey.Modifiers.None);
		RegisterDebugHotkey("MbFaceGeneratorScreenHotkeyNumpad6", InputKey.Numpad6, HotKey.Modifiers.None);
		RegisterDebugHotkey("MbFaceGeneratorScreenHotkeyNumpad7", InputKey.Numpad7, HotKey.Modifiers.None);
		RegisterDebugHotkey("MbFaceGeneratorScreenHotkeyNumpad8", InputKey.Numpad8, HotKey.Modifiers.None);
		RegisterDebugHotkey("MbFaceGeneratorScreenHotkeyNumpad9", InputKey.Numpad9, HotKey.Modifiers.None);
		RegisterDebugHotkey("MbFaceGeneratorScreenHotkeyResetFaceToDefault", InputKey.D0, HotKey.Modifiers.None);
		RegisterDebugHotkey("FormationTestMissionExtendedDebugKey", InputKey.Period, HotKey.Modifiers.None);
		RegisterDebugHotkey("FormationTestMissionControllerHotkeyChargeSide", InputKey.Numpad9, HotKey.Modifiers.None);
		RegisterDebugHotkey("FormationTestMissionControllerHotkeyToggleSide", InputKey.NumpadPeriod, HotKey.Modifiers.None);
		RegisterDebugHotkey("FormationTestMissionControllerHotkeyToggleFactionBackward", InputKey.Numpad1, HotKey.Modifiers.None);
		RegisterDebugHotkey("FormationTestMissionControllerHotkeyToggleFactionForward", InputKey.Numpad2, HotKey.Modifiers.None);
		RegisterDebugHotkey("FormationTestMissionControllerHotkeyToggleTroopForward", InputKey.Numpad5, HotKey.Modifiers.None);
		RegisterDebugHotkey("FormationTestMissionControllerHotkeyToggleTroopBackward", InputKey.Numpad4, HotKey.Modifiers.None);
		RegisterDebugHotkey("FormationTestMissionControllerHotkeyIncreaseSpawnCount", InputKey.Numpad8, HotKey.Modifiers.None);
		RegisterDebugHotkey("FormationTestMissionControllerHotkeyDecreaseSpawnCount", InputKey.Numpad7, HotKey.Modifiers.None);
		RegisterDebugHotkey("FormationTestMissionControllerHotkeySpawnCustom", InputKey.Numpad0, HotKey.Modifiers.None);
		RegisterDebugHotkey("FormationTestMissionControllerHotkeyOrderLooseAndInfantryFormation", InputKey.Numpad1, HotKey.Modifiers.Alt);
		RegisterDebugHotkey("FormationTestMissionControllerHotkeyOrderScatterAndRangedFormation", InputKey.Numpad2, HotKey.Modifiers.Alt);
		RegisterDebugHotkey("FormationTestMissionControllerHotkeyOrderSkeinAndCavalryFormation", InputKey.Numpad3, HotKey.Modifiers.Alt);
		RegisterDebugHotkey("FormationTestMissionControllerHotkeyOrderLineAndHorseArcherFormation", InputKey.Numpad4, HotKey.Modifiers.Alt);
		RegisterDebugHotkey("FormationTestMissionExtendedDebugKey2", InputKey.S, HotKey.Modifiers.None);
		RegisterDebugHotkey("FormationTestMissionControllerHotkeyOrderCircle", InputKey.Numpad5, HotKey.Modifiers.Alt);
		RegisterDebugHotkey("FormationTestMissionControllerHotkeyOrderColumn", InputKey.Numpad6, HotKey.Modifiers.Alt);
		RegisterDebugHotkey("FormationTestMissionControllerHotkeyOrderShieldWall", InputKey.Numpad7, HotKey.Modifiers.Alt);
		RegisterDebugHotkey("FormationTestMissionControllerHotkeyOrderSquare", InputKey.Numpad8, HotKey.Modifiers.Alt);
		RegisterDebugHotkey("CustomCameraMissionViewHotkeyIncreaseCustomCameraIndex", InputKey.Y, HotKey.Modifiers.Alt | HotKey.Modifiers.Control);
		RegisterDebugHotkey("CraftingScreenHotkeyEnableRuler", InputKey.R, HotKey.Modifiers.Shift | HotKey.Modifiers.Control);
		RegisterDebugHotkey("CraftingScreenHotkeyEnableRulerPoint1", InputKey.D1, HotKey.Modifiers.Shift | HotKey.Modifiers.Control);
		RegisterDebugHotkey("CraftingScreenHotkeyEnableRulerPoint2", InputKey.D2, HotKey.Modifiers.Shift | HotKey.Modifiers.Control);
		RegisterDebugHotkey("CraftingScreenHotkeySwitchSelectedPieceMovement", InputKey.M, HotKey.Modifiers.Shift | HotKey.Modifiers.Control);
		RegisterDebugHotkey("CraftingScreenHotkeySetSelectedVariableIndexZero", InputKey.Numpad1, HotKey.Modifiers.Shift | HotKey.Modifiers.Control);
		RegisterDebugHotkey("CraftingScreenHotkeySetSelectedVariableIndexOne", InputKey.Numpad2, HotKey.Modifiers.Shift | HotKey.Modifiers.Control);
		RegisterDebugHotkey("CraftingScreenHotkeySetSelectedVariableIndexTwo", InputKey.Numpad3, HotKey.Modifiers.Shift | HotKey.Modifiers.Control);
		RegisterDebugHotkey("CraftingScreenHotkeySelectPieceZero", InputKey.D1, HotKey.Modifiers.Alt | HotKey.Modifiers.Control);
		RegisterDebugHotkey("CraftingScreenHotkeySelectPieceOne", InputKey.D2, HotKey.Modifiers.Alt | HotKey.Modifiers.Control);
		RegisterDebugHotkey("CraftingScreenHotkeySelectPieceTwo", InputKey.D3, HotKey.Modifiers.Alt | HotKey.Modifiers.Control);
		RegisterDebugHotkey("CraftingScreenHotkeySelectPieceThree", InputKey.D4, HotKey.Modifiers.Alt | HotKey.Modifiers.Control);
		RegisterDebugHotkey("MbFaceGeneratorScreenHotkeySetFaceKeyMin", InputKey.D1, HotKey.Modifiers.Shift);
		RegisterDebugHotkey("MbFaceGeneratorScreenHotkeySetFaceKeyMax", InputKey.D2, HotKey.Modifiers.Shift);
		RegisterDebugHotkey("Refresh", InputKey.R, HotKey.Modifiers.Control);
		RegisterDebugHotkey("MbFaceGeneratorScreenHotkeySetCurFaceKeyToMin", InputKey.D1, HotKey.Modifiers.Alt);
		RegisterDebugHotkey("MbFaceGeneratorScreenHotkeySetCurFaceKeyToMax", InputKey.D2, HotKey.Modifiers.Alt);
		RegisterDebugHotkey("MissionScreenHotkeyFixCamera", InputKey.Home, HotKey.Modifiers.Alt | HotKey.Modifiers.Control);
		RegisterDebugHotkey("ReplayCaptureLogicHotkeyRenderWithScreenshot", InputKey.Numpad1, HotKey.Modifiers.Control);
		RegisterDebugHotkey("EnterEditMode", InputKey.E, HotKey.Modifiers.Control);
		RegisterDebugHotkey("MissionScreenHotkeyIncrementArtificialLag", InputKey.L, HotKey.Modifiers.Shift);
		RegisterDebugHotkey("MissionScreenHotkeyIncrementArtificialLoss", InputKey.J, HotKey.Modifiers.Shift);
		RegisterDebugHotkey("MissionScreenHotkeyResetDebugVariables", InputKey.X, HotKey.Modifiers.Shift | HotKey.Modifiers.Control);
		RegisterDebugHotkey("FixSkeletons", InputKey.K, HotKey.Modifiers.Shift | HotKey.Modifiers.Control);
		RegisterDebugHotkey("MissionScreenHotkeySwitchCameraSmooth", InputKey.S, HotKey.Modifiers.Alt | HotKey.Modifiers.Control);
		RegisterDebugHotkey("DebugAgentTeleportMissionControllerHotkeyTeleportMainAgent", InputKey.MiddleMouseButton, HotKey.Modifiers.Alt | HotKey.Modifiers.Control);
		RegisterDebugHotkey("MissionDebugHandlerHotkeyKillAI", InputKey.Numpad1, HotKey.Modifiers.Alt);
		RegisterDebugHotkey("MissionDebugHandlerHotkeyOpenMissionDebug", InputKey.PageUp, HotKey.Modifiers.Control);
		RegisterDebugHotkey("MissionDebugHandlerHotkeyKillDefender", InputKey.Numpad2, HotKey.Modifiers.Alt);
		RegisterDebugHotkey("MissionDebugHandlerHotkeyKillAttacker", InputKey.Numpad3, HotKey.Modifiers.Alt);
		RegisterDebugHotkey("MissionDebugHandlerHotkeyKillMainAgent", InputKey.Numpad9, HotKey.Modifiers.Alt);
		RegisterDebugHotkey("MissionDebugHandlerHotkeyAttackingAiAgent", InputKey.LeftMouseButton, HotKey.Modifiers.Alt);
		RegisterDebugHotkey("MissionDebugHandlerHotkeyDefendingAiAgent", InputKey.RightMouseButton, HotKey.Modifiers.Alt);
		RegisterDebugHotkey("MissionDebugHandlerHotkeyNormalAiAgent", InputKey.MiddleMouseButton, HotKey.Modifiers.Alt);
		RegisterDebugHotkey("MissionDebugHandlerHotkeyAiAgentSideZero", InputKey.Left, HotKey.Modifiers.Alt);
		RegisterDebugHotkey("MissionDebugHandlerHotkeyAiAgentSideOne", InputKey.Right, HotKey.Modifiers.Alt);
		RegisterDebugHotkey("MissionDebugHandlerHotkeyAiAgentSideTwo", InputKey.Up, HotKey.Modifiers.Alt);
		RegisterDebugHotkey("MissionDebugHandlerHotkeyAiAgentSideThree", InputKey.Down, HotKey.Modifiers.Alt);
		RegisterDebugHotkey("UsableMachineAiBaseHotkeyShowMachineUsers", InputKey.Up, HotKey.Modifiers.Alt | HotKey.Modifiers.Control);
		RegisterDebugHotkey("UsableMachineAiBaseHotkeyRetreatScriptActive", InputKey.Numpad7, HotKey.Modifiers.Control);
		RegisterDebugHotkey("UsableMachineAiBaseHotkeyRetreatScriptPassive", InputKey.Numpad7, HotKey.Modifiers.Alt);
		RegisterDebugHotkey("MissionDebugHandlerHotkeyColorEnemyTeam", InputKey.NumpadMultiply, HotKey.Modifiers.Alt);
		RegisterDebugHotkey("DebugSiegeBehaviorHotkeyAimAtBallistas", InputKey.Numpad3, HotKey.Modifiers.Control);
		RegisterDebugHotkey("DebugSiegeBehaviorHotkeyAimAtBallistas2", InputKey.Numpad3, HotKey.Modifiers.Shift);
		RegisterDebugHotkey("DebugSiegeBehaviorHotkeyAimAtMangonels", InputKey.Numpad4, HotKey.Modifiers.Control);
		RegisterDebugHotkey("DebugSiegeBehaviorHotkeyAimAtMangonels2", InputKey.Numpad4, HotKey.Modifiers.Shift);
		RegisterDebugHotkey("DebugSiegeBehaviorHotkeyAimAtBattlements", InputKey.Numpad5, HotKey.Modifiers.Control);
		RegisterDebugHotkey("DebugSiegeBehaviorHotkeyTargetDebugActive", InputKey.Numpad8, HotKey.Modifiers.Control);
		RegisterDebugHotkey("DebugSiegeBehaviorHotkeyTargetDebugDisactive", InputKey.Numpad8, HotKey.Modifiers.Shift);
		RegisterDebugHotkey("DebugSiegeBehaviorHotkeyAimAtNone", InputKey.Numpad0, HotKey.Modifiers.Control);
		RegisterDebugHotkey("DebugSiegeBehaviorHotkeyAimAtNone2", InputKey.Numpad0, HotKey.Modifiers.Shift);
		RegisterDebugHotkey("DebugSiegeBehaviorHotkeyAimAtRam", InputKey.Numpad1, HotKey.Modifiers.Shift);
		RegisterDebugHotkey("DebugSiegeBehaviorHotkeyAimAtSt", InputKey.Numpad2, HotKey.Modifiers.Shift);
		RegisterDebugHotkey("DebugNetworkEventStatisticsHotkeyToggleActive", InputKey.F6, HotKey.Modifiers.Shift | HotKey.Modifiers.Alt);
		RegisterDebugHotkey("DebugNetworkEventStatisticsHotkeyClear", InputKey.M, HotKey.Modifiers.Shift | HotKey.Modifiers.Alt);
		RegisterDebugHotkey("DebugNetworkEventStatisticsHotkeyDumpDataAndClear", InputKey.N, HotKey.Modifiers.Shift | HotKey.Modifiers.Alt);
		RegisterDebugHotkey("DebugNetworkEventStatisticsHotkeyDumpData", InputKey.J, HotKey.Modifiers.Shift | HotKey.Modifiers.Alt);
		RegisterDebugHotkey("DebugNetworkEventStatisticsHotkeyClearReplicationData", InputKey.V, HotKey.Modifiers.Shift | HotKey.Modifiers.Alt);
		RegisterDebugHotkey("DebugNetworkEventStatisticsHotkeyDumpReplicationData", InputKey.C, HotKey.Modifiers.Shift | HotKey.Modifiers.Alt);
		RegisterDebugHotkey("DebugNetworkEventStatisticsHotkeyDumpAndClearReplicationData", InputKey.B, HotKey.Modifiers.Shift | HotKey.Modifiers.Alt);
		RegisterDebugHotkey("DebugCustomBattlePredefinedSettings1", InputKey.F1, HotKey.Modifiers.Control);
		RegisterDebugHotkey("DisableParallelSettlementPositionUpdate", InputKey.M, HotKey.Modifiers.Alt);
		RegisterDebugHotkey("OpenUIEditor", InputKey.U, HotKey.Modifiers.Alt);
		RegisterDebugHotkey("MapScreenPrintMultiLineText", InputKey.Minus, HotKey.Modifiers.None);
		RegisterDebugHotkey("LeaveWhileInConversation", InputKey.Tab, HotKey.Modifiers.None);
		RegisterDebugHotkey("ShowHighlightsSummary", InputKey.H, HotKey.Modifiers.Shift);
		RegisterDebugHotkey("ResetMusicParameters", InputKey.R, HotKey.Modifiers.Shift | HotKey.Modifiers.Alt | HotKey.Modifiers.Control);
	}

	private void RegisterDebugHotkey(string id, InputKey hotkeyKey, HotKey.Modifiers modifiers, HotKey.Modifiers negativeModifiers = HotKey.Modifiers.None)
	{
		RegisterHotKey(new HotKey(id, "Debug", hotkeyKey, modifiers, negativeModifiers));
	}
}
