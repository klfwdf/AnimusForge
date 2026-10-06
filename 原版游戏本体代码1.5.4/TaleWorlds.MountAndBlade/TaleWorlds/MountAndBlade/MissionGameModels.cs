using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade.ComponentInterfaces;

namespace TaleWorlds.MountAndBlade;

public sealed class MissionGameModels : GameModelsManager
{
	public static MissionGameModels Current { get; private set; }

	public AgentStatCalculateModel AgentStatCalculateModel { get; private set; }

	public ApplyWeatherEffectsModel ApplyWeatherEffectsModel { get; private set; }

	public StrikeMagnitudeCalculationModel StrikeMagnitudeModel { get; private set; }

	public AgentApplyDamageModel AgentApplyDamageModel { get; private set; }

	public AgentDecideKilledOrUnconsciousModel AgentDecideKilledOrUnconsciousModel { get; private set; }

	public MissionDifficultyModel MissionDifficultyModel { get; private set; }

	public BattleMoraleModel BattleMoraleModel { get; private set; }

	public BattleInitializationModel BattleInitializationModel { get; private set; }

	public BattleSpawnModel BattleSpawnModel { get; private set; }

	public BattleBannerBearersModel BattleBannerBearersModel { get; private set; }

	public FormationArrangementModel FormationArrangementsModel { get; private set; }

	public AutoBlockModel AutoBlockModel { get; private set; }

	public DamageParticleModel DamageParticleModel { get; private set; }

	public ItemPickupModel ItemPickupModel { get; private set; }

	public MissionShipParametersModel MissionShipParametersModel { get; private set; }

	public MissionSiegeEngineCalculationModel MissionSiegeEngineCalculationModel { get; private set; }

	private void GetSpecificGameBehaviors()
	{
		AgentStatCalculateModel = GetGameModel<AgentStatCalculateModel>();
		ApplyWeatherEffectsModel = GetGameModel<ApplyWeatherEffectsModel>();
		StrikeMagnitudeModel = GetGameModel<StrikeMagnitudeCalculationModel>();
		AgentApplyDamageModel = GetGameModel<AgentApplyDamageModel>();
		AgentDecideKilledOrUnconsciousModel = GetGameModel<AgentDecideKilledOrUnconsciousModel>();
		MissionDifficultyModel = GetGameModel<MissionDifficultyModel>();
		BattleMoraleModel = GetGameModel<BattleMoraleModel>();
		BattleInitializationModel = GetGameModel<BattleInitializationModel>();
		BattleSpawnModel = GetGameModel<BattleSpawnModel>();
		BattleBannerBearersModel = GetGameModel<BattleBannerBearersModel>();
		FormationArrangementsModel = GetGameModel<FormationArrangementModel>();
		AutoBlockModel = GetGameModel<AutoBlockModel>();
		DamageParticleModel = GetGameModel<DamageParticleModel>();
		ItemPickupModel = GetGameModel<ItemPickupModel>();
		MissionShipParametersModel = GetGameModel<MissionShipParametersModel>();
		MissionSiegeEngineCalculationModel = GetGameModel<MissionSiegeEngineCalculationModel>();
	}

	private void MakeGameComponentBindings()
	{
	}

	public MissionGameModels(IEnumerable<GameModel> inputComponents)
		: base(inputComponents)
	{
		Current = this;
		GetSpecificGameBehaviors();
		MakeGameComponentBindings();
	}

	public static void Clear()
	{
		Current = null;
	}
}
