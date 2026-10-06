using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public static class ScreenFadeController
{
	public enum ScreenFadeState
	{
		None,
		FadingOut,
		FadedOut,
		FadingIn
	}

	private static IScreenFadeHandler _handler;

	public static bool IsFadeActive
	{
		get
		{
			if (_handler != null)
			{
				return _handler.GetScreenFadeState() != ScreenFadeState.None;
			}
			return false;
		}
	}

	public static bool IsFadingOut
	{
		get
		{
			if (_handler != null)
			{
				return _handler.GetScreenFadeState() == ScreenFadeState.FadingOut;
			}
			return false;
		}
	}

	public static bool IsFadingIn
	{
		get
		{
			if (_handler != null)
			{
				return _handler.GetScreenFadeState() == ScreenFadeState.FadingIn;
			}
			return false;
		}
	}

	public static bool IsFadedOut
	{
		get
		{
			if (_handler != null)
			{
				return _handler.GetScreenFadeState() == ScreenFadeState.FadedOut;
			}
			return false;
		}
	}

	public static void RegisterHandler(IScreenFadeHandler handler)
	{
		if (_handler == null)
		{
			_handler = handler;
		}
		else
		{
			Debug.FailedAssert("ScreenFade handler already registered!", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade\\ScreenFadeController.cs", "RegisterHandler", 30);
		}
	}

	public static void BeginFadeOutAndIn(float fadeOutDuration = 0.5f, float blackOutDuration = 0.5f, float fadeInDuration = 0.5f)
	{
		_handler?.BeginFadeOutAndIn(fadeOutDuration, blackOutDuration, fadeInDuration);
	}

	public static void BeginFadeOut(float fadeOutDuration = 0.5f)
	{
		_handler?.BeginFadeOut(fadeOutDuration);
	}

	public static void BeginFadeIn(float fadeInDuration = 0.5f)
	{
		_handler?.BeginFadeIn(fadeInDuration);
	}
}
