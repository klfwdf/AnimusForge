using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public struct MissionSpawnSettings
{
	public enum ReinforcementSpawnMethod
	{
		Balanced,
		Wave,
		Fixed
	}

	public enum ReinforcementTimingMethod
	{
		GlobalTimer,
		CustomTimer
	}

	public enum InitialSpawnMethod
	{
		BattleSizeAllocating,
		FreeAllocation
	}

	public const float MinimumReinforcementInterval = 1f;

	public const float MinimumDefenderAdvantageFactor = 0.1f;

	public const float MaximumDefenderAdvantageFactor = 10f;

	public const float MinimumBattleSizeRatioLimit = 0.5f;

	public const float MaximumBattleSizeRatioLimit = 0.99f;

	public const float DefaultMaximumBattleSizeRatio = 0.75f;

	public const float DefaultDefenderAdvantageFactor = 1f;

	private float _globalReinforcementInterval;

	private float _defenderAdvantageFactor;

	private float _maximumBattleSizeRatio;

	private float _reinforcementBatchPercentage;

	private float _desiredReinforcementPercentage;

	private float _reinforcementWavePercentage;

	private int _maximumReinforcementWaveCount;

	private float _defenderReinforcementBatchPercentage;

	private float _attackerReinforcementBatchPercentage;

	public float GlobalReinforcementInterval
	{
		get
		{
			return _globalReinforcementInterval;
		}
		set
		{
			_globalReinforcementInterval = MathF.Max(value, 1f);
		}
	}

	public float DefenderAdvantageFactor
	{
		get
		{
			return _defenderAdvantageFactor;
		}
		set
		{
			_defenderAdvantageFactor = MathF.Clamp(value, 0.1f, 10f);
		}
	}

	public float MaximumBattleSideRatio
	{
		get
		{
			return _maximumBattleSizeRatio;
		}
		set
		{
			_maximumBattleSizeRatio = MathF.Clamp(value, 0.5f, 0.99f);
		}
	}

	public InitialSpawnMethod InitialTroopsSpawnMethod { get; private set; }

	public ReinforcementTimingMethod ReinforcementTroopsTimingMethod { get; private set; }

	public ReinforcementSpawnMethod ReinforcementTroopsSpawnMethod { get; private set; }

	public float ReinforcementBatchPercentage
	{
		get
		{
			return _reinforcementBatchPercentage;
		}
		set
		{
			_reinforcementBatchPercentage = MathF.Clamp(value, 0f, 1f);
		}
	}

	public float DesiredReinforcementPercentage
	{
		get
		{
			return _desiredReinforcementPercentage;
		}
		set
		{
			_desiredReinforcementPercentage = MathF.Clamp(value, 0f, 1f);
		}
	}

	public float ReinforcementWavePercentage
	{
		get
		{
			return _reinforcementWavePercentage;
		}
		set
		{
			_reinforcementWavePercentage = MathF.Clamp(value, 0f, 1f);
		}
	}

	public int MaximumReinforcementWaveCount
	{
		get
		{
			return _maximumReinforcementWaveCount;
		}
		set
		{
			_maximumReinforcementWaveCount = MathF.Max(value, 0);
		}
	}

	public float DefenderReinforcementBatchPercentage
	{
		get
		{
			return _defenderReinforcementBatchPercentage;
		}
		set
		{
			_defenderReinforcementBatchPercentage = MathF.Clamp(value, 0f, 1f);
		}
	}

	public float AttackerReinforcementBatchPercentage
	{
		get
		{
			return _attackerReinforcementBatchPercentage;
		}
		set
		{
			_attackerReinforcementBatchPercentage = MathF.Clamp(value, 0f, 1f);
		}
	}

	public MissionSpawnSettings(InitialSpawnMethod initialTroopsSpawnMethod, ReinforcementTimingMethod reinforcementTimingMethod, ReinforcementSpawnMethod reinforcementTroopsSpawnMethod, float globalReinforcementInterval = 0f, float reinforcementBatchPercentage = 0f, float desiredReinforcementPercentage = 0f, float reinforcementWavePercentage = 0f, int maximumReinforcementWaveCount = 0, float defenderReinforcementBatchPercentage = 0f, float attackerReinforcementBatchPercentage = 0f, float defenderAdvantageFactor = 1f, float maximumBattleSizeRatio = 0.75f)
	{
		this = default(MissionSpawnSettings);
		InitialTroopsSpawnMethod = initialTroopsSpawnMethod;
		ReinforcementTroopsTimingMethod = reinforcementTimingMethod;
		ReinforcementTroopsSpawnMethod = reinforcementTroopsSpawnMethod;
		GlobalReinforcementInterval = globalReinforcementInterval;
		ReinforcementBatchPercentage = reinforcementBatchPercentage;
		DesiredReinforcementPercentage = desiredReinforcementPercentage;
		ReinforcementWavePercentage = reinforcementWavePercentage;
		MaximumReinforcementWaveCount = maximumReinforcementWaveCount;
		DefenderReinforcementBatchPercentage = defenderReinforcementBatchPercentage;
		AttackerReinforcementBatchPercentage = attackerReinforcementBatchPercentage;
		DefenderAdvantageFactor = defenderAdvantageFactor;
		MaximumBattleSideRatio = maximumBattleSizeRatio;
	}

	public static MissionSpawnSettings CreateDefaultSpawnSettings()
	{
		return new MissionSpawnSettings(InitialSpawnMethod.BattleSizeAllocating, ReinforcementTimingMethod.GlobalTimer, ReinforcementSpawnMethod.Balanced, 10f, 0.05f, 0.166f);
	}
}
