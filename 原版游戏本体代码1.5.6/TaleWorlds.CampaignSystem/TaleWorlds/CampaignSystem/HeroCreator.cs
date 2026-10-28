using Helpers;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem;

public static class HeroCreator
{
	private class HeroInitializationArgs
	{
		public Hero Hero { get; }

		public TextObject Name { get; private set; }

		public TextObject FirstName { get; private set; }

		public Hero Mother { get; private set; }

		public Hero Father { get; private set; }

		public bool IsFemale { get; private set; }

		public Settlement BornSettlement { get; private set; }

		public int Level { get; private set; }

		public float Weight { get; private set; }

		public float Build { get; private set; }

		public StaticBodyProperties? StaticBodyProperties { get; private set; }

		public FormationClass? PreferredUpgradeFormation { get; private set; }

		public Clan Clan { get; private set; }

		public CultureObject Culture { get; private set; }

		public Clan SupporterOf { get; private set; }

		public Occupation Occupation { get; private set; }

		public bool IsOffspring { get; private set; }

		public bool GenerateFirstAndFullName { get; private set; }

		public bool HasBornSettlementBeenSet { get; private set; }

		public bool HasClanBeenSet { get; private set; }

		public HeroInitializationArgs(Hero hero, bool isOffspring)
		{
			DynamicBodyProperties dynamicBodyPropertiesBetweenMinMaxRange = CharacterHelper.GetDynamicBodyPropertiesBetweenMinMaxRange(hero.CharacterObject);
			Hero = hero;
			IsOffspring = isOffspring;
			Name = hero.Name;
			FirstName = hero.FirstName;
			Mother = hero.Mother;
			Father = hero.Father;
			IsFemale = hero.IsFemale;
			BornSettlement = null;
			Level = hero.Level;
			Weight = dynamicBodyPropertiesBetweenMinMaxRange.Weight;
			Build = dynamicBodyPropertiesBetweenMinMaxRange.Build;
			StaticBodyProperties = null;
			PreferredUpgradeFormation = null;
			Clan = null;
			SupporterOf = hero.SupporterOf;
			Occupation = hero.Occupation;
			Culture = null;
		}

		public HeroInitializationArgs SetGenerateFirstAndFullName(bool value)
		{
			GenerateFirstAndFullName = value;
			return this;
		}

		public HeroInitializationArgs SetName(TextObject name)
		{
			Name = name;
			return this;
		}

		public HeroInitializationArgs SetFirstName(TextObject firstName)
		{
			FirstName = firstName;
			return this;
		}

		public HeroInitializationArgs SetMother(Hero mother)
		{
			Mother = mother;
			return this;
		}

		public HeroInitializationArgs SetFather(Hero father)
		{
			Father = father;
			return this;
		}

		public HeroInitializationArgs SetIsFemale(bool isFemale)
		{
			IsFemale = isFemale;
			return this;
		}

		public HeroInitializationArgs SetBornSettlement(Settlement bornSettlement)
		{
			BornSettlement = bornSettlement;
			HasBornSettlementBeenSet = true;
			return this;
		}

		public HeroInitializationArgs SetLevel(int level)
		{
			Level = level;
			return this;
		}

		public HeroInitializationArgs SetAppearance(StaticBodyProperties? staticBodyProperties, float weight = -1f, float build = -1f, int hair = -1, int beard = -1, int tattoo = -1)
		{
			if (weight > 0f)
			{
				Weight = weight;
			}
			if (build > 0f)
			{
				Build = build;
			}
			BodyProperties bodyProperties = new BodyProperties(new DynamicBodyProperties(Hero.Age, Weight, Build), staticBodyProperties ?? default(StaticBodyProperties));
			FaceGen.SetHair(ref bodyProperties, hair, beard, tattoo);
			StaticBodyProperties = bodyProperties.StaticProperties;
			return this;
		}

		public HeroInitializationArgs SetPreferredUpgradeFormation(FormationClass preferredUpgradeFormation)
		{
			PreferredUpgradeFormation = preferredUpgradeFormation;
			return this;
		}

		public HeroInitializationArgs SetClan(Clan clan)
		{
			Clan = clan;
			HasClanBeenSet = true;
			return this;
		}

		public HeroInitializationArgs SetCulture(CultureObject culture)
		{
			Culture = culture;
			return this;
		}

		public HeroInitializationArgs SetSupporterOf(Clan supporterOf)
		{
			SupporterOf = supporterOf;
			return this;
		}

		public HeroInitializationArgs SetOccupation(Occupation occupation)
		{
			Occupation = occupation;
			return this;
		}
	}

	public static Hero CreateNotable(Occupation occupation, Settlement settlement = null)
	{
		CharacterObject randomTemplateByOccupation = Campaign.Current.Models.HeroCreationModel.GetRandomTemplateByOccupation(occupation, settlement);
		(CampaignTime birthDay, CampaignTime deathDay) birthAndDeathDay = Campaign.Current.Models.HeroCreationModel.GetBirthAndDeathDay(randomTemplateByOccupation, createAlive: true, -1);
		CampaignTime item = birthAndDeathDay.birthDay;
		CampaignTime item2 = birthAndDeathDay.deathDay;
		Hero hero = CreateHero(randomTemplateByOccupation, useCharacterAsTemplate: true, item, item2);
		HeroInitializationArgs heroInitializationArgs = new HeroInitializationArgs(hero, isOffspring: false).SetGenerateFirstAndFullName(value: true);
		if (settlement != null)
		{
			heroInitializationArgs.SetBornSettlement(settlement);
		}
		heroInitializationArgs.SetAppearance(Campaign.Current.Models.HeroCreationModel.GetStaticBodyProperties(hero, isOffspring: false, 0f));
		InitializeHeroFromSettings(heroInitializationArgs.Hero, heroInitializationArgs);
		return hero;
	}

	public static Hero CreateSpecialHero(CharacterObject template, Settlement bornSettlement = null, Clan faction = null, Clan supporterOfClan = null, int age = -1)
	{
		(CampaignTime birthDay, CampaignTime deathDay) birthAndDeathDay = Campaign.Current.Models.HeroCreationModel.GetBirthAndDeathDay(template, createAlive: true, age);
		CampaignTime item = birthAndDeathDay.birthDay;
		CampaignTime item2 = birthAndDeathDay.deathDay;
		Hero hero = CreateHero(template, useCharacterAsTemplate: true, item, item2);
		HeroInitializationArgs heroInitializationArgs = new HeroInitializationArgs(hero, isOffspring: false).SetGenerateFirstAndFullName(value: true);
		if (bornSettlement != null)
		{
			heroInitializationArgs.SetBornSettlement(bornSettlement);
		}
		if (faction != null)
		{
			heroInitializationArgs.SetClan(faction);
		}
		if (supporterOfClan != null)
		{
			heroInitializationArgs.SetSupporterOf(supporterOfClan);
		}
		InitializeHeroFromSettings(heroInitializationArgs.Hero, heroInitializationArgs);
		return hero;
	}

	public static Hero CreateChild(CharacterObject template, Settlement bornSettlement, Clan clan, int age)
	{
		(CampaignTime birthDay, CampaignTime deathDay) birthAndDeathDay = Campaign.Current.Models.HeroCreationModel.GetBirthAndDeathDay(template, createAlive: true, age);
		CampaignTime item = birthAndDeathDay.birthDay;
		CampaignTime item2 = birthAndDeathDay.deathDay;
		Hero hero = CreateHero(template, useCharacterAsTemplate: true, item, item2);
		HeroInitializationArgs heroInitializationArgs = new HeroInitializationArgs(hero, isOffspring: false).SetGenerateFirstAndFullName(value: true).SetBornSettlement(bornSettlement).SetClan(clan)
			.SetLevel(1);
		InitializeHeroFromSettings(heroInitializationArgs.Hero, heroInitializationArgs);
		return hero;
	}

	public static Hero CreateRelativeNotableHero(Hero relative)
	{
		CharacterObject randomTemplateByOccupation = Campaign.Current.Models.HeroCreationModel.GetRandomTemplateByOccupation(relative.Occupation, relative.HomeSettlement);
		(CampaignTime birthDay, CampaignTime deathDay) birthAndDeathDay = Campaign.Current.Models.HeroCreationModel.GetBirthAndDeathDay(randomTemplateByOccupation, createAlive: true, -1);
		CampaignTime item = birthAndDeathDay.birthDay;
		CampaignTime item2 = birthAndDeathDay.deathDay;
		Hero hero = CreateHero(randomTemplateByOccupation, useCharacterAsTemplate: true, item, item2);
		BodyProperties bodyPropertiesMin = relative.CharacterObject.GetBodyPropertiesMin();
		BodyProperties bodyPropertiesMin2 = randomTemplateByOccupation.GetBodyPropertiesMin();
		int defaultFaceSeed = relative.CharacterObject.GetDefaultFaceSeed(1);
		MBBodyProperty bodyPropertyRange = hero.CharacterObject.BodyPropertyRange;
		BodyProperties randomBodyProperties = BodyProperties.GetRandomBodyProperties(randomTemplateByOccupation.Race, randomTemplateByOccupation.IsFemale, bodyPropertiesMin, bodyPropertiesMin2, 1, defaultFaceSeed, bodyPropertyRange.HairTags, bodyPropertyRange.BeardTags, bodyPropertyRange.TattooTags);
		HeroInitializationArgs heroInitializationArgs = new HeroInitializationArgs(hero, isOffspring: false).SetBornSettlement(relative.HomeSettlement).SetCulture(relative.Culture).SetAppearance(randomBodyProperties.StaticProperties)
			.SetGenerateFirstAndFullName(value: true);
		InitializeHeroFromSettings(heroInitializationArgs.Hero, heroInitializationArgs);
		return hero;
	}

	public static bool CreateBasicHero(string stringId, CharacterObject character, out Hero hero, bool isAlive = true)
	{
		hero = Campaign.Current.CampaignObjectManager.Find<Hero>(stringId);
		if (hero == null)
		{
			(CampaignTime birthDay, CampaignTime deathDay) birthAndDeathDay = Campaign.Current.Models.HeroCreationModel.GetBirthAndDeathDay(character, isAlive, (int)character.Age);
			CampaignTime item = birthAndDeathDay.birthDay;
			CampaignTime item2 = birthAndDeathDay.deathDay;
			hero = CreateHero(character, useCharacterAsTemplate: false, item, item2);
			HeroInitializationArgs heroInitializationArgs = new HeroInitializationArgs(hero, isOffspring: false);
			InitializeHeroFromSettings(heroInitializationArgs.Hero, heroInitializationArgs);
			return true;
		}
		return false;
	}

	public static Hero DeliverOffSpring(Hero mother, Hero father, bool isOffspringFemale)
	{
		Debug.SilentAssert(mother.CharacterObject.Race == father.CharacterObject.Race, "", getDump: false, "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\HeroCreator.cs", "DeliverOffSpring", 272);
		CharacterObject characterTemplateForOffspring = Campaign.Current.Models.HeroCreationModel.GetCharacterTemplateForOffspring(mother, father, isOffspringFemale);
		(CampaignTime birthDay, CampaignTime deathDay) birthAndDeathDay = Campaign.Current.Models.HeroCreationModel.GetBirthAndDeathDay(characterTemplateForOffspring, createAlive: true, 0);
		CampaignTime item = birthAndDeathDay.birthDay;
		CampaignTime item2 = birthAndDeathDay.deathDay;
		Hero hero = CreateHero(characterTemplateForOffspring, useCharacterAsTemplate: true, item, item2);
		HeroInitializationArgs heroInitializationArgs = new HeroInitializationArgs(hero, isOffspring: true).SetMother(mother).SetFather(father).SetIsFemale(isOffspringFemale)
			.SetOccupation(isOffspringFemale ? mother.Occupation : father.Occupation)
			.SetLevel(1)
			.SetGenerateFirstAndFullName(value: true);
		if (mother == Hero.MainHero || father == Hero.MainHero)
		{
			heroInitializationArgs.SetClan(Hero.MainHero.Clan).SetCulture(Hero.MainHero.Culture);
		}
		else
		{
			CultureObject culture = ((MBRandom.RandomFloat < 0.5f) ? father.Culture : mother.Culture);
			heroInitializationArgs.SetClan(father.Clan).SetCulture(culture);
		}
		InitializeHeroFromSettings(heroInitializationArgs.Hero, heroInitializationArgs);
		return hero;
	}

	private static Hero CreateHero(CharacterObject character, bool useCharacterAsTemplate, CampaignTime birthDay, CampaignTime deathDay)
	{
		if (useCharacterAsTemplate)
		{
			Debug.Print("creating hero from template with id: " + character.StringId);
			character = CharacterObject.CreateFrom(character);
		}
		else
		{
			Debug.Print("creating hero for character with id: " + character.StringId);
		}
		return new Hero(character.StringId, character, birthDay, deathDay);
	}

	private static void InitializeHeroFromSettings(Hero hero, HeroInitializationArgs initializationArgs)
	{
		hero.Mother = initializationArgs.Mother;
		hero.Father = initializationArgs.Father;
		hero.IsFemale = initializationArgs.IsFemale;
		hero.BornSettlement = (initializationArgs.HasBornSettlementBeenSet ? initializationArgs.BornSettlement : Campaign.Current.Models.HeroCreationModel.GetBornSettlement(hero));
		hero.PreferredUpgradeFormation = initializationArgs.PreferredUpgradeFormation ?? Campaign.Current.Models.HeroCreationModel.GetPreferredUpgradeFormation(hero);
		hero.Clan = (initializationArgs.HasClanBeenSet ? initializationArgs.Clan : Campaign.Current.Models.HeroCreationModel.GetClan(hero));
		hero.Culture = initializationArgs.Culture ?? Campaign.Current.Models.HeroCreationModel.GetCulture(hero, hero.BornSettlement, hero.Clan);
		hero.StaticBodyProperties = initializationArgs.StaticBodyProperties ?? Campaign.Current.Models.HeroCreationModel.GetStaticBodyProperties(hero, initializationArgs.IsOffspring);
		hero.SupporterOf = initializationArgs.SupporterOf;
		hero.Level = initializationArgs.Level;
		hero.Weight = initializationArgs.Weight;
		hero.Build = initializationArgs.Build;
		if (initializationArgs.GenerateFirstAndFullName)
		{
			var (firstName, fullName) = Campaign.Current.Models.HeroCreationModel.GenerateFirstAndFullName(hero);
			hero.SetName(fullName, firstName);
		}
		else
		{
			hero.SetName(initializationArgs.Name, initializationArgs.FirstName);
		}
		if (initializationArgs.Occupation != hero.Occupation)
		{
			hero.SetNewOccupation(initializationArgs.Occupation);
		}
		foreach (var (trait, value) in Campaign.Current.Models.HeroCreationModel.GetTraitsForHero(hero))
		{
			hero.SetTraitLevel(trait, value);
		}
		foreach (var (skill, value2) in Campaign.Current.Models.HeroCreationModel.GetDefaultSkillsForHero(hero))
		{
			hero.SetSkillValue(skill, value2);
		}
		if (initializationArgs.IsOffspring)
		{
			hero.HeroDeveloper.InitializeHeroDeveloper();
			hero.ClearTraits();
		}
		else if (hero.Age >= (float)Campaign.Current.Models.AgeModel.HeroComesOfAge)
		{
			hero.HeroDeveloper.InitializeHeroDeveloper();
		}
		Equipment civilianEquipment = Campaign.Current.Models.HeroCreationModel.GetCivilianEquipment(hero);
		EquipmentHelper.AssignHeroEquipmentFromEquipment(hero, civilianEquipment);
		Equipment battleEquipment = Campaign.Current.Models.HeroCreationModel.GetBattleEquipment(hero);
		EquipmentHelper.AssignHeroEquipmentFromEquipment(hero, battleEquipment);
		CampaignEventDispatcher.Instance.OnHeroCreated(initializationArgs.Hero, initializationArgs.IsOffspring);
	}
}
