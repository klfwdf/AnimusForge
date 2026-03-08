using System;
using TaleWorlds.Localization;

namespace TaleWorlds.Core
{
	// Token: 0x0200007D RID: 125
	public interface IBattleCombatant
	{
		// Token: 0x170002DF RID: 735
		// (get) Token: 0x0600086B RID: 2155
		TextObject Name { get; }

		// Token: 0x170002E0 RID: 736
		// (get) Token: 0x0600086C RID: 2156
		BattleSideEnum Side { get; }

		// Token: 0x170002E1 RID: 737
		// (get) Token: 0x0600086D RID: 2157
		BasicCultureObject BasicCulture { get; }

		// Token: 0x170002E2 RID: 738
		// (get) Token: 0x0600086E RID: 2158
		BasicCharacterObject General { get; }

		// Token: 0x170002E3 RID: 739
		// (get) Token: 0x0600086F RID: 2159
		Tuple<uint, uint> PrimaryColorPair { get; }

		// Token: 0x170002E4 RID: 740
		// (get) Token: 0x06000870 RID: 2160
		Banner Banner { get; }

		// Token: 0x06000871 RID: 2161
		int GetTacticsSkillAmount();
	}
}
