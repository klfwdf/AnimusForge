using System;
using System.Collections.Generic;
using SandBox.View.Missions;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Map;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Source.Missions;
using TaleWorlds.MountAndBlade.Source.Missions.Handlers;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;

namespace AnimusForge;

/// <summary>Opens the camp mission from the camp menu (field-camp-v1 step 2).</summary>
internal static class FieldCampMissionLauncher
{
	private const string LogSource = "FieldCamp";

	internal static bool TryOpen(List<MobileParty> parties, out string failure)
	{
		failure = null;
		MobileParty main = MobileParty.MainParty;
		try
		{
			IMapScene map = Campaign.Current.MapSceneWrapper;
			MapPatchData patch = map.GetMapPatchAtPosition(main.Position);
			string scene = Campaign.Current.Models.SceneModel.GetBattleSceneForMapPatch(patch, false);
			if (string.IsNullOrWhiteSpace(scene))
			{
				failure = "no battle scene for this map patch";
				return false;
			}
			var rec = new MissionInitializerRecord(scene)
			{
				TerrainType = (int)BannerlordApiCompat.ResolveTerrainTypeForParty(main, TerrainType.Plain, allowNavigationFaceFallback: false),
				DamageToFriendsMultiplier = Campaign.Current.Models.DifficultyModel.GetPlayerTroopsReceivedDamageMultiplier(),
				DamageFromPlayerToFriendsMultiplier = Campaign.Current.Models.DifficultyModel.GetPlayerTroopsReceivedDamageMultiplier(),
				NeedsRandomTerrain = false,
				PlayingInCampaignMode = true,
				DoNotUseLoadingScreen = false,
				RandomTerrainSeed = MBRandom.RandomInt(10000),
				AtmosphereOnCampaign = Campaign.Current.Models.MapWeatherModel.GetAtmosphereModel(main.Position),
				DecalAtlasGroup = 2,
				SceneHasMapPatch = true,
				PatchCoordinates = patch.normalizedCoordinates,
				PatchEncounterDir = Vec2.Forward
			};
			var snapshot = new List<MobileParty>(parties);
			Mission mission = MissionState.OpenNew(FieldCampMissionLogic.MissionName, rec, m => new MissionBehavior[]
			{
				new MissionOptionsComponent(),
				new FieldCampMissionLogic(snapshot),
				// Hold TAB to leave (also ends the mission if the player is knocked out).
				new BasicLeaveMissionLogic(),
				new AgentHumanAILogic(),
				new MissionHardBorderPlacer(),
				new MissionBoundaryPlacer(),
				CreateBoundaryCrossingHandler()
			});
			if (mission == null)
			{
				failure = "MissionState.OpenNew returned null";
				return false;
			}
			Logger.Log(LogSource, "camp mission opening scene=" + scene + " parties=" + snapshot.Count);
			return true;
		}
		catch (Exception ex)
		{
			failure = ex.GetType().Name + ": " + ex.Message;
			return false;
		}
	}

	private static MissionBehavior CreateBoundaryCrossingHandler()
	{
#if BANNERLORD_1_4_OR_GREATER
		return new MissionBoundaryCrossingHandler();
#else
		return new MissionBoundaryCrossingHandler(10f);
#endif
	}
}

/// <summary>Mission views for the camp: escape menu, agent labels, options, cheering.</summary>
[ViewCreatorModule]
public class FieldCampMissionViews
{
	[ViewMethod(FieldCampMissionLogic.MissionName)]
	public static MissionView[] OpenFieldCampMission(Mission mission)
	{
		return new MissionView[]
		{
			new MissionCampaignView(),
			ViewCreator.CreateMissionSingleplayerEscapeMenu(CampaignOptions.IsIronmanMode),
			ViewCreator.CreateMissionAgentLabelUIHandler(mission),
			ViewCreator.CreateOptionsUIHandler(),
			new MissionSingleplayerViewHandler(),
			ViewCreator.CreateMissionMainAgentEquipmentController(mission),
			ViewCreator.CreateMissionMainAgentCheerBarkControllerView(mission),
			ViewCreator.CreateMissionBoundaryCrossingView(),
			// Hold-TAB progress ring; without it a TAB tap gives no feedback.
			ViewCreator.CreateMissionLeaveView(),
			new MissionBoundaryWallView(),
			new MissionItemContourControllerView(),
			new MissionAgentContourControllerView(),
			ViewCreator.CreatePhotoModeView(),
			new MissionFaceCacheView()
		};
	}
}
