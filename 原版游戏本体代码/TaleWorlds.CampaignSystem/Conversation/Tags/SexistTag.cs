using System;
using System.Linq;
using TaleWorlds.CampaignSystem.CharacterDevelopment;

namespace TaleWorlds.CampaignSystem.Conversation.Tags
{
	// Token: 0x0200025E RID: 606
	public class SexistTag : ConversationTag
	{
		// Token: 0x170008AC RID: 2220
		// (get) Token: 0x06002300 RID: 8960 RVA: 0x00098A41 File Offset: 0x00096C41
		public override string StringId
		{
			get
			{
				return "SexistTag";
			}
		}

		// Token: 0x06002301 RID: 8961 RVA: 0x00098A48 File Offset: 0x00096C48
		public override bool IsApplicableTo(CharacterObject character)
		{
			bool flag = character.HeroObject.Clan.Heroes.Any((Hero x) => x.IsFemale && x.IsCommander);
			int num = character.GetTraitLevel(DefaultTraits.Calculating) + character.GetTraitLevel(DefaultTraits.Mercy);
			int num2 = character.GetTraitLevel(DefaultTraits.Valor) + character.GetTraitLevel(DefaultTraits.Generosity);
			return num < 0 && num2 <= 0 && !flag;
		}

		// Token: 0x04000A6A RID: 2666
		public const string Id = "SexistTag";
	}
}
