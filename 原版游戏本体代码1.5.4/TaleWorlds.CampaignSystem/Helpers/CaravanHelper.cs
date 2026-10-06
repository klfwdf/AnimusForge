using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;

namespace Helpers;

public static class CaravanHelper
{
	public static PartyTemplateObject GetRandomCaravanTemplate(CultureObject culture, bool isElite, bool isLand)
	{
		if (isElite)
		{
			return culture.EliteCaravanPartyTemplates.GetRandomElementWithPredicate((PartyTemplateObject x) => IsPartyTemplateSuitable(x, isLand));
		}
		return culture.CaravanPartyTemplates.GetRandomElementWithPredicate((PartyTemplateObject x) => IsPartyTemplateSuitable(x, isLand));
	}

	private static bool IsPartyTemplateSuitable(PartyTemplateObject template, bool isLand)
	{
		if (!isLand)
		{
			return template.ShipHulls.Count > 0;
		}
		return template.ShipHulls.Count == 0;
	}
}
