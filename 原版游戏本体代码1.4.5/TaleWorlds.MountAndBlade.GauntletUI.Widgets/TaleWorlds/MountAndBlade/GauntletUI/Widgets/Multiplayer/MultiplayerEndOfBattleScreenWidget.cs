using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Multiplayer;

public class MultiplayerEndOfBattleScreenWidget : Widget
{
	private float _initialAlpha;

	private float _targetAlpha = 1f;

	private bool _isAnimationStarted;

	private float _fadeInTimeElapsed;

	private bool _isShown;

	private float _fadeInDuration = 0.3f;

	[Editor(false)]
	public bool IsShown
	{
		get
		{
			return _isShown;
		}
		set
		{
			if (value != _isShown)
			{
				_isShown = value;
				OnPropertyChanged(value, "IsShown");
				this.SetGlobalAlphaRecursively(0f);
				_isAnimationStarted = value;
				base.IsVisible = value;
				_fadeInTimeElapsed = 0f;
			}
		}
	}

	[Editor(false)]
	public float FadeInDuration
	{
		get
		{
			return _fadeInDuration;
		}
		set
		{
			if (value != _fadeInDuration)
			{
				_fadeInDuration = value;
				OnPropertyChanged(value, "FadeInDuration");
			}
		}
	}

	public MultiplayerEndOfBattleScreenWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		if (_isAnimationStarted)
		{
			this.SetGlobalAlphaRecursively(MathF.Lerp(_initialAlpha, _targetAlpha, _fadeInTimeElapsed / FadeInDuration));
			_fadeInTimeElapsed += dt;
			if (_fadeInTimeElapsed >= FadeInDuration)
			{
				_isAnimationStarted = false;
			}
		}
	}
}
