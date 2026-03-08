using System;

namespace TaleWorlds.Core
{
	// Token: 0x0200007C RID: 124
	public interface IAgentOriginBase
	{
		// Token: 0x170002D3 RID: 723
		// (get) Token: 0x06000858 RID: 2136
		bool IsUnderPlayersCommand { get; }

		// Token: 0x170002D4 RID: 724
		// (get) Token: 0x06000859 RID: 2137
		uint FactionColor { get; }

		// Token: 0x170002D5 RID: 725
		// (get) Token: 0x0600085A RID: 2138
		uint FactionColor2 { get; }

		// Token: 0x170002D6 RID: 726
		// (get) Token: 0x0600085B RID: 2139
		IBattleCombatant BattleCombatant { get; }

		// Token: 0x170002D7 RID: 727
		// (get) Token: 0x0600085C RID: 2140
		int UniqueSeed { get; }

		// Token: 0x170002D8 RID: 728
		// (get) Token: 0x0600085D RID: 2141
		int Seed { get; }

		// Token: 0x170002D9 RID: 729
		// (get) Token: 0x0600085E RID: 2142
		Banner Banner { get; }

		// Token: 0x170002DA RID: 730
		// (get) Token: 0x0600085F RID: 2143
		BasicCharacterObject Troop { get; }

		// Token: 0x170002DB RID: 731
		// (get) Token: 0x06000860 RID: 2144
		bool HasThrownWeapon { get; }

		// Token: 0x170002DC RID: 732
		// (get) Token: 0x06000861 RID: 2145
		bool HasHeavyArmor { get; }

		// Token: 0x170002DD RID: 733
		// (get) Token: 0x06000862 RID: 2146
		bool HasShield { get; }

		// Token: 0x170002DE RID: 734
		// (get) Token: 0x06000863 RID: 2147
		bool HasSpear { get; }

		// Token: 0x06000864 RID: 2148
		void SetWounded();

		// Token: 0x06000865 RID: 2149
		void SetKilled();

		// Token: 0x06000866 RID: 2150
		void SetRouted(bool isOrderRetreat);

		// Token: 0x06000867 RID: 2151
		void OnAgentRemoved(float agentHealth);

		// Token: 0x06000868 RID: 2152
		void OnScoreHit(BasicCharacterObject victim, BasicCharacterObject formationCaptain, int damage, bool isFatal, bool isTeamKill, WeaponComponentData attackerWeapon);

		// Token: 0x06000869 RID: 2153
		void SetBanner(Banner banner);

		// Token: 0x0600086A RID: 2154
		TroopTraitsMask GetTraitsMask();
	}
}
