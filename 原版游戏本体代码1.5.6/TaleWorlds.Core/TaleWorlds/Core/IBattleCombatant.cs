using System;
using TaleWorlds.Localization;

namespace TaleWorlds.Core;

public interface IBattleCombatant
{
	TextObject Name { get; }

	BattleSideEnum Side { get; }

	BasicCultureObject BasicCulture { get; }

	BasicCharacterObject General { get; }

	Tuple<uint, uint> PrimaryColorPair { get; }

	Banner Banner { get; }

	BattleEnvironment CurrentBattleEnvironment { get; }

	int GetTacticsSkillAmount();

	int GetNumberOfMissionReadyTroops();

	bool IsUnderPlayersCommand(BattleSideEnum playerSide);
}
