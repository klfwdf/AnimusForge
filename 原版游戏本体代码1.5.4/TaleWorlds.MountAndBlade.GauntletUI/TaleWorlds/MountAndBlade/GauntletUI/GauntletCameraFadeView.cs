using TaleWorlds.Core.ViewModelCollection.Generic;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.Library;
using TaleWorlds.ScreenSystem;

namespace TaleWorlds.MountAndBlade.GauntletUI;

public class GauntletCameraFadeView : GlobalLayer, IScreenFadeHandler
{
	private float _fadeAlpha;

	private ScreenFadeController.ScreenFadeState _fadeState;

	private float _currentStateTimer;

	private float _currentStateBeginAlpha;

	private float _fadeOutDuration;

	private float _blackOutDuration;

	private float _fadeInDuration;

	private bool _autoFadeIn;

	private static bool _isInitialized;

	private readonly GauntletLayer _gauntletLayer;

	private readonly BindingListFloatItem _dataSource;

	public GauntletCameraFadeView()
	{
		_dataSource = new BindingListFloatItem(_fadeAlpha);
		_gauntletLayer = new GauntletLayer("CameraFade", 100000);
		_gauntletLayer.LoadMovie("CameraFade", _dataSource);
		base.Layer = _gauntletLayer;
	}

	public static void Initialize()
	{
		if (!_isInitialized)
		{
			GauntletCameraFadeView gauntletCameraFadeView = new GauntletCameraFadeView();
			ScreenManager.AddGlobalLayer(gauntletCameraFadeView, isFocusable: false);
			ScreenFadeController.RegisterHandler(gauntletCameraFadeView);
			_isInitialized = true;
		}
	}

	protected override void OnTick(float dt)
	{
		base.OnTick(dt);
		switch (_fadeState)
		{
		case ScreenFadeController.ScreenFadeState.None:
			_fadeAlpha = 0f;
			break;
		case ScreenFadeController.ScreenFadeState.FadingOut:
			_currentStateTimer += dt;
			_fadeAlpha = MathF.Lerp(_currentStateBeginAlpha, 1f, MathF.Min(_currentStateTimer / _fadeOutDuration, 1f));
			if (_currentStateTimer > _fadeOutDuration)
			{
				SetFadeState(ScreenFadeController.ScreenFadeState.FadedOut);
			}
			break;
		case ScreenFadeController.ScreenFadeState.FadedOut:
			_fadeAlpha = 1f;
			if (_autoFadeIn)
			{
				_currentStateTimer += dt;
				if (_currentStateTimer > _blackOutDuration)
				{
					SetFadeState(ScreenFadeController.ScreenFadeState.FadingIn);
				}
			}
			break;
		case ScreenFadeController.ScreenFadeState.FadingIn:
			_currentStateTimer += dt;
			_fadeAlpha = MathF.Lerp(_currentStateBeginAlpha, 0f, MathF.Min(_currentStateTimer / _fadeInDuration, 1f));
			if (_currentStateTimer > _fadeInDuration)
			{
				SetFadeState(ScreenFadeController.ScreenFadeState.None);
			}
			break;
		}
		_dataSource.Item = _fadeAlpha;
	}

	public void BeginFadeOutAndIn(float fadeOutDuration = 0.5f, float blackOutDuration = 0.5f, float fadeInDuration = 0.5f)
	{
		_fadeOutDuration = MathF.Max(fadeOutDuration, 0f);
		_blackOutDuration = MathF.Max(blackOutDuration, 0f);
		_fadeInDuration = MathF.Max(fadeInDuration, 0f);
		_autoFadeIn = true;
		SetFadeState(ScreenFadeController.ScreenFadeState.FadingOut);
	}

	public void BeginFadeOut(float fadeOutDuration = 0.5f)
	{
		_fadeOutDuration = MathF.Max(fadeOutDuration, 0f);
		_autoFadeIn = false;
		SetFadeState(ScreenFadeController.ScreenFadeState.FadingOut);
	}

	public void BeginFadeIn(float fadeInDuration = 0.5f)
	{
		_fadeInDuration = MathF.Max(fadeInDuration, 0f);
		if (_fadeState == ScreenFadeController.ScreenFadeState.FadingOut || _fadeState == ScreenFadeController.ScreenFadeState.FadedOut)
		{
			SetFadeState(ScreenFadeController.ScreenFadeState.FadingIn);
		}
	}

	private void SetFadeState(ScreenFadeController.ScreenFadeState fadeState)
	{
		if (_fadeState != fadeState)
		{
			_fadeState = fadeState;
			_currentStateTimer = 0f;
			_currentStateBeginAlpha = _fadeAlpha;
		}
	}

	public ScreenFadeController.ScreenFadeState GetScreenFadeState()
	{
		return _fadeState;
	}
}
