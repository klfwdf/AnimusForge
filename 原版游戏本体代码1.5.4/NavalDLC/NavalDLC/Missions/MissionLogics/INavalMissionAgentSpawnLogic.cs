namespace NavalDLC.Missions.MissionLogics;

internal interface INavalMissionAgentSpawnLogic
{
	int DeployablePlayerShipCount { get; }

	bool ReassignCaptainsOfRemovedShips { get; }

	void SetReassignCaptainsOfRemovedShips(bool value);

	void OnPlayerShipsUpdated();
}
