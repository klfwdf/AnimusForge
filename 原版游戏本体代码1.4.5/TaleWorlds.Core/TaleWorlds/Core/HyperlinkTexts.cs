using System;
using TaleWorlds.Localization;

namespace TaleWorlds.Core;

public static class HyperlinkTexts
{
	private enum ConsoleType
	{
		Xbox,
		Ps4,
		Ps5
	}

	public const string GoldIcon = "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"6\">";

	public const string MoraleIcon = "{=!}<img src=\"General\\Icons\\Morale@2x\" extend=\"4\">";

	public const string InfluenceIcon = "{=!}<img src=\"General\\Icons\\Influence@2x\" extend=\"5\">";

	public const string IssueAvailableIcon = "{=!}<img src=\"General\\Icons\\icon_issue_available_square\" extend=\"4\">";

	public const string IssueActiveIcon = "{=!}<img src=\"General\\Icons\\icon_issue_active_square\" extend=\"4\">";

	public const string TrackedIssueIcon = "{=!}<img src=\"General\\Icons\\issue_target_icon\" extend=\"4\">";

	public const string QuestAvailableIcon = "{=!}<img src=\"General\\Icons\\icon_quest_available\" extend=\"4\">";

	public const string QuestActiveIcon = "{=!}<img src=\"General\\Icons\\icon_issue_active_square\" extend=\"4\">";

	public const string StoryQuestActiveIcon = "{=!}<img src=\"General\\Icons\\icon_story_quest_active_square\" extend=\"4\">";

	public const string TrackedStoryQuestIcon = "{=!}<img src=\"General\\Icons\\quest_target_icon\" extend=\"4\">";

	public const string InPrisonIcon = "{=!}<img src=\"SPGeneral\\Clan\\Status\\icon_inprison\" extend=\"4\">";

	public const string ChildIcon = "{=!}<img src=\"SPGeneral\\Clan\\Status\\icon_ischild\" extend=\"4\">";

	public const string PregnantIcon = "{=!}<img src=\"SPGeneral\\Clan\\Status\\icon_pregnant\" extend=\"4\">";

	public const string IllIcon = "{=!}<img src=\"SPGeneral\\Clan\\Status\\icon_terminallyill\" extend=\"4\">";

	public const string HeirIcon = "{=!}<img src=\"SPGeneral\\Clan\\Status\\icon_heir\" extend=\"4\">";

	public const string UnreadIcon = "{=!}<img src=\"MapMenuUnread@2x\" extend=\"4\">";

	public const string UnselectedPerkIcon = "{=!}<img src=\"CharacterDeveloper\\UnselectedPerksIcon\" extend=\"2\">";

	public const string HorseIcon = "{=!}<img src=\"StdAssets\\ItemIcons\\Mount\" extend=\"14\">";

	public const string CrimeIcon = "{=!}<img src=\"SPGeneral\\MapOverlay\\Settlement\\icon_crime\" extend=\"12\">";

	public const string UpgradeAvailableIcon = "{=!}<img src=\"PartyScreen\\upgrade_icon\" extend=\"5\">";

	public const string FocusIcon = "{=!}<img src=\"CharacterDeveloper\\cp_icon\">";

	public static Func<bool> IsPlayStationGamepadActive;

	public static TextObject GetSettlementHyperlinkText(string link, TextObject settlementName)
	{
		TextObject textObject = new TextObject("{=!}{.link}<a style=\"Link.Settlement\" href=\"event:{LINK}\"><b>{SETTLEMENT_NAME}</b></a>");
		textObject.SetTextVariable("LINK", link);
		textObject.SetTextVariable("SETTLEMENT_NAME", settlementName);
		return textObject;
	}

	public static TextObject GetKingdomHyperlinkText(string link, TextObject kingdomName)
	{
		TextObject textObject = new TextObject("{=!}{.link}<a style=\"Link.Kingdom\" href=\"event:{LINK}\"><b>{KINGDOM_NAME}</b></a>");
		textObject.SetTextVariable("LINK", link);
		textObject.SetTextVariable("KINGDOM_NAME", kingdomName);
		return textObject;
	}

	public static TextObject GetHeroHyperlinkText(string link, TextObject heroName)
	{
		TextObject textObject = new TextObject("{=!}{.link}<a style=\"Link.Hero\" href=\"event:{LINK}\"><b>{HERO_NAME}</b></a>");
		textObject.SetTextVariable("LINK", link);
		textObject.SetTextVariable("HERO_NAME", heroName);
		return textObject;
	}

	public static TextObject GetConceptHyperlinkText(string link, TextObject conceptName)
	{
		TextObject textObject = new TextObject("{=!}{.link}<a style=\"Link.Concept\" href=\"event:{LINK}\"><b>{CONCEPT_NAME}</b></a>");
		textObject.SetTextVariable("LINK", link);
		textObject.SetTextVariable("CONCEPT_NAME", conceptName);
		return textObject;
	}

	public static TextObject GetClanHyperlinkText(string link, TextObject clanName)
	{
		TextObject textObject = new TextObject("{=!}{.link}<a style=\"Link.Clan\" href=\"event:{LINK}\"><b>{CLAN_NAME}</b></a>");
		textObject.SetTextVariable("LINK", link);
		textObject.SetTextVariable("CLAN_NAME", clanName);
		return textObject;
	}

	public static TextObject GetShipHyperlinkText(string link, TextObject shipHullName)
	{
		TextObject textObject = new TextObject("{=!}{.link}<a style=\"Link.Ship\" href=\"event:{LINK}\"><b>{SHIP}</b></a>");
		textObject.SetTextVariable("LINK", link);
		textObject.SetTextVariable("SHIP", shipHullName);
		return textObject;
	}

	public static TextObject GetUnitHyperlinkText(string link, TextObject unitName)
	{
		TextObject textObject = new TextObject("{=!}{.link}<a style=\"Link.Unit\" href=\"event:{LINK}\"><b>{UNIT_NAME}</b></a>");
		textObject.SetTextVariable("LINK", link);
		textObject.SetTextVariable("UNIT_NAME", unitName);
		return textObject;
	}

	public static string GetGenericHyperlinkText(string link, string name)
	{
		return "<a style=\"Link\" href=\"event:" + link + "\"><b>" + name + "</b></a>";
	}

	public static string GetGenericImageText(string meshId, int extend = 0)
	{
		return $"<img src=\"{meshId}\" extend=\"{extend}\">";
	}

	public static string GetKeyHyperlinkText(string keyID, float overrideExtendScale = 1f)
	{
		string text = "None";
		int num = 16;
		ConsoleType consoleType = ConsoleType.Xbox;
		Func<bool> isPlayStationGamepadActive = IsPlayStationGamepadActive;
		if (isPlayStationGamepadActive != null && isPlayStationGamepadActive())
		{
			consoleType = ConsoleType.Ps5;
		}
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
		case "Escape":
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
			num = 24;
			text = keyID.ToLower();
			break;
		case "Space":
		case "BackSpace":
		case "CapsLock":
			num = 10;
			text = keyID.ToLower();
			break;
		case "Tab":
			num = 12;
			text = keyID.ToLower();
			break;
		case "LeftMouseButton":
		case "RightMouseButton":
		case "MiddleMouseButton":
		case "MouseScrollUp":
		case "MouseScrollDown":
		case "X1MouseButton":
		case "X2MouseButton":
			num = 16;
			text = keyID.ToLower();
			break;
		case "ControllerLUp":
		case "ControllerLDown":
		case "ControllerLLeft":
		case "ControllerLRight":
			num = ((consoleType == ConsoleType.Xbox) ? 16 : 10);
			text = keyID.ToLower();
			break;
		case "ControllerRUp":
		case "ControllerRDown":
		case "ControllerRLeft":
		case "ControllerRRight":
			num = 14;
			text = keyID.ToLower();
			break;
		case "ControllerLBumper":
		case "ControllerRBumper":
			num = ((consoleType == ConsoleType.Xbox) ? 14 : 20);
			text = keyID.ToLower();
			break;
		case "ControllerLTrigger":
		case "ControllerRTrigger":
			num = 16;
			text = keyID.ToLower();
			break;
		case "ControllerLOption":
			num = ((consoleType == ConsoleType.Xbox) ? 14 : 8);
			text = ((consoleType == ConsoleType.Ps4) ? (keyID.ToLower() + "_4") : keyID.ToLower());
			break;
		case "ControllerROption":
			num = 16;
			text = ((consoleType == ConsoleType.Ps4) ? (keyID.ToLower() + "_4") : keyID.ToLower());
			break;
		case "Numpad0":
		case "D0":
		case "Numpad1":
		case "D1":
		case "Numpad2":
		case "D2":
		case "Numpad3":
		case "D3":
		case "Numpad4":
		case "D4":
		case "Numpad5":
		case "D5":
		case "Numpad6":
		case "D6":
		case "Numpad7":
		case "D7":
		case "Numpad8":
		case "D8":
		case "Numpad9":
		case "D9":
			num = 24;
			text = keyID.Substring(keyID.Length - 1);
			break;
		case "NumpadMinus":
		case "Minus":
			num = 24;
			text = "-";
			break;
		case "NumpadPlus":
			num = 24;
			text = "+";
			break;
		case "NumpadEnter":
		case "Enter":
			num = 12;
			text = "enter";
			break;
		case "LeftShift":
		case "RightShift":
			num = 14;
			text = "shift";
			break;
		case "LeftControl":
		case "RightControl":
			num = 12;
			text = "control";
			break;
		case "LeftAlt":
		case "RightAlt":
			num = 24;
			text = "alt";
			break;
		case "ControllerLThumb":
			num = ((consoleType == ConsoleType.Xbox) ? 12 : 10);
			text = "controllerlthumb";
			break;
		case "ControllerLStick":
		case "ControllerLStickUp":
		case "ControllerLStickDown":
		case "ControllerLStickLeft":
		case "ControllerLStickRight":
			num = ((consoleType == ConsoleType.Xbox) ? 12 : 10);
			text = "controllerlstick";
			break;
		case "ControllerRThumb":
			num = ((consoleType == ConsoleType.Xbox) ? 12 : 10);
			text = "controllerrthumb";
			break;
		case "ControllerRStick":
		case "ControllerRStickUp":
		case "ControllerRStickDown":
		case "ControllerRStickLeft":
		case "ControllerRStickRight":
			num = ((consoleType == ConsoleType.Xbox) ? 12 : 10);
			text = "controllerrstick";
			break;
		case "NumpadSlash":
		case "Slash":
			num = 24;
			text = "slash";
			break;
		case "NumpadPeriod":
		case "Period":
			num = 24;
			text = "period";
			break;
		case "NumpadMultiply":
			num = 24;
			text = "multiply";
			break;
		}
		if (consoleType == ConsoleType.Ps4 || consoleType == ConsoleType.Ps5)
		{
			text += "_ps";
		}
		num = (int)((float)num * overrideExtendScale);
		return $"<img src=\"General\\InputKeys\\{text}\" extend=\"{num}\">";
	}
}
