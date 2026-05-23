using System.Collections.Generic;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces;

public abstract class HeroCreationModel : MBGameModel<HeroCreationModel>
{
	public abstract (CampaignTime birthDay, CampaignTime deathDay) GetBirthAndDeathDay(CharacterObject character, bool createAlive, int age);

	public abstract Settlement GetBornSettlement(Hero character);

	public abstract StaticBodyProperties GetStaticBodyProperties(Hero character, bool isOffspring, float variationAmount = 0.35f);

	public abstract FormationClass GetPreferredUpgradeFormation(Hero character);

	public abstract Clan GetClan(Hero character);

	public abstract CultureObject GetCulture(Hero hero, Settlement bornSettlement, Clan clan);

	public abstract CharacterObject GetRandomTemplateByOccupation(Occupation occupation, Settlement settlement = null);

	public abstract List<(TraitObject trait, int level)> GetTraitsForHero(Hero hero);

	public abstract Equipment GetCivilianEquipment(Hero hero);

	public abstract Equipment GetBattleEquipment(Hero hero);

	public abstract CharacterObject GetCharacterTemplateForOffspring(Hero mother, Hero father, bool isOffspringFemale);

	public abstract (TextObject firstName, TextObject name) GenerateFirstAndFullName(Hero hero);

	public abstract List<(SkillObject, int)> GetDefaultSkillsForHero(Hero hero);

	public abstract List<(SkillObject, int)> GetInheritedSkillsForHero(Hero hero);

	public abstract bool IsHeroCombatant(Hero hero);
}
