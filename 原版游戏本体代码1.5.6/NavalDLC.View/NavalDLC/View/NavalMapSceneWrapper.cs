using System.Collections.Generic;
using System.Linq;
using SandBox;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace NavalDLC.View;

public class NavalMapSceneWrapper : INavalMapSceneWrapper
{
	private const string VillageDropOffPointTag = "main_map_village_dropoff";

	private MapScene _mapScene;

	private Dictionary<string, List<(CampaignVec2, float)>> _pirateSpawnPoints = new Dictionary<string, List<(CampaignVec2, float)>>();

	public NavalMapSceneWrapper()
	{
		_mapScene = (MapScene)Campaign.Current.MapSceneWrapper;
		InitializePirateSpawnPoints();
		InitializeDropOffLocations();
		InitializeMapWaterWake();
	}

	public void Tick(float dt)
	{
	}

	private void InitializePirateSpawnPoints()
	{
		List<GameEntity> entities = new List<GameEntity>();
		_mapScene.Scene.GetAllEntitiesWithScriptComponent<PirateSpawnPoint>(ref entities);
		for (int i = 0; i < entities.Count; i++)
		{
			PirateSpawnPoint firstScriptOfType = entities[i].GetFirstScriptOfType<PirateSpawnPoint>();
			string clanStringId = firstScriptOfType.ClanStringId;
			if (!_pirateSpawnPoints.TryGetValue(clanStringId, out var _))
			{
				_pirateSpawnPoints[clanStringId] = new List<(CampaignVec2, float)>();
			}
			CampaignVec2 item = new CampaignVec2(firstScriptOfType.GetPosition(), isOnLand: false);
			_pirateSpawnPoints[clanStringId].Add((item, firstScriptOfType.Radius));
		}
	}

	public List<(CampaignVec2, float)> GetSpawnPoints(string stringId)
	{
		if (_pirateSpawnPoints.TryGetValue(stringId, out var value))
		{
			return value;
		}
		return new List<(CampaignVec2, float)>();
	}

	private List<(CampaignVec2, float)> GetSpawnPoints()
	{
		List<(CampaignVec2, float)> list = new List<(CampaignVec2, float)>();
		foreach (KeyValuePair<string, List<(CampaignVec2, float)>> pirateSpawnPoint in _pirateSpawnPoints)
		{
			list.AddRange(pirateSpawnPoint.Value);
		}
		return list;
	}

	private void InitializeDropOffLocations()
	{
		foreach (GameEntity entity in _mapScene.Scene.FindEntitiesWithTag("main_map_village_dropoff"))
		{
			Village? village = Village.All.FirstOrDefault((Village x) => x.Settlement.StringId == entity.Parent.Name);
			CampaignVec2 portPosition = new CampaignVec2(entity.GlobalPosition.AsVec2, isOnLand: false);
			village.Settlement.SetPortPosition(portPosition);
		}
	}

	public Vec2 GetWindAtPosition(Vec2 position)
	{
		return _mapScene.GetWindAtPosition(position);
	}

	private void InitializeMapWaterWake()
	{
		_mapScene.SetupWaterWake(128f, 8f);
	}
}
