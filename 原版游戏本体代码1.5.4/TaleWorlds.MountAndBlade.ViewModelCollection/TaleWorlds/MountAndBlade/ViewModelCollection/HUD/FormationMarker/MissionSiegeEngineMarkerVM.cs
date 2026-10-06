using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.ViewModelCollection.HUD.FormationMarker;

public class MissionSiegeEngineMarkerVM : ViewModel
{
	public class SiegeEngineMarkerDistanceComparer : IComparer<MissionSiegeEngineMarkerTargetVM>
	{
		public int Compare(MissionSiegeEngineMarkerTargetVM x, MissionSiegeEngineMarkerTargetVM y)
		{
			return y.Distance.CompareTo(x.Distance);
		}
	}

	private Mission _mission;

	private Camera _missionCamera;

	private List<SiegeWeapon> _siegeEngines;

	private Vec3 _heightOffset = new Vec3(0f, 0f, 3f);

	private bool _prevIsEnabled;

	private SiegeEngineMarkerDistanceComparer _comparer;

	private bool _fadeOutTimerStarted;

	private float _fadeOutTimer;

	private bool _isEnabled;

	private MBBindingList<MissionSiegeEngineMarkerTargetVM> _targets;

	public bool IsInitialized { get; private set; }

	[DataSourceProperty]
	public bool IsEnabled
	{
		get
		{
			return _isEnabled;
		}
		set
		{
			if (value != _isEnabled)
			{
				_isEnabled = value;
				OnPropertyChangedWithValue(value, "IsEnabled");
				UpdateTargetStates(value);
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<MissionSiegeEngineMarkerTargetVM> Targets
	{
		get
		{
			return _targets;
		}
		set
		{
			if (value != _targets)
			{
				_targets = value;
				OnPropertyChangedWithValue(value, "Targets");
			}
		}
	}

	public MissionSiegeEngineMarkerVM(Mission mission, Camera missionCamera)
	{
		_mission = mission;
		_missionCamera = missionCamera;
		_comparer = new SiegeEngineMarkerDistanceComparer();
		Targets = new MBBindingList<MissionSiegeEngineMarkerTargetVM>();
	}

	public void InitializeWith(List<SiegeWeapon> siegeEngines)
	{
		_siegeEngines = siegeEngines;
		for (int i = 0; i < _siegeEngines.Count; i++)
		{
			SiegeWeapon engine = _siegeEngines[i];
			BattleSideEnum side = _mission.PlayerTeam.Side;
			if (!Targets.Any((MissionSiegeEngineMarkerTargetVM t) => t.Engine == engine))
			{
				MissionSiegeEngineMarkerTargetVM missionSiegeEngineMarkerTargetVM = new MissionSiegeEngineMarkerTargetVM(engine, engine.Side != side);
				Targets.Add(missionSiegeEngineMarkerTargetVM);
				missionSiegeEngineMarkerTargetVM.IsEnabled = IsEnabled;
			}
		}
		IsInitialized = true;
	}

	public void Tick(float dt)
	{
		if (_siegeEngines == null)
		{
			return;
		}
		if (IsEnabled)
		{
			RefreshSiegeEngineList();
			RefreshSiegeEnginePositions();
			RefreshSiegeEngineItemProperties();
			SortMarkersInList();
			_fadeOutTimerStarted = false;
			_fadeOutTimer = 0f;
			_prevIsEnabled = IsEnabled;
		}
		else
		{
			if (_prevIsEnabled)
			{
				_fadeOutTimerStarted = true;
			}
			if (_fadeOutTimerStarted)
			{
				_fadeOutTimer += dt;
			}
			if (_fadeOutTimer < 2f)
			{
				RefreshSiegeEnginePositions();
			}
			else
			{
				_fadeOutTimerStarted = false;
			}
		}
		_prevIsEnabled = IsEnabled;
	}

	private void RefreshSiegeEngineList()
	{
		_ = _mission.PlayerTeam.IsDefender;
		for (int num = _siegeEngines.Count - 1; num >= 0; num--)
		{
			SiegeWeapon engine = _siegeEngines[num];
			if (engine.DestructionComponent.IsDestroyed)
			{
				_siegeEngines.RemoveAt(num);
				MissionSiegeEngineMarkerTargetVM item = Targets.SingleOrDefault((MissionSiegeEngineMarkerTargetVM t) => t.Engine == engine);
				Targets.Remove(item);
			}
		}
	}

	private void RefreshSiegeEnginePositions()
	{
		foreach (MissionSiegeEngineMarkerTargetVM target in Targets)
		{
			float screenX = 0f;
			float screenY = 0f;
			float w = 0f;
			Vec3 globalPosition = target.Engine.GameEntity.GlobalPosition;
			MBWindowManager.WorldToScreenInsideUsableArea(_missionCamera, globalPosition + _heightOffset, ref screenX, ref screenY, ref w);
			if (w < 0f || !MathF.IsValidValue(screenX) || !MathF.IsValidValue(screenY))
			{
				screenX = -10000f;
				screenY = -10000f;
				w = 0f;
			}
			if (_prevIsEnabled && IsEnabled)
			{
				target.ScreenPosition = Vec2.Lerp(target.ScreenPosition, new Vec2(screenX, screenY), 0.9f);
			}
			else
			{
				target.ScreenPosition = new Vec2(screenX, screenY);
			}
			Agent main = Agent.Main;
			target.Distance = ((main != null && main.IsActive()) ? Agent.Main.Position.Distance(globalPosition) : w);
		}
	}

	private void SortMarkersInList()
	{
		Targets.Sort(_comparer);
	}

	private void RefreshSiegeEngineItemProperties()
	{
		foreach (MissionSiegeEngineMarkerTargetVM target in Targets)
		{
			target.Refresh();
		}
	}

	private void UpdateTargetStates(bool isEnabled)
	{
		foreach (MissionSiegeEngineMarkerTargetVM target in Targets)
		{
			target.IsEnabled = isEnabled;
		}
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
		_siegeEngines?.Clear();
		_siegeEngines = null;
	}
}
