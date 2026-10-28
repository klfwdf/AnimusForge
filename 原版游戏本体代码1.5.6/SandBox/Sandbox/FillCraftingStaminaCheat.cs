using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace SandBox;

public class FillCraftingStaminaCheat : GameplayCheatItem
{
	public override void ExecuteCheat()
	{
		ICraftingCampaignBehavior campaignBehavior = Campaign.Current.GetCampaignBehavior<ICraftingCampaignBehavior>();
		if (campaignBehavior == null || PartyBase.MainParty == null)
		{
			return;
		}
		for (int i = 0; i < PartyBase.MainParty.MemberRoster.Count; i++)
		{
			CharacterObject characterAtIndex = PartyBase.MainParty.MemberRoster.GetCharacterAtIndex(i);
			if (characterAtIndex.HeroObject != null)
			{
				int maxHeroCraftingStamina = campaignBehavior.GetMaxHeroCraftingStamina(characterAtIndex.HeroObject);
				campaignBehavior?.SetHeroCraftingStamina(characterAtIndex.HeroObject, MathF.Max(maxHeroCraftingStamina, 100));
			}
		}
	}

	public override TextObject GetName()
	{
		return new TextObject("{=1Pc0SXXL}Fill Crafting Stamina");
	}
}
