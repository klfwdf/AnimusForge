using System;

namespace TaleWorlds.CampaignSystem.Conversation.Tags
{
	// Token: 0x02000287 RID: 647
	public class NPCIsInSeaTag : ConversationTag
	{
		// Token: 0x170008D5 RID: 2261
		// (get) Token: 0x0600237B RID: 9083 RVA: 0x00099347 File Offset: 0x00097547
		public override string StringId
		{
			get
			{
				return "NPCIsInSeaTag";
			}
		}

		// Token: 0x0600237C RID: 9084 RVA: 0x00099350 File Offset: 0x00097550
		public override bool IsApplicableTo(CharacterObject character)
		{
			bool result = false;
			if (character.IsHero)
			{
				result = (character.HeroObject.IsPrisoner ? character.HeroObject.PartyBelongedToAsPrisoner.MobileParty : character.HeroObject.PartyBelongedTo).IsCurrentlyAtSea;
			}
			return result;
		}

		// Token: 0x04000A94 RID: 2708
		public const string Id = "NPCIsInSeaTag";
	}
}
