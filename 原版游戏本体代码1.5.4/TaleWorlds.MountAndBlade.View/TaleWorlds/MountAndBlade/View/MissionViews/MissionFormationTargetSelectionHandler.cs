using System;
using System.Collections.Generic;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.View.MissionViews;

public class MissionFormationTargetSelectionHandler : MissionView
{
	public enum FormationMarkerVisibility
	{
		NotEvaluated,
		Hidden,
		Visible
	}

	public struct VisibilityConfig
	{
		public MultiplayerOptions.FormationTargetingVisibilityModes Mode;

		public int Threshold;

		public bool AppliesAtCloseRange;
	}

	public const float MaxDistanceForFocusCheck = 1000f;

	public readonly float MaxDistanceToCenterForFocus = 70f * (Screen.RealScreenResolutionHeight / 1080f);

	private const int MaxSampledUnitsPerFormation = 24;

	private const float VisibilityResultCacheDuration = 0.2f;

	private readonly List<(Formation Formation, float DistanceToScreenCenter)> _distanceCache = new List<(Formation, float)>();

	private Formation _focusedFormation;

	private Vec2 _centerOfScreen = new Vec2(Screen.RealScreenResolutionWidth / 2f, Screen.RealScreenResolutionHeight / 2f);

	private VisibilityConfig _visibilityConfig;

	private readonly Dictionary<Formation, (bool IsVisible, float VisibilityRatio, float ComputedAtTime)> _visibilityResultCache;

	private readonly Dictionary<Formation, bool> _markerVisibilityCache;

	private float _elapsedTime;

	private bool _isTargetingDisabled;

	private readonly List<Formation> _expiredVisibilityKeys;

	public static float MinDistanceForFocusCheck
	{
		get
		{
			if (!GameNetwork.IsMultiplayer)
			{
				return 8f;
			}
			return 4.8f;
		}
	}

	private Camera ActiveCamera => base.MissionScreen.CustomCamera ?? base.MissionScreen.CombatCamera;

	public event Action<Formation> OnFormationFocused;

	public MissionFormationTargetSelectionHandler()
	{
		_visibilityResultCache = new Dictionary<Formation, (bool, float, float)>();
		_markerVisibilityCache = new Dictionary<Formation, bool>();
		_expiredVisibilityKeys = new List<Formation>();
		_visibilityConfig = new VisibilityConfig
		{
			Mode = MultiplayerOptions.FormationTargetingVisibilityModes.Disabled,
			Threshold = 0,
			AppliesAtCloseRange = false
		};
	}

	public void SetVisibilityConfig(VisibilityConfig config)
	{
		_visibilityConfig = config;
		_visibilityResultCache.Clear();
		_markerVisibilityCache.Clear();
	}

	public override void OnPreDisplayMissionTick(float dt)
	{
		base.OnPreDisplayMissionTick(dt);
		_distanceCache.Clear();
		_focusedFormation = null;
		_markerVisibilityCache.Clear();
		_elapsedTime += dt;
		PruneExpiredVisibilityResults();
		if (base.Mission?.Teams == null)
		{
			return;
		}
		if (!_isTargetingDisabled)
		{
			Vec3 position = ActiveCamera.Position;
			_centerOfScreen.x = Screen.RealScreenResolutionWidth / 2f;
			_centerOfScreen.y = Screen.RealScreenResolutionHeight / 2f;
			bool flag = _visibilityConfig.Mode != MultiplayerOptions.FormationTargetingVisibilityModes.Disabled;
			MatrixFrame viewProj = MatrixFrame.Identity;
			Vec3 cameraOrigin = Vec3.Zero;
			if (flag)
			{
				ActiveCamera.GetViewProjMatrix(ref viewProj);
				cameraOrigin = ActiveCamera.Position;
			}
			for (int i = 0; i < base.Mission.Teams.Count; i++)
			{
				Team team = base.Mission.Teams[i];
				if (team.IsPlayerAlly)
				{
					continue;
				}
				for (int j = 0; j < team.FormationsIncludingEmpty.Count; j++)
				{
					Formation formation = team.FormationsIncludingEmpty[j];
					if (formation.CountOfUnits > 0)
					{
						TryGetFormationDistanceToCenter(formation, position, out var isFormationFocusable, out var distanceToScreenCenter);
						if (isFormationFocusable)
						{
							_distanceCache.Add((formation, distanceToScreenCenter));
						}
						if (flag)
						{
							bool flag2 = formation.CachedMedianPosition.AsVec2.Distance(position.AsVec2) < 1000f;
							_markerVisibilityCache[formation] = flag2 && IsFormationVisibleEnough(formation, ref viewProj, cameraOrigin);
						}
					}
				}
			}
		}
		if (_distanceCache.Count == 0)
		{
			this.OnFormationFocused?.Invoke(null);
			return;
		}
		_distanceCache.Sort(CompareByDistanceToScreenCenter);
		MatrixFrame viewProj2 = MatrixFrame.Identity;
		Vec3 cameraOrigin2 = Vec3.Zero;
		bool flag3 = _visibilityConfig.Mode != MultiplayerOptions.FormationTargetingVisibilityModes.Disabled;
		if (flag3)
		{
			ActiveCamera.GetViewProjMatrix(ref viewProj2);
			cameraOrigin2 = ActiveCamera.Position;
		}
		for (int k = 0; k < _distanceCache.Count; k++)
		{
			(Formation Formation, float DistanceToScreenCenter) tuple = _distanceCache[k];
			var (formation2, _) = tuple;
			if (tuple.DistanceToScreenCenter >= MaxDistanceToCenterForFocus)
			{
				break;
			}
			if (!flag3 || IsFormationVisibleEnough(formation2, ref viewProj2, cameraOrigin2))
			{
				_focusedFormation = formation2;
				break;
			}
		}
		this.OnFormationFocused?.Invoke(_focusedFormation);
	}

	private static int CompareByDistanceToScreenCenter((Formation, float) a, (Formation, float) b)
	{
		return a.Item2.CompareTo(b.Item2);
	}

	private void TryGetFormationDistanceToCenter(Formation formation, Vec3 cameraPosition, out bool isFormationFocusable, out float distanceToScreenCenter)
	{
		WorldPosition cachedMedianPosition = formation.CachedMedianPosition;
		float num = cachedMedianPosition.AsVec2.Distance(cameraPosition.AsVec2);
		float screenX = 0f;
		float screenY = 0f;
		float w = 0f;
		MBWindowManager.WorldToScreenInsideUsableArea(ActiveCamera, cachedMedianPosition.GetGroundVec3() + new Vec3(0f, 0f, 3f), ref screenX, ref screenY, ref w);
		bool flag = w <= 0f;
		if (num >= 1000f || num <= MinDistanceForFocusCheck || flag)
		{
			distanceToScreenCenter = 2.1474836E+09f;
			isFormationFocusable = false;
		}
		else
		{
			isFormationFocusable = true;
			distanceToScreenCenter = new Vec2(screenX, screenY).Distance(_centerOfScreen);
		}
	}

	private bool IsFormationVisibleEnough(Formation formation, ref MatrixFrame viewProjection, Vec3 cameraOrigin)
	{
		if (_visibilityResultCache.TryGetValue(formation, out (bool, float, float) value) && _elapsedTime - value.Item3 < 0.2f)
		{
			return value.Item1;
		}
		float visibilityRatio;
		bool flag = ComputeFormationVisibility(formation, ref viewProjection, cameraOrigin, out visibilityRatio);
		_visibilityResultCache[formation] = (flag, visibilityRatio, _elapsedTime);
		return flag;
	}

	public float GetFormationVisibilityRatio(Formation formation)
	{
		if (_visibilityResultCache.TryGetValue(formation, out (bool, float, float) value) && _elapsedTime - value.Item3 < 0.2f)
		{
			return value.Item2;
		}
		return 1f;
	}

	public FormationMarkerVisibility GetFormationMarkerVisibility(Formation formation)
	{
		if (_isTargetingDisabled || _visibilityConfig.Mode == MultiplayerOptions.FormationTargetingVisibilityModes.Disabled)
		{
			return FormationMarkerVisibility.NotEvaluated;
		}
		if (_markerVisibilityCache.TryGetValue(formation, out var value))
		{
			if (!value)
			{
				return FormationMarkerVisibility.Hidden;
			}
			return FormationMarkerVisibility.Visible;
		}
		return FormationMarkerVisibility.NotEvaluated;
	}

	private void PruneExpiredVisibilityResults()
	{
		if (_visibilityResultCache.Count == 0)
		{
			return;
		}
		_expiredVisibilityKeys.Clear();
		foreach (KeyValuePair<Formation, (bool, float, float)> item in _visibilityResultCache)
		{
			if (_elapsedTime - item.Value.Item3 >= 0.2f)
			{
				_expiredVisibilityKeys.Add(item.Key);
			}
		}
		for (int i = 0; i < _expiredVisibilityKeys.Count; i++)
		{
			_visibilityResultCache.Remove(_expiredVisibilityKeys[i]);
		}
		_expiredVisibilityKeys.Clear();
	}

	private bool ComputeFormationVisibility(Formation formation, ref MatrixFrame viewProjection, Vec3 cameraOrigin, out float visibilityRatio)
	{
		visibilityRatio = 1f;
		int countOfUnits = formation.CountOfUnits;
		if (countOfUnits == 0)
		{
			visibilityRatio = 0f;
			return false;
		}
		MBReadOnlyList<IFormationUnit> unitsWithoutLooseDetachedOnes = formation.UnitsWithoutLooseDetachedOnes;
		MBReadOnlyList<Agent> detachedUnits = formation.DetachedUnits;
		int count = unitsWithoutLooseDetachedOnes.Count;
		int count2 = detachedUnits.Count;
		int num = count + count2;
		int num2 = ((num <= 24) ? 1 : ((num + 24 - 1) / 24));
		int num3 = 0;
		int num4 = 0;
		for (int i = 0; i < num; i += num2)
		{
			Agent agent = ((i >= count) ? detachedUnits[i - count] : ((Agent)unitsWithoutLooseDetachedOnes[i]));
			if (!agent.IsActive())
			{
				continue;
			}
			Vec3 chestGlobalPosition = agent.GetChestGlobalPosition();
			Vec3 vec = chestGlobalPosition;
			vec.w = 1f;
			Vec3 vec2 = vec * viewProjection;
			if (vec2.w <= 0f)
			{
				num3++;
				continue;
			}
			float num5 = vec2.x / vec2.w;
			float num6 = vec2.y / vec2.w;
			if (num5 < -1f || num5 > 1f || num6 < -1f || num6 > 1f)
			{
				num3++;
				continue;
			}
			if (!base.Mission.Scene.CheckPointCanSeePoint(cameraOrigin, chestGlobalPosition))
			{
				num3++;
				continue;
			}
			num3++;
			num4++;
		}
		if (num3 == 0)
		{
			visibilityRatio = 0f;
			return false;
		}
		visibilityRatio = (float)num4 / (float)num3;
		return _visibilityConfig.Mode switch
		{
			MultiplayerOptions.FormationTargetingVisibilityModes.Percentage => visibilityRatio * 100f >= (float)_visibilityConfig.Threshold, 
			MultiplayerOptions.FormationTargetingVisibilityModes.AbsoluteCount => visibilityRatio * (float)countOfUnits >= (float)_visibilityConfig.Threshold, 
			_ => true, 
		};
	}

	public void SetIsFormationTargetingDisabled(bool isDisabled)
	{
		if (_isTargetingDisabled != isDisabled)
		{
			_isTargetingDisabled = isDisabled;
			if (isDisabled)
			{
				_distanceCache.Clear();
				_focusedFormation = null;
				_visibilityResultCache.Clear();
				_markerVisibilityCache.Clear();
				this.OnFormationFocused?.Invoke(null);
			}
		}
	}

	public override void OnRemoveBehavior()
	{
		_distanceCache.Clear();
		_focusedFormation = null;
		_visibilityResultCache.Clear();
		_markerVisibilityCache.Clear();
		_expiredVisibilityKeys.Clear();
		this.OnFormationFocused = null;
		base.OnRemoveBehavior();
	}
}
