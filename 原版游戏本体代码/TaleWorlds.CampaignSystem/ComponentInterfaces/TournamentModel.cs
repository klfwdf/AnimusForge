using System;
using System.Runtime.CompilerServices;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.TournamentGames;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces
{
	// Token: 0x020001E1 RID: 481
	public abstract class TournamentModel : MBGameModel<TournamentModel>
	{
		// Token: 0x06001E80 RID: 7808
		public abstract float GetTournamentStartChance(Town town);

		// Token: 0x06001E81 RID: 7809
		public abstract TournamentGame CreateTournament(Town town);

		// Token: 0x06001E82 RID: 7810
		public abstract float GetTournamentEndChance(TournamentGame tournament);

		// Token: 0x06001E83 RID: 7811
		public abstract int GetNumLeaderboardVictoriesAtGameStart();

		// Token: 0x06001E84 RID: 7812
		public abstract float GetTournamentSimulationScore(CharacterObject character);

		// Token: 0x06001E85 RID: 7813
		public abstract int GetRenownReward(Hero winner, Town town);

		// Token: 0x06001E86 RID: 7814
		public abstract int GetInfluenceReward(Hero winner, Town town);

		// Token: 0x06001E87 RID: 7815
		[return: TupleElementNames(new string[] { "skill", "xp" })]
		public abstract ValueTuple<SkillObject, int> GetSkillXpGainFromTournament(Town town);

		// Token: 0x06001E88 RID: 7816
		public abstract Equipment GetParticipantArmor(CharacterObject participant);

		// Token: 0x06001E89 RID: 7817
		public abstract MBList<ItemObject> GetRegularRewardItems(Town town, int regularRewardMinValue, int regularRewardMaxValue);

		// Token: 0x06001E8A RID: 7818
		public abstract MBList<ItemObject> GetEliteRewardItems(Town town, int regularRewardMinValue, int regularRewardMaxValue);
	}
}
