using System;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.ClanManagement
{
	// Token: 0x0200011B RID: 283
	public readonly struct ClanCardSelectionItemPropertyInfo
	{
		// Token: 0x06001A08 RID: 6664 RVA: 0x00062434 File Offset: 0x00060634
		public ClanCardSelectionItemPropertyInfo(TextObject title, TextObject value)
		{
			this.Title = title;
			this.Value = value;
		}

		// Token: 0x06001A09 RID: 6665 RVA: 0x00062454 File Offset: 0x00060654
		public ClanCardSelectionItemPropertyInfo(TextObject value)
		{
			this.Title = null;
			this.Value = value;
		}

		// Token: 0x06001A0A RID: 6666 RVA: 0x00062471 File Offset: 0x00060671
		public static TextObject CreateLabeledValueText(TextObject label, TextObject value)
		{
			TextObject textObject = new TextObject("{=!}<span style=\"Label\">{LABEL}</span>: {VALUE}", null);
			textObject.SetTextVariable("LABEL", label);
			textObject.SetTextVariable("VALUE", value);
			return textObject;
		}

		// Token: 0x06001A0B RID: 6667 RVA: 0x00062498 File Offset: 0x00060698
		public static TextObject CreateActionGoldChangeText(int goldChange)
		{
			if (goldChange != 0)
			{
				bool flag = goldChange > 0;
				string arg = (flag ? "PositiveChange" : "NegativeChange");
				TextObject textObject = (flag ? new TextObject("{=8N1EdPB3}You will earn {GOLD}{GOLD_ICON}", null) : new TextObject("{=kjaACKUq}This action will cost {GOLD}{GOLD_ICON}", null));
				textObject.SetTextVariable("GOLD", string.Format("<span style=\"{0}\">{1}</span>", arg, Math.Abs(goldChange)));
				textObject.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"8\">");
				return textObject;
			}
			return TextObject.GetEmpty();
		}

		// Token: 0x04000BFA RID: 3066
		public readonly TextObject Title;

		// Token: 0x04000BFB RID: 3067
		public readonly TextObject Value;
	}
}
