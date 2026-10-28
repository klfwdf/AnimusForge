namespace TaleWorlds.Core;

public interface IMissionSiegeWeapon
{
	int Index { get; }

	SiegeEngineType Type { get; }

	float Health { get; }

	float InitialHealth { get; }

	float MaxHealth { get; }
}
