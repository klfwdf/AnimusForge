using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;

namespace Helpers
{
	// Token: 0x0200000C RID: 12
	public static class CaravanHelper
	{
		// Token: 0x06000052 RID: 82 RVA: 0x00005654 File Offset: 0x00003854
		public static PartyTemplateObject GetRandomCaravanTemplate(CultureObject culture, bool isElite, bool isLand)
		{
			PartyTemplateObject randomElementWithPredicate;
			if (isElite)
			{
				randomElementWithPredicate = culture.EliteCaravanPartyTemplates.GetRandomElementWithPredicate((PartyTemplateObject x) => CaravanHelper.IsPartyTemplateSuitable(x, isLand));
			}
			else
			{
				randomElementWithPredicate = culture.CaravanPartyTemplates.GetRandomElementWithPredicate((PartyTemplateObject x) => CaravanHelper.IsPartyTemplateSuitable(x, isLand));
			}
			return randomElementWithPredicate;
		}

		// Token: 0x06000053 RID: 83 RVA: 0x000056A4 File Offset: 0x000038A4
		private static bool IsPartyTemplateSuitable(PartyTemplateObject template, bool isLand)
		{
			if (!isLand)
			{
				return template.ShipHulls.Count > 0;
			}
			return template.ShipHulls.Count == 0;
		}
	}
}
