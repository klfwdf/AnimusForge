using System;

namespace TaleWorlds.CampaignSystem.Conversation.Tags
{
	// Token: 0x02000263 RID: 611
	public class RomanticallyInvolvedTag : ConversationTag
	{
		// Token: 0x170008B1 RID: 2225
		// (get) Token: 0x0600230F RID: 8975 RVA: 0x00098BDB File Offset: 0x00096DDB
		public override string StringId
		{
			get
			{
				return "RomanticallyInvolvedTag";
			}
		}

		// Token: 0x06002310 RID: 8976 RVA: 0x00098BE2 File Offset: 0x00096DE2
		public override bool IsApplicableTo(CharacterObject character)
		{
			return character.IsHero && Romance.GetRomanticLevel(character.HeroObject, CharacterObject.PlayerCharacter.HeroObject) >= Romance.RomanceLevelEnum.CourtshipStarted;
		}

		// Token: 0x04000A6F RID: 2671
		public const string Id = "RomanticallyInvolvedTag";
	}
}
