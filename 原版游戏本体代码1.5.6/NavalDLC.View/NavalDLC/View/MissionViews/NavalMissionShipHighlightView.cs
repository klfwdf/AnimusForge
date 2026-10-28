using System.Collections.Generic;
using NavalDLC.Missions.MissionLogics;
using NavalDLC.Missions.Objects;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.MissionViews;

namespace NavalDLC.View.MissionViews;

public class NavalMissionShipHighlightView : MissionView
{
	private NavalShipsLogic _navalShipsLogic;

	private Dictionary<MissionShip, (bool, uint)> _contourCache = new Dictionary<MissionShip, (bool, uint)>();

	private MissionShip _focusedShip;

	public override void OnMissionScreenInitialize()
	{
		base.OnMissionScreenInitialize();
		_navalShipsLogic = base.Mission.GetMissionBehavior<NavalShipsLogic>();
	}

	public override void OnMissionScreenTick(float dt)
	{
		base.OnMissionScreenTick(dt);
		UpdateSelectedShipContours();
	}

	public override void OnMissionScreenDeactivate()
	{
		base.OnMissionScreenDeactivate();
		_contourCache.Clear();
	}

	public void OnShipFocused(MissionShip focusedShip)
	{
		_focusedShip = focusedShip;
	}

	private void UpdateSelectedShipContours()
	{
		if (_navalShipsLogic?.AllShips == null)
		{
			foreach (KeyValuePair<MissionShip, (bool, uint)> item in _contourCache)
			{
				MissionShip key = item.Key;
				if (key != null && key.GameEntity.IsValid)
				{
					item.Key?.GameEntity.SetContourColor(null, alwaysVisible: false);
				}
			}
			return;
		}
		for (int i = 0; i < _navalShipsLogic.AllShips.Count; i++)
		{
			MissionShip missionShip = _navalShipsLogic.AllShips[i];
			if (missionShip == null || !missionShip.GameEntity.IsValid)
			{
				continue;
			}
			uint num = 0u;
			bool flag;
			if (base.Mission.Mode == MissionMode.Deployment || base.Mission.IsOrderMenuOpen)
			{
				flag = missionShip.Formation != null && (missionShip.Captain == null || missionShip.Captain != Agent.Main) && base.Mission.PlayerTeam.PlayerOrderController.SelectedFormations.Contains(missionShip.Formation);
				num = 4294105105u;
			}
			else
			{
				flag = _focusedShip == missionShip && base.Input.IsGameKeyDown(5);
				if (_focusedShip?.Team != null)
				{
					switch (_focusedShip.Team.TeamSide)
					{
					case TeamSideEnum.PlayerTeam:
						num = 4282512610u;
						break;
					case TeamSideEnum.PlayerAllyTeam:
						num = 4282578006u;
						break;
					case TeamSideEnum.EnemyTeam:
						num = 4294197569u;
						break;
					}
				}
			}
			bool flag2 = false;
			if (_contourCache.TryGetValue(missionShip, out var value))
			{
				if (value.Item1 != flag || value.Item2 != num)
				{
					flag2 = true;
					_contourCache[missionShip] = (flag, num);
				}
			}
			else
			{
				flag2 = true;
				_contourCache[missionShip] = (flag, num);
			}
			if (flag2)
			{
				if (flag)
				{
					missionShip.GameEntity.SetContourColor(num);
				}
				else
				{
					missionShip.GameEntity.SetContourColor(null, alwaysVisible: false);
				}
			}
		}
	}
}
