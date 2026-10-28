using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Multiplayer.Lobby;

public class MultiplayerLobbyCosmeticAnimationPartWidget : Widget
{
	private float _alphaChangeDuration;

	private float _minAlpha;

	private float _maxAlpha;

	private float _currentAlpha;

	private float _targetAlpha;

	private float _alphaChangeTimeElapsed;

	private bool _isAnimationPlaying;

	public MultiplayerLobbyCosmeticAnimationPartWidget(UIContext context)
		: base(context)
	{
		StopAnimation();
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		if (_isAnimationPlaying)
		{
			if (_alphaChangeTimeElapsed >= _alphaChangeDuration)
			{
				InvertAnimationDirection();
				InitializeAnimationParameters();
			}
			_currentAlpha = MathF.Lerp(_currentAlpha, _targetAlpha, _alphaChangeTimeElapsed / _alphaChangeDuration);
			base.AlphaFactor = _currentAlpha;
			_alphaChangeTimeElapsed += dt;
		}
	}

	public void InitializeAnimationParameters()
	{
		_currentAlpha = _minAlpha;
		_targetAlpha = _maxAlpha;
		_alphaChangeTimeElapsed = 0f;
		base.AlphaFactor = _currentAlpha;
	}

	private void InvertAnimationDirection()
	{
		float minAlpha = _minAlpha;
		_minAlpha = _maxAlpha;
		_maxAlpha = minAlpha;
	}

	public void StartAnimation(float alphaChangeDuration, float minAlpha, float maxAlpha)
	{
		_alphaChangeDuration = alphaChangeDuration;
		_minAlpha = minAlpha;
		_maxAlpha = maxAlpha;
		InitializeAnimationParameters();
		_isAnimationPlaying = true;
		base.IsVisible = true;
	}

	public void StopAnimation()
	{
		InitializeAnimationParameters();
		_isAnimationPlaying = false;
		base.IsVisible = false;
	}
}
