using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.DotNet;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.Objects;

public class FlagCapturePoint : SynchedMissionObject
{
	public const float PointRadius = 4f;

	public const float RadiusMultiplierForContestedArea = 1.5f;

	private const float TimeToTravelBetweenBoundaries = 10f;

	public int FlagIndex;

	private SynchedMissionObject _theFlag;

	private SynchedMissionObject _flagHolder;

	private GameEntity _flagBottomBoundary;

	private GameEntity _flagTopBoundary;

	private List<SynchedMissionObject> _flagDependentObjects;

	private CaptureTheFlagFlagDirection _currentDirection = CaptureTheFlagFlagDirection.None;

	[EditableScriptComponentVariable(false, "")]
	public Vec3 Position => base.GameEntity.GlobalPosition;

	public int FlagChar => 65 + FlagIndex;

	public bool IsContested => _currentDirection == CaptureTheFlagFlagDirection.Down;

	public bool IsFullyRaised => _currentDirection == CaptureTheFlagFlagDirection.None;

	public bool IsDeactivated => !base.GameEntity.IsVisibleIncludeParents();

	protected internal override void OnMissionReset()
	{
		_currentDirection = CaptureTheFlagFlagDirection.None;
	}

	public void ResetPointAsServer(uint defaultColor, uint defaultColor2)
	{
		MatrixFrame frame = _flagTopBoundary.GetGlobalFrame();
		_flagHolder.SetGlobalFrameSynched(ref frame);
		SetTeamColorsWithAllSynched(defaultColor, defaultColor2);
		SetVisibleWithAllSynched(value: true);
	}

	public void RemovePointAsServer()
	{
		SetVisibleWithAllSynched(value: false);
	}

	protected internal override void OnInit()
	{
		_flagHolder = base.GameEntity.GetFirstChildEntityWithTag("score_stand").GetFirstScriptOfType<SynchedMissionObject>();
		_theFlag = _flagHolder.GameEntity.GetFirstChildEntityWithTag("flag_white").GetFirstScriptOfType<SynchedMissionObject>();
		_flagBottomBoundary = TaleWorlds.Engine.GameEntity.CreateFromWeakEntity(base.GameEntity.GetFirstChildEntityWithTag("flag_raising_bottom"));
		_flagTopBoundary = TaleWorlds.Engine.GameEntity.CreateFromWeakEntity(base.GameEntity.GetFirstChildEntityWithTag("flag_raising_top"));
		MatrixFrame frame = _flagTopBoundary.GetGlobalFrame();
		_flagHolder.GameEntity.SetGlobalFrame(in frame);
		_flagDependentObjects = new List<SynchedMissionObject>();
		foreach (WeakGameEntity item in Mission.Current.Scene.FindWeakEntitiesWithTag("depends_flag_" + FlagIndex).ToList())
		{
			SynchedMissionObject firstScriptOfType = item.GetFirstScriptOfType<SynchedMissionObject>();
			_flagDependentObjects.Add(firstScriptOfType);
		}
	}

	protected internal override void OnEditorTick(float dt)
	{
		base.OnEditorTick(dt);
		if (MBEditor.IsEntitySelected(base.GameEntity))
		{
			DebugExtensions.RenderDebugCircleOnTerrain(base.Scene, base.GameEntity.GetGlobalFrame(), 4f, 2852192000u);
			DebugExtensions.RenderDebugCircleOnTerrain(base.Scene, base.GameEntity.GetGlobalFrame(), 6f, 2868838400u);
		}
	}

	public void OnAfterTick(bool canOwnershipChange, out bool ownerTeamChanged)
	{
		ownerTeamChanged = false;
		if (!_flagHolder.SynchronizeCompleted)
		{
			return;
		}
		bool flag = _flagHolder.GameEntity.GlobalPosition.DistanceSquared(_flagTopBoundary.GlobalPosition).ApproximatelyEqualsTo(0f);
		if (canOwnershipChange)
		{
			if (!flag)
			{
				ownerTeamChanged = true;
			}
			else
			{
				_currentDirection = CaptureTheFlagFlagDirection.None;
			}
		}
		else if (flag)
		{
			_currentDirection = CaptureTheFlagFlagDirection.None;
		}
	}

	public void SetMoveFlag(CaptureTheFlagFlagDirection directionTo, float speedMultiplier = 1f)
	{
		float flagProgress = GetFlagProgress();
		float num = 1f / speedMultiplier;
		float num2 = ((directionTo == CaptureTheFlagFlagDirection.Up) ? (1f - flagProgress) : flagProgress);
		float num3 = 10f * num;
		float duration = num2 * num3;
		_currentDirection = directionTo;
		MatrixFrame frame = directionTo switch
		{
			CaptureTheFlagFlagDirection.Up => _flagTopBoundary.GetFrame(), 
			CaptureTheFlagFlagDirection.Down => _flagBottomBoundary.GetFrame(), 
			_ => throw new ArgumentOutOfRangeException("directionTo", directionTo, null), 
		};
		_flagHolder.SetFrameSynchedOverTime(ref frame, duration);
	}

	public void ChangeMovementSpeed(float speedMultiplier)
	{
		if (_currentDirection != CaptureTheFlagFlagDirection.None)
		{
			SetMoveFlag(_currentDirection, speedMultiplier);
		}
	}

	public void SetMoveNone()
	{
		_currentDirection = CaptureTheFlagFlagDirection.None;
		MatrixFrame frame = _flagHolder.GameEntity.GetFrame();
		_flagHolder.SetFrameSynched(ref frame);
	}

	public void SetVisibleWithAllSynched(bool value, bool forceChildrenVisible = false)
	{
		SetVisibleSynched(value, forceChildrenVisible);
		foreach (SynchedMissionObject flagDependentObject in _flagDependentObjects)
		{
			flagDependentObject.SetVisibleSynched(value);
		}
	}

	public void SetTeamColorsWithAllSynched(uint color, uint color2)
	{
		_theFlag.SetTeamColorsSynched(color, color2);
		foreach (SynchedMissionObject flagDependentObject in _flagDependentObjects)
		{
			flagDependentObject.SetTeamColorsSynched(color, color2);
		}
	}

	public uint GetFlagColor()
	{
		return _theFlag.Color;
	}

	public uint GetFlagColor2()
	{
		return _theFlag.Color2;
	}

	public float GetFlagProgress()
	{
		return TaleWorlds.Library.MathF.Clamp((_theFlag.GameEntity.GlobalPosition.z - _flagBottomBoundary.GlobalPosition.z) / (_flagTopBoundary.GlobalPosition.z - _flagBottomBoundary.GlobalPosition.z), 0f, 1f);
	}
}
