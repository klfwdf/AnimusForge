namespace SandBox.View.Missions.SandBox;

public class SpawnPointUnits
{
	public enum SceneType
	{
		Center,
		Shipyard,
		Tavern,
		VillageCenter,
		Arena,
		LordsHall,
		Castle,
		Dungeon,
		EmptyShop,
		All,
		NotDetermined
	}

	public int CurrentCount;

	public int SpawnedAgentCount;

	public string SpName { get; private set; }

	public SceneType Place { get; private set; }

	public int MinCount { get; private set; }

	public int MaxCount { get; private set; }

	public string Type { get; private set; }

	public SpawnPointUnits(string sp_name, SceneType place, int minCount, int maxCount)
	{
		SpName = sp_name;
		Place = place;
		MinCount = minCount;
		MaxCount = maxCount;
		CurrentCount = 0;
		SpawnedAgentCount = 0;
		Type = "other";
	}

	public SpawnPointUnits(string sp_name, SceneType place, string type, int minCount, int maxCount)
	{
		SpName = sp_name;
		Place = place;
		Type = type;
		MinCount = minCount;
		MaxCount = maxCount;
		CurrentCount = 0;
		SpawnedAgentCount = 0;
	}
}
