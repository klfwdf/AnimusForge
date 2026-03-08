using System;

namespace TaleWorlds.CampaignSystem.Conversation.Tags
{
	// Token: 0x0200026A RID: 618
	public class PlayerIsLiegeTag : ConversationTag
	{
		// Token: 0x170008B8 RID: 2232
		// (get) Token: 0x06002324 RID: 8996 RVA: 0x00098DBF File Offset: 0x00096FBF
		public override string StringId
		{
			get
			{
				return "PlayerIsLiegeTag";
			}
		}

		// Token: 0x06002325 RID: 8997 RVA: 0x00098DC8 File Offset: 0x00096FC8
		public override bool IsApplicableTo(CharacterObject character)
		{
			return character.IsHero && character.HeroObject.MapFaction.IsKingdomFaction && character.HeroObject.MapFaction == Hero.MainHero.MapFaction && Hero.MainHero.MapFaction.Leader == Hero.MainHero;
		}

		// Token: 0x04000A77 RID: 2679
		public const string Id = "PlayerIsLiegeTag";
	}
}
