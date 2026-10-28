using TaleWorlds.Localization;

namespace NavalDLC.ViewModelCollection.Port.PortScreenHandlers;

public readonly struct PortActionInfo
{
	public readonly bool IsRelevant;

	public readonly bool IsEnabled;

	public readonly int GoldCost;

	public readonly TextObject ActionName;

	public readonly TextObject Tooltip;

	private PortActionInfo(bool isRelevant, bool isEnabled, int goldCost, TextObject actionName, TextObject tooltip = null)
	{
		IsRelevant = isRelevant;
		IsEnabled = isEnabled;
		GoldCost = goldCost;
		ActionName = actionName;
		Tooltip = tooltip ?? TextObject.GetEmpty();
	}

	public static PortActionInfo CreateValid(bool isEnabled, int goldCost, TextObject name, TextObject tooltip)
	{
		return new PortActionInfo(isRelevant: true, isEnabled, goldCost, name, tooltip);
	}

	public static PortActionInfo CreateInvalid(TextObject reason = null)
	{
		return new PortActionInfo(isRelevant: false, isEnabled: false, 0, TextObject.GetEmpty(), reason);
	}
}
