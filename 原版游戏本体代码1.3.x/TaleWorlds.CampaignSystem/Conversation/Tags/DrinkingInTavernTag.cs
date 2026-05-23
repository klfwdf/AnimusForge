using System;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;

namespace TaleWorlds.CampaignSystem.Conversation.Tags
{
	// Token: 0x0200023F RID: 575
	public class DrinkingInTavernTag : ConversationTag
	{
		// Token: 0x1700088D RID: 2189
		// (get) Token: 0x060022A3 RID: 8867 RVA: 0x0009829E File Offset: 0x0009649E
		public override string StringId
		{
			get
			{
				return "DrinkingInTavernTag";
			}
		}

		// Token: 0x060022A4 RID: 8868 RVA: 0x000982A8 File Offset: 0x000964A8
		public override bool IsApplicableTo(CharacterObject character)
		{
			if (LocationComplex.Current != null && character.IsHero)
			{
				Location locationOfCharacter = LocationComplex.Current.GetLocationOfCharacter(character.HeroObject);
				Location locationWithId = LocationComplex.Current.GetLocationWithId("tavern");
				if (character.HeroObject.IsWanderer && Settlement.CurrentSettlement != null && locationWithId == locationOfCharacter)
				{
					return true;
				}
			}
			else if (character.HeroObject == null && LocationComplex.Current != null && Settlement.CurrentSettlement != null && LocationComplex.Current.GetLocationWithId("tavern") == CampaignMission.Current.Location)
			{
				return true;
			}
			return false;
		}

		// Token: 0x04000A4B RID: 2635
		public const string Id = "DrinkingInTavernTag";
	}
}
