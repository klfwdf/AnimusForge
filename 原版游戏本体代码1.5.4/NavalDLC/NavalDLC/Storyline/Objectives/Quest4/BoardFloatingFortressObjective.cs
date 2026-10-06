using NavalDLC.Missions.Objects;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Missions.Objectives;

namespace NavalDLC.Storyline.Objectives.Quest4;

public class BoardFloatingFortressObjective : MissionObjective
{
	private readonly MissionShip _playerShip;

	private readonly MBList<MissionShip> _enemyShips;

	public override string UniqueId => "naval_storyline_quest_4_board_floating_fortress_objective";

	public override TextObject Name => new TextObject("{=UcZmBaYV}Storm the Floating Fortress");

	public override TextObject Description => new TextObject("{=wCiAvXU6}Lead your fleet in to board Crusas’ lashed-together ships");

	public BoardFloatingFortressObjective(Mission mission, MissionShip playerShip, MBList<MissionShip> enemyShips)
		: base(mission)
	{
		_playerShip = playerShip;
		_enemyShips = enemyShips;
	}

	protected override bool IsActivationRequirementsMet()
	{
		return true;
	}

	protected override bool IsCompletionRequirementsMet()
	{
		foreach (MissionShip connectedShip in _playerShip.GetConnectedShips())
		{
			if (_enemyShips.Contains(connectedShip))
			{
				return true;
			}
		}
		return false;
	}
}
