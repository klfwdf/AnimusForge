using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem.Party;

namespace TaleWorlds.CampaignSystem
{
	// Token: 0x0200002A RID: 42
	public struct BattleResultPartyData
	{
		// Token: 0x060001D8 RID: 472 RVA: 0x000134C7 File Offset: 0x000116C7
		public BattleResultPartyData(PartyBase party)
		{
			this.Party = party;
			this.Characters = new List<CharacterObject>();
		}

		// Token: 0x0400001C RID: 28
		public readonly PartyBase Party;

		// Token: 0x0400001D RID: 29
		public readonly List<CharacterObject> Characters;
	}
}
