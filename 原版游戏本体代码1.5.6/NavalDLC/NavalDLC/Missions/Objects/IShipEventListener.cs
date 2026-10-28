using TaleWorlds.MountAndBlade;

namespace NavalDLC.Missions.Objects;

internal interface IShipEventListener
{
	void OnShipSpawned(MissionShip ship);

	void OnShipRemoved(MissionShip ship);

	void OnShipTransferred(MissionShip ship, Formation oldFormation);
}
