using System.Collections.Generic;
using NavalDLC.CharacterDevelopment;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterCreationContent;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace NavalDLC.CampaignBehaviors;

public class NavalCharacterCreationCampaignBehavior : CampaignBehaviorBase, ICharacterCreationContentHandler
{
	private static class NavalCharacterOccupationTypes
	{
		public const string Retainer = "retainer";

		public const string Bard = "bard";

		public const string Hunter = "hunter";

		public const string Mercenary = "mercenary";

		public const string Infantry = "infantry";

		public const string Skirmisher = "skirmisher";

		public const string Artisan = "artisan";

		public const string Vagabond = "vagabond";

		public const string Guard = "guard";

		public const string ArtisanUrban = "artisan_urban";

		public const string MercenaryUrban = "mercenary_urban";

		public const string MerchantUrban = "merchant_urban";

		public const string VagabondUrban = "vagabond_urban";

		public const string RetainerUrban = "retainer_urban";

		public const string PhysicianUrban = "physician_urban";

		public const string HealerUrban = "healer_urban";

		public const string BardUrban = "bard_urban";

		public const string Seafarer = "seafarer";

		public const string ShipmasterUrban = "shipmaster_urban";

		public static bool IsUrbanOccupation(string occupation)
		{
			switch (occupation)
			{
			default:
				return occupation == "bard_urban";
			case "mercenary_urban":
			case "merchant_urban":
			case "vagabond_urban":
			case "artisan_urban":
			case "shipmaster_urban":
			case "retainer_urban":
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
		{ "mercenary", "mercenary" },
		{ "infantry", "infantry" },
		{ "skirmisher", "skirmisher" },
		{ "artisan", "artisan" },
		{ "vagabond", "vagabond" },
		{ "guard", "guard" },
		{ "artisan_urban", "artisan" },
		{ "mercenary_urban", "artisan" },
		{ "merchant_urban", "merchant" },
		{ "vagabond_urban", "vagabond" },
		{ "seafarer", "seafarer" },
		{ "shipmaster_urban", "shipmaster" }
	};

	public const string MotherNarrativeCharacterStringId = "mother_character";

	public const string FatherNarrativeCharacterStringId = "father_character";

	public const string PlayerChildhoodCharacterStringId = "player_childhood_character";

	public const string PlayerEducationCharacterStringId = "player_education_character";

	public const string PlayerYouthCharacterStringId = "player_youth_character";

	private int _focusToAdd;

	private int _skillLevelToAdd;

	private int _attributeLevelToAdd;

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
		characterCreationManager.RegisterCharacterCreationContentHandler(this, 1000);
	}

	void ICharacterCreationContentHandler.InitializeContent(CharacterCreationManager characterCreationManager)
	{
		characterCreationManager.CharacterCreationContent.AddEquipmentToUseGetter(delegate(string occupationId, out string equipmentId)
		{
			return _occupationToEquipmentMapping.TryGetValue(occupationId, out equipmentId);
		});
		InitializeCharacterCreationCultures(characterCreationManager);
		InitializeData(characterCreationManager);
	}

	void ICharacterCreationContentHandler.AfterInitializeContent(CharacterCreationManager characterCreationManager)
	{
	}

	void ICharacterCreationContentHandler.OnStageCompleted(CharacterCreationStageBase stage)
	{
	}

	void ICharacterCreationContentHandler.OnCharacterCreationFinalize(CharacterCreationManager characterCreationManager)
	{
	}

	public void InitializeCharacterCreationCultures(CharacterCreationManager characterCreationManager)
	{
		characterCreationManager.CharacterCreationContent.AddCharacterCreationCulture(Game.Current.ObjectManager.GetObject<CultureObject>("nord"), 1, 10);
	}

	public void InitializeData(CharacterCreationManager characterCreationManager)
	{
		AddVlandiaParentMenuOptions(characterCreationManager);
		AddSturgiaParentMenuOptions(characterCreationManager);
		AddAseraiParentMenuOptions(characterCreationManager);
		AddBattaniaParentMenuOptions(characterCreationManager);
		AddKhuzaitParentMenuOptions(characterCreationManager);
		AddEmpireParentMenuOptions(characterCreationManager);
		AddNordParentMenuOptions(characterCreationManager);
		AddEarlyChildhoodMenuOptions(characterCreationManager);
		AddEducationMenuOptions(characterCreationManager);
		AddYouthMenuOptions(characterCreationManager);
	}

	private void AddVlandiaParentMenuOptions(CharacterCreationManager characterCreationManager)
	{
		NarrativeMenu narrativeMenuWithId = characterCreationManager.GetNarrativeMenuWithId("narrative_parent_menu");
		NarrativeMenuOption narrativeMenuOption = new NarrativeMenuOption("vlandia_coastal_fisherman_option", new TextObject("{=MPaZbhRc}Coastal fisherman"), new TextObject("{=VBy8WxVw}Your family has been fishing these waters for generations, struggling to make a living off the unpredictable sea. You grew up mending nets, hauling in catches, and dreaming of a life beyond the constant struggle for survival."), GetVlandiaCoastalFishermanNarrativeOptionArgs, VlandiaCoastalFishermanNarrativeOptionOnCondition, VlandiaCoastalFishermanNarrativeOptionOnSelect, null);
		narrativeMenuWithId.AddNarrativeMenuOption(narrativeMenuOption);
		NarrativeMenuOption narrativeMenuOption2 = new NarrativeMenuOption("vlandia_dockers_option", new TextObject("{=rsUCF3H8}Dockers"), new TextObject("{=OyIKF2r6}Your family toiled on the docks, their hands calloused from hauling the endless flow of goods from the sea. A vital but often thankless task that kept Vlandia's ports alive. You learned the rhythm of the tides and the languages of foreign sailors before you learned to read."), GetVlandiaDockersNarrativeOptionArgs, VlandiaDockersNarrativeOptionOnCondition, GetVlandiaDockersNarrativeOptionOnSelect, null);
		narrativeMenuWithId.AddNarrativeMenuOption(narrativeMenuOption2);
	}

	private void GetVlandiaCoastalFishermanNarrativeOptionArgs(NarrativeMenuOptionArgs args)
	{
		SkillObject[] affectedSkills = new SkillObject[2]
		{
			NavalSkills.Boatswain,
			DefaultSkills.Scouting
		};
		args.SetAffectedSkills(affectedSkills);
		args.SetFocusToSkills(_focusToAdd);
		args.SetLevelToSkills(_skillLevelToAdd);
		args.SetLevelToAttribute(DefaultCharacterAttributes.Endurance, _attributeLevelToAdd);
	}

	private bool VlandiaCoastalFishermanNarrativeOptionOnCondition(CharacterCreationManager characterCreationManager)
	{
		return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "vlandia";
	}

	private void VlandiaCoastalFishermanNarrativeOptionOnSelect(CharacterCreationManager characterCreationManager)
	{
		characterCreationManager.CharacterCreationContent.SetParentOccupation("seafarer");
		string motherEquipmentId = GetMotherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
		string fatherEquipmentId = GetFatherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
		MBEquipmentRoster motherEquipment = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(motherEquipmentId);
		MBEquipmentRoster fatherEquipment = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(fatherEquipmentId);
		string motherAnimation = "act_character_creation_vlandia_fisherman_mother";
		string fatherAnimation = "act_character_creation_vlandia_fisherman_father";
		foreach (NarrativeMenuCharacter character in characterCreationManager.CurrentMenu.Characters)
		{
			if (character.StringId == "mother_character")
			{
				character.SetAnimationId("act_character_creation_vlandia_fisherman_mother");
				character.SetLeftHandItem("fishnet_char_creation");
			}
			if (character.StringId == "father_character")
			{
				character.SetAnimationId("act_character_creation_vlandia_fisherman_father");
				character.SetRightHandItem("fishing_rod_s");
			}
		}
		UpdateParentEquipment(characterCreationManager, motherEquipment, fatherEquipment, motherAnimation, fatherAnimation);
	}

	private void GetVlandiaDockersNarrativeOptionArgs(NarrativeMenuOptionArgs args)
	{
		SkillObject[] affectedSkills = new SkillObject[2]
		{
			NavalSkills.Shipmaster,
			DefaultSkills.Athletics
		};
		args.SetAffectedSkills(affectedSkills);
		args.SetFocusToSkills(_focusToAdd);
		args.SetLevelToSkills(_skillLevelToAdd);
		args.SetLevelToAttribute(DefaultCharacterAttributes.Vigor, _attributeLevelToAdd);
	}

	private bool VlandiaDockersNarrativeOptionOnCondition(CharacterCreationManager characterCreationManager)
	{
		return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "vlandia";
	}

	private void GetVlandiaDockersNarrativeOptionOnSelect(CharacterCreationManager characterCreationManager)
	{
		characterCreationManager.CharacterCreationContent.SetParentOccupation("shipmaster_urban");
		string motherEquipmentId = GetMotherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
		string fatherEquipmentId = GetFatherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
		MBEquipmentRoster motherEquipment = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(motherEquipmentId);
		MBEquipmentRoster fatherEquipment = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(fatherEquipmentId);
		string motherAnimation = "act_character_creation_vlandia_dockers_mother";
		string fatherAnimation = "act_character_creation_vlandia_dockers_father";
		foreach (NarrativeMenuCharacter character in characterCreationManager.CurrentMenu.Characters)
		{
			if (character.StringId == "mother_character")
			{
				character.SetAnimationId("act_character_creation_vlandia_dockers_mother");
				character.SetRightHandItem("sack_s");
			}
			if (character.StringId == "father_character")
			{
				character.SetAnimationId("act_character_creation_vlandia_dockers_father");
				character.SetRightHandItem("sack");
			}
		}
		UpdateParentEquipment(characterCreationManager, motherEquipment, fatherEquipment, motherAnimation, fatherAnimation);
	}

	private void AddSturgiaParentMenuOptions(CharacterCreationManager characterCreationManager)
	{
		NarrativeMenu narrativeMenuWithId = characterCreationManager.GetNarrativeMenuWithId("narrative_parent_menu");
		NarrativeMenuOption narrativeMenuOption = new NarrativeMenuOption("sturgia_river_fisherman_option", new TextObject("{=iuAi8rZ4}River Fisherman"), new TextObject("{=gpNBMzW8}Your family lived by the water, skilled in casting nets, setting lines, and mending the wear and tear of daily fishing. You understood the currents, the seasons of the fish, and the importance of a good catch for your community. Life was dictated by the river's flow and its bounty."), GetSturgiaRiverFishermanNarrativeOptionArgs, SturgiaRiverFishermanNarrativeOptionOnCondition, GetSturgiaRiverFishermanNarrativeOptionOnSelect, null);
		narrativeMenuWithId.AddNarrativeMenuOption(narrativeMenuOption);
		NarrativeMenuOption narrativeMenuOption2 = new NarrativeMenuOption("sturgia_shipbuilders_option", new TextObject("{=V0GSUvaU}Shipbuilders"), new TextObject("{=9XmQrI23}Your family builded longships for the Sturgian river lords. You grew up amidst the sounds of hammering and the smell of tar, learning the craft of shipbuilding from your father and uncles."), GetSturgiaShipbuildersNarrativeOptionArgs, SturgiaShipbuildersNarrativeOptionOnCondition, GetSturgiaShipbuildersNarrativeOptionOnSelect, null);
		narrativeMenuWithId.AddNarrativeMenuOption(narrativeMenuOption2);
	}

	private void GetSturgiaRiverFishermanNarrativeOptionArgs(NarrativeMenuOptionArgs args)
	{
		SkillObject[] affectedSkills = new SkillObject[2]
		{
			NavalSkills.Boatswain,
			DefaultSkills.Throwing
		};
		args.SetAffectedSkills(affectedSkills);
		args.SetFocusToSkills(_focusToAdd);
		args.SetLevelToSkills(_skillLevelToAdd);
		args.SetLevelToAttribute(DefaultCharacterAttributes.Control, _attributeLevelToAdd);
	}

	private bool SturgiaRiverFishermanNarrativeOptionOnCondition(CharacterCreationManager characterCreationManager)
	{
		return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "sturgia";
	}

	private void GetSturgiaRiverFishermanNarrativeOptionOnSelect(CharacterCreationManager characterCreationManager)
	{
		characterCreationManager.CharacterCreationContent.SetParentOccupation("seafarer");
		string motherEquipmentId = GetMotherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
		string fatherEquipmentId = GetFatherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
		MBEquipmentRoster motherEquipment = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(motherEquipmentId);
		MBEquipmentRoster fatherEquipment = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(fatherEquipmentId);
		string motherAnimation = "act_character_creation_sturgia_riverfisherman_mother";
		string fatherAnimation = "act_character_creation_sturgia_riverfisherman_father";
		foreach (NarrativeMenuCharacter character in characterCreationManager.CurrentMenu.Characters)
		{
			if (character.StringId == "mother_character")
			{
				character.SetAnimationId("act_character_creation_sturgia_riverfisherman_mother");
			}
			if (character.StringId == "father_character")
			{
				character.SetAnimationId("act_character_creation_sturgia_riverfisherman_father");
				character.SetLeftHandItem("fishnet");
			}
		}
		UpdateParentEquipment(characterCreationManager, motherEquipment, fatherEquipment, motherAnimation, fatherAnimation);
	}

	private void GetSturgiaShipbuildersNarrativeOptionArgs(NarrativeMenuOptionArgs args)
	{
		SkillObject[] affectedSkills = new SkillObject[2]
		{
			NavalSkills.Shipmaster,
			DefaultSkills.Engineering
		};
		args.SetAffectedSkills(affectedSkills);
		args.SetFocusToSkills(_focusToAdd);
		args.SetLevelToSkills(_skillLevelToAdd);
		args.SetLevelToAttribute(DefaultCharacterAttributes.Intelligence, _attributeLevelToAdd);
	}

	private bool SturgiaShipbuildersNarrativeOptionOnCondition(CharacterCreationManager characterCreationManager)
	{
		return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "sturgia";
	}

	private void GetSturgiaShipbuildersNarrativeOptionOnSelect(CharacterCreationManager characterCreationManager)
	{
		characterCreationManager.CharacterCreationContent.SetParentOccupation("shipmaster_urban");
		string motherEquipmentId = GetMotherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
		string fatherEquipmentId = GetFatherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
		MBEquipmentRoster motherEquipment = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(motherEquipmentId);
		MBEquipmentRoster fatherEquipment = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(fatherEquipmentId);
		string motherAnimation = "act_character_creation_sturgia_shipbuilder_mother";
		string fatherAnimation = "act_character_creation_sturgia_shipbuilder_father";
		foreach (NarrativeMenuCharacter character in characterCreationManager.CurrentMenu.Characters)
		{
			if (character.StringId == "mother_character")
			{
				character.SetAnimationId("act_character_creation_sturgia_shipbuilder_mother");
			}
			if (character.StringId == "father_character")
			{
				character.SetAnimationId("act_character_creation_sturgia_shipbuilder_father");
				character.SetLeftHandItem("blacksmith_hammer");
			}
		}
		UpdateParentEquipment(characterCreationManager, motherEquipment, fatherEquipment, motherAnimation, fatherAnimation);
	}

	private void AddAseraiParentMenuOptions(CharacterCreationManager characterCreationManager)
	{
		NarrativeMenu narrativeMenuWithId = characterCreationManager.GetNarrativeMenuWithId("narrative_parent_menu");
		NarrativeMenuOption narrativeMenuOption = new NarrativeMenuOption("aserai_ferryman_option", new TextObject("{=PaXaNLrb}Ferryman"), new TextObject("{=LtOCnEC8}Your family are from a small rural community along a river bank where they operated a small ferry to transport goods and people across the river, connecting rural communities. You learned about boats and ebbs and flows of the river navigation."), GetAseraiFerrymanNarrativeOptionArgs, AseraiFerrymanNarrativeOptionOnCondition, GetAseraiFerrymanNarrativeOptionOnSelect, null);
		narrativeMenuWithId.AddNarrativeMenuOption(narrativeMenuOption);
		NarrativeMenuOption narrativeMenuOption2 = new NarrativeMenuOption("aserai_corsair_traders_option", new TextObject("{=V0IGaFFn}Corsair Traders"), new TextObject("{=Gl5CFpEM}Raised on Aserai dhows, your father thought you about the trade winds and routes. The ship you were raised in made long and tedious voyages, smuggling silks and spices or ambushing Vlandian ships when profits dwindled."), GetAseraiCorsairTradersNarrativeOptionArgs, AseraiCorsairTradersNarrativeOptionOnCondition, GetAseraiCorsairTradersNarrativeOptionOnSelect, null);
		narrativeMenuWithId.AddNarrativeMenuOption(narrativeMenuOption2);
	}

	private void GetAseraiFerrymanNarrativeOptionArgs(NarrativeMenuOptionArgs args)
	{
		SkillObject[] affectedSkills = new SkillObject[2]
		{
			NavalSkills.Boatswain,
			DefaultSkills.Trade
		};
		args.SetAffectedSkills(affectedSkills);
		args.SetFocusToSkills(_focusToAdd);
		args.SetLevelToSkills(_skillLevelToAdd);
		args.SetLevelToAttribute(DefaultCharacterAttributes.Cunning, _attributeLevelToAdd);
	}

	private bool AseraiFerrymanNarrativeOptionOnCondition(CharacterCreationManager characterCreationManager)
	{
		return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "aserai";
	}

	private void GetAseraiFerrymanNarrativeOptionOnSelect(CharacterCreationManager characterCreationManager)
	{
		characterCreationManager.CharacterCreationContent.SetParentOccupation("seafarer");
		string motherEquipmentId = GetMotherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
		string fatherEquipmentId = GetFatherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
		MBEquipmentRoster motherEquipment = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(motherEquipmentId);
		MBEquipmentRoster fatherEquipment = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(fatherEquipmentId);
		string motherAnimation = "act_character_creation_aserai_ferryman_mother";
		string fatherAnimation = "act_character_creation_aserai_ferryman_father";
		foreach (NarrativeMenuCharacter character in characterCreationManager.CurrentMenu.Characters)
		{
			if (character.StringId == "mother_character")
			{
				character.SetAnimationId("act_character_creation_aserai_ferryman_mother");
			}
			if (character.StringId == "father_character")
			{
				character.SetAnimationId("act_character_creation_aserai_ferryman_father");
				character.SetRightHandItem("shovel_right_hand");
			}
		}
		foreach (NarrativeMenuCharacter character2 in characterCreationManager.CurrentMenu.Characters)
		{
			if (character2.StringId == "mother_character")
			{
				character2.SetAnimationId("act_character_creation_aserai_ferryman_mother");
			}
			if (character2.StringId == "father_character")
			{
				character2.SetAnimationId("act_character_creation_aserai_ferryman_father");
				character2.SetRightHandItem("shovel_right_hand");
			}
		}
		UpdateParentEquipment(characterCreationManager, motherEquipment, fatherEquipment, motherAnimation, fatherAnimation);
	}

	private void GetAseraiCorsairTradersNarrativeOptionArgs(NarrativeMenuOptionArgs args)
	{
		SkillObject[] affectedSkills = new SkillObject[2]
		{
			NavalSkills.Boatswain,
			NavalSkills.Mariner
		};
		args.SetAffectedSkills(affectedSkills);
		args.SetFocusToSkills(_focusToAdd);
		args.SetLevelToSkills(_skillLevelToAdd);
		args.SetLevelToAttribute(DefaultCharacterAttributes.Vigor, _attributeLevelToAdd);
	}

	private bool AseraiCorsairTradersNarrativeOptionOnCondition(CharacterCreationManager characterCreationManager)
	{
		return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "aserai";
	}

	private void GetAseraiCorsairTradersNarrativeOptionOnSelect(CharacterCreationManager characterCreationManager)
	{
		characterCreationManager.CharacterCreationContent.SetParentOccupation("shipmaster_urban");
		string motherEquipmentId = GetMotherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
		string fatherEquipmentId = GetFatherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
		MBEquipmentRoster motherEquipment = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(motherEquipmentId);
		MBEquipmentRoster fatherEquipment = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(fatherEquipmentId);
		string motherAnimation = "act_character_creation_aserai_corsair_trader_mother";
		string fatherAnimation = "act_character_creation_aserai_corsair_trader_father";
		UpdateParentEquipment(characterCreationManager, motherEquipment, fatherEquipment, motherAnimation, fatherAnimation);
	}

	private void AddBattaniaParentMenuOptions(CharacterCreationManager characterCreationManager)
	{
		NarrativeMenu narrativeMenuWithId = characterCreationManager.GetNarrativeMenuWithId("narrative_parent_menu");
		NarrativeMenuOption narrativeMenuOption = new NarrativeMenuOption("battania_currach_sailors_option", new TextObject("{=4zNU0J1S}Currach Sailors"), new TextObject("{=bnrmJHc6}Your kin braved the lakes and rivers in hide-covered currachs, fishing icy waters and facing the dangers of strong currents and occasional banditry. You grew up learning to navigate the treacherous waters and to defend yourself from those who would prey on the river traffic."), GetBattaniaCurrachSailorsNarrativeOptionArgs, BattaniaCurrachSailorsNarrativeOptionOnCondition, GetBattaniaCurrachSailorsNarrativeOptionOnSelect, null);
		narrativeMenuWithId.AddNarrativeMenuOption(narrativeMenuOption);
		NarrativeMenuOption narrativeMenuOption2 = new NarrativeMenuOption("battania_guardian_of_the_lake_option", new TextObject("{=o7BFw2WW}Guardian of the Lake"), new TextObject("{=ydyaMa6E}Your kin were part of a group of warriors tasked with maintaining small boats for defense or patrol of vital waterways, protecting it from raiders or invaders. While they weren't around much while you were growing up, you still earned some riverine navigation and combat skills."), GetBattaniaGuardianOfTheLakeNarrativeOptionArgs, BattaniaGuardianOfTheLakeNarrativeOptionOnCondition, GetBattaniaGuardianOfTheLakeNarrativeOptionOnSelect, null);
		narrativeMenuWithId.AddNarrativeMenuOption(narrativeMenuOption2);
	}

	private void GetBattaniaCurrachSailorsNarrativeOptionArgs(NarrativeMenuOptionArgs args)
	{
		SkillObject[] affectedSkills = new SkillObject[2]
		{
			NavalSkills.Boatswain,
			DefaultSkills.Bow
		};
		args.SetAffectedSkills(affectedSkills);
		args.SetFocusToSkills(_focusToAdd);
		args.SetLevelToSkills(_skillLevelToAdd);
		args.SetLevelToAttribute(DefaultCharacterAttributes.Control, _attributeLevelToAdd);
	}

	private bool BattaniaCurrachSailorsNarrativeOptionOnCondition(CharacterCreationManager characterCreationManager)
	{
		return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "battania";
	}

	private void GetBattaniaCurrachSailorsNarrativeOptionOnSelect(CharacterCreationManager characterCreationManager)
	{
		characterCreationManager.CharacterCreationContent.SetParentOccupation("seafarer");
		string motherEquipmentId = GetMotherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
		string fatherEquipmentId = GetFatherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
		MBEquipmentRoster motherEquipment = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(motherEquipmentId);
		MBEquipmentRoster fatherEquipment = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(fatherEquipmentId);
		string motherAnimation = "act_character_creation_battania_currach_sailors_mother";
		string fatherAnimation = "act_character_creation_battania_currach_sailors_father";
		foreach (NarrativeMenuCharacter character in characterCreationManager.CurrentMenu.Characters)
		{
			if (character.StringId == "mother_character")
			{
				character.SetAnimationId("act_character_creation_battania_currach_sailors_mother");
				character.SetLeftHandItem("bow");
			}
			if (character.StringId == "father_character")
			{
				character.SetAnimationId("act_character_creation_battania_currach_sailors_father");
				character.SetRightHandItem("battle_axe");
			}
		}
		UpdateParentEquipment(characterCreationManager, motherEquipment, fatherEquipment, motherAnimation, fatherAnimation);
	}

	private void GetBattaniaGuardianOfTheLakeNarrativeOptionArgs(NarrativeMenuOptionArgs args)
	{
		SkillObject[] affectedSkills = new SkillObject[2]
		{
			NavalSkills.Mariner,
			DefaultSkills.Polearm
		};
		args.SetAffectedSkills(affectedSkills);
		args.SetFocusToSkills(_focusToAdd);
		args.SetLevelToSkills(_skillLevelToAdd);
		args.SetLevelToAttribute(DefaultCharacterAttributes.Vigor, _attributeLevelToAdd);
	}

	private bool BattaniaGuardianOfTheLakeNarrativeOptionOnCondition(CharacterCreationManager characterCreationManager)
	{
		return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "battania";
	}

	private void GetBattaniaGuardianOfTheLakeNarrativeOptionOnSelect(CharacterCreationManager characterCreationManager)
	{
		characterCreationManager.CharacterCreationContent.SetParentOccupation("shipmaster_urban");
		string motherEquipmentId = GetMotherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
		string fatherEquipmentId = GetFatherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
		MBEquipmentRoster motherEquipment = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(motherEquipmentId);
		MBEquipmentRoster fatherEquipment = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(fatherEquipmentId);
		string motherAnimation = "act_character_creation_battania_guardian_of_the_lake_mother";
		string fatherAnimation = "act_character_creation_battania_guardian_of_the_lake_father";
		foreach (NarrativeMenuCharacter character in characterCreationManager.CurrentMenu.Characters)
		{
			if (character.StringId == "mother_character")
			{
				character.SetAnimationId("act_character_creation_battania_guardian_of_the_lake_mother");
				character.SetRightHandItem("javelin_a");
			}
			if (character.StringId == "father_character")
			{
				character.SetAnimationId("act_character_creation_battania_guardian_of_the_lake_father");
				character.SetLeftHandItem("heater_shield");
				character.SetRightHandItem("blacksmith_sword");
			}
		}
		UpdateParentEquipment(characterCreationManager, motherEquipment, fatherEquipment, motherAnimation, fatherAnimation);
	}

	private void AddKhuzaitParentMenuOptions(CharacterCreationManager characterCreationManager)
	{
		NarrativeMenu narrativeMenuWithId = characterCreationManager.GetNarrativeMenuWithId("narrative_parent_menu");
		NarrativeMenuOption narrativeMenuOption = new NarrativeMenuOption("khuzait_river_foragers_option", new TextObject("{=fEIzJtSF}River Foragers"), new TextObject("{=2rNqqZnm}Along the winding veins of a major river that cuts through the steppe, your family carved a life from the water's edge. Using small, makeshift rafts and boats, they developed a keen eye for the river's bounty, gathering specific plants from its banks and fishing in its shallows. From your humble parents, you inherited a deep well of knowledge."), GetKhuzaitRiverForagersNarrativeOptionArgs, KhuzaitRiverForagersNarrativeOptionOnCondition, GetKhuzaitRiverForagersNarrativeOptionOnSelect, null);
		narrativeMenuWithId.AddNarrativeMenuOption(narrativeMenuOption);
		NarrativeMenuOption narrativeMenuOption2 = new NarrativeMenuOption("khuzait_river_traders_option", new TextObject("{=DQQogYtq}River Traders"), new TextObject("{=enS8isiB}Your family transports goods and people along the river, facing the dangers of strong currents and occasional banditry. You grew up learning to navigate the treacherous waters and to defend yourself from those who would prey on the river traffic."), GetKhuzaitRiverTradersNarrativeOptionArgs, KhuzaitRiverTradersNarrativeOptionOnCondition, GetKhuzaitRiverTradersNarrativeOptionOnSelect, null);
		narrativeMenuWithId.AddNarrativeMenuOption(narrativeMenuOption2);
	}

	private void GetKhuzaitRiverForagersNarrativeOptionArgs(NarrativeMenuOptionArgs args)
	{
		SkillObject[] affectedSkills = new SkillObject[2]
		{
			NavalSkills.Boatswain,
			NavalSkills.Shipmaster
		};
		args.SetAffectedSkills(affectedSkills);
		args.SetFocusToSkills(_focusToAdd);
		args.SetLevelToSkills(_skillLevelToAdd);
		args.SetLevelToAttribute(DefaultCharacterAttributes.Endurance, _attributeLevelToAdd);
	}

	private bool KhuzaitRiverForagersNarrativeOptionOnCondition(CharacterCreationManager characterCreationManager)
	{
		return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "khuzait";
	}

	private void GetKhuzaitRiverForagersNarrativeOptionOnSelect(CharacterCreationManager characterCreationManager)
	{
		characterCreationManager.CharacterCreationContent.SetParentOccupation("seafarer");
		string motherEquipmentId = GetMotherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
		string fatherEquipmentId = GetFatherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
		MBEquipmentRoster motherEquipment = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(motherEquipmentId);
		MBEquipmentRoster fatherEquipment = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(fatherEquipmentId);
		string motherAnimation = "act_character_creation_khuzait_river_foragers_mother";
		string fatherAnimation = "act_character_creation_khuzait_river_foragers_father";
		foreach (NarrativeMenuCharacter character in characterCreationManager.CurrentMenu.Characters)
		{
			if (character.StringId == "mother_character")
			{
				character.SetAnimationId("act_character_creation_khuzait_river_foragers_mother");
			}
			if (character.StringId == "father_character")
			{
				character.SetAnimationId("act_character_creation_khuzait_river_foragers_father");
				character.SetLeftHandItem("fish_stick");
			}
		}
		UpdateParentEquipment(characterCreationManager, motherEquipment, fatherEquipment, motherAnimation, fatherAnimation);
	}

	private void GetKhuzaitRiverTradersNarrativeOptionArgs(NarrativeMenuOptionArgs args)
	{
		SkillObject[] affectedSkills = new SkillObject[2]
		{
			DefaultSkills.Bow,
			NavalSkills.Mariner
		};
		args.SetAffectedSkills(affectedSkills);
		args.SetFocusToSkills(_focusToAdd);
		args.SetLevelToSkills(_skillLevelToAdd);
		args.SetLevelToAttribute(DefaultCharacterAttributes.Cunning, _attributeLevelToAdd);
	}

	private bool KhuzaitRiverTradersNarrativeOptionOnCondition(CharacterCreationManager characterCreationManager)
	{
		return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "khuzait";
	}

	private void GetKhuzaitRiverTradersNarrativeOptionOnSelect(CharacterCreationManager characterCreationManager)
	{
		characterCreationManager.CharacterCreationContent.SetParentOccupation("shipmaster_urban");
		string motherEquipmentId = GetMotherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
		string fatherEquipmentId = GetFatherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
		MBEquipmentRoster motherEquipment = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(motherEquipmentId);
		MBEquipmentRoster fatherEquipment = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(fatherEquipmentId);
		string motherAnimation = "act_character_creation_khuzait_river_traders_mother";
		string fatherAnimation = "act_character_creation_khuzait_river_traders_father";
		foreach (NarrativeMenuCharacter character in characterCreationManager.CurrentMenu.Characters)
		{
			if (character.StringId == "mother_character")
			{
				character.SetAnimationId("act_character_creation_khuzait_river_foragers_mother");
			}
			if (character.StringId == "father_character")
			{
				character.SetAnimationId("act_character_creation_khuzait_river_foragers_father");
				character.SetLeftHandItem("stick");
			}
		}
		UpdateParentEquipment(characterCreationManager, motherEquipment, fatherEquipment, motherAnimation, fatherAnimation);
	}

	private void AddEmpireParentMenuOptions(CharacterCreationManager characterCreationManager)
	{
		NarrativeMenu narrativeMenuWithId = characterCreationManager.GetNarrativeMenuWithId("narrative_parent_menu");
		NarrativeMenuOption narrativeMenuOption = new NarrativeMenuOption("empire_small_boat_fisherman_option", new TextObject("{=e1aebAAL}Small Boat Fisherman"), new TextObject("{=nBr0jL3X}Your family inhabited a small, relatively isolated coastal village within the Empire. They foraged along the shoreline for fish using small boats. You grew up with the smell of salt and the rhythm of the tides, learning to navigate close to shore and brave the smaller waves in your sturdy little vessel."), GetEmpireSmallBoatFishermanNarrativeOptionArgs, EmpireSmallBoatFishermanNarrativeOptionOnCondition, GetEmpireSmallBoatFishermanNarrativeOptionOnSelect, null);
		narrativeMenuWithId.AddNarrativeMenuOption(narrativeMenuOption);
		NarrativeMenuOption narrativeMenuOption2 = new NarrativeMenuOption("empire_imperial_fleet_option", new TextObject("{=LdCQfaUi}Imperial Fleet"), new TextObject("{=N6o7Gnpz}Your father served in one the Imperial Navy's liburna as a quartermaster. He bought supplies for the crew and basically kept the ship running. He wanted the same path for you so you were schooled in trading and ship maintenance."), GetEmpireImperialFleetNarrativeOptionArgs, EmpireImperialFleetNarrativeOptionOnCondition, GetEmpireImperialFleetNarrativeOptionOnSelect, null);
		narrativeMenuWithId.AddNarrativeMenuOption(narrativeMenuOption2);
	}

	private void GetEmpireSmallBoatFishermanNarrativeOptionArgs(NarrativeMenuOptionArgs args)
	{
		SkillObject[] affectedSkills = new SkillObject[2]
		{
			NavalSkills.Boatswain,
			DefaultSkills.Throwing
		};
		args.SetAffectedSkills(affectedSkills);
		args.SetFocusToSkills(_focusToAdd);
		args.SetLevelToSkills(_skillLevelToAdd);
		args.SetLevelToAttribute(DefaultCharacterAttributes.Control, _attributeLevelToAdd);
	}

	private bool EmpireSmallBoatFishermanNarrativeOptionOnCondition(CharacterCreationManager characterCreationManager)
	{
		return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "empire";
	}

	private void GetEmpireSmallBoatFishermanNarrativeOptionOnSelect(CharacterCreationManager characterCreationManager)
	{
		characterCreationManager.CharacterCreationContent.SetParentOccupation("seafarer");
		string motherEquipmentId = GetMotherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
		string fatherEquipmentId = GetFatherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
		MBEquipmentRoster motherEquipment = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(motherEquipmentId);
		MBEquipmentRoster fatherEquipment = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(fatherEquipmentId);
		string motherAnimation = "act_character_creation_empire_smallboatfisherman_mother";
		string fatherAnimation = "act_character_creation_empire_smallboatfisherman_father";
		foreach (NarrativeMenuCharacter character in characterCreationManager.CurrentMenu.Characters)
		{
			if (character.StringId == "mother_character")
			{
				character.SetAnimationId("act_character_creation_empire_smallboatfisherman_mother");
			}
			if (character.StringId == "father_character")
			{
				character.SetAnimationId("act_character_creation_empire_smallboatfisherman_father");
				character.SetLeftHandItem("hanging_fishes");
				character.SetRightHandItem("hanging_fishes");
			}
		}
		UpdateParentEquipment(characterCreationManager, motherEquipment, fatherEquipment, motherAnimation, fatherAnimation);
	}

	private void GetEmpireImperialFleetNarrativeOptionArgs(NarrativeMenuOptionArgs args)
	{
		SkillObject[] affectedSkills = new SkillObject[2]
		{
			DefaultSkills.Trade,
			NavalSkills.Shipmaster
		};
		args.SetAffectedSkills(affectedSkills);
		args.SetFocusToSkills(_focusToAdd);
		args.SetLevelToSkills(_skillLevelToAdd);
		args.SetLevelToAttribute(DefaultCharacterAttributes.Social, _attributeLevelToAdd);
	}

	private bool EmpireImperialFleetNarrativeOptionOnCondition(CharacterCreationManager characterCreationManager)
	{
		return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "empire";
	}

	private void GetEmpireImperialFleetNarrativeOptionOnSelect(CharacterCreationManager characterCreationManager)
	{
		characterCreationManager.CharacterCreationContent.SetParentOccupation("shipmaster_urban");
		string motherEquipmentId = GetMotherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
		string fatherEquipmentId = GetFatherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
		MBEquipmentRoster motherEquipment = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(motherEquipmentId);
		MBEquipmentRoster fatherEquipment = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(fatherEquipmentId);
		string motherAnimation = "act_character_creation_empire_imperial_fleet_mother";
		string fatherAnimation = "act_character_creation_empire_imperial_fleet_father";
		foreach (NarrativeMenuCharacter character in characterCreationManager.CurrentMenu.Characters)
		{
			if (character.StringId == "mother_character")
			{
				character.SetAnimationId("act_character_creation_empire_imperial_fleet_mother");
			}
			if (character.StringId == "father_character")
			{
				character.SetAnimationId("act_character_creation_empire_imperial_fleet_father");
				character.SetRightHandItem("book_right_hand");
			}
		}
		UpdateParentEquipment(characterCreationManager, motherEquipment, fatherEquipment, motherAnimation, fatherAnimation);
	}

	private void AddNordParentMenuOptions(CharacterCreationManager characterCreationManager)
	{
		NarrativeMenu narrativeMenuWithId = characterCreationManager.GetNarrativeMenuWithId("narrative_parent_menu");
		NarrativeMenuOption narrativeMenuOption = new NarrativeMenuOption("nord_hersir_option", new TextObject("{=DRC5bTE5}Hersir"), new TextObject("{=w3AI4lwM}Your family's loyalty ran deep, not in sprawling lands or grand titles, but in service. For generations, they'd served as hersirs, the trusted retainers, for a minor Jarl who kept watch over a windswept corner of the Nord territory."), GetNordHersirNarrativeOptionArgs, NordHersirNarrativeOptionOnCondition, NordHersirNarrativeOptionOnSelect, null);
		narrativeMenuWithId.AddNarrativeMenuOption(narrativeMenuOption);
		NarrativeMenuOption narrativeMenuOption2 = new NarrativeMenuOption("nord_market_trader_option", new TextObject("{=uqpHfuZV}Peddler"), new TextObject("{=DvgmjoCE}You grew up amidst the bustling chaos of a Norse market town, a hub of trade where goods from across the known world exchanged hands. Your family were established traders, perhaps dealing in furs, amber, crafted goods, or even imported luxuries. You learned the art of negotiation, the value of different commodities, and the diverse languages and customs of the merchants who passed through. The market was your school, and shrewd dealing your lesson."), GetNordMarketTraderNarrativeOptionArgs, NordMarketTraderNarrativeOptionOnCondition, NordMarketTraderNarrativeOptionOnSelect, null);
		narrativeMenuWithId.AddNarrativeMenuOption(narrativeMenuOption2);
		NarrativeMenuOption narrativeMenuOption3 = new NarrativeMenuOption("nord_skald_option", new TextObject("{=1lX8eks5}Travelling skalds"), new TextObject("{=KtucaHqd}Your family's voices carried the tales of the North. Not grand courtly Skalds, but traveling storytellers with weathered cloaks and worn lutes. They wandered from village to village, weaving tales of heroes and hearth into songs and sagas. You grew up surrounded by the rhythmic strum of their instruments and the flickering firelight reflecting off their eyes as they spun fantastical yarns. These weren't just stories - they were the beating heart of Nord culture, passed down from generation to generation by your family's calloused hands and booming voices."), GetNordSkaldNarrativeOptionArgs, NordSkaldNarrativeOptionOnCondition, NordSkaldNarrativeOptionOnSelect, null);
		narrativeMenuWithId.AddNarrativeMenuOption(narrativeMenuOption3);
		NarrativeMenuOption narrativeMenuOption4 = new NarrativeMenuOption("nord_blacksmith_option", new TextObject("{=v48N6h1t}Urban artisans"), new TextObject("{=AAHhp1ly}The clang of hammer on hot iron was the defining sound of your upbringing. Your family were more than mere smiths; they were artisans who coaxed wonders from limited resources, shaping valuable iron into formidable weapons and treasured tools. From the forge, you learned to work with what little you had, understanding the unique properties of each piece and the almost magical skill required to transform it."), GetNordBlacksmithNarrativeOptionArgs, NordBlacksmithNarrativeOptionOnCondition, NordBlacksmithNarrativeOptionOnSelect, null);
		narrativeMenuWithId.AddNarrativeMenuOption(narrativeMenuOption4);
		NarrativeMenuOption narrativeMenuOption5 = new NarrativeMenuOption("nord_hunter_option", new TextObject("{=izTHRXo5}Hunters"), new TextObject("{=rdRamFhv}You were born into a family of foresters living off the land. You learned to track prey, hunt for sustenance and gathering herbs and mushrooms from a young age. The forest provided, but it also demanded respect. You learned the medicinal properties of plants and mushrooms for the inevitable scrapes and ailments that came with life in the wild. The harsh environment became your teacher, and survival your greatest lesson."), GetNordHunterNarrativeOptionArgs, NordHunterNarrativeOptionOnCondition, NordHunterNarrativeOptionOnSelect, null);
		narrativeMenuWithId.AddNarrativeMenuOption(narrativeMenuOption5);
		NarrativeMenuOption narrativeMenuOption6 = new NarrativeMenuOption("nord_vagabonds_option", new TextObject("{=TPoK3GSj}Vagabonds"), new TextObject("{=nrtrMbLx}You were part of a tight-knit family scraping by on the fringes of a bustling Nord port. Hard work wasn't always an option, and your kin did what they had to - unloading ships one day, \"borrowing\" a stray coin the next. Life was rough, lessons learned on cobblestone streets, but the fierce loyalty that bound your family together was stronger than any harbor wall."), GetNordVagabondNarrativeOptionArgs, NordVagabondNarrativeOptionOnCondition, NordVagabondNarrativeOptionOnSelect, null);
		narrativeMenuWithId.AddNarrativeMenuOption(narrativeMenuOption6);
		NarrativeMenuOption narrativeMenuOption7 = new NarrativeMenuOption("nord_sailors_option", new TextObject("{=6aKaV4ua}Sailors"), new TextObject("{=BbOM3F8H}Your family was a tight-knit crew on a sturdy fishing vessel. They weren't charting uncharted seas, but venturing just beyond the familiar fjords, bartering with coastal settlements for smoked fish and bragging rights about the biggest catch. Tales of faraway lands might have been spun under flickering lanterns, but the reality was weathered sails, calloused hands, and a knack for reading the temperamental sea."), GetNordSailorsNarrativeOptionArgs, NordSailorsNarrativeOptionOnCondition, GetNordSailorsNarrativeOptionOnSelect, null);
		narrativeMenuWithId.AddNarrativeMenuOption(narrativeMenuOption7);
		NarrativeMenuOption narrativeMenuOption8 = new NarrativeMenuOption("nord_shipwrights_option", new TextObject("{=WYS68dRq}Shipwrights"), new TextObject("{=qUwVnncn}Your kin weren't grand shipwrights building mighty drakkars, but a family of skilled boatbuilders crafting sturdy vessels. Their longships weren't feared in battle, but prized for braving the treacherous coasts. Each plank and sail held the legacy of generations, passed down through calloused hands and the rhythmic tap of the hammer."), GetNordShipwrightsNarrativeOptionArgs, NordShipwrightsNarrativeOptionOnCondition, GetNordShipwrightsNarrativeOptionOnSelect, null);
		narrativeMenuWithId.AddNarrativeMenuOption(narrativeMenuOption8);
	}

	private void GetNordHersirNarrativeOptionArgs(NarrativeMenuOptionArgs args)
	{
		SkillObject[] affectedSkills = new SkillObject[2]
		{
			DefaultSkills.Steward,
			DefaultSkills.OneHanded
		};
		args.SetAffectedSkills(affectedSkills);
		args.SetFocusToSkills(_focusToAdd);
		args.SetLevelToSkills(_skillLevelToAdd);
		args.SetLevelToAttribute(DefaultCharacterAttributes.Vigor, _attributeLevelToAdd);
	}

	private bool NordHersirNarrativeOptionOnCondition(CharacterCreationManager characterCreationManager)
	{
		return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "nord";
	}

	private void NordHersirNarrativeOptionOnSelect(CharacterCreationManager characterCreationManager)
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

	private void GetNordMarketTraderNarrativeOptionArgs(NarrativeMenuOptionArgs args)
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

	private bool NordMarketTraderNarrativeOptionOnCondition(CharacterCreationManager characterCreationManager)
	{
		return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "nord";
	}

	private void NordMarketTraderNarrativeOptionOnSelect(CharacterCreationManager characterCreationManager)
	{
		characterCreationManager.CharacterCreationContent.SetParentOccupation("merchant_urban");
		string motherEquipmentId = GetMotherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
		string fatherEquipmentId = GetFatherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
		MBEquipmentRoster motherEquipment = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(motherEquipmentId);
		MBEquipmentRoster fatherEquipment = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(fatherEquipmentId);
		string motherAnimation = "act_character_creation_female_default_side_to_side_2";
		string fatherAnimation = "act_character_creation_male_default_side_to_side_2";
		UpdateParentEquipment(characterCreationManager, motherEquipment, fatherEquipment, motherAnimation, fatherAnimation);
	}

	private void GetNordSkaldNarrativeOptionArgs(NarrativeMenuOptionArgs args)
	{
		SkillObject[] affectedSkills = new SkillObject[2]
		{
			DefaultSkills.Scouting,
			DefaultSkills.Charm
		};
		args.SetAffectedSkills(affectedSkills);
		args.SetFocusToSkills(_focusToAdd);
		args.SetLevelToSkills(_skillLevelToAdd);
		args.SetLevelToAttribute(DefaultCharacterAttributes.Intelligence, _attributeLevelToAdd);
	}

	private bool NordSkaldNarrativeOptionOnCondition(CharacterCreationManager characterCreationManager)
	{
		return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "nord";
	}

	private void NordSkaldNarrativeOptionOnSelect(CharacterCreationManager characterCreationManager)
	{
		characterCreationManager.CharacterCreationContent.SetParentOccupation("bard");
		string motherEquipmentId = GetMotherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
		string fatherEquipmentId = GetFatherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
		MBEquipmentRoster motherEquipment = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(motherEquipmentId);
		MBEquipmentRoster fatherEquipment = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(fatherEquipmentId);
		string motherAnimation = "act_character_creation_female_default_father_sitting";
		string fatherAnimation = "act_character_creation_male_default_father_sitting";
		UpdateParentEquipment(characterCreationManager, motherEquipment, fatherEquipment, motherAnimation, fatherAnimation);
	}

	private void GetNordBlacksmithNarrativeOptionArgs(NarrativeMenuOptionArgs args)
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

	private bool NordBlacksmithNarrativeOptionOnCondition(CharacterCreationManager characterCreationManager)
	{
		return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "nord";
	}

	private void NordBlacksmithNarrativeOptionOnSelect(CharacterCreationManager characterCreationManager)
	{
		characterCreationManager.CharacterCreationContent.SetParentOccupation("artisan_urban");
		string motherEquipmentId = GetMotherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
		string fatherEquipmentId = GetFatherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
		MBEquipmentRoster motherEquipment = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(motherEquipmentId);
		MBEquipmentRoster fatherEquipment = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(fatherEquipmentId);
		string motherAnimation = "act_character_creation_female_default_mother_front";
		string fatherAnimation = "act_character_creation_male_default_mother_front";
		UpdateParentEquipment(characterCreationManager, motherEquipment, fatherEquipment, motherAnimation, fatherAnimation);
	}

	private void GetNordHunterNarrativeOptionArgs(NarrativeMenuOptionArgs args)
	{
		SkillObject[] affectedSkills = new SkillObject[2]
		{
			DefaultSkills.Bow,
			DefaultSkills.Medicine
		};
		args.SetAffectedSkills(affectedSkills);
		args.SetFocusToSkills(_focusToAdd);
		args.SetLevelToSkills(_skillLevelToAdd);
		args.SetLevelToAttribute(DefaultCharacterAttributes.Control, _attributeLevelToAdd);
	}

	private bool NordHunterNarrativeOptionOnCondition(CharacterCreationManager characterCreationManager)
	{
		return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "nord";
	}

	private void NordHunterNarrativeOptionOnSelect(CharacterCreationManager characterCreationManager)
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

	private void GetNordVagabondNarrativeOptionArgs(NarrativeMenuOptionArgs args)
	{
		SkillObject[] affectedSkills = new SkillObject[2]
		{
			DefaultSkills.Throwing,
			DefaultSkills.Roguery
		};
		args.SetAffectedSkills(affectedSkills);
		args.SetFocusToSkills(_focusToAdd);
		args.SetLevelToSkills(_skillLevelToAdd);
		args.SetLevelToAttribute(DefaultCharacterAttributes.Cunning, _attributeLevelToAdd);
	}

	private bool NordVagabondNarrativeOptionOnCondition(CharacterCreationManager characterCreationManager)
	{
		return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "nord";
	}

	private void NordVagabondNarrativeOptionOnSelect(CharacterCreationManager characterCreationManager)
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

	private void GetNordSailorsNarrativeOptionArgs(NarrativeMenuOptionArgs args)
	{
		SkillObject[] affectedSkills = new SkillObject[2]
		{
			DefaultSkills.Trade,
			NavalSkills.Boatswain
		};
		args.SetAffectedSkills(affectedSkills);
		args.SetFocusToSkills(_focusToAdd);
		args.SetLevelToSkills(_skillLevelToAdd);
		args.SetLevelToAttribute(DefaultCharacterAttributes.Social, _attributeLevelToAdd);
	}

	private bool NordSailorsNarrativeOptionOnCondition(CharacterCreationManager characterCreationManager)
	{
		return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "nord";
	}

	private void GetNordSailorsNarrativeOptionOnSelect(CharacterCreationManager characterCreationManager)
	{
		characterCreationManager.CharacterCreationContent.SetParentOccupation("seafarer");
		string motherEquipmentId = GetMotherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
		string fatherEquipmentId = GetFatherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
		MBEquipmentRoster motherEquipment = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(motherEquipmentId);
		MBEquipmentRoster fatherEquipment = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(fatherEquipmentId);
		string motherAnimation = "act_character_creation_nord_sailors_mother";
		string fatherAnimation = "act_character_creation_nord_sailors_father";
		foreach (NarrativeMenuCharacter character in characterCreationManager.CurrentMenu.Characters)
		{
			if (character.StringId == "mother_character")
			{
				character.SetAnimationId("act_character_creation_nord_sailors_mother");
				character.SetRightHandItem("fish_basket");
			}
			if (character.StringId == "father_character")
			{
				character.SetAnimationId("act_character_creation_nord_sailors_father");
				character.SetLeftHandItem("fish_left_hand");
			}
		}
		UpdateParentEquipment(characterCreationManager, motherEquipment, fatherEquipment, motherAnimation, fatherAnimation);
	}

	private void GetNordShipwrightsNarrativeOptionArgs(NarrativeMenuOptionArgs args)
	{
		SkillObject[] affectedSkills = new SkillObject[2]
		{
			DefaultSkills.Engineering,
			NavalSkills.Shipmaster
		};
		args.SetAffectedSkills(affectedSkills);
		args.SetFocusToSkills(_focusToAdd);
		args.SetLevelToSkills(_skillLevelToAdd);
		args.SetLevelToAttribute(DefaultCharacterAttributes.Endurance, _attributeLevelToAdd);
	}

	private bool NordShipwrightsNarrativeOptionOnCondition(CharacterCreationManager characterCreationManager)
	{
		return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "nord";
	}

	private void GetNordShipwrightsNarrativeOptionOnSelect(CharacterCreationManager characterCreationManager)
	{
		characterCreationManager.CharacterCreationContent.SetParentOccupation("shipmaster_urban");
		string motherEquipmentId = GetMotherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
		string fatherEquipmentId = GetFatherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
		MBEquipmentRoster motherEquipment = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(motherEquipmentId);
		MBEquipmentRoster fatherEquipment = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(fatherEquipmentId);
		string motherAnimation = "act_character_creation_nord_shipwrights_mother";
		string fatherAnimation = "act_character_creation_nord_shipwrights_father";
		foreach (NarrativeMenuCharacter character in characterCreationManager.CurrentMenu.Characters)
		{
			if (character.StringId == "mother_character")
			{
				character.SetAnimationId("act_character_creation_nord_shipwrights_mother");
			}
			if (character.StringId == "father_character")
			{
				character.SetAnimationId("act_character_creation_nord_shipwrights_father");
				character.SetRightHandItem("blacksmith_hammer");
			}
		}
		UpdateParentEquipment(characterCreationManager, motherEquipment, fatherEquipment, motherAnimation, fatherAnimation);
	}

	private void UpdateParentEquipment(CharacterCreationManager characterCreationManager, MBEquipmentRoster motherEquipment, MBEquipmentRoster fatherEquipment, string motherAnimation, string fatherAnimation)
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

	private void AddEarlyChildhoodMenuOptions(CharacterCreationManager characterCreationManager)
	{
		NarrativeMenu narrativeMenuWithId = characterCreationManager.GetNarrativeMenuWithId("narrative_childhood_menu");
		NarrativeMenuOption narrativeMenuOption = new NarrativeMenuOption("childhood_predict_weather_option", new TextObject("{=cYIB0838}your uncanny ability to predict the weather."), new TextObject("{=w77I1ijB}You were fascinated with clouds and patterns and always observed weather, often warning your family of impending storms with uncanny accuracy."), GetChildhoodPredictWeatherOptionArgs, ChildhoodPredictWeatherOptionOnCondition, ChildhoodPredictWeatherOptionOnSelect, null);
		narrativeMenuWithId.AddNarrativeMenuOption(narrativeMenuOption);
	}

	private void GetChildhoodPredictWeatherOptionArgs(NarrativeMenuOptionArgs args)
	{
		SkillObject[] affectedSkills = new SkillObject[2]
		{
			NavalSkills.Boatswain,
			DefaultSkills.Scouting
		};
		args.SetAffectedSkills(affectedSkills);
		args.SetFocusToSkills(_focusToAdd);
		args.SetLevelToSkills(_skillLevelToAdd);
		args.SetLevelToAttribute(DefaultCharacterAttributes.Intelligence, _attributeLevelToAdd);
	}

	private bool ChildhoodPredictWeatherOptionOnCondition(CharacterCreationManager characterCreationManager)
	{
		return true;
	}

	private void ChildhoodPredictWeatherOptionOnSelect(CharacterCreationManager characterCreationManager)
	{
		foreach (NarrativeMenuCharacter character in characterCreationManager.CurrentMenu.Characters)
		{
			if (character.StringId == "player_childhood_character")
			{
				character.SetAnimationId("act_childhood_memory");
			}
		}
	}

	private void AddEducationMenuOptions(CharacterCreationManager characterCreationManager)
	{
		NarrativeMenu narrativeMenuWithId = characterCreationManager.GetNarrativeMenuWithId("narrative_education_menu");
		NarrativeMenuOption narrativeMenuOption = new NarrativeMenuOption("education_fishing_boat", new TextObject("{=MHXeREoc}worked as a deckhand on a fishing boat."), new TextObject("{=3H4sk6zN}You spent your adolescence helping your uncle with his fishing business, learning the ropes (literally!) of seamanship, from mending nets to hauling in the catch."), GetEducationFishingBoatOptionArgs, EducationFishingBoatOptionOnCondition, EducationFishingBoatOptionOnSelect, null);
		narrativeMenuWithId.AddNarrativeMenuOption(narrativeMenuOption);
		NarrativeMenuOption narrativeMenuOption2 = new NarrativeMenuOption("education_docks", new TextObject("{=eTXb0QYP}worked at the docks."), new TextObject("{=EDwrct2r}You spent your adolescence helping out at the bustling docks, assisting with the loading and unloading of ships, and learning the ins and outs of maritime trade. You witnessed the arrival and departure of exotic goods and people from far-off lands, fueling your dreams of adventure on the high seas."), GetEducationDocksOptionArgs, EducationDocksOptionOnCondition, EducationDocksOptionOnSelect, null);
		narrativeMenuWithId.AddNarrativeMenuOption(narrativeMenuOption2);
	}

	private void GetEducationFishingBoatOptionArgs(NarrativeMenuOptionArgs args)
	{
		SkillObject[] affectedSkills = new SkillObject[2]
		{
			NavalSkills.Boatswain,
			DefaultSkills.Athletics
		};
		args.SetAffectedSkills(affectedSkills);
		args.SetFocusToSkills(_focusToAdd);
		args.SetLevelToSkills(_skillLevelToAdd);
		args.SetLevelToAttribute(DefaultCharacterAttributes.Endurance, _attributeLevelToAdd);
	}

	private bool EducationFishingBoatOptionOnCondition(CharacterCreationManager characterCreationManager)
	{
		return !NavalCharacterOccupationTypes.IsUrbanOccupation(characterCreationManager.CharacterCreationContent.SelectedParentOccupation);
	}

	private void EducationFishingBoatOptionOnSelect(CharacterCreationManager characterCreationManager)
	{
		foreach (NarrativeMenuCharacter character in characterCreationManager.CurrentMenu.Characters)
		{
			if (character.StringId == "player_education_character")
			{
				character.SetAnimationId("act_childhood_athlete");
				break;
			}
		}
	}

	private void GetEducationDocksOptionArgs(NarrativeMenuOptionArgs args)
	{
		SkillObject[] affectedSkills = new SkillObject[2]
		{
			NavalSkills.Shipmaster,
			DefaultSkills.Trade
		};
		args.SetAffectedSkills(affectedSkills);
		args.SetFocusToSkills(_focusToAdd);
		args.SetLevelToSkills(_skillLevelToAdd);
		args.SetLevelToAttribute(DefaultCharacterAttributes.Social, _attributeLevelToAdd);
	}

	private bool EducationDocksOptionOnCondition(CharacterCreationManager characterCreationManager)
	{
		return NavalCharacterOccupationTypes.IsUrbanOccupation(characterCreationManager.CharacterCreationContent.SelectedParentOccupation);
	}

	private void EducationDocksOptionOnSelect(CharacterCreationManager characterCreationManager)
	{
		foreach (NarrativeMenuCharacter character in characterCreationManager.CurrentMenu.Characters)
		{
			if (character.StringId == "player_education_character")
			{
				character.SetAnimationId("act_childhood_tough");
				break;
			}
		}
	}

	private void AddYouthMenuOptions(CharacterCreationManager characterCreationManager)
	{
		NarrativeMenu narrativeMenuWithId = characterCreationManager.GetNarrativeMenuWithId("narrative_youth_menu");
		NarrativeMenuOption narrativeMenuOption = new NarrativeMenuOption("youth_nord_guard_option", new TextObject("{=I23UbK4E}served as a shieldbearer to a huscarl."), new TextObject("{=Rffyscuk}War was a constant presence in your village. You served as a shieldbearer to a renowned Huscarl, a veteran Nord warrior. Witnessing countless battles and learning the art of defense from a master, you yearn to prove yourself worthy of wielding a weapon in the front lines."), GetYouthNordGuardOptionArgs, YouthNordGuardOptionOnCondition, YouthNordGuardOptionOnSelect, null);
		narrativeMenuWithId.AddNarrativeMenuOption(narrativeMenuOption);
		NarrativeMenuOption narrativeMenuOption2 = new NarrativeMenuOption("youth_nord_skirmisher_option", new TextObject("{=8c7mwLQQ}joined the raiders as a lookout."), new TextObject("{=6X2hZY6z}Growing up on the harsh Nordic coast, you were trained from a young age to spot enemy sails and signal incoming raids. Agile and quick-witted, you honed your skills with a throwing axe and learned to fight in skirmishes. You dream of joining a raiding party and tasting the glory of conquest."), GetYouthNordSkirmisherOptionArgs, YouthNordSkirmisherOptionOnCondition, YouthNordSkirmisherOptionOnSelect, null);
		narrativeMenuWithId.AddNarrativeMenuOption(narrativeMenuOption2);
		NarrativeMenuOption narrativeMenuOption3 = new NarrativeMenuOption("youth_nord_vagabond_option", new TextObject("{=T7B4KmHz}drafted to war as a thrall."), new TextObject("{=lilGmaCg}Thrown into servitude to a Jarl, war ripped through your village. Drafted alongside other thralls, you were thrown into battle with minimal training and a simple spear. Though fear grips you, a deep loyalty to your Jarl and a desperate will to survive drive you forward."), GetYouthNordVagabondOptionArgs, YouthNordVagabondOptionOnCondition, YouthNordVagabondOptionOnSelect, null);
		narrativeMenuWithId.AddNarrativeMenuOption(narrativeMenuOption3);
		NarrativeMenuOption narrativeMenuOption4 = new NarrativeMenuOption("youth_nord_artisan_option", new TextObject("{=qJweXkmJ}stood sentry at the Walls of the Hold."), new TextObject("{=XpiyI865}With enemy forces constantly threatening your village, you spent your youth helping fortify the local hold. You became skilled in basic construction, learned to use a pickaxe and shovel, and assisted in defending the walls during sieges. Now, you yearn to be part of the offensive and take the fight to the enemy."), GetYouthNordArtisanOptionArgs, YouthNordArtisanOptionOnCondition, YouthNordArtisanOptionOnSelect, null);
		narrativeMenuWithId.AddNarrativeMenuOption(narrativeMenuOption4);
		NarrativeMenuOption narrativeMenuOption5 = new NarrativeMenuOption("youth_nord_infantry_option", new TextObject("{=ZfaBIuFL}scavenged the battlefields for scraps."), new TextObject("{=unIV7bqB}Born into a Calradia perpetually at war, you didn't know playgrounds, you knew battlefields. Survival as a youngster meant picking through the battlefields. You learned to be self-sufficient, sometimes tending to wounds amidst the carnage. The law of the battlefield was simple: take what you can, and don't get caught."), GetYouthNordInfantryOptionArgs, YouthNordInfantryOptionOnCondition, YouthNordInfantryOptionOnSelect, null);
		narrativeMenuWithId.AddNarrativeMenuOption(narrativeMenuOption5);
		NarrativeMenuOption narrativeMenuOption6 = new NarrativeMenuOption("youth_nord_mercenary_option", new TextObject("{=On8SIR0J}became a warchild of the North."), new TextObject("{=La4V8zQn}War ravaged your village, leaving you orphaned and hardened by hardship. You scavenged for scraps, learning to fight for survival in the harsh wilderness. Now, driven by a thirst for vengeance and a desire to carve your own path, you seek to join a warband and prove your worth."), GetYouthNordMercenaryOptionArgs, YouthNordMercenaryOptionOnCondition, YouthNordMercenaryOptionOnSelect, null);
		narrativeMenuWithId.AddNarrativeMenuOption(narrativeMenuOption6);
		NarrativeMenuOption narrativeMenuOption7 = new NarrativeMenuOption("youth_crewed_a_galley_option", new TextObject("{=Hhkt6gtQ}crewed a galley in the coastal raids."), new TextObject("{=KWI2QOAO}You spent your youth participating in coastal raids, learning the skills of a rower, a lookout, and a boarding party. You witnessed the thrill of naval combat firsthand, experiencing the fear and the glory of maritime warfare."), GetYouthCrewedAGalleyNarrativeOptionArgs, YouthCrewedAGalleyNarrativeOptionOnCondition, YouthCrewedAGalleyNarrativeOptionOnSelect, null);
		narrativeMenuWithId.AddNarrativeMenuOption(narrativeMenuOption7);
		NarrativeMenuOption narrativeMenuOption8 = new NarrativeMenuOption("youth_rowed_river_trader_option", new TextObject("{=BRcMIDYK}rowed on a river trader."), new TextObject("{=urpdbYXl}You spent your youth helping your family transport goods along the river, learning to navigate the treacherous currents and to defend yourselves from raiders. You witnessed the bustling trade centers and encountered a diverse array of cultures."), GetYouthRowedRiverTraderNarrativeOptionArgs, YouthRowedRiverTraderNarrativeOptionOnCondition, YouthRowedRiverTraderNarrativeOptionOnSelect, null);
		narrativeMenuWithId.AddNarrativeMenuOption(narrativeMenuOption8);
		NarrativeMenuOption narrativeMenuOption9 = new NarrativeMenuOption("youth_deckhand_corsair_option", new TextObject("{=h0h4abww}served as a deckhand on a corsair."), new TextObject("{=LVxRFT5b}Growing up in a coastal town, you were drawn to the allure of the sea and the thrill of adventure. You joined a corsair crew as a deckhand, learning the ropes seamanship and witnessing the brutality of pirate raids firsthand."), GetYouthDeckhandCorsairNarrativeOptionArgs, YouthDeckhandCorsairNarrativeOptionOnCondition, YouthDeckhandCorsairNarrativeOptionOnSelect, null);
		narrativeMenuWithId.AddNarrativeMenuOption(narrativeMenuOption9);
		NarrativeMenuOption narrativeMenuOption10 = new NarrativeMenuOption("youth_raided_river_traffic_option", new TextObject("{=C04DgO2S}raided river traffic."), new TextObject("{=lHd0H3jg}You grew up along the great rivers, learning to navigate the treacherous currents and to fight from swift riverboats. You learned to raid rival clans and extort tribute from wealthy merchants, honing your skills as a river pirate."), GetYouthRaidedRiverTrafficNarrativeOptionArgs, YouthRaidedRiverTrafficNarrativeOptionOnCondition, YouthRaidedRiverTrafficNarrativeOptionOnSelect, null);
		narrativeMenuWithId.AddNarrativeMenuOption(narrativeMenuOption10);
		NarrativeMenuOption narrativeMenuOption11 = new NarrativeMenuOption("youth_coastal_defender_option", new TextObject("{=OMnJGBCR}served as a coastal defender."), new TextObject("{=gvp4AsMQ}You grew up amidst tales of legendary sea battles and legendary heroes, destined to carry on the proud traditions of your seafaring ancestors and defend your coastal towns just like them. You learned the skills of a mariner, a rower, and a warrior."), GetYouthCoastalDefenderNarrativeOptionArgs, YouthCoastalDefenderNarrativeOptionOnCondition, YouthCoastalDefenderNarrativeOptionOnSelect, null);
		narrativeMenuWithId.AddNarrativeMenuOption(narrativeMenuOption11);
		NarrativeMenuOption narrativeMenuOption12 = new NarrativeMenuOption("youth_serve_raider_ship_option", new TextObject("{=8GnOKv5r}went serving on a raider ship."), new TextObject("{=Xclr9fU3}You grew up in a coastal village, surrounded by tales of legendary Viking warriors and their daring voyages. As a youth, you learned the skills of a sailor, a warrior, and a raider, preparing for the day when you would join your kin on a voyage of exploration and conquest."), GetYouthServeRaiderShipNarrativeOptionArgs, YouthServeRaiderShipNarrativeOptionOnCondition, YouthServeRaiderShipNarrativeOptionOnSelect, null);
		narrativeMenuWithId.AddNarrativeMenuOption(narrativeMenuOption12);
	}

	private void GetYouthNordGuardOptionArgs(NarrativeMenuOptionArgs args)
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

	private bool YouthNordGuardOptionOnCondition(CharacterCreationManager characterCreationManager)
	{
		return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "nord";
	}

	private void YouthNordGuardOptionOnSelect(CharacterCreationManager characterCreationManager)
	{
		characterCreationManager.CharacterCreationContent.SelectedTitleType = "guard";
		string playerEquipmentId = GetPlayerEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedTitleType, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId, Hero.MainHero.IsFemale);
		foreach (NarrativeMenuCharacter character in characterCreationManager.CurrentMenu.Characters)
		{
			if (character.StringId == "player_youth_character")
			{
				character.SetAnimationId("act_character_creation_nord_served_as_a_shieldbearer");
				character.SetEquipment(Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(playerEquipmentId));
			}
		}
	}

	private void GetYouthNordSkirmisherOptionArgs(NarrativeMenuOptionArgs args)
	{
		SkillObject[] affectedSkills = new SkillObject[2]
		{
			DefaultSkills.Throwing,
			DefaultSkills.Tactics
		};
		args.SetAffectedSkills(affectedSkills);
		args.SetFocusToSkills(_focusToAdd);
		args.SetLevelToSkills(_skillLevelToAdd);
		args.SetLevelToAttribute(DefaultCharacterAttributes.Cunning, _attributeLevelToAdd);
	}

	private bool YouthNordSkirmisherOptionOnCondition(CharacterCreationManager characterCreationManager)
	{
		return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "nord";
	}

	private void YouthNordSkirmisherOptionOnSelect(CharacterCreationManager characterCreationManager)
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

	private void GetYouthNordVagabondOptionArgs(NarrativeMenuOptionArgs args)
	{
		SkillObject[] affectedSkills = new SkillObject[2]
		{
			DefaultSkills.OneHanded,
			DefaultSkills.Polearm
		};
		args.SetAffectedSkills(affectedSkills);
		args.SetFocusToSkills(_focusToAdd);
		args.SetLevelToSkills(_skillLevelToAdd);
		args.SetLevelToAttribute(DefaultCharacterAttributes.Endurance, _attributeLevelToAdd);
	}

	private bool YouthNordVagabondOptionOnCondition(CharacterCreationManager characterCreationManager)
	{
		return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "nord";
	}

	private void YouthNordVagabondOptionOnSelect(CharacterCreationManager characterCreationManager)
	{
		characterCreationManager.CharacterCreationContent.SelectedTitleType = "vagabond";
		string playerEquipmentId = GetPlayerEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedTitleType, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId, Hero.MainHero.IsFemale);
		foreach (NarrativeMenuCharacter character in characterCreationManager.CurrentMenu.Characters)
		{
			if (character.StringId == "player_youth_character")
			{
				character.SetAnimationId("act_drafted_to_war_pose");
				character.SetEquipment(Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(playerEquipmentId));
			}
		}
	}

	private void GetYouthNordArtisanOptionArgs(NarrativeMenuOptionArgs args)
	{
		SkillObject[] affectedSkills = new SkillObject[2]
		{
			DefaultSkills.Engineering,
			DefaultSkills.Bow
		};
		args.SetAffectedSkills(affectedSkills);
		args.SetFocusToSkills(_focusToAdd);
		args.SetLevelToSkills(_skillLevelToAdd);
		args.SetLevelToAttribute(DefaultCharacterAttributes.Intelligence, _attributeLevelToAdd);
	}

	private bool YouthNordArtisanOptionOnCondition(CharacterCreationManager characterCreationManager)
	{
		return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "nord";
	}

	private void YouthNordArtisanOptionOnSelect(CharacterCreationManager characterCreationManager)
	{
		characterCreationManager.CharacterCreationContent.SelectedTitleType = "artisan";
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

	private void GetYouthNordInfantryOptionArgs(NarrativeMenuOptionArgs args)
	{
		SkillObject[] affectedSkills = new SkillObject[2]
		{
			DefaultSkills.Roguery,
			DefaultSkills.Medicine
		};
		args.SetAffectedSkills(affectedSkills);
		args.SetFocusToSkills(_focusToAdd);
		args.SetLevelToSkills(_skillLevelToAdd);
		args.SetLevelToAttribute(DefaultCharacterAttributes.Cunning, _attributeLevelToAdd);
	}

	private bool YouthNordInfantryOptionOnCondition(CharacterCreationManager characterCreationManager)
	{
		return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "nord";
	}

	private void YouthNordInfantryOptionOnSelect(CharacterCreationManager characterCreationManager)
	{
		characterCreationManager.CharacterCreationContent.SelectedTitleType = "infantry";
		string playerEquipmentId = GetPlayerEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedTitleType, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId, Hero.MainHero.IsFemale);
		foreach (NarrativeMenuCharacter character in characterCreationManager.CurrentMenu.Characters)
		{
			if (character.StringId == "player_youth_character")
			{
				character.SetAnimationId("act_character_creation_nord_served_as_a_shieldbearer");
				character.SetEquipment(Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(playerEquipmentId));
			}
		}
	}

	private void GetYouthNordMercenaryOptionArgs(NarrativeMenuOptionArgs args)
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

	private bool YouthNordMercenaryOptionOnCondition(CharacterCreationManager characterCreationManager)
	{
		return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "nord";
	}

	private void YouthNordMercenaryOptionOnSelect(CharacterCreationManager characterCreationManager)
	{
		characterCreationManager.CharacterCreationContent.SelectedTitleType = "mercenary";
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

	private void GetYouthCrewedAGalleyNarrativeOptionArgs(NarrativeMenuOptionArgs args)
	{
		SkillObject[] affectedSkills = new SkillObject[2]
		{
			NavalSkills.Mariner,
			DefaultSkills.OneHanded
		};
		args.SetAffectedSkills(affectedSkills);
		args.SetFocusToSkills(_focusToAdd);
		args.SetLevelToSkills(_skillLevelToAdd);
		args.SetLevelToAttribute(DefaultCharacterAttributes.Vigor, _attributeLevelToAdd);
	}

	private bool YouthCrewedAGalleyNarrativeOptionOnCondition(CharacterCreationManager characterCreationManager)
	{
		if (!(characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "vlandia"))
		{
			return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "empire";
		}
		return true;
	}

	private void YouthCrewedAGalleyNarrativeOptionOnSelect(CharacterCreationManager characterCreationManager)
	{
		characterCreationManager.CharacterCreationContent.SelectedTitleType = "seafarer";
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

	private void GetYouthRowedRiverTraderNarrativeOptionArgs(NarrativeMenuOptionArgs args)
	{
		SkillObject[] affectedSkills = new SkillObject[2]
		{
			NavalSkills.Boatswain,
			DefaultSkills.TwoHanded
		};
		args.SetAffectedSkills(affectedSkills);
		args.SetFocusToSkills(_focusToAdd);
		args.SetLevelToSkills(_skillLevelToAdd);
		args.SetLevelToAttribute(DefaultCharacterAttributes.Endurance, _attributeLevelToAdd);
	}

	private bool YouthRowedRiverTraderNarrativeOptionOnCondition(CharacterCreationManager characterCreationManager)
	{
		return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "sturgia";
	}

	private void YouthRowedRiverTraderNarrativeOptionOnSelect(CharacterCreationManager characterCreationManager)
	{
		characterCreationManager.CharacterCreationContent.SelectedTitleType = "seafarer";
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

	private void GetYouthDeckhandCorsairNarrativeOptionArgs(NarrativeMenuOptionArgs args)
	{
		SkillObject[] affectedSkills = new SkillObject[2]
		{
			NavalSkills.Mariner,
			DefaultSkills.OneHanded
		};
		args.SetAffectedSkills(affectedSkills);
		args.SetFocusToSkills(_focusToAdd);
		args.SetLevelToSkills(_skillLevelToAdd);
		args.SetLevelToAttribute(DefaultCharacterAttributes.Cunning, _attributeLevelToAdd);
	}

	private bool YouthDeckhandCorsairNarrativeOptionOnCondition(CharacterCreationManager characterCreationManager)
	{
		return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "aserai";
	}

	private void YouthDeckhandCorsairNarrativeOptionOnSelect(CharacterCreationManager characterCreationManager)
	{
		characterCreationManager.CharacterCreationContent.SelectedTitleType = "seafarer";
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

	private void GetYouthRaidedRiverTrafficNarrativeOptionArgs(NarrativeMenuOptionArgs args)
	{
		SkillObject[] affectedSkills = new SkillObject[2]
		{
			NavalSkills.Mariner,
			DefaultSkills.Charm
		};
		args.SetAffectedSkills(affectedSkills);
		args.SetFocusToSkills(_focusToAdd);
		args.SetLevelToSkills(_skillLevelToAdd);
		args.SetLevelToAttribute(DefaultCharacterAttributes.Social, _attributeLevelToAdd);
	}

	private bool YouthRaidedRiverTrafficNarrativeOptionOnCondition(CharacterCreationManager characterCreationManager)
	{
		return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "khuzait";
	}

	private void YouthRaidedRiverTrafficNarrativeOptionOnSelect(CharacterCreationManager characterCreationManager)
	{
		characterCreationManager.CharacterCreationContent.SelectedTitleType = "seafarer";
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

	private void GetYouthCoastalDefenderNarrativeOptionArgs(NarrativeMenuOptionArgs args)
	{
		SkillObject[] affectedSkills = new SkillObject[2]
		{
			DefaultSkills.Bow,
			NavalSkills.Boatswain
		};
		args.SetAffectedSkills(affectedSkills);
		args.SetFocusToSkills(_focusToAdd);
		args.SetLevelToSkills(_skillLevelToAdd);
		args.SetLevelToAttribute(DefaultCharacterAttributes.Vigor, _attributeLevelToAdd);
	}

	private bool YouthCoastalDefenderNarrativeOptionOnCondition(CharacterCreationManager characterCreationManager)
	{
		return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "battania";
	}

	private void YouthCoastalDefenderNarrativeOptionOnSelect(CharacterCreationManager characterCreationManager)
	{
		characterCreationManager.CharacterCreationContent.SelectedTitleType = "seafarer";
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

	private void GetYouthServeRaiderShipNarrativeOptionArgs(NarrativeMenuOptionArgs args)
	{
		SkillObject[] affectedSkills = new SkillObject[2]
		{
			NavalSkills.Mariner,
			DefaultSkills.OneHanded
		};
		args.SetAffectedSkills(affectedSkills);
		args.SetFocusToSkills(_focusToAdd);
		args.SetLevelToSkills(_skillLevelToAdd);
		args.SetLevelToAttribute(DefaultCharacterAttributes.Vigor, _attributeLevelToAdd);
	}

	private bool YouthServeRaiderShipNarrativeOptionOnCondition(CharacterCreationManager characterCreationManager)
	{
		return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "nord";
	}

	private void YouthServeRaiderShipNarrativeOptionOnSelect(CharacterCreationManager characterCreationManager)
	{
		characterCreationManager.CharacterCreationContent.SelectedTitleType = "seafarer";
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
}
