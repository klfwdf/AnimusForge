using System;
using AnimusForge.SiegeAftermathIntervention;
using SandBox;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

/// <summary>
/// Opens castle GCCZ on Bannerlord's siege scene layer without starting another siege battle.
/// The standalone profile owns scene policy; this bridge only resolves live campaign objects.
/// </summary>
internal static class CastleAftermathSiegeSceneBridge
{
	private sealed class ScenePreparation
	{
		internal string SettlementId;
		internal string SceneName;
		internal string SceneLevels;
		internal float[] WallHitPointRatios;
		internal int BreachedWallSections;
	}

	private sealed class CastleSiegePreparationHandler : SiegeMissionPreparationHandler
	{
		private readonly ScenePreparation _preparation;

		internal CastleSiegePreparationHandler(ScenePreparation preparation)
			: base(false, false, preparation.WallHitPointRatios, false)
		{
			_preparation = preparation;
		}

		public override void OnBehaviorInitialize()
		{
			base.OnBehaviorInitialize();
			GcczDiagnosticLog.Log("CastleSiegeScene", "preparation initialized settlement=" + (_preparation.SettlementId ?? "N/A")
				+ " scene=" + (_preparation.SceneName ?? "N/A")
				+ " levels=" + (_preparation.SceneLevels ?? "N/A")
				+ " breached=" + _preparation.BreachedWallSections);
		}
	}

	internal static bool TryOpenMission(Settlement settlement, Location location, string source)
	{
		if (settlement?.IsCastle != true
			|| location == null
			|| !string.Equals(location.StringId, SiegeCastleWarSceneProfile.CenterLocationId, StringComparison.OrdinalIgnoreCase)
			|| settlement.Town == null
			|| Campaign.Current?.Models?.LocationModel == null)
		{
			return false;
		}

		try
		{
			int wallLevel = settlement.Town.GetWallLevel();
			string sceneName = location.GetSceneName(wallLevel);
			string upgradeLevelTag = Campaign.Current.Models.LocationModel.GetUpgradeLevelTag(wallLevel);
			string sceneLevels = SiegeCastleWarSceneProfile.BuildSceneLevels(upgradeLevelTag);
			float[] wallRatios = SiegeCastleWarSceneProfile.NormalizeWallHitPointRatios(settlement.SettlementWallSectionHitPointsRatioList);
			if (string.IsNullOrWhiteSpace(sceneName) || wallRatios.Length == 0)
			{
				Logger.Log("CastleAftermath", "Castle siege scene context unavailable. Settlement=" + (settlement.StringId ?? "N/A")
					+ ", Scene=" + (sceneName ?? "N/A") + ", WallSections=" + wallRatios.Length);
				return false;
			}

			ScenePreparation preparation = new ScenePreparation
			{
				SettlementId = settlement.StringId ?? "",
				SceneName = sceneName,
				SceneLevels = sceneLevels,
				WallHitPointRatios = wallRatios,
				BreachedWallSections = SiegeCastleWarSceneProfile.CountBreachedWallSections(wallRatios)
			};

			Mission mission = SandBoxMissions.OpenCastleCourtyardMission(sceneName, sceneLevels, location, null);
			if (mission == null)
			{
				throw new InvalidOperationException("SandBoxMissions.OpenCastleCourtyardMission returned null.");
			}

			mission.AddMissionBehavior(new CastleSiegePreparationHandler(preparation));
			GcczDiagnosticLog.Log("CastleSiegeScene", "open requested settlement=" + (preparation.SettlementId ?? settlement.StringId ?? "N/A")
				+ " scene=" + sceneName + " levels=" + sceneLevels
				+ " wallSections=" + wallRatios.Length
				+ " breached=" + SiegeCastleWarSceneProfile.CountBreachedWallSections(wallRatios));
			Logger.Log("CastleAftermath", "Opened castle aftermath on vanilla siege scene layer. Scene=" + sceneName
				+ ", Levels=" + sceneLevels + ", WallSections=" + wallRatios.Length
				+ ", Breached=" + SiegeCastleWarSceneProfile.CountBreachedWallSections(wallRatios));
			return true;
		}
		catch (Exception ex)
		{
			GcczDiagnosticLog.Log("CastleSiegeScene", "open failed settlement=" + (settlement?.StringId ?? "N/A")
				+ " source=" + (source ?? "N/A") + " error=" + ex);
			Logger.Log("CastleAftermath", "Open castle aftermath siege scene failed. Source=" + (source ?? "N/A") + ", Error=" + ex);
			return false;
		}
	}
}
