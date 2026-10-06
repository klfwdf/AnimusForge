using TaleWorlds.Library;

namespace TaleWorlds.Engine;

public static class Screen
{
	public static float RealScreenResolutionWidth { get; private set; }

	public static float RealScreenResolutionHeight { get; private set; }

	public static Vec2 RealScreenResolution => new Vec2(RealScreenResolutionWidth, RealScreenResolutionHeight);

	public static float AspectRatio { get; private set; }

	public static Vec2 DesktopResolution { get; private set; }

	public static Vec2 ScreenScale { get; private set; }

	internal static void Update()
	{
		RealScreenResolutionWidth = EngineApplicationInterface.IScreen.GetRealScreenResolutionWidth();
		RealScreenResolutionHeight = EngineApplicationInterface.IScreen.GetRealScreenResolutionHeight();
		AspectRatio = EngineApplicationInterface.IScreen.GetAspectRatio();
		DesktopResolution = new Vec2(EngineApplicationInterface.IScreen.GetDesktopWidth(), EngineApplicationInterface.IScreen.GetDesktopHeight());
		ScreenScale = new Vec2(RealScreenResolutionWidth / DesktopResolution.x, RealScreenResolutionHeight / DesktopResolution.y);
	}

	public static bool GetMouseVisible()
	{
		return EngineApplicationInterface.IScreen.GetMouseVisible();
	}
}
