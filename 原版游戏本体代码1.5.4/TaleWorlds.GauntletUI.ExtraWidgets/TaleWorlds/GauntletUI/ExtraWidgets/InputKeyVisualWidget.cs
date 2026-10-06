using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.InputSystem;

namespace TaleWorlds.GauntletUI.ExtraWidgets;

public class InputKeyVisualWidget : Widget
{
	private string _visualName = "None";

	private string _keyID;

	private string _iconsPath = "General\\InputKeys";

	public string KeyID
	{
		get
		{
			return _keyID;
		}
		set
		{
			if (value != _keyID)
			{
				_keyID = value;
				_visualName = GetKeyVisualName(value);
				SetKeyVisual(_visualName);
			}
		}
	}

	public string IconsPath
	{
		get
		{
			return _iconsPath;
		}
		set
		{
			if (value != _iconsPath)
			{
				_iconsPath = value;
				SetKeyVisual(_visualName);
			}
		}
	}

	public InputKeyVisualWidget(UIContext context)
		: base(context)
	{
		base.DoNotAcceptEvents = true;
	}

	private string GetKeyVisualName(string keyID)
	{
		Input.ControllerTypes controllerType = Input.ControllerType;
		string result = "None";
		switch (keyID)
		{
		case "A":
		case "B":
		case "C":
		case "D":
		case "E":
		case "F":
		case "G":
		case "H":
		case "I":
		case "J":
		case "K":
		case "L":
		case "M":
		case "N":
		case "O":
		case "P":
		case "Q":
		case "R":
		case "S":
		case "T":
		case "U":
		case "V":
		case "W":
		case "X":
		case "Y":
		case "Z":
		case "Up":
		case "Down":
		case "Left":
		case "Right":
		case "F1":
		case "F2":
		case "F3":
		case "F4":
		case "F5":
		case "F6":
		case "F7":
		case "F8":
		case "F9":
		case "F10":
		case "F11":
		case "F12":
		case "F13":
		case "F14":
		case "F15":
		case "F16":
		case "F17":
		case "F18":
		case "F19":
		case "F20":
		case "F21":
		case "F22":
		case "F23":
		case "F24":
		case "Space":
		case "Escape":
		case "Tab":
		case "BackSpace":
		case "CapsLock":
		case "LeftMouseButton":
		case "RightMouseButton":
		case "MiddleMouseButton":
		case "MouseScrollUp":
		case "MouseScrollDown":
		case "X1MouseButton":
		case "X2MouseButton":
		case "ControllerLUp":
		case "ControllerLDown":
		case "ControllerLLeft":
		case "ControllerLRight":
		case "ControllerRUp":
		case "ControllerRDown":
		case "ControllerRLeft":
		case "ControllerRRight":
		case "ControllerLBumper":
		case "ControllerRBumper":
		case "ControllerLTrigger":
		case "ControllerRTrigger":
		case "Tilde":
		case "BackSlash":
		case "Insert":
		case "Delete":
		case "Home":
		case "End":
		case "PageUp":
		case "PageDown":
		case "OpenBraces":
		case "CloseBraces":
		case "Comma":
		case "Equals":
		case "SemiColon":
		case "Apostrophe":
		case "Extended":
		case "NumLock":
			result = keyID.ToLower();
			break;
		case "ControllerLOption":
		case "ControllerROption":
			result = ((controllerType != Input.ControllerTypes.PlayStationDualShock) ? keyID.ToLower() : (keyID.ToLower() + "_4"));
			break;
		case "Numpad0":
		case "D0":
			result = "0";
			break;
		case "Numpad1":
		case "D1":
			result = "1";
			break;
		case "Numpad2":
		case "D2":
			result = "2";
			break;
		case "Numpad3":
		case "D3":
			result = "3";
			break;
		case "Numpad4":
		case "D4":
			result = "4";
			break;
		case "Numpad5":
		case "D5":
			result = "5";
			break;
		case "Numpad6":
		case "D6":
			result = "6";
			break;
		case "Numpad7":
		case "D7":
			result = "7";
			break;
		case "Numpad8":
		case "D8":
			result = "8";
			break;
		case "Numpad9":
		case "D9":
			result = "9";
			break;
		case "NumpadMinus":
		case "Minus":
			result = "-";
			break;
		case "NumpadPlus":
			result = "+";
			break;
		case "NumpadEnter":
		case "Enter":
			result = "enter";
			break;
		case "LeftShift":
		case "RightShift":
			result = "shift";
			break;
		case "LeftControl":
		case "RightControl":
			result = "control";
			break;
		case "LeftAlt":
		case "RightAlt":
			result = "alt";
			break;
		case "ControllerLThumb":
			result = "controllerlthumb";
			break;
		case "ControllerLStick":
		case "ControllerLStickUp":
		case "ControllerLStickDown":
		case "ControllerLStickLeft":
		case "ControllerLStickRight":
			result = "controllerlstick";
			break;
		case "ControllerRThumb":
			result = "controllerrthumb";
			break;
		case "ControllerRStick":
		case "ControllerRStickUp":
		case "ControllerRStickDown":
		case "ControllerRStickLeft":
		case "ControllerRStickRight":
			result = "controllerrstick";
			break;
		case "ControllerShare":
			result = ((controllerType != Input.ControllerTypes.PlayStationDualShock) ? keyID.ToLower() : (keyID.ToLower() + "_4"));
			break;
		case "NumpadSlash":
		case "Slash":
			result = "slash";
			break;
		case "NumpadPeriod":
		case "Period":
			result = "period";
			break;
		case "NumpadMultiply":
			result = "multiply";
			break;
		}
		return result;
	}

	private void SetKeyVisual(string visualName)
	{
		string text = IconsPath + "\\" + visualName;
		bool flag = Input.ControllerType.IsPlaystation();
		if (Input.IsGamepadActive && flag)
		{
			base.Sprite = base.Context.SpriteData.GetSprite(text + "_ps");
		}
		else
		{
			base.Sprite = base.Context.SpriteData.GetSprite(text);
		}
	}
}
