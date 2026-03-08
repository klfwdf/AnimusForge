using System;
using System.Collections.Generic;
using SandBox.View.Missions.Sound.Components;
using SandBox.View.Missions.Tournaments;
using SandBox.ViewModelCollection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Missions.Handlers;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TaleWorlds.MountAndBlade.View.MissionViews.Order;
using TaleWorlds.MountAndBlade.View.MissionViews.Singleplayer;
using TaleWorlds.MountAndBlade.View.MissionViews.Sound;

namespace SandBox.View.Missions
{
	// Token: 0x02000025 RID: 37
	[ViewCreatorModule]
	public class SandBoxMissionViews
	{
		// Token: 0x060000E2 RID: 226 RVA: 0x0000A9F4 File Offset: 0x00008BF4
		[ViewMethod("TownCenter")]
		public static MissionView[] OpenTownCenterMission(Mission mission)
		{
			return new List<MissionView>
			{
				new MissionCampaignView(),
				new MissionConversationCameraView(),
				SandBoxViewCreator.CreateMissionConversationView(mission),
				ViewCreator.CreateMissionSingleplayerEscapeMenu(CampaignOptions.IsIronmanMode),
				ViewCreator.CreateOptionsUIHandler(),
				ViewCreator.CreateMissionMainAgentEquipDropView(mission),
				new MissionSingleplayerViewHandler(),
				ViewCreator.CreateMissionAgentStatusUIHandler(mission),
				ViewCreator.CreateMissionMainAgentEquipmentController(mission),
				ViewCreator.CreateMissionAgentLockVisualizerView(mission),
				SandBoxViewCreator.CreateMissionNameMarkerUIHandler(mission),
				new MissionItemContourControllerView(),
				new MissionAgentContourControllerView(),
				ViewCreator.CreateMissionBoundaryCrossingView(),
				ViewCreator.CreateMissionLeaveView(),
				ViewCreator.CreatePhotoModeView(),
				SandBoxViewCreator.CreateMissionBarterView(),
				ViewCreator.CreateSingleplayerMissionKillNotificationUIHandler(),
				new MissionBoundaryWallView(),
				new MissionCampaignBattleSpectatorView()
			}.ToArray();
		}

		// Token: 0x060000E3 RID: 227 RVA: 0x0000AAF4 File Offset: 0x00008CF4
		[ViewMethod("FacialAnimationTest")]
		public static MissionView[] OpenFacialAnimationTest(Mission mission)
		{
			return new List<MissionView>
			{
				new MissionCampaignView(),
				new MissionConversationCameraView(),
				SandBoxViewCreator.CreateMissionConversationView(mission),
				ViewCreator.CreateMissionSingleplayerEscapeMenu(CampaignOptions.IsIronmanMode),
				ViewCreator.CreateOptionsUIHandler(),
				ViewCreator.CreateMissionMainAgentEquipDropView(mission),
				new MissionSingleplayerViewHandler(),
				ViewCreator.CreateMissionAgentStatusUIHandler(mission),
				ViewCreator.CreateMissionMainAgentEquipmentController(mission),
				ViewCreator.CreateMissionMainAgentCheerBarkControllerView(mission),
				ViewCreator.CreateMissionAgentLockVisualizerView(mission),
				ViewCreator.CreateMissionBoundaryCrossingView(),
				SandBoxViewCreator.CreateMissionBarterView(),
				new MissionBoundaryWallView(),
				ViewCreator.CreatePhotoModeView()
			}.ToArray();
		}

		// Token: 0x060000E4 RID: 228 RVA: 0x0000ABBC File Offset: 0x00008DBC
		[ViewMethod("Indoor")]
		public static MissionView[] OpenTavernMission(Mission mission)
		{
			return new List<MissionView>
			{
				new MissionCampaignView(),
				new MissionConversationCameraView(),
				SandBoxViewCreator.CreateMissionConversationView(mission),
				ViewCreator.CreateMissionSingleplayerEscapeMenu(CampaignOptions.IsIronmanMode),
				ViewCreator.CreateOptionsUIHandler(),
				ViewCreator.CreateMissionMainAgentEquipDropView(mission),
				new MissionSingleplayerViewHandler(),
				ViewCreator.CreateMissionAgentStatusUIHandler(mission),
				ViewCreator.CreateMissionMainAgentEquipmentController(mission),
				ViewCreator.CreateMissionAgentLockVisualizerView(mission),
				new MusicSilencedMissionView(),
				SandBoxViewCreator.CreateMissionBarterView(),
				ViewCreator.CreateMissionLeaveView(),
				SandBoxViewCreator.CreateBoardGameView(),
				SandBoxViewCreator.CreateMissionNameMarkerUIHandler(mission),
				new MissionItemContourControllerView(),
				new MissionAgentContourControllerView(),
				new MissionCampaignBattleSpectatorView(),
				ViewCreator.CreatePhotoModeView(),
				ViewCreator.CreateSingleplayerMissionKillNotificationUIHandler()
			}.ToArray();
		}

		// Token: 0x060000E5 RID: 229 RVA: 0x0000ACBC File Offset: 0x00008EBC
		[ViewMethod("PrisonBreak")]
		public static MissionView[] OpenPrisonBreakMission(Mission mission)
		{
			return new List<MissionView>
			{
				new MissionCampaignView(),
				ViewCreator.CreateMissionSingleplayerEscapeMenu(CampaignOptions.IsIronmanMode),
				ViewCreator.CreateOptionsUIHandler(),
				ViewCreator.CreateMissionMainAgentEquipDropView(mission),
				new MissionSingleplayerViewHandler(),
				ViewCreator.CreateMissionAgentStatusUIHandler(mission),
				ViewCreator.CreateMissionMainAgentEquipmentController(mission),
				new MusicSilencedMissionView(),
				ViewCreator.CreateMissionLeaveView(),
				SandBoxViewCreator.CreateMissionNameMarkerUIHandler(mission),
				new MissionItemContourControllerView(),
				new MissionAgentContourControllerView(),
				SandBoxViewCreator.CreateMissionAgentAlarmStateView(mission),
				ViewCreator.CreateSingleplayerMissionKillNotificationUIHandler()
			}.ToArray();
		}

		// Token: 0x060000E6 RID: 230 RVA: 0x0000AD78 File Offset: 0x00008F78
		[ViewMethod("Village")]
		public static MissionView[] OpenVillageMission(Mission mission)
		{
			return new List<MissionView>
			{
				new MissionCampaignView(),
				new MissionConversationCameraView(),
				SandBoxViewCreator.CreateMissionConversationView(mission),
				ViewCreator.CreateMissionSingleplayerEscapeMenu(CampaignOptions.IsIronmanMode),
				ViewCreator.CreateOptionsUIHandler(),
				ViewCreator.CreateMissionMainAgentEquipDropView(mission),
				new MissionSingleplayerViewHandler(),
				ViewCreator.CreateMissionAgentStatusUIHandler(mission),
				ViewCreator.CreateMissionMainAgentEquipmentController(mission),
				ViewCreator.CreateMissionAgentLockVisualizerView(mission),
				ViewCreator.CreateMissionBoundaryCrossingView(),
				SandBoxViewCreator.CreateMissionBarterView(),
				ViewCreator.CreateMissionLeaveView(),
				new MissionBoundaryWallView(),
				SandBoxViewCreator.CreateMissionNameMarkerUIHandler(mission),
				new MissionItemContourControllerView(),
				new MissionAgentContourControllerView(),
				new MissionCampaignBattleSpectatorView(),
				ViewCreator.CreatePhotoModeView(),
				ViewCreator.CreateSingleplayerMissionKillNotificationUIHandler()
			}.ToArray();
		}

		// Token: 0x060000E7 RID: 231 RVA: 0x0000AE78 File Offset: 0x00009078
		[ViewMethod("Retirement")]
		public static MissionView[] OpenRetirementMission(Mission mission)
		{
			return new List<MissionView>
			{
				new MissionCampaignView(),
				new MissionConversationCameraView(),
				SandBoxViewCreator.CreateMissionConversationView(mission),
				ViewCreator.CreateMissionSingleplayerEscapeMenu(CampaignOptions.IsIronmanMode),
				ViewCreator.CreateOptionsUIHandler(),
				ViewCreator.CreateMissionMainAgentEquipDropView(mission),
				new MissionSingleplayerViewHandler(),
				ViewCreator.CreateMissionAgentStatusUIHandler(mission),
				ViewCreator.CreateMissionMainAgentEquipmentController(mission),
				ViewCreator.CreateMissionAgentLockVisualizerView(mission),
				ViewCreator.CreateMissionBoundaryCrossingView(),
				SandBoxViewCreator.CreateMissionBarterView(),
				ViewCreator.CreateMissionLeaveView(),
				new MissionBoundaryWallView(),
				SandBoxViewCreator.CreateMissionNameMarkerUIHandler(mission),
				new MissionItemContourControllerView(),
				new MissionAgentContourControllerView(),
				ViewCreator.CreatePhotoModeView()
			}.ToArray();
		}

		// Token: 0x060000E8 RID: 232 RVA: 0x0000AF60 File Offset: 0x00009160
		[ViewMethod("ArenaPracticeFight")]
		public static MissionView[] OpenArenaStartMission(Mission mission)
		{
			return new List<MissionView>
			{
				new MissionCampaignView(),
				new MissionConversationCameraView(),
				SandBoxViewCreator.CreateMissionConversationView(mission),
				ViewCreator.CreateMissionSingleplayerEscapeMenu(CampaignOptions.IsIronmanMode),
				ViewCreator.CreateOptionsUIHandler(),
				ViewCreator.CreateMissionMainAgentEquipDropView(mission),
				new MissionSingleplayerViewHandler(),
				ViewCreator.CreateMissionAgentStatusUIHandler(mission),
				ViewCreator.CreateMissionMainAgentEquipmentController(mission),
				ViewCreator.CreateMissionMainAgentCheerBarkControllerView(mission),
				ViewCreator.CreateMissionAgentLockVisualizerView(mission),
				new MissionAudienceHandler(0.4f + MBRandom.RandomFloat * 0.3f),
				SandBoxViewCreator.CreateMissionArenaPracticeFightView(),
				ViewCreator.CreateMissionLeaveView(),
				ViewCreator.CreateSingleplayerMissionKillNotificationUIHandler(),
				new MusicArenaPracticeMissionView(),
				SandBoxViewCreator.CreateMissionBarterView(),
				new MissionItemContourControllerView(),
				new MissionAgentContourControllerView(),
				new MissionCampaignBattleSpectatorView(),
				ViewCreator.CreatePhotoModeView(),
				new ArenaPreloadView()
			}.ToArray();
		}

		// Token: 0x060000E9 RID: 233 RVA: 0x0000B088 File Offset: 0x00009288
		[ViewMethod("ArenaDuelMission")]
		public static MissionView[] OpenArenaDuelMission(Mission mission)
		{
			return new List<MissionView>
			{
				new MissionCampaignView(),
				ViewCreator.CreateMissionSingleplayerEscapeMenu(CampaignOptions.IsIronmanMode),
				ViewCreator.CreateOptionsUIHandler(),
				ViewCreator.CreateMissionMainAgentEquipDropView(mission),
				ViewCreator.CreateMissionLeaveView(),
				ViewCreator.CreateSingleplayerMissionKillNotificationUIHandler(),
				new MissionSingleplayerViewHandler(),
				new MusicSilencedMissionView(),
				ViewCreator.CreateMissionAgentStatusUIHandler(mission),
				ViewCreator.CreateMissionMainAgentEquipmentController(mission),
				ViewCreator.CreateMissionMainAgentCheerBarkControllerView(mission),
				ViewCreator.CreateMissionAgentLockVisualizerView(mission),
				new MissionAudienceHandler(0.4f + MBRandom.RandomFloat * 0.3f),
				new MissionItemContourControllerView(),
				new MissionAgentContourControllerView(),
				new MissionCampaignBattleSpectatorView(),
				ViewCreator.CreatePhotoModeView()
			}.ToArray();
		}

		// Token: 0x060000EA RID: 234 RVA: 0x0000B178 File Offset: 0x00009378
		[ViewMethod("TownMerchant")]
		public static MissionView[] OpenTownMerchantMission(Mission mission)
		{
			return new List<MissionView>
			{
				new MissionCampaignView(),
				new MissionConversationCameraView(),
				SandBoxViewCreator.CreateMissionConversationView(mission),
				ViewCreator.CreateMissionSingleplayerEscapeMenu(CampaignOptions.IsIronmanMode),
				ViewCreator.CreateOptionsUIHandler(),
				ViewCreator.CreateMissionMainAgentEquipDropView(mission),
				new MissionSingleplayerViewHandler(),
				ViewCreator.CreateMissionAgentStatusUIHandler(mission),
				ViewCreator.CreateMissionMainAgentEquipmentController(mission),
				ViewCreator.CreateMissionAgentLockVisualizerView(mission),
				ViewCreator.CreateMissionLeaveView(),
				SandBoxViewCreator.CreateMissionBarterView(),
				SandBoxViewCreator.CreateMissionNameMarkerUIHandler(mission),
				new MissionItemContourControllerView(),
				new MissionAgentContourControllerView(),
				new MissionCampaignBattleSpectatorView(),
				ViewCreator.CreatePhotoModeView(),
				ViewCreator.CreateSingleplayerMissionKillNotificationUIHandler()
			}.ToArray();
		}

		// Token: 0x060000EB RID: 235 RVA: 0x0000B260 File Offset: 0x00009460
		[ViewMethod("Alley")]
		public static MissionView[] OpenAlleyMission(Mission mission)
		{
			return new List<MissionView>
			{
				new MissionCampaignView(),
				new MissionConversationCameraView(),
				SandBoxViewCreator.CreateMissionConversationView(mission),
				ViewCreator.CreateMissionSingleplayerEscapeMenu(CampaignOptions.IsIronmanMode),
				ViewCreator.CreateMissionLeaveView(),
				ViewCreator.CreateOptionsUIHandler(),
				ViewCreator.CreateMissionMainAgentEquipDropView(mission),
				new MissionSingleplayerViewHandler(),
				ViewCreator.CreateMissionAgentStatusUIHandler(mission),
				ViewCreator.CreateMissionMainAgentEquipmentController(mission),
				ViewCreator.CreateMissionMainAgentCheerBarkControllerView(mission),
				ViewCreator.CreateMissionAgentLockVisualizerView(mission),
				ViewCreator.CreateMissionBoundaryCrossingView(),
				SandBoxViewCreator.CreateMissionBarterView(),
				new MissionBoundaryWallView(),
				new MissionItemContourControllerView(),
				new MissionAgentContourControllerView(),
				new MissionCampaignBattleSpectatorView(),
				ViewCreator.CreatePhotoModeView(),
				ViewCreator.CreateSingleplayerMissionKillNotificationUIHandler()
			}.ToArray();
		}

		// Token: 0x060000EC RID: 236 RVA: 0x0000B360 File Offset: 0x00009560
		[ViewMethod("SneakTeam3")]
		public static MissionView[] OpenSneakTeam3Mission(Mission mission)
		{
			return new List<MissionView>
			{
				new MissionConversationCameraView(),
				ViewCreator.CreateMissionSingleplayerEscapeMenu(CampaignOptions.IsIronmanMode),
				ViewCreator.CreateOptionsUIHandler(),
				ViewCreator.CreateMissionMainAgentEquipDropView(mission),
				ViewCreator.CreateMissionAgentStatusUIHandler(mission),
				ViewCreator.CreateMissionMainAgentEquipmentController(mission),
				ViewCreator.CreateMissionMainAgentCheerBarkControllerView(mission),
				ViewCreator.CreateMissionAgentLockVisualizerView(mission),
				new MissionSingleplayerViewHandler(),
				ViewCreator.CreateMissionBoundaryCrossingView(),
				new MissionBoundaryWallView(),
				new MissionCampaignBattleSpectatorView(),
				ViewCreator.CreatePhotoModeView(),
				ViewCreator.CreateSingleplayerMissionKillNotificationUIHandler()
			}.ToArray();
		}

		// Token: 0x060000ED RID: 237 RVA: 0x0000B41B File Offset: 0x0000961B
		[ViewMethod("SimpleMountedPlayer")]
		public static MissionView[] OpenSimpleMountedPlayerMission(Mission mission)
		{
			return new List<MissionView>().ToArray();
		}

		// Token: 0x060000EE RID: 238 RVA: 0x0000B428 File Offset: 0x00009628
		[ViewMethod("Battle")]
		public static MissionView[] OpenBattleMission(Mission mission)
		{
			List<MissionView> list = new List<MissionView>();
			list.Add(new MissionCampaignView());
			list.Add(ViewCreator.CreateMissionSingleplayerEscapeMenu(CampaignOptions.IsIronmanMode));
			list.Add(ViewCreator.CreateMissionAgentLabelUIHandler(mission));
			list.Add(ViewCreator.CreateMissionBattleScoreUIHandler(mission, new SPScoreboardVM(null)));
			list.Add(ViewCreator.CreateOptionsUIHandler());
			list.Add(ViewCreator.CreateMissionMainAgentEquipDropView(mission));
			MissionView missionView = ViewCreator.CreateMissionOrderUIHandler(null);
			list.Add(missionView);
			list.Add(new OrderTroopPlacer(null));
			list.Add(new MissionSingleplayerViewHandler());
			list.Add(ViewCreator.CreateMissionAgentStatusUIHandler(mission));
			list.Add(ViewCreator.CreateMissionMainAgentEquipmentController(mission));
			list.Add(ViewCreator.CreateMissionMainAgentCheerBarkControllerView(mission));
			list.Add(ViewCreator.CreateMissionAgentLockVisualizerView(mission));
			list.Add(new MusicBattleMissionView(false));
			list.Add(new DeploymentMissionView());
			list.Add(new MissionDeploymentBoundaryMarker("swallowtail_banner", 2f));
			list.Add(ViewCreator.CreateMissionBoundaryCrossingView());
			list.Add(new MissionBoundaryWallView());
			list.Add(ViewCreator.CreateMissionFormationMarkerUIHandler(mission));
			list.Add(new MissionFormationTargetSelectionHandler());
			list.Add(ViewCreator.CreateSingleplayerMissionKillNotificationUIHandler());
			list.Add(ViewCreator.CreateMissionSpectatorControlView(mission));
			list.Add(new MissionItemContourControllerView());
			list.Add(new MissionAgentContourControllerView());
			list.Add(new MissionPreloadView());
			list.Add(new MissionCampaignBattleSpectatorView());
			list.Add(ViewCreator.CreatePhotoModeView());
			list.Add(new MissionFaceCacheView());
			ISiegeDeploymentView @object = missionView as ISiegeDeploymentView;
			list.Add(new MissionEntitySelectionUIHandler(new Action<WeakGameEntity>(@object.OnEntitySelection), new Action<WeakGameEntity>(@object.OnEntityHover)));
			list.Add(ViewCreator.CreateMissionOrderOfBattleUIHandler(mission, new SPOrderOfBattleVM()));
			return list.ToArray();
		}

		// Token: 0x060000EF RID: 239 RVA: 0x0000B5D4 File Offset: 0x000097D4
		[ViewMethod("AlleyFight")]
		public static MissionView[] OpenAlleyFightMission(Mission mission)
		{
			return new List<MissionView>
			{
				new MissionCampaignView(),
				ViewCreator.CreateMissionSingleplayerEscapeMenu(CampaignOptions.IsIronmanMode),
				ViewCreator.CreateOptionsUIHandler(),
				ViewCreator.CreateMissionMainAgentEquipDropView(mission),
				ViewCreator.CreateMissionBattleScoreUIHandler(mission, new SPScoreboardVM(null)),
				ViewCreator.CreateMissionAgentLabelUIHandler(mission),
				ViewCreator.CreateMissionOrderUIHandler(null),
				new OrderTroopPlacer(null),
				new MissionSingleplayerViewHandler(),
				ViewCreator.CreateMissionAgentStatusUIHandler(mission),
				ViewCreator.CreateMissionMainAgentEquipmentController(mission),
				ViewCreator.CreateMissionMainAgentCheerBarkControllerView(mission),
				ViewCreator.CreateMissionAgentLockVisualizerView(mission),
				ViewCreator.CreateMissionBoundaryCrossingView(),
				new MissionBoundaryWallView(),
				ViewCreator.CreateMissionFormationMarkerUIHandler(mission),
				new MissionFormationTargetSelectionHandler(),
				ViewCreator.CreateSingleplayerMissionKillNotificationUIHandler(),
				ViewCreator.CreateMissionSpectatorControlView(mission),
				new MissionItemContourControllerView(),
				new MissionAgentContourControllerView(),
				new MissionCampaignBattleSpectatorView(),
				ViewCreator.CreatePhotoModeView()
			}.ToArray();
		}

		// Token: 0x060000F0 RID: 240 RVA: 0x0000B700 File Offset: 0x00009900
		[ViewMethod("HideoutBattle")]
		public static MissionView[] OpenHideoutBattleMission(Mission mission)
		{
			return new List<MissionView>
			{
				new MissionCampaignView(),
				new MissionConversationCameraView(),
				new MissionHideoutCinematicView(),
				SandBoxViewCreator.CreateMissionConversationView(mission),
				ViewCreator.CreateMissionSingleplayerEscapeMenu(CampaignOptions.IsIronmanMode),
				ViewCreator.CreateOptionsUIHandler(),
				ViewCreator.CreateMissionMainAgentEquipDropView(mission),
				ViewCreator.CreateMissionBattleScoreUIHandler(mission, new SPScoreboardVM(null)),
				ViewCreator.CreateMissionAgentLabelUIHandler(mission),
				ViewCreator.CreateMissionOrderUIHandler(null),
				new OrderTroopPlacer(null),
				new MissionSingleplayerViewHandler(),
				new MusicSilencedMissionView(),
				ViewCreator.CreateMissionAgentStatusUIHandler(mission),
				ViewCreator.CreateMissionMainAgentEquipmentController(mission),
				ViewCreator.CreateMissionMainAgentCheerBarkControllerView(mission),
				ViewCreator.CreateMissionAgentLockVisualizerView(mission),
				ViewCreator.CreateMissionBoundaryCrossingView(),
				new MissionBoundaryWallView(),
				ViewCreator.CreateMissionFormationMarkerUIHandler(mission),
				new MissionFormationTargetSelectionHandler(),
				ViewCreator.CreateSingleplayerMissionKillNotificationUIHandler(),
				ViewCreator.CreateMissionSpectatorControlView(mission),
				new MissionItemContourControllerView(),
				new MissionAgentContourControllerView(),
				new MissionCampaignBattleSpectatorView(),
				new MissionPreloadView(),
				ViewCreator.CreatePhotoModeView()
			}.ToArray();
		}

		// Token: 0x060000F1 RID: 241 RVA: 0x0000B864 File Offset: 0x00009A64
		[ViewMethod("HideoutAmbushMission")]
		public static MissionView[] OpenHideoutAmbushMission(Mission mission)
		{
			return new List<MissionView>
			{
				new MissionCampaignView(),
				ViewCreator.CreateMissionSingleplayerEscapeMenu(CampaignOptions.IsIronmanMode),
				ViewCreator.CreateOptionsUIHandler(),
				ViewCreator.CreateMissionMainAgentEquipDropView(mission),
				ViewCreator.CreateMissionAgentLabelUIHandler(mission),
				new MissionSingleplayerViewHandler(),
				new MusicStealthMissionView(),
				ViewCreator.CreateMissionAgentStatusUIHandler(mission),
				ViewCreator.CreateMissionMainAgentEquipmentController(mission),
				ViewCreator.CreateMissionAgentLockVisualizerView(mission),
				ViewCreator.CreateMissionBoundaryCrossingView(),
				new MissionBoundaryWallView(),
				ViewCreator.CreateSingleplayerMissionKillNotificationUIHandler(),
				SandBoxViewCreator.CreateMissionNameMarkerUIHandler(mission),
				SandBoxViewCreator.CreateMissionAgentAlarmStateView(mission),
				ViewCreator.CreateMissionSpectatorControlView(mission),
				new MissionItemContourControllerView(),
				new MissionAgentContourControllerView(),
				new MissionCampaignBattleSpectatorView(),
				ViewCreator.CreatePhotoModeView(),
				ViewCreator.CreateMissionLeaveView(),
				new MissionConversationCameraView(),
				SandBoxViewCreator.CreateMissionConversationView(mission),
				ViewCreator.CreateMissionBattleScoreUIHandler(mission, new SPScoreboardVM(null)),
				ViewCreator.CreateMissionOrderUIHandler(null),
				SandBoxViewCreator.CreateMissionStealthFailCounter(null),
				new OrderTroopPlacer(null),
				ViewCreator.CreateMissionMainAgentCheerBarkControllerView(mission),
				ViewCreator.CreateMissionFormationMarkerUIHandler(mission),
				new MissionPreloadView(),
				ViewCreatorManager.CreateMissionView<MissionHideoutAmbushCinematicView>(mission != null, mission, Array.Empty<object>()),
				ViewCreatorManager.CreateMissionView<MissionHideoutAmbushBossFightCinematicView>(mission != null, mission, Array.Empty<object>())
			}.ToArray();
		}

		// Token: 0x060000F2 RID: 242 RVA: 0x0000BA0C File Offset: 0x00009C0C
		[ViewMethod("EnteringSettlementBattle")]
		public static MissionView[] OpenBattleMissionWhileEnteringSettlement(Mission mission)
		{
			return new List<MissionView>
			{
				new MissionCampaignView(),
				new MissionConversationCameraView(),
				ViewCreator.CreateMissionSingleplayerEscapeMenu(CampaignOptions.IsIronmanMode),
				SandBoxViewCreator.CreateMissionConversationView(mission),
				ViewCreator.CreateOptionsUIHandler(),
				ViewCreator.CreateMissionMainAgentEquipDropView(mission),
				ViewCreator.CreateMissionBattleScoreUIHandler(mission, new SPScoreboardVM(null)),
				ViewCreator.CreateMissionAgentLabelUIHandler(mission),
				ViewCreator.CreateMissionOrderUIHandler(null),
				new OrderTroopPlacer(null),
				new MissionSingleplayerViewHandler(),
				ViewCreator.CreateMissionAgentStatusUIHandler(mission),
				ViewCreator.CreateMissionMainAgentEquipmentController(mission),
				ViewCreator.CreateMissionMainAgentCheerBarkControllerView(mission),
				ViewCreator.CreateMissionAgentLockVisualizerView(mission),
				ViewCreator.CreateMissionBoundaryCrossingView(),
				new MissionBoundaryWallView(),
				ViewCreator.CreateMissionFormationMarkerUIHandler(mission),
				new MissionFormationTargetSelectionHandler(),
				ViewCreator.CreateSingleplayerMissionKillNotificationUIHandler(),
				ViewCreator.CreateMissionSpectatorControlView(mission),
				new MissionItemContourControllerView(),
				new MissionAgentContourControllerView(),
				new MissionCampaignBattleSpectatorView(),
				ViewCreator.CreatePhotoModeView()
			}.ToArray();
		}

		// Token: 0x060000F3 RID: 243 RVA: 0x0000BB50 File Offset: 0x00009D50
		[ViewMethod("CombatWithDialogue")]
		public static MissionView[] OpenCombatMissionWithDialogue(Mission mission)
		{
			return new List<MissionView>
			{
				new MissionCampaignView(),
				new MissionConversationCameraView(),
				ViewCreator.CreateMissionSingleplayerEscapeMenu(CampaignOptions.IsIronmanMode),
				SandBoxViewCreator.CreateMissionConversationView(mission),
				ViewCreator.CreateOptionsUIHandler(),
				ViewCreator.CreateMissionMainAgentEquipDropView(mission),
				ViewCreator.CreateMissionBattleScoreUIHandler(mission, new SPScoreboardVM(null)),
				ViewCreator.CreateMissionAgentLabelUIHandler(mission),
				ViewCreator.CreateMissionOrderUIHandler(null),
				new OrderTroopPlacer(null),
				new MissionSingleplayerViewHandler(),
				ViewCreator.CreateMissionAgentStatusUIHandler(mission),
				ViewCreator.CreateMissionMainAgentEquipmentController(mission),
				ViewCreator.CreateMissionMainAgentCheerBarkControllerView(mission),
				ViewCreator.CreateMissionAgentLockVisualizerView(mission),
				ViewCreator.CreateMissionBoundaryCrossingView(),
				new MissionBoundaryWallView(),
				ViewCreator.CreateMissionFormationMarkerUIHandler(mission),
				new MissionFormationTargetSelectionHandler(),
				ViewCreator.CreateSingleplayerMissionKillNotificationUIHandler(),
				ViewCreator.CreateMissionSpectatorControlView(mission),
				new MissionItemContourControllerView(),
				new MissionAgentContourControllerView(),
				new MissionCampaignBattleSpectatorView(),
				ViewCreator.CreatePhotoModeView()
			}.ToArray();
		}

		// Token: 0x060000F4 RID: 244 RVA: 0x0000BC91 File Offset: 0x00009E91
		[ViewMethod("SiegeEngine")]
		public static MissionView[] OpenTestSiegeEngineMission(Mission mission)
		{
			return new List<MissionView>
			{
				new MissionConversationCameraView(),
				ViewCreator.CreateMissionOrderUIHandler(null),
				new OrderTroopPlacer(null)
			}.ToArray();
		}

		// Token: 0x060000F5 RID: 245 RVA: 0x0000BCC0 File Offset: 0x00009EC0
		[ViewMethod("CustomCameraMission")]
		public static MissionView[] OpenCustomCameraMission(Mission mission)
		{
			return new List<MissionView>
			{
				new MissionConversationCameraView(),
				new MissionCustomCameraView()
			}.ToArray();
		}

		// Token: 0x060000F6 RID: 246 RVA: 0x0000BCE2 File Offset: 0x00009EE2
		[ViewMethod("AmbushBattle")]
		public static MissionView[] OpenAmbushBattleMission(Mission mission)
		{
			throw new NotImplementedException("Ambush battle is not implemented.");
		}

		// Token: 0x060000F7 RID: 247 RVA: 0x0000BCF0 File Offset: 0x00009EF0
		[ViewMethod("Camp")]
		public static MissionView[] OpenCampMission(Mission mission)
		{
			return new List<MissionView>
			{
				new MissionCampaignView(),
				new MissionConversationCameraView(),
				ViewCreator.CreateMissionSingleplayerEscapeMenu(CampaignOptions.IsIronmanMode),
				ViewCreator.CreateOptionsUIHandler(),
				ViewCreator.CreateMissionMainAgentEquipDropView(mission),
				new MissionSingleplayerViewHandler(),
				ViewCreator.CreateMissionAgentStatusUIHandler(mission),
				ViewCreator.CreateMissionMainAgentEquipmentController(mission),
				ViewCreator.CreateMissionMainAgentCheerBarkControllerView(mission),
				ViewCreator.CreateMissionAgentLockVisualizerView(mission),
				ViewCreator.CreateMissionBoundaryCrossingView(),
				new MissionBoundaryWallView(),
				new MissionCampaignBattleSpectatorView(),
				ViewCreator.CreatePhotoModeView(),
				ViewCreator.CreateSingleplayerMissionKillNotificationUIHandler()
			}.ToArray();
		}

		// Token: 0x060000F8 RID: 248 RVA: 0x0000BDB8 File Offset: 0x00009FB8
		[ViewMethod("SiegeMissionWithDeployment")]
		public static MissionView[] OpenSiegeMissionWithDeployment(Mission mission)
		{
			List<MissionView> list = new List<MissionView>();
			mission.GetMissionBehavior<SiegeDeploymentHandler>();
			list.Add(new MissionCampaignView());
			list.Add(new MissionConversationCameraView());
			list.Add(ViewCreator.CreateMissionSingleplayerEscapeMenu(CampaignOptions.IsIronmanMode));
			list.Add(ViewCreator.CreateOptionsUIHandler());
			list.Add(ViewCreator.CreateMissionMainAgentEquipDropView(mission));
			list.Add(ViewCreator.CreateMissionAgentLabelUIHandler(mission));
			list.Add(ViewCreator.CreateMissionBattleScoreUIHandler(mission, new SPScoreboardVM(null)));
			MissionView missionView = ViewCreator.CreateMissionOrderUIHandler(null);
			list.Add(ViewCreator.CreateMissionAgentStatusUIHandler(mission));
			list.Add(ViewCreator.CreateMissionMainAgentEquipmentController(mission));
			list.Add(ViewCreator.CreateMissionMainAgentCheerBarkControllerView(mission));
			list.Add(ViewCreator.CreateMissionAgentLockVisualizerView(mission));
			list.Add(missionView);
			list.Add(new OrderTroopPlacer(null));
			list.Add(new MissionSingleplayerViewHandler());
			list.Add(new MusicBattleMissionView(true));
			list.Add(new DeploymentMissionView());
			list.Add(new MissionDeploymentBoundaryMarker("swallowtail_banner", 2f));
			list.Add(ViewCreator.CreateMissionBoundaryCrossingView());
			list.Add(ViewCreator.CreateSingleplayerMissionKillNotificationUIHandler());
			list.Add(ViewCreator.CreatePhotoModeView());
			list.Add(ViewCreator.CreateMissionFormationMarkerUIHandler(mission));
			list.Add(new MissionFormationTargetSelectionHandler());
			ISiegeDeploymentView @object = missionView as ISiegeDeploymentView;
			list.Add(new MissionEntitySelectionUIHandler(new Action<WeakGameEntity>(@object.OnEntitySelection), new Action<WeakGameEntity>(@object.OnEntityHover)));
			list.Add(ViewCreator.CreateMissionSpectatorControlView(mission));
			list.Add(new MissionItemContourControllerView());
			list.Add(new MissionAgentContourControllerView());
			list.Add(new MissionPreloadView());
			list.Add(new MissionCampaignBattleSpectatorView());
			list.Add(ViewCreator.CreateMissionOrderOfBattleUIHandler(mission, new SPOrderOfBattleVM()));
			list.Add(ViewCreator.CreateMissionSiegeEngineMarkerView(mission));
			list.Add(new MissionFaceCacheView());
			return list.ToArray();
		}

		// Token: 0x060000F9 RID: 249 RVA: 0x0000BF78 File Offset: 0x0000A178
		[ViewMethod("SiegeMissionNoDeployment")]
		public static MissionView[] OpenSiegeMissionNoDeployment(Mission mission)
		{
			return new List<MissionView>
			{
				new MissionCampaignView(),
				new MissionConversationCameraView(),
				ViewCreator.CreateMissionSingleplayerEscapeMenu(CampaignOptions.IsIronmanMode),
				ViewCreator.CreateOptionsUIHandler(),
				ViewCreator.CreateMissionMainAgentEquipDropView(mission),
				ViewCreator.CreateMissionAgentLabelUIHandler(mission),
				ViewCreator.CreateMissionBattleScoreUIHandler(mission, new SPScoreboardVM(null)),
				ViewCreator.CreateMissionOrderUIHandler(null),
				new OrderTroopPlacer(null),
				new MissionSingleplayerViewHandler(),
				ViewCreator.CreateMissionAgentStatusUIHandler(mission),
				ViewCreator.CreateMissionMainAgentEquipmentController(mission),
				ViewCreator.CreateMissionMainAgentCheerBarkControllerView(mission),
				ViewCreator.CreateMissionAgentLockVisualizerView(mission),
				ViewCreator.CreateSingleplayerMissionKillNotificationUIHandler(),
				ViewCreator.CreatePhotoModeView(),
				ViewCreator.CreateMissionFormationMarkerUIHandler(mission),
				new MissionFormationTargetSelectionHandler(),
				new MusicBattleMissionView(true),
				ViewCreator.CreateMissionBoundaryCrossingView(),
				new MissionBoundaryWallView(),
				ViewCreator.CreateMissionSpectatorControlView(mission),
				new MissionItemContourControllerView(),
				new MissionAgentContourControllerView(),
				new MissionPreloadView(),
				new MissionCampaignBattleSpectatorView(),
				ViewCreator.CreateMissionSiegeEngineMarkerView(mission)
			}.ToArray();
		}

		// Token: 0x060000FA RID: 250 RVA: 0x0000C0D0 File Offset: 0x0000A2D0
		[ViewMethod("SiegeLordsHallFightMission")]
		public static MissionView[] OpenSiegeLordsHallFightMission(Mission mission)
		{
			return new List<MissionView>
			{
				new MissionCampaignView(),
				new MissionConversationCameraView(),
				ViewCreator.CreateMissionSingleplayerEscapeMenu(CampaignOptions.IsIronmanMode),
				ViewCreator.CreateOptionsUIHandler(),
				ViewCreator.CreateMissionMainAgentEquipDropView(mission),
				ViewCreator.CreateMissionBattleScoreUIHandler(mission, new SPScoreboardVM(null)),
				ViewCreator.CreateMissionAgentLabelUIHandler(mission),
				ViewCreator.CreateMissionOrderUIHandler(null),
				new OrderTroopPlacer(null),
				new MissionSingleplayerViewHandler(),
				ViewCreator.CreateMissionAgentStatusUIHandler(mission),
				ViewCreator.CreateMissionMainAgentEquipmentController(mission),
				ViewCreator.CreateMissionMainAgentCheerBarkControllerView(mission),
				ViewCreator.CreateMissionBoundaryCrossingView(),
				new MissionBoundaryWallView(),
				ViewCreator.CreateMissionFormationMarkerUIHandler(mission),
				new MissionFormationTargetSelectionHandler(),
				ViewCreator.CreateSingleplayerMissionKillNotificationUIHandler(),
				ViewCreator.CreateMissionSpectatorControlView(mission),
				new MissionItemContourControllerView(),
				new MissionAgentContourControllerView(),
				new MissionPreloadView(),
				ViewCreator.CreatePhotoModeView()
			}.ToArray();
		}

		// Token: 0x060000FB RID: 251 RVA: 0x0000C1FC File Offset: 0x0000A3FC
		[ViewMethod("Siege")]
		public static MissionView[] OpenSiegeMission(Mission mission)
		{
			List<MissionView> list = new List<MissionView>();
			mission.GetMissionBehavior<SiegeDeploymentHandler>();
			list.Add(new MissionCampaignView());
			list.Add(new MissionConversationCameraView());
			list.Add(ViewCreator.CreateMissionSingleplayerEscapeMenu(CampaignOptions.IsIronmanMode));
			list.Add(ViewCreator.CreateOptionsUIHandler());
			list.Add(ViewCreator.CreateMissionMainAgentEquipDropView(mission));
			MissionView missionView = ViewCreator.CreateMissionOrderUIHandler(null);
			list.Add(missionView);
			list.Add(new OrderTroopPlacer(null));
			list.Add(new MissionSingleplayerViewHandler());
			list.Add(new DeploymentMissionView());
			list.Add(new MissionDeploymentBoundaryMarker("swallowtail_banner", 2f));
			list.Add(ViewCreator.CreateMissionBoundaryCrossingView());
			list.Add(ViewCreator.CreateSingleplayerMissionKillNotificationUIHandler());
			list.Add(ViewCreator.CreateMissionAgentStatusUIHandler(mission));
			list.Add(ViewCreator.CreateMissionMainAgentEquipmentController(mission));
			list.Add(ViewCreator.CreateMissionMainAgentCheerBarkControllerView(mission));
			list.Add(ViewCreator.CreateMissionAgentLockVisualizerView(mission));
			list.Add(ViewCreator.CreateMissionSpectatorControlView(mission));
			list.Add(ViewCreator.CreatePhotoModeView());
			ISiegeDeploymentView @object = missionView as ISiegeDeploymentView;
			list.Add(new MissionEntitySelectionUIHandler(new Action<WeakGameEntity>(@object.OnEntitySelection), new Action<WeakGameEntity>(@object.OnEntityHover)));
			list.Add(ViewCreator.CreateMissionFormationMarkerUIHandler(mission));
			list.Add(new MissionFormationTargetSelectionHandler());
			list.Add(new MissionItemContourControllerView());
			list.Add(new MissionAgentContourControllerView());
			list.Add(new MissionCampaignBattleSpectatorView());
			list.Add(ViewCreator.CreateMissionSiegeEngineMarkerView(mission));
			list.Add(new MissionFaceCacheView());
			return list.ToArray();
		}

		// Token: 0x060000FC RID: 252 RVA: 0x0000C374 File Offset: 0x0000A574
		[ViewMethod("SiegeMissionForTutorial")]
		public static MissionView[] OpenSiegeMissionForTutorial(Mission mission)
		{
			Debug.FailedAssert("Do not use SiegeForTutorial! Use campaign!", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox.View\\Missions\\SandBoxMissionViews.cs", "OpenSiegeMissionForTutorial", 879);
			List<MissionView> list = new List<MissionView>();
			list.Add(new MissionConversationCameraView());
			list.Add(ViewCreator.CreateMissionSingleplayerEscapeMenu(CampaignOptions.IsIronmanMode));
			list.Add(ViewCreator.CreateOptionsUIHandler());
			list.Add(ViewCreator.CreateMissionMainAgentEquipDropView(mission));
			MissionView missionView = ViewCreator.CreateMissionOrderUIHandler(null);
			list.Add(missionView);
			list.Add(new OrderTroopPlacer(null));
			list.Add(new MissionSingleplayerViewHandler());
			list.Add(ViewCreator.CreateMissionAgentStatusUIHandler(mission));
			list.Add(ViewCreator.CreateMissionMainAgentEquipmentController(mission));
			list.Add(ViewCreator.CreateMissionMainAgentCheerBarkControllerView(mission));
			list.Add(ViewCreator.CreateMissionAgentLockVisualizerView(mission));
			list.Add(ViewCreator.CreateMissionSpectatorControlView(mission));
			list.Add(ViewCreator.CreatePhotoModeView());
			list.Add(ViewCreator.CreateMissionSiegeEngineMarkerView(mission));
			ISiegeDeploymentView @object = missionView as ISiegeDeploymentView;
			list.Add(new MissionEntitySelectionUIHandler(new Action<WeakGameEntity>(@object.OnEntitySelection), new Action<WeakGameEntity>(@object.OnEntityHover)));
			list.Add(new MissionDeploymentBoundaryMarker("swallowtail_banner", 2f));
			list.Add(new MissionCampaignBattleSpectatorView());
			return list.ToArray();
		}

		// Token: 0x060000FD RID: 253 RVA: 0x0000C49A File Offset: 0x0000A69A
		[ViewMethod("FormationTest")]
		public static MissionView[] OpenFormationTestMission(Mission mission)
		{
			return new List<MissionView>
			{
				new MissionConversationCameraView(),
				ViewCreator.CreateMissionOrderUIHandler(null),
				new OrderTroopPlacer(null)
			}.ToArray();
		}

		// Token: 0x060000FE RID: 254 RVA: 0x0000C4CC File Offset: 0x0000A6CC
		[ViewMethod("VillageBattle")]
		public static MissionView[] OpenVillageBattleMission(Mission mission)
		{
			return new List<MissionView>
			{
				new MissionCampaignView(),
				new MissionConversationCameraView(),
				ViewCreator.CreateMissionSingleplayerEscapeMenu(CampaignOptions.IsIronmanMode),
				ViewCreator.CreateOptionsUIHandler(),
				ViewCreator.CreateMissionMainAgentEquipDropView(mission),
				ViewCreator.CreateMissionBattleScoreUIHandler(mission, new SPScoreboardVM(null)),
				ViewCreator.CreateMissionOrderUIHandler(null),
				new OrderTroopPlacer(null),
				ViewCreator.CreateMissionAgentStatusUIHandler(mission),
				ViewCreator.CreateMissionMainAgentEquipmentController(mission),
				ViewCreator.CreateMissionMainAgentCheerBarkControllerView(mission),
				ViewCreator.CreateMissionAgentLockVisualizerView(mission),
				new MissionSingleplayerViewHandler(),
				ViewCreator.CreateMissionBoundaryCrossingView(),
				new MissionBoundaryWallView(),
				ViewCreator.CreateMissionFormationMarkerUIHandler(mission),
				new MissionFormationTargetSelectionHandler(),
				ViewCreator.CreateMissionSpectatorControlView(mission),
				new MissionItemContourControllerView(),
				new MissionAgentContourControllerView(),
				new MissionCampaignBattleSpectatorView(),
				ViewCreator.CreatePhotoModeView(),
				ViewCreator.CreateSingleplayerMissionKillNotificationUIHandler()
			}.ToArray();
		}

		// Token: 0x060000FF RID: 255 RVA: 0x0000C5F8 File Offset: 0x0000A7F8
		[ViewMethod("SettlementTest")]
		public static MissionView[] OpenSettlementTestMission(Mission mission)
		{
			return new List<MissionView>
			{
				new MissionConversationCameraView(),
				ViewCreator.CreateMissionSingleplayerEscapeMenu(CampaignOptions.IsIronmanMode),
				ViewCreator.CreateOptionsUIHandler(),
				ViewCreator.CreateMissionMainAgentEquipDropView(mission),
				new MissionSingleplayerViewHandler(),
				ViewCreator.CreateMissionAgentStatusUIHandler(mission),
				ViewCreator.CreateMissionMainAgentEquipmentController(mission),
				ViewCreator.CreateMissionMainAgentCheerBarkControllerView(mission),
				ViewCreator.CreateMissionAgentLockVisualizerView(mission),
				ViewCreator.CreateMissionBoundaryCrossingView(),
				new MissionBoundaryWallView(),
				ViewCreator.CreateMissionSpectatorControlView(mission),
				new MissionCampaignBattleSpectatorView(),
				ViewCreator.CreatePhotoModeView()
			}.ToArray();
		}

		// Token: 0x06000100 RID: 256 RVA: 0x0000C6B4 File Offset: 0x0000A8B4
		[ViewMethod("EquipmentTest")]
		public static MissionView[] OpenEquipmentTestMission(Mission mission)
		{
			return new List<MissionView>
			{
				new MissionConversationCameraView(),
				ViewCreator.CreateMissionSingleplayerEscapeMenu(CampaignOptions.IsIronmanMode),
				ViewCreator.CreateOptionsUIHandler(),
				ViewCreator.CreateMissionMainAgentEquipDropView(mission),
				new MissionSingleplayerViewHandler(),
				ViewCreator.CreateMissionBoundaryCrossingView(),
				new MissionBoundaryWallView(),
				new MissionCampaignBattleSpectatorView(),
				ViewCreator.CreatePhotoModeView()
			}.ToArray();
		}

		// Token: 0x06000101 RID: 257 RVA: 0x0000C734 File Offset: 0x0000A934
		[ViewMethod("FacialAnimTest")]
		public static MissionView[] OpenFacialAnimTestMission(Mission mission)
		{
			return new List<MissionView>
			{
				new MissionConversationCameraView(),
				ViewCreator.CreateMissionSingleplayerEscapeMenu(CampaignOptions.IsIronmanMode),
				ViewCreator.CreateOptionsUIHandler(),
				ViewCreator.CreateMissionMainAgentEquipDropView(mission),
				new MissionSingleplayerViewHandler(),
				ViewCreator.CreateMissionAgentStatusUIHandler(mission),
				ViewCreator.CreateMissionMainAgentEquipmentController(mission),
				ViewCreator.CreateMissionMainAgentCheerBarkControllerView(mission),
				ViewCreator.CreateMissionAgentLockVisualizerView(mission),
				ViewCreator.CreateMissionBoundaryCrossingView(),
				SandBoxViewCreator.CreateMissionConversationView(mission),
				SandBoxViewCreator.CreateMissionBarterView(),
				new MissionBoundaryWallView(),
				new MissionCampaignBattleSpectatorView(),
				ViewCreator.CreatePhotoModeView()
			}.ToArray();
		}

		// Token: 0x06000102 RID: 258 RVA: 0x0000C7FC File Offset: 0x0000A9FC
		[ViewMethod("EquipItemTool")]
		public static MissionView[] OpenEquipItemToolMission(Mission mission)
		{
			return new List<MissionView>
			{
				new MissionConversationCameraView(),
				ViewCreator.CreateOptionsUIHandler(),
				ViewCreator.CreateMissionMainAgentEquipDropView(mission),
				new MissionEquipItemToolView(),
				ViewCreator.CreateMissionLeaveView()
			}.ToArray();
		}

		// Token: 0x06000103 RID: 259 RVA: 0x0000C84C File Offset: 0x0000AA4C
		[ViewMethod("Conversation")]
		public static MissionView[] OpenConversationMission(Mission mission)
		{
			return new List<MissionView>
			{
				new MissionCampaignView(),
				new MissionConversationCameraView(),
				new MissionSingleplayerViewHandler(),
				SandBoxViewCreator.CreateMissionConversationView(mission),
				ViewCreator.CreateMissionSingleplayerEscapeMenu(CampaignOptions.IsIronmanMode),
				ViewCreator.CreateOptionsUIHandler(),
				ViewCreator.CreateMissionMainAgentEquipDropView(mission),
				SandBoxViewCreator.CreateMissionBarterView(),
				ViewCreator.CreateMissionAgentLockVisualizerView(mission),
				new MissionCampaignBattleSpectatorView(),
				ViewCreator.CreatePhotoModeView(),
				new MissionConversationPrepareView()
			}.ToArray();
		}

		// Token: 0x06000104 RID: 260 RVA: 0x0000C8F0 File Offset: 0x0000AAF0
		[ViewMethod("ShadowingATargetMission")]
		public static MissionView[] OpenShadowingATargetMission(Mission mission)
		{
			return new List<MissionView>
			{
				new MissionCampaignView(),
				new MissionConversationCameraView(),
				SandBoxViewCreator.CreateMissionConversationView(mission),
				ViewCreator.CreateMissionSingleplayerEscapeMenu(CampaignOptions.IsIronmanMode),
				ViewCreator.CreateOptionsUIHandler(),
				ViewCreator.CreateMissionMainAgentEquipDropView(mission),
				new MissionSingleplayerViewHandler(),
				new MusicStealthMissionView(),
				ViewCreator.CreateMissionAgentStatusUIHandler(mission),
				ViewCreator.CreateMissionMainAgentEquipmentController(mission),
				ViewCreator.CreateMissionAgentLockVisualizerView(mission),
				SandBoxViewCreator.CreateMissionNameMarkerUIHandler(mission),
				new MissionItemContourControllerView(),
				new MissionAgentContourControllerView(),
				ViewCreator.CreateMissionBoundaryCrossingView(),
				ViewCreator.CreateMissionLeaveView(),
				ViewCreator.CreatePhotoModeView(),
				ViewCreator.CreateSingleplayerMissionKillNotificationUIHandler(),
				new MissionBoundaryWallView(),
				new MissionCampaignBattleSpectatorView(),
				SandBoxViewCreator.CreateMissionAgentAlarmStateView(mission),
				SandBoxViewCreator.CreateMissionMainAgentDetectionView(null),
				ViewCreatorManager.CreateMissionView<EavesdroppingMissionCameraView>(mission != null, mission, Array.Empty<object>())
			}.ToArray();
		}

		// Token: 0x06000105 RID: 261 RVA: 0x0000CA1C File Offset: 0x0000AC1C
		[ViewMethod("DisguiseMission")]
		public static MissionView[] OpenDisguiseMission(Mission mission)
		{
			return new List<MissionView>
			{
				new MissionCampaignView(),
				new MissionConversationCameraView(),
				SandBoxViewCreator.CreateMissionConversationView(mission),
				ViewCreator.CreateMissionSingleplayerEscapeMenu(CampaignOptions.IsIronmanMode),
				ViewCreator.CreateOptionsUIHandler(),
				ViewCreator.CreateMissionMainAgentEquipDropView(mission),
				new MissionSingleplayerViewHandler(),
				new MusicStealthMissionView(),
				ViewCreator.CreateMissionAgentStatusUIHandler(mission),
				ViewCreator.CreateMissionMainAgentEquipmentController(mission),
				ViewCreator.CreateMissionAgentLockVisualizerView(mission),
				SandBoxViewCreator.CreateMissionNameMarkerUIHandler(mission),
				new MissionItemContourControllerView(),
				new MissionAgentContourControllerView(),
				ViewCreator.CreateMissionBoundaryCrossingView(),
				ViewCreator.CreateMissionLeaveView(),
				ViewCreator.CreatePhotoModeView(),
				ViewCreator.CreateSingleplayerMissionKillNotificationUIHandler(),
				new MissionBoundaryWallView(),
				new MissionCampaignBattleSpectatorView(),
				SandBoxViewCreator.CreateMissionAgentAlarmStateView(mission),
				SandBoxViewCreator.CreateMissionMainAgentDetectionView(null),
				new StealthMissionUIHandler()
			}.ToArray();
		}
	}
}
