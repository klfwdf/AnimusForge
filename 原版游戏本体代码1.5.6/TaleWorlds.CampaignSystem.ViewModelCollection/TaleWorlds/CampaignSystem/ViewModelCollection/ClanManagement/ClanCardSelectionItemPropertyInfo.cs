using System;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.ClanManagement;

public readonly struct ClanCardSelectionItemPropertyInfo
{
	public readonly TextObject Title;

	public readonly TextObject Value;

	public ClanCardSelectionItemPropertyInfo(TextObject title, TextObject value)
	{
		Title = title;
		Value = value;
	}

	public ClanCardSelectionItemPropertyInfo(TextObject value)
	{
		Title = null;
		Value = value;
	}

	public static TextObject CreateLabeledValueText(TextObject label, TextObject value)
	{
		TextObject textObject = new TextObject("{=!}<span style=\"Label\">{LABEL}</span>: {VALUE}");
		textObject.SetTextVariable("LABEL", label);
		textObject.SetTextVariable("VALUE", value);
		return textObject;
	}

	public static TextObject CreateActionGoldChangeText(int goldChange)
	{
		if (goldChange != 0)
		{
			bool num = goldChange > 0;
			string arg = (num ? "PositiveChange" : "NegativeChange");
			TextObject obj = (num ? new TextObject("{=8N1EdPB3}You will earn {GOLD}{GOLD_ICON}") : new TextObject("{=kjaACKUq}This action will cost {GOLD}{GOLD_ICON}"));
			obj.SetTextVariable("GOLD", $"<span style=\"{arg}\">{Math.Abs(goldChange)}</span>");
			obj.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"6\">");
			return obj;
		}
		return TextObject.GetEmpty();
	}
}
