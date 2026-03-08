using System;
using System.Collections.Generic;
using SandBox.View.Missions.Sound.Components;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TaleWorlds.MountAndBlade.View.MissionViews.Singleplayer;

namespace SandBox.View.Missions.Tournaments
{
	// Token: 0x0200002A RID: 42
	[ViewCreatorModule]
	public class TournamentMissionViews
	{
		// Token: 0x0600011F RID: 287 RVA: 0x0000D198 File Offset: 0x0000B398
		[ViewMethod("TournamentArchery")]
		public static MissionView[] OpenTournamentArcheryMission(Mission mission)
		{
			return new List<MissionView>
			{
				new MissionCampaignView(),
				new MissionConversationCameraView(),
				ViewCreator.CreateMissionSingleplayerEscapeMenu(CampaignOptions.IsIronmanMode),
				ViewCreator.CreateOptionsUIHandler(),
				ViewCreator.CreateMissionMainAgentEquipDropView(mission),
				ViewCreator.CreateMissionAgentStatusUIHandler(mission),
				ViewCreator.CreateMissionMainAgentEquipmentController(mission),
				ViewCreator.CreateMissionMainAgentCheerBarkControllerView(mission),
				ViewCreator.CreateMissionAgentLockVisualizerView(mission),
				ViewCreator.CreateMissionSpectatorControlView(mission),
				SandBoxViewCreator.CreateMissionTournamentView(),
				new MissionAudienceHandler(0.4f + MBRandom.RandomFloat * 0.6f),
				new MissionSingleplayerViewHandler(),
				new MissionMessageUIHandler(),
				ViewCreator.CreateSingleplayerMissionKillNotificationUIHandler(),
				ViewCreator.CreateMissionAgentLabelUIHandler(mission),
				new MissionItemContourControllerView(),
				ViewCreator.CreatePhotoModeView(),
				new ArenaPreloadView()
			}.ToArray();
		}

		// Token: 0x06000120 RID: 288 RVA: 0x0000D2A0 File Offset: 0x0000B4A0
		[ViewMethod("TournamentFight")]
		public static MissionView[] OpenTournamentFightMission(Mission mission)
		{
			return new List<MissionView>
			{
				new MissionCampaignView(),
				new MissionConversationCameraView(),
				ViewCreator.CreateMissionSingleplayerEscapeMenu(CampaignOptions.IsIronmanMode),
				ViewCreator.CreateOptionsUIHandler(),
				ViewCreator.CreateMissionMainAgentEquipDropView(mission),
				SandBoxViewCreator.CreateMissionTournamentView(),
				new MissionAudienceHandler(0.4f + MBRandom.RandomFloat * 0.6f),
				ViewCreator.CreateMissionAgentStatusUIHandler(mission),
				ViewCreator.CreateMissionMainAgentEquipmentController(mission),
				ViewCreator.CreateMissionMainAgentCheerBarkControllerView(mission),
				ViewCreator.CreateMissionAgentLockVisualizerView(mission),
				ViewCreator.CreateMissionSpectatorControlView(mission),
				new MusicTournamentMissionView(),
				new MissionSingleplayerViewHandler(),
				ViewCreator.CreateSingleplayerMissionKillNotificationUIHandler(),
				ViewCreator.CreateMissionAgentLabelUIHandler(mission),
				new MissionItemContourControllerView(),
				new MissionCampaignBattleSpectatorView(),
				ViewCreator.CreatePhotoModeView(),
				new ArenaPreloadView()
			}.ToArray();
		}

		// Token: 0x06000121 RID: 289 RVA: 0x0000D3B0 File Offset: 0x0000B5B0
		[ViewMethod("TournamentHorseRace")]
		public static MissionView[] OpenTournamentHorseRaceMission(Mission mission)
		{
			return new List<MissionView>
			{
				new MissionCampaignView(),
				new MissionConversationCameraView(),
				ViewCreator.CreateMissionSingleplayerEscapeMenu(CampaignOptions.IsIronmanMode),
				ViewCreator.CreateOptionsUIHandler(),
				ViewCreator.CreateMissionMainAgentEquipDropView(mission),
				ViewCreator.CreateMissionAgentStatusUIHandler(mission),
				ViewCreator.CreateMissionMainAgentEquipmentController(mission),
				ViewCreator.CreateMissionMainAgentCheerBarkControllerView(mission),
				ViewCreator.CreateMissionAgentLockVisualizerView(mission),
				ViewCreator.CreateMissionSpectatorControlView(mission),
				new MissionTournamentView(),
				new MissionSingleplayerViewHandler(),
				new MissionAudienceHandler(0.4f + MBRandom.RandomFloat * 0.6f),
				ViewCreator.CreateSingleplayerMissionKillNotificationUIHandler(),
				ViewCreator.CreateMissionAgentLabelUIHandler(mission),
				new MissionItemContourControllerView(),
				new MissionCampaignBattleSpectatorView(),
				ViewCreator.CreatePhotoModeView(),
				new ArenaPreloadView()
			}.ToArray();
		}

		// Token: 0x06000122 RID: 290 RVA: 0x0000D4B8 File Offset: 0x0000B6B8
		[ViewMethod("TournamentJousting")]
		public static MissionView[] OpenTournamentJoustingMission(Mission mission)
		{
			return new List<MissionView>
			{
				new MissionCampaignView(),
				new MissionConversationCameraView(),
				ViewCreator.CreateMissionSingleplayerEscapeMenu(CampaignOptions.IsIronmanMode),
				ViewCreator.CreateOptionsUIHandler(),
				ViewCreator.CreateMissionMainAgentEquipDropView(mission),
				ViewCreator.CreateMissionAgentStatusUIHandler(mission),
				ViewCreator.CreateMissionMainAgentEquipmentController(mission),
				ViewCreator.CreateMissionMainAgentCheerBarkControllerView(mission),
				ViewCreator.CreateMissionAgentLockVisualizerView(mission),
				ViewCreator.CreateMissionSpectatorControlView(mission),
				SandBoxViewCreator.CreateMissionTournamentView(),
				new MissionAudienceHandler(0.4f + MBRandom.RandomFloat * 0.6f),
				new MissionSingleplayerViewHandler(),
				new MissionMessageUIHandler(),
				new MissionScoreUIHandler(),
				ViewCreator.CreateSingleplayerMissionKillNotificationUIHandler(),
				new MissionTournamentJoustingView(),
				ViewCreator.CreateMissionAgentLabelUIHandler(mission),
				new MissionItemContourControllerView(),
				new MissionCampaignBattleSpectatorView(),
				ViewCreator.CreatePhotoModeView(),
				new ArenaPreloadView()
			}.ToArray();
		}
	}
}
