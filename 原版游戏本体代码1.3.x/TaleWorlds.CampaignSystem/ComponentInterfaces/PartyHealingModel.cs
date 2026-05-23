using System;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces
{
	// Token: 0x02000195 RID: 405
	public abstract class PartyHealingModel : MBGameModel<PartyHealingModel>
	{
		// Token: 0x06001C20 RID: 7200
		public abstract float GetSurgeryChance(PartyBase party);

		// Token: 0x06001C21 RID: 7201
		public abstract float GetSurvivalChance(PartyBase party, CharacterObject agentCharacter, DamageTypes damageType, bool canDamageKillEvenIfBlunt, PartyBase enemyParty = null);

		// Token: 0x06001C22 RID: 7202
		public abstract int GetSkillXpFromHealingTroop(PartyBase party);

		// Token: 0x06001C23 RID: 7203
		public abstract ExplainedNumber GetDailyHealingForRegulars(PartyBase partyBase, bool isPrisoner, bool includeDescriptions = false);

		// Token: 0x06001C24 RID: 7204
		public abstract ExplainedNumber GetDailyHealingHpForHeroes(PartyBase partyBase, bool isPrisoners, bool includeDescriptions = false);

		// Token: 0x06001C25 RID: 7205
		public abstract int GetHeroesEffectedHealingAmount(Hero hero, float healingRate);

		// Token: 0x06001C26 RID: 7206
		public abstract float GetSiegeBombardmentHitSurgeryChance(PartyBase party);

		// Token: 0x06001C27 RID: 7207
		public abstract ExplainedNumber GetBattleEndHealingAmount(PartyBase partyBase, Hero hero);
	}
}
