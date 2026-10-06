using Helpers;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.Extensions;

public static class TextObjectExtensions
{
	public static void SetCharacterProperties(this TextObject to, string tag, CharacterObject character, bool includeDetails = false)
	{
		StringHelpers.SetCharacterProperties(tag, character, to, includeDetails);
	}

	public static void SetSettlementProperties(this TextObject to, Settlement settlement)
	{
		to.SetTextVariable("IS_SETTLEMENT", 1);
		to.SetTextVariable("IS_CASTLE", settlement.IsCastle ? 1 : 0);
		to.SetTextVariable("IS_TOWN", settlement.IsTown ? 1 : 0);
		to.SetTextVariable("IS_HIDEOUT", settlement.IsHideout ? 1 : 0);
	}
}
