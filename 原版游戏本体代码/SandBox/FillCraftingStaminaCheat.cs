using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace SandBox
{
	// Token: 0x02000012 RID: 18
	public class FillCraftingStaminaCheat : GameplayCheatItem
	{
		// Token: 0x06000036 RID: 54 RVA: 0x000039E8 File Offset: 0x00001BE8
		public override void ExecuteCheat()
		{
			ICraftingCampaignBehavior campaignBehavior = Campaign.Current.GetCampaignBehavior<ICraftingCampaignBehavior>();
			if (campaignBehavior != null && PartyBase.MainParty != null)
			{
				for (int i = 0; i < PartyBase.MainParty.MemberRoster.Count; i++)
				{
					CharacterObject characterAtIndex = PartyBase.MainParty.MemberRoster.GetCharacterAtIndex(i);
					if (characterAtIndex.HeroObject != null)
					{
						int maxHeroCraftingStamina = campaignBehavior.GetMaxHeroCraftingStamina(characterAtIndex.HeroObject);
						if (campaignBehavior != null)
						{
							campaignBehavior.SetHeroCraftingStamina(characterAtIndex.HeroObject, MathF.Max(maxHeroCraftingStamina, 100));
						}
					}
				}
			}
		}

		// Token: 0x06000037 RID: 55 RVA: 0x00003A61 File Offset: 0x00001C61
		public override TextObject GetName()
		{
			return new TextObject("{=1Pc0SXXL}Fill Crafting Stamina", null);
		}
	}
}
