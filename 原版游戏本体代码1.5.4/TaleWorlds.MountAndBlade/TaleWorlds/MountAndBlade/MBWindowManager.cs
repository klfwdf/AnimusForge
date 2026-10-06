using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.ScreenSystem;

namespace TaleWorlds.MountAndBlade;

public class MBWindowManager
{
	public static float WorldToScreen(Camera camera, Vec3 worldSpacePosition, ref float screenX, ref float screenY, ref float w)
	{
		return MBAPI.IMBWindowManager.WorldToScreen(camera.Pointer, worldSpacePosition, ref screenX, ref screenY, ref w);
	}

	public static float WorldToScreenInsideUsableArea(Camera camera, Vec3 worldSpacePosition, ref float screenX, ref float screenY, ref float w)
	{
		float result = MBAPI.IMBWindowManager.WorldToScreen(camera.Pointer, worldSpacePosition, ref screenX, ref screenY, ref w);
		screenX -= (Screen.RealScreenResolutionWidth - ScreenManager.UsableArea.X * Screen.RealScreenResolutionWidth) / 2f;
		screenY -= (Screen.RealScreenResolutionHeight - ScreenManager.UsableArea.Y * Screen.RealScreenResolutionHeight) / 2f;
		return result;
	}

	public static float WorldToScreenWithFixedZ(Camera camera, Vec3 cameraPosition, Vec3 worldSpacePosition, ref float screenX, ref float screenY, ref float w)
	{
		return MBAPI.IMBWindowManager.WorldToScreenWithFixedZ(camera.Pointer, cameraPosition, worldSpacePosition, ref screenX, ref screenY, ref w);
	}

	public static void ScreenToWorld(Camera camera, float screenX, float screenY, float w, ref Vec3 worldSpacePosition)
	{
		MBAPI.IMBWindowManager.ScreenToWorld(camera.Pointer, screenX, screenY, w, ref worldSpacePosition);
	}

	public static Vec2 GetScreenResolution()
	{
		return MBAPI.IMBWindowManager.GetScreenResolution();
	}

	public static void PreDisplay()
	{
		MBAPI.IMBWindowManager.PreDisplay();
	}

	public static void DontChangeCursorPos()
	{
		MBAPI.IMBWindowManager.DontChangeCursorPos();
	}
}
