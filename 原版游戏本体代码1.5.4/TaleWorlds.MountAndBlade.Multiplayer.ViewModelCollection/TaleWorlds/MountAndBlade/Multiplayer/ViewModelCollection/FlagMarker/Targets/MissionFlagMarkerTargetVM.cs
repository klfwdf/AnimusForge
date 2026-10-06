using System;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.Objects;

namespace TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.FlagMarker.Targets;

public class MissionFlagMarkerTargetVM : MissionMarkerTargetVM
{
	private bool _isKeepFlag;

	private bool _isSpawnAffectorFlag;

	private float _flagProgress;

	private int _remainingRemovalTime = -1;

	public FlagCapturePoint TargetFlag { get; private set; }

	public override Vec3 WorldPosition
	{
		get
		{
			if (TargetFlag != null)
			{
				return TargetFlag.Position;
			}
			Debug.FailedAssert("No target found!", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection\\FlagMarker\\Targets\\MissionFlagMarkerTargetVM.cs", "WorldPosition", 24);
			return Vec3.One;
		}
	}

	protected override float HeightOffset => 2f;

	[DataSourceProperty]
	public float FlagProgress
	{
		get
		{
			return _flagProgress;
		}
		set
		{
			if (value != _flagProgress)
			{
				_flagProgress = value;
				OnPropertyChangedWithValue(value, "FlagProgress");
			}
		}
	}

	[DataSourceProperty]
	public bool IsSpawnAffectorFlag
	{
		get
		{
			return _isSpawnAffectorFlag;
		}
		set
		{
			if (value != _isSpawnAffectorFlag)
			{
				_isSpawnAffectorFlag = value;
				OnPropertyChangedWithValue(value, "IsSpawnAffectorFlag");
			}
		}
	}

	[DataSourceProperty]
	public int RemainingRemovalTime
	{
		get
		{
			return _remainingRemovalTime;
		}
		set
		{
			if (value != _remainingRemovalTime)
			{
				_remainingRemovalTime = value;
				OnPropertyChangedWithValue(value, "RemainingRemovalTime");
			}
		}
	}

	[DataSourceProperty]
	public bool IsKeepFlag
	{
		get
		{
			return _isKeepFlag;
		}
		set
		{
			if (value != _isKeepFlag)
			{
				_isKeepFlag = value;
				OnPropertyChangedWithValue(value, "IsKeepFlag");
			}
		}
	}

	public MissionFlagMarkerTargetVM(FlagCapturePoint flag)
		: base(MissionMarkerType.Flag)
	{
		TargetFlag = flag;
		base.Name = Convert.ToChar(flag.FlagChar).ToString();
		string[] tags = TargetFlag.GameEntity.Tags;
		foreach (string text in tags)
		{
			if (text.StartsWith("enable_") || text.StartsWith("disable_"))
			{
				IsSpawnAffectorFlag = true;
			}
		}
		if (TargetFlag.GameEntity.HasTag("keep_capture_point"))
		{
			IsKeepFlag = true;
		}
		OnOwnerChanged(null);
	}

	private Vec3 Vector3Maxamize(Vec3 vector)
	{
		Vec3 vec = vector;
		float num = 0f;
		num = ((vector.x > num) ? vector.x : num);
		num = ((vector.y > num) ? vector.y : num);
		num = ((vector.z > num) ? vector.z : num);
		return vec / num;
	}

	public override void UpdateScreenPosition(Camera missionCamera)
	{
		Vec3 worldPoint = WorldPosition;
		worldPoint.z += HeightOffset;
		Vec3 vector = missionCamera.WorldPointToViewPortPoint(ref worldPoint);
		vector.y = 1f - vector.y;
		if (vector.z < 0f)
		{
			vector.x = 1f - vector.x;
			vector.y = 1f - vector.y;
			vector.z = 0f;
			vector = Vector3Maxamize(vector);
		}
		if (float.IsPositiveInfinity(vector.x))
		{
			vector.x = 1f;
		}
		else if (float.IsNegativeInfinity(vector.x))
		{
			vector.x = 0f;
		}
		if (float.IsPositiveInfinity(vector.y))
		{
			vector.y = 1f;
		}
		else if (float.IsNegativeInfinity(vector.y))
		{
			vector.y = 0f;
		}
		vector.x = TaleWorlds.Library.MathF.Clamp(vector.x, 0f, 1f) * Screen.RealScreenResolutionWidth;
		vector.y = TaleWorlds.Library.MathF.Clamp(vector.y, 0f, 1f) * Screen.RealScreenResolutionHeight;
		base.ScreenPosition = new Vec2(vector.x, vector.y);
		FlagProgress = TargetFlag.GetFlagProgress();
	}

	public void OnOwnerChanged(Team team)
	{
		int num;
		int num2;
		if (team != null)
		{
			num = ((team.TeamIndex == -1) ? 1 : 0);
			if (num == 0)
			{
				num2 = (int)team.Color;
				goto IL_001f;
			}
		}
		else
		{
			num = 1;
		}
		num2 = -10855846;
		goto IL_001f;
		IL_001f:
		uint color = (uint)num2;
		uint color2 = ((num != 0) ? uint.MaxValue : team.Color2);
		RefreshColor(color, color2);
	}

	public void OnRemainingMoraleChanged(int remainingMorale)
	{
		if (RemainingRemovalTime != remainingMorale && remainingMorale != 90)
		{
			RemainingRemovalTime = (int)((float)remainingMorale / 1f);
		}
	}
}
