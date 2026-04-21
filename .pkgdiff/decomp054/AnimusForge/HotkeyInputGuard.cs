using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.ScreenSystem;

namespace AnimusForge;

internal static class HotkeyInputGuard
{
	public static bool IsTextInputFocused()
	{
		if (DevHistoryEditPopup.IsOpen)
		{
			return true;
		}
		try
		{
			if (InformationManager.IsAnyInquiryActive())
			{
				return true;
			}
		}
		catch
		{
		}
		try
		{
			if (Input.IsOnScreenKeyboardActive)
			{
				return true;
			}
		}
		catch
		{
		}
		try
		{
			ScreenLayer focusedLayer = ScreenManager.FocusedLayer;
			if (focusedLayer != null && focusedLayer.IsFocusedOnInput())
			{
				return true;
			}
		}
		catch
		{
		}
		try
		{
			ScreenBase topScreen = ScreenManager.TopScreen;
			GauntletLayer val = ((topScreen != null) ? topScreen.FindLayer<GauntletLayer>() : null);
			if (val != null && ((ScreenLayer)val).IsFocusedOnInput())
			{
				return true;
			}
		}
		catch
		{
		}
		return false;
	}
}
