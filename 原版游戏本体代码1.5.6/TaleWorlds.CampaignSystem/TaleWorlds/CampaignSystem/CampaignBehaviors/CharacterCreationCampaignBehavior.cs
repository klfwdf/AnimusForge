using System.Collections.Generic;
using TaleWorlds.CampaignSystem.CharacterCreationContent;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors;

public class CharacterCreationCampaignBehavior : CampaignBehaviorBase, ICharacterCreationContentHandler
{
	private static class CharacterOccupationTypes
	{
		public const string Retainer = "retainer";

		public const string Bard = "bard";

		public const string Hunter = "hunter";

		public const string Farmer = "farmer";

		public const string Herder = "herder";

		public const string Healer = "healer";

		public const string Mercenary = "mercenary";

		public const string Infantry = "infantry";

		public const string Skirmisher = "skirmisher";

		public const string Kern = "kern";

		public const string Guard = "guard";

		public const string RetainerUrban = "retainer_urban";

		public const string MercenaryUrban = "mercenary_urban";

		public const string MerchantUrban = "merchant_urban";

		public const string VagabondUrban = "vagabond_urban";

		public const string ArtisanUrban = "artisan_urban";

		public const string PhysicianUrban = "physician_urban";

		public const string HealerUrban = "healer_urban";

		public const string BardUrban = "bard_urban";

		public static bool IsUrbanOccupation(string occupation)
		{
			switch (occupation)
			{
			default:
				return occupation == "bard_urban";
			case "retainer_urban":
			case "mercenary_urban":
			case "merchant_urban":
			case "vagabond_urban":
			case "artisan_urban":
			case "physician_urban":
			case "healer_urban":
				return true;
			}
		}
	}

	private readonly IReadOnlyDictionary<string, string> _occupationToEquipmentMapping = new Dictionary<string, string>
	{
		{ "retainer", "retainer" },
		{ "bard", "bard" },
		{ "hunter", "hunter" },
		{ "farmer", "farmer" },
		{ "herder", "herder" },
		{ "healer", "healer" },
		{ "mercenary", "mercenary" },
		{ "infantry", "infantry" },
		{ "skirmisher", "skirmisher" },
		{ "kern", "kern" },
		{ "guard", "guard" },
		{ "retainer_urban", "retainer" },
		{ "mercenary_urban", "mercenary" },
		{ "merchant_urban", "merchant" },
		{ "vagabond_urban", "vagabond" },
		{ "artisan_urban", "artisan" },
		{ "physician_urban", "physician" },
		{ "healer_urban", "healer" },
		{ "bard_urban", "bard" }
	};

	private const int ChildhoodAge = 7;

	private const int EducationAge = 12;

	private const int YouthAge = 17;

	private const int AccomplishmentAge = 20;

	private const int ParentAge = 33;

	private const int YoungAdultAge = 20;

	private const int AdultAge = 30;

	private const int MiddleAge = 40;

	private const int ElderAge = 50;

	public const int FocusToAddYouthStart = 2;

	public const int FocusToAddAdultStart = 4;

	public const int FocusToAddMiddleAgedStart = 6;

	public const int FocusToAddElderlyStart = 8;

	public const int AttributeToAddYouthStart = 1;

	public const int AttributeToAddAdultStart = 2;

	public const int AttributeToAddMiddleAgedStart = 3;

	public const int AttributeToAddElderlyStart = 4;

	public const string MotherNarrativeCharacterStringId = "mother_character";

	public const string FatherNarrativeCharacterStringId = "father_character";

	public const string PlayerChildhoodCharacterStringId = "player_childhood_character";

	public const string PlayerEducationCharacterStringId = "player_education_character";

	public const string PlayerYouthCharacterStringId = "player_youth_character";

	public const string PlayerAdulthoodCharacterStringId = "player_adulthood_character";

	public const string PlayerAgeSelectionCharacterStringId = "player_age_selection_character";

	public const string HorseNarrativeCharacterStringId = "narrative_character_horse";

	private int _focusToAdd = 1;

	private int _skillLevelToAdd = 10;

	private int _attributeLevelToAdd = 1;

	private string GetMotherEquipmentId(CharacterCreationManager characterCreationManager, string occupationType, string cultureId)
	{
		characterCreationManager.CharacterCreationContent.TryGetEquipmentToUse(occupationType, out var equipmentId);
		return "mother_char_creation_" + equipmentId + "_" + cultureId;
	}

	private string GetFatherEquipmentId(CharacterCreationManager characterCreationManager, string occupationType, string cultureId)
	{
		characterCreationManager.CharacterCreationContent.TryGetEquipmentToUse(occupationType, out var equipmentId);
		return "father_char_creation_" + equipmentId + "_" + cultureId;
	}

	private string GetPlayerChildhoodAgeEquipmentId(CharacterCreationManager characterCreationManager, string parentOccupationType, string cultureId, bool isFemale)
	{
		characterCreationManager.CharacterCreationContent.TryGetEquipmentToUse(parentOccupationType, out var equipmentId);
		return "player_char_creation_childhood_age_" + cultureId + "_" + equipmentId + "_" + (isFemale ? "f" : "m");
	}

	private string GetPlayerEducationAgeEquipmentId(CharacterCreationManager characterCreationManager, string parentOccupationType, string cultureId, bool isFemale)
	{
		characterCreationManager.CharacterCreationContent.TryGetEquipmentToUse(parentOccupationType, out var equipmentId);
		return "player_char_creation_education_age_" + cultureId + "_" + equipmentId + "_" + (isFemale ? "f" : "m");
	}

	private string GetPlayerEquipmentId(CharacterCreationManager characterCreationManager, string occupationType, string cultureId, bool isFemale)
	{
		characterCreationManager.CharacterCreationContent.TryGetEquipmentToUse(occupationType, out var equipmentId);
		return "player_char_creation_" + cultureId + "_" + equipmentId + "_" + (isFemale ? "f" : "m");
	}

	public override void RegisterEvents()
	{
		CampaignEvents.OnCharacterCreationInitializedEvent.AddNonSerializedListener(this, OnCharacterCreationInitialized);
	}

	public override void SyncData(IDataStore dataStore)
	{
	}

	private void OnCharacterCreationInitialized(CharacterCreationManager characterCreationManager)
	{
		_focusToAdd = characterCreationManager.CharacterCreationContent.FocusToAdd;
		_skillLevelToAdd = characterCreationManager.CharacterCreationContent.SkillLevelToAdd;
		_attributeLevelToAdd = characterCreationManager.CharacterCreationContent.AttributeLevelToAdd;
		characterCreationManager.CharacterCreationContent.DefaultSelectedTitleType = "guard";
		characterCreationManager.RegisterCharacterCreationContentHandler(this, 800);
	}

	void ICharacterCreationContentHandler.InitializeContent(CharacterCreationManager characterCreationManager)
	{
		characterCreationManager.CharacterCreationContent.AddEquipmentToUseGetter(delegate(string occupationId, out string equipmentId)
		{
			return _occupationToEquipmentMapping.TryGetValue(occupationId, out equipmentId);
		});
		InitializeCharacterCreationStages(characterCreationManager);
		InitializeCharacterCreationCultures(characterCreationManager);
		InitializeData(characterCreationManager);
	}

	void ICharacterCreationContentHandler.AfterInitializeContent(CharacterCreationManager characterCreationManager)
	{
	}

	void ICharacterCreationContentHandler.OnStageCompleted(CharacterCreationStageBase stage)
	{
		if (stage is CharacterCreationFaceGeneratorStage)
		{
			FaceGenUpdated();
		}
	}

	void ICharacterCreationContentHandler.OnCharacterCreationFinalize(CharacterCreationManager characterCreationManager)
	{
	}

	public void InitializeCharacterCreationStages(CharacterCreationManager characterCreationManager)
	{
		characterCreationManager.AddStage(new CharacterCreationCultureStage());
		characterCreationManager.AddStage(new CharacterCreationFaceGeneratorStage());
		characterCreationManager.AddStage(new CharacterCreationNarrativeStage());
		characterCreationManager.AddStage(new CharacterCreationBannerEditorStage());
		characterCreationManager.AddStage(new CharacterCreationClanNamingStage());
		characterCreationManager.AddStage(new CharacterCreationReviewStage());
		characterCreationManager.AddStage(new CharacterCreationOptionsStage());
	}

	public void InitializeCharacterCreationCultures(CharacterCreationManager characterCreationManager)
	{
		foreach (CultureObject objectType in Game.Current.ObjectManager.GetObjectTypeList<CultureObject>())
		{
			if (objectType.StringId == "aserai" || objectType.StringId == "battania" || objectType.StringId == "empire" || objectType.StringId == "khuzait" || objectType.StringId == "sturgia" || objectType.StringId == "vlandia")
			{
				characterCreationManager.CharacterCreationContent.AddCharacterCreationCulture(objectType, 1, 10);
			}
		}
	}

	public void InitializeData(CharacterCreationManager characterCreationManager)
	{
		characterCreationManager.CharacterCreationContent.ChangeReviewPageDescription(new TextObject("{=W6pKpEoT}You prepare to set off for a grand adventure in Calradia! Here is your character. Continue if you are ready, or go back to make changes."));
		AddParentsMenu(characterCreationManager);
		AddChildhoodMenu(characterCreationManager);
		AddEducationMenu(characterCreationManager);
		AddYouthMenu(characterCreationManager);
		AddAdulthoodMenu(characterCreationManager);
		AddAgeSelectionMenu(characterCreationManager);
	}

	public void FaceGenUpdated()
	{
		CharacterCreationManager characterCreationManager = (GameStateManager.Current.ActiveState as CharacterCreationState).CharacterCreationManager;
		BodyProperties motherBodyProperties;
		BodyProperties fatherBodyProperties;
		FaceGen.GenerateParentKey(fatherBodyProperties = (motherBodyProperties = CharacterObject.PlayerCharacter.GetBodyProperties(CharacterObject.PlayerCharacter.Equipment)), CharacterObject.PlayerCharacter.Race, ref motherBodyProperties, ref fatherBodyProperties);
		motherBodyProperties = new BodyProperties(new DynamicBodyProperties(33f, 0.3f, 0.2f), motherBodyProperties.StaticProperties);
		fatherBodyProperties = new BodyProperties(new DynamicBodyProperties(33f, 0.5f, 0.5f), fatherBodyProperties.StaticProperties);
		foreach (NarrativeMenu narrativeMenu in characterCreationManager.NarrativeMenus)
		{
			foreach (NarrativeMenuCharacter character in narrativeMenu.Characters)
			{
				if (character.StringId.Equals("mother_character"))
				{
					character.UpdateBodyProperties(motherBodyProperties, CharacterObject.PlayerCharacter.Race, isFemale: true);
				}
				if (character.StringId.Equals("father_character"))
				{
					character.UpdateBodyProperties(fatherBodyProperties, CharacterObject.PlayerCharacter.Race, isFemale: false);
				}
				if (character.StringId.Equals("player_childhood_character") || character.StringId.Equals("player_education_character") || character.StringId.Equals("player_youth_character") || character.StringId.Equals("player_adulthood_character") || character.StringId.Equals("player_age_selection_character"))
				{
					character.UpdateBodyProperties(CharacterObject.PlayerCharacter.GetBodyProperties(null), CharacterObject.PlayerCharacter.Race, isFemale: false);
				}
			}
		}
	}

	private List<NarrativeMenuCharacterArgs> GetParentMenuNarrativeMenuCharacterArgs(CultureObject culture, string occupationType, CharacterCreationManager characterCreationManager)
	{
		return new List<NarrativeMenuCharacterArgs>
		{
			new NarrativeMenuCharacterArgs("mother_character", 33, "mother_char_creation_none_" + characterCreationManager.CharacterCreationContent.SelectedCulture.StringId, "act_character_creation_female_default_standing", "spawnpoint_player_1", "", "", null, isHuman: true, isFemale: true),
			new NarrativeMenuCharacterArgs("father_character", 33, "father_char_creation_none_" + characterCreationManager.CharacterCreationContent.SelectedCulture.StringId, "act_character_creation_male_default_standing", "spawnpoint_player_1")
		};
	}

	private void AddParentsMenu(CharacterCreationManager characterCreationManager)
	{
		List<NarrativeMenuCharacter> list = new List<NarrativeMenuCharacter>();
		BodyProperties motherBodyProperties;
		BodyProperties fatherBodyProperties;
		FaceGen.GenerateParentKey(fatherBodyProperties = (motherBodyProperties = CharacterObject.PlayerCharacter.GetBodyProperties(CharacterObject.PlayerCharacter.Equipment)), CharacterObject.PlayerCharacter.Race, ref motherBodyProperties, ref fatherBodyProperties);
		motherBodyProperties = new BodyProperties(new DynamicBodyProperties(33f, 0.3f, 0.2f), motherBodyProperties.StaticProperties);
		fatherBodyProperties = new BodyProperties(new DynamicBodyProperties(33f, 0.5f, 0.5f), fatherBodyProperties.StaticProperties);
		NarrativeMenuCharacter item = new NarrativeMenuCharacter("mother_character", motherBodyProperties, CharacterObject.PlayerCharacter.Race, isFemale: true);
		list.Add(item);
		NarrativeMenuCharacter item2 = new NarrativeMenuCharacter("father_character", fatherBodyProperties, CharacterObject.PlayerCharacter.Race, isFemale: false);
		list.Add(item2);
		NarrativeMenu narrativeMenu = new NarrativeMenu("narrative_parent_menu", "start", "narrative_childhood_menu", new TextObject("{=b4lDDcli}Family"), new TextObject("{=XgFU1pCx}You were born into a family of..."), list, GetParentMenuNarrativeMenuCharacterArgs);
		AddEmpireParentNarrativeMenuOptions(narrativeMenu);
		AddVlandianParentNarrativeMenuOptions(narrativeMenu);
		AddSturgianParentNarrativeMenuOptions(narrativeMenu);
		AddAseraiParentNarrativeMenuOptions(narrativeMenu);
		AddBattaniaNarrativeMenuOptions(narrativeMenu);
		AddKhuzaitNarrativeMenuOptions(narrativeMenu);
		characterCreationManager.AddNewMenu(narrativeMenu);
	}

	private void AddEmpireParentNarrativeMenuOptions(NarrativeMenu narrativeMenu)
	{
		NarrativeMenuOption narrativeMenuOption = new NarrativeMenuOption("empire_lanlord_option", new TextObject("{=InN5ZZt3}A landlord's retainers"), new TextObject("{=ivKl4mV2}Your father was a trusted lieutenant of the local landowning aristocrat. He rode with the lord's cavalry, fighting as an armored lancer."), GetEmpireLandlordNarrativeOptionArgs, EmpireLandlordNarrativeOptionOnCondition, EmpireLandlordNarrativeOptionOnSelect, null);
		narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption);
		NarrativeMenuOption narrativeMenuOption2 = new NarrativeMenuOption("empire_merchant_option", new TextObject("{=651FhzdR}Urban merchants"), new TextObject("{=FQntPChs}Your family were merchants in one of the main cities of the Empire. They sometimes organized caravans to nearby towns, and discussed issues in the town council."), GetEmpireUrbanNarrativeOptionArgs, EmpireUrbanNarrativeOptionOnCondition, EmpireUrbanNarrativeOptionOnSelect, null);
		narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption2);
		NarrativeMenuOption narrativeMenuOption3 = new NarrativeMenuOption("empire_farmer_option", new TextObject("{=sb4gg8Ak}Freeholders"), new TextObject("{=09z8Q08f}Your family were small farmers with just enough land to feed themselves and make a small profit. People like them were the pillars of the imperial rural economy, as well as the backbone of the levy."), GetEmpireFarmerNarrativeOptionArgs, EmpireFarmerNarrativeOptionOnCondition, EmpireFarmerNarrativeOptionOnSelect, null);
		narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption3);
		NarrativeMenuOption narrativeMenuOption4 = new NarrativeMenuOption("empire_artisan_option", new TextObject("{=v48N6h1t}Urban artisans"), new TextObject("{=ueCm5y1C}Your family owned their own workshop in a city, making goods from raw materials brought in from the countryside. Your father played an active if minor role in the town council, and also served in the militia."), GetEmpireArtisanNarrativeOptionArgs, EmpireArtisanNarrativeOptionOnCondition, EmpireArtisanNarrativeOptionOnSelect, null);
		narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption4);
		NarrativeMenuOption narrativeMenuOption5 = new NarrativeMenuOption("empire_hunter_option", new TextObject("{=7eWmU2mF}Foresters"), new TextObject("{=yRFSzSDZ}Your family lived in a village, but did not own their own land. Instead, your father supplemented paid jobs with long trips in the woods, hunting and trapping, always keeping a wary eye for the lord's game wardens."), GetEmpireHunterNarrativeOptionArgs, EmpireHunterNarrativeOptionOnCondition, EmpireHunterNarrativeOptionOnSelect, null);
		narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption5);
		NarrativeMenuOption narrativeMenuOption6 = new NarrativeMenuOption("empire_vagabond_option", new TextObject("{=aEke8dSb}Urban vagabonds"), new TextObject("{=Jvf6K7TZ}Your family numbered among the many poor migrants living in the slums that grow up outside the walls of imperial cities, making whatever money they could from a variety of odd jobs. Sometimes they did service for one of the Empire's many criminal gangs, and you had an early look at the dark side of life."), GetEmpireVagabondNarrativeOptionArgs, EmpireVagabondNarrativeOptionOnCondition, EmpireVagabondNarrativeOptionOnSelect, null);
		narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption6);
	}

	private void GetEmpireLandlordNarrativeOptionArgs(NarrativeMenuOptionArgs args)
	{
		SkillObject[] affectedSkills = new SkillObject[2]
		{
			DefaultSkills.Riding,
			DefaultSkills.Polearm
		};
		args.SetAffectedSkills(affectedSkills);
		args.SetFocusToSkills(_focusToAdd);
		args.SetLevelToSkills(_skillLevelToAdd);
		args.SetLevelToAttribute(DefaultCharacterAttributes.Vigor, _attributeLevelToAdd);
	}

	private bool EmpireLandlordNarrativeOptionOnCondition(CharacterCreationManager characterCreationManager)
	{
		return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "empire";
	}

	private void EmpireLandlordNarrativeOptionOnSelect(CharacterCreationManager characterCreationManager)
	{
		characterCreationManager.CharacterCreationContent.SetParentOccupation("retainer");
		string motherEquipmentId = GetMotherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
		string fatherEquipmentId = GetFatherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
		MBEquipmentRoster motherEquipment = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(motherEquipmentId);
		MBEquipmentRoster fatherEquipment = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(fatherEquipmentId);
		string motherAnimation = "act_character_creation_female_default_side_to_side_1";
		string fatherAnimation = "act_character_creation_male_default_side_to_side_1";
		UpdateParentEquipment(characterCreationManager, motherEquipment, fatherEquipment, motherAnimation, fatherAnimation);
	}

	private void GetEmpireUrbanNarrativeOptionArgs(NarrativeMenuOptionArgs args)
	{
		SkillObject[] affectedSkills = new SkillObject[2]
		{
			DefaultSkills.Trade,
			DefaultSkills.Charm
		};
		args.SetAffectedSkills(affectedSkills);
		args.SetFocusToSkills(_focusToAdd);
		args.SetLevelToSkills(_skillLevelToAdd);
		args.SetLevelToAttribute(DefaultCharacterAttributes.Social, _attributeLevelToAdd);
	}

	private bool EmpireUrbanNarrativeOptionOnCondition(CharacterCreationManager characterCreationManager)
	{
		return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "empire";
	}

	private void EmpireUrbanNarrativeOptionOnSelect(CharacterCreationManager characterCreationManager)
	{
		characterCreationManager.CharacterCreationContent.SetParentOccupation("merchant_urban");
		string motherEquipmentId = GetMotherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
		string fatherEquipmentId = GetFatherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
		MBEquipmentRoster motherEquipment = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(motherEquipmentId);
		MBEquipmentRoster fatherEquipment = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(fatherEquipmentId);
		string motherAnimation = "act_character_creation_female_default_mother_front";
		string fatherAnimation = "act_character_creation_male_default_mother_front";
		UpdateParentEquipment(characterCreationManager, motherEquipment, fatherEquipment, motherAnimation, fatherAnimation);
	}

	private void GetEmpireFarmerNarrativeOptionArgs(NarrativeMenuOptionArgs args)
	{
		SkillObject[] affectedSkills = new SkillObject[2]
		{
			DefaultSkills.Athletics,
			DefaultSkills.Polearm
		};
		args.SetAffectedSkills(affectedSkills);
		args.SetFocusToSkills(_focusToAdd);
		args.SetLevelToSkills(_skillLevelToAdd);
		args.SetLevelToAttribute(DefaultCharacterAttributes.Endurance, _attributeLevelToAdd);
	}

	private bool EmpireFarmerNarrativeOptionOnCondition(CharacterCreationManager characterCreationManager)
	{
		return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "empire";
	}

	private void EmpireFarmerNarrativeOptionOnSelect(CharacterCreationManager characterCreationManager)
	{
		characterCreationManager.CharacterCreationContent.SetParentOccupation("farmer");
		string motherEquipmentId = GetMotherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
		string fatherEquipmentId = GetFatherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
		MBEquipmentRoster motherEquipment = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(motherEquipmentId);
		MBEquipmentRoster fatherEquipment = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(fatherEquipmentId);
		string motherAnimation = "act_character_creation_female_default_father_sitting";
		string fatherAnimation = "act_character_creation_male_default_father_sitting";
		UpdateParentEquipment(characterCreationManager, motherEquipment, fatherEquipment, motherAnimation, fatherAnimation);
	}

	private void GetEmpireArtisanNarrativeOptionArgs(NarrativeMenuOptionArgs args)
	{
		SkillObject[] affectedSkills = new SkillObject[2]
		{
			DefaultSkills.Crafting,
			DefaultSkills.Crossbow
		};
		args.SetAffectedSkills(affectedSkills);
		args.SetFocusToSkills(_focusToAdd);
		args.SetLevelToSkills(_skillLevelToAdd);
		args.SetLevelToAttribute(DefaultCharacterAttributes.Intelligence, _attributeLevelToAdd);
	}

	private bool EmpireArtisanNarrativeOptionOnCondition(CharacterCreationManager characterCreationManager)
	{
		return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "empire";
	}

	private void EmpireArtisanNarrativeOptionOnSelect(CharacterCreationManager characterCreationManager)
	{
		characterCreationManager.CharacterCreationContent.SetParentOccupation("artisan_urban");
		string motherEquipmentId = GetMotherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
		string fatherEquipmentId = GetFatherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
		MBEquipmentRoster motherEquipment = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(motherEquipmentId);
		MBEquipmentRoster fatherEquipment = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(fatherEquipmentId);
		string motherAnimation = "act_character_creation_female_default_side_to_side_2";
		string fatherAnimation = "act_character_creation_male_default_side_to_side_2";
		UpdateParentEquipment(characterCreationManager, motherEquipment, fatherEquipment, motherAnimation, fatherAnimation);
	}

	private void GetEmpireHunterNarrativeOptionArgs(NarrativeMenuOptionArgs args)
	{
		SkillObject[] affectedSkills = new SkillObject[2]
		{
			DefaultSkills.Scouting,
			DefaultSkills.Bow
		};
		args.SetAffectedSkills(affectedSkills);
		args.SetFocusToSkills(_focusToAdd);
		args.SetLevelToSkills(_skillLevelToAdd);
		args.SetLevelToAttribute(DefaultCharacterAttributes.Control, _attributeLevelToAdd);
	}

	private bool EmpireHunterNarrativeOptionOnCondition(CharacterCreationManager characterCreationManager)
	{
		return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "empire";
	}

	private void EmpireHunterNarrativeOptionOnSelect(CharacterCreationManager characterCreationManager)
	{
		characterCreationManager.CharacterCreationContent.SetParentOccupation("hunter");
		string motherEquipmentId = GetMotherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
		string fatherEquipmentId = GetFatherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
		MBEquipmentRoster motherEquipment = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(motherEquipmentId);
		MBEquipmentRoster fatherEquipment = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(fatherEquipmentId);
		string motherAnimation = "act_character_creation_female_default_side_to_side_3";
		string fatherAnimation = "act_character_creation_male_default_side_to_side_3";
		UpdateParentEquipment(characterCreationManager, motherEquipment, fatherEquipment, motherAnimation, fatherAnimation);
	}

	private void GetEmpireVagabondNarrativeOptionArgs(NarrativeMenuOptionArgs args)
	{
		SkillObject[] affectedSkills = new SkillObject[2]
		{
			DefaultSkills.Roguery,
			DefaultSkills.Throwing
		};
		args.SetAffectedSkills(affectedSkills);
		args.SetFocusToSkills(_focusToAdd);
		args.SetLevelToSkills(_skillLevelToAdd);
		args.SetLevelToAttribute(DefaultCharacterAttributes.Cunning, _attributeLevelToAdd);
	}

	private bool EmpireVagabondNarrativeOptionOnCondition(CharacterCreationManager characterCreationManager)
	{
		return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "empire";
	}

	private void EmpireVagabondNarrativeOptionOnSelect(CharacterCreationManager characterCreationManager)
	{
		characterCreationManager.CharacterCreationContent.SetParentOccupation("vagabond_urban");
		string motherEquipmentId = GetMotherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
		string fatherEquipmentId = GetFatherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
		MBEquipmentRoster motherEquipment = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(motherEquipmentId);
		MBEquipmentRoster fatherEquipment = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(fatherEquipmentId);
		string motherAnimation = "act_character_creation_female_default_hugging";
		string fatherAnimation = "act_character_creation_male_default_hugging";
		UpdateParentEquipment(characterCreationManager, motherEquipment, fatherEquipment, motherAnimation, fatherAnimation);
	}

	public void UpdateParentEquipment(CharacterCreationManager characterCreationManager, MBEquipmentRoster motherEquipment, MBEquipmentRoster fatherEquipment, string motherAnimation, string fatherAnimation)
	{
		foreach (NarrativeMenuCharacter character in characterCreationManager.CurrentMenu.Characters)
		{
			if (character.StringId.Equals("mother_character"))
			{
				character.SetEquipment(motherEquipment);
				character.SetAnimationId(motherAnimation);
			}
			if (character.StringId.Equals("father_character"))
			{
				character.SetEquipment(fatherEquipment);
				character.SetAnimationId(fatherAnimation);
			}
		}
	}

	private void AddVlandianParentNarrativeMenuOptions(NarrativeMenu narrativeMenu)
	{
		NarrativeMenuOption narrativeMenuOption = new NarrativeMenuOption("vlandia_retainer_option", new TextObject("{=2TptWc4m}A baron's retainers"), new TextObject("{=0Suu1Q9q}Your father was a bailiff for a local feudal magnate. He looked after his liege's estates, resolved disputes in the village, and helped train the village levy. He rode with the lord's cavalry, fighting as an armored knight."), GetVlandiaRetainerNarrativeOptionArgs, VlandiaRetainerNarrativeOptionOnCondition, VlandiaRetainerNarrativeOptionOnSelect, null);
		narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption);
		NarrativeMenuOption narrativeMenuOption2 = new NarrativeMenuOption("vlandia_merchant_option", new TextObject("{=651FhzdR}Urban merchants"), new TextObject("{=qNZFkxJb}Your family were merchants in one of the main cities of the kingdom. They organized caravans to nearby towns and were active in the local merchant's guild."), GetVlandiaMerchantNarrativeOptionArgs, VlandiaMerchantNarrativeOptionOnCondition, VlandiaMerchantNarrativeOptionOnSelect, null);
		narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption2);
		NarrativeMenuOption narrativeMenuOption3 = new NarrativeMenuOption("vlandia_farmer_option", new TextObject("{=RDfXuVxT}Yeomen"), new TextObject("{=BLZ4mdhb}Your family were small farmers with just enough land to feed themselves and make a small profit. People like them were the pillars of the kingdom's economy, as well as the backbone of the levy."), GetVlandiaFarmerNarrativeOptionArgs, VlandiaFarmerNarrativeOptionOnCondition, VlandiaFarmerNarrativeOptionOnSelect, null);
		narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption3);
		NarrativeMenuOption narrativeMenuOption4 = new NarrativeMenuOption("vlandia_blacksmith_option", new TextObject("{=p2KIhGbE}Urban blacksmith"), new TextObject("{=btsMpRcA}Your family owned a smithy in a city. Your father played an active if minor role in the town council, and also served in the militia."), GetVlandiaBlacksmithNarrativeOptionArgs, VlandiaBlacksmithNarrativeOptionOnCondition, VlandiaBlacksmithNarrativeOptionOnSelect, null);
		narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption4);
		NarrativeMenuOption narrativeMenuOption5 = new NarrativeMenuOption("vlandia_hunter_option", new TextObject("{=YcnK0Thk}Hunters"), new TextObject("{=yRFSzSDZ}Your family lived in a village, but did not own their own land. Instead, your father supplemented paid jobs with long trips in the woods, hunting and trapping, always keeping a wary eye for the lord's game wardens."), GetVlandiaHunterNarrativeOptionArgs, VlandiaHunterNarrativeOptionOnCondition, VlandiaHunterNarrativeOptionOnSelect, null);
		narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption5);
		NarrativeMenuOption narrativeMenuOption6 = new NarrativeMenuOption("vlandia_mercenary_option", new TextObject("{=ipQP6aVi}Mercenaries"), new TextObject("{=yYhX6JQC}Your father joined one of Vlandia's many mercenary companies, composed of men who got such a taste for war in their lord's service that they never took well to peace. Their crossbowmen were much valued across Calradia. Your mother was a camp follower, taking you along in the wake of bloody campaigns."), GetVlandiaMercenaryNarrativeOptionArgs, VlandiaMercenaryNarrativeOptionOnCondition, VlandiaMercenaryNarrativeOptionOnSelect, null);
		narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption6);
	}

	private void GetVlandiaRetainerNarrativeOptionArgs(NarrativeMenuOptionArgs args)
	{
		SkillObject[] affectedSkills = new SkillObject[2]
		{
			DefaultSkills.Riding,
			DefaultSkills.Polearm
		};
		args.SetAffectedSkills(affectedSkills);
		args.SetFocusToSkills(_focusToAdd);
		args.SetLevelToSkills(_skillLevelToAdd);
		args.SetLevelToAttribute(DefaultCharacterAttributes.Social, _attributeLevelToAdd);
	}

	private bool VlandiaRetainerNarrativeOptionOnCondition(CharacterCreationManager characterCreationManager)
	{
		return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "vlandia";
	}

	private void VlandiaRetainerNarrativeOptionOnSelect(CharacterCreationManager characterCreationManager)
	{
		characterCreationManager.CharacterCreationContent.SetParentOccupation("retainer");
		string motherEquipmentId = GetMotherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
		string fatherEquipmentId = GetFatherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
		MBEquipmentRoster motherEquipment = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(motherEquipmentId);
		MBEquipmentRoster fatherEquipment = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(fatherEquipmentId);
		string motherAnimation = "act_character_creation_female_default_side_to_side_1";
		string fatherAnimation = "act_character_creation_male_default_side_to_side_1";
		UpdateParentEquipment(characterCreationManager, motherEquipment, fatherEquipment, motherAnimation, fatherAnimation);
	}

	private void GetVlandiaMerchantNarrativeOptionArgs(NarrativeMenuOptionArgs args)
	{
		SkillObject[] affectedSkills = new SkillObject[2]
		{
			DefaultSkills.Trade,
			DefaultSkills.Charm
		};
		args.SetAffectedSkills(affectedSkills);
		args.SetFocusToSkills(_focusToAdd);
		args.SetLevelToSkills(_skillLevelToAdd);
		args.SetLevelToAttribute(DefaultCharacterAttributes.Intelligence, _attributeLevelToAdd);
	}

	private bool VlandiaMerchantNarrativeOptionOnCondition(CharacterCreationManager characterCreationManager)
	{
		return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "vlandia";
	}

	private void VlandiaMerchantNarrativeOptionOnSelect(CharacterCreationManager characterCreationManager)
	{
		characterCreationManager.CharacterCreationContent.SetParentOccupation("merchant_urban");
		string motherEquipmentId = GetMotherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
		string fatherEquipmentId = GetFatherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
		MBEquipmentRoster motherEquipment = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(motherEquipmentId);
		MBEquipmentRoster fatherEquipment = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(fatherEquipmentId);
		string motherAnimation = "act_character_creation_female_default_mother_front";
		string fatherAnimation = "act_character_creation_male_default_mother_front";
		UpdateParentEquipment(characterCreationManager, motherEquipment, fatherEquipment, motherAnimation, fatherAnimation);
	}

	private void GetVlandiaFarmerNarrativeOptionArgs(NarrativeMenuOptionArgs args)
	{
		SkillObject[] affectedSkills = new SkillObject[2]
		{
			DefaultSkills.Polearm,
			DefaultSkills.Crossbow
		};
		args.SetAffectedSkills(affectedSkills);
		args.SetFocusToSkills(_focusToAdd);
		args.SetLevelToSkills(_skillLevelToAdd);
		args.SetLevelToAttribute(DefaultCharacterAttributes.Endurance, _attributeLevelToAdd);
	}

	private bool VlandiaFarmerNarrativeOptionOnCondition(CharacterCreationManager characterCreationManager)
	{
		return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "vlandia";
	}

	private void VlandiaFarmerNarrativeOptionOnSelect(CharacterCreationManager characterCreationManager)
	{
		characterCreationManager.CharacterCreationContent.SetParentOccupation("farmer");
		string motherEquipmentId = GetMotherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
		string fatherEquipmentId = GetFatherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
		MBEquipmentRoster motherEquipment = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(motherEquipmentId);
		MBEquipmentRoster fatherEquipment = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(fatherEquipmentId);
		string motherAnimation = "act_character_creation_female_default_father_sitting";
		string fatherAnimation = "act_character_creation_male_default_father_sitting";
		UpdateParentEquipment(characterCreationManager, motherEquipment, fatherEquipment, motherAnimation, fatherAnimation);
	}

	private void GetVlandiaBlacksmithNarrativeOptionArgs(NarrativeMenuOptionArgs args)
	{
		SkillObject[] affectedSkills = new SkillObject[2]
		{
			DefaultSkills.Crafting,
			DefaultSkills.TwoHanded
		};
		args.SetAffectedSkills(affectedSkills);
		args.SetFocusToSkills(_focusToAdd);
		args.SetLevelToSkills(_skillLevelToAdd);
		args.SetLevelToAttribute(DefaultCharacterAttributes.Vigor, _attributeLevelToAdd);
	}

	private bool VlandiaBlacksmithNarrativeOptionOnCondition(CharacterCreationManager characterCreationManager)
	{
		return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "vlandia";
	}

	private void VlandiaBlacksmithNarrativeOptionOnSelect(CharacterCreationManager characterCreationManager)
	{
		characterCreationManager.CharacterCreationContent.SetParentOccupation("artisan_urban");
		string motherEquipmentId = GetMotherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
		string fatherEquipmentId = GetFatherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
		MBEquipmentRoster motherEquipment = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(motherEquipmentId);
		MBEquipmentRoster fatherEquipment = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(fatherEquipmentId);
		string motherAnimation = "act_character_creation_female_default_side_to_side_2";
		string fatherAnimation = "act_character_creation_male_default_side_to_side_2";
		UpdateParentEquipment(characterCreationManager, motherEquipment, fatherEquipment, motherAnimation, fatherAnimation);
	}

	private void GetVlandiaHunterNarrativeOptionArgs(NarrativeMenuOptionArgs args)
	{
		SkillObject[] affectedSkills = new SkillObject[2]
		{
			DefaultSkills.Scouting,
			DefaultSkills.Crossbow
		};
		args.SetAffectedSkills(affectedSkills);
		args.SetFocusToSkills(_focusToAdd);
		args.SetLevelToSkills(_skillLevelToAdd);
		args.SetLevelToAttribute(DefaultCharacterAttributes.Control, _attributeLevelToAdd);
	}

	private bool VlandiaHunterNarrativeOptionOnCondition(CharacterCreationManager characterCreationManager)
	{
		return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "vlandia";
	}

	private void VlandiaHunterNarrativeOptionOnSelect(CharacterCreationManager characterCreationManager)
	{
		characterCreationManager.CharacterCreationContent.SetParentOccupation("hunter");
		string motherEquipmentId = GetMotherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
		string fatherEquipmentId = GetFatherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
		MBEquipmentRoster motherEquipment = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(motherEquipmentId);
		MBEquipmentRoster fatherEquipment = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(fatherEquipmentId);
		string motherAnimation = "act_character_creation_female_default_side_to_side_3";
		string fatherAnimation = "act_character_creation_male_default_side_to_side_3";
		UpdateParentEquipment(characterCreationManager, motherEquipment, fatherEquipment, motherAnimation, fatherAnimation);
	}

	private void GetVlandiaMercenaryNarrativeOptionArgs(NarrativeMenuOptionArgs args)
	{
		SkillObject[] affectedSkills = new SkillObject[2]
		{
			DefaultSkills.Roguery,
			DefaultSkills.Crossbow
		};
		args.SetAffectedSkills(affectedSkills);
		args.SetFocusToSkills(_focusToAdd);
		args.SetLevelToSkills(_skillLevelToAdd);
		args.SetLevelToAttribute(DefaultCharacterAttributes.Cunning, _attributeLevelToAdd);
	}

	private bool VlandiaMercenaryNarrativeOptionOnCondition(CharacterCreationManager characterCreationManager)
	{
		return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "vlandia";
	}

	private void VlandiaMercenaryNarrativeOptionOnSelect(CharacterCreationManager characterCreationManager)
	{
		characterCreationManager.CharacterCreationContent.SetParentOccupation("merchant_urban");
		string motherEquipmentId = GetMotherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
		string fatherEquipmentId = GetFatherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
		MBEquipmentRoster motherEquipment = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(motherEquipmentId);
		MBEquipmentRoster fatherEquipment = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(fatherEquipmentId);
		string motherAnimation = "act_character_creation_female_default_hugging";
		string fatherAnimation = "act_character_creation_male_default_hugging";
		UpdateParentEquipment(characterCreationManager, motherEquipment, fatherEquipment, motherAnimation, fatherAnimation);
	}

	private void AddSturgianParentNarrativeMenuOptions(NarrativeMenu narrativeMenu)
	{
		NarrativeMenuOption narrativeMenuOption = new NarrativeMenuOption("sturgia_companion_option", new TextObject("{=mc78FEbA}A boyar's companions"), new TextObject("{=hob3WVkU}Your father was a member of a boyar's druzhina, the 'companions' that make up his retinue. He sat at his lord's table in the great hall, oversaw the boyar's estates, and stood by his side in the center of the shield wall in battle."), GetSturgiaCompanionNarrativeOptionArgs, SturgiaCompanionNarrativeOptionOnCondition, SturgiaCompanionNarrativeOptionOnSelect, null);
		narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption);
		NarrativeMenuOption narrativeMenuOption2 = new NarrativeMenuOption("sturgia_trader_option", new TextObject("{=HqzVBfpl}Urban traders"), new TextObject("{=bjVMtW3W}Your family were merchants who lived in one of Sturgia's great river ports, organizing the shipment of the north's bounty of furs, honey and other goods to faraway lands."), GetSturgiaTraderNarrativeOptionArgs, SturgiaTraderNarrativeOptionOnCondition, SturgiaTraderNarrativeOptionOnSelect, null);
		narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption2);
		NarrativeMenuOption narrativeMenuOption3 = new NarrativeMenuOption("sturgia_farmer_option", new TextObject("{=zrpqSWSh}Free farmers"), new TextObject("{=Mcd3ZyKq}Your family had just enough land to feed themselves and make a small profit. People like them were the pillars of the kingdom's economy, as well as the backbone of the levy."), GetSturgiaFarmerNarrativeOptionArgs, SturgiaFarmerNarrativeOptionOnCondition, SturgiaFarmerNarrativeOptionOnSelect, null);
		narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption3);
		NarrativeMenuOption narrativeMenuOption4 = new NarrativeMenuOption("sturgia_artisan_option", new TextObject("{=v48N6h1t}Urban artisans"), new TextObject("{=ueCm5y1C}Your family owned their own workshop in a city, making goods from raw materials brought in from the countryside. Your father played an active if minor role in the town council, and also served in the militia."), GetSturgiaArtisanNarrativeOptionArgs, SturgiaArtisanNarrativeOptionOnCondition, SturgiaArtisanNarrativeOptionOnSelect, null);
		narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption4);
		NarrativeMenuOption narrativeMenuOption5 = new NarrativeMenuOption("sturgia_hunter_option", new TextObject("{=YcnK0Thk}Hunters"), new TextObject("{=WyZ2UtFF}Your family had no taste for the authority of the boyars. They made their living deep in the woods, slashing and burning fields which they tended for a year or two before moving on. They hunted and trapped fox, hare, ermine, and other fur-bearing animals."), GetSturgiaHunterNarrativeOptionArgs, SturgiaHunterNarrativeOptionOnCondition, SturgiaHunterNarrativeOptionOnSelect, null);
		narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption5);
		NarrativeMenuOption narrativeMenuOption6 = new NarrativeMenuOption("sturgia_vagabond_option", new TextObject("{=TPoK3GSj}Vagabonds"), new TextObject("{=2SDWhGmQ}Your family numbered among the poor migrants living in the slums that grow up outside the walls of the river cities, making whatever money they could from a variety of odd jobs. Sometimes they did services for one of the region's many criminal gangs."), GetSturgiaVagabondNarrativeOptionArgs, SturgiaVagabondNarrativeOptionOnCondition, SturgiaVagabondNarrativeOptionOnSelect, null);
		narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption6);
	}

	private void GetSturgiaCompanionNarrativeOptionArgs(NarrativeMenuOptionArgs args)
	{
		SkillObject[] affectedSkills = new SkillObject[2]
		{
			DefaultSkills.Riding,
			DefaultSkills.TwoHanded
		};
		args.SetAffectedSkills(affectedSkills);
		args.SetFocusToSkills(_focusToAdd);
		args.SetLevelToSkills(_skillLevelToAdd);
		args.SetLevelToAttribute(DefaultCharacterAttributes.Social, _attributeLevelToAdd);
	}

	private bool SturgiaCompanionNarrativeOptionOnCondition(CharacterCreationManager characterCreationManager)
	{
		return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "sturgia";
	}

	private void SturgiaCompanionNarrativeOptionOnSelect(CharacterCreationManager characterCreationManager)
	{
		characterCreationManager.CharacterCreationContent.SetParentOccupation("retainer");
		string motherEquipmentId = GetMotherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
		string fatherEquipmentId = GetFatherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
		MBEquipmentRoster motherEquipment = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(motherEquipmentId);
		MBEquipmentRoster fatherEquipment = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(fatherEquipmentId);
		string motherAnimation = "act_character_creation_female_default_side_to_side_1";
		string fatherAnimation = "act_character_creation_male_default_side_to_side_1";
		UpdateParentEquipment(characterCreationManager, motherEquipment, fatherEquipment, motherAnimation, fatherAnimation);
	}

	private void GetSturgiaTraderNarrativeOptionArgs(NarrativeMenuOptionArgs args)
	{
		SkillObject[] affectedSkills = new SkillObject[2]
		{
			DefaultSkills.Trade,
			DefaultSkills.Tactics
		};
		args.SetAffectedSkills(affectedSkills);
		args.SetFocusToSkills(_focusToAdd);
		args.SetLevelToSkills(_skillLevelToAdd);
		args.SetLevelToAttribute(DefaultCharacterAttributes.Cunning, _attributeLevelToAdd);
	}

	private bool SturgiaTraderNarrativeOptionOnCondition(CharacterCreationManager characterCreationManager)
	{
		return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "sturgia";
	}

	private void SturgiaTraderNarrativeOptionOnSelect(CharacterCreationManager characterCreationManager)
	{
		characterCreationManager.CharacterCreationContent.SetParentOccupation("merchant_urban");
		string motherEquipmentId = GetMotherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
		string fatherEquipmentId = GetFatherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
		MBEquipmentRoster motherEquipment = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(motherEquipmentId);
		MBEquipmentRoster fatherEquipment = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(fatherEquipmentId);
		string motherAnimation = "act_character_creation_female_default_mother_front";
		string fatherAnimation = "act_character_creation_male_default_mother_front";
		UpdateParentEquipment(characterCreationManager, motherEquipment, fatherEquipment, motherAnimation, fatherAnimation);
	}

	private void GetSturgiaFarmerNarrativeOptionArgs(NarrativeMenuOptionArgs args)
	{
		SkillObject[] affectedSkills = new SkillObject[2]
		{
			DefaultSkills.Athletics,
			DefaultSkills.Polearm
		};
		args.SetAffectedSkills(affectedSkills);
		args.SetFocusToSkills(_focusToAdd);
		args.SetLevelToSkills(_skillLevelToAdd);
		args.SetLevelToAttribute(DefaultCharacterAttributes.Endurance, _attributeLevelToAdd);
	}

	private bool SturgiaFarmerNarrativeOptionOnCondition(CharacterCreationManager characterCreationManager)
	{
		return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "sturgia";
	}

	private void SturgiaFarmerNarrativeOptionOnSelect(CharacterCreationManager characterCreationManager)
	{
		characterCreationManager.CharacterCreationContent.SetParentOccupation("farmer");
		string motherEquipmentId = GetMotherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
		string fatherEquipmentId = GetFatherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
		MBEquipmentRoster motherEquipment = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(motherEquipmentId);
		MBEquipmentRoster fatherEquipment = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(fatherEquipmentId);
		string motherAnimation = "act_character_creation_female_default_father_sitting";
		string fatherAnimation = "act_character_creation_male_default_father_sitting";
		UpdateParentEquipment(characterCreationManager, motherEquipment, fatherEquipment, motherAnimation, fatherAnimation);
	}

	private void GetSturgiaArtisanNarrativeOptionArgs(NarrativeMenuOptionArgs args)
	{
		SkillObject[] affectedSkills = new SkillObject[2]
		{
			DefaultSkills.Crafting,
			DefaultSkills.OneHanded
		};
		args.SetAffectedSkills(affectedSkills);
		args.SetFocusToSkills(_focusToAdd);
		args.SetLevelToSkills(_skillLevelToAdd);
		args.SetLevelToAttribute(DefaultCharacterAttributes.Intelligence, _attributeLevelToAdd);
	}

	private bool SturgiaArtisanNarrativeOptionOnCondition(CharacterCreationManager characterCreationManager)
	{
		return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "sturgia";
	}

	private void SturgiaArtisanNarrativeOptionOnSelect(CharacterCreationManager characterCreationManager)
	{
		characterCreationManager.CharacterCreationContent.SetParentOccupation("artisan_urban");
		string motherEquipmentId = GetMotherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
		string fatherEquipmentId = GetFatherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
		MBEquipmentRoster motherEquipment = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(motherEquipmentId);
		MBEquipmentRoster fatherEquipment = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(fatherEquipmentId);
		string motherAnimation = "act_character_creation_female_default_side_to_side_2";
		string fatherAnimation = "act_character_creation_male_default_side_to_side_2";
		UpdateParentEquipment(characterCreationManager, motherEquipment, fatherEquipment, motherAnimation, fatherAnimation);
	}

	private void GetSturgiaHunterNarrativeOptionArgs(NarrativeMenuOptionArgs args)
	{
		SkillObject[] affectedSkills = new SkillObject[2]
		{
			DefaultSkills.Scouting,
			DefaultSkills.Bow
		};
		args.SetAffectedSkills(affectedSkills);
		args.SetFocusToSkills(_focusToAdd);
		args.SetLevelToSkills(_skillLevelToAdd);
		args.SetLevelToAttribute(DefaultCharacterAttributes.Vigor, _attributeLevelToAdd);
	}

	private bool SturgiaHunterNarrativeOptionOnCondition(CharacterCreationManager characterCreationManager)
	{
		return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "sturgia";
	}

	private void SturgiaHunterNarrativeOptionOnSelect(CharacterCreationManager characterCreationManager)
	{
		characterCreationManager.CharacterCreationContent.SetParentOccupation("hunter");
		string motherEquipmentId = GetMotherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
		string fatherEquipmentId = GetFatherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
		MBEquipmentRoster motherEquipment = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(motherEquipmentId);
		MBEquipmentRoster fatherEquipment = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(fatherEquipmentId);
		string motherAnimation = "act_character_creation_female_default_side_to_side_3";
		string fatherAnimation = "act_character_creation_male_default_side_to_side_3";
		UpdateParentEquipment(characterCreationManager, motherEquipment, fatherEquipment, motherAnimation, fatherAnimation);
	}

	private void GetSturgiaVagabondNarrativeOptionArgs(NarrativeMenuOptionArgs args)
	{
		SkillObject[] affectedSkills = new SkillObject[2]
		{
			DefaultSkills.Roguery,
			DefaultSkills.Throwing
		};
		args.SetAffectedSkills(affectedSkills);
		args.SetFocusToSkills(_focusToAdd);
		args.SetLevelToSkills(_skillLevelToAdd);
		args.SetLevelToAttribute(DefaultCharacterAttributes.Control, _attributeLevelToAdd);
	}

	private bool SturgiaVagabondNarrativeOptionOnCondition(CharacterCreationManager characterCreationManager)
	{
		return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "sturgia";
	}

	private void SturgiaVagabondNarrativeOptionOnSelect(CharacterCreationManager characterCreationManager)
	{
		characterCreationManager.CharacterCreationContent.SetParentOccupation("vagabond_urban");
		string motherEquipmentId = GetMotherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
		string fatherEquipmentId = GetFatherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
		MBEquipmentRoster motherEquipment = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(motherEquipmentId);
		MBEquipmentRoster fatherEquipment = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(fatherEquipmentId);
		string motherAnimation = "act_character_creation_female_default_hugging";
		string fatherAnimation = "act_character_creation_male_default_hugging";
		UpdateParentEquipment(characterCreationManager, motherEquipment, fatherEquipment, motherAnimation, fatherAnimation);
	}

	private void AddAseraiParentNarrativeMenuOptions(NarrativeMenu narrativeMenu)
	{
		NarrativeMenuOption narrativeMenuOption = new NarrativeMenuOption("aserai_kinsfolk_option", new TextObject("{=Sw8OxnNr}Kinsfolk of an emir"), new TextObject("{=MFrIHJZM}Your family was from a smaller offshoot of an emir's tribe. Your father's land gave him enough income to afford a horse but he was not quite wealthy enough to buy the armor needed to join the heavier cavalry. He fought as one of the light horsemen for which the desert is famous."), GetAseraiKinsfolkNarrativeOptionArgs, AseraiKinsfolkNarrativeOptionOnCondition, AseraiKinsfolkNarrativeOptionOnSelect, null);
		narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption);
		NarrativeMenuOption narrativeMenuOption2 = new NarrativeMenuOption("aserai_slave_option", new TextObject("{=ngFVgwDD}Warrior-slaves"), new TextObject("{=GsPC2MgU}Your father was part of one of the slave-bodyguards maintained by the Aserai emirs. He fought by his master's side with tribe's armored cavalry, and was freed - perhaps for an act of valor, or perhaps he paid for his freedom with his share of the spoils of battle. He then married your mother."), GetAseraiSlaveNarrativeOptionArgs, AseraiSlaveNarrativeOptionOnCondition, AseraiSlaveNarrativeOptionOnSelect, null);
		narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption2);
		NarrativeMenuOption narrativeMenuOption3 = new NarrativeMenuOption("aserai_physician_option", new TextObject("{=bgy8LVvY}Physician"), new TextObject("{=BhQlmQoj}Your family were respected physicians in an oasis town. They set bones and cured the sick, and their skills were in much demand. They were respected in the higher echelons of society too."), GetAseraiPhysicianNarrativeOptionArgs, AseraiPhysicianNarrativeOptionOnCondition, AseraiPhysicianNarrativeOptionOnSelect, null);
		narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption3);
		NarrativeMenuOption narrativeMenuOption4 = new NarrativeMenuOption("aserai_farmer_option", new TextObject("{=g31pXuqi}Oasis farmers"), new TextObject("{=5P0KqBAw}Your family tilled the soil in one of the oases of the Nahasa and tended the palm orchards that produced the desert's famous dates. Your father was a member of the main foot levy of his tribe, fighting with his kinsmen under the emir's banner."), GetAseraiFarmerNarrativeOptionArgs, AseraiFarmerNarrativeOptionOnCondition, AseraiFarmerNarrativeOptionOnSelect, null);
		narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption4);
		NarrativeMenuOption narrativeMenuOption5 = new NarrativeMenuOption("aserai_herder_option", new TextObject("{=EEedqolz}Bedouin"), new TextObject("{=PKhcPbBX}Your family were part of a nomadic clan, crisscrossing the wastes between wadi beds and wells to feed their herds of goats and camels on the scraggly scrubs of the Nahasa."), GetAseraiHerderNarrativeOptionArgs, AseraiHerderNarrativeOptionOnCondition, AseraiHerderNarrativeOptionOnSelect, null);
		narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption5);
		NarrativeMenuOption narrativeMenuOption6 = new NarrativeMenuOption("aserai_artisan_option", new TextObject("{=tRIrbTvv}Urban back-alley thugs"), new TextObject("{=6bUSbsKC}Your father worked for a fitiwi, one of the strongmen who keep order in the poorer quarters of the oasis towns. He resolved disputes over land, dice and insults, imposing his authority with the fitiwi's traditional staff."), GetAseraiArtisanNarrativeOptionArgs, AseraiArtisanNarrativeOptionOnCondition, AseraiArtisanNarrativeOptionOnSelect, null);
		narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption6);
	}

	private void GetAseraiKinsfolkNarrativeOptionArgs(NarrativeMenuOptionArgs args)
	{
		SkillObject[] affectedSkills = new SkillObject[2]
		{
			DefaultSkills.Riding,
			DefaultSkills.Throwing
		};
		args.SetAffectedSkills(affectedSkills);
		args.SetFocusToSkills(_focusToAdd);
		args.SetLevelToSkills(_skillLevelToAdd);
		args.SetLevelToAttribute(DefaultCharacterAttributes.Social, _attributeLevelToAdd);
	}

	private bool AseraiKinsfolkNarrativeOptionOnCondition(CharacterCreationManager characterCreationManager)
	{
		return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "aserai";
	}

	private void AseraiKinsfolkNarrativeOptionOnSelect(CharacterCreationManager characterCreationManager)
	{
		characterCreationManager.CharacterCreationContent.SetParentOccupation("retainer");
		string motherEquipmentId = GetMotherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
		string fatherEquipmentId = GetFatherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
		MBEquipmentRoster motherEquipment = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(motherEquipmentId);
		MBEquipmentRoster fatherEquipment = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(fatherEquipmentId);
		string motherAnimation = "act_character_creation_female_default_side_to_side_1";
		string fatherAnimation = "act_character_creation_male_default_side_to_side_1";
		UpdateParentEquipment(characterCreationManager, motherEquipment, fatherEquipment, motherAnimation, fatherAnimation);
	}

	private void GetAseraiSlaveNarrativeOptionArgs(NarrativeMenuOptionArgs args)
	{
		SkillObject[] affectedSkills = new SkillObject[2]
		{
			DefaultSkills.Riding,
			DefaultSkills.Polearm
		};
		args.SetAffectedSkills(affectedSkills);
		args.SetFocusToSkills(_focusToAdd);
		args.SetLevelToSkills(_skillLevelToAdd);
		args.SetLevelToAttribute(DefaultCharacterAttributes.Vigor, _attributeLevelToAdd);
	}

	private bool AseraiSlaveNarrativeOptionOnCondition(CharacterCreationManager characterCreationManager)
	{
		return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "aserai";
	}

	private void AseraiSlaveNarrativeOptionOnSelect(CharacterCreationManager characterCreationManager)
	{
		characterCreationManager.CharacterCreationContent.SetParentOccupation("mercenary_urban");
		string motherEquipmentId = GetMotherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
		string fatherEquipmentId = GetFatherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
		MBEquipmentRoster motherEquipment = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(motherEquipmentId);
		MBEquipmentRoster fatherEquipment = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(fatherEquipmentId);
		string motherAnimation = "act_character_creation_female_default_mother_front";
		string fatherAnimation = "act_character_creation_male_default_mother_front";
		UpdateParentEquipment(characterCreationManager, motherEquipment, fatherEquipment, motherAnimation, fatherAnimation);
	}

	private void GetAseraiPhysicianNarrativeOptionArgs(NarrativeMenuOptionArgs args)
	{
		SkillObject[] affectedSkills = new SkillObject[2]
		{
			DefaultSkills.Medicine,
			DefaultSkills.Charm
		};
		args.SetAffectedSkills(affectedSkills);
		args.SetFocusToSkills(_focusToAdd);
		args.SetLevelToSkills(_skillLevelToAdd);
		args.SetLevelToAttribute(DefaultCharacterAttributes.Intelligence, _attributeLevelToAdd);
	}

	private bool AseraiPhysicianNarrativeOptionOnCondition(CharacterCreationManager characterCreationManager)
	{
		return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "aserai";
	}

	private void AseraiPhysicianNarrativeOptionOnSelect(CharacterCreationManager characterCreationManager)
	{
		characterCreationManager.CharacterCreationContent.SetParentOccupation("physician_urban");
		string motherEquipmentId = GetMotherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
		string fatherEquipmentId = GetFatherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
		MBEquipmentRoster motherEquipment = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(motherEquipmentId);
		MBEquipmentRoster fatherEquipment = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(fatherEquipmentId);
		string motherAnimation = "act_character_creation_female_default_father_sitting";
		string fatherAnimation = "act_character_creation_male_default_father_sitting";
		UpdateParentEquipment(characterCreationManager, motherEquipment, fatherEquipment, motherAnimation, fatherAnimation);
	}

	private void GetAseraiFarmerNarrativeOptionArgs(NarrativeMenuOptionArgs args)
	{
		SkillObject[] affectedSkills = new SkillObject[2]
		{
			DefaultSkills.Athletics,
			DefaultSkills.OneHanded
		};
		args.SetAffectedSkills(affectedSkills);
		args.SetFocusToSkills(_focusToAdd);
		args.SetLevelToSkills(_skillLevelToAdd);
		args.SetLevelToAttribute(DefaultCharacterAttributes.Endurance, _attributeLevelToAdd);
	}

	private bool AseraiFarmerNarrativeOptionOnCondition(CharacterCreationManager characterCreationManager)
	{
		return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "aserai";
	}

	private void AseraiFarmerNarrativeOptionOnSelect(CharacterCreationManager characterCreationManager)
	{
		characterCreationManager.CharacterCreationContent.SetParentOccupation("farmer");
		string motherEquipmentId = GetMotherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
		string fatherEquipmentId = GetFatherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
		MBEquipmentRoster motherEquipment = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(motherEquipmentId);
		MBEquipmentRoster fatherEquipment = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(fatherEquipmentId);
		string motherAnimation = "act_character_creation_female_default_side_to_side_2";
		string fatherAnimation = "act_character_creation_male_default_side_to_side_2";
		UpdateParentEquipment(characterCreationManager, motherEquipment, fatherEquipment, motherAnimation, fatherAnimation);
	}

	private void GetAseraiHerderNarrativeOptionArgs(NarrativeMenuOptionArgs args)
	{
		SkillObject[] affectedSkills = new SkillObject[2]
		{
			DefaultSkills.Scouting,
			DefaultSkills.Bow
		};
		args.SetAffectedSkills(affectedSkills);
		args.SetFocusToSkills(_focusToAdd);
		args.SetLevelToSkills(_skillLevelToAdd);
		args.SetLevelToAttribute(DefaultCharacterAttributes.Cunning, _attributeLevelToAdd);
	}

	private bool AseraiHerderNarrativeOptionOnCondition(CharacterCreationManager characterCreationManager)
	{
		return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "aserai";
	}

	private void AseraiHerderNarrativeOptionOnSelect(CharacterCreationManager characterCreationManager)
	{
		characterCreationManager.CharacterCreationContent.SetParentOccupation("herder");
		string motherEquipmentId = GetMotherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
		string fatherEquipmentId = GetFatherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
		MBEquipmentRoster motherEquipment = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(motherEquipmentId);
		MBEquipmentRoster fatherEquipment = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(fatherEquipmentId);
		string motherAnimation = "act_character_creation_female_default_side_to_side_3";
		string fatherAnimation = "act_character_creation_male_default_side_to_side_3";
		UpdateParentEquipment(characterCreationManager, motherEquipment, fatherEquipment, motherAnimation, fatherAnimation);
	}

	private void GetAseraiArtisanNarrativeOptionArgs(NarrativeMenuOptionArgs args)
	{
		SkillObject[] affectedSkills = new SkillObject[2]
		{
			DefaultSkills.Roguery,
			DefaultSkills.Polearm
		};
		args.SetAffectedSkills(affectedSkills);
		args.SetFocusToSkills(_focusToAdd);
		args.SetLevelToSkills(_skillLevelToAdd);
		args.SetLevelToAttribute(DefaultCharacterAttributes.Control, _attributeLevelToAdd);
	}

	private bool AseraiArtisanNarrativeOptionOnCondition(CharacterCreationManager characterCreationManager)
	{
		return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "aserai";
	}

	private void AseraiArtisanNarrativeOptionOnSelect(CharacterCreationManager characterCreationManager)
	{
		characterCreationManager.CharacterCreationContent.SetParentOccupation("artisan_urban");
		string motherEquipmentId = GetMotherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
		string fatherEquipmentId = GetFatherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
		MBEquipmentRoster motherEquipment = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(motherEquipmentId);
		MBEquipmentRoster fatherEquipment = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(fatherEquipmentId);
		string motherAnimation = "act_character_creation_female_default_hugging";
		string fatherAnimation = "act_character_creation_male_default_hugging";
		UpdateParentEquipment(characterCreationManager, motherEquipment, fatherEquipment, motherAnimation, fatherAnimation);
	}

	private void AddBattaniaNarrativeMenuOptions(NarrativeMenu narrativeMenu)
	{
		NarrativeMenuOption narrativeMenuOption = new NarrativeMenuOption("battania_retainer_option", new TextObject("{=GeNKQlHR}Members of the chieftain's hearthguard"), new TextObject("{=LpH8SYFL}Your family were the trusted kinfolk of a Battanian chieftain, and sat at his table in his great hall. Your father assisted his chief in running the affairs of the clan and trained with the traditional weapons of the Battanian elite, the two-handed sword or falx and the bow."), GetBattaniaRetainerNarrativeOptionArgs, BattaniaRetainerNarrativeOptionOnCondition, BattaniaRetainerNarrativeOptionOnSelect, null);
		narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption);
		NarrativeMenuOption narrativeMenuOption2 = new NarrativeMenuOption("battania_healer_option", new TextObject("{=AeBzTj6w}Healers"), new TextObject("{=j6py5Rv5}Your parents were healers who gathered herbs and treated the sick. As a living reservoir of Battanian tradition, they were also asked to adjudicate many disputes between the clans."), GetBattaniaHealerNarrativeOptionArgs, BattaniaHealerNarrativeOptionOnCondition, BattaniaHealerNarrativeOptionOnSelect, null);
		narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption2);
		NarrativeMenuOption narrativeMenuOption3 = new NarrativeMenuOption("battania_farmer_option", new TextObject("{=tGEStbxb}Tribespeople"), new TextObject("{=WchH8bS2}Your family were middle-ranking members of a Battanian clan, who tilled their own land. Your father fought with the kern, the main body of his people's warriors, joining in the screaming charges for which the Battanians were famous."), GetBattaniaFarmerNarrativeOptionArgs, BattaniaFarmerNarrativeOptionOnCondition, BattaniaFarmerNarrativeOptionOnSelect, null);
		narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption3);
		NarrativeMenuOption narrativeMenuOption4 = new NarrativeMenuOption("battania_artisan_option", new TextObject("{=BCU6RezA}Smiths"), new TextObject("{=kg9YtrOg}Your family were smiths, a revered profession among the Battanians. They crafted everything from fine filigree jewelry in geometric designs to the well-balanced longswords favored by the Battanian aristocracy."), GetBattaniaArtisanNarrativeOptionArgs, BattaniaArtisanNarrativeOptionOnCondition, BattaniaArtisanNarrativeOptionOnSelect, null);
		narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption4);
		NarrativeMenuOption narrativeMenuOption5 = new NarrativeMenuOption("battania_hunter_option", new TextObject("{=7eWmU2mF}Foresters"), new TextObject("{=7jBroUUQ}Your family had little land of their own, so they earned their living from the woods, hunting and trapping. They taught you from an early age that skills like finding game trails and killing an animal with one shot could make the difference between eating and starvation."), GetBattaniaHunterNarrativeOptionArgs, BattaniaHunterNarrativeOptionOnCondition, BattaniaHunterNarrativeOptionOnSelect, null);
		narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption5);
		NarrativeMenuOption narrativeMenuOption6 = new NarrativeMenuOption("battania_bard_option", new TextObject("{=SpJqhEEh}Bards"), new TextObject("{=aVzcyhhy}Your father was a bard, drifting from chieftain's hall to chieftain's hall making his living singing the praises of one Battanian aristocrat and mocking his enemies, then going to his enemy's hall and doing the reverse. You learned from him that a clever tongue could spare you  from a life toiling in the fields, if you kept your wits about you."), GetBattaniaBardNarrativeOptionArgs, BattaniaBardNarrativeOptionOnCondition, BattaniaBardNarrativeOptionOnSelect, null);
		narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption6);
	}

	private void GetBattaniaRetainerNarrativeOptionArgs(NarrativeMenuOptionArgs args)
	{
		SkillObject[] affectedSkills = new SkillObject[2]
		{
			DefaultSkills.TwoHanded,
			DefaultSkills.Bow
		};
		args.SetAffectedSkills(affectedSkills);
		args.SetFocusToSkills(_focusToAdd);
		args.SetLevelToSkills(_skillLevelToAdd);
		args.SetLevelToAttribute(DefaultCharacterAttributes.Vigor, _attributeLevelToAdd);
	}

	private bool BattaniaRetainerNarrativeOptionOnCondition(CharacterCreationManager characterCreationManager)
	{
		return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "battania";
	}

	private void BattaniaRetainerNarrativeOptionOnSelect(CharacterCreationManager characterCreationManager)
	{
		characterCreationManager.CharacterCreationContent.SetParentOccupation("retainer_urban");
		string motherEquipmentId = GetMotherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
		string fatherEquipmentId = GetFatherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
		MBEquipmentRoster motherEquipment = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(motherEquipmentId);
		MBEquipmentRoster fatherEquipment = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(fatherEquipmentId);
		string motherAnimation = "act_character_creation_female_default_side_to_side_1";
		string fatherAnimation = "act_character_creation_male_default_side_to_side_1";
		UpdateParentEquipment(characterCreationManager, motherEquipment, fatherEquipment, motherAnimation, fatherAnimation);
	}

	private void GetBattaniaHealerNarrativeOptionArgs(NarrativeMenuOptionArgs args)
	{
		SkillObject[] affectedSkills = new SkillObject[2]
		{
			DefaultSkills.Medicine,
			DefaultSkills.Charm
		};
		args.SetAffectedSkills(affectedSkills);
		args.SetFocusToSkills(_focusToAdd);
		args.SetLevelToSkills(_skillLevelToAdd);
		args.SetLevelToAttribute(DefaultCharacterAttributes.Intelligence, _attributeLevelToAdd);
	}

	private bool BattaniaHealerNarrativeOptionOnCondition(CharacterCreationManager characterCreationManager)
	{
		return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "battania";
	}

	private void BattaniaHealerNarrativeOptionOnSelect(CharacterCreationManager characterCreationManager)
	{
		characterCreationManager.CharacterCreationContent.SetParentOccupation("healer");
		string motherEquipmentId = GetMotherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
		string fatherEquipmentId = GetFatherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
		MBEquipmentRoster motherEquipment = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(motherEquipmentId);
		MBEquipmentRoster fatherEquipment = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(fatherEquipmentId);
		string motherAnimation = "act_character_creation_female_default_mother_front";
		string fatherAnimation = "act_character_creation_male_default_mother_front";
		UpdateParentEquipment(characterCreationManager, motherEquipment, fatherEquipment, motherAnimation, fatherAnimation);
	}

	private void GetBattaniaFarmerNarrativeOptionArgs(NarrativeMenuOptionArgs args)
	{
		SkillObject[] affectedSkills = new SkillObject[2]
		{
			DefaultSkills.Athletics,
			DefaultSkills.Throwing
		};
		args.SetAffectedSkills(affectedSkills);
		args.SetFocusToSkills(_focusToAdd);
		args.SetLevelToSkills(_skillLevelToAdd);
		args.SetLevelToAttribute(DefaultCharacterAttributes.Control, _attributeLevelToAdd);
	}

	private bool BattaniaFarmerNarrativeOptionOnCondition(CharacterCreationManager characterCreationManager)
	{
		return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "battania";
	}

	private void BattaniaFarmerNarrativeOptionOnSelect(CharacterCreationManager characterCreationManager)
	{
		characterCreationManager.CharacterCreationContent.SetParentOccupation("farmer");
		string motherEquipmentId = GetMotherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
		string fatherEquipmentId = GetFatherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
		MBEquipmentRoster motherEquipment = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(motherEquipmentId);
		MBEquipmentRoster fatherEquipment = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(fatherEquipmentId);
		string motherAnimation = "act_character_creation_female_default_father_sitting";
		string fatherAnimation = "act_character_creation_male_default_father_sitting";
		UpdateParentEquipment(characterCreationManager, motherEquipment, fatherEquipment, motherAnimation, fatherAnimation);
	}

	private void GetBattaniaArtisanNarrativeOptionArgs(NarrativeMenuOptionArgs args)
	{
		SkillObject[] affectedSkills = new SkillObject[2]
		{
			DefaultSkills.Crafting,
			DefaultSkills.TwoHanded
		};
		args.SetAffectedSkills(affectedSkills);
		args.SetFocusToSkills(_focusToAdd);
		args.SetLevelToSkills(_skillLevelToAdd);
		args.SetLevelToAttribute(DefaultCharacterAttributes.Endurance, _attributeLevelToAdd);
	}

	private bool BattaniaArtisanNarrativeOptionOnCondition(CharacterCreationManager characterCreationManager)
	{
		return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "battania";
	}

	private void BattaniaArtisanNarrativeOptionOnSelect(CharacterCreationManager characterCreationManager)
	{
		characterCreationManager.CharacterCreationContent.SetParentOccupation("artisan_urban");
		string motherEquipmentId = GetMotherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
		string fatherEquipmentId = GetFatherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
		MBEquipmentRoster motherEquipment = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(motherEquipmentId);
		MBEquipmentRoster fatherEquipment = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(fatherEquipmentId);
		string motherAnimation = "act_character_creation_female_default_side_to_side_2";
		string fatherAnimation = "act_character_creation_male_default_side_to_side_2";
		UpdateParentEquipment(characterCreationManager, motherEquipment, fatherEquipment, motherAnimation, fatherAnimation);
	}

	private void GetBattaniaHunterNarrativeOptionArgs(NarrativeMenuOptionArgs args)
	{
		SkillObject[] affectedSkills = new SkillObject[2]
		{
			DefaultSkills.Scouting,
			DefaultSkills.Tactics
		};
		args.SetAffectedSkills(affectedSkills);
		args.SetFocusToSkills(_focusToAdd);
		args.SetLevelToSkills(_skillLevelToAdd);
		args.SetLevelToAttribute(DefaultCharacterAttributes.Cunning, _attributeLevelToAdd);
	}

	private bool BattaniaHunterNarrativeOptionOnCondition(CharacterCreationManager characterCreationManager)
	{
		return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "battania";
	}

	private void BattaniaHunterNarrativeOptionOnSelect(CharacterCreationManager characterCreationManager)
	{
		characterCreationManager.CharacterCreationContent.SetParentOccupation("hunter");
		string motherEquipmentId = GetMotherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
		string fatherEquipmentId = GetFatherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
		MBEquipmentRoster motherEquipment = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(motherEquipmentId);
		MBEquipmentRoster fatherEquipment = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(fatherEquipmentId);
		string motherAnimation = "act_character_creation_female_default_side_to_side_3";
		string fatherAnimation = "act_character_creation_male_default_side_to_side_3";
		UpdateParentEquipment(characterCreationManager, motherEquipment, fatherEquipment, motherAnimation, fatherAnimation);
	}

	private void GetBattaniaBardNarrativeOptionArgs(NarrativeMenuOptionArgs args)
	{
		SkillObject[] affectedSkills = new SkillObject[2]
		{
			DefaultSkills.Roguery,
			DefaultSkills.Charm
		};
		args.SetAffectedSkills(affectedSkills);
		args.SetFocusToSkills(_focusToAdd);
		args.SetLevelToSkills(_skillLevelToAdd);
		args.SetLevelToAttribute(DefaultCharacterAttributes.Social, _attributeLevelToAdd);
	}

	private bool BattaniaBardNarrativeOptionOnCondition(CharacterCreationManager characterCreationManager)
	{
		return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "battania";
	}

	private void BattaniaBardNarrativeOptionOnSelect(CharacterCreationManager characterCreationManager)
	{
		characterCreationManager.CharacterCreationContent.SetParentOccupation("bard_urban");
		string motherEquipmentId = GetMotherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
		string fatherEquipmentId = GetFatherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
		MBEquipmentRoster motherEquipment = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(motherEquipmentId);
		MBEquipmentRoster fatherEquipment = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(fatherEquipmentId);
		string motherAnimation = "act_character_creation_female_default_hugging";
		string fatherAnimation = "act_character_creation_male_default_hugging";
		UpdateParentEquipment(characterCreationManager, motherEquipment, fatherEquipment, motherAnimation, fatherAnimation);
	}

	private void AddKhuzaitNarrativeMenuOptions(NarrativeMenu narrativeMenu)
	{
		NarrativeMenuOption narrativeMenuOption = new NarrativeMenuOption("khuzait_retainer_option", new TextObject("{=FVaRDe2a}A noyan's kinsfolk"), new TextObject("{=jAs3kDXh}Your family were the trusted kinsfolk of a Khuzait noyan, and shared his meals in the chieftain's yurt. Your father assisted his chief in running the affairs of the clan and fought in the core of armored lancers in the center of the Khuzait battle line."), GetKhuzaitRetainerNarrativeOptionArgs, KhuzaitRetainerNarrativeOptionOnCondition, KhuzaitRetainerNarrativeOptionOnSelect, null);
		narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption);
		NarrativeMenuOption narrativeMenuOption2 = new NarrativeMenuOption("khuzait_merhant_option", new TextObject("{=TkgLEDRM}Merchants"), new TextObject("{=qPg3IDiq}Your family came from one of the merchant clans that dominated the cities in eastern Calradia before the Khuzait conquest. They adjusted quickly to their new masters, keeping the caravan routes running and ensuring that the tariff revenues that once went into imperial coffers now flowed to the khanate."), GetKhuzaitMerchantNarrativeOptionArgs, KhuzaitMerchantNarrativeOptionOnCondition, KhuzaitMerchantNarrativeOptionOnSelect, null);
		narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption2);
		NarrativeMenuOption narrativeMenuOption3 = new NarrativeMenuOption("khuzait_mercenary_option", new TextObject("{=tGEStbxb}Tribespeople"), new TextObject("{=URgZ4ai4}Your family were middle-ranking members of one of the Khuzait clans. He had some herds of his own, but was not rich. When the Khuzait horde was summoned to battle, he fought with the horse archers, shooting and wheeling and wearing down the enemy before the lancers delivered the final punch."), GetKhuzaitHerderNarrativeOptionArgs, KhuzaitHerderNarrativeOptionOnCondition, KhuzaitHerderNarrativeOptionOnSelect, null);
		narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption3);
		NarrativeMenuOption narrativeMenuOption4 = new NarrativeMenuOption("khuzait_farmer_option", new TextObject("{=gQ2tAvCz}Farmers"), new TextObject("{=5QSGoRFj}Your family tilled one of the small patches of arable land in the steppes for generations. When the Khuzaits came, they ceased paying taxes to the emperor and providing conscripts for his army, and served the khan instead."), GetKhuzaitFarmerNarrativeOptionArgs, KhuzaitFarmerNarrativeOptionOnCondition, KhuzaitFarmerNarrativeOptionOnSelect, null);
		narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption4);
		NarrativeMenuOption narrativeMenuOption5 = new NarrativeMenuOption("khuzait_healer_option", new TextObject("{=vfhVveLW}Shamans"), new TextObject("{=WOKNhaG2}Your family were guardians of the sacred traditions of the Khuzaits, channelling the spirits of the wilderness and of the ancestors. They tended the sick and dispensed wisdom, resolving disputes and providing practical advice."), GetKhuzaitHealerNarrativeOptionArgs, KhuzaitHealerNarrativeOptionOnCondition, KhuzaitHealerNarrativeOptionOnSelect, null);
		narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption5);
		NarrativeMenuOption narrativeMenuOption6 = new NarrativeMenuOption("khuzait_herder_option", new TextObject("{=Xqba1Obq}Nomads"), new TextObject("{=9aoQYpZs}Your family's clan never pledged its loyalty to the khan and never settled down, preferring to live out in the deep steppe away from his authority. They remain some of the finest trackers and scouts in the grasslands, as the ability to spot an enemy coming and move quickly is often all that protects their herds from their neighbors' predations."), GetKhuzaitNomadHerderNarrativeOptionArgs, KhuzaitNomadHerderNarrativeOptionOnCondition, KhuzaitNomadHerderNarrativeOptionOnSelect, null);
		narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption6);
	}

	private void GetKhuzaitRetainerNarrativeOptionArgs(NarrativeMenuOptionArgs args)
	{
		SkillObject[] affectedSkills = new SkillObject[2]
		{
			DefaultSkills.Riding,
			DefaultSkills.Polearm
		};
		args.SetAffectedSkills(affectedSkills);
		args.SetFocusToSkills(_focusToAdd);
		args.SetLevelToSkills(_skillLevelToAdd);
		args.SetLevelToAttribute(DefaultCharacterAttributes.Endurance, _attributeLevelToAdd);
	}

	private bool KhuzaitRetainerNarrativeOptionOnCondition(CharacterCreationManager characterCreationManager)
	{
		return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "khuzait";
	}

	private void KhuzaitRetainerNarrativeOptionOnSelect(CharacterCreationManager characterCreationManager)
	{
		characterCreationManager.CharacterCreationContent.SetParentOccupation("retainer_urban");
		string motherEquipmentId = GetMotherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
		string fatherEquipmentId = GetFatherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
		MBEquipmentRoster motherEquipment = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(motherEquipmentId);
		MBEquipmentRoster fatherEquipment = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(fatherEquipmentId);
		string motherAnimation = "act_character_creation_female_default_side_to_side_1";
		string fatherAnimation = "act_character_creation_male_default_side_to_side_1";
		UpdateParentEquipment(characterCreationManager, motherEquipment, fatherEquipment, motherAnimation, fatherAnimation);
	}

	private void GetKhuzaitMerchantNarrativeOptionArgs(NarrativeMenuOptionArgs args)
	{
		SkillObject[] affectedSkills = new SkillObject[2]
		{
			DefaultSkills.Trade,
			DefaultSkills.Charm
		};
		args.SetAffectedSkills(affectedSkills);
		args.SetFocusToSkills(_focusToAdd);
		args.SetLevelToSkills(_skillLevelToAdd);
		args.SetLevelToAttribute(DefaultCharacterAttributes.Social, _attributeLevelToAdd);
	}

	private bool KhuzaitMerchantNarrativeOptionOnCondition(CharacterCreationManager characterCreationManager)
	{
		return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "khuzait";
	}

	private void KhuzaitMerchantNarrativeOptionOnSelect(CharacterCreationManager characterCreationManager)
	{
		characterCreationManager.CharacterCreationContent.SetParentOccupation("merchant_urban");
		string motherEquipmentId = GetMotherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
		string fatherEquipmentId = GetFatherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
		MBEquipmentRoster motherEquipment = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(motherEquipmentId);
		MBEquipmentRoster fatherEquipment = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(fatherEquipmentId);
		string motherAnimation = "act_character_creation_female_default_mother_front";
		string fatherAnimation = "act_character_creation_male_default_mother_front";
		UpdateParentEquipment(characterCreationManager, motherEquipment, fatherEquipment, motherAnimation, fatherAnimation);
	}

	private void GetKhuzaitHerderNarrativeOptionArgs(NarrativeMenuOptionArgs args)
	{
		SkillObject[] affectedSkills = new SkillObject[2]
		{
			DefaultSkills.Bow,
			DefaultSkills.Riding
		};
		args.SetAffectedSkills(affectedSkills);
		args.SetFocusToSkills(_focusToAdd);
		args.SetLevelToSkills(_skillLevelToAdd);
		args.SetLevelToAttribute(DefaultCharacterAttributes.Control, _attributeLevelToAdd);
	}

	private bool KhuzaitHerderNarrativeOptionOnCondition(CharacterCreationManager characterCreationManager)
	{
		return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "khuzait";
	}

	private void KhuzaitHerderNarrativeOptionOnSelect(CharacterCreationManager characterCreationManager)
	{
		characterCreationManager.CharacterCreationContent.SetParentOccupation("herder");
		string motherEquipmentId = GetMotherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
		string fatherEquipmentId = GetFatherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
		MBEquipmentRoster motherEquipment = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(motherEquipmentId);
		MBEquipmentRoster fatherEquipment = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(fatherEquipmentId);
		string motherAnimation = "act_character_creation_female_default_father_sitting";
		string fatherAnimation = "act_character_creation_male_default_father_sitting";
		UpdateParentEquipment(characterCreationManager, motherEquipment, fatherEquipment, motherAnimation, fatherAnimation);
	}

	private void GetKhuzaitFarmerNarrativeOptionArgs(NarrativeMenuOptionArgs args)
	{
		SkillObject[] affectedSkills = new SkillObject[2]
		{
			DefaultSkills.Polearm,
			DefaultSkills.Throwing
		};
		args.SetAffectedSkills(affectedSkills);
		args.SetFocusToSkills(_focusToAdd);
		args.SetLevelToSkills(_skillLevelToAdd);
		args.SetLevelToAttribute(DefaultCharacterAttributes.Vigor, _attributeLevelToAdd);
	}

	private bool KhuzaitFarmerNarrativeOptionOnCondition(CharacterCreationManager characterCreationManager)
	{
		return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "khuzait";
	}

	private void KhuzaitFarmerNarrativeOptionOnSelect(CharacterCreationManager characterCreationManager)
	{
		characterCreationManager.CharacterCreationContent.SetParentOccupation("farmer");
		string motherEquipmentId = GetMotherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
		string fatherEquipmentId = GetFatherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
		MBEquipmentRoster motherEquipment = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(motherEquipmentId);
		MBEquipmentRoster fatherEquipment = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(fatherEquipmentId);
		string motherAnimation = "act_character_creation_female_default_side_to_side_2";
		string fatherAnimation = "act_character_creation_male_default_side_to_side_2";
		UpdateParentEquipment(characterCreationManager, motherEquipment, fatherEquipment, motherAnimation, fatherAnimation);
	}

	private void GetKhuzaitHealerNarrativeOptionArgs(NarrativeMenuOptionArgs args)
	{
		SkillObject[] affectedSkills = new SkillObject[2]
		{
			DefaultSkills.Medicine,
			DefaultSkills.Charm
		};
		args.SetAffectedSkills(affectedSkills);
		args.SetFocusToSkills(_focusToAdd);
		args.SetLevelToSkills(_skillLevelToAdd);
		args.SetLevelToAttribute(DefaultCharacterAttributes.Intelligence, _attributeLevelToAdd);
	}

	private bool KhuzaitHealerNarrativeOptionOnCondition(CharacterCreationManager characterCreationManager)
	{
		return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "khuzait";
	}

	private void KhuzaitHealerNarrativeOptionOnSelect(CharacterCreationManager characterCreationManager)
	{
		characterCreationManager.CharacterCreationContent.SetParentOccupation("healer_urban");
		string motherEquipmentId = GetMotherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
		string fatherEquipmentId = GetFatherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
		MBEquipmentRoster motherEquipment = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(motherEquipmentId);
		MBEquipmentRoster fatherEquipment = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(fatherEquipmentId);
		string motherAnimation = "act_character_creation_female_default_side_to_side_3";
		string fatherAnimation = "act_character_creation_male_default_side_to_side_3";
		UpdateParentEquipment(characterCreationManager, motherEquipment, fatherEquipment, motherAnimation, fatherAnimation);
	}

	private void GetKhuzaitNomadHerderNarrativeOptionArgs(NarrativeMenuOptionArgs args)
	{
		SkillObject[] affectedSkills = new SkillObject[2]
		{
			DefaultSkills.Scouting,
			DefaultSkills.Riding
		};
		args.SetAffectedSkills(affectedSkills);
		args.SetFocusToSkills(_focusToAdd);
		args.SetLevelToSkills(_skillLevelToAdd);
		args.SetLevelToAttribute(DefaultCharacterAttributes.Cunning, _attributeLevelToAdd);
	}

	private bool KhuzaitNomadHerderNarrativeOptionOnCondition(CharacterCreationManager characterCreationManager)
	{
		return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "khuzait";
	}

	private void KhuzaitNomadHerderNarrativeOptionOnSelect(CharacterCreationManager characterCreationManager)
	{
		characterCreationManager.CharacterCreationContent.SetParentOccupation("herder");
		string motherEquipmentId = GetMotherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
		string fatherEquipmentId = GetFatherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
		MBEquipmentRoster motherEquipment = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(motherEquipmentId);
		MBEquipmentRoster fatherEquipment = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(fatherEquipmentId);
		string motherAnimation = "act_character_creation_female_default_hugging";
		string fatherAnimation = "act_character_creation_male_default_hugging";
		UpdateParentEquipment(characterCreationManager, motherEquipment, fatherEquipment, motherAnimation, fatherAnimation);
	}

	private List<NarrativeMenuCharacterArgs> GetChildhoodMenuNarrativeMenuCharacterArgs(CultureObject culture, string occupationType, CharacterCreationManager characterCreationManager)
	{
		List<NarrativeMenuCharacterArgs> list = new List<NarrativeMenuCharacterArgs>();
		string playerChildhoodAgeEquipmentId = GetPlayerChildhoodAgeEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId, Hero.MainHero.IsFemale);
		list.Add(new NarrativeMenuCharacterArgs("player_childhood_character", 7, playerChildhoodAgeEquipmentId, "act_childhood_schooled", "spawnpoint_player_1", "", "", null, isHuman: true, CharacterObject.PlayerCharacter.IsFemale));
		return list;
	}

	private void AddChildhoodMenu(CharacterCreationManager characterCreationManager)
	{
		List<NarrativeMenuCharacter> list = new List<NarrativeMenuCharacter>();
		BodyProperties originalBodyProperties = CharacterObject.PlayerCharacter.GetBodyProperties(CharacterObject.PlayerCharacter.Equipment);
		originalBodyProperties = FaceGen.GetBodyPropertiesWithAge(ref originalBodyProperties, 7f);
		NarrativeMenuCharacter item = new NarrativeMenuCharacter("player_childhood_character", originalBodyProperties, CharacterObject.PlayerCharacter.Race, CharacterObject.PlayerCharacter.IsFemale);
		list.Add(item);
		NarrativeMenu narrativeMenu = new NarrativeMenu("narrative_childhood_menu", "narrative_parent_menu", "narrative_education_menu", new TextObject("{=8Yiwt1z6}Early Childhood"), new TextObject("{=character_creation_content_16}As a child you were noted for..."), list, GetChildhoodMenuNarrativeMenuCharacterArgs);
		AddChildhoodNarrativeMenuOptions(narrativeMenu);
		characterCreationManager.AddNewMenu(narrativeMenu);
	}

	private void AddChildhoodNarrativeMenuOptions(NarrativeMenu narrativeMenu)
	{
		NarrativeMenuOption narrativeMenuOption = new NarrativeMenuOption("childhood_leadership_option", new TextObject("{=kmM68Qx4}your leadership skills."), new TextObject("{=FfNwXtii}If the wolf pup gang of your early childhood had an alpha, it was definitely you. All the other kids followed your lead as you decided what to play and where to play, and led them in games and mischief."), GetChildhoodLeadershipOptionArgs, ChildhoodLeadershipOptionOnCondition, ChildhoodLeadershipOptionOnSelect, null);
		narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption);
		NarrativeMenuOption narrativeMenuOption2 = new NarrativeMenuOption("childhood_brawn_option", new TextObject("{=5HXS8HEY}your brawn."), new TextObject("{=YKzuGc54}You were big, and other children looked to have you around in any scrap with children from a neighboring village. You pushed a plough and threw an axe like an adult."), GetChildhoodBrawnOptionArgs, ChildhoodBrawnOptionOnCondition, ChildhoodBrawnOptionOnSelect, null);
		narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption2);
		NarrativeMenuOption narrativeMenuOption3 = new NarrativeMenuOption("childhood_detail_option", new TextObject("{=QrYjPUEf}your attention to detail."), new TextObject("{=JUSHAPnu}You were quick on your feet and attentive to what was going on around you. Usually you could run away from trouble, though you could give a good account of yourself in a fight with other children if cornered."), GetChildhoodDetailOptionArgs, ChildhoodDetailOptionOnCondition, ChildhoodDetailOptionOnSelect, null);
		narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption3);
		NarrativeMenuOption narrativeMenuOption4 = new NarrativeMenuOption("childhood_smart_option", new TextObject("{=Y3UcaX74}your aptitude for numbers."), new TextObject("{=DFidSjIf}Most children around you had only the most rudimentary education, but you lingered after class to study letters and mathematics. You were fascinated by the marketplace - weights and measures, tallies and accounts, the chatter about profits and losses."), GetChildhoodSmartOptionArgs, ChildhoodSmartOptionOnCondition, ChildhoodSmartOptionOnSelect, null);
		narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption4);
		NarrativeMenuOption narrativeMenuOption5 = new NarrativeMenuOption("childhood_leader_option", new TextObject("{=GEYzLuwb}your way with people."), new TextObject("{=w2TEQq26}You were always attentive to other people, good at guessing their motivations. You studied how individuals were swayed, and tried out what you learned from adults on your friends."), GetChildhoodLeaderOptionArgs, ChildhoodLeaderOptionOnCondition, ChildhoodLeaderOptionOnSelect, null);
		narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption5);
		NarrativeMenuOption narrativeMenuOption6 = new NarrativeMenuOption("childhood_horse_option", new TextObject("{=MEgLE2kj}your skill with horses."), new TextObject("{=ngazFofr}You were always drawn to animals, and spent as much time as possible hanging out in the village stables. You could calm horses, and were sometimes called upon to break in new colts. You learned the basics of veterinary arts, much of which is applicable to humans as well."), GetChildhoodHorseOptionArgs, ChildhoodHorseOptionOnCondition, ChildhoodHorseOptionOnSelect, null);
		narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption6);
	}

	private void GetChildhoodLeadershipOptionArgs(NarrativeMenuOptionArgs args)
	{
		SkillObject[] affectedSkills = new SkillObject[2]
		{
			DefaultSkills.Leadership,
			DefaultSkills.Tactics
		};
		args.SetAffectedSkills(affectedSkills);
		args.SetFocusToSkills(_focusToAdd);
		args.SetLevelToSkills(_skillLevelToAdd);
		args.SetLevelToAttribute(DefaultCharacterAttributes.Cunning, _attributeLevelToAdd);
	}

	private bool ChildhoodLeadershipOptionOnCondition(CharacterCreationManager characterCreationManager)
	{
		return true;
	}

	private void ChildhoodLeadershipOptionOnSelect(CharacterCreationManager characterCreationManager)
	{
		foreach (NarrativeMenuCharacter character in characterCreationManager.CurrentMenu.Characters)
		{
			if (character.StringId == "player_childhood_character")
			{
				character.SetAnimationId("act_childhood_leader");
			}
		}
	}

	private void GetChildhoodBrawnOptionArgs(NarrativeMenuOptionArgs args)
	{
		SkillObject[] affectedSkills = new SkillObject[2]
		{
			DefaultSkills.TwoHanded,
			DefaultSkills.Throwing
		};
		args.SetAffectedSkills(affectedSkills);
		args.SetFocusToSkills(_focusToAdd);
		args.SetLevelToSkills(_skillLevelToAdd);
		args.SetLevelToAttribute(DefaultCharacterAttributes.Vigor, _attributeLevelToAdd);
	}

	private bool ChildhoodBrawnOptionOnCondition(CharacterCreationManager characterCreationManager)
	{
		return true;
	}

	private void ChildhoodBrawnOptionOnSelect(CharacterCreationManager characterCreationManager)
	{
		foreach (NarrativeMenuCharacter character in characterCreationManager.CurrentMenu.Characters)
		{
			if (character.StringId == "player_childhood_character")
			{
				character.SetAnimationId("act_childhood_athlete");
			}
		}
	}

	private void GetChildhoodDetailOptionArgs(NarrativeMenuOptionArgs args)
	{
		SkillObject[] affectedSkills = new SkillObject[2]
		{
			DefaultSkills.Athletics,
			DefaultSkills.Bow
		};
		args.SetAffectedSkills(affectedSkills);
		args.SetFocusToSkills(_focusToAdd);
		args.SetLevelToSkills(_skillLevelToAdd);
		args.SetLevelToAttribute(DefaultCharacterAttributes.Control, _attributeLevelToAdd);
	}

	private bool ChildhoodDetailOptionOnCondition(CharacterCreationManager characterCreationManager)
	{
		return true;
	}

	private void ChildhoodDetailOptionOnSelect(CharacterCreationManager characterCreationManager)
	{
		foreach (NarrativeMenuCharacter character in characterCreationManager.CurrentMenu.Characters)
		{
			if (character.StringId == "player_childhood_character")
			{
				character.SetAnimationId("act_childhood_memory");
			}
		}
	}

	private void GetChildhoodSmartOptionArgs(NarrativeMenuOptionArgs args)
	{
		SkillObject[] affectedSkills = new SkillObject[2]
		{
			DefaultSkills.Engineering,
			DefaultSkills.Trade
		};
		args.SetAffectedSkills(affectedSkills);
		args.SetFocusToSkills(_focusToAdd);
		args.SetLevelToSkills(_skillLevelToAdd);
		args.SetLevelToAttribute(DefaultCharacterAttributes.Intelligence, _attributeLevelToAdd);
	}

	private bool ChildhoodSmartOptionOnCondition(CharacterCreationManager characterCreationManager)
	{
		return true;
	}

	private void ChildhoodSmartOptionOnSelect(CharacterCreationManager characterCreationManager)
	{
		foreach (NarrativeMenuCharacter character in characterCreationManager.CurrentMenu.Characters)
		{
			if (character.StringId == "player_childhood_character")
			{
				character.SetAnimationId("act_childhood_numbers");
			}
		}
	}

	private void GetChildhoodLeaderOptionArgs(NarrativeMenuOptionArgs args)
	{
		SkillObject[] affectedSkills = new SkillObject[2]
		{
			DefaultSkills.Charm,
			DefaultSkills.Leadership
		};
		args.SetAffectedSkills(affectedSkills);
		args.SetFocusToSkills(_focusToAdd);
		args.SetLevelToSkills(_skillLevelToAdd);
		args.SetLevelToAttribute(DefaultCharacterAttributes.Social, _attributeLevelToAdd);
	}

	private bool ChildhoodLeaderOptionOnCondition(CharacterCreationManager characterCreationManager)
	{
		return true;
	}

	private void ChildhoodLeaderOptionOnSelect(CharacterCreationManager characterCreationManager)
	{
		foreach (NarrativeMenuCharacter character in characterCreationManager.CurrentMenu.Characters)
		{
			if (character.StringId == "player_childhood_character")
			{
				character.SetAnimationId("act_childhood_manners");
			}
		}
	}

	private void GetChildhoodHorseOptionArgs(NarrativeMenuOptionArgs args)
	{
		SkillObject[] affectedSkills = new SkillObject[2]
		{
			DefaultSkills.Riding,
			DefaultSkills.Medicine
		};
		args.SetAffectedSkills(affectedSkills);
		args.SetFocusToSkills(_focusToAdd);
		args.SetLevelToSkills(_skillLevelToAdd);
		args.SetLevelToAttribute(DefaultCharacterAttributes.Endurance, _attributeLevelToAdd);
	}

	private bool ChildhoodHorseOptionOnCondition(CharacterCreationManager characterCreationManager)
	{
		return true;
	}

	private void ChildhoodHorseOptionOnSelect(CharacterCreationManager characterCreationManager)
	{
		foreach (NarrativeMenuCharacter character in characterCreationManager.CurrentMenu.Characters)
		{
			if (character.StringId == "player_childhood_character")
			{
				character.SetAnimationId("act_childhood_animals");
			}
		}
	}

	private List<NarrativeMenuCharacterArgs> GetEducationMenuNarrativeMenuCharacterArgs(CultureObject culture, string occupationType, CharacterCreationManager characterCreationManager)
	{
		List<NarrativeMenuCharacterArgs> list = new List<NarrativeMenuCharacterArgs>();
		string playerEducationAgeEquipmentId = GetPlayerEducationAgeEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId, Hero.MainHero.IsFemale);
		list.Add(new NarrativeMenuCharacterArgs("player_education_character", 12, playerEducationAgeEquipmentId, "act_childhood_schooled", "spawnpoint_player_1", "", "", null, isHuman: true, CharacterObject.PlayerCharacter.IsFemale));
		return list;
	}

	public void AddEducationMenu(CharacterCreationManager characterCreationManager)
	{
		BodyProperties originalBodyProperties = CharacterObject.PlayerCharacter.GetBodyProperties(CharacterObject.PlayerCharacter.Equipment);
		originalBodyProperties = FaceGen.GetBodyPropertiesWithAge(ref originalBodyProperties, 12f);
		List<NarrativeMenuCharacter> list = new List<NarrativeMenuCharacter>();
		NarrativeMenuCharacter item = new NarrativeMenuCharacter("player_education_character", originalBodyProperties, CharacterObject.PlayerCharacter.Race, CharacterObject.PlayerCharacter.IsFemale);
		list.Add(item);
		NarrativeMenu narrativeMenu = new NarrativeMenu("narrative_education_menu", "narrative_childhood_menu", "narrative_youth_menu", new TextObject("{=rcoueCmk}Adolescence"), new TextObject("{=WYvnWcXQ}Like all village children you helped out in the fields. You also..."), list, GetEducationMenuNarrativeMenuCharacterArgs);
		AddEducationMenuOptions(narrativeMenu);
		characterCreationManager.AddNewMenu(narrativeMenu);
	}

	private void AddEducationMenuOptions(NarrativeMenu narrativeMenu)
	{
		NarrativeMenuOption narrativeMenuOption = new NarrativeMenuOption("education_herder_option", new TextObject("{=RKVNvimC}herded the sheep."), new TextObject("{=KfaqPpbK}You went with other fleet-footed youths to take the villages' sheep, goats or cattle to graze in pastures near the village. You were in charge of chasing down stray beasts, and always kept a big stone on hand to be hurled at lurking predators if necessary."), GetEducationHerderOptionArgs, EducationHerderOptionOnCondition, EducationHerderOptionOnSelect, null);
		narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption);
		NarrativeMenuOption narrativeMenuOption2 = new NarrativeMenuOption("education_smith_option", new TextObject("{=bTKiN0hr}worked in the village smithy."), new TextObject("{=y6j1bJTH}You were apprenticed to the local smith. You learned how to heat and forge metal, hammering for hours at a time until your muscles ached."), GetEducationSmithOptionArgs, EducationSmithOptionOnCondition, EducationSmithOptionOnSelect, null);
		narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption2);
		NarrativeMenuOption narrativeMenuOption3 = new NarrativeMenuOption("education_engineer_option", new TextObject("{=tI8ZLtoA}repaired projects."), new TextObject("{=6LFj919J}You helped dig wells, rethatch houses, and fix broken plows. You learned about the basics of construction, as well as what it takes to keep a farming community prosperous."), GetEducationEngineerOptionArgs, EducationEngineerOptionOnCondition, EducationEngineerOptionOnSelect, null);
		narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption3);
		NarrativeMenuOption narrativeMenuOption4 = new NarrativeMenuOption("education_doctor_option", new TextObject("{=TRwgSLD2}gathered herbs in the wild."), new TextObject("{=9ks4u5cH}You were sent by the village healer up into the hills to look for useful medicinal plants. You learned which herbs healed wounds or brought down a fever, and how to find them."), GetEducationDoctorOptionArgs, EducationDoctorOptionOnCondition, EducationDoctorOptionOnSelect, null);
		narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption4);
		NarrativeMenuOption narrativeMenuOption5 = new NarrativeMenuOption("education_hunter_option", new TextObject("{=T7m7ReTq}hunted small game."), new TextObject("{=RuvSk3QT}You accompanied a local hunter as he went into the wilderness, helping him set up traps and catch small animals."), GetEducationHunterOptionArgs, EducationHunterOptionOnCondition, EducationHunterOptionOnSelect, null);
		narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption5);
		NarrativeMenuOption narrativeMenuOption6 = new NarrativeMenuOption("education_merchant_option", new TextObject("{=qAbMagWq}sold product at the market."), new TextObject("{=DIgsfYfz}You took your family's goods to the nearest town to sell your produce and buy supplies. It was hard work, but you enjoyed the hubbub of the marketplace."), GetEducationMerchantOptionArgs, EducationMerchantOptionOnCondition, EducationMerchantOptionOnSelect, null);
		narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption6);
		NarrativeMenuOption narrativeMenuOption7 = new NarrativeMenuOption("education_watcher_option", new TextObject("{=go7Yu7KS}watched the militia training."), new TextObject("{=qnqdEJOv}You watched the town's watch practice shooting and perfect their plans to defend the walls in case of a siege."), GetEducationWatcherOptionArgs, EducationWatcherOptionOnCondition, EducationWatcherOptionOnSelect, null);
		narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption7);
		NarrativeMenuOption narrativeMenuOption8 = new NarrativeMenuOption("education_ganger_option", new TextObject("{=gAjvAGTa}hung out with the gangs in the alleys."), new TextObject("{=1SUTcF0J}The gang leaders who kept watch over the slums of Calradian cities were always in need of poor youth to run messages and back them up in turf wars, while thrill-seeking merchants' sons and daughters sometimes slummed it in their company as well."), GetEducationGangerOptionArgs, EducationGangerOptionOnCondition, EducationGangerOptionOnSelect, null);
		narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption8);
		NarrativeMenuOption narrativeMenuOption9 = new NarrativeMenuOption("education_docker_option", new TextObject("{=QVVCgajg}helped at building sites."), new TextObject("{=bhdkegZ4}All towns had their share of projects that were constantly in need of both skilled and unskilled labor. You learned how hoists and scaffolds were constructed, how planks and stones were hewn and fitted, and other skills."), GetEducationDockerOptionArgs, EducationDockerOptionOnCondition, EducationDockerOptionOnSelect, null);
		narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption9);
		NarrativeMenuOption narrativeMenuOption10 = new NarrativeMenuOption("education_marketer_option", new TextObject("{=JTsv6PFe}worked in the markets and caravanserais."), new TextObject("{=rmMcwSn8}You helped your family handle their business affairs, going down to the marketplace to make purchases and oversee the arrival of caravans."), GetEducationMarketerOptionArgs, EducationMarketerOptionOnCondition, EducationMarketerOptionOnSelect, null);
		narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption10);
		NarrativeMenuOption narrativeMenuOption11 = new NarrativeMenuOption("education_tutor_option", new TextObject("{=EMVojYzW}studied with your private tutor."), new TextObject("{=hXl25avg}Your family arranged for a private tutor and you took full advantage, reading voraciously on history, mathematics, and philosophy and discussing what you read with your tutor and classmates."), GetEducationTutorOptionArgs, EducationTutorOptionOnCondition, EducationTutorOptionOnSelect, null);
		narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption11);
		NarrativeMenuOption narrativeMenuOption12 = new NarrativeMenuOption("education_horser_option", new TextObject("{=hin3iA2D}cared for the horses."), new TextObject("{=Ghz90npw}Your family owned a few horses at the town stables and you took charge of their care. Many evenings you would take them out beyond the walls and gallup through the fields, racing other youth."), GetEducationPoorHorserOptionArgs, EducationPoorHorserOptionOnCondition, EducationPoorHorserOptionOnSelect, null);
		narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption12);
	}

	private void GetEducationHerderOptionArgs(NarrativeMenuOptionArgs args)
	{
		SkillObject[] affectedSkills = new SkillObject[2]
		{
			DefaultSkills.Athletics,
			DefaultSkills.Throwing
		};
		args.SetAffectedSkills(affectedSkills);
		args.SetFocusToSkills(_focusToAdd);
		args.SetLevelToSkills(_skillLevelToAdd);
		args.SetLevelToAttribute(DefaultCharacterAttributes.Control, _attributeLevelToAdd);
	}

	private bool EducationHerderOptionOnCondition(CharacterCreationManager characterCreationManager)
	{
		return !CharacterOccupationTypes.IsUrbanOccupation(characterCreationManager.CharacterCreationContent.SelectedParentOccupation);
	}

	private void EducationHerderOptionOnSelect(CharacterCreationManager characterCreationManager)
	{
		foreach (NarrativeMenuCharacter character in characterCreationManager.CurrentMenu.Characters)
		{
			if (character.StringId == "player_education_character")
			{
				character.SetAnimationId("act_childhood_streets");
				character.SetLeftHandItem("");
				character.SetRightHandItem("carry_bostaff_rogue1");
				break;
			}
		}
	}

	private void GetEducationSmithOptionArgs(NarrativeMenuOptionArgs args)
	{
		SkillObject[] affectedSkills = new SkillObject[2]
		{
			DefaultSkills.TwoHanded,
			DefaultSkills.Crafting
		};
		args.SetAffectedSkills(affectedSkills);
		args.SetFocusToSkills(_focusToAdd);
		args.SetLevelToSkills(_skillLevelToAdd);
		args.SetLevelToAttribute(DefaultCharacterAttributes.Vigor, _attributeLevelToAdd);
	}

	private bool EducationSmithOptionOnCondition(CharacterCreationManager characterCreationManager)
	{
		return !CharacterOccupationTypes.IsUrbanOccupation(characterCreationManager.CharacterCreationContent.SelectedParentOccupation);
	}

	private void EducationSmithOptionOnSelect(CharacterCreationManager characterCreationManager)
	{
		foreach (NarrativeMenuCharacter character in characterCreationManager.CurrentMenu.Characters)
		{
			if (character.StringId == "player_education_character")
			{
				character.SetAnimationId("act_childhood_militia");
				character.SetLeftHandItem("");
				character.SetRightHandItem("peasant_hammer_1_t1");
				break;
			}
		}
	}

	private void GetEducationEngineerOptionArgs(NarrativeMenuOptionArgs args)
	{
		SkillObject[] affectedSkills = new SkillObject[2]
		{
			DefaultSkills.Crafting,
			DefaultSkills.Engineering
		};
		args.SetAffectedSkills(affectedSkills);
		args.SetFocusToSkills(_focusToAdd);
		args.SetLevelToSkills(_skillLevelToAdd);
		args.SetLevelToAttribute(DefaultCharacterAttributes.Intelligence, _attributeLevelToAdd);
	}

	private bool EducationEngineerOptionOnCondition(CharacterCreationManager characterCreationManager)
	{
		return !CharacterOccupationTypes.IsUrbanOccupation(characterCreationManager.CharacterCreationContent.SelectedParentOccupation);
	}

	private void EducationEngineerOptionOnSelect(CharacterCreationManager characterCreationManager)
	{
		foreach (NarrativeMenuCharacter character in characterCreationManager.CurrentMenu.Characters)
		{
			if (character.StringId == "player_education_character")
			{
				character.SetAnimationId("act_childhood_grit");
				character.SetLeftHandItem("");
				character.SetRightHandItem("carry_hammer");
				break;
			}
		}
	}

	private void GetEducationDoctorOptionArgs(NarrativeMenuOptionArgs args)
	{
		SkillObject[] affectedSkills = new SkillObject[2]
		{
			DefaultSkills.Medicine,
			DefaultSkills.Scouting
		};
		args.SetAffectedSkills(affectedSkills);
		args.SetFocusToSkills(_focusToAdd);
		args.SetLevelToSkills(_skillLevelToAdd);
		args.SetLevelToAttribute(DefaultCharacterAttributes.Endurance, _attributeLevelToAdd);
	}

	private bool EducationDoctorOptionOnCondition(CharacterCreationManager characterCreationManager)
	{
		return !CharacterOccupationTypes.IsUrbanOccupation(characterCreationManager.CharacterCreationContent.SelectedParentOccupation);
	}

	private void EducationDoctorOptionOnSelect(CharacterCreationManager characterCreationManager)
	{
		foreach (NarrativeMenuCharacter character in characterCreationManager.CurrentMenu.Characters)
		{
			if (character.StringId == "player_education_character")
			{
				character.SetAnimationId("act_childhood_peddlers");
				character.SetLeftHandItem("");
				character.SetRightHandItem("_to_carry_bd_basket_a");
				break;
			}
		}
	}

	private void GetEducationHunterOptionArgs(NarrativeMenuOptionArgs args)
	{
		SkillObject[] affectedSkills = new SkillObject[2]
		{
			DefaultSkills.Bow,
			DefaultSkills.Tactics
		};
		args.SetAffectedSkills(affectedSkills);
		args.SetFocusToSkills(_focusToAdd);
		args.SetLevelToSkills(_skillLevelToAdd);
		args.SetLevelToAttribute(DefaultCharacterAttributes.Cunning, _attributeLevelToAdd);
	}

	private bool EducationHunterOptionOnCondition(CharacterCreationManager characterCreationManager)
	{
		return !CharacterOccupationTypes.IsUrbanOccupation(characterCreationManager.CharacterCreationContent.SelectedParentOccupation);
	}

	private void EducationHunterOptionOnSelect(CharacterCreationManager characterCreationManager)
	{
		foreach (NarrativeMenuCharacter character in characterCreationManager.CurrentMenu.Characters)
		{
			if (character.StringId == "player_education_character")
			{
				character.SetAnimationId("act_childhood_sharp");
				character.SetLeftHandItem("");
				character.SetRightHandItem("composite_bow");
				break;
			}
		}
	}

	private void GetEducationMerchantOptionArgs(NarrativeMenuOptionArgs args)
	{
		SkillObject[] affectedSkills = new SkillObject[2]
		{
			DefaultSkills.Trade,
			DefaultSkills.Charm
		};
		args.SetAffectedSkills(affectedSkills);
		args.SetFocusToSkills(_focusToAdd);
		args.SetLevelToSkills(_skillLevelToAdd);
		args.SetLevelToAttribute(DefaultCharacterAttributes.Social, _attributeLevelToAdd);
	}

	private bool EducationMerchantOptionOnCondition(CharacterCreationManager characterCreationManager)
	{
		return !CharacterOccupationTypes.IsUrbanOccupation(characterCreationManager.CharacterCreationContent.SelectedParentOccupation);
	}

	private void EducationMerchantOptionOnSelect(CharacterCreationManager characterCreationManager)
	{
		foreach (NarrativeMenuCharacter character in characterCreationManager.CurrentMenu.Characters)
		{
			if (character.StringId == "player_education_character")
			{
				character.SetAnimationId("act_childhood_peddlers_2");
				character.SetLeftHandItem("");
				character.SetRightHandItem("_to_carry_bd_fabric_c");
				break;
			}
		}
	}

	private void GetEducationWatcherOptionArgs(NarrativeMenuOptionArgs args)
	{
		SkillObject[] affectedSkills = new SkillObject[2]
		{
			DefaultSkills.Polearm,
			DefaultSkills.Tactics
		};
		args.SetAffectedSkills(affectedSkills);
		args.SetFocusToSkills(_focusToAdd);
		args.SetLevelToSkills(_skillLevelToAdd);
		args.SetLevelToAttribute(DefaultCharacterAttributes.Control, _attributeLevelToAdd);
	}

	private bool EducationWatcherOptionOnCondition(CharacterCreationManager characterCreationManager)
	{
		return CharacterOccupationTypes.IsUrbanOccupation(characterCreationManager.CharacterCreationContent.SelectedParentOccupation);
	}

	private void EducationWatcherOptionOnSelect(CharacterCreationManager characterCreationManager)
	{
		foreach (NarrativeMenuCharacter character in characterCreationManager.CurrentMenu.Characters)
		{
			if (character.StringId == "player_education_character")
			{
				character.SetAnimationId("act_childhood_fox");
				character.SetLeftHandItem("");
				character.SetRightHandItem("");
				break;
			}
		}
	}

	private void GetEducationGangerOptionArgs(NarrativeMenuOptionArgs args)
	{
		SkillObject[] affectedSkills = new SkillObject[2]
		{
			DefaultSkills.Roguery,
			DefaultSkills.OneHanded
		};
		args.SetAffectedSkills(affectedSkills);
		args.SetFocusToSkills(_focusToAdd);
		args.SetLevelToSkills(_skillLevelToAdd);
		args.SetLevelToAttribute(DefaultCharacterAttributes.Cunning, _attributeLevelToAdd);
	}

	private bool EducationGangerOptionOnCondition(CharacterCreationManager characterCreationManager)
	{
		return CharacterOccupationTypes.IsUrbanOccupation(characterCreationManager.CharacterCreationContent.SelectedParentOccupation);
	}

	private void EducationGangerOptionOnSelect(CharacterCreationManager characterCreationManager)
	{
		foreach (NarrativeMenuCharacter character in characterCreationManager.CurrentMenu.Characters)
		{
			if (character.StringId == "player_education_character")
			{
				character.SetAnimationId("act_childhood_athlete");
				character.SetLeftHandItem("");
				character.SetRightHandItem("");
				break;
			}
		}
	}

	private void GetEducationDockerOptionArgs(NarrativeMenuOptionArgs args)
	{
		SkillObject[] affectedSkills = new SkillObject[2]
		{
			DefaultSkills.Athletics,
			DefaultSkills.Crafting
		};
		args.SetAffectedSkills(affectedSkills);
		args.SetFocusToSkills(_focusToAdd);
		args.SetLevelToSkills(_skillLevelToAdd);
		args.SetLevelToAttribute(DefaultCharacterAttributes.Vigor, _attributeLevelToAdd);
	}

	private bool EducationDockerOptionOnCondition(CharacterCreationManager characterCreationManager)
	{
		return CharacterOccupationTypes.IsUrbanOccupation(characterCreationManager.CharacterCreationContent.SelectedParentOccupation);
	}

	private void EducationDockerOptionOnSelect(CharacterCreationManager characterCreationManager)
	{
		foreach (NarrativeMenuCharacter character in characterCreationManager.CurrentMenu.Characters)
		{
			if (character.StringId == "player_education_character")
			{
				character.SetAnimationId("act_childhood_peddlers");
				character.SetLeftHandItem("");
				character.SetRightHandItem("_to_carry_bd_basket_a");
				break;
			}
		}
	}

	private void GetEducationMarketerOptionArgs(NarrativeMenuOptionArgs args)
	{
		SkillObject[] affectedSkills = new SkillObject[2]
		{
			DefaultSkills.Trade,
			DefaultSkills.Charm
		};
		args.SetAffectedSkills(affectedSkills);
		args.SetFocusToSkills(_focusToAdd);
		args.SetLevelToSkills(_skillLevelToAdd);
		args.SetLevelToAttribute(DefaultCharacterAttributes.Social, _attributeLevelToAdd);
	}

	private bool EducationMarketerOptionOnCondition(CharacterCreationManager characterCreationManager)
	{
		return CharacterOccupationTypes.IsUrbanOccupation(characterCreationManager.CharacterCreationContent.SelectedParentOccupation);
	}

	private void EducationMarketerOptionOnSelect(CharacterCreationManager characterCreationManager)
	{
		foreach (NarrativeMenuCharacter character in characterCreationManager.CurrentMenu.Characters)
		{
			if (character.StringId == "player_education_character")
			{
				character.SetAnimationId("act_childhood_manners");
				character.SetLeftHandItem("");
				character.SetRightHandItem("");
				break;
			}
		}
	}

	private void GetEducationTutorOptionArgs(NarrativeMenuOptionArgs args)
	{
		SkillObject[] affectedSkills = new SkillObject[2]
		{
			DefaultSkills.Engineering,
			DefaultSkills.Leadership
		};
		args.SetAffectedSkills(affectedSkills);
		args.SetFocusToSkills(_focusToAdd);
		args.SetLevelToSkills(_skillLevelToAdd);
		args.SetLevelToAttribute(DefaultCharacterAttributes.Intelligence, _attributeLevelToAdd);
	}

	private bool EducationTutorOptionOnCondition(CharacterCreationManager characterCreationManager)
	{
		return CharacterOccupationTypes.IsUrbanOccupation(characterCreationManager.CharacterCreationContent.SelectedParentOccupation);
	}

	private void EducationTutorOptionOnSelect(CharacterCreationManager characterCreationManager)
	{
		foreach (NarrativeMenuCharacter character in characterCreationManager.CurrentMenu.Characters)
		{
			if (character.StringId == "player_education_character")
			{
				character.SetAnimationId("act_childhood_book");
				character.SetLeftHandItem("character_creation_notebook");
				character.SetRightHandItem("");
				break;
			}
		}
	}

	private void GetEducationPoorHorserOptionArgs(NarrativeMenuOptionArgs args)
	{
		SkillObject[] affectedSkills = new SkillObject[2]
		{
			DefaultSkills.Riding,
			DefaultSkills.Steward
		};
		args.SetAffectedSkills(affectedSkills);
		args.SetFocusToSkills(_focusToAdd);
		args.SetLevelToSkills(_skillLevelToAdd);
		args.SetLevelToAttribute(DefaultCharacterAttributes.Endurance, _attributeLevelToAdd);
	}

	private bool EducationPoorHorserOptionOnCondition(CharacterCreationManager characterCreationManager)
	{
		return CharacterOccupationTypes.IsUrbanOccupation(characterCreationManager.CharacterCreationContent.SelectedParentOccupation);
	}

	private void EducationPoorHorserOptionOnSelect(CharacterCreationManager characterCreationManager)
	{
		foreach (NarrativeMenuCharacter character in characterCreationManager.CurrentMenu.Characters)
		{
			if (character.StringId == "player_education_character")
			{
				character.SetAnimationId("act_childhood_peddlers_2");
				character.SetLeftHandItem("");
				character.SetRightHandItem("_to_carry_bd_fabric_c");
				break;
			}
		}
	}

	private List<NarrativeMenuCharacterArgs> GetYouthMenuNarrativeMenuCharacterArgs(CultureObject culture, string occupationType, CharacterCreationManager characterCreationManager)
	{
		if (string.IsNullOrEmpty(characterCreationManager.CharacterCreationContent.SelectedTitleType))
		{
			characterCreationManager.CharacterCreationContent.SelectedTitleType = "guard";
		}
		List<NarrativeMenuCharacterArgs> list = new List<NarrativeMenuCharacterArgs>();
		string playerEquipmentId = GetPlayerEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedTitleType, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId, Hero.MainHero.IsFemale);
		list.Add(new NarrativeMenuCharacterArgs("player_youth_character", 17, playerEquipmentId, "act_childhood_schooled", "spawnpoint_player_1", "", "", null, isHuman: true, CharacterObject.PlayerCharacter.IsFemale));
		MBEquipmentRoster mBEquipmentRoster = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(playerEquipmentId);
		list.Add(new NarrativeMenuCharacterArgs(mountCreationKey: MountCreationKey.GetRandomMountKey(mBEquipmentRoster.DefaultEquipment[EquipmentIndex.ArmorItemEndSlot].Item, CharacterObject.PlayerCharacter.GetMountKeySeed()), characterId: "narrative_character_horse", age: -1, equipmentId: "", animationId: "act_inventory_idle_start", spawnPointEntityId: "spawnpoint_mount_1", leftHandItemId: mBEquipmentRoster.DefaultEquipment[EquipmentIndex.ArmorItemEndSlot].Item.StringId, rightHandItemId: mBEquipmentRoster.DefaultEquipment[EquipmentIndex.HorseHarness].Item.StringId, isHuman: false));
		return list;
	}

	private void AddYouthMenu(CharacterCreationManager characterCreationManager)
	{
		TextObject description = (CharacterObject.PlayerCharacter.IsFemale ? new TextObject("{=5kbeAC7k}In wartorn Calradia, especially in frontier or tribal areas, some women as well as men learn to fight from an early age. You...") : new TextObject("{=F7OO5SAa}As a youngster growing up in Calradia, war was never too far away. You..."));
		BodyProperties originalBodyProperties = CharacterObject.PlayerCharacter.GetBodyProperties(CharacterObject.PlayerCharacter.Equipment);
		originalBodyProperties = FaceGen.GetBodyPropertiesWithAge(ref originalBodyProperties, 17f);
		NarrativeMenuCharacter item = new NarrativeMenuCharacter("player_youth_character", originalBodyProperties, CharacterObject.PlayerCharacter.Race, CharacterObject.PlayerCharacter.IsFemale);
		NarrativeMenuCharacter item2 = new NarrativeMenuCharacter("narrative_character_horse");
		List<NarrativeMenuCharacter> list = new List<NarrativeMenuCharacter>();
		list.Add(item);
		list.Add(item2);
		NarrativeMenu narrativeMenu = new NarrativeMenu("narrative_youth_menu", "narrative_education_menu", "narrative_adulthood_menu", new TextObject("{=ok8lSW6M}Youth"), description, list, GetYouthMenuNarrativeMenuCharacterArgs);
		AddYouthMenuOptions(narrativeMenu);
		characterCreationManager.AddNewMenu(narrativeMenu);
	}

	private void AddYouthMenuOptions(NarrativeMenu narrativeMenu)
	{
		NarrativeMenuOption narrativeMenuOption = new NarrativeMenuOption("youth_staff_first_option", new TextObject("{=CITG915d}joined a commander's staff."), new TextObject("{=wNHqFlDL}You were chosen by your superior officer to serve an imperial strategos as a courier. You were not given major responsibilities - mostly carrying messages and tending to his horse - but it did give you a chance to see how campaigns were planned and men were deployed in battle."), GetYouthStaffOptionArgs, YouthStaffOneOptionOnCondition, YouthStaffOptionOnSelect, null);
		narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption);
		NarrativeMenuOption narrativeMenuOption2 = new NarrativeMenuOption("youth_staff_second_option", new TextObject("{=CITG915d}joined a commander's staff."), new TextObject("{=ANbNblaH}You were picked as the courier of the commander of the local forces. You were not given major responsibilities - mostly carrying messages and tending to his horse - but it did give you a chance to see how campaigns were planned and men were deployed in battle."), GetYouthStaffOptionArgs, YouthStaffTwoOptionOnCondition, YouthStaffOptionOnSelect, null);
		narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption2);
		NarrativeMenuOption narrativeMenuOption3 = new NarrativeMenuOption("youth_groom_option", new TextObject("{=bhE2i6OU}served as a baron's groom."), new TextObject("{=i3k7YtA8}You were chosen by a knight to accompany a minor baron of the Vlandian kingdom. You were not given major responsibilities - mostly carrying messages and tending to his horse - but it did give you a chance to see how campaigns were planned and men were deployed in battle."), GetYouthGroomOptionArgs, YouthGroomOptionOnCondition, YouthGroomOptionOnSelect, null);
		narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption3);
		NarrativeMenuOption narrativeMenuOption4 = new NarrativeMenuOption("youth_servant_first_option", new TextObject("{=F2bgujPo}were a chieftain's servant."), new TextObject("{=AXWO4C69}Your were choosen among others to accompany a chieftain of your people. You were not given major responsibilities - mostly carrying messages and tending to his horse - but it did give you a chance to see how campaigns were planned and men were deployed in battle."), GetYouthServantOptionArgs, YouthServantOneOptionOnCondition, YouthServantOptionOnSelect, null);
		narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption4);
		NarrativeMenuOption narrativeMenuOption5 = new NarrativeMenuOption("youth_servant_second_option", new TextObject("{=F2bgujPo}were a chieftain's servant."), new TextObject("{=neMCgMZM}Local wise man picked you to become the messenger of a chieftain of your people. You were not given major responsibilities - mostly carrying messages and tending to his horse - but it did give you a chance to see how campaigns were planned and men were deployed in battle."), GetYouthServantOptionArgs, YouthServantTwoOptionOnCondition, YouthServantOptionOnSelect, null);
		narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption5);
		NarrativeMenuOption narrativeMenuOption6 = new NarrativeMenuOption("youth_cavalry_option", new TextObject("{=h2KnarLL}trained with the cavalry."), new TextObject("{=7cHsIMLP}You could never have bought the equipment on your own, but you were a good enough rider so that the local lord lent you a horse and equipment. You joined the armored cavalry, training with the lance."), GetYouthCavalryOptionArgs, YouthCavalryOptionOnCondition, YouthCavalryOptionOnSelect, null);
		narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption6);
		NarrativeMenuOption narrativeMenuOption7 = new NarrativeMenuOption("youth_hearth_option", new TextObject("{=zsC2t5Hb}trained with the hearth guard."), new TextObject("{=RmbWW6Bm}You were a big and imposing enough youth that the chief's guard allowed you to train alongside them, in preparation to join them some day."), GetYouthHearthOptionArgs, YouthHearthOptionOnCondition, YouthHearthOptionOnSelect, null);
		narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption7);
		NarrativeMenuOption narrativeMenuOption8 = new NarrativeMenuOption("youth_guard_high_register_option", new TextObject("{=aTncHUfL}stood guard with the garrisons."), new TextObject("{=63TAYbkx}Urban troops spend much of their time guarding the town walls. Most of their training was in missile weapons, especially useful during sieges."), GetYouthGuardHighRegisterOptionArgs, YouthGuardHighRegisterOptionOnCondition, YouthGuardHighRegisterOptionOnSelect, null);
		narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption8);
		NarrativeMenuOption narrativeMenuOption9 = new NarrativeMenuOption("youth_guard_low_register_option", new TextObject("{=aTncHUfL}stood guard with the garrisons."), new TextObject("{=oR58iNDz}Urban troops spend much of their time guarding the town walls. Most of their training was in missile weapons."), GetYouthGuardLowRegisterOptionArgs, YouthGuardLowRegisterOptionOnCondition, YouthGuardLowRegisterOptionOnSelect, null);
		narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption9);
		NarrativeMenuOption narrativeMenuOption10 = new NarrativeMenuOption("youth_guard_garrisons_register_option", new TextObject("{=aTncHUfL}stood guard with the garrisons."), new TextObject("{=e6lINjFg}The garrisons spent most of their time guarding the town walls, and their training focused largely on missile weapons."), GetYouthGuardGarrisonRegisterOptionArgs, YouthGuardGarrisonRegisterOptionOnCondition, YouthGuardGarrisonRegisterOptionOnSelect, null);
		narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption10);
		NarrativeMenuOption narrativeMenuOption11 = new NarrativeMenuOption("youth_guard_empire_register_option", new TextObject("{=aTncHUfL}stood guard with the garrisons."), new TextObject("{=oR58iNDz}Urban troops spend much of their time guarding the town walls. Most of their training was in missile weapons."), GetYouthGuardEmpireRegisterOptionArgs, YouthGuardEmpireRegisterOptionOnCondition, YouthGuardEmpireRegisterOptionOnSelect, null);
		narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption11);
		NarrativeMenuOption narrativeMenuOption12 = new NarrativeMenuOption("youth_rider_high_register_option", new TextObject("{=VlXOgIX6}rode with the scouts."), new TextObject("{=888lmJqs}All of Calradia's kingdoms recognize the value of good light cavalry and horse archers, and are sure to recruit nomads and borderers with the skills to fulfill those duties. You were a good enough rider that your neighbors pitched in to buy you a small pony and a good bow so that you could fulfill their levy obligations."), GetYouthRiderHighRegisterOptionArgs, YouthRiderHighRegisterOptionOnCondition, YouthRiderHighRegisterOptionOnSelect, null);
		narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption12);
		NarrativeMenuOption narrativeMenuOption13 = new NarrativeMenuOption("youth_rider_low_register_option", new TextObject("{=VlXOgIX6}rode with the scouts."), new TextObject("{=sYuN6hPD}All of Calradia's kingdoms recognize the value of good light cavalry, and are sure to recruit nomads and borderers with the skills to fulfill those duties. You were a good enough rider that your neighbors pitched in to buy you a small pony and a sheaf of javelins so that you could fulfill their levy obligations."), GetYouthRiderLowRegisterOptionArgs, YouthRiderLowRegisterOptionOnCondition, YouthRiderLowRegisterOptionOnSelect, null);
		narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption13);
		NarrativeMenuOption narrativeMenuOption14 = new NarrativeMenuOption("youth_infantry_option", new TextObject("{=a8arFSra}trained with the infantry."), new TextObject("{=afH90aNs}Levy armed with spear and shield, drawn from smallholding farmers, have always been the backbone of most armies of Calradia."), GetYouthInfantryOptionArgs, YouthInfantryOptionOnCondition, YouthInfantryOptionOnSelect, null);
		narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption14);
		NarrativeMenuOption narrativeMenuOption15 = new NarrativeMenuOption("youth_skirmisher_option", new TextObject("{=oMbOIPc9}joined the skirmishers."), new TextObject("{=bXAg5w19}Younger recruits, or those of a slighter build, or those too poor to buy shield and armor tend to join the skirmishers. Fighting with bow and javelin, they try to stay out of reach of the main enemy forces."), GetYouthSkirmisherOptionArgs, YouthSkirmisherOptionOnCondition, YouthSkirmisherOptionOnSelect, null);
		narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption15);
		NarrativeMenuOption narrativeMenuOption16 = new NarrativeMenuOption("youth_kern_option", new TextObject("{=cDWbwBwI}joined the kern."), new TextObject("{=tTb28jyU}Many Battanians fight as kern, versatile troops who could both harass the enemy line with their javelins or join in the final screaming charge once it weakened."), GetYouthKernOptionArgs, YouthKernOptionOnCondition, YouthKernOptionOnSelect, null);
		narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption16);
		NarrativeMenuOption narrativeMenuOption17 = new NarrativeMenuOption("youth_camp_option", new TextObject("{=GFUggps8}marched with the camp followers."), new TextObject("{=64rWqBLN}You avoided service with one of the main forces of your realm's armies, but followed instead in the train - the troops' wives, lovers and servants, and those who make their living by caring for, entertaining, or cheating the soldiery."), GetYouthCampOptionArgs, YouthCampOptionOnCondition, YouthCampOptionOnSelect, null);
		narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption17);
		NarrativeMenuOption narrativeMenuOption18 = new NarrativeMenuOption("youth_envoys_guard_first_option", new TextObject("{=YmPlLGXb}served in an envoy's entourage"), new TextObject("{=qPamcCkA}Your family arranged for you to accompany an envoy. You were not given major responsibilities - mostly carrying arms and trying to look imposing. - but it did give you a chance to travel a lot and socialise and see the world."), GetEnvoysGuardFirstOptionArgs, EnvoysGuardFirstOptionOnCondition, EnvoysGuardFirstOptionOnSelect, null);
		narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption18);
		NarrativeMenuOption narrativeMenuOption19 = new NarrativeMenuOption("youth_envoys_guard_second_option", new TextObject("{=YmPlLGXb}served in an envoy's entourage"), new TextObject("{=VYU1nEHP}Your family arranged for you to accompany an envoy. You were not given major responsibilities but it did give you a chance to travel and socialise and see a bit of the world."), GetEnvoysGuardSecondOptionArgs, EnvoysGuardSecondOptionOnCondition, EnvoysGuardSecondOptionOnSelect, null);
		narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption19);
	}

	private void GetYouthStaffOptionArgs(NarrativeMenuOptionArgs args)
	{
		SkillObject[] affectedSkills = new SkillObject[2]
		{
			DefaultSkills.Steward,
			DefaultSkills.Tactics
		};
		args.SetAffectedSkills(affectedSkills);
		args.SetFocusToSkills(_focusToAdd);
		args.SetLevelToSkills(_skillLevelToAdd);
		args.SetLevelToAttribute(DefaultCharacterAttributes.Cunning, _attributeLevelToAdd);
	}

	private bool YouthStaffOneOptionOnCondition(CharacterCreationManager characterCreationManager)
	{
		return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "empire";
	}

	private bool YouthStaffTwoOptionOnCondition(CharacterCreationManager characterCreationManager)
	{
		return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "aserai";
	}

	private void YouthStaffOptionOnSelect(CharacterCreationManager characterCreationManager)
	{
		characterCreationManager.CharacterCreationContent.SelectedTitleType = "retainer";
		string playerEquipmentId = GetPlayerEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedTitleType, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId, Hero.MainHero.IsFemale);
		foreach (NarrativeMenuCharacter character in characterCreationManager.CurrentMenu.Characters)
		{
			if (character.StringId == "player_youth_character")
			{
				character.SetAnimationId("act_childhood_decisive");
				character.SetEquipment(Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(playerEquipmentId));
			}
		}
	}

	private void GetYouthGroomOptionArgs(NarrativeMenuOptionArgs args)
	{
		SkillObject[] affectedSkills = new SkillObject[2]
		{
			DefaultSkills.Charm,
			DefaultSkills.Tactics
		};
		args.SetAffectedSkills(affectedSkills);
		args.SetFocusToSkills(_focusToAdd);
		args.SetLevelToSkills(_skillLevelToAdd);
		args.SetLevelToAttribute(DefaultCharacterAttributes.Social, _attributeLevelToAdd);
	}

	private bool YouthGroomOptionOnCondition(CharacterCreationManager characterCreationManager)
	{
		return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "vlandia";
	}

	private void YouthGroomOptionOnSelect(CharacterCreationManager characterCreationManager)
	{
		characterCreationManager.CharacterCreationContent.SelectedTitleType = "retainer";
		string playerEquipmentId = GetPlayerEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedTitleType, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId, Hero.MainHero.IsFemale);
		foreach (NarrativeMenuCharacter character in characterCreationManager.CurrentMenu.Characters)
		{
			if (character.StringId == "player_youth_character")
			{
				character.SetAnimationId("act_childhood_sharp");
				character.SetEquipment(Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(playerEquipmentId));
			}
		}
	}

	private void GetYouthServantOptionArgs(NarrativeMenuOptionArgs args)
	{
		SkillObject[] affectedSkills = new SkillObject[2]
		{
			DefaultSkills.Steward,
			DefaultSkills.Tactics
		};
		args.SetAffectedSkills(affectedSkills);
		args.SetFocusToSkills(_focusToAdd);
		args.SetLevelToSkills(_skillLevelToAdd);
		args.SetLevelToAttribute(DefaultCharacterAttributes.Cunning, _attributeLevelToAdd);
	}

	private bool YouthServantOneOptionOnCondition(CharacterCreationManager characterCreationManager)
	{
		return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "khuzait";
	}

	private bool YouthServantTwoOptionOnCondition(CharacterCreationManager characterCreationManager)
	{
		return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "battania";
	}

	private void YouthServantOptionOnSelect(CharacterCreationManager characterCreationManager)
	{
		characterCreationManager.CharacterCreationContent.SelectedTitleType = "retainer";
		string playerEquipmentId = GetPlayerEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedTitleType, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId, Hero.MainHero.IsFemale);
		foreach (NarrativeMenuCharacter character in characterCreationManager.CurrentMenu.Characters)
		{
			if (character.StringId == "player_youth_character")
			{
				character.SetAnimationId("act_childhood_ready");
				character.SetEquipment(Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(playerEquipmentId));
			}
		}
	}

	private void GetYouthCavalryOptionArgs(NarrativeMenuOptionArgs args)
	{
		SkillObject[] affectedSkills = new SkillObject[2]
		{
			DefaultSkills.Riding,
			DefaultSkills.Polearm
		};
		args.SetAffectedSkills(affectedSkills);
		args.SetFocusToSkills(_focusToAdd);
		args.SetLevelToSkills(_skillLevelToAdd);
		args.SetLevelToAttribute(DefaultCharacterAttributes.Endurance, _attributeLevelToAdd);
	}

	private bool YouthCavalryOptionOnCondition(CharacterCreationManager characterCreationManager)
	{
		return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "vlandia";
	}

	private void YouthCavalryOptionOnSelect(CharacterCreationManager characterCreationManager)
	{
		characterCreationManager.CharacterCreationContent.SelectedTitleType = "mercenary";
		string playerEquipmentId = GetPlayerEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedTitleType, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId, Hero.MainHero.IsFemale);
		foreach (NarrativeMenuCharacter character in characterCreationManager.CurrentMenu.Characters)
		{
			if (character.StringId == "player_youth_character")
			{
				character.SetAnimationId("act_childhood_apprentice");
				character.SetEquipment(Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(playerEquipmentId));
			}
		}
	}

	private void GetYouthHearthOptionArgs(NarrativeMenuOptionArgs args)
	{
		SkillObject[] affectedSkills = new SkillObject[2]
		{
			DefaultSkills.Riding,
			DefaultSkills.Polearm
		};
		args.SetAffectedSkills(affectedSkills);
		args.SetFocusToSkills(_focusToAdd);
		args.SetLevelToSkills(_skillLevelToAdd);
		args.SetLevelToAttribute(DefaultCharacterAttributes.Endurance, _attributeLevelToAdd);
	}

	private bool YouthHearthOptionOnCondition(CharacterCreationManager characterCreationManager)
	{
		if (!(characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "sturgia"))
		{
			return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "battania";
		}
		return true;
	}

	private void YouthHearthOptionOnSelect(CharacterCreationManager characterCreationManager)
	{
		characterCreationManager.CharacterCreationContent.SelectedTitleType = "mercenary";
		string playerEquipmentId = GetPlayerEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedTitleType, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId, Hero.MainHero.IsFemale);
		foreach (NarrativeMenuCharacter character in characterCreationManager.CurrentMenu.Characters)
		{
			if (character.StringId == "player_youth_character")
			{
				character.SetAnimationId("act_childhood_athlete");
				character.SetEquipment(Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(playerEquipmentId));
			}
		}
	}

	private void GetYouthGuardHighRegisterOptionArgs(NarrativeMenuOptionArgs args)
	{
		SkillObject[] affectedSkills = new SkillObject[2]
		{
			DefaultSkills.Crossbow,
			DefaultSkills.Engineering
		};
		args.SetAffectedSkills(affectedSkills);
		args.SetFocusToSkills(_focusToAdd);
		args.SetLevelToSkills(_skillLevelToAdd);
		args.SetLevelToAttribute(DefaultCharacterAttributes.Intelligence, _attributeLevelToAdd);
	}

	private bool YouthGuardHighRegisterOptionOnCondition(CharacterCreationManager characterCreationManager)
	{
		return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "vlandia";
	}

	private void YouthGuardHighRegisterOptionOnSelect(CharacterCreationManager characterCreationManager)
	{
		characterCreationManager.CharacterCreationContent.SelectedTitleType = "guard";
		string playerEquipmentId = GetPlayerEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedTitleType, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId, Hero.MainHero.IsFemale);
		foreach (NarrativeMenuCharacter character in characterCreationManager.CurrentMenu.Characters)
		{
			if (character.StringId == "player_youth_character")
			{
				character.SetAnimationId("act_childhood_vibrant");
				character.SetEquipment(Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(playerEquipmentId));
			}
		}
	}

	private void GetYouthGuardLowRegisterOptionArgs(NarrativeMenuOptionArgs args)
	{
		SkillObject[] affectedSkills = new SkillObject[2]
		{
			DefaultSkills.Bow,
			DefaultSkills.Engineering
		};
		args.SetAffectedSkills(affectedSkills);
		args.SetFocusToSkills(_focusToAdd);
		args.SetLevelToSkills(_skillLevelToAdd);
		args.SetLevelToAttribute(DefaultCharacterAttributes.Intelligence, _attributeLevelToAdd);
	}

	private bool YouthGuardLowRegisterOptionOnCondition(CharacterCreationManager characterCreationManager)
	{
		return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "sturgia";
	}

	private void YouthGuardLowRegisterOptionOnSelect(CharacterCreationManager characterCreationManager)
	{
		characterCreationManager.CharacterCreationContent.SelectedTitleType = "guard";
		string playerEquipmentId = GetPlayerEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedTitleType, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId, Hero.MainHero.IsFemale);
		foreach (NarrativeMenuCharacter character in characterCreationManager.CurrentMenu.Characters)
		{
			if (character.StringId == "player_youth_character")
			{
				character.SetAnimationId("act_childhood_sharp");
				character.SetEquipment(Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(playerEquipmentId));
			}
		}
	}

	private void GetYouthGuardGarrisonRegisterOptionArgs(NarrativeMenuOptionArgs args)
	{
		SkillObject[] affectedSkills = new SkillObject[2]
		{
			DefaultSkills.Bow,
			DefaultSkills.Engineering
		};
		args.SetAffectedSkills(affectedSkills);
		args.SetFocusToSkills(_focusToAdd);
		args.SetLevelToSkills(_skillLevelToAdd);
		args.SetLevelToAttribute(DefaultCharacterAttributes.Intelligence, _attributeLevelToAdd);
	}

	private bool YouthGuardGarrisonRegisterOptionOnCondition(CharacterCreationManager characterCreationManager)
	{
		if (!(characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "battania") && !(characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "khuzait"))
		{
			return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "aserai";
		}
		return true;
	}

	private void YouthGuardGarrisonRegisterOptionOnSelect(CharacterCreationManager characterCreationManager)
	{
		characterCreationManager.CharacterCreationContent.SelectedTitleType = "guard";
		string playerEquipmentId = GetPlayerEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedTitleType, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId, Hero.MainHero.IsFemale);
		foreach (NarrativeMenuCharacter character in characterCreationManager.CurrentMenu.Characters)
		{
			if (character.StringId == "player_youth_character")
			{
				character.SetAnimationId("act_childhood_sharp");
				character.SetEquipment(Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(playerEquipmentId));
			}
		}
	}

	private void GetYouthGuardEmpireRegisterOptionArgs(NarrativeMenuOptionArgs args)
	{
		SkillObject[] affectedSkills = new SkillObject[2]
		{
			DefaultSkills.Crossbow,
			DefaultSkills.Engineering
		};
		args.SetAffectedSkills(affectedSkills);
		args.SetFocusToSkills(_focusToAdd);
		args.SetLevelToSkills(_skillLevelToAdd);
		args.SetLevelToAttribute(DefaultCharacterAttributes.Intelligence, _attributeLevelToAdd);
	}

	private bool YouthGuardEmpireRegisterOptionOnCondition(CharacterCreationManager characterCreationManager)
	{
		return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "empire";
	}

	private void YouthGuardEmpireRegisterOptionOnSelect(CharacterCreationManager characterCreationManager)
	{
		characterCreationManager.CharacterCreationContent.SelectedTitleType = "guard";
		string playerEquipmentId = GetPlayerEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedTitleType, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId, Hero.MainHero.IsFemale);
		foreach (NarrativeMenuCharacter character in characterCreationManager.CurrentMenu.Characters)
		{
			if (character.StringId == "player_youth_character")
			{
				character.SetAnimationId("act_childhood_sharp");
				character.SetEquipment(Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(playerEquipmentId));
			}
		}
	}

	private void GetYouthRiderHighRegisterOptionArgs(NarrativeMenuOptionArgs args)
	{
		SkillObject[] affectedSkills = new SkillObject[2]
		{
			DefaultSkills.Riding,
			DefaultSkills.Bow
		};
		args.SetAffectedSkills(affectedSkills);
		args.SetFocusToSkills(_focusToAdd);
		args.SetLevelToSkills(_skillLevelToAdd);
		args.SetLevelToAttribute(DefaultCharacterAttributes.Endurance, _attributeLevelToAdd);
	}

	private bool YouthRiderHighRegisterOptionOnCondition(CharacterCreationManager characterCreationManager)
	{
		if (!(characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "empire"))
		{
			return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "khuzait";
		}
		return true;
	}

	private void YouthRiderHighRegisterOptionOnSelect(CharacterCreationManager characterCreationManager)
	{
		characterCreationManager.CharacterCreationContent.SelectedTitleType = "hunter";
		string playerEquipmentId = GetPlayerEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedTitleType, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId, Hero.MainHero.IsFemale);
		foreach (NarrativeMenuCharacter character in characterCreationManager.CurrentMenu.Characters)
		{
			if (character.StringId == "player_youth_character")
			{
				character.SetAnimationId("act_sturgia_mp_warrior_axe");
				character.SetEquipment(Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(playerEquipmentId));
			}
		}
	}

	private void GetYouthRiderLowRegisterOptionArgs(NarrativeMenuOptionArgs args)
	{
		SkillObject[] affectedSkills = new SkillObject[2]
		{
			DefaultSkills.Riding,
			DefaultSkills.Bow
		};
		args.SetAffectedSkills(affectedSkills);
		args.SetFocusToSkills(_focusToAdd);
		args.SetLevelToSkills(_skillLevelToAdd);
		args.SetLevelToAttribute(DefaultCharacterAttributes.Endurance, _attributeLevelToAdd);
	}

	private bool YouthRiderLowRegisterOptionOnCondition(CharacterCreationManager characterCreationManager)
	{
		if (!(characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "aserai"))
		{
			return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "sturgia";
		}
		return true;
	}

	private void YouthRiderLowRegisterOptionOnSelect(CharacterCreationManager characterCreationManager)
	{
		characterCreationManager.CharacterCreationContent.SelectedTitleType = "hunter";
		string playerEquipmentId = GetPlayerEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedTitleType, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId, Hero.MainHero.IsFemale);
		foreach (NarrativeMenuCharacter character in characterCreationManager.CurrentMenu.Characters)
		{
			if (character.StringId == "player_youth_character")
			{
				character.SetAnimationId("act_sturgia_mp_huskarl_idle");
				character.SetEquipment(Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(playerEquipmentId));
			}
		}
	}

	private void GetYouthInfantryOptionArgs(NarrativeMenuOptionArgs args)
	{
		SkillObject[] affectedSkills = new SkillObject[2]
		{
			DefaultSkills.Polearm,
			DefaultSkills.OneHanded
		};
		args.SetAffectedSkills(affectedSkills);
		args.SetFocusToSkills(_focusToAdd);
		args.SetLevelToSkills(_skillLevelToAdd);
		args.SetLevelToAttribute(DefaultCharacterAttributes.Vigor, _attributeLevelToAdd);
	}

	private bool YouthInfantryOptionOnCondition(CharacterCreationManager characterCreationManager)
	{
		if (!(characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "empire") && !(characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "vlandia") && !(characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "khuzait") && !(characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "aserai") && !(characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "battania"))
		{
			return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "sturgia";
		}
		return true;
	}

	private void YouthInfantryOptionOnSelect(CharacterCreationManager characterCreationManager)
	{
		characterCreationManager.CharacterCreationContent.SelectedTitleType = "infantry";
		string playerEquipmentId = GetPlayerEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedTitleType, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId, Hero.MainHero.IsFemale);
		foreach (NarrativeMenuCharacter character in characterCreationManager.CurrentMenu.Characters)
		{
			if (character.StringId == "player_youth_character")
			{
				character.SetAnimationId("act_childhood_fierce");
				character.SetEquipment(Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(playerEquipmentId));
			}
		}
	}

	private void GetYouthSkirmisherOptionArgs(NarrativeMenuOptionArgs args)
	{
		SkillObject[] affectedSkills = new SkillObject[2]
		{
			DefaultSkills.Throwing,
			DefaultSkills.OneHanded
		};
		args.SetAffectedSkills(affectedSkills);
		args.SetFocusToSkills(_focusToAdd);
		args.SetLevelToSkills(_skillLevelToAdd);
		args.SetLevelToAttribute(DefaultCharacterAttributes.Control, _attributeLevelToAdd);
	}

	private bool YouthSkirmisherOptionOnCondition(CharacterCreationManager characterCreationManager)
	{
		if (!(characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "empire") && !(characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "vlandia") && !(characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "khuzait") && !(characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "aserai"))
		{
			return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "sturgia";
		}
		return true;
	}

	private void YouthSkirmisherOptionOnSelect(CharacterCreationManager characterCreationManager)
	{
		characterCreationManager.CharacterCreationContent.SelectedTitleType = "skirmisher";
		string playerEquipmentId = GetPlayerEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedTitleType, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId, Hero.MainHero.IsFemale);
		foreach (NarrativeMenuCharacter character in characterCreationManager.CurrentMenu.Characters)
		{
			if (character.StringId == "player_youth_character")
			{
				character.SetAnimationId("act_childhood_fox");
				character.SetEquipment(Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(playerEquipmentId));
			}
		}
	}

	private void GetYouthKernOptionArgs(NarrativeMenuOptionArgs args)
	{
		SkillObject[] affectedSkills = new SkillObject[2]
		{
			DefaultSkills.Throwing,
			DefaultSkills.OneHanded
		};
		args.SetAffectedSkills(affectedSkills);
		args.SetFocusToSkills(_focusToAdd);
		args.SetLevelToSkills(_skillLevelToAdd);
		args.SetLevelToAttribute(DefaultCharacterAttributes.Control, _attributeLevelToAdd);
	}

	private bool YouthKernOptionOnCondition(CharacterCreationManager characterCreationManager)
	{
		return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "battania";
	}

	private void YouthKernOptionOnSelect(CharacterCreationManager characterCreationManager)
	{
		characterCreationManager.CharacterCreationContent.SelectedTitleType = "kern";
		string playerEquipmentId = GetPlayerEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedTitleType, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId, Hero.MainHero.IsFemale);
		foreach (NarrativeMenuCharacter character in characterCreationManager.CurrentMenu.Characters)
		{
			if (character.StringId == "player_youth_character")
			{
				character.SetAnimationId("act_childhood_apprentice");
				character.SetEquipment(Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(playerEquipmentId));
			}
		}
	}

	private void GetYouthCampOptionArgs(NarrativeMenuOptionArgs args)
	{
		SkillObject[] affectedSkills = new SkillObject[2]
		{
			DefaultSkills.Roguery,
			DefaultSkills.Throwing
		};
		args.SetAffectedSkills(affectedSkills);
		args.SetFocusToSkills(_focusToAdd);
		args.SetLevelToSkills(_skillLevelToAdd);
		args.SetLevelToAttribute(DefaultCharacterAttributes.Cunning, _attributeLevelToAdd);
	}

	private bool YouthCampOptionOnCondition(CharacterCreationManager characterCreationManager)
	{
		if (!(characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "vlandia"))
		{
			return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "sturgia";
		}
		return true;
	}

	private void YouthCampOptionOnSelect(CharacterCreationManager characterCreationManager)
	{
		characterCreationManager.CharacterCreationContent.SelectedTitleType = "bard";
		string playerEquipmentId = GetPlayerEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedTitleType, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId, Hero.MainHero.IsFemale);
		foreach (NarrativeMenuCharacter character in characterCreationManager.CurrentMenu.Characters)
		{
			if (character.StringId == "player_youth_character")
			{
				character.SetAnimationId("act_childhood_militia");
				character.SetEquipment(Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(playerEquipmentId));
			}
		}
	}

	private void GetEnvoysGuardFirstOptionArgs(NarrativeMenuOptionArgs args)
	{
		SkillObject[] affectedSkills = new SkillObject[2]
		{
			DefaultSkills.Charm,
			DefaultSkills.Scouting
		};
		args.SetAffectedSkills(affectedSkills);
		args.SetFocusToSkills(_focusToAdd);
		args.SetLevelToSkills(_skillLevelToAdd);
		args.SetLevelToAttribute(DefaultCharacterAttributes.Social, _attributeLevelToAdd);
	}

	private void GetEnvoysGuardSecondOptionArgs(NarrativeMenuOptionArgs args)
	{
		SkillObject[] affectedSkills = new SkillObject[2]
		{
			DefaultSkills.Charm,
			DefaultSkills.Scouting
		};
		args.SetAffectedSkills(affectedSkills);
		args.SetFocusToSkills(_focusToAdd);
		args.SetLevelToSkills(_skillLevelToAdd);
		args.SetLevelToAttribute(DefaultCharacterAttributes.Social, _attributeLevelToAdd);
	}

	private bool EnvoysGuardFirstOptionOnCondition(CharacterCreationManager characterCreationManager)
	{
		if (!(characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "empire"))
		{
			return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "khuzait";
		}
		return true;
	}

	private bool EnvoysGuardSecondOptionOnCondition(CharacterCreationManager characterCreationManager)
	{
		if (!(characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "battania"))
		{
			return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "aserai";
		}
		return true;
	}

	private void EnvoysGuardFirstOptionOnSelect(CharacterCreationManager characterCreationManager)
	{
		characterCreationManager.CharacterCreationContent.SelectedTitleType = "guard";
		string playerEquipmentId = GetPlayerEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedTitleType, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId, Hero.MainHero.IsFemale);
		foreach (NarrativeMenuCharacter character in characterCreationManager.CurrentMenu.Characters)
		{
			if (character.StringId == "player_youth_character")
			{
				character.SetAnimationId("act_childhood_sharp");
				character.SetEquipment(Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(playerEquipmentId));
			}
		}
	}

	private void EnvoysGuardSecondOptionOnSelect(CharacterCreationManager characterCreationManager)
	{
		characterCreationManager.CharacterCreationContent.SelectedTitleType = "guard";
		string playerEquipmentId = GetPlayerEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedTitleType, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId, Hero.MainHero.IsFemale);
		foreach (NarrativeMenuCharacter character in characterCreationManager.CurrentMenu.Characters)
		{
			if (character.StringId == "player_youth_character")
			{
				character.SetAnimationId("act_childhood_sharp");
				character.SetEquipment(Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(playerEquipmentId));
			}
		}
	}

	private List<NarrativeMenuCharacterArgs> GetAdultMenuNarrativeMenuCharacterArgs(CultureObject culture, string occupationType, CharacterCreationManager characterCreationManager)
	{
		List<NarrativeMenuCharacterArgs> list = new List<NarrativeMenuCharacterArgs>();
		string playerEquipmentId = GetPlayerEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedTitleType, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId, Hero.MainHero.IsFemale);
		list.Add(new NarrativeMenuCharacterArgs("player_adulthood_character", 20, playerEquipmentId, "act_childhood_schooled", "spawnpoint_player_1", "", "", null, isHuman: true, CharacterObject.PlayerCharacter.IsFemale));
		MBEquipmentRoster mBEquipmentRoster = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(playerEquipmentId);
		list.Add(new NarrativeMenuCharacterArgs(mountCreationKey: MountCreationKey.GetRandomMountKey(mBEquipmentRoster.DefaultEquipment[EquipmentIndex.ArmorItemEndSlot].Item, CharacterObject.PlayerCharacter.GetMountKeySeed()), characterId: "narrative_character_horse", age: -1, equipmentId: "", animationId: "act_horse_stand_1", spawnPointEntityId: "spawnpoint_mount_1", leftHandItemId: mBEquipmentRoster.DefaultEquipment[EquipmentIndex.ArmorItemEndSlot].Item.StringId, rightHandItemId: mBEquipmentRoster.DefaultEquipment[EquipmentIndex.HorseHarness].Item.StringId, isHuman: false));
		return list;
	}

	private void AddAdulthoodMenu(CharacterCreationManager characterCreationManager)
	{
		BodyProperties originalBodyProperties = CharacterObject.PlayerCharacter.GetBodyProperties(CharacterObject.PlayerCharacter.Equipment);
		originalBodyProperties = FaceGen.GetBodyPropertiesWithAge(ref originalBodyProperties, 20f);
		NarrativeMenuCharacter item = new NarrativeMenuCharacter("player_adulthood_character", originalBodyProperties, CharacterObject.PlayerCharacter.Race, CharacterObject.PlayerCharacter.IsFemale);
		NarrativeMenuCharacter item2 = new NarrativeMenuCharacter("narrative_character_horse");
		List<NarrativeMenuCharacter> list = new List<NarrativeMenuCharacter>();
		list.Add(item);
		list.Add(item2);
		MBTextManager.SetTextVariable("EXP_VALUE", _skillLevelToAdd);
		NarrativeMenu narrativeMenu = new NarrativeMenu("narrative_adulthood_menu", "narrative_youth_menu", "narrative_age_selection_menu", new TextObject("{=MafIe9yI}Young Adulthood"), new TextObject("{=4WYY0X59}Before you set out for a life of adventure, your biggest achievement was..."), list, GetAdultMenuNarrativeMenuCharacterArgs);
		AddAdulthoodMenuOptions(narrativeMenu);
		characterCreationManager.AddNewMenu(narrativeMenu);
	}

	private void AddAdulthoodMenuOptions(NarrativeMenu narrativeMenu)
	{
		NarrativeMenuOption narrativeMenuOption = new NarrativeMenuOption("adulthood_defeated_enemy_option", new TextObject("{=8bwpVpgy}you defeated an enemy in battle."), new TextObject("{=1IEroJKs}Not everyone who musters for the levy marches to war, and not everyone who goes on campaign sees action. You did both, and you also took down an enemy warrior in direct one-to-one combat, in the full view of your comrades."), GetAdulthoodDefeatedEnemyOptionArgs, AdulthoodDefeatedEnemyOptionOnCondition, AdulthoodDefeatedEnemyOptionOnSelect, null);
		narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption);
		NarrativeMenuOption narrativeMenuOption2 = new NarrativeMenuOption("adulthood_manhunt_option", new TextObject("{=mP3uFbcq}you led a successful manhunt."), new TextObject("{=4f5xwzX0}When your community needed to organize a posse to pursue horse thieves, you were the obvious choice. You hunted down the raiders, surrounded them and forced their surrender, and took back your stolen property."), GetAdulthoodManhuntOptionArgs, AdulthoodManhuntOptionOnCondition, AdulthoodManhuntOptionOnSelect, null);
		narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption2);
		NarrativeMenuOption narrativeMenuOption3 = new NarrativeMenuOption("adulthood_caravan_leader_option", new TextObject("{=wfbtS71d}you led a caravan."), new TextObject("{=joRHKCkm}Your family needed someone trustworthy to take a caravan to a neighboring town. You organized supplies, ensured a constant watch to keep away bandits, and brought it safely to its destination."), GetAdulthoodCaravanLeaderOptionArgs, AdulthoodCaravanLeaderOptionOnCondition, AdulthoodCaravanLeaderOptionOnSelect, null);
		narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption3);
		NarrativeMenuOption narrativeMenuOption4 = new NarrativeMenuOption("adulthood_saved_village_option", new TextObject("{=x1HTX5hq}you saved your village from a flood."), new TextObject("{=bWlmGDf3}When a sudden storm caused the local stream to rise suddenly, your neighbors needed quick-thinking leadership. You provided it, directing them to build levees to save their homes."), GetAdulthoodSavedVillageOptionArgs, AdulthoodSavedVillageOptionOnCondition, AdulthoodSavedVillageOptionOnSelect, null);
		narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption4);
		NarrativeMenuOption narrativeMenuOption5 = new NarrativeMenuOption("adulthood_saved_city_option", new TextObject("{=s8PNllPN}you saved your city quarter from a fire."), new TextObject("{=ZAGR6PYc}When a sudden blaze broke out in a back alley, your neighbors needed quick-thinking leadership and you provided it. You organized a bucket line to the nearest well, putting the fire out before any homes were lost."), GetAdulthoodSavedCityOptionArgs, AdulthoodSavedCityOptionOnCondition, AdulthoodSavedCityOptionOnSelect, null);
		narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption5);
		NarrativeMenuOption narrativeMenuOption6 = new NarrativeMenuOption("adulthood_workshop_option", new TextObject("{=xORjDTal}you invested some money in a workshop."), new TextObject("{=PyVqDLBu}Your parents didn't give you much money, but they did leave just enough for you to secure a loan against a larger amount to build a small workshop. You paid back what you borrowed, and sold your enterprise for a profit."), GetAdulthoodWorkshopOptionArgs, AdulthoodWorkshopOptionOnCondition, AdulthoodWorkshopOptionOnSelect, null);
		narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption6);
		NarrativeMenuOption narrativeMenuOption7 = new NarrativeMenuOption("adulthood_investor_option", new TextObject("{=xKXcqRJI}you invested some money in land."), new TextObject("{=cbF9jdQo}Your parents didn't give you much money, but they did leave just enough for you to purchase a plot of unused land at the edge of the village. You cleared away rocks and dug an irrigation ditch, raised a few seasons of crops, than sold it for a considerable profit."), GetAdulthoodInvestorOptionArgs, AdulthoodInvestorOptionOnCondition, AdulthoodInvestorOptionOnSelect, null);
		narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption7);
		NarrativeMenuOption narrativeMenuOption8 = new NarrativeMenuOption("adulthood_hunter_option", new TextObject("{=TbNRtUjb}you hunted a dangerous animal."), new TextObject("{=I3PcdaaL}Wolves, bears are a constant menace to the flocks of northern Calradia, while hyenas and leopards trouble the south. You went with a group of your fellow villagers and fired the missile that brought down the beast."), GetAdulthoodHunterOptionArgs, AdulthoodHunterOptionOnCondition, AdulthoodHunterOptionOnSelect, null);
		narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption8);
		NarrativeMenuOption narrativeMenuOption9 = new NarrativeMenuOption("adulthood_siege_survivor_option", new TextObject("{=WbHfGCbd}you survived a siege."), new TextObject("{=FhZPjhli}Your hometown was briefly placed under siege, and you were called to defend the walls. Everyone did their part to repulse the enemy assault, and everyone is justly proud of what they endured."), GetAdulthoodSiegeSurvivorOptionArgs, AdulthoodSiegeSurvivorOptionOnCondition, AdulthoodSiegeSurvivorOptionOnSelect, null);
		narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption9);
		NarrativeMenuOption narrativeMenuOption10 = new NarrativeMenuOption("adulthood_escapade_high_register_option", new TextObject("{=kNXet6Um}you had a famous escapade in town."), new TextObject("{=DjeAJtix}Maybe it was a love affair, or maybe you cheated at dice, or maybe you just chose your words poorly when drinking with a dangerous crowd. Anyway, on one of your trips into town you got into the kind of trouble from which only a quick tongue or quick feet get you out alive."), GetAdulthoodEscapadeHighRegisterOptionArgs, AdulthoodEscapadeHighRegisterOptionOnCondition, AdulthoodEscapadeHighRegisterOptionOnSelect, null);
		narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption10);
		NarrativeMenuOption narrativeMenuOption11 = new NarrativeMenuOption("adulthood_escapade_low_register_option", new TextObject("{=qlOuiKXj}you had a famous escapade."), new TextObject("{=lD5Ob3R4}Maybe it was a love affair, or maybe you cheated at dice, or maybe you just chose your words poorly when drinking with a dangerous crowd. Anyway, you got into the kind of trouble from which only a quick tongue or quick feet get you out alive."), GetAdulthoodEscapadeLowRegisterOptionArgs, AdulthoodEscapadeLowRegisterOptionOnCondition, AdulthoodEscapadeLowRegisterOptionOnSelect, null);
		narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption11);
		NarrativeMenuOption narrativeMenuOption12 = new NarrativeMenuOption("adulthood_nice_person_option", new TextObject("{=Yqm0Dics}you treated people well."), new TextObject("{=dDmcqTzb}Yours wasn't the kind of reputation that local legends are made of, but it was the kind that wins you respect among those around you. You were consistently fair and honest in your business dealings and helpful to those in trouble. In doing so, you got a sense of what made people tick."), GetAdulthoodNicePersonOptionArgs, AdulthoodNicePersonOptionOnCondition, AdulthoodNicePersonOptionOnSelect, null);
		narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption12);
	}

	private void GetAdulthoodDefeatedEnemyOptionArgs(NarrativeMenuOptionArgs args)
	{
		SkillObject[] affectedSkills = new SkillObject[2]
		{
			DefaultSkills.OneHanded,
			DefaultSkills.TwoHanded
		};
		args.SetAffectedSkills(affectedSkills);
		args.SetFocusToSkills(_focusToAdd);
		args.SetLevelToSkills(_skillLevelToAdd);
		args.SetLevelToAttribute(DefaultCharacterAttributes.Vigor, _attributeLevelToAdd);
		TraitObject[] affectedTraits = new TraitObject[1] { DefaultTraits.Valor };
		args.SetAffectedTraits(affectedTraits);
		args.SetLevelToTraits(1);
		args.SetRenownToAdd(20);
	}

	private bool AdulthoodDefeatedEnemyOptionOnCondition(CharacterCreationManager characterCreationManager)
	{
		return true;
	}

	private void AdulthoodDefeatedEnemyOptionOnSelect(CharacterCreationManager characterCreationManager)
	{
		foreach (NarrativeMenuCharacter character in characterCreationManager.CurrentMenu.Characters)
		{
			if (character.StringId == "player_adulthood_character")
			{
				character.SetAnimationId("act_childhood_athlete");
			}
		}
	}

	private void GetAdulthoodManhuntOptionArgs(NarrativeMenuOptionArgs args)
	{
		SkillObject[] affectedSkills = new SkillObject[2]
		{
			DefaultSkills.Tactics,
			DefaultSkills.Leadership
		};
		args.SetAffectedSkills(affectedSkills);
		args.SetFocusToSkills(_focusToAdd);
		args.SetLevelToSkills(_skillLevelToAdd);
		args.SetLevelToAttribute(DefaultCharacterAttributes.Cunning, _attributeLevelToAdd);
		TraitObject[] affectedTraits = new TraitObject[1] { DefaultTraits.Calculating };
		args.SetAffectedTraits(affectedTraits);
		args.SetLevelToTraits(1);
		args.SetRenownToAdd(10);
	}

	private bool AdulthoodManhuntOptionOnCondition(CharacterCreationManager characterCreationManager)
	{
		if (!CharacterOccupationTypes.IsUrbanOccupation(characterCreationManager.CharacterCreationContent.SelectedParentOccupation))
		{
			if (!(characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "vlandia") && !(characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "empire") && !(characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "aserai") && !(characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "battania"))
			{
				return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "khuzait";
			}
			return true;
		}
		return false;
	}

	private void AdulthoodManhuntOptionOnSelect(CharacterCreationManager characterCreationManager)
	{
		foreach (NarrativeMenuCharacter character in characterCreationManager.CurrentMenu.Characters)
		{
			if (character.StringId == "player_adulthood_character")
			{
				character.SetAnimationId("act_battania_mp_clan_warrior_shieldperk_idle");
			}
		}
	}

	private void GetAdulthoodCaravanLeaderOptionArgs(NarrativeMenuOptionArgs args)
	{
		SkillObject[] affectedSkills = new SkillObject[2]
		{
			DefaultSkills.Trade,
			DefaultSkills.Leadership
		};
		args.SetAffectedSkills(affectedSkills);
		args.SetFocusToSkills(_focusToAdd);
		args.SetLevelToSkills(_skillLevelToAdd);
		args.SetLevelToAttribute(DefaultCharacterAttributes.Cunning, _attributeLevelToAdd);
		TraitObject[] affectedTraits = new TraitObject[1] { DefaultTraits.Calculating };
		args.SetAffectedTraits(affectedTraits);
		args.SetLevelToTraits(1);
		args.SetRenownToAdd(10);
	}

	private bool AdulthoodCaravanLeaderOptionOnCondition(CharacterCreationManager characterCreationManager)
	{
		if (CharacterOccupationTypes.IsUrbanOccupation(characterCreationManager.CharacterCreationContent.SelectedParentOccupation))
		{
			if (!(characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "vlandia") && !(characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "sturgia") && !(characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "empire") && !(characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "aserai") && !(characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "khuzait"))
			{
				return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "nord";
			}
			return true;
		}
		return false;
	}

	private void AdulthoodCaravanLeaderOptionOnSelect(CharacterCreationManager characterCreationManager)
	{
		foreach (NarrativeMenuCharacter character in characterCreationManager.CurrentMenu.Characters)
		{
			if (character.StringId == "player_adulthood_character")
			{
				character.SetAnimationId("act_childhood_ready_handshield");
			}
		}
	}

	private void GetAdulthoodSavedVillageOptionArgs(NarrativeMenuOptionArgs args)
	{
		SkillObject[] affectedSkills = new SkillObject[2]
		{
			DefaultSkills.Tactics,
			DefaultSkills.Leadership
		};
		args.SetAffectedSkills(affectedSkills);
		args.SetFocusToSkills(_focusToAdd);
		args.SetLevelToSkills(_skillLevelToAdd);
		args.SetLevelToAttribute(DefaultCharacterAttributes.Cunning, _attributeLevelToAdd);
		TraitObject[] affectedTraits = new TraitObject[1] { DefaultTraits.Valor };
		args.SetAffectedTraits(affectedTraits);
		args.SetLevelToTraits(1);
		args.SetRenownToAdd(10);
	}

	private bool AdulthoodSavedVillageOptionOnCondition(CharacterCreationManager characterCreationManager)
	{
		if (!CharacterOccupationTypes.IsUrbanOccupation(characterCreationManager.CharacterCreationContent.SelectedParentOccupation))
		{
			if (!(characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "sturgia"))
			{
				return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "nord";
			}
			return true;
		}
		return false;
	}

	private void AdulthoodSavedVillageOptionOnSelect(CharacterCreationManager characterCreationManager)
	{
		foreach (NarrativeMenuCharacter character in characterCreationManager.CurrentMenu.Characters)
		{
			if (character.StringId == "player_adulthood_character")
			{
				character.SetAnimationId("act_drafted_to_war_pose");
			}
		}
	}

	private void GetAdulthoodSavedCityOptionArgs(NarrativeMenuOptionArgs args)
	{
		SkillObject[] affectedSkills = new SkillObject[2]
		{
			DefaultSkills.Tactics,
			DefaultSkills.Leadership
		};
		args.SetAffectedSkills(affectedSkills);
		args.SetFocusToSkills(_focusToAdd);
		args.SetLevelToSkills(_skillLevelToAdd);
		args.SetLevelToAttribute(DefaultCharacterAttributes.Cunning, _attributeLevelToAdd);
		TraitObject[] affectedTraits = new TraitObject[1] { DefaultTraits.Calculating };
		args.SetAffectedTraits(affectedTraits);
		args.SetLevelToTraits(1);
		args.SetRenownToAdd(10);
	}

	private bool AdulthoodSavedCityOptionOnCondition(CharacterCreationManager characterCreationManager)
	{
		if (CharacterOccupationTypes.IsUrbanOccupation(characterCreationManager.CharacterCreationContent.SelectedParentOccupation))
		{
			return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "battania";
		}
		return false;
	}

	private void AdulthoodSavedCityOptionOnSelect(CharacterCreationManager characterCreationManager)
	{
		foreach (NarrativeMenuCharacter character in characterCreationManager.CurrentMenu.Characters)
		{
			if (character.StringId == "player_adulthood_character")
			{
				character.SetAnimationId("act_childhood_vibrant");
			}
		}
	}

	private void GetAdulthoodWorkshopOptionArgs(NarrativeMenuOptionArgs args)
	{
		SkillObject[] affectedSkills = new SkillObject[2]
		{
			DefaultSkills.Trade,
			DefaultSkills.Crafting
		};
		args.SetAffectedSkills(affectedSkills);
		args.SetFocusToSkills(_focusToAdd);
		args.SetLevelToSkills(_skillLevelToAdd);
		args.SetLevelToAttribute(DefaultCharacterAttributes.Intelligence, _attributeLevelToAdd);
		TraitObject[] affectedTraits = new TraitObject[1] { DefaultTraits.Calculating };
		args.SetAffectedTraits(affectedTraits);
		args.SetLevelToTraits(1);
		args.SetRenownToAdd(10);
	}

	private bool AdulthoodWorkshopOptionOnCondition(CharacterCreationManager characterCreationManager)
	{
		return CharacterOccupationTypes.IsUrbanOccupation(characterCreationManager.CharacterCreationContent.SelectedParentOccupation);
	}

	private void AdulthoodWorkshopOptionOnSelect(CharacterCreationManager characterCreationManager)
	{
		foreach (NarrativeMenuCharacter character in characterCreationManager.CurrentMenu.Characters)
		{
			if (character.StringId == "player_adulthood_character")
			{
				character.SetAnimationId("act_childhood_decisive");
			}
		}
	}

	private void GetAdulthoodInvestorOptionArgs(NarrativeMenuOptionArgs args)
	{
		SkillObject[] affectedSkills = new SkillObject[2]
		{
			DefaultSkills.Trade,
			DefaultSkills.Crafting
		};
		args.SetAffectedSkills(affectedSkills);
		args.SetFocusToSkills(_focusToAdd);
		args.SetLevelToSkills(_skillLevelToAdd);
		args.SetLevelToAttribute(DefaultCharacterAttributes.Intelligence, _attributeLevelToAdd);
		TraitObject[] affectedTraits = new TraitObject[1] { DefaultTraits.Calculating };
		args.SetAffectedTraits(affectedTraits);
		args.SetLevelToTraits(1);
		args.SetRenownToAdd(10);
	}

	private bool AdulthoodInvestorOptionOnCondition(CharacterCreationManager characterCreationManager)
	{
		return !CharacterOccupationTypes.IsUrbanOccupation(characterCreationManager.CharacterCreationContent.SelectedParentOccupation);
	}

	private void AdulthoodInvestorOptionOnSelect(CharacterCreationManager characterCreationManager)
	{
		foreach (NarrativeMenuCharacter character in characterCreationManager.CurrentMenu.Characters)
		{
			if (character.StringId == "player_adulthood_character")
			{
				character.SetAnimationId("act_childhood_decisive");
			}
		}
	}

	private void GetAdulthoodHunterOptionArgs(NarrativeMenuOptionArgs args)
	{
		SkillObject[] affectedSkills = new SkillObject[2]
		{
			DefaultSkills.Polearm,
			DefaultSkills.Athletics
		};
		args.SetAffectedSkills(affectedSkills);
		args.SetFocusToSkills(_focusToAdd);
		args.SetLevelToSkills(_skillLevelToAdd);
		args.SetLevelToAttribute(DefaultCharacterAttributes.Control, _attributeLevelToAdd);
		TraitObject[] affectedTraits = new TraitObject[1] { DefaultTraits.Valor };
		args.SetAffectedTraits(affectedTraits);
		args.SetLevelToTraits(1);
		args.SetRenownToAdd(5);
	}

	private bool AdulthoodHunterOptionOnCondition(CharacterCreationManager characterCreationManager)
	{
		return !CharacterOccupationTypes.IsUrbanOccupation(characterCreationManager.CharacterCreationContent.SelectedParentOccupation);
	}

	private void AdulthoodHunterOptionOnSelect(CharacterCreationManager characterCreationManager)
	{
		foreach (NarrativeMenuCharacter character in characterCreationManager.CurrentMenu.Characters)
		{
			if (character.StringId == "player_adulthood_character")
			{
				character.SetAnimationId("act_childhood_tough");
			}
		}
	}

	private void GetAdulthoodSiegeSurvivorOptionArgs(NarrativeMenuOptionArgs args)
	{
		SkillObject[] affectedSkills = new SkillObject[2]
		{
			DefaultSkills.Bow,
			DefaultSkills.Crossbow
		};
		args.SetAffectedSkills(affectedSkills);
		args.SetFocusToSkills(_focusToAdd);
		args.SetLevelToSkills(_skillLevelToAdd);
		args.SetLevelToAttribute(DefaultCharacterAttributes.Control, _attributeLevelToAdd);
		args.SetRenownToAdd(5);
	}

	private bool AdulthoodSiegeSurvivorOptionOnCondition(CharacterCreationManager characterCreationManager)
	{
		return CharacterOccupationTypes.IsUrbanOccupation(characterCreationManager.CharacterCreationContent.SelectedParentOccupation);
	}

	private void AdulthoodSiegeSurvivorOptionOnSelect(CharacterCreationManager characterCreationManager)
	{
		foreach (NarrativeMenuCharacter character in characterCreationManager.CurrentMenu.Characters)
		{
			if (character.StringId == "player_adulthood_character")
			{
				character.SetAnimationId("act_childhood_tough");
			}
		}
	}

	private void GetAdulthoodEscapadeHighRegisterOptionArgs(NarrativeMenuOptionArgs args)
	{
		SkillObject[] affectedSkills = new SkillObject[2]
		{
			DefaultSkills.Athletics,
			DefaultSkills.Roguery
		};
		args.SetAffectedSkills(affectedSkills);
		args.SetFocusToSkills(_focusToAdd);
		args.SetLevelToSkills(_skillLevelToAdd);
		args.SetLevelToAttribute(DefaultCharacterAttributes.Endurance, _attributeLevelToAdd);
		TraitObject[] affectedTraits = new TraitObject[1] { DefaultTraits.Valor };
		args.SetAffectedTraits(affectedTraits);
		args.SetLevelToTraits(1);
		args.SetRenownToAdd(5);
	}

	private bool AdulthoodEscapadeHighRegisterOptionOnCondition(CharacterCreationManager characterCreationManager)
	{
		return !CharacterOccupationTypes.IsUrbanOccupation(characterCreationManager.CharacterCreationContent.SelectedParentOccupation);
	}

	private void AdulthoodEscapadeHighRegisterOptionOnSelect(CharacterCreationManager characterCreationManager)
	{
		foreach (NarrativeMenuCharacter character in characterCreationManager.CurrentMenu.Characters)
		{
			if (character.StringId == "player_adulthood_character")
			{
				character.SetAnimationId("act_childhood_clever");
			}
		}
	}

	private void GetAdulthoodEscapadeLowRegisterOptionArgs(NarrativeMenuOptionArgs args)
	{
		SkillObject[] affectedSkills = new SkillObject[2]
		{
			DefaultSkills.Athletics,
			DefaultSkills.Roguery
		};
		args.SetAffectedSkills(affectedSkills);
		args.SetFocusToSkills(_focusToAdd);
		args.SetLevelToSkills(_skillLevelToAdd);
		args.SetLevelToAttribute(DefaultCharacterAttributes.Endurance, _attributeLevelToAdd);
		TraitObject[] affectedTraits = new TraitObject[1] { DefaultTraits.Valor };
		args.SetAffectedTraits(affectedTraits);
		args.SetLevelToTraits(1);
		args.SetRenownToAdd(5);
	}

	private bool AdulthoodEscapadeLowRegisterOptionOnCondition(CharacterCreationManager characterCreationManager)
	{
		return CharacterOccupationTypes.IsUrbanOccupation(characterCreationManager.CharacterCreationContent.SelectedParentOccupation);
	}

	private void AdulthoodEscapadeLowRegisterOptionOnSelect(CharacterCreationManager characterCreationManager)
	{
		foreach (NarrativeMenuCharacter character in characterCreationManager.CurrentMenu.Characters)
		{
			if (character.StringId == "player_adulthood_character")
			{
				character.SetAnimationId("act_childhood_clever");
			}
		}
	}

	private void GetAdulthoodNicePersonOptionArgs(NarrativeMenuOptionArgs args)
	{
		SkillObject[] affectedSkills = new SkillObject[2]
		{
			DefaultSkills.Charm,
			DefaultSkills.Steward
		};
		args.SetAffectedSkills(affectedSkills);
		args.SetFocusToSkills(_focusToAdd);
		args.SetLevelToSkills(_skillLevelToAdd);
		args.SetLevelToAttribute(DefaultCharacterAttributes.Social, _attributeLevelToAdd);
		TraitObject[] affectedTraits = new TraitObject[3]
		{
			DefaultTraits.Mercy,
			DefaultTraits.Generosity,
			DefaultTraits.Honor
		};
		args.SetAffectedTraits(affectedTraits);
		args.SetLevelToTraits(1);
		args.SetRenownToAdd(5);
	}

	private bool AdulthoodNicePersonOptionOnCondition(CharacterCreationManager characterCreationManager)
	{
		return true;
	}

	private void AdulthoodNicePersonOptionOnSelect(CharacterCreationManager characterCreationManager)
	{
		foreach (NarrativeMenuCharacter character in characterCreationManager.CurrentMenu.Characters)
		{
			if (character.StringId == "player_adulthood_character")
			{
				character.SetAnimationId("act_childhood_manners");
			}
		}
	}

	private List<NarrativeMenuCharacterArgs> GetAgeSelectionMenuNarrativeMenuCharacterArgs(CultureObject culture, string occupationType, CharacterCreationManager characterCreationManager)
	{
		List<NarrativeMenuCharacterArgs> list = new List<NarrativeMenuCharacterArgs>();
		string playerEquipmentId = GetPlayerEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedTitleType, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId, Hero.MainHero.IsFemale);
		list.Add(new NarrativeMenuCharacterArgs("player_age_selection_character", characterCreationManager.CharacterCreationContent.StartingAge, playerEquipmentId, "act_childhood_schooled", "spawnpoint_player_1", "", "", null, isHuman: true, CharacterObject.PlayerCharacter.IsFemale));
		MBEquipmentRoster mBEquipmentRoster = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(playerEquipmentId);
		list.Add(new NarrativeMenuCharacterArgs(mountCreationKey: MountCreationKey.GetRandomMountKey(mBEquipmentRoster.DefaultEquipment[EquipmentIndex.ArmorItemEndSlot].Item, CharacterObject.PlayerCharacter.GetMountKeySeed()), characterId: "narrative_character_horse", age: -1, equipmentId: "", animationId: "act_horse_stand_1", spawnPointEntityId: "spawnpoint_mount_1", leftHandItemId: mBEquipmentRoster.DefaultEquipment[EquipmentIndex.ArmorItemEndSlot].Item.StringId, rightHandItemId: mBEquipmentRoster.DefaultEquipment[EquipmentIndex.HorseHarness].Item.StringId, isHuman: false));
		return list;
	}

	private void AddAgeSelectionMenu(CharacterCreationManager characterCreationManager)
	{
		MBTextManager.SetTextVariable("EXP_VALUE", _skillLevelToAdd);
		BodyProperties originalBodyProperties = CharacterObject.PlayerCharacter.GetBodyProperties(CharacterObject.PlayerCharacter.Equipment);
		originalBodyProperties = FaceGen.GetBodyPropertiesWithAge(ref originalBodyProperties, characterCreationManager.CharacterCreationContent.StartingAge);
		NarrativeMenuCharacter item = new NarrativeMenuCharacter("player_age_selection_character", originalBodyProperties, CharacterObject.PlayerCharacter.Race, CharacterObject.PlayerCharacter.IsFemale);
		NarrativeMenuCharacter item2 = new NarrativeMenuCharacter("narrative_character_horse");
		List<NarrativeMenuCharacter> list = new List<NarrativeMenuCharacter>();
		list.Add(item);
		list.Add(item2);
		NarrativeMenu narrativeMenu = new NarrativeMenu("narrative_age_selection_menu", "narrative_adulthood_menu", "", new TextObject("{=HDFEAYDk}Starting Age"), new TextObject("{=VlOGrGSn}Your character started off on the adventuring path at the age of..."), list, GetAgeSelectionMenuNarrativeMenuCharacterArgs);
		NarrativeMenuOption narrativeMenuOption = new NarrativeMenuOption("age_selection_young_adult_option", new TextObject("{=!}20"), new TextObject("{=2k7adlh7}While lacking experience a bit, you are full with youthful energy, you are fully eager, for the long years of adventuring ahead."), GetAgeSelectionYoungAdultAgeOptionArgs, AgeSelectionYoungAdultAgeOptionOnCondition, AgeSelectionYoungAdultAgeOptionOnSelect, AgeSelectionYoungAdultAgeOptionOnConsequence);
		narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption);
		NarrativeMenuOption narrativeMenuOption2 = new NarrativeMenuOption("age_selection_adult_option", new TextObject("{=!}30"), new TextObject("{=NUlVFRtK}You are at your prime, You still have some youthful energy but also have a substantial amount of experience under your belt. "), GetAgeSelectionAdultOptionArgs, AgeSelectionAdultOptionOnCondition, AgeSelectionAdultOptionOnSelect, AgeSelectionAdultOptionOnConsequence);
		narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption2);
		NarrativeMenuOption narrativeMenuOption3 = new NarrativeMenuOption("age_selection_middle_age_option", new TextObject("{=!}40"), new TextObject("{=5MxTYApM}This is the right age for starting off, you have years of experience, and you are old enough for people to respect you and gather under your banner."), GetAgeSelectionMiddleAgeOptionArgs, AgeSelectionMiddleAgeOptionOnCondition, AgeSelectionMiddleAgeOptionOnSelect, AgeSelectionMiddleAgeOptionOnConsequence);
		narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption3);
		NarrativeMenuOption narrativeMenuOption4 = new NarrativeMenuOption("age_selection_elder_option", new TextObject("{=!}50"), new TextObject("{=ePD5Afvy}While you are past your prime, there is still enough time to go on that last big adventure for you. And you have all the experience you need to overcome anything!"), GetAgeSelectionElderOptionArgs, AgeSelectionElderOptionOnCondition, AgeSelectionElderOptionOnSelect, AgeSelectionElderOptionOnConsequence);
		narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption4);
		characterCreationManager.AddNewMenu(narrativeMenu);
	}

	private void GetAgeSelectionYoungAdultAgeOptionArgs(NarrativeMenuOptionArgs args)
	{
		args.SetUnspentFocusToAdd(2);
		args.SetUnspentAttributeToAdd(1);
	}

	private bool AgeSelectionYoungAdultAgeOptionOnCondition(CharacterCreationManager characterCreationManager)
	{
		return true;
	}

	private void AgeSelectionYoungAdultAgeOptionOnSelect(CharacterCreationManager characterCreationManager)
	{
		string playerEquipmentId = GetPlayerEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedTitleType, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId, Hero.MainHero.IsFemale);
		foreach (NarrativeMenuCharacter character in characterCreationManager.CurrentMenu.Characters)
		{
			if (character.StringId == "player_age_selection_character")
			{
				character.SetAnimationId("act_childhood_focus");
				character.ChangeAge(20f);
				MBEquipmentRoster mBEquipmentRoster = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(playerEquipmentId);
				if (mBEquipmentRoster == null)
				{
					Debug.FailedAssert("character creation menu character equipment should not be null!", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\CampaignBehaviors\\CharacterCreationCampaignBehavior.cs", "AgeSelectionYoungAdultAgeOptionOnSelect", 4884);
					mBEquipmentRoster = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>("player_char_creation_default");
				}
				character.SetEquipment(mBEquipmentRoster);
				break;
			}
		}
		characterCreationManager.CharacterCreationContent.StartingAge = 20;
		Hero.MainHero.SetBirthDay(CampaignTime.YearsFromNow(-20f));
	}

	private void AgeSelectionYoungAdultAgeOptionOnConsequence(CharacterCreationManager characterCreationManager)
	{
		characterCreationManager.CharacterCreationContent.StartingAge = 20;
		ApplyMainHeroEquipment(characterCreationManager);
	}

	private void GetAgeSelectionAdultOptionArgs(NarrativeMenuOptionArgs args)
	{
		args.SetUnspentFocusToAdd(4);
		args.SetUnspentAttributeToAdd(2);
	}

	private bool AgeSelectionAdultOptionOnCondition(CharacterCreationManager characterCreationManager)
	{
		return true;
	}

	private void AgeSelectionAdultOptionOnSelect(CharacterCreationManager characterCreationManager)
	{
		string playerEquipmentId = GetPlayerEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedTitleType, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId, Hero.MainHero.IsFemale);
		foreach (NarrativeMenuCharacter character in characterCreationManager.CurrentMenu.Characters)
		{
			if (character.StringId == "player_age_selection_character")
			{
				character.SetAnimationId("act_childhood_athlete");
				character.ChangeAge(30f);
				MBEquipmentRoster mBEquipmentRoster = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(playerEquipmentId);
				if (mBEquipmentRoster == null)
				{
					Debug.FailedAssert("character creation menu character equipment should not be null!", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\CampaignBehaviors\\CharacterCreationCampaignBehavior.cs", "AgeSelectionAdultOptionOnSelect", 4934);
					mBEquipmentRoster = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>("player_char_creation_default");
				}
				character.SetEquipment(mBEquipmentRoster);
				break;
			}
		}
		characterCreationManager.CharacterCreationContent.StartingAge = 30;
		Hero.MainHero.SetBirthDay(CampaignTime.YearsFromNow(-30f));
	}

	private void AgeSelectionAdultOptionOnConsequence(CharacterCreationManager characterCreationManager)
	{
		characterCreationManager.CharacterCreationContent.StartingAge = 30;
		ApplyMainHeroEquipment(characterCreationManager);
	}

	private void GetAgeSelectionMiddleAgeOptionArgs(NarrativeMenuOptionArgs args)
	{
		args.SetUnspentFocusToAdd(6);
		args.SetUnspentAttributeToAdd(3);
	}

	private bool AgeSelectionMiddleAgeOptionOnCondition(CharacterCreationManager characterCreationManager)
	{
		return true;
	}

	private void AgeSelectionMiddleAgeOptionOnSelect(CharacterCreationManager characterCreationManager)
	{
		string playerEquipmentId = GetPlayerEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedTitleType, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId, Hero.MainHero.IsFemale);
		foreach (NarrativeMenuCharacter character in characterCreationManager.CurrentMenu.Characters)
		{
			if (character.StringId == "player_age_selection_character")
			{
				character.SetAnimationId("act_childhood_sharp");
				character.ChangeAge(40f);
				MBEquipmentRoster mBEquipmentRoster = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(playerEquipmentId);
				if (mBEquipmentRoster == null)
				{
					Debug.FailedAssert("character creation menu character equipment should not be null!", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\CampaignBehaviors\\CharacterCreationCampaignBehavior.cs", "AgeSelectionMiddleAgeOptionOnSelect", 4984);
					mBEquipmentRoster = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>("player_char_creation_default");
				}
				character.SetEquipment(mBEquipmentRoster);
				break;
			}
		}
		characterCreationManager.CharacterCreationContent.StartingAge = 40;
		Hero.MainHero.SetBirthDay(CampaignTime.YearsFromNow(-40f));
	}

	private void AgeSelectionMiddleAgeOptionOnConsequence(CharacterCreationManager characterCreationManager)
	{
		characterCreationManager.CharacterCreationContent.StartingAge = 40;
		ApplyMainHeroEquipment(characterCreationManager);
	}

	private void GetAgeSelectionElderOptionArgs(NarrativeMenuOptionArgs args)
	{
		args.SetUnspentFocusToAdd(8);
		args.SetUnspentAttributeToAdd(4);
	}

	private bool AgeSelectionElderOptionOnCondition(CharacterCreationManager characterCreationManager)
	{
		return true;
	}

	private void AgeSelectionElderOptionOnSelect(CharacterCreationManager characterCreationManager)
	{
		string playerEquipmentId = GetPlayerEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedTitleType, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId, Hero.MainHero.IsFemale);
		foreach (NarrativeMenuCharacter character in characterCreationManager.CurrentMenu.Characters)
		{
			if (character.StringId == "player_age_selection_character")
			{
				character.SetAnimationId("act_childhood_tough");
				character.ChangeAge(50f);
				MBEquipmentRoster mBEquipmentRoster = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(playerEquipmentId);
				if (mBEquipmentRoster == null)
				{
					Debug.FailedAssert("character creation menu character equipment should not be null!", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\CampaignBehaviors\\CharacterCreationCampaignBehavior.cs", "AgeSelectionElderOptionOnSelect", 5034);
					mBEquipmentRoster = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>("player_char_creation_default");
				}
				character.SetEquipment(mBEquipmentRoster);
				break;
			}
		}
		characterCreationManager.CharacterCreationContent.StartingAge = 50;
		Hero.MainHero.SetBirthDay(CampaignTime.YearsFromNow(-50f));
	}

	private void AgeSelectionElderOptionOnConsequence(CharacterCreationManager characterCreationManager)
	{
		characterCreationManager.CharacterCreationContent.StartingAge = 50;
		ApplyMainHeroEquipment(characterCreationManager);
	}

	private void ApplyMainHeroEquipment(CharacterCreationManager characterCreationManager)
	{
		NarrativeMenu narrativeMenuWithId = characterCreationManager.GetNarrativeMenuWithId("narrative_age_selection_menu");
		NarrativeMenuCharacter narrativeMenuCharacter = null;
		foreach (NarrativeMenuCharacter character in narrativeMenuWithId.Characters)
		{
			if (character.StringId.Equals("player_age_selection_character"))
			{
				narrativeMenuCharacter = character;
				break;
			}
		}
		CharacterObject.PlayerCharacter.Equipment.FillFrom(narrativeMenuCharacter.Equipment.DefaultEquipment);
		CharacterObject.PlayerCharacter.FirstCivilianEquipment.FillFrom(narrativeMenuCharacter.Equipment.GetRandomCivilianEquipment());
	}

	public void SetHeroAge(float age)
	{
		Hero.MainHero.SetBirthDay(CampaignTime.YearsFromNow(0f - age));
	}
}
