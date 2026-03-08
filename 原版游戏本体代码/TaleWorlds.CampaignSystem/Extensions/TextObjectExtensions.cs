using System;
using Helpers;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.Extensions
{
	// Token: 0x0200016E RID: 366
	public static class TextObjectExtensions
	{
		// Token: 0x06001AFA RID: 6906 RVA: 0x0008AC66 File Offset: 0x00088E66
		public static void SetCharacterProperties(this TextObject to, string tag, CharacterObject character, bool includeDetails = false)
		{
			StringHelpers.SetCharacterProperties(tag, character, to, includeDetails);
		}

		// Token: 0x06001AFB RID: 6907 RVA: 0x0008AC74 File Offset: 0x00088E74
		public static void SetSettlementProperties(this TextObject to, Settlement settlement)
		{
			to.SetTextVariable("IS_SETTLEMENT", 1);
			to.SetTextVariable("IS_CASTLE", settlement.IsCastle ? 1 : 0);
			to.SetTextVariable("IS_TOWN", settlement.IsTown ? 1 : 0);
			to.SetTextVariable("IS_HIDEOUT", settlement.IsHideout ? 1 : 0);
		}
	}
}
