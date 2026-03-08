using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces
{
	// Token: 0x020001FA RID: 506
	public abstract class HeroCreationModel : MBGameModel<HeroCreationModel>
	{
		// Token: 0x06001F36 RID: 7990
		[return: TupleElementNames(new string[] { "birthDay", "deathDay" })]
		public abstract ValueTuple<CampaignTime, CampaignTime> GetBirthAndDeathDay(CharacterObject character, bool createAlive, int age);

		// Token: 0x06001F37 RID: 7991
		public abstract Settlement GetBornSettlement(Hero character);

		// Token: 0x06001F38 RID: 7992
		public abstract StaticBodyProperties GetStaticBodyProperties(Hero character, bool isOffspring, float variationAmount = 0.35f);

		// Token: 0x06001F39 RID: 7993
		public abstract FormationClass GetPreferredUpgradeFormation(Hero character);

		// Token: 0x06001F3A RID: 7994
		public abstract Clan GetClan(Hero character);

		// Token: 0x06001F3B RID: 7995
		public abstract CultureObject GetCulture(Hero hero, Settlement bornSettlement, Clan clan);

		// Token: 0x06001F3C RID: 7996
		public abstract CharacterObject GetRandomTemplateByOccupation(Occupation occupation, Settlement settlement = null);

		// Token: 0x06001F3D RID: 7997
		[return: TupleElementNames(new string[] { "trait", "level" })]
		public abstract List<ValueTuple<TraitObject, int>> GetTraitsForHero(Hero hero);

		// Token: 0x06001F3E RID: 7998
		public abstract Equipment GetCivilianEquipment(Hero hero);

		// Token: 0x06001F3F RID: 7999
		public abstract Equipment GetBattleEquipment(Hero hero);

		// Token: 0x06001F40 RID: 8000
		public abstract CharacterObject GetCharacterTemplateForOffspring(Hero mother, Hero father, bool isOffspringFemale);

		// Token: 0x06001F41 RID: 8001
		[return: TupleElementNames(new string[] { "firstName", "name" })]
		public abstract ValueTuple<TextObject, TextObject> GenerateFirstAndFullName(Hero hero);

		// Token: 0x06001F42 RID: 8002
		public abstract List<ValueTuple<SkillObject, int>> GetDefaultSkillsForHero(Hero hero);

		// Token: 0x06001F43 RID: 8003
		public abstract List<ValueTuple<SkillObject, int>> GetInheritedSkillsForHero(Hero hero);

		// Token: 0x06001F44 RID: 8004
		public abstract bool IsHeroCombatant(Hero hero);
	}
}
