using System;
using System.Collections.Generic;
using NavalDLC.Missions.MissionLogics;
using NavalDLC.Missions.Objects;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.MissionViews;

namespace NavalDLC.View.MissionViews;

public class NavalShipTargetSelectionHandler : MissionView
{
	public const float MaxDistanceForFocusCheck = 1000f;

	public const float MinDistanceForFocusCheck = 8f;

	public readonly float MaxDistanceToCenterForFocus = 70f * (Screen.RealScreenResolutionHeight / 1080f);

	private readonly List<(MissionShip Ship, float DistanceToScreenCenter)> _distanceCache = new List<(MissionShip, float)>();

	private MissionShip _focusedShip;

	private readonly MBList<MissionShip> _enemyShipsCache = new MBList<MissionShip>();

	private Vec2 _centerOfScreen = new Vec2(Screen.RealScreenResolutionWidth / 2f, Screen.RealScreenResolutionHeight / 2f);

	private bool _isTargetingDisabled;

	private Camera ActiveCamera => base.MissionScreen.CustomCamera ?? base.MissionScreen.CombatCamera;

	public event Action<MissionShip> OnShipsFocused;

	public override void OnPreDisplayMissionTick(float dt)
	{
		base.OnPreDisplayMissionTick(dt);
		_distanceCache.Clear();
		_focusedShip = null;
		_enemyShipsCache.Clear();
		NavalShipsLogic missionBehavior = base.Mission.GetMissionBehavior<NavalShipsLogic>();
		if (missionBehavior == null)
		{
			return;
		}
		if (!_isTargetingDisabled)
		{
			missionBehavior.FillTeamShips(TeamSideEnum.EnemyTeam, _enemyShipsCache);
			Vec3 position = ActiveCamera.Position;
			_centerOfScreen.x = Screen.RealScreenResolutionWidth / 2f;
			_centerOfScreen.y = Screen.RealScreenResolutionHeight / 2f;
			for (int i = 0; i < _enemyShipsCache.Count; i++)
			{
				MissionShip missionShip = _enemyShipsCache[i];
				TryGetShipDistanceToCenter(missionShip, position, out var isShipFocusable, out var distanceToScreenCenter);
				if (isShipFocusable)
				{
					_distanceCache.Add((missionShip, distanceToScreenCenter));
				}
			}
		}
		float num = MaxDistanceToCenterForFocus;
		for (int j = 0; j < _distanceCache.Count; j++)
		{
			(MissionShip, float) tuple = _distanceCache[j];
			if (tuple.Item2 < num)
			{
				num = tuple.Item2;
				(_focusedShip, _) = tuple;
			}
		}
		this.OnShipsFocused?.Invoke(_focusedShip);
	}

	private void TryGetShipDistanceToCenter(MissionShip ship, Vec3 cameraPosition, out bool isShipFocusable, out float distanceToScreenCenter)
	{
		Vec3 origin = ship.GlobalFrame.origin;
		float num = origin.AsVec2.Distance(cameraPosition.AsVec2);
		float screenX = 0f;
		float screenY = 0f;
		float w = 0f;
		MBWindowManager.WorldToScreenInsideUsableArea(ActiveCamera, origin + Vec3.Up * 3f, ref screenX, ref screenY, ref w);
		bool flag = w <= 0f;
		if (num >= 1000f || num <= 8f || flag)
		{
			distanceToScreenCenter = 2.1474836E+09f;
			isShipFocusable = false;
		}
		else
		{
			isShipFocusable = true;
			distanceToScreenCenter = new Vec2(screenX, screenY).Distance(_centerOfScreen);
		}
	}

	public void SetIsFormationTargetingDisabled(bool isDisabled)
	{
		if (_isTargetingDisabled != isDisabled)
		{
			_isTargetingDisabled = isDisabled;
			if (isDisabled)
			{
				_distanceCache.Clear();
				_enemyShipsCache.Clear();
				_focusedShip = null;
				this.OnShipsFocused?.Invoke(null);
			}
		}
	}

	public override void OnRemoveBehavior()
	{
		_distanceCache.Clear();
		_focusedShip = null;
		this.OnShipsFocused = null;
		base.OnRemoveBehavior();
	}
}
