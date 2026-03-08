using System;

namespace TaleWorlds.CampaignSystem.Conversation.Tags
{
	// Token: 0x02000264 RID: 612
	public class AttractedToPlayerTag : ConversationTag
	{
		// Token: 0x170008B2 RID: 2226
		// (get) Token: 0x06002312 RID: 8978 RVA: 0x00098C11 File Offset: 0x00096E11
		public override string StringId
		{
			get
			{
				return "AttractedToPlayerTag";
			}
		}

		// Token: 0x06002313 RID: 8979 RVA: 0x00098C18 File Offset: 0x00096E18
		public override bool IsApplicableTo(CharacterObject character)
		{
			Hero heroObject = character.HeroObject;
			return heroObject != null && Hero.MainHero.IsFemale != heroObject.IsFemale && !FactionManager.IsAtWarAgainstFaction(heroObject.MapFaction, Hero.MainHero.MapFaction) && Campaign.Current.Models.RomanceModel.GetAttractionValuePercentage(heroObject, Hero.MainHero) > 70 && heroObject.Spouse == null && Hero.MainHero.Spouse == null;
		}

		// Token: 0x04000A70 RID: 2672
		public const string Id = "AttractedToPlayerTag";

		// Token: 0x04000A71 RID: 2673
		private const int MinimumFlirtPercentageForComment = 70;
	}
}
