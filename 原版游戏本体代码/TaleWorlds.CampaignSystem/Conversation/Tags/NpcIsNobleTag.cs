using System;

namespace TaleWorlds.CampaignSystem.Conversation.Tags
{
	// Token: 0x02000257 RID: 599
	public class NpcIsNobleTag : ConversationTag
	{
		// Token: 0x170008A5 RID: 2213
		// (get) Token: 0x060022EB RID: 8939 RVA: 0x00098808 File Offset: 0x00096A08
		public override string StringId
		{
			get
			{
				return "NpcIsNobleTag";
			}
		}

		// Token: 0x060022EC RID: 8940 RVA: 0x00098810 File Offset: 0x00096A10
		public override bool IsApplicableTo(CharacterObject character)
		{
			Hero heroObject = character.HeroObject;
			if (heroObject == null)
			{
				return false;
			}
			Clan clan = heroObject.Clan;
			bool? flag = ((clan != null) ? new bool?(clan.IsNoble) : null);
			bool flag2 = true;
			return (flag.GetValueOrDefault() == flag2) & (flag != null);
		}

		// Token: 0x04000A63 RID: 2659
		public const string Id = "NpcIsNobleTag";
	}
}
