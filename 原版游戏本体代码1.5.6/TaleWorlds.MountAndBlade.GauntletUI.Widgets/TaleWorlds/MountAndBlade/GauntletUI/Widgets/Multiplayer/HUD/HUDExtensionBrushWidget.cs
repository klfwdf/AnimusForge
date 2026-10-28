using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Multiplayer.HUD;

public class HUDExtensionBrushWidget : BrushWidget
{
	private float _alphaChangeTimeElapsed;

	private float _initialAlpha = 1f;

	private float _targetAlpha = 1f;

	private float _currentAlpha = 1f;

	private bool _isOrderActive;

	public float AlphaChangeDuration { get; set; } = 0.15f;

	public float OrderEnabledAlpha { get; set; } = 0.3f;

	[Editor(false)]
	public bool IsOrderActive
	{
		get
		{
			return _isOrderActive;
		}
		set
		{
			if (_isOrderActive != value)
			{
				_isOrderActive = value;
				OnPropertyChanged(value, "IsOrderActive");
				OnIsOrderEnabledChanged();
			}
		}
	}

	public HUDExtensionBrushWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		if (_currentAlpha - _targetAlpha > float.Epsilon)
		{
			if (_alphaChangeTimeElapsed < AlphaChangeDuration)
			{
				_currentAlpha = MathF.Lerp(_initialAlpha, _targetAlpha, _alphaChangeTimeElapsed / AlphaChangeDuration);
				this.SetGlobalAlphaRecursively(_currentAlpha);
				_alphaChangeTimeElapsed += dt;
			}
		}
		else if (_currentAlpha != _targetAlpha)
		{
			_currentAlpha = _targetAlpha;
			this.SetGlobalAlphaRecursively(_targetAlpha);
		}
	}

	private void OnIsOrderEnabledChanged()
	{
		_alphaChangeTimeElapsed = 0f;
		_targetAlpha = (IsOrderActive ? OrderEnabledAlpha : 1f);
		_initialAlpha = _currentAlpha;
	}
}
