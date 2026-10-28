using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Mission.OrderOfBattle;

public class OrderOfBattleScreenWidget : Widget
{
	private float _alphaChangeTimeElapsed;

	private float _initialAlpha = 1f;

	private float _targetAlpha;

	private float _currentAlpha = 1f;

	private bool _isTransitioning;

	private bool _areCameraControlsEnabled;

	private float _cameraEnabledAlpha = 0.2f;

	private ListPanel _leftSideFormations;

	private ListPanel _rightSideFormations;

	private ListPanel _captainPool;

	private Widget _markers;

	private bool _canToggleHeroSelection;

	public float AlphaChangeDuration { get; set; } = 0.15f;

	[Editor(false)]
	public bool AreCameraControlsEnabled
	{
		get
		{
			return _areCameraControlsEnabled;
		}
		set
		{
			if (value != _areCameraControlsEnabled)
			{
				_areCameraControlsEnabled = value;
				OnPropertyChanged(value, "AreCameraControlsEnabled");
				OnCameraControlsEnabledChanged();
			}
		}
	}

	[Editor(false)]
	public float CameraEnabledAlpha
	{
		get
		{
			return _cameraEnabledAlpha;
		}
		set
		{
			if (value != _cameraEnabledAlpha)
			{
				_cameraEnabledAlpha = value;
				OnPropertyChanged(value, "CameraEnabledAlpha");
			}
		}
	}

	[Editor(false)]
	public ListPanel LeftSideFormations
	{
		get
		{
			return _leftSideFormations;
		}
		set
		{
			if (value != _leftSideFormations)
			{
				_leftSideFormations = value;
				OnPropertyChanged(value, "LeftSideFormations");
			}
		}
	}

	[Editor(false)]
	public ListPanel RightSideFormations
	{
		get
		{
			return _rightSideFormations;
		}
		set
		{
			if (value != _rightSideFormations)
			{
				_rightSideFormations = value;
				OnPropertyChanged(value, "RightSideFormations");
			}
		}
	}

	[Editor(false)]
	public ListPanel CaptainPool
	{
		get
		{
			return _captainPool;
		}
		set
		{
			if (value != _captainPool)
			{
				_captainPool = value;
				OnPropertyChanged(value, "CaptainPool");
			}
		}
	}

	[Editor(false)]
	public Widget Markers
	{
		get
		{
			return _markers;
		}
		set
		{
			if (value != _markers)
			{
				_markers = value;
				OnPropertyChanged(value, "Markers");
			}
		}
	}

	[Editor(false)]
	public bool CanToggleHeroSelection
	{
		get
		{
			return _canToggleHeroSelection;
		}
		set
		{
			if (value != _canToggleHeroSelection)
			{
				_canToggleHeroSelection = value;
				OnPropertyChanged(value, "CanToggleHeroSelection");
			}
		}
	}

	public OrderOfBattleScreenWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnLateUpdate(float dt)
	{
		CanToggleHeroSelection = base.EventManager.DraggedWidget == null;
		if (_isTransitioning)
		{
			if (_alphaChangeTimeElapsed < AlphaChangeDuration)
			{
				_currentAlpha = MathF.Lerp(_initialAlpha, _targetAlpha, _alphaChangeTimeElapsed / AlphaChangeDuration);
				LeftSideFormations?.SetGlobalAlphaRecursively(_currentAlpha);
				RightSideFormations?.SetGlobalAlphaRecursively(_currentAlpha);
				CaptainPool?.SetGlobalAlphaRecursively(_currentAlpha);
				Markers?.SetGlobalAlphaRecursively(_currentAlpha);
				_alphaChangeTimeElapsed += dt;
			}
			else
			{
				_currentAlpha = _targetAlpha;
				LeftSideFormations?.SetGlobalAlphaRecursively(_currentAlpha);
				RightSideFormations?.SetGlobalAlphaRecursively(_currentAlpha);
				CaptainPool?.SetGlobalAlphaRecursively(_currentAlpha);
				Markers?.SetGlobalAlphaRecursively(_currentAlpha);
				_isTransitioning = false;
			}
		}
	}

	protected void OnCameraControlsEnabledChanged()
	{
		_alphaChangeTimeElapsed = 0f;
		_targetAlpha = (AreCameraControlsEnabled ? CameraEnabledAlpha : 1f);
		_initialAlpha = _currentAlpha;
		_isTransitioning = true;
	}
}
