using System;
using TaleWorlds.CampaignSystem.CharacterDevelopment;

namespace TaleWorlds.CampaignSystem.Conversation.Tags
{
	// Token: 0x0200025C RID: 604
	public class AmoralTag : ConversationTag
	{
		// Token: 0x170008AA RID: 2218
		// (get) Token: 0x060022FA RID: 8954 RVA: 0x000989EB File Offset: 0x00096BEB
		public override string StringId
		{
			get
			{
				return "AmoralTag";
			}
		}

		// Token: 0x060022FB RID: 8955 RVA: 0x000989F2 File Offset: 0x00096BF2
		public override bool IsApplicableTo(CharacterObject character)
		{
			return character.GetTraitLevel(DefaultTraits.Honor) + character.GetTraitLevel(DefaultTraits.Mercy) < 0;
		}

		// Token: 0x04000A68 RID: 2664
		public const string Id = "AmoralTag";
	}
}
