using System.Collections.Generic;
using NavalDLC.ComponentInterfaces;
using TaleWorlds.Core;

namespace NavalDLC;

public sealed class GameModels : GameModelsManager
{
	public static GameModels Instance => NavalDLCManager.Instance.GameModels;

	public ShipPhysicsParametersModel ShipPhysicsParametersModel { get; private set; }

	public ClanShipOwnershipModel ClanShipOwnershipModel { get; private set; }

	public ShipDeploymentModel ShipDeploymentModel { get; private set; }

	public MapStormModel MapStormModel { get; private set; }

	public GameModels(IEnumerable<GameModel> inputComponents)
		: base(inputComponents)
	{
		GetDefaultGameModels();
	}

	private void GetDefaultGameModels()
	{
		ShipPhysicsParametersModel = GetGameModel<ShipPhysicsParametersModel>();
		ClanShipOwnershipModel = GetGameModel<ClanShipOwnershipModel>();
		ShipDeploymentModel = GetGameModel<ShipDeploymentModel>();
		MapStormModel = GetGameModel<MapStormModel>();
	}
}
