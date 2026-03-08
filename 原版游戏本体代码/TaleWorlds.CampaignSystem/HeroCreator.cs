using System;
using Helpers;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem
{
	// Token: 0x0200008D RID: 141
	public static class HeroCreator
	{
		// Token: 0x06001240 RID: 4672 RVA: 0x000534CC File Offset: 0x000516CC
		public static Hero CreateNotable(Occupation occupation, Settlement settlement = null)
		{
			CharacterObject randomTemplateByOccupation = Campaign.Current.Models.HeroCreationModel.GetRandomTemplateByOccupation(occupation, settlement);
			ValueTuple<CampaignTime, CampaignTime> birthAndDeathDay = Campaign.Current.Models.HeroCreationModel.GetBirthAndDeathDay(randomTemplateByOccupation, true, -1);
			CampaignTime item = birthAndDeathDay.Item1;
			CampaignTime item2 = birthAndDeathDay.Item2;
			Hero hero = HeroCreator.CreateHero(randomTemplateByOccupation, true, item, item2);
			HeroCreator.HeroInitializationArgs heroInitializationArgs = new HeroCreator.HeroInitializationArgs(hero, false).SetGenerateFirstAndFullName(true);
			if (settlement != null)
			{
				heroInitializationArgs.SetBornSettlement(settlement);
			}
			heroInitializationArgs.SetAppearance(new StaticBodyProperties?(Campaign.Current.Models.HeroCreationModel.GetStaticBodyProperties(hero, false, 0f)), -1f, -1f, -1, -1, -1);
			HeroCreator.InitializeHeroFromSettings(heroInitializationArgs.Hero, heroInitializationArgs);
			return hero;
		}

		// Token: 0x06001241 RID: 4673 RVA: 0x00053580 File Offset: 0x00051780
		public static Hero CreateSpecialHero(CharacterObject template, Settlement bornSettlement = null, Clan faction = null, Clan supporterOfClan = null, int age = -1)
		{
			ValueTuple<CampaignTime, CampaignTime> birthAndDeathDay = Campaign.Current.Models.HeroCreationModel.GetBirthAndDeathDay(template, true, age);
			CampaignTime item = birthAndDeathDay.Item1;
			CampaignTime item2 = birthAndDeathDay.Item2;
			Hero hero = HeroCreator.CreateHero(template, true, item, item2);
			HeroCreator.HeroInitializationArgs heroInitializationArgs = new HeroCreator.HeroInitializationArgs(hero, false).SetGenerateFirstAndFullName(true);
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
			HeroCreator.InitializeHeroFromSettings(heroInitializationArgs.Hero, heroInitializationArgs);
			return hero;
		}

		// Token: 0x06001242 RID: 4674 RVA: 0x000535F8 File Offset: 0x000517F8
		public static Hero CreateChild(CharacterObject template, Settlement bornSettlement, Clan clan, int age)
		{
			ValueTuple<CampaignTime, CampaignTime> birthAndDeathDay = Campaign.Current.Models.HeroCreationModel.GetBirthAndDeathDay(template, true, age);
			CampaignTime item = birthAndDeathDay.Item1;
			CampaignTime item2 = birthAndDeathDay.Item2;
			Hero hero = HeroCreator.CreateHero(template, true, item, item2);
			HeroCreator.HeroInitializationArgs heroInitializationArgs = new HeroCreator.HeroInitializationArgs(hero, false).SetGenerateFirstAndFullName(true).SetBornSettlement(bornSettlement).SetClan(clan)
				.SetLevel(1);
			HeroCreator.InitializeHeroFromSettings(heroInitializationArgs.Hero, heroInitializationArgs);
			return hero;
		}

		// Token: 0x06001243 RID: 4675 RVA: 0x00053660 File Offset: 0x00051860
		public static Hero CreateRelativeNotableHero(Hero relative)
		{
			CharacterObject randomTemplateByOccupation = Campaign.Current.Models.HeroCreationModel.GetRandomTemplateByOccupation(relative.Occupation, relative.HomeSettlement);
			ValueTuple<CampaignTime, CampaignTime> birthAndDeathDay = Campaign.Current.Models.HeroCreationModel.GetBirthAndDeathDay(randomTemplateByOccupation, true, -1);
			CampaignTime item = birthAndDeathDay.Item1;
			CampaignTime item2 = birthAndDeathDay.Item2;
			Hero hero = HeroCreator.CreateHero(randomTemplateByOccupation, true, item, item2);
			BodyProperties bodyPropertiesMin = relative.CharacterObject.GetBodyPropertiesMin(false);
			BodyProperties bodyPropertiesMin2 = randomTemplateByOccupation.GetBodyPropertiesMin(false);
			int defaultFaceSeed = relative.CharacterObject.GetDefaultFaceSeed(1);
			MBBodyProperty bodyPropertyRange = hero.CharacterObject.BodyPropertyRange;
			BodyProperties randomBodyProperties = BodyProperties.GetRandomBodyProperties(randomTemplateByOccupation.Race, randomTemplateByOccupation.IsFemale, bodyPropertiesMin, bodyPropertiesMin2, 1, defaultFaceSeed, bodyPropertyRange.HairTags, bodyPropertyRange.BeardTags, bodyPropertyRange.TattooTags, 0f);
			HeroCreator.HeroInitializationArgs heroInitializationArgs = new HeroCreator.HeroInitializationArgs(hero, false).SetBornSettlement(relative.HomeSettlement).SetCulture(relative.Culture).SetAppearance(new StaticBodyProperties?(randomBodyProperties.StaticProperties), -1f, -1f, -1, -1, -1)
				.SetGenerateFirstAndFullName(true);
			HeroCreator.InitializeHeroFromSettings(heroInitializationArgs.Hero, heroInitializationArgs);
			return hero;
		}

		// Token: 0x06001244 RID: 4676 RVA: 0x00053770 File Offset: 0x00051970
		public static bool CreateBasicHero(string stringId, CharacterObject character, out Hero hero, bool isAlive = true)
		{
			hero = Campaign.Current.CampaignObjectManager.Find<Hero>(stringId);
			if (hero == null)
			{
				ValueTuple<CampaignTime, CampaignTime> birthAndDeathDay = Campaign.Current.Models.HeroCreationModel.GetBirthAndDeathDay(character, isAlive, (int)character.Age);
				CampaignTime item = birthAndDeathDay.Item1;
				CampaignTime item2 = birthAndDeathDay.Item2;
				hero = HeroCreator.CreateHero(character, false, item, item2);
				HeroCreator.HeroInitializationArgs heroInitializationArgs = new HeroCreator.HeroInitializationArgs(hero, false);
				HeroCreator.InitializeHeroFromSettings(heroInitializationArgs.Hero, heroInitializationArgs);
				return true;
			}
			return false;
		}

		// Token: 0x06001245 RID: 4677 RVA: 0x000537E0 File Offset: 0x000519E0
		public static Hero DeliverOffSpring(Hero mother, Hero father, bool isOffspringFemale)
		{
			Debug.SilentAssert(mother.CharacterObject.Race == father.CharacterObject.Race, "", false, "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\HeroCreator.cs", "DeliverOffSpring", 275);
			CharacterObject characterTemplateForOffspring = Campaign.Current.Models.HeroCreationModel.GetCharacterTemplateForOffspring(mother, father, isOffspringFemale);
			ValueTuple<CampaignTime, CampaignTime> birthAndDeathDay = Campaign.Current.Models.HeroCreationModel.GetBirthAndDeathDay(characterTemplateForOffspring, true, 0);
			CampaignTime item = birthAndDeathDay.Item1;
			CampaignTime item2 = birthAndDeathDay.Item2;
			Hero hero = HeroCreator.CreateHero(characterTemplateForOffspring, true, item, item2);
			HeroCreator.HeroInitializationArgs heroInitializationArgs = new HeroCreator.HeroInitializationArgs(hero, true).SetMother(mother).SetFather(father).SetIsFemale(isOffspringFemale)
				.SetOccupation(isOffspringFemale ? mother.Occupation : father.Occupation)
				.SetLevel(1)
				.SetGenerateFirstAndFullName(true);
			if (mother == Hero.MainHero || father == Hero.MainHero)
			{
				heroInitializationArgs.SetClan(Hero.MainHero.Clan).SetCulture(Hero.MainHero.Culture);
			}
			else
			{
				CultureObject culture = ((MBRandom.RandomFloat < 0.5f) ? father.Culture : mother.Culture);
				heroInitializationArgs.SetClan(father.Clan).SetCulture(culture);
			}
			HeroCreator.InitializeHeroFromSettings(heroInitializationArgs.Hero, heroInitializationArgs);
			return hero;
		}

		// Token: 0x06001246 RID: 4678 RVA: 0x00053910 File Offset: 0x00051B10
		private static Hero CreateHero(CharacterObject character, bool useCharacterAsTemplate, CampaignTime birthDay, CampaignTime deathDay)
		{
			if (useCharacterAsTemplate)
			{
				Debug.Print("creating hero from template with id: " + character.StringId, 0, Debug.DebugColor.White, 17592186044416UL);
				character = CharacterObject.CreateFrom(character, null);
			}
			else
			{
				Debug.Print("creating hero for character with id: " + character.StringId, 0, Debug.DebugColor.White, 17592186044416UL);
			}
			return new Hero(character.StringId, character, birthDay, deathDay);
		}

		// Token: 0x06001247 RID: 4679 RVA: 0x00053984 File Offset: 0x00051B84
		private static void InitializeHeroFromSettings(Hero hero, HeroCreator.HeroInitializationArgs initializationArgs)
		{
			hero.Mother = initializationArgs.Mother;
			hero.Father = initializationArgs.Father;
			hero.IsFemale = initializationArgs.IsFemale;
			hero.BornSettlement = (initializationArgs.HasBornSettlementBeenSet ? initializationArgs.BornSettlement : Campaign.Current.Models.HeroCreationModel.GetBornSettlement(hero));
			hero.PreferredUpgradeFormation = initializationArgs.PreferredUpgradeFormation ?? Campaign.Current.Models.HeroCreationModel.GetPreferredUpgradeFormation(hero);
			hero.Clan = (initializationArgs.HasClanBeenSet ? initializationArgs.Clan : Campaign.Current.Models.HeroCreationModel.GetClan(hero));
			hero.Culture = initializationArgs.Culture ?? Campaign.Current.Models.HeroCreationModel.GetCulture(hero, hero.BornSettlement, hero.Clan);
			hero.StaticBodyProperties = initializationArgs.StaticBodyProperties ?? Campaign.Current.Models.HeroCreationModel.GetStaticBodyProperties(hero, initializationArgs.IsOffspring, 0.35f);
			hero.SupporterOf = initializationArgs.SupporterOf;
			hero.Level = initializationArgs.Level;
			hero.Weight = initializationArgs.Weight;
			hero.Build = initializationArgs.Build;
			if (initializationArgs.GenerateFirstAndFullName)
			{
				ValueTuple<TextObject, TextObject> valueTuple = Campaign.Current.Models.HeroCreationModel.GenerateFirstAndFullName(hero);
				TextObject item = valueTuple.Item1;
				TextObject item2 = valueTuple.Item2;
				hero.SetName(item2, item);
			}
			else
			{
				hero.SetName(initializationArgs.Name, initializationArgs.FirstName);
			}
			if (initializationArgs.Occupation != hero.Occupation)
			{
				hero.SetNewOccupation(initializationArgs.Occupation);
			}
			foreach (ValueTuple<TraitObject, int> valueTuple2 in Campaign.Current.Models.HeroCreationModel.GetTraitsForHero(hero))
			{
				TraitObject item3 = valueTuple2.Item1;
				int item4 = valueTuple2.Item2;
				hero.SetTraitLevel(item3, item4);
			}
			foreach (ValueTuple<SkillObject, int> valueTuple3 in Campaign.Current.Models.HeroCreationModel.GetDefaultSkillsForHero(hero))
			{
				SkillObject item5 = valueTuple3.Item1;
				int item6 = valueTuple3.Item2;
				hero.SetSkillValue(item5, item6);
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

		// Token: 0x0200053F RID: 1343
		private class HeroInitializationArgs
		{
			// Token: 0x17000EDA RID: 3802
			// (get) Token: 0x06004C33 RID: 19507 RVA: 0x001797B6 File Offset: 0x001779B6
			public Hero Hero { get; }

			// Token: 0x17000EDB RID: 3803
			// (get) Token: 0x06004C34 RID: 19508 RVA: 0x001797BE File Offset: 0x001779BE
			// (set) Token: 0x06004C35 RID: 19509 RVA: 0x001797C6 File Offset: 0x001779C6
			public TextObject Name { get; private set; }

			// Token: 0x17000EDC RID: 3804
			// (get) Token: 0x06004C36 RID: 19510 RVA: 0x001797CF File Offset: 0x001779CF
			// (set) Token: 0x06004C37 RID: 19511 RVA: 0x001797D7 File Offset: 0x001779D7
			public TextObject FirstName { get; private set; }

			// Token: 0x17000EDD RID: 3805
			// (get) Token: 0x06004C38 RID: 19512 RVA: 0x001797E0 File Offset: 0x001779E0
			// (set) Token: 0x06004C39 RID: 19513 RVA: 0x001797E8 File Offset: 0x001779E8
			public Hero Mother { get; private set; }

			// Token: 0x17000EDE RID: 3806
			// (get) Token: 0x06004C3A RID: 19514 RVA: 0x001797F1 File Offset: 0x001779F1
			// (set) Token: 0x06004C3B RID: 19515 RVA: 0x001797F9 File Offset: 0x001779F9
			public Hero Father { get; private set; }

			// Token: 0x17000EDF RID: 3807
			// (get) Token: 0x06004C3C RID: 19516 RVA: 0x00179802 File Offset: 0x00177A02
			// (set) Token: 0x06004C3D RID: 19517 RVA: 0x0017980A File Offset: 0x00177A0A
			public bool IsFemale { get; private set; }

			// Token: 0x17000EE0 RID: 3808
			// (get) Token: 0x06004C3E RID: 19518 RVA: 0x00179813 File Offset: 0x00177A13
			// (set) Token: 0x06004C3F RID: 19519 RVA: 0x0017981B File Offset: 0x00177A1B
			public Settlement BornSettlement { get; private set; }

			// Token: 0x17000EE1 RID: 3809
			// (get) Token: 0x06004C40 RID: 19520 RVA: 0x00179824 File Offset: 0x00177A24
			// (set) Token: 0x06004C41 RID: 19521 RVA: 0x0017982C File Offset: 0x00177A2C
			public int Level { get; private set; }

			// Token: 0x17000EE2 RID: 3810
			// (get) Token: 0x06004C42 RID: 19522 RVA: 0x00179835 File Offset: 0x00177A35
			// (set) Token: 0x06004C43 RID: 19523 RVA: 0x0017983D File Offset: 0x00177A3D
			public float Weight { get; private set; }

			// Token: 0x17000EE3 RID: 3811
			// (get) Token: 0x06004C44 RID: 19524 RVA: 0x00179846 File Offset: 0x00177A46
			// (set) Token: 0x06004C45 RID: 19525 RVA: 0x0017984E File Offset: 0x00177A4E
			public float Build { get; private set; }

			// Token: 0x17000EE4 RID: 3812
			// (get) Token: 0x06004C46 RID: 19526 RVA: 0x00179857 File Offset: 0x00177A57
			// (set) Token: 0x06004C47 RID: 19527 RVA: 0x0017985F File Offset: 0x00177A5F
			public StaticBodyProperties? StaticBodyProperties { get; private set; }

			// Token: 0x17000EE5 RID: 3813
			// (get) Token: 0x06004C48 RID: 19528 RVA: 0x00179868 File Offset: 0x00177A68
			// (set) Token: 0x06004C49 RID: 19529 RVA: 0x00179870 File Offset: 0x00177A70
			public FormationClass? PreferredUpgradeFormation { get; private set; }

			// Token: 0x17000EE6 RID: 3814
			// (get) Token: 0x06004C4A RID: 19530 RVA: 0x00179879 File Offset: 0x00177A79
			// (set) Token: 0x06004C4B RID: 19531 RVA: 0x00179881 File Offset: 0x00177A81
			public Clan Clan { get; private set; }

			// Token: 0x17000EE7 RID: 3815
			// (get) Token: 0x06004C4C RID: 19532 RVA: 0x0017988A File Offset: 0x00177A8A
			// (set) Token: 0x06004C4D RID: 19533 RVA: 0x00179892 File Offset: 0x00177A92
			public CultureObject Culture { get; private set; }

			// Token: 0x17000EE8 RID: 3816
			// (get) Token: 0x06004C4E RID: 19534 RVA: 0x0017989B File Offset: 0x00177A9B
			// (set) Token: 0x06004C4F RID: 19535 RVA: 0x001798A3 File Offset: 0x00177AA3
			public Clan SupporterOf { get; private set; }

			// Token: 0x17000EE9 RID: 3817
			// (get) Token: 0x06004C50 RID: 19536 RVA: 0x001798AC File Offset: 0x00177AAC
			// (set) Token: 0x06004C51 RID: 19537 RVA: 0x001798B4 File Offset: 0x00177AB4
			public Occupation Occupation { get; private set; }

			// Token: 0x17000EEA RID: 3818
			// (get) Token: 0x06004C52 RID: 19538 RVA: 0x001798BD File Offset: 0x00177ABD
			// (set) Token: 0x06004C53 RID: 19539 RVA: 0x001798C5 File Offset: 0x00177AC5
			public bool IsOffspring { get; private set; }

			// Token: 0x17000EEB RID: 3819
			// (get) Token: 0x06004C54 RID: 19540 RVA: 0x001798CE File Offset: 0x00177ACE
			// (set) Token: 0x06004C55 RID: 19541 RVA: 0x001798D6 File Offset: 0x00177AD6
			public bool GenerateFirstAndFullName { get; private set; }

			// Token: 0x17000EEC RID: 3820
			// (get) Token: 0x06004C56 RID: 19542 RVA: 0x001798DF File Offset: 0x00177ADF
			// (set) Token: 0x06004C57 RID: 19543 RVA: 0x001798E7 File Offset: 0x00177AE7
			public bool HasBornSettlementBeenSet { get; private set; }

			// Token: 0x17000EED RID: 3821
			// (get) Token: 0x06004C58 RID: 19544 RVA: 0x001798F0 File Offset: 0x00177AF0
			// (set) Token: 0x06004C59 RID: 19545 RVA: 0x001798F8 File Offset: 0x00177AF8
			public bool HasClanBeenSet { get; private set; }

			// Token: 0x06004C5A RID: 19546 RVA: 0x00179904 File Offset: 0x00177B04
			public HeroInitializationArgs(Hero hero, bool isOffspring)
			{
				DynamicBodyProperties dynamicBodyPropertiesBetweenMinMaxRange = CharacterHelper.GetDynamicBodyPropertiesBetweenMinMaxRange(hero.CharacterObject);
				this.Hero = hero;
				this.IsOffspring = isOffspring;
				this.Name = hero.Name;
				this.FirstName = hero.FirstName;
				this.Mother = hero.Mother;
				this.Father = hero.Father;
				this.IsFemale = hero.IsFemale;
				this.BornSettlement = null;
				this.Level = hero.Level;
				this.Weight = dynamicBodyPropertiesBetweenMinMaxRange.Weight;
				this.Build = dynamicBodyPropertiesBetweenMinMaxRange.Build;
				this.StaticBodyProperties = null;
				this.PreferredUpgradeFormation = null;
				this.Clan = null;
				this.SupporterOf = hero.SupporterOf;
				this.Occupation = hero.Occupation;
				this.Culture = null;
			}

			// Token: 0x06004C5B RID: 19547 RVA: 0x001799DC File Offset: 0x00177BDC
			public HeroCreator.HeroInitializationArgs SetGenerateFirstAndFullName(bool value)
			{
				this.GenerateFirstAndFullName = value;
				return this;
			}

			// Token: 0x06004C5C RID: 19548 RVA: 0x001799E6 File Offset: 0x00177BE6
			public HeroCreator.HeroInitializationArgs SetName(TextObject name)
			{
				this.Name = name;
				return this;
			}

			// Token: 0x06004C5D RID: 19549 RVA: 0x001799F0 File Offset: 0x00177BF0
			public HeroCreator.HeroInitializationArgs SetFirstName(TextObject firstName)
			{
				this.FirstName = firstName;
				return this;
			}

			// Token: 0x06004C5E RID: 19550 RVA: 0x001799FA File Offset: 0x00177BFA
			public HeroCreator.HeroInitializationArgs SetMother(Hero mother)
			{
				this.Mother = mother;
				return this;
			}

			// Token: 0x06004C5F RID: 19551 RVA: 0x00179A04 File Offset: 0x00177C04
			public HeroCreator.HeroInitializationArgs SetFather(Hero father)
			{
				this.Father = father;
				return this;
			}

			// Token: 0x06004C60 RID: 19552 RVA: 0x00179A0E File Offset: 0x00177C0E
			public HeroCreator.HeroInitializationArgs SetIsFemale(bool isFemale)
			{
				this.IsFemale = isFemale;
				return this;
			}

			// Token: 0x06004C61 RID: 19553 RVA: 0x00179A18 File Offset: 0x00177C18
			public HeroCreator.HeroInitializationArgs SetBornSettlement(Settlement bornSettlement)
			{
				this.BornSettlement = bornSettlement;
				this.HasBornSettlementBeenSet = true;
				return this;
			}

			// Token: 0x06004C62 RID: 19554 RVA: 0x00179A29 File Offset: 0x00177C29
			public HeroCreator.HeroInitializationArgs SetLevel(int level)
			{
				this.Level = level;
				return this;
			}

			// Token: 0x06004C63 RID: 19555 RVA: 0x00179A34 File Offset: 0x00177C34
			public HeroCreator.HeroInitializationArgs SetAppearance(StaticBodyProperties? staticBodyProperties, float weight = -1f, float build = -1f, int hair = -1, int beard = -1, int tattoo = -1)
			{
				if (weight > 0f)
				{
					this.Weight = weight;
				}
				if (build > 0f)
				{
					this.Build = build;
				}
				BodyProperties bodyProperties = new BodyProperties(new DynamicBodyProperties(this.Hero.Age, this.Weight, this.Build), staticBodyProperties ?? default(StaticBodyProperties));
				FaceGen.SetHair(ref bodyProperties, hair, beard, tattoo);
				this.StaticBodyProperties = new StaticBodyProperties?(bodyProperties.StaticProperties);
				return this;
			}

			// Token: 0x06004C64 RID: 19556 RVA: 0x00179ABF File Offset: 0x00177CBF
			public HeroCreator.HeroInitializationArgs SetPreferredUpgradeFormation(FormationClass preferredUpgradeFormation)
			{
				this.PreferredUpgradeFormation = new FormationClass?(preferredUpgradeFormation);
				return this;
			}

			// Token: 0x06004C65 RID: 19557 RVA: 0x00179ACE File Offset: 0x00177CCE
			public HeroCreator.HeroInitializationArgs SetClan(Clan clan)
			{
				this.Clan = clan;
				this.HasClanBeenSet = true;
				return this;
			}

			// Token: 0x06004C66 RID: 19558 RVA: 0x00179ADF File Offset: 0x00177CDF
			public HeroCreator.HeroInitializationArgs SetCulture(CultureObject culture)
			{
				this.Culture = culture;
				return this;
			}

			// Token: 0x06004C67 RID: 19559 RVA: 0x00179AE9 File Offset: 0x00177CE9
			public HeroCreator.HeroInitializationArgs SetSupporterOf(Clan supporterOf)
			{
				this.SupporterOf = supporterOf;
				return this;
			}

			// Token: 0x06004C68 RID: 19560 RVA: 0x00179AF3 File Offset: 0x00177CF3
			public HeroCreator.HeroInitializationArgs SetOccupation(Occupation occupation)
			{
				this.Occupation = occupation;
				return this;
			}
		}
	}
}
