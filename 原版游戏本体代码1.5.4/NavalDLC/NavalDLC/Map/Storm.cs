using System;
using System.Diagnostics;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.SaveSystem;

namespace NavalDLC.Map;

public class Storm
{
	public enum StormTypes
	{
		Storm,
		ThunderStorm,
		Hurricane
	}

	public struct PreviousData : ISavedStruct
	{
		[SaveableField(10)]
		public Vec2 Position;

		[SaveableField(20)]
		public float EffectRadius;

		public static PreviousData Invalid => new PreviousData(Vec2.Invalid, -1f);

		public PreviousData(Vec2 position, float effectRadius)
		{
			Position = position;
			EffectRadius = effectRadius;
		}

		public bool IsDefault()
		{
			if (Position == Vec2.Zero)
			{
				return EffectRadius == 0f;
			}
			return false;
		}

		public override string ToString()
		{
			return Position.ToString() + ": " + EffectRadius;
		}
	}

	private const int PreviousPositionsCount = 6;

	private const int LastPositionUpdatePeriodInHours = 4;

	[SaveableField(10)]
	private Vec2 _currentPosition;

	[SaveableField(100)]
	private PreviousData[] _previousPositionsAndRadius = new PreviousData[6];

	[SaveableField(120)]
	private int _nextUpdatePreviousDataArrayIndex;

	[SaveableField(130)]
	private CampaignTime _nextUpdateTime;

	[SaveableField(20)]
	public readonly StormTypes StormType;

	[SaveableField(30)]
	private float _intensity;

	private float _speed;

	[SaveableField(50)]
	private CampaignTime _developingStateFinishCampaignTime;

	[SaveableField(60)]
	private CampaignTime _finalizingStateStartCampaignTime;

	[SaveableField(80)]
	private Vec2 _desiredMoveDirection;

	[SaveableField(90)]
	private Vec2 _currentMoveDirection;

	public bool IsActive
	{
		get
		{
			if (!IsInDevelopingState)
			{
				return !IsInFinalizingState;
			}
			return false;
		}
	}

	public Vec2 CurrentPosition
	{
		get
		{
			return _currentPosition;
		}
		private set
		{
			_currentPosition = value;
			if (IsPositionOutOfMapBoundary(_currentPosition))
			{
				ForceDeactivate();
			}
			SetVisualDirty();
		}
	}

	public float Intensity
	{
		get
		{
			return _intensity;
		}
		set
		{
			_intensity = MBMath.ClampFloat(value, 0f, 1f);
			if (_intensity <= 0f)
			{
				ForceDeactivate();
			}
		}
	}

	public bool IsInDevelopingState => _developingStateFinishCampaignTime.IsFuture;

	public bool IsInFinalizingState => _finalizingStateStartCampaignTime.IsPast;

	public bool IsReadyToBeFinalized => (_finalizingStateStartCampaignTime + NavalDLCManager.Instance.GameModels.MapStormModel.GetFinalizingStateDurationOfStorm(this)).IsPast;

	public bool IsVisuallyDirty { get; private set; }

	public float EffectRadius => NavalDLCManager.Instance.GameModels.MapStormModel.GetEffectRadiusOfStorm(this);

	public float EyeRadius => NavalDLCManager.Instance.GameModels.MapStormModel.GetEyeRadiusOfStorm(this);

	public Storm(Vec2 initialPosition, StormTypes stormType)
	{
		StormType = stormType;
		CurrentPosition = initialPosition;
		Intensity = 0.5f;
		_speed = NavalDLCManager.Instance.GameModels.MapStormModel.GetSpeedOfStorm(this);
		_developingStateFinishCampaignTime = CampaignTime.Now + NavalDLCManager.Instance.GameModels.MapStormModel.GetDevelopingStateDurationOfStorm(this);
		NavalDLCManager.Instance.GameModels.MapStormModel.GetStormLifeSpan(out var minimumDuration, out var maximumDuration);
		CampaignTime campaignTime = minimumDuration + CampaignTime.Days(MBRandom.RandomInt(0, (int)maximumDuration.ToDays));
		_finalizingStateStartCampaignTime = _developingStateFinishCampaignTime + campaignTime;
		ChangeMoveDirection();
		SetVisualDirty();
	}

	public void ForceDeactivate()
	{
		_finalizingStateStartCampaignTime = CampaignTime.Now;
		SetVisualDirty();
	}

	public void SetVisualDirty()
	{
		IsVisuallyDirty = true;
	}

	public void OnVisualUpdated()
	{
		IsVisuallyDirty = false;
	}

	public bool HasWetWeatherEffectAtPosition(Vec2 pos)
	{
		if (CurrentPosition.DistanceSquared(pos) < EffectRadius * EffectRadius * 1.2f)
		{
			return true;
		}
		int num = Math.Min(_nextUpdatePreviousDataArrayIndex, 6);
		for (int i = 0; i < num; i++)
		{
			PreviousData previousData = _previousPositionsAndRadius[i];
			if (previousData.Position.DistanceSquared(pos) < previousData.EffectRadius * previousData.EffectRadius * 1.2f)
			{
				return true;
			}
		}
		return false;
	}

	public void HourlyTick()
	{
		if (IsActive && _nextUpdateTime.IsPast)
		{
			_previousPositionsAndRadius[_nextUpdatePreviousDataArrayIndex] = new PreviousData(CurrentPosition, EffectRadius);
			_nextUpdatePreviousDataArrayIndex = (_nextUpdatePreviousDataArrayIndex + 1) % _previousPositionsAndRadius.Length;
			_nextUpdateTime = CampaignTime.HoursFromNow(4f);
		}
	}

	public void Tick(float dt)
	{
		if (IsActive && !NavalDLCManager.Instance.StormManager.DebugVisualsStopped)
		{
			_currentMoveDirection = Vec2.Lerp(_currentMoveDirection, _desiredMoveDirection, dt);
			CurrentPosition += _currentMoveDirection * dt * _speed;
		}
	}

	public void OnAfterLoad()
	{
		_speed = NavalDLCManager.Instance.GameModels.MapStormModel.GetSpeedOfStorm(this);
		SetVisualDirty();
	}

	public void ChangeMoveDirection()
	{
		_desiredMoveDirection = Campaign.Current.Models.MapWeatherModel.GetWindForPosition(new CampaignVec2(CurrentPosition, isOnLand: false));
		float num = MBRandom.RandomFloatNormal * 30f;
		_desiredMoveDirection.RotateCCW(num * (System.MathF.PI / 180f));
		_desiredMoveDirection.Normalize();
	}

	private bool IsPositionOutOfMapBoundary(Vec2 position)
	{
		Campaign.Current.MapSceneWrapper.GetMapBorders(out var minimumPosition, out var maximumPosition, out var _);
		if (position.X < minimumPosition.X || position.X > maximumPosition.X || position.Y < minimumPosition.Y || position.Y > maximumPosition.Y)
		{
			return true;
		}
		return false;
	}

	[Conditional("DEBUG")]
	private void DebugVisualTick()
	{
		if (NavalDLCManager.Instance.StormManager.DebugVisualsEnabled)
		{
			_ = IsActive;
			new Vec3(CurrentPosition, 5f);
			PreviousData[] previousPositionsAndRadius = _previousPositionsAndRadius;
			for (int i = 0; i < previousPositionsAndRadius.Length; i++)
			{
				_ = ref previousPositionsAndRadius[i];
			}
		}
	}
}
