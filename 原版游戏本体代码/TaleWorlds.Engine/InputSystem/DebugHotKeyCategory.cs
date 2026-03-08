using System;
using TaleWorlds.InputSystem;

namespace TaleWorlds.Engine.InputSystem
{
	// Token: 0x020000AE RID: 174
	public class DebugHotKeyCategory : GameKeyContext
	{
		// Token: 0x06000F79 RID: 3961 RVA: 0x000128AC File Offset: 0x00010AAC
		public DebugHotKeyCategory()
			: base("Debug", 0, GameKeyContext.GameKeyContextType.AuxiliaryNotSerialized)
		{
			this.RegisterDebugHotkey("CraftingScreenResetVariable", InputKey.Home, HotKey.Modifiers.None, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("LeftMouseButton", InputKey.LeftMouseButton, HotKey.Modifiers.None, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("RightMouseButton", InputKey.RightMouseButton, HotKey.Modifiers.None, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("Score", InputKey.O, HotKey.Modifiers.Control, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("Copy", InputKey.C, HotKey.Modifiers.Control, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("Duplicate", InputKey.D, HotKey.Modifiers.Control, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("Numpad0", InputKey.Numpad0, HotKey.Modifiers.None, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("Numpad1", InputKey.Numpad1, HotKey.Modifiers.None, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("NumpadArrowBackward", InputKey.Numpad2, HotKey.Modifiers.None, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("Numpad3", InputKey.Numpad3, HotKey.Modifiers.None, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("NumpadArrowLeft", InputKey.Numpad4, HotKey.Modifiers.None, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("Numpad5", InputKey.Numpad5, HotKey.Modifiers.None, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("NumpadArrowRight", InputKey.Numpad6, HotKey.Modifiers.None, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("Numpad7", InputKey.Numpad7, HotKey.Modifiers.None, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("NumpadArrowForward", InputKey.Numpad8, HotKey.Modifiers.None, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("Numpad9", InputKey.Numpad9, HotKey.Modifiers.None, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("Reset", InputKey.R, HotKey.Modifiers.None, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("SiegeDeploymentViewHotkeyFinishDeployment", InputKey.P, HotKey.Modifiers.None, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("MapScreenHotkeyShowPos", InputKey.T, HotKey.Modifiers.None, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("Y", InputKey.Y, HotKey.Modifiers.None, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("E", InputKey.E, HotKey.Modifiers.None, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("J", InputKey.J, HotKey.Modifiers.None, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("H", InputKey.H, HotKey.Modifiers.None, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("F1", InputKey.F1, HotKey.Modifiers.None, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("F2", InputKey.F2, HotKey.Modifiers.None, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("F3", InputKey.F3, HotKey.Modifiers.None, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("F4", InputKey.F4, HotKey.Modifiers.None, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("F5", InputKey.F5, HotKey.Modifiers.None, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("F6", InputKey.F6, HotKey.Modifiers.None, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("F7", InputKey.F7, HotKey.Modifiers.None, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("F8", InputKey.F8, HotKey.Modifiers.None, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("F9", InputKey.F9, HotKey.Modifiers.None, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("F10", InputKey.F10, HotKey.Modifiers.None, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("F11", InputKey.F11, HotKey.Modifiers.None, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("MissionScreenHotkeyShowDebug", InputKey.F12, HotKey.Modifiers.None, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("ToggleUI", InputKey.F10, HotKey.Modifiers.Alt | HotKey.Modifiers.Control, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("MissionScreenHotkeyExtendedDebugKey", InputKey.K, HotKey.Modifiers.None, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("DebugAgentTeleportMissionControllerHotkeyDisableScriptedMovement", InputKey.Home, HotKey.Modifiers.None, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("EngineInterfaceHotkeyTakeScreenShot", InputKey.Insert, HotKey.Modifiers.None, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("MissionScreenIncreaseTotalUploadLimit", InputKey.PageDown, HotKey.Modifiers.None, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("MissionScreenHotkeyIncreaseTotalUploadLimit", InputKey.PageUp, HotKey.Modifiers.None, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("MissionScreenHotkeyDecreaseRulerDistanceFromPivot", InputKey.PageDown, HotKey.Modifiers.None, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("MissionScreenHotkeyIncreaseRulerDistanceFromPivot", InputKey.PageUp, HotKey.Modifiers.None, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("MissionHotkeySetDebugPathEndPos", InputKey.X, HotKey.Modifiers.None, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("MissionHotkeyRenderCombatCollisionCapsules", InputKey.V, HotKey.Modifiers.None, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("T", InputKey.T, HotKey.Modifiers.None, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("K", InputKey.K, HotKey.Modifiers.None, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("U", InputKey.U, HotKey.Modifiers.None, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("MissionHotkeySetDebugPathStartPos", InputKey.Z, HotKey.Modifiers.None, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("A", InputKey.A, HotKey.Modifiers.None, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("B", InputKey.B, HotKey.Modifiers.None, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("C", InputKey.C, HotKey.Modifiers.None, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("AiTestMissionControllerHotkeySpawnFormation", InputKey.M, HotKey.Modifiers.None, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("N", InputKey.N, HotKey.Modifiers.None, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("Q", InputKey.Q, HotKey.Modifiers.None, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("Space", InputKey.Space, HotKey.Modifiers.None, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("F", InputKey.F, HotKey.Modifiers.None, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("W", InputKey.W, HotKey.Modifiers.None, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("S", InputKey.S, HotKey.Modifiers.None, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("D", InputKey.D, HotKey.Modifiers.None, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("UpArrow", InputKey.Up, HotKey.Modifiers.None, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("LeftArrow", InputKey.Left, HotKey.Modifiers.None, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("RightArrow", InputKey.Right, HotKey.Modifiers.None, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("DownArrow", InputKey.Down, HotKey.Modifiers.None, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("D0", InputKey.D0, HotKey.Modifiers.None, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("D1", InputKey.D1, HotKey.Modifiers.None, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("D2", InputKey.D2, HotKey.Modifiers.None, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("D3", InputKey.D3, HotKey.Modifiers.None, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("D4", InputKey.D4, HotKey.Modifiers.None, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("D5", InputKey.D5, HotKey.Modifiers.None, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("D6", InputKey.D6, HotKey.Modifiers.None, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("D7", InputKey.D7, HotKey.Modifiers.None, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("D8", InputKey.D8, HotKey.Modifiers.None, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("D9", InputKey.D9, HotKey.Modifiers.None, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("ChangeEnemyTeam", InputKey.NumpadSlash, HotKey.Modifiers.Control, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("AnimationTestControllerHotkeyUseWeaponTesting", InputKey.L, HotKey.Modifiers.None, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("SwapToEnemy", InputKey.E, HotKey.Modifiers.Shift | HotKey.Modifiers.Control, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("BaseBattleMissionControllerHotkeyBecomePlayer", InputKey.P, HotKey.Modifiers.Shift | HotKey.Modifiers.Control, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("BaseBattleMissionControllerHotkeyDrawNavMeshLines", InputKey.N, HotKey.Modifiers.Shift, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("ModuleHotkeyOpenDebug", InputKey.D, HotKey.Modifiers.Control, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("TabbedPanelHotkeyDecreaseSelectedIndex", InputKey.Tab, HotKey.Modifiers.Shift | HotKey.Modifiers.Control, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("TabbedPanelHotkeyIncreaseSelectedIndex", InputKey.Tab, HotKey.Modifiers.Control, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("Paste", InputKey.V, HotKey.Modifiers.Control, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("Cut", InputKey.X, HotKey.Modifiers.Control, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("MissionSingleplayerUiHandlerHotkeyUpdateItems", InputKey.U, HotKey.Modifiers.Control, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("MissionSingleplayerUiHandlerHotkeyJoinEnemyTeam", InputKey.Numpad9, HotKey.Modifiers.Control, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("MapScreenHotkeySwitchCampaignTrueSight", InputKey.T, HotKey.Modifiers.Alt | HotKey.Modifiers.Control, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("SiegeDeploymentViewHotkeyTeleportMainAgent", InputKey.LeftMouseButton, HotKey.Modifiers.Control, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("SoftwareOcclusionCheckerHotkeySaveOcclusionImage", InputKey.NumpadSlash, HotKey.Modifiers.None, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("UIExtendedDebugKey", InputKey.G, HotKey.Modifiers.None, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("MapScreenHotkeyOpenEncyclopedia", InputKey.E, HotKey.Modifiers.None, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("MbFaceGeneratorScreenHotkeyCamDebugAndAdjustEnabled", InputKey.L, HotKey.Modifiers.Control, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("FaceGeneratorExtendedDebugKey", InputKey.F, HotKey.Modifiers.None, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("MbFaceGeneratorScreenHotkeyNumpad0", InputKey.Numpad0, HotKey.Modifiers.None, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("MbFaceGeneratorScreenHotkeyNumpad1", InputKey.Numpad1, HotKey.Modifiers.None, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("MbFaceGeneratorScreenHotkeyNumpad2", InputKey.Numpad2, HotKey.Modifiers.None, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("MbFaceGeneratorScreenHotkeyNumpad3", InputKey.Numpad3, HotKey.Modifiers.None, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("MbFaceGeneratorScreenHotkeyNumpad4", InputKey.Numpad4, HotKey.Modifiers.None, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("MbFaceGeneratorScreenHotkeyNumpad5", InputKey.Numpad5, HotKey.Modifiers.None, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("MbFaceGeneratorScreenHotkeyNumpad6", InputKey.Numpad6, HotKey.Modifiers.None, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("MbFaceGeneratorScreenHotkeyNumpad7", InputKey.Numpad7, HotKey.Modifiers.None, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("MbFaceGeneratorScreenHotkeyNumpad8", InputKey.Numpad8, HotKey.Modifiers.None, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("MbFaceGeneratorScreenHotkeyNumpad9", InputKey.Numpad9, HotKey.Modifiers.None, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("MbFaceGeneratorScreenHotkeyResetFaceToDefault", InputKey.D0, HotKey.Modifiers.None, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("FormationTestMissionExtendedDebugKey", InputKey.Period, HotKey.Modifiers.None, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("FormationTestMissionControllerHotkeyChargeSide", InputKey.Numpad9, HotKey.Modifiers.None, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("FormationTestMissionControllerHotkeyToggleSide", InputKey.NumpadPeriod, HotKey.Modifiers.None, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("FormationTestMissionControllerHotkeyToggleFactionBackward", InputKey.Numpad1, HotKey.Modifiers.None, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("FormationTestMissionControllerHotkeyToggleFactionForward", InputKey.Numpad2, HotKey.Modifiers.None, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("FormationTestMissionControllerHotkeyToggleTroopForward", InputKey.Numpad5, HotKey.Modifiers.None, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("FormationTestMissionControllerHotkeyToggleTroopBackward", InputKey.Numpad4, HotKey.Modifiers.None, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("FormationTestMissionControllerHotkeyIncreaseSpawnCount", InputKey.Numpad8, HotKey.Modifiers.None, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("FormationTestMissionControllerHotkeyDecreaseSpawnCount", InputKey.Numpad7, HotKey.Modifiers.None, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("FormationTestMissionControllerHotkeySpawnCustom", InputKey.Numpad0, HotKey.Modifiers.None, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("FormationTestMissionControllerHotkeyOrderLooseAndInfantryFormation", InputKey.Numpad1, HotKey.Modifiers.Alt, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("FormationTestMissionControllerHotkeyOrderScatterAndRangedFormation", InputKey.Numpad2, HotKey.Modifiers.Alt, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("FormationTestMissionControllerHotkeyOrderSkeinAndCavalryFormation", InputKey.Numpad3, HotKey.Modifiers.Alt, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("FormationTestMissionControllerHotkeyOrderLineAndHorseArcherFormation", InputKey.Numpad4, HotKey.Modifiers.Alt, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("FormationTestMissionExtendedDebugKey2", InputKey.S, HotKey.Modifiers.None, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("FormationTestMissionControllerHotkeyOrderCircle", InputKey.Numpad5, HotKey.Modifiers.Alt, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("FormationTestMissionControllerHotkeyOrderColumn", InputKey.Numpad6, HotKey.Modifiers.Alt, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("FormationTestMissionControllerHotkeyOrderShieldWall", InputKey.Numpad7, HotKey.Modifiers.Alt, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("FormationTestMissionControllerHotkeyOrderSquare", InputKey.Numpad8, HotKey.Modifiers.Alt, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("CustomCameraMissionViewHotkeyIncreaseCustomCameraIndex", InputKey.Y, HotKey.Modifiers.Alt | HotKey.Modifiers.Control, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("CraftingScreenHotkeyEnableRuler", InputKey.R, HotKey.Modifiers.Shift | HotKey.Modifiers.Control, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("CraftingScreenHotkeyEnableRulerPoint1", InputKey.D1, HotKey.Modifiers.Shift | HotKey.Modifiers.Control, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("CraftingScreenHotkeyEnableRulerPoint2", InputKey.D2, HotKey.Modifiers.Shift | HotKey.Modifiers.Control, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("CraftingScreenHotkeySwitchSelectedPieceMovement", InputKey.M, HotKey.Modifiers.Shift | HotKey.Modifiers.Control, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("CraftingScreenHotkeySetSelectedVariableIndexZero", InputKey.Numpad1, HotKey.Modifiers.Shift | HotKey.Modifiers.Control, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("CraftingScreenHotkeySetSelectedVariableIndexOne", InputKey.Numpad2, HotKey.Modifiers.Shift | HotKey.Modifiers.Control, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("CraftingScreenHotkeySetSelectedVariableIndexTwo", InputKey.Numpad3, HotKey.Modifiers.Shift | HotKey.Modifiers.Control, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("CraftingScreenHotkeySelectPieceZero", InputKey.D1, HotKey.Modifiers.Alt | HotKey.Modifiers.Control, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("CraftingScreenHotkeySelectPieceOne", InputKey.D2, HotKey.Modifiers.Alt | HotKey.Modifiers.Control, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("CraftingScreenHotkeySelectPieceTwo", InputKey.D3, HotKey.Modifiers.Alt | HotKey.Modifiers.Control, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("CraftingScreenHotkeySelectPieceThree", InputKey.D4, HotKey.Modifiers.Alt | HotKey.Modifiers.Control, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("MbFaceGeneratorScreenHotkeySetFaceKeyMin", InputKey.D1, HotKey.Modifiers.Shift, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("MbFaceGeneratorScreenHotkeySetFaceKeyMax", InputKey.D2, HotKey.Modifiers.Shift, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("Refresh", InputKey.R, HotKey.Modifiers.Control, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("MbFaceGeneratorScreenHotkeySetCurFaceKeyToMin", InputKey.D1, HotKey.Modifiers.Alt, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("MbFaceGeneratorScreenHotkeySetCurFaceKeyToMax", InputKey.D2, HotKey.Modifiers.Alt, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("MissionScreenHotkeyFixCamera", InputKey.Home, HotKey.Modifiers.Alt | HotKey.Modifiers.Control, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("ReplayCaptureLogicHotkeyRenderWithScreenshot", InputKey.Numpad1, HotKey.Modifiers.Control, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("EnterEditMode", InputKey.E, HotKey.Modifiers.Control, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("MissionScreenHotkeyIncrementArtificialLag", InputKey.L, HotKey.Modifiers.Shift, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("MissionScreenHotkeyIncrementArtificialLoss", InputKey.J, HotKey.Modifiers.Shift, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("MissionScreenHotkeyResetDebugVariables", InputKey.X, HotKey.Modifiers.Shift | HotKey.Modifiers.Control, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("FixSkeletons", InputKey.K, HotKey.Modifiers.Shift | HotKey.Modifiers.Control, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("MissionScreenHotkeySwitchCameraSmooth", InputKey.S, HotKey.Modifiers.Alt | HotKey.Modifiers.Control, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("DebugAgentTeleportMissionControllerHotkeyTeleportMainAgent", InputKey.MiddleMouseButton, HotKey.Modifiers.Alt | HotKey.Modifiers.Control, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("MissionDebugHandlerHotkeyKillAI", InputKey.Numpad1, HotKey.Modifiers.Alt, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("MissionDebugHandlerHotkeyOpenMissionDebug", InputKey.PageUp, HotKey.Modifiers.Control, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("MissionDebugHandlerHotkeyKillDefender", InputKey.Numpad2, HotKey.Modifiers.Alt, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("MissionDebugHandlerHotkeyKillAttacker", InputKey.Numpad3, HotKey.Modifiers.Alt, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("MissionDebugHandlerHotkeyKillMainAgent", InputKey.Numpad9, HotKey.Modifiers.Alt, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("MissionDebugHandlerHotkeyAttackingAiAgent", InputKey.LeftMouseButton, HotKey.Modifiers.Alt, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("MissionDebugHandlerHotkeyDefendingAiAgent", InputKey.RightMouseButton, HotKey.Modifiers.Alt, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("MissionDebugHandlerHotkeyNormalAiAgent", InputKey.MiddleMouseButton, HotKey.Modifiers.Alt, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("MissionDebugHandlerHotkeyAiAgentSideZero", InputKey.Left, HotKey.Modifiers.Alt, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("MissionDebugHandlerHotkeyAiAgentSideOne", InputKey.Right, HotKey.Modifiers.Alt, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("MissionDebugHandlerHotkeyAiAgentSideTwo", InputKey.Up, HotKey.Modifiers.Alt, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("MissionDebugHandlerHotkeyAiAgentSideThree", InputKey.Down, HotKey.Modifiers.Alt, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("UsableMachineAiBaseHotkeyShowMachineUsers", InputKey.Up, HotKey.Modifiers.Alt | HotKey.Modifiers.Control, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("UsableMachineAiBaseHotkeyRetreatScriptActive", InputKey.Numpad7, HotKey.Modifiers.Control, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("UsableMachineAiBaseHotkeyRetreatScriptPassive", InputKey.Numpad7, HotKey.Modifiers.Alt, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("MissionDebugHandlerHotkeyColorEnemyTeam", InputKey.NumpadMultiply, HotKey.Modifiers.Alt, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("DebugSiegeBehaviorHotkeyAimAtBallistas", InputKey.Numpad3, HotKey.Modifiers.Control, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("DebugSiegeBehaviorHotkeyAimAtBallistas2", InputKey.Numpad3, HotKey.Modifiers.Shift, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("DebugSiegeBehaviorHotkeyAimAtMangonels", InputKey.Numpad4, HotKey.Modifiers.Control, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("DebugSiegeBehaviorHotkeyAimAtMangonels2", InputKey.Numpad4, HotKey.Modifiers.Shift, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("DebugSiegeBehaviorHotkeyAimAtBattlements", InputKey.Numpad5, HotKey.Modifiers.Control, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("DebugSiegeBehaviorHotkeyTargetDebugActive", InputKey.Numpad8, HotKey.Modifiers.Control, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("DebugSiegeBehaviorHotkeyTargetDebugDisactive", InputKey.Numpad8, HotKey.Modifiers.Shift, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("DebugSiegeBehaviorHotkeyAimAtNone", InputKey.Numpad0, HotKey.Modifiers.Control, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("DebugSiegeBehaviorHotkeyAimAtNone2", InputKey.Numpad0, HotKey.Modifiers.Shift, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("DebugSiegeBehaviorHotkeyAimAtRam", InputKey.Numpad1, HotKey.Modifiers.Shift, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("DebugSiegeBehaviorHotkeyAimAtSt", InputKey.Numpad2, HotKey.Modifiers.Shift, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("DebugNetworkEventStatisticsHotkeyToggleActive", InputKey.F6, HotKey.Modifiers.Shift | HotKey.Modifiers.Alt, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("DebugNetworkEventStatisticsHotkeyClear", InputKey.M, HotKey.Modifiers.Shift | HotKey.Modifiers.Alt, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("DebugNetworkEventStatisticsHotkeyDumpDataAndClear", InputKey.N, HotKey.Modifiers.Shift | HotKey.Modifiers.Alt, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("DebugNetworkEventStatisticsHotkeyDumpData", InputKey.J, HotKey.Modifiers.Shift | HotKey.Modifiers.Alt, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("DebugNetworkEventStatisticsHotkeyClearReplicationData", InputKey.V, HotKey.Modifiers.Shift | HotKey.Modifiers.Alt, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("DebugNetworkEventStatisticsHotkeyDumpReplicationData", InputKey.C, HotKey.Modifiers.Shift | HotKey.Modifiers.Alt, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("DebugNetworkEventStatisticsHotkeyDumpAndClearReplicationData", InputKey.B, HotKey.Modifiers.Shift | HotKey.Modifiers.Alt, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("DebugCustomBattlePredefinedSettings1", InputKey.F1, HotKey.Modifiers.Control, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("DisableParallelSettlementPositionUpdate", InputKey.M, HotKey.Modifiers.Alt, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("OpenUIEditor", InputKey.U, HotKey.Modifiers.Alt, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("MapScreenPrintMultiLineText", InputKey.Minus, HotKey.Modifiers.None, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("LeaveWhileInConversation", InputKey.Tab, HotKey.Modifiers.None, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("ShowHighlightsSummary", InputKey.H, HotKey.Modifiers.Shift, HotKey.Modifiers.None);
			this.RegisterDebugHotkey("ResetMusicParameters", InputKey.R, HotKey.Modifiers.Shift | HotKey.Modifiers.Alt | HotKey.Modifiers.Control, HotKey.Modifiers.None);
		}

		// Token: 0x06000F7A RID: 3962 RVA: 0x00013428 File Offset: 0x00011628
		private void RegisterDebugHotkey(string id, InputKey hotkeyKey, HotKey.Modifiers modifiers, HotKey.Modifiers negativeModifiers = HotKey.Modifiers.None)
		{
			base.RegisterHotKey(new HotKey(id, "Debug", hotkeyKey, modifiers, negativeModifiers), true);
		}

		// Token: 0x0400023C RID: 572
		public const string CategoryId = "Debug";

		// Token: 0x0400023D RID: 573
		public const string LeftMouseButton = "LeftMouseButton";

		// Token: 0x0400023E RID: 574
		public const string RightMouseButton = "RightMouseButton";

		// Token: 0x0400023F RID: 575
		public const string SelectAll = "SelectAll";

		// Token: 0x04000240 RID: 576
		public const string Redo = "Redo";

		// Token: 0x04000241 RID: 577
		public const string Undo = "Undo";

		// Token: 0x04000242 RID: 578
		public const string Copy = "Copy";

		// Token: 0x04000243 RID: 579
		public const string Duplicate = "Duplicate";

		// Token: 0x04000244 RID: 580
		public const string Score = "Score";

		// Token: 0x04000245 RID: 581
		public const string SetOriginToZero = "SetOriginToZero";

		// Token: 0x04000246 RID: 582
		public const string TestEngineCrash = "TestEngineCrash";

		// Token: 0x04000247 RID: 583
		public const string AgentHotkeySwitchRender = "AgentHotkeySwitchRender";

		// Token: 0x04000248 RID: 584
		public const string AgentHotkeySwitchFaceAnimationDebug = "AgentHotkeySwitchFaceAnimationDebug";

		// Token: 0x04000249 RID: 585
		public const string AgentHotkeyCheckCollisionCapsule = "AgentHotkeyCheckCollisionCapsule";

		// Token: 0x0400024A RID: 586
		public const string EngineInterfaceHotkeyWireframe = "EngineInterfaceHotkeyWireframe";

		// Token: 0x0400024B RID: 587
		public const string EngineInterfaceHotkeyWireframe2 = "EngineInterfaceHotkeyWireframe2";

		// Token: 0x0400024C RID: 588
		public const string EngineInterfaceHotkeyTakeScreenShot = "EngineInterfaceHotkeyTakeScreenShot";

		// Token: 0x0400024D RID: 589
		public const string EditingManagerHotkeyCrashReporting = "EditingManagerHotkeyCrashReporting";

		// Token: 0x0400024E RID: 590
		public const string EditingManagerHotkeyEmergencySceneSaving = "EditingManagerHotkeyEmergencySceneSaving";

		// Token: 0x0400024F RID: 591
		public const string EditingManagerHotkeyAssertTestEntityOperations = "EditingManagerHotkeyAssertTestEntityOperations";

		// Token: 0x04000250 RID: 592
		public const string EditingManagerHotkeyUpdateSceneDialog = "EditingManagerHotkeyUpdateSceneDialog";

		// Token: 0x04000251 RID: 593
		public const string EditingManagerHotkeySetTriadToWorld = "EditingManagerHotkeySetTriadToWorld";

		// Token: 0x04000252 RID: 594
		public const string EditingManagerHotkeySetTriadToLocal = "EditingManagerHotkeySetTriadToLocal";

		// Token: 0x04000253 RID: 595
		public const string EditingManagerHotkeySetTriadToScreen = "EditingManagerHotkeySetTriadToScreen";

		// Token: 0x04000254 RID: 596
		public const string EditingManagerHotkeyCameraSmoothMode = "EditingManagerHotkeyCameraSmoothMode";

		// Token: 0x04000255 RID: 597
		public const string EditingManagerHotkeyDisplayNormalsOfSelectedEntities = "EditingManagerHotkeyDisplayNormalsOfSelectedEntities";

		// Token: 0x04000256 RID: 598
		public const string EditingManagerHotkeyChoosePhysicsMaterial = "EditingManagerHotkeyChoosePhysicsMaterial";

		// Token: 0x04000257 RID: 599
		public const string EditingManagerHotkeySwitchObjectsLockedForSelection = "EditingManagerHotkeySwitchObjectsLockedForSelection";

		// Token: 0x04000258 RID: 600
		public const string ApplicationHotkeyAnimationReload = "ApplicationHotkeyAnimationReload";

		// Token: 0x04000259 RID: 601
		public const string ApplicationHotkeyIncreasePingDelay = "ApplicationHotkeyIncreasePingDelay";

		// Token: 0x0400025A RID: 602
		public const string ApplicationHotkeyIncreaseLossRatio = "ApplicationHotkeyIncreaseLossRatio";

		// Token: 0x0400025B RID: 603
		public const string ApplicationHotkeySaveAllContentFilesWithType = "ApplicationHotkeySaveAllContentFilesWithType";

		// Token: 0x0400025C RID: 604
		public const string MissionHotkeySwitchAnimationDebugSystem = "MissionHotkeySwitchAnimationDebugSystem";

		// Token: 0x0400025D RID: 605
		public const string MissionHotkeyAssignMainAgentToDebugAgent = "MissionHotkeyAssignMainAgentToDebugAgent";

		// Token: 0x0400025E RID: 606
		public const string MissionHotkeyUseProgrammerSound = "MissionHotkeyUseProgrammerSound";

		// Token: 0x0400025F RID: 607
		public const string MissionHotkeySetDebugPathStartPos = "MissionHotkeySetDebugPathStartPos";

		// Token: 0x04000260 RID: 608
		public const string MissionHotkeySetDebugPathEndPos = "MissionHotkeySetDebugPathEndPos";

		// Token: 0x04000261 RID: 609
		public const string MissionHotkeyRenderCombatCollisionCapsules = "MissionHotkeyRenderCombatCollisionCapsules";

		// Token: 0x04000262 RID: 610
		public const string ModelviewerHotkeyApplyUpwardsForce = "ModelviewerHotkeyApplyUpwardsForce";

		// Token: 0x04000263 RID: 611
		public const string ModelviewerHotkeyApplyDownwardsForce = "ModelviewerHotkeyApplyDownwardsForce";

		// Token: 0x04000264 RID: 612
		public const string NavigationMeshBuilderHotkeyMakeFourLastVerticesFace = "NavigationMeshBuilderHotkeyMakeFourLastVerticesFace";

		// Token: 0x04000265 RID: 613
		public const string CameraControllerHotkeyMoveForward = "CameraControllerHotkeyMoveForward";

		// Token: 0x04000266 RID: 614
		public const string CameraControllerHotkeyMoveBackward = "CameraControllerHotkeyMoveBackward";

		// Token: 0x04000267 RID: 615
		public const string CameraControllerHotkeyMoveLeft = "CameraControllerHotkeyMoveLeft";

		// Token: 0x04000268 RID: 616
		public const string CameraControllerHotkeyMoveRight = "CameraControllerHotkeyMoveRight";

		// Token: 0x04000269 RID: 617
		public const string CameraControllerHotkeyMoveUpward = "CameraControllerHotkeyMoveUpward";

		// Token: 0x0400026A RID: 618
		public const string CameraControllerHotkeyMoveDownward = "CameraControllerHotkeyMoveDownward";

		// Token: 0x0400026B RID: 619
		public const string CameraControllerHotkeyPenCamera = "CameraControllerHotkeyPenCamera";

		// Token: 0x0400026C RID: 620
		public const string ClothSimulationHotkeyResetAllMeshes = "ClothSimulationHotkeyResetAllMeshes";

		// Token: 0x0400026D RID: 621
		public const string EngineInterfaceHotkeySwitchForwardPhysicxDebugMode = "EngineInterfaceHotkeySwitchForwardPhysicxDebugMode";

		// Token: 0x0400026E RID: 622
		public const string EngineInterfaceHotkeySwitchBackwardPhysicxDebugMode = "EngineInterfaceHotkeySwitchBackwardPhysicxDebugMode";

		// Token: 0x0400026F RID: 623
		public const string EngineInterfaceHotkeyShowPhysicsDebugInfo = "EngineInterfaceHotkeyShowPhysicsDebugInfo";

		// Token: 0x04000270 RID: 624
		public const string EngineInterfaceHotkeyShowProfileModes = "EngineInterfaceHotkeyShowProfileModes";

		// Token: 0x04000271 RID: 625
		public const string EngineInterfaceHotkeyShowDebugInfo = "EngineInterfaceHotkeyShowDebugInfo";

		// Token: 0x04000272 RID: 626
		public const string EngineInterfaceHotkeyDecreaseByTenDrawOneByOneIndex = "EngineInterfaceHotkeyDecreaseByTenDrawOneByOneIndex";

		// Token: 0x04000273 RID: 627
		public const string EngineInterfaceHotkeyIncreaseByTenDrawOneByOneIndex = "EngineInterfaceHotkeyIncreaseByTenDrawOneByOneIndex";

		// Token: 0x04000274 RID: 628
		public const string EngineInterfaceHotkeyDecreaseDrawOneByOneIndex = "EngineInterfaceHotkeyDecreaseDrawOneByOneIndex";

		// Token: 0x04000275 RID: 629
		public const string EngineInterfaceHotkeyIncreaseDrawOneByOneIndex = "EngineInterfaceHotkeyIncreaseDrawOneByOneIndex";

		// Token: 0x04000276 RID: 630
		public const string EngineInterfaceHotkeyForceSetDrawOneByOneIndexMinusone = "EngineInterfaceHotkeyForceSetDrawOneByOneIndexMinusone";

		// Token: 0x04000277 RID: 631
		public const string EngineInterfaceHotkeySetDrawOneByOneIndexMinusone = "EngineInterfaceHotkeySetDrawOneByOneIndexMinusone";

		// Token: 0x04000278 RID: 632
		public const string EngineInterfaceHotkeyReleaseUnusedMemory = "EngineInterfaceHotkeyReleaseUnusedMemory";

		// Token: 0x04000279 RID: 633
		public const string EngineInterfaceHotkeyChangeShaderVisualizationMode = "EngineInterfaceHotkeyChangeShaderVisualizationMode";

		// Token: 0x0400027A RID: 634
		public const string EngineInterfaceHotkeyOnlyRenderDeferredQuad = "EngineInterfaceHotkeyOnlyRenderDeferredQuad";

		// Token: 0x0400027B RID: 635
		public const string EngineInterfaceHotkeyOnlyRenderNonDeferredMeshes = "EngineInterfaceHotkeyOnlyRenderNonDeferredMeshes";

		// Token: 0x0400027C RID: 636
		public const string EngineInterfaceHotkeyChangeAnimationDebugMode = "EngineInterfaceHotkeyChangeAnimationDebugMode";

		// Token: 0x0400027D RID: 637
		public const string EngineInterfaceHotkeyTestAssertReport = "EngineInterfaceHotkeyTestAssertReport";

		// Token: 0x0400027E RID: 638
		public const string EngineInterfaceHotkeyTestCreateBugReportTask = "EngineInterfaceHotkeyTestCreateBugReportTask";

		// Token: 0x0400027F RID: 639
		public const string EngineInterfaceHotkeySlowmotion = "EngineInterfaceHotkeySlowmotion";

		// Token: 0x04000280 RID: 640
		public const string EngineInterfaceHotkeyRecompileShader = "EngineInterfaceHotkeyRecompileShader";

		// Token: 0x04000281 RID: 641
		public const string EngineInterfaceHotkeyToggleConsole = "EngineInterfaceHotkeyToggleConsole";

		// Token: 0x04000282 RID: 642
		public const string EngineInterfaceHotkeyShowConsoleManager = "EngineInterfaceHotkeyShowConsoleManager";

		// Token: 0x04000283 RID: 643
		public const string EngineInterfaceHotkeyShowDebugTools = "EngineInterfaceHotkeyShowDebugTools";

		// Token: 0x04000284 RID: 644
		public const string SceneHotkeyIncreaseEnforcedSkyboxIndex = "SceneHotkeyIncreaseEnforcedSkyboxIndex";

		// Token: 0x04000285 RID: 645
		public const string SceneHotkeyDecreaseEnforcedSkyboxIndex = "SceneHotkeyDecreaseEnforcedSkyboxIndex";

		// Token: 0x04000286 RID: 646
		public const string SceneHotkeyCheckBoundingBoxCorrectness = "SceneHotkeyCheckBoundingBoxCorrectness";

		// Token: 0x04000287 RID: 647
		public const string SceneHotkeyShowNavigationMeshIds = "SceneHotkeyShowNavigationMeshIds";

		// Token: 0x04000288 RID: 648
		public const string SceneHotkeyShowNavigationMeshIdsXray = "SceneHotkeyShowNavigationMeshIdsXray";

		// Token: 0x04000289 RID: 649
		public const string SceneHotkeyShowNavigationMeshIslands = "SceneHotkeyShowNavigationMeshIslands";

		// Token: 0x0400028A RID: 650
		public const string SceneHotkeySetNewCharacterDetailModifier = "SceneHotkeySetNewCharacterDetailModifier";

		// Token: 0x0400028B RID: 651
		public const string SceneHotkeyShowTerrainMaterials = "SceneHotkeyShowTerrainMaterials";

		// Token: 0x0400028C RID: 652
		public const string SceneViewHotkeyTakeHighQualityScreenshot = "SceneViewHotkeyTakeHighQualityScreenshot";

		// Token: 0x0400028D RID: 653
		public const string SoundManagerHotkeyReloadSounds = "SoundManagerHotkeyReloadSounds";

		// Token: 0x0400028E RID: 654
		public const string ReplayEditorHotkeyRenderSounds = "ReplayEditorHotkeyRenderSounds";

		// Token: 0x0400028F RID: 655
		public const string FrameMoveTaskHotkeyUseTelemetryProfiler = "FrameMoveTaskHotkeyUseTelemetryProfiler";

		// Token: 0x04000290 RID: 656
		public const string SkeletonHotkeyActivateDisableAnimationFpsOptimization = "SkeletonHotkeyActivateDisableAnimationFpsOptimization";

		// Token: 0x04000291 RID: 657
		public const string SkeletonHotkeyDisactiveDisableAnimationFpsOptimization = "SkeletonHotkeyDisactiveDisableAnimationFpsOptimization";

		// Token: 0x04000292 RID: 658
		public const string LibraryHotkeyDisableCommitChanges = "LibraryHotkeyDisableCommitChanges";

		// Token: 0x04000293 RID: 659
		public const string Numpad0 = "Numpad0";

		// Token: 0x04000294 RID: 660
		public const string Numpad1 = "Numpad1";

		// Token: 0x04000295 RID: 661
		public const string Numpad3 = "Numpad3";

		// Token: 0x04000296 RID: 662
		public const string Numpad5 = "Numpad5";

		// Token: 0x04000297 RID: 663
		public const string Numpad7 = "Numpad7";

		// Token: 0x04000298 RID: 664
		public const string Numpad9 = "Numpad9";

		// Token: 0x04000299 RID: 665
		public const string D0 = "D0";

		// Token: 0x0400029A RID: 666
		public const string D1 = "D1";

		// Token: 0x0400029B RID: 667
		public const string D2 = "D2";

		// Token: 0x0400029C RID: 668
		public const string D3 = "D3";

		// Token: 0x0400029D RID: 669
		public const string D4 = "D4";

		// Token: 0x0400029E RID: 670
		public const string D5 = "D5";

		// Token: 0x0400029F RID: 671
		public const string D6 = "D6";

		// Token: 0x040002A0 RID: 672
		public const string D7 = "D7";

		// Token: 0x040002A1 RID: 673
		public const string D8 = "D8";

		// Token: 0x040002A2 RID: 674
		public const string D9 = "D9";

		// Token: 0x040002A3 RID: 675
		public const string F1 = "F1";

		// Token: 0x040002A4 RID: 676
		public const string F2 = "F2";

		// Token: 0x040002A5 RID: 677
		public const string F3 = "F3";

		// Token: 0x040002A6 RID: 678
		public const string F4 = "F4";

		// Token: 0x040002A7 RID: 679
		public const string F5 = "F5";

		// Token: 0x040002A8 RID: 680
		public const string F6 = "F6";

		// Token: 0x040002A9 RID: 681
		public const string F7 = "F7";

		// Token: 0x040002AA RID: 682
		public const string F8 = "F8";

		// Token: 0x040002AB RID: 683
		public const string F9 = "F9";

		// Token: 0x040002AC RID: 684
		public const string F10 = "F10";

		// Token: 0x040002AD RID: 685
		public const string F11 = "F11";

		// Token: 0x040002AE RID: 686
		public const string Y = "Y";

		// Token: 0x040002AF RID: 687
		public const string A = "A";

		// Token: 0x040002B0 RID: 688
		public const string F = "F";

		// Token: 0x040002B1 RID: 689
		public const string B = "B";

		// Token: 0x040002B2 RID: 690
		public const string N = "N";

		// Token: 0x040002B3 RID: 691
		public const string C = "C";

		// Token: 0x040002B4 RID: 692
		public const string E = "E";

		// Token: 0x040002B5 RID: 693
		public const string J = "J";

		// Token: 0x040002B6 RID: 694
		public const string Q = "Q";

		// Token: 0x040002B7 RID: 695
		public const string H = "H";

		// Token: 0x040002B8 RID: 696
		public const string W = "W";

		// Token: 0x040002B9 RID: 697
		public const string S = "S";

		// Token: 0x040002BA RID: 698
		public const string U = "U";

		// Token: 0x040002BB RID: 699
		public const string T = "T";

		// Token: 0x040002BC RID: 700
		public const string K = "K";

		// Token: 0x040002BD RID: 701
		public const string M = "M";

		// Token: 0x040002BE RID: 702
		public const string G = "G";

		// Token: 0x040002BF RID: 703
		public const string D = "D";

		// Token: 0x040002C0 RID: 704
		public const string Space = "Space";

		// Token: 0x040002C1 RID: 705
		public const string UpArrow = "UpArrow";

		// Token: 0x040002C2 RID: 706
		public const string LeftArrow = "LeftArrow";

		// Token: 0x040002C3 RID: 707
		public const string DownArrow = "DownArrow";

		// Token: 0x040002C4 RID: 708
		public const string RightArrow = "RightArrow";

		// Token: 0x040002C5 RID: 709
		public const string NumpadArrowForward = "NumpadArrowForward";

		// Token: 0x040002C6 RID: 710
		public const string NumpadArrowBackward = "NumpadArrowBackward";

		// Token: 0x040002C7 RID: 711
		public const string NumpadArrowLeft = "NumpadArrowLeft";

		// Token: 0x040002C8 RID: 712
		public const string NumpadArrowRight = "NumpadArrowRight";

		// Token: 0x040002C9 RID: 713
		public const string SwapToEnemy = "SwapToEnemy";

		// Token: 0x040002CA RID: 714
		public const string ChangeEnemyTeam = "ChangeEnemyTeam";

		// Token: 0x040002CB RID: 715
		public const string Paste = "Paste";

		// Token: 0x040002CC RID: 716
		public const string Cut = "Cut";

		// Token: 0x040002CD RID: 717
		public const string Refresh = "Refresh";

		// Token: 0x040002CE RID: 718
		public const string EnterEditMode = "EnterEditMode";

		// Token: 0x040002CF RID: 719
		public const string FixSkeletons = "FixSkeletons";

		// Token: 0x040002D0 RID: 720
		public const string Reset = "Reset";

		// Token: 0x040002D1 RID: 721
		public const string AnimationTestControllerHotkeyUseWeaponTesting = "AnimationTestControllerHotkeyUseWeaponTesting";

		// Token: 0x040002D2 RID: 722
		public const string BaseBattleMissionControllerHotkeyBecomePlayer = "BaseBattleMissionControllerHotkeyBecomePlayer";

		// Token: 0x040002D3 RID: 723
		public const string BaseBattleMissionControllerHotkeyDrawNavMeshLines = "BaseBattleMissionControllerHotkeyDrawNavMeshLines";

		// Token: 0x040002D4 RID: 724
		public const string ModuleHotkeyOpenDebug = "ModuleHotkeyOpenDebug";

		// Token: 0x040002D5 RID: 725
		public const string FormationTestMissionControllerHotkeyChargeSide = "FormationTestMissionControllerHotkeyChargeSide";

		// Token: 0x040002D6 RID: 726
		public const string FormationTestMissionControllerHotkeyToggleSide = "FormationTestMissionControllerHotkeyToggleSide";

		// Token: 0x040002D7 RID: 727
		public const string FormationTestMissionControllerHotkeyToggleFactionBackward = "FormationTestMissionControllerHotkeyToggleFactionBackward";

		// Token: 0x040002D8 RID: 728
		public const string FormationTestMissionControllerHotkeyToggleFactionForward = "FormationTestMissionControllerHotkeyToggleFactionForward";

		// Token: 0x040002D9 RID: 729
		public const string FormationTestMissionControllerHotkeyToggleTroopForward = "FormationTestMissionControllerHotkeyToggleTroopForward";

		// Token: 0x040002DA RID: 730
		public const string FormationTestMissionControllerHotkeyToggleTroopBackward = "FormationTestMissionControllerHotkeyToggleTroopBackward";

		// Token: 0x040002DB RID: 731
		public const string FormationTestMissionControllerHotkeyIncreaseSpawnCount = "FormationTestMissionControllerHotkeyIncreaseSpawnCount";

		// Token: 0x040002DC RID: 732
		public const string FormationTestMissionControllerHotkeyDecreaseSpawnCount = "FormationTestMissionControllerHotkeyDecreaseSpawnCount";

		// Token: 0x040002DD RID: 733
		public const string FormationTestMissionControllerHotkeySpawnCustom = "FormationTestMissionControllerHotkeySpawnCustom";

		// Token: 0x040002DE RID: 734
		public const string FormationTestMissionControllerHotkeyOrderLooseAndInfantryFormation = "FormationTestMissionControllerHotkeyOrderLooseAndInfantryFormation";

		// Token: 0x040002DF RID: 735
		public const string FormationTestMissionControllerHotkeyOrderScatterAndRangedFormation = "FormationTestMissionControllerHotkeyOrderScatterAndRangedFormation";

		// Token: 0x040002E0 RID: 736
		public const string FormationTestMissionControllerHotkeyOrderSkeinAndCavalryFormation = "FormationTestMissionControllerHotkeyOrderSkeinAndCavalryFormation";

		// Token: 0x040002E1 RID: 737
		public const string FormationTestMissionControllerHotkeyOrderLineAndHorseArcherFormation = "FormationTestMissionControllerHotkeyOrderLineAndHorseArcherFormation";

		// Token: 0x040002E2 RID: 738
		public const string FormationTestMissionControllerHotkeyOrderCircle = "FormationTestMissionControllerHotkeyOrderCircle";

		// Token: 0x040002E3 RID: 739
		public const string FormationTestMissionControllerHotkeyOrderColumn = "FormationTestMissionControllerHotkeyOrderColumn";

		// Token: 0x040002E4 RID: 740
		public const string FormationTestMissionControllerHotkeyOrderShieldWall = "FormationTestMissionControllerHotkeyOrderShieldWall";

		// Token: 0x040002E5 RID: 741
		public const string FormationTestMissionControllerHotkeyOrderSquare = "FormationTestMissionControllerHotkeyOrderSquare";

		// Token: 0x040002E6 RID: 742
		public const string AiTestMissionControllerHotkeySpawnFormation = "AiTestMissionControllerHotkeySpawnFormation";

		// Token: 0x040002E7 RID: 743
		public const string TabbedPanelHotkeyDecreaseSelectedIndex = "TabbedPanelHotkeyDecreaseSelectedIndex";

		// Token: 0x040002E8 RID: 744
		public const string TabbedPanelHotkeyIncreaseSelectedIndex = "TabbedPanelHotkeyIncreaseSelectedIndex";

		// Token: 0x040002E9 RID: 745
		public const string MissionSingleplayerUiHandlerHotkeyUpdateItems = "MissionSingleplayerUiHandlerHotkeyUpdateItems";

		// Token: 0x040002EA RID: 746
		public const string MissionSingleplayerUiHandlerHotkeyJoinEnemyTeam = "MissionSingleplayerUiHandlerHotkeyJoinEnemyTeam";

		// Token: 0x040002EB RID: 747
		public const string SiegeDeploymentViewHotkeyTeleportMainAgent = "SiegeDeploymentViewHotkeyTeleportMainAgent";

		// Token: 0x040002EC RID: 748
		public const string SiegeDeploymentViewHotkeyFinishDeployment = "SiegeDeploymentViewHotkeyFinishDeployment";

		// Token: 0x040002ED RID: 749
		public const string CraftingScreenHotkeyEnableRuler = "CraftingScreenHotkeyEnableRuler";

		// Token: 0x040002EE RID: 750
		public const string CraftingScreenHotkeyEnableRulerPoint1 = "CraftingScreenHotkeyEnableRulerPoint1";

		// Token: 0x040002EF RID: 751
		public const string CraftingScreenHotkeyEnableRulerPoint2 = "CraftingScreenHotkeyEnableRulerPoint2";

		// Token: 0x040002F0 RID: 752
		public const string CraftingScreenHotkeySwitchSelectedPieceMovement = "CraftingScreenHotkeySwitchSelectedPieceMovement";

		// Token: 0x040002F1 RID: 753
		public const string CraftingScreenHotkeySetSelectedVariableIndexZero = "CraftingScreenHotkeySetSelectedVariableIndexZero";

		// Token: 0x040002F2 RID: 754
		public const string CraftingScreenHotkeySetSelectedVariableIndexOne = "CraftingScreenHotkeySetSelectedVariableIndexOne";

		// Token: 0x040002F3 RID: 755
		public const string CraftingScreenHotkeySetSelectedVariableIndexTwo = "CraftingScreenHotkeySetSelectedVariableIndexTwo";

		// Token: 0x040002F4 RID: 756
		public const string CraftingScreenHotkeySelectPieceZero = "CraftingScreenHotkeySelectPieceZero";

		// Token: 0x040002F5 RID: 757
		public const string CraftingScreenHotkeySelectPieceOne = "CraftingScreenHotkeySelectPieceOne";

		// Token: 0x040002F6 RID: 758
		public const string CraftingScreenHotkeySelectPieceTwo = "CraftingScreenHotkeySelectPieceTwo";

		// Token: 0x040002F7 RID: 759
		public const string CraftingScreenHotkeySelectPieceThree = "CraftingScreenHotkeySelectPieceThree";

		// Token: 0x040002F8 RID: 760
		public const string MbFaceGeneratorScreenHotkeyCamDebugAndAdjustEnabled = "MbFaceGeneratorScreenHotkeyCamDebugAndAdjustEnabled";

		// Token: 0x040002F9 RID: 761
		public const string MbFaceGeneratorScreenHotkeyNumpad0 = "MbFaceGeneratorScreenHotkeyNumpad0";

		// Token: 0x040002FA RID: 762
		public const string MbFaceGeneratorScreenHotkeyNumpad1 = "MbFaceGeneratorScreenHotkeyNumpad1";

		// Token: 0x040002FB RID: 763
		public const string MbFaceGeneratorScreenHotkeyNumpad2 = "MbFaceGeneratorScreenHotkeyNumpad2";

		// Token: 0x040002FC RID: 764
		public const string MbFaceGeneratorScreenHotkeyNumpad3 = "MbFaceGeneratorScreenHotkeyNumpad3";

		// Token: 0x040002FD RID: 765
		public const string MbFaceGeneratorScreenHotkeyNumpad4 = "MbFaceGeneratorScreenHotkeyNumpad4";

		// Token: 0x040002FE RID: 766
		public const string MbFaceGeneratorScreenHotkeyNumpad5 = "MbFaceGeneratorScreenHotkeyNumpad5";

		// Token: 0x040002FF RID: 767
		public const string MbFaceGeneratorScreenHotkeyNumpad6 = "MbFaceGeneratorScreenHotkeyNumpad6";

		// Token: 0x04000300 RID: 768
		public const string MbFaceGeneratorScreenHotkeyNumpad7 = "MbFaceGeneratorScreenHotkeyNumpad7";

		// Token: 0x04000301 RID: 769
		public const string MbFaceGeneratorScreenHotkeyNumpad8 = "MbFaceGeneratorScreenHotkeyNumpad8";

		// Token: 0x04000302 RID: 770
		public const string MbFaceGeneratorScreenHotkeyNumpad9 = "MbFaceGeneratorScreenHotkeyNumpad9";

		// Token: 0x04000303 RID: 771
		public const string MbFaceGeneratorScreenHotkeyResetFaceToDefault = "MbFaceGeneratorScreenHotkeyResetFaceToDefault";

		// Token: 0x04000304 RID: 772
		public const string MbFaceGeneratorScreenHotkeySetFaceKeyMax = "MbFaceGeneratorScreenHotkeySetFaceKeyMax";

		// Token: 0x04000305 RID: 773
		public const string MbFaceGeneratorScreenHotkeySetFaceKeyMin = "MbFaceGeneratorScreenHotkeySetFaceKeyMin";

		// Token: 0x04000306 RID: 774
		public const string MbFaceGeneratorScreenHotkeySetCurFaceKeyToMax = "MbFaceGeneratorScreenHotkeySetCurFaceKeyToMax";

		// Token: 0x04000307 RID: 775
		public const string MbFaceGeneratorScreenHotkeySetCurFaceKeyToMin = "MbFaceGeneratorScreenHotkeySetCurFaceKeyToMin";

		// Token: 0x04000308 RID: 776
		public const string SoftwareOcclusionCheckerHotkeySaveOcclusionImage = "SoftwareOcclusionCheckerHotkeySaveOcclusionImage";

		// Token: 0x04000309 RID: 777
		public const string MapScreenHotkeySwitchCampaignTrueSight = "MapScreenHotkeySwitchCampaignTrueSight";

		// Token: 0x0400030A RID: 778
		public const string MapScreenPrintMultiLineText = "MapScreenPrintMultiLineText";

		// Token: 0x0400030B RID: 779
		public const string MapScreenHotkeyShowPos = "MapScreenHotkeyShowPos";

		// Token: 0x0400030C RID: 780
		public const string MapScreenHotkeyOpenEncyclopedia = "MapScreenHotkeyOpenEncyclopedia";

		// Token: 0x0400030D RID: 781
		public const string ReplayCaptureLogicHotkeyRenderWithScreenshot = "ReplayCaptureLogicHotkeyRenderWithScreenshot";

		// Token: 0x0400030E RID: 782
		public const string MissionScreenHotkeyFixCamera = "MissionScreenHotkeyFixCamera";

		// Token: 0x0400030F RID: 783
		public const string MissionScreenHotkeyIncrementArtificialLag = "MissionScreenHotkeyIncrementArtificialLag";

		// Token: 0x04000310 RID: 784
		public const string MissionScreenHotkeyIncrementArtificialLoss = "MissionScreenHotkeyIncrementArtificialLoss";

		// Token: 0x04000311 RID: 785
		public const string MissionScreenHotkeyResetDebugVariables = "MissionScreenHotkeyResetDebugVariables";

		// Token: 0x04000312 RID: 786
		public const string MissionScreenHotkeySwitchCameraSmooth = "MissionScreenHotkeySwitchCameraSmooth";

		// Token: 0x04000313 RID: 787
		public const string MissionScreenHotkeyIncreaseFirstFormationWidth = "MissionScreenHotkeyIncreaseFirstFormationWidth";

		// Token: 0x04000314 RID: 788
		public const string MissionScreenHotkeyDecreaseFirstFormationWidth = "MissionScreenHotkeyDecreaseFirstFormationWidth";

		// Token: 0x04000315 RID: 789
		public const string MissionScreenHotkeyExtendedDebugKey = "MissionScreenHotkeyExtendedDebugKey";

		// Token: 0x04000316 RID: 790
		public const string MissionScreenHotkeyShowDebug = "MissionScreenHotkeyShowDebug";

		// Token: 0x04000317 RID: 791
		public const string MissionScreenHotkeyIncreaseTotalUploadLimit = "MissionScreenHotkeyIncreaseTotalUploadLimit";

		// Token: 0x04000318 RID: 792
		public const string MissionScreenIncreaseTotalUploadLimit = "MissionScreenIncreaseTotalUploadLimit";

		// Token: 0x04000319 RID: 793
		public const string MissionScreenHotkeyDecreaseRulerDistanceFromPivot = "MissionScreenHotkeyDecreaseRulerDistanceFromPivot";

		// Token: 0x0400031A RID: 794
		public const string MissionScreenHotkeyIncreaseRulerDistanceFromPivot = "MissionScreenHotkeyIncreaseRulerDistanceFromPivot";

		// Token: 0x0400031B RID: 795
		public const string DebugAgentTeleportMissionControllerHotkeyTeleportMainAgent = "DebugAgentTeleportMissionControllerHotkeyTeleportMainAgent";

		// Token: 0x0400031C RID: 796
		public const string DebugAgentTeleportMissionControllerHotkeyDisableScriptedMovement = "DebugAgentTeleportMissionControllerHotkeyDisableScriptedMovement";

		// Token: 0x0400031D RID: 797
		public const string MissionDebugHandlerHotkeyKillAI = "MissionDebugHandlerHotkeyKillAI";

		// Token: 0x0400031E RID: 798
		public const string MissionDebugHandlerHotkeyKillAttacker = "MissionDebugHandlerHotkeyKillAttacker";

		// Token: 0x0400031F RID: 799
		public const string MissionDebugHandlerHotkeyKillDefender = "MissionDebugHandlerHotkeyKillDefender";

		// Token: 0x04000320 RID: 800
		public const string MissionDebugHandlerHotkeyKillMainAgent = "MissionDebugHandlerHotkeyKillMainAgent";

		// Token: 0x04000321 RID: 801
		public const string MissionDebugHandlerHotkeyAttackingAiAgent = "MissionDebugHandlerHotkeyAttackingAiAgent";

		// Token: 0x04000322 RID: 802
		public const string MissionDebugHandlerHotkeyDefendingAiAgent = "MissionDebugHandlerHotkeyDefendingAiAgent";

		// Token: 0x04000323 RID: 803
		public const string MissionDebugHandlerHotkeyNormalAiAgent = "MissionDebugHandlerHotkeyNormalAiAgent";

		// Token: 0x04000324 RID: 804
		public const string MissionDebugHandlerHotkeyAiAgentSideZero = "MissionDebugHandlerHotkeyAiAgentSideZero";

		// Token: 0x04000325 RID: 805
		public const string MissionDebugHandlerHotkeyAiAgentSideOne = "MissionDebugHandlerHotkeyAiAgentSideOne";

		// Token: 0x04000326 RID: 806
		public const string MissionDebugHandlerHotkeyAiAgentSideTwo = "MissionDebugHandlerHotkeyAiAgentSideTwo";

		// Token: 0x04000327 RID: 807
		public const string MissionDebugHandlerHotkeyAiAgentSideThree = "MissionDebugHandlerHotkeyAiAgentSideThree";

		// Token: 0x04000328 RID: 808
		public const string MissionDebugHandlerHotkeyColorEnemyTeam = "MissionDebugHandlerHotkeyColorEnemyTeam";

		// Token: 0x04000329 RID: 809
		public const string MissionDebugHandlerHotkeyOpenMissionDebug = "MissionDebugHandlerHotkeyOpenMissionDebug";

		// Token: 0x0400032A RID: 810
		public const string UsableMachineAiBaseHotkeyShowMachineUsers = "UsableMachineAiBaseHotkeyShowMachineUsers";

		// Token: 0x0400032B RID: 811
		public const string UsableMachineAiBaseHotkeyRetreatScriptActive = "UsableMachineAiBaseHotkeyRetreatScriptActive";

		// Token: 0x0400032C RID: 812
		public const string UsableMachineAiBaseHotkeyRetreatScriptPassive = "UsableMachineAiBaseHotkeyRetreatScriptPassive";

		// Token: 0x0400032D RID: 813
		public const string CustomCameraMissionViewHotkeyIncreaseCustomCameraIndex = "CustomCameraMissionViewHotkeyIncreaseCustomCameraIndex";

		// Token: 0x0400032E RID: 814
		public const string DebugSiegeBehaviorHotkeyAimAtBallistas = "DebugSiegeBehaviorHotkeyAimAtBallistas";

		// Token: 0x0400032F RID: 815
		public const string DebugSiegeBehaviorHotkeyAimAtMangonels = "DebugSiegeBehaviorHotkeyAimAtMangonels";

		// Token: 0x04000330 RID: 816
		public const string DebugSiegeBehaviorHotkeyAimAtBattlements = "DebugSiegeBehaviorHotkeyAimAtBattlements";

		// Token: 0x04000331 RID: 817
		public const string DebugSiegeBehaviorHotkeyAimAtNone = "DebugSiegeBehaviorHotkeyAimAtNone";

		// Token: 0x04000332 RID: 818
		public const string DebugSiegeBehaviorHotkeyAimAtNone2 = "DebugSiegeBehaviorHotkeyAimAtNone2";

		// Token: 0x04000333 RID: 819
		public const string DebugSiegeBehaviorHotkeyTargetDebugActive = "DebugSiegeBehaviorHotkeyTargetDebugActive";

		// Token: 0x04000334 RID: 820
		public const string DebugSiegeBehaviorHotkeyTargetDebugDisactive = "DebugSiegeBehaviorHotkeyTargetDebugDisactive";

		// Token: 0x04000335 RID: 821
		public const string DebugSiegeBehaviorHotkeyAimAtRam = "DebugSiegeBehaviorHotkeyAimAtRam";

		// Token: 0x04000336 RID: 822
		public const string DebugSiegeBehaviorHotkeyAimAtSt = "DebugSiegeBehaviorHotkeyAimAtSt";

		// Token: 0x04000337 RID: 823
		public const string DebugSiegeBehaviorHotkeyAimAtBallistas2 = "DebugSiegeBehaviorHotkeyAimAtBallistas2";

		// Token: 0x04000338 RID: 824
		public const string DebugSiegeBehaviorHotkeyAimAtMangonels2 = "DebugSiegeBehaviorHotkeyAimAtMangonels2";

		// Token: 0x04000339 RID: 825
		public const string DebugNetworkEventStatisticsHotkeyClear = "DebugNetworkEventStatisticsHotkeyClear";

		// Token: 0x0400033A RID: 826
		public const string DebugNetworkEventStatisticsHotkeyDumpDataAndClear = "DebugNetworkEventStatisticsHotkeyDumpDataAndClear";

		// Token: 0x0400033B RID: 827
		public const string DebugNetworkEventStatisticsHotkeyDumpData = "DebugNetworkEventStatisticsHotkeyDumpData";

		// Token: 0x0400033C RID: 828
		public const string DebugNetworkEventStatisticsHotkeyClearReplicationData = "DebugNetworkEventStatisticsHotkeyClearReplicationData";

		// Token: 0x0400033D RID: 829
		public const string DebugNetworkEventStatisticsHotkeyDumpReplicationData = "DebugNetworkEventStatisticsHotkeyDumpReplicationData";

		// Token: 0x0400033E RID: 830
		public const string DebugNetworkEventStatisticsHotkeyDumpAndClearReplicationData = "DebugNetworkEventStatisticsHotkeyDumpAndClearReplicationData";

		// Token: 0x0400033F RID: 831
		public const string DebugNetworkEventStatisticsHotkeyToggleActive = "DebugNetworkEventStatisticsHotkeyToggleActive";

		// Token: 0x04000340 RID: 832
		public const string AiSelectDebugAgent1 = "AiSelectDebugAgent1";

		// Token: 0x04000341 RID: 833
		public const string AiSelectDebugAgent2 = "AiSelectDebugAgent2";

		// Token: 0x04000342 RID: 834
		public const string AiClearDebugAgents = "AiClearDebugAgents";

		// Token: 0x04000343 RID: 835
		public const string DebugCustomBattlePredefinedSettings1 = "DebugCustomBattlePredefinedSettings1";

		// Token: 0x04000344 RID: 836
		public const string CraftingScreenResetVariable = "CraftingScreenResetVariable";

		// Token: 0x04000345 RID: 837
		public const string DisableParallelSettlementPositionUpdate = "DisableParallelSettlementPositionUpdate";

		// Token: 0x04000346 RID: 838
		public const string OpenUIEditor = "OpenUIEditor";

		// Token: 0x04000347 RID: 839
		public const string ToggleUI = "ToggleUI";

		// Token: 0x04000348 RID: 840
		public const string LeaveWhileInConversation = "LeaveWhileInConversation";

		// Token: 0x04000349 RID: 841
		public const string ShowHighlightsSummary = "ShowHighlightsSummary";

		// Token: 0x0400034A RID: 842
		public const string ResetMusicParameters = "ResetMusicParameters";

		// Token: 0x0400034B RID: 843
		public const string UIExtendedDebugKey = "UIExtendedDebugKey";

		// Token: 0x0400034C RID: 844
		public const string FaceGeneratorExtendedDebugKey = "FaceGeneratorExtendedDebugKey";

		// Token: 0x0400034D RID: 845
		public const string FormationTestMissionExtendedDebugKey = "FormationTestMissionExtendedDebugKey";

		// Token: 0x0400034E RID: 846
		public const string FormationTestMissionExtendedDebugKey2 = "FormationTestMissionExtendedDebugKey2";
	}
}
