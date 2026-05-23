using System;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Buildings;
using TaleWorlds.Localization;

namespace SandBox
{
	// Token: 0x02000011 RID: 17
	public class CompleteBuildingProjectCheat : GameplayCheatItem
	{
		// Token: 0x06000033 RID: 51 RVA: 0x00003920 File Offset: 0x00001B20
		public override void ExecuteCheat()
		{
			if (Settlement.CurrentSettlement != null && Settlement.CurrentSettlement.IsFortification)
			{
				foreach (Building building in Settlement.CurrentSettlement.Town.Buildings)
				{
					if (building.CurrentLevel < 3)
					{
						Building building2 = building;
						int currentLevel = building2.CurrentLevel;
						building2.CurrentLevel = currentLevel + 1;
					}
				}
			}
		}

		// Token: 0x06000034 RID: 52 RVA: 0x000039A4 File Offset: 0x00001BA4
		public override TextObject GetName()
		{
			Settlement currentSettlement = Settlement.CurrentSettlement;
			if (currentSettlement != null)
			{
				TextObject textObject = new TextObject("{=5uXs8pS9}Complete All Building Projects in {SETTLEMENT_NAME}", null);
				textObject.SetTextVariable("SETTLEMENT_NAME", currentSettlement.Name.ToString());
				return textObject;
			}
			return null;
		}
	}
}
