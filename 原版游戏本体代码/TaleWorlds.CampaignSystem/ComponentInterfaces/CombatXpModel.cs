using System;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces
{
	// Token: 0x0200019F RID: 415
	public abstract class CombatXpModel : MBGameModel<CombatXpModel>
	{
		// Token: 0x06001C65 RID: 7269
		public abstract SkillObject GetSkillForWeapon(WeaponComponentData weapon, bool isSiegeEngineHit);

		// Token: 0x06001C66 RID: 7270
		public abstract ExplainedNumber GetXpFromHit(CharacterObject attackerTroop, CharacterObject captain, CharacterObject attackedTroop, PartyBase attackerParty, int damage, bool isFatal, CombatXpModel.MissionTypeEnum missionType);

		// Token: 0x06001C67 RID: 7271
		public abstract float GetXpMultiplierFromShotDifficulty(float shotDifficulty);

		// Token: 0x1700070C RID: 1804
		// (get) Token: 0x06001C68 RID: 7272
		public abstract float CaptainRadius { get; }

		// Token: 0x020005F2 RID: 1522
		public enum MissionTypeEnum
		{
			// Token: 0x04001895 RID: 6293
			Battle,
			// Token: 0x04001896 RID: 6294
			PracticeFight,
			// Token: 0x04001897 RID: 6295
			Tournament,
			// Token: 0x04001898 RID: 6296
			SimulationBattle,
			// Token: 0x04001899 RID: 6297
			NoXp
		}
	}
}
