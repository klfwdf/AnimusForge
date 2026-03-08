using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem.CharacterCreationContent;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors
{
	// Token: 0x020003DA RID: 986
	public class CharacterCreationCampaignBehavior : CampaignBehaviorBase, ICharacterCreationContentHandler
	{
		// Token: 0x06003B01 RID: 15105 RVA: 0x000F77A8 File Offset: 0x000F59A8
		private string GetMotherEquipmentId(CharacterCreationManager characterCreationManager, string occupationType, string cultureId)
		{
			string str;
			characterCreationManager.CharacterCreationContent.TryGetEquipmentToUse(occupationType, out str);
			return "mother_char_creation_" + str + "_" + cultureId;
		}

		// Token: 0x06003B02 RID: 15106 RVA: 0x000F77D8 File Offset: 0x000F59D8
		private string GetFatherEquipmentId(CharacterCreationManager characterCreationManager, string occupationType, string cultureId)
		{
			string str;
			characterCreationManager.CharacterCreationContent.TryGetEquipmentToUse(occupationType, out str);
			return "father_char_creation_" + str + "_" + cultureId;
		}

		// Token: 0x06003B03 RID: 15107 RVA: 0x000F7808 File Offset: 0x000F5A08
		private string GetPlayerChildhoodAgeEquipmentId(CharacterCreationManager characterCreationManager, string parentOccupationType, string cultureId, bool isFemale)
		{
			string text;
			characterCreationManager.CharacterCreationContent.TryGetEquipmentToUse(parentOccupationType, out text);
			return string.Concat(new string[]
			{
				"player_char_creation_childhood_age_",
				cultureId,
				"_",
				text,
				"_",
				isFemale ? "f" : "m"
			});
		}

		// Token: 0x06003B04 RID: 15108 RVA: 0x000F7864 File Offset: 0x000F5A64
		private string GetPlayerEducationAgeEquipmentId(CharacterCreationManager characterCreationManager, string parentOccupationType, string cultureId, bool isFemale)
		{
			string text;
			characterCreationManager.CharacterCreationContent.TryGetEquipmentToUse(parentOccupationType, out text);
			return string.Concat(new string[]
			{
				"player_char_creation_education_age_",
				cultureId,
				"_",
				text,
				"_",
				isFemale ? "f" : "m"
			});
		}

		// Token: 0x06003B05 RID: 15109 RVA: 0x000F78C0 File Offset: 0x000F5AC0
		private string GetPlayerEquipmentId(CharacterCreationManager characterCreationManager, string occupationType, string cultureId, bool isFemale)
		{
			string text;
			characterCreationManager.CharacterCreationContent.TryGetEquipmentToUse(occupationType, out text);
			return string.Concat(new string[]
			{
				"player_char_creation_",
				cultureId,
				"_",
				text,
				"_",
				isFemale ? "f" : "m"
			});
		}

		// Token: 0x06003B06 RID: 15110 RVA: 0x000F791A File Offset: 0x000F5B1A
		public override void RegisterEvents()
		{
			CampaignEvents.OnCharacterCreationInitializedEvent.AddNonSerializedListener(this, new Action<CharacterCreationManager>(this.OnCharacterCreationInitialized));
		}

		// Token: 0x06003B07 RID: 15111 RVA: 0x000F7933 File Offset: 0x000F5B33
		public override void SyncData(IDataStore dataStore)
		{
		}

		// Token: 0x06003B08 RID: 15112 RVA: 0x000F7938 File Offset: 0x000F5B38
		private void OnCharacterCreationInitialized(CharacterCreationManager characterCreationManager)
		{
			this._focusToAdd = characterCreationManager.CharacterCreationContent.FocusToAdd;
			this._skillLevelToAdd = characterCreationManager.CharacterCreationContent.SkillLevelToAdd;
			this._attributeLevelToAdd = characterCreationManager.CharacterCreationContent.AttributeLevelToAdd;
			characterCreationManager.CharacterCreationContent.DefaultSelectedTitleType = "guard";
			characterCreationManager.RegisterCharacterCreationContentHandler(this, 800);
		}

		// Token: 0x06003B09 RID: 15113 RVA: 0x000F7994 File Offset: 0x000F5B94
		void ICharacterCreationContentHandler.InitializeContent(CharacterCreationManager characterCreationManager)
		{
			characterCreationManager.CharacterCreationContent.AddEquipmentToUseGetter(delegate(string occupationId, out string equipmentId)
			{
				return this._occupationToEquipmentMapping.TryGetValue(occupationId, out equipmentId);
			});
			this.InitializeCharacterCreationStages(characterCreationManager);
			this.InitializeCharacterCreationCultures(characterCreationManager);
			this.InitializeData(characterCreationManager);
		}

		// Token: 0x06003B0A RID: 15114 RVA: 0x000F79C2 File Offset: 0x000F5BC2
		void ICharacterCreationContentHandler.AfterInitializeContent(CharacterCreationManager characterCreationManager)
		{
		}

		// Token: 0x06003B0B RID: 15115 RVA: 0x000F79C4 File Offset: 0x000F5BC4
		void ICharacterCreationContentHandler.OnStageCompleted(CharacterCreationStageBase stage)
		{
			if (stage is CharacterCreationFaceGeneratorStage)
			{
				this.FaceGenUpdated();
			}
		}

		// Token: 0x06003B0C RID: 15116 RVA: 0x000F79D4 File Offset: 0x000F5BD4
		void ICharacterCreationContentHandler.OnCharacterCreationFinalize(CharacterCreationManager characterCreationManager)
		{
		}

		// Token: 0x06003B0D RID: 15117 RVA: 0x000F79D8 File Offset: 0x000F5BD8
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

		// Token: 0x06003B0E RID: 15118 RVA: 0x000F7A34 File Offset: 0x000F5C34
		public void InitializeCharacterCreationCultures(CharacterCreationManager characterCreationManager)
		{
			foreach (CultureObject cultureObject in Game.Current.ObjectManager.GetObjectTypeList<CultureObject>())
			{
				if (cultureObject.StringId == "aserai" || cultureObject.StringId == "battania" || cultureObject.StringId == "empire" || cultureObject.StringId == "khuzait" || cultureObject.StringId == "sturgia" || cultureObject.StringId == "vlandia")
				{
					characterCreationManager.CharacterCreationContent.AddCharacterCreationCulture(cultureObject, 1, 10);
				}
			}
		}

		// Token: 0x06003B0F RID: 15119 RVA: 0x000F7B0C File Offset: 0x000F5D0C
		public void InitializeData(CharacterCreationManager characterCreationManager)
		{
			characterCreationManager.CharacterCreationContent.ChangeReviewPageDescription(new TextObject("{=W6pKpEoT}You prepare to set off for a grand adventure in Calradia! Here is your character. Continue if you are ready, or go back to make changes.", null));
			this.AddParentsMenu(characterCreationManager);
			this.AddChildhoodMenu(characterCreationManager);
			this.AddEducationMenu(characterCreationManager);
			this.AddYouthMenu(characterCreationManager);
			this.AddAdulthoodMenu(characterCreationManager);
			this.AddAgeSelectionMenu(characterCreationManager);
		}

		// Token: 0x06003B10 RID: 15120 RVA: 0x000F7B5C File Offset: 0x000F5D5C
		public void FaceGenUpdated()
		{
			CharacterCreationManager characterCreationManager = (GameStateManager.Current.ActiveState as CharacterCreationState).CharacterCreationManager;
			BodyProperties bodyProperties2;
			BodyProperties bodyProperties;
			FaceGen.GenerateParentKey(bodyProperties = (bodyProperties2 = CharacterObject.PlayerCharacter.GetBodyProperties(CharacterObject.PlayerCharacter.Equipment, -1)), CharacterObject.PlayerCharacter.Race, ref bodyProperties2, ref bodyProperties);
			bodyProperties2 = new BodyProperties(new DynamicBodyProperties(33f, 0.3f, 0.2f), bodyProperties2.StaticProperties);
			bodyProperties = new BodyProperties(new DynamicBodyProperties(33f, 0.5f, 0.5f), bodyProperties.StaticProperties);
			foreach (NarrativeMenu narrativeMenu in characterCreationManager.NarrativeMenus)
			{
				foreach (NarrativeMenuCharacter narrativeMenuCharacter in narrativeMenu.Characters)
				{
					if (narrativeMenuCharacter.StringId.Equals("mother_character"))
					{
						narrativeMenuCharacter.UpdateBodyProperties(bodyProperties2, CharacterObject.PlayerCharacter.Race, true);
					}
					if (narrativeMenuCharacter.StringId.Equals("father_character"))
					{
						narrativeMenuCharacter.UpdateBodyProperties(bodyProperties, CharacterObject.PlayerCharacter.Race, false);
					}
					if (narrativeMenuCharacter.StringId.Equals("player_childhood_character") || narrativeMenuCharacter.StringId.Equals("player_education_character") || narrativeMenuCharacter.StringId.Equals("player_youth_character") || narrativeMenuCharacter.StringId.Equals("player_adulthood_character") || narrativeMenuCharacter.StringId.Equals("player_age_selection_character"))
					{
						narrativeMenuCharacter.UpdateBodyProperties(CharacterObject.PlayerCharacter.GetBodyProperties(null, -1), CharacterObject.PlayerCharacter.Race, false);
					}
				}
			}
		}

		// Token: 0x06003B11 RID: 15121 RVA: 0x000F7D54 File Offset: 0x000F5F54
		private List<NarrativeMenuCharacterArgs> GetParentMenuNarrativeMenuCharacterArgs(CultureObject culture, string occupationType, CharacterCreationManager characterCreationManager)
		{
			return new List<NarrativeMenuCharacterArgs>
			{
				new NarrativeMenuCharacterArgs("mother_character", 33, "mother_char_creation_none_" + characterCreationManager.CharacterCreationContent.SelectedCulture.StringId, "act_character_creation_female_default_standing", "spawnpoint_player_1", "", "", null, true, true),
				new NarrativeMenuCharacterArgs("father_character", 33, "father_char_creation_none_" + characterCreationManager.CharacterCreationContent.SelectedCulture.StringId, "act_character_creation_male_default_standing", "spawnpoint_player_1", "", "", null, true, false)
			};
		}

		// Token: 0x06003B12 RID: 15122 RVA: 0x000F7DEC File Offset: 0x000F5FEC
		private void AddParentsMenu(CharacterCreationManager characterCreationManager)
		{
			List<NarrativeMenuCharacter> list = new List<NarrativeMenuCharacter>();
			BodyProperties bodyProperties2;
			BodyProperties bodyProperties;
			FaceGen.GenerateParentKey(bodyProperties = (bodyProperties2 = CharacterObject.PlayerCharacter.GetBodyProperties(CharacterObject.PlayerCharacter.Equipment, -1)), CharacterObject.PlayerCharacter.Race, ref bodyProperties2, ref bodyProperties);
			bodyProperties2 = new BodyProperties(new DynamicBodyProperties(33f, 0.3f, 0.2f), bodyProperties2.StaticProperties);
			bodyProperties = new BodyProperties(new DynamicBodyProperties(33f, 0.5f, 0.5f), bodyProperties.StaticProperties);
			NarrativeMenuCharacter item = new NarrativeMenuCharacter("mother_character", bodyProperties2, CharacterObject.PlayerCharacter.Race, true);
			list.Add(item);
			NarrativeMenuCharacter item2 = new NarrativeMenuCharacter("father_character", bodyProperties, CharacterObject.PlayerCharacter.Race, false);
			list.Add(item2);
			NarrativeMenu narrativeMenu = new NarrativeMenu("narrative_parent_menu", "start", "narrative_childhood_menu", new TextObject("{=b4lDDcli}Family", null), new TextObject("{=XgFU1pCx}You were born into a family of...", null), list, new NarrativeMenu.GetNarrativeMenuCharacterArgsDelegate(this.GetParentMenuNarrativeMenuCharacterArgs));
			this.AddEmpireParentNarrativeMenuOptions(narrativeMenu);
			this.AddVlandianParentNarrativeMenuOptions(narrativeMenu);
			this.AddSturgianParentNarrativeMenuOptions(narrativeMenu);
			this.AddAseraiParentNarrativeMenuOptions(narrativeMenu);
			this.AddBattaniaNarrativeMenuOptions(narrativeMenu);
			this.AddKhuzaitNarrativeMenuOptions(narrativeMenu);
			characterCreationManager.AddNewMenu(narrativeMenu);
		}

		// Token: 0x06003B13 RID: 15123 RVA: 0x000F7F20 File Offset: 0x000F6120
		private void AddEmpireParentNarrativeMenuOptions(NarrativeMenu narrativeMenu)
		{
			NarrativeMenuOption narrativeMenuOption = new NarrativeMenuOption("empire_lanlord_option", new TextObject("{=InN5ZZt3}A landlord's retainers", null), new TextObject("{=ivKl4mV2}Your father was a trusted lieutenant of the local landowning aristocrat. He rode with the lord's cavalry, fighting as an armored lancer.", null), new GetNarrativeMenuOptionArgsDelegate(this.GetEmpireLandlordNarrativeOptionArgs), new NarrativeMenuOptionOnConditionDelegate(this.EmpireLandlordNarrativeOptionOnCondition), new NarrativeMenuOptionOnSelectDelegate(this.EmpireLandlordNarrativeOptionOnSelect), null);
			narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption);
			NarrativeMenuOption narrativeMenuOption2 = new NarrativeMenuOption("empire_merchant_option", new TextObject("{=651FhzdR}Urban merchants", null), new TextObject("{=FQntPChs}Your family were merchants in one of the main cities of the Empire. They sometimes organized caravans to nearby towns, and discussed issues in the town council.", null), new GetNarrativeMenuOptionArgsDelegate(this.GetEmpireUrbanNarrativeOptionArgs), new NarrativeMenuOptionOnConditionDelegate(this.EmpireUrbanNarrativeOptionOnCondition), new NarrativeMenuOptionOnSelectDelegate(this.EmpireUrbanNarrativeOptionOnSelect), null);
			narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption2);
			NarrativeMenuOption narrativeMenuOption3 = new NarrativeMenuOption("empire_farmer_option", new TextObject("{=sb4gg8Ak}Freeholders", null), new TextObject("{=09z8Q08f}Your family were small farmers with just enough land to feed themselves and make a small profit. People like them were the pillars of the imperial rural economy, as well as the backbone of the levy.", null), new GetNarrativeMenuOptionArgsDelegate(this.GetEmpireFarmerNarrativeOptionArgs), new NarrativeMenuOptionOnConditionDelegate(this.EmpireFarmerNarrativeOptionOnCondition), new NarrativeMenuOptionOnSelectDelegate(this.EmpireFarmerNarrativeOptionOnSelect), null);
			narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption3);
			NarrativeMenuOption narrativeMenuOption4 = new NarrativeMenuOption("empire_artisan_option", new TextObject("{=v48N6h1t}Urban artisans", null), new TextObject("{=ueCm5y1C}Your family owned their own workshop in a city, making goods from raw materials brought in from the countryside. Your father played an active if minor role in the town council, and also served in the militia.", null), new GetNarrativeMenuOptionArgsDelegate(this.GetEmpireArtisanNarrativeOptionArgs), new NarrativeMenuOptionOnConditionDelegate(this.EmpireArtisanNarrativeOptionOnCondition), new NarrativeMenuOptionOnSelectDelegate(this.EmpireArtisanNarrativeOptionOnSelect), null);
			narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption4);
			NarrativeMenuOption narrativeMenuOption5 = new NarrativeMenuOption("empire_hunter_option", new TextObject("{=7eWmU2mF}Foresters", null), new TextObject("{=yRFSzSDZ}Your family lived in a village, but did not own their own land. Instead, your father supplemented paid jobs with long trips in the woods, hunting and trapping, always keeping a wary eye for the lord's game wardens.", null), new GetNarrativeMenuOptionArgsDelegate(this.GetEmpireHunterNarrativeOptionArgs), new NarrativeMenuOptionOnConditionDelegate(this.EmpireHunterNarrativeOptionOnCondition), new NarrativeMenuOptionOnSelectDelegate(this.EmpireHunterNarrativeOptionOnSelect), null);
			narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption5);
			NarrativeMenuOption narrativeMenuOption6 = new NarrativeMenuOption("empire_vagabond_option", new TextObject("{=aEke8dSb}Urban vagabonds", null), new TextObject("{=Jvf6K7TZ}Your family numbered among the many poor migrants living in the slums that grow up outside the walls of imperial cities, making whatever money they could from a variety of odd jobs. Sometimes they did service for one of the Empire's many criminal gangs, and you had an early look at the dark side of life.", null), new GetNarrativeMenuOptionArgsDelegate(this.GetEmpireVagabondNarrativeOptionArgs), new NarrativeMenuOptionOnConditionDelegate(this.EmpireVagabondNarrativeOptionOnCondition), new NarrativeMenuOptionOnSelectDelegate(this.EmpireVagabondNarrativeOptionOnSelect), null);
			narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption6);
		}

		// Token: 0x06003B14 RID: 15124 RVA: 0x000F8100 File Offset: 0x000F6300
		private void GetEmpireLandlordNarrativeOptionArgs(NarrativeMenuOptionArgs args)
		{
			SkillObject[] affectedSkills = new SkillObject[]
			{
				DefaultSkills.Riding,
				DefaultSkills.Polearm
			};
			args.SetAffectedSkills(affectedSkills);
			args.SetFocusToSkills(this._focusToAdd);
			args.SetLevelToSkills(this._skillLevelToAdd);
			args.SetLevelToAttribute(DefaultCharacterAttributes.Vigor, this._attributeLevelToAdd);
		}

		// Token: 0x06003B15 RID: 15125 RVA: 0x000F8154 File Offset: 0x000F6354
		private bool EmpireLandlordNarrativeOptionOnCondition(CharacterCreationManager characterCreationManager)
		{
			return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "empire";
		}

		// Token: 0x06003B16 RID: 15126 RVA: 0x000F8170 File Offset: 0x000F6370
		private void EmpireLandlordNarrativeOptionOnSelect(CharacterCreationManager characterCreationManager)
		{
			characterCreationManager.CharacterCreationContent.SetParentOccupation("retainer");
			string motherEquipmentId = this.GetMotherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
			string fatherEquipmentId = this.GetFatherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
			MBEquipmentRoster @object = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(motherEquipmentId);
			MBEquipmentRoster object2 = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(fatherEquipmentId);
			string motherAnimation = "act_character_creation_female_default_side_to_side_1";
			string fatherAnimation = "act_character_creation_male_default_side_to_side_1";
			this.UpdateParentEquipment(characterCreationManager, @object, object2, motherAnimation, fatherAnimation);
		}

		// Token: 0x06003B17 RID: 15127 RVA: 0x000F8210 File Offset: 0x000F6410
		private void GetEmpireUrbanNarrativeOptionArgs(NarrativeMenuOptionArgs args)
		{
			SkillObject[] affectedSkills = new SkillObject[]
			{
				DefaultSkills.Trade,
				DefaultSkills.Charm
			};
			args.SetAffectedSkills(affectedSkills);
			args.SetFocusToSkills(this._focusToAdd);
			args.SetLevelToSkills(this._skillLevelToAdd);
			args.SetLevelToAttribute(DefaultCharacterAttributes.Social, this._attributeLevelToAdd);
		}

		// Token: 0x06003B18 RID: 15128 RVA: 0x000F8264 File Offset: 0x000F6464
		private bool EmpireUrbanNarrativeOptionOnCondition(CharacterCreationManager characterCreationManager)
		{
			return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "empire";
		}

		// Token: 0x06003B19 RID: 15129 RVA: 0x000F8280 File Offset: 0x000F6480
		private void EmpireUrbanNarrativeOptionOnSelect(CharacterCreationManager characterCreationManager)
		{
			characterCreationManager.CharacterCreationContent.SetParentOccupation("merchant_urban");
			string motherEquipmentId = this.GetMotherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
			string fatherEquipmentId = this.GetFatherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
			MBEquipmentRoster @object = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(motherEquipmentId);
			MBEquipmentRoster object2 = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(fatherEquipmentId);
			string motherAnimation = "act_character_creation_female_default_mother_front";
			string fatherAnimation = "act_character_creation_male_default_mother_front";
			this.UpdateParentEquipment(characterCreationManager, @object, object2, motherAnimation, fatherAnimation);
		}

		// Token: 0x06003B1A RID: 15130 RVA: 0x000F8320 File Offset: 0x000F6520
		private void GetEmpireFarmerNarrativeOptionArgs(NarrativeMenuOptionArgs args)
		{
			SkillObject[] affectedSkills = new SkillObject[]
			{
				DefaultSkills.Athletics,
				DefaultSkills.Polearm
			};
			args.SetAffectedSkills(affectedSkills);
			args.SetFocusToSkills(this._focusToAdd);
			args.SetLevelToSkills(this._skillLevelToAdd);
			args.SetLevelToAttribute(DefaultCharacterAttributes.Endurance, this._attributeLevelToAdd);
		}

		// Token: 0x06003B1B RID: 15131 RVA: 0x000F8374 File Offset: 0x000F6574
		private bool EmpireFarmerNarrativeOptionOnCondition(CharacterCreationManager characterCreationManager)
		{
			return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "empire";
		}

		// Token: 0x06003B1C RID: 15132 RVA: 0x000F8390 File Offset: 0x000F6590
		private void EmpireFarmerNarrativeOptionOnSelect(CharacterCreationManager characterCreationManager)
		{
			characterCreationManager.CharacterCreationContent.SetParentOccupation("farmer");
			string motherEquipmentId = this.GetMotherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
			string fatherEquipmentId = this.GetFatherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
			MBEquipmentRoster @object = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(motherEquipmentId);
			MBEquipmentRoster object2 = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(fatherEquipmentId);
			string motherAnimation = "act_character_creation_female_default_father_sitting";
			string fatherAnimation = "act_character_creation_male_default_father_sitting";
			this.UpdateParentEquipment(characterCreationManager, @object, object2, motherAnimation, fatherAnimation);
		}

		// Token: 0x06003B1D RID: 15133 RVA: 0x000F8430 File Offset: 0x000F6630
		private void GetEmpireArtisanNarrativeOptionArgs(NarrativeMenuOptionArgs args)
		{
			SkillObject[] affectedSkills = new SkillObject[]
			{
				DefaultSkills.Crafting,
				DefaultSkills.Crossbow
			};
			args.SetAffectedSkills(affectedSkills);
			args.SetFocusToSkills(this._focusToAdd);
			args.SetLevelToSkills(this._skillLevelToAdd);
			args.SetLevelToAttribute(DefaultCharacterAttributes.Intelligence, this._attributeLevelToAdd);
		}

		// Token: 0x06003B1E RID: 15134 RVA: 0x000F8484 File Offset: 0x000F6684
		private bool EmpireArtisanNarrativeOptionOnCondition(CharacterCreationManager characterCreationManager)
		{
			return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "empire";
		}

		// Token: 0x06003B1F RID: 15135 RVA: 0x000F84A0 File Offset: 0x000F66A0
		private void EmpireArtisanNarrativeOptionOnSelect(CharacterCreationManager characterCreationManager)
		{
			characterCreationManager.CharacterCreationContent.SetParentOccupation("artisan_urban");
			string motherEquipmentId = this.GetMotherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
			string fatherEquipmentId = this.GetFatherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
			MBEquipmentRoster @object = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(motherEquipmentId);
			MBEquipmentRoster object2 = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(fatherEquipmentId);
			string motherAnimation = "act_character_creation_female_default_side_to_side_2";
			string fatherAnimation = "act_character_creation_male_default_side_to_side_2";
			this.UpdateParentEquipment(characterCreationManager, @object, object2, motherAnimation, fatherAnimation);
		}

		// Token: 0x06003B20 RID: 15136 RVA: 0x000F8540 File Offset: 0x000F6740
		private void GetEmpireHunterNarrativeOptionArgs(NarrativeMenuOptionArgs args)
		{
			SkillObject[] affectedSkills = new SkillObject[]
			{
				DefaultSkills.Scouting,
				DefaultSkills.Bow
			};
			args.SetAffectedSkills(affectedSkills);
			args.SetFocusToSkills(this._focusToAdd);
			args.SetLevelToSkills(this._skillLevelToAdd);
			args.SetLevelToAttribute(DefaultCharacterAttributes.Control, this._attributeLevelToAdd);
		}

		// Token: 0x06003B21 RID: 15137 RVA: 0x000F8594 File Offset: 0x000F6794
		private bool EmpireHunterNarrativeOptionOnCondition(CharacterCreationManager characterCreationManager)
		{
			return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "empire";
		}

		// Token: 0x06003B22 RID: 15138 RVA: 0x000F85B0 File Offset: 0x000F67B0
		private void EmpireHunterNarrativeOptionOnSelect(CharacterCreationManager characterCreationManager)
		{
			characterCreationManager.CharacterCreationContent.SetParentOccupation("hunter");
			string motherEquipmentId = this.GetMotherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
			string fatherEquipmentId = this.GetFatherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
			MBEquipmentRoster @object = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(motherEquipmentId);
			MBEquipmentRoster object2 = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(fatherEquipmentId);
			string motherAnimation = "act_character_creation_female_default_side_to_side_3";
			string fatherAnimation = "act_character_creation_male_default_side_to_side_3";
			this.UpdateParentEquipment(characterCreationManager, @object, object2, motherAnimation, fatherAnimation);
		}

		// Token: 0x06003B23 RID: 15139 RVA: 0x000F8650 File Offset: 0x000F6850
		private void GetEmpireVagabondNarrativeOptionArgs(NarrativeMenuOptionArgs args)
		{
			SkillObject[] affectedSkills = new SkillObject[]
			{
				DefaultSkills.Roguery,
				DefaultSkills.Throwing
			};
			args.SetAffectedSkills(affectedSkills);
			args.SetFocusToSkills(this._focusToAdd);
			args.SetLevelToSkills(this._skillLevelToAdd);
			args.SetLevelToAttribute(DefaultCharacterAttributes.Cunning, this._attributeLevelToAdd);
		}

		// Token: 0x06003B24 RID: 15140 RVA: 0x000F86A4 File Offset: 0x000F68A4
		private bool EmpireVagabondNarrativeOptionOnCondition(CharacterCreationManager characterCreationManager)
		{
			return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "empire";
		}

		// Token: 0x06003B25 RID: 15141 RVA: 0x000F86C0 File Offset: 0x000F68C0
		private void EmpireVagabondNarrativeOptionOnSelect(CharacterCreationManager characterCreationManager)
		{
			characterCreationManager.CharacterCreationContent.SetParentOccupation("vagabond_urban");
			string motherEquipmentId = this.GetMotherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
			string fatherEquipmentId = this.GetFatherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
			MBEquipmentRoster @object = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(motherEquipmentId);
			MBEquipmentRoster object2 = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(fatherEquipmentId);
			string motherAnimation = "act_character_creation_female_default_hugging";
			string fatherAnimation = "act_character_creation_male_default_hugging";
			this.UpdateParentEquipment(characterCreationManager, @object, object2, motherAnimation, fatherAnimation);
		}

		// Token: 0x06003B26 RID: 15142 RVA: 0x000F8760 File Offset: 0x000F6960
		public void UpdateParentEquipment(CharacterCreationManager characterCreationManager, MBEquipmentRoster motherEquipment, MBEquipmentRoster fatherEquipment, string motherAnimation, string fatherAnimation)
		{
			foreach (NarrativeMenuCharacter narrativeMenuCharacter in characterCreationManager.CurrentMenu.Characters)
			{
				if (narrativeMenuCharacter.StringId.Equals("mother_character"))
				{
					narrativeMenuCharacter.SetEquipment(motherEquipment);
					narrativeMenuCharacter.SetAnimationId(motherAnimation);
				}
				if (narrativeMenuCharacter.StringId.Equals("father_character"))
				{
					narrativeMenuCharacter.SetEquipment(fatherEquipment);
					narrativeMenuCharacter.SetAnimationId(fatherAnimation);
				}
			}
		}

		// Token: 0x06003B27 RID: 15143 RVA: 0x000F87F4 File Offset: 0x000F69F4
		private void AddVlandianParentNarrativeMenuOptions(NarrativeMenu narrativeMenu)
		{
			NarrativeMenuOption narrativeMenuOption = new NarrativeMenuOption("vlandia_retainer_option", new TextObject("{=2TptWc4m}A baron's retainers", null), new TextObject("{=0Suu1Q9q}Your father was a bailiff for a local feudal magnate. He looked after his liege's estates, resolved disputes in the village, and helped train the village levy. He rode with the lord's cavalry, fighting as an armored knight.", null), new GetNarrativeMenuOptionArgsDelegate(this.GetVlandiaRetainerNarrativeOptionArgs), new NarrativeMenuOptionOnConditionDelegate(this.VlandiaRetainerNarrativeOptionOnCondition), new NarrativeMenuOptionOnSelectDelegate(this.VlandiaRetainerNarrativeOptionOnSelect), null);
			narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption);
			NarrativeMenuOption narrativeMenuOption2 = new NarrativeMenuOption("vlandia_merchant_option", new TextObject("{=651FhzdR}Urban merchants", null), new TextObject("{=qNZFkxJb}Your family were merchants in one of the main cities of the kingdom. They organized caravans to nearby towns and were active in the local merchant's guild.", null), new GetNarrativeMenuOptionArgsDelegate(this.GetVlandiaMerchantNarrativeOptionArgs), new NarrativeMenuOptionOnConditionDelegate(this.VlandiaMerchantNarrativeOptionOnCondition), new NarrativeMenuOptionOnSelectDelegate(this.VlandiaMerchantNarrativeOptionOnSelect), null);
			narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption2);
			NarrativeMenuOption narrativeMenuOption3 = new NarrativeMenuOption("vlandia_farmer_option", new TextObject("{=RDfXuVxT}Yeomen", null), new TextObject("{=BLZ4mdhb}Your family were small farmers with just enough land to feed themselves and make a small profit. People like them were the pillars of the kingdom's economy, as well as the backbone of the levy.", null), new GetNarrativeMenuOptionArgsDelegate(this.GetVlandiaFarmerNarrativeOptionArgs), new NarrativeMenuOptionOnConditionDelegate(this.VlandiaFarmerNarrativeOptionOnCondition), new NarrativeMenuOptionOnSelectDelegate(this.VlandiaFarmerNarrativeOptionOnSelect), null);
			narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption3);
			NarrativeMenuOption narrativeMenuOption4 = new NarrativeMenuOption("vlandia_blacksmith_option", new TextObject("{=p2KIhGbE}Urban blacksmith", null), new TextObject("{=btsMpRcA}Your family owned a smithy in a city. Your father played an active if minor role in the town council, and also served in the militia.", null), new GetNarrativeMenuOptionArgsDelegate(this.GetVlandiaBlacksmithNarrativeOptionArgs), new NarrativeMenuOptionOnConditionDelegate(this.VlandiaBlacksmithNarrativeOptionOnCondition), new NarrativeMenuOptionOnSelectDelegate(this.VlandiaBlacksmithNarrativeOptionOnSelect), null);
			narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption4);
			NarrativeMenuOption narrativeMenuOption5 = new NarrativeMenuOption("vlandia_hunter_option", new TextObject("{=YcnK0Thk}Hunters", null), new TextObject("{=yRFSzSDZ}Your family lived in a village, but did not own their own land. Instead, your father supplemented paid jobs with long trips in the woods, hunting and trapping, always keeping a wary eye for the lord's game wardens.", null), new GetNarrativeMenuOptionArgsDelegate(this.GetVlandiaHunterNarrativeOptionArgs), new NarrativeMenuOptionOnConditionDelegate(this.VlandiaHunterNarrativeOptionOnCondition), new NarrativeMenuOptionOnSelectDelegate(this.VlandiaHunterNarrativeOptionOnSelect), null);
			narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption5);
			NarrativeMenuOption narrativeMenuOption6 = new NarrativeMenuOption("vlandia_mercenary_option", new TextObject("{=ipQP6aVi}Mercenaries", null), new TextObject("{=yYhX6JQC}Your father joined one of Vlandia's many mercenary companies, composed of men who got such a taste for war in their lord's service that they never took well to peace. Their crossbowmen were much valued across Calradia. Your mother was a camp follower, taking you along in the wake of bloody campaigns.", null), new GetNarrativeMenuOptionArgsDelegate(this.GetVlandiaMercenaryNarrativeOptionArgs), new NarrativeMenuOptionOnConditionDelegate(this.VlandiaMercenaryNarrativeOptionOnCondition), new NarrativeMenuOptionOnSelectDelegate(this.VlandiaMercenaryNarrativeOptionOnSelect), null);
			narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption6);
		}

		// Token: 0x06003B28 RID: 15144 RVA: 0x000F89D4 File Offset: 0x000F6BD4
		private void GetVlandiaRetainerNarrativeOptionArgs(NarrativeMenuOptionArgs args)
		{
			SkillObject[] affectedSkills = new SkillObject[]
			{
				DefaultSkills.Riding,
				DefaultSkills.Polearm
			};
			args.SetAffectedSkills(affectedSkills);
			args.SetFocusToSkills(this._focusToAdd);
			args.SetLevelToSkills(this._skillLevelToAdd);
			args.SetLevelToAttribute(DefaultCharacterAttributes.Social, this._attributeLevelToAdd);
		}

		// Token: 0x06003B29 RID: 15145 RVA: 0x000F8A28 File Offset: 0x000F6C28
		private bool VlandiaRetainerNarrativeOptionOnCondition(CharacterCreationManager characterCreationManager)
		{
			return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "vlandia";
		}

		// Token: 0x06003B2A RID: 15146 RVA: 0x000F8A44 File Offset: 0x000F6C44
		private void VlandiaRetainerNarrativeOptionOnSelect(CharacterCreationManager characterCreationManager)
		{
			characterCreationManager.CharacterCreationContent.SetParentOccupation("retainer");
			string motherEquipmentId = this.GetMotherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
			string fatherEquipmentId = this.GetFatherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
			MBEquipmentRoster @object = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(motherEquipmentId);
			MBEquipmentRoster object2 = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(fatherEquipmentId);
			string motherAnimation = "act_character_creation_female_default_side_to_side_1";
			string fatherAnimation = "act_character_creation_male_default_side_to_side_1";
			this.UpdateParentEquipment(characterCreationManager, @object, object2, motherAnimation, fatherAnimation);
		}

		// Token: 0x06003B2B RID: 15147 RVA: 0x000F8AE4 File Offset: 0x000F6CE4
		private void GetVlandiaMerchantNarrativeOptionArgs(NarrativeMenuOptionArgs args)
		{
			SkillObject[] affectedSkills = new SkillObject[]
			{
				DefaultSkills.Trade,
				DefaultSkills.Charm
			};
			args.SetAffectedSkills(affectedSkills);
			args.SetFocusToSkills(this._focusToAdd);
			args.SetLevelToSkills(this._skillLevelToAdd);
			args.SetLevelToAttribute(DefaultCharacterAttributes.Intelligence, this._attributeLevelToAdd);
		}

		// Token: 0x06003B2C RID: 15148 RVA: 0x000F8B38 File Offset: 0x000F6D38
		private bool VlandiaMerchantNarrativeOptionOnCondition(CharacterCreationManager characterCreationManager)
		{
			return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "vlandia";
		}

		// Token: 0x06003B2D RID: 15149 RVA: 0x000F8B54 File Offset: 0x000F6D54
		private void VlandiaMerchantNarrativeOptionOnSelect(CharacterCreationManager characterCreationManager)
		{
			characterCreationManager.CharacterCreationContent.SetParentOccupation("merchant_urban");
			string motherEquipmentId = this.GetMotherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
			string fatherEquipmentId = this.GetFatherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
			MBEquipmentRoster @object = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(motherEquipmentId);
			MBEquipmentRoster object2 = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(fatherEquipmentId);
			string motherAnimation = "act_character_creation_female_default_mother_front";
			string fatherAnimation = "act_character_creation_male_default_mother_front";
			this.UpdateParentEquipment(characterCreationManager, @object, object2, motherAnimation, fatherAnimation);
		}

		// Token: 0x06003B2E RID: 15150 RVA: 0x000F8BF4 File Offset: 0x000F6DF4
		private void GetVlandiaFarmerNarrativeOptionArgs(NarrativeMenuOptionArgs args)
		{
			SkillObject[] affectedSkills = new SkillObject[]
			{
				DefaultSkills.Polearm,
				DefaultSkills.Crossbow
			};
			args.SetAffectedSkills(affectedSkills);
			args.SetFocusToSkills(this._focusToAdd);
			args.SetLevelToSkills(this._skillLevelToAdd);
			args.SetLevelToAttribute(DefaultCharacterAttributes.Endurance, this._attributeLevelToAdd);
		}

		// Token: 0x06003B2F RID: 15151 RVA: 0x000F8C48 File Offset: 0x000F6E48
		private bool VlandiaFarmerNarrativeOptionOnCondition(CharacterCreationManager characterCreationManager)
		{
			return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "vlandia";
		}

		// Token: 0x06003B30 RID: 15152 RVA: 0x000F8C64 File Offset: 0x000F6E64
		private void VlandiaFarmerNarrativeOptionOnSelect(CharacterCreationManager characterCreationManager)
		{
			characterCreationManager.CharacterCreationContent.SetParentOccupation("farmer");
			string motherEquipmentId = this.GetMotherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
			string fatherEquipmentId = this.GetFatherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
			MBEquipmentRoster @object = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(motherEquipmentId);
			MBEquipmentRoster object2 = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(fatherEquipmentId);
			string motherAnimation = "act_character_creation_female_default_father_sitting";
			string fatherAnimation = "act_character_creation_male_default_father_sitting";
			this.UpdateParentEquipment(characterCreationManager, @object, object2, motherAnimation, fatherAnimation);
		}

		// Token: 0x06003B31 RID: 15153 RVA: 0x000F8D04 File Offset: 0x000F6F04
		private void GetVlandiaBlacksmithNarrativeOptionArgs(NarrativeMenuOptionArgs args)
		{
			SkillObject[] affectedSkills = new SkillObject[]
			{
				DefaultSkills.Crafting,
				DefaultSkills.TwoHanded
			};
			args.SetAffectedSkills(affectedSkills);
			args.SetFocusToSkills(this._focusToAdd);
			args.SetLevelToSkills(this._skillLevelToAdd);
			args.SetLevelToAttribute(DefaultCharacterAttributes.Vigor, this._attributeLevelToAdd);
		}

		// Token: 0x06003B32 RID: 15154 RVA: 0x000F8D58 File Offset: 0x000F6F58
		private bool VlandiaBlacksmithNarrativeOptionOnCondition(CharacterCreationManager characterCreationManager)
		{
			return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "vlandia";
		}

		// Token: 0x06003B33 RID: 15155 RVA: 0x000F8D74 File Offset: 0x000F6F74
		private void VlandiaBlacksmithNarrativeOptionOnSelect(CharacterCreationManager characterCreationManager)
		{
			characterCreationManager.CharacterCreationContent.SetParentOccupation("artisan_urban");
			string motherEquipmentId = this.GetMotherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
			string fatherEquipmentId = this.GetFatherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
			MBEquipmentRoster @object = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(motherEquipmentId);
			MBEquipmentRoster object2 = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(fatherEquipmentId);
			string motherAnimation = "act_character_creation_female_default_side_to_side_2";
			string fatherAnimation = "act_character_creation_male_default_side_to_side_2";
			this.UpdateParentEquipment(characterCreationManager, @object, object2, motherAnimation, fatherAnimation);
		}

		// Token: 0x06003B34 RID: 15156 RVA: 0x000F8E14 File Offset: 0x000F7014
		private void GetVlandiaHunterNarrativeOptionArgs(NarrativeMenuOptionArgs args)
		{
			SkillObject[] affectedSkills = new SkillObject[]
			{
				DefaultSkills.Scouting,
				DefaultSkills.Crossbow
			};
			args.SetAffectedSkills(affectedSkills);
			args.SetFocusToSkills(this._focusToAdd);
			args.SetLevelToSkills(this._skillLevelToAdd);
			args.SetLevelToAttribute(DefaultCharacterAttributes.Control, this._attributeLevelToAdd);
		}

		// Token: 0x06003B35 RID: 15157 RVA: 0x000F8E68 File Offset: 0x000F7068
		private bool VlandiaHunterNarrativeOptionOnCondition(CharacterCreationManager characterCreationManager)
		{
			return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "vlandia";
		}

		// Token: 0x06003B36 RID: 15158 RVA: 0x000F8E84 File Offset: 0x000F7084
		private void VlandiaHunterNarrativeOptionOnSelect(CharacterCreationManager characterCreationManager)
		{
			characterCreationManager.CharacterCreationContent.SetParentOccupation("hunter");
			string motherEquipmentId = this.GetMotherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
			string fatherEquipmentId = this.GetFatherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
			MBEquipmentRoster @object = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(motherEquipmentId);
			MBEquipmentRoster object2 = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(fatherEquipmentId);
			string motherAnimation = "act_character_creation_female_default_side_to_side_3";
			string fatherAnimation = "act_character_creation_male_default_side_to_side_3";
			this.UpdateParentEquipment(characterCreationManager, @object, object2, motherAnimation, fatherAnimation);
		}

		// Token: 0x06003B37 RID: 15159 RVA: 0x000F8F24 File Offset: 0x000F7124
		private void GetVlandiaMercenaryNarrativeOptionArgs(NarrativeMenuOptionArgs args)
		{
			SkillObject[] affectedSkills = new SkillObject[]
			{
				DefaultSkills.Roguery,
				DefaultSkills.Crossbow
			};
			args.SetAffectedSkills(affectedSkills);
			args.SetFocusToSkills(this._focusToAdd);
			args.SetLevelToSkills(this._skillLevelToAdd);
			args.SetLevelToAttribute(DefaultCharacterAttributes.Cunning, this._attributeLevelToAdd);
		}

		// Token: 0x06003B38 RID: 15160 RVA: 0x000F8F78 File Offset: 0x000F7178
		private bool VlandiaMercenaryNarrativeOptionOnCondition(CharacterCreationManager characterCreationManager)
		{
			return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "vlandia";
		}

		// Token: 0x06003B39 RID: 15161 RVA: 0x000F8F94 File Offset: 0x000F7194
		private void VlandiaMercenaryNarrativeOptionOnSelect(CharacterCreationManager characterCreationManager)
		{
			characterCreationManager.CharacterCreationContent.SetParentOccupation("merchant_urban");
			string motherEquipmentId = this.GetMotherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
			string fatherEquipmentId = this.GetFatherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
			MBEquipmentRoster @object = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(motherEquipmentId);
			MBEquipmentRoster object2 = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(fatherEquipmentId);
			string motherAnimation = "act_character_creation_female_default_hugging";
			string fatherAnimation = "act_character_creation_male_default_hugging";
			this.UpdateParentEquipment(characterCreationManager, @object, object2, motherAnimation, fatherAnimation);
		}

		// Token: 0x06003B3A RID: 15162 RVA: 0x000F9034 File Offset: 0x000F7234
		private void AddSturgianParentNarrativeMenuOptions(NarrativeMenu narrativeMenu)
		{
			NarrativeMenuOption narrativeMenuOption = new NarrativeMenuOption("sturgia_companion_option", new TextObject("{=mc78FEbA}A boyar's companions", null), new TextObject("{=hob3WVkU}Your father was a member of a boyar's druzhina, the 'companions' that make up his retinue. He sat at his lord's table in the great hall, oversaw the boyar's estates, and stood by his side in the center of the shield wall in battle.", null), new GetNarrativeMenuOptionArgsDelegate(this.GetSturgiaCompanionNarrativeOptionArgs), new NarrativeMenuOptionOnConditionDelegate(this.SturgiaCompanionNarrativeOptionOnCondition), new NarrativeMenuOptionOnSelectDelegate(this.SturgiaCompanionNarrativeOptionOnSelect), null);
			narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption);
			NarrativeMenuOption narrativeMenuOption2 = new NarrativeMenuOption("sturgia_trader_option", new TextObject("{=HqzVBfpl}Urban traders", null), new TextObject("{=bjVMtW3W}Your family were merchants who lived in one of Sturgia's great river ports, organizing the shipment of the north's bounty of furs, honey and other goods to faraway lands.", null), new GetNarrativeMenuOptionArgsDelegate(this.GetSturgiaTraderNarrativeOptionArgs), new NarrativeMenuOptionOnConditionDelegate(this.SturgiaTraderNarrativeOptionOnCondition), new NarrativeMenuOptionOnSelectDelegate(this.SturgiaTraderNarrativeOptionOnSelect), null);
			narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption2);
			NarrativeMenuOption narrativeMenuOption3 = new NarrativeMenuOption("sturgia_farmer_option", new TextObject("{=zrpqSWSh}Free farmers", null), new TextObject("{=Mcd3ZyKq}Your family had just enough land to feed themselves and make a small profit. People like them were the pillars of the kingdom's economy, as well as the backbone of the levy.", null), new GetNarrativeMenuOptionArgsDelegate(this.GetSturgiaFarmerNarrativeOptionArgs), new NarrativeMenuOptionOnConditionDelegate(this.SturgiaFarmerNarrativeOptionOnCondition), new NarrativeMenuOptionOnSelectDelegate(this.SturgiaFarmerNarrativeOptionOnSelect), null);
			narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption3);
			NarrativeMenuOption narrativeMenuOption4 = new NarrativeMenuOption("sturgia_artisan_option", new TextObject("{=v48N6h1t}Urban artisans", null), new TextObject("{=ueCm5y1C}Your family owned their own workshop in a city, making goods from raw materials brought in from the countryside. Your father played an active if minor role in the town council, and also served in the militia.", null), new GetNarrativeMenuOptionArgsDelegate(this.GetSturgiaArtisanNarrativeOptionArgs), new NarrativeMenuOptionOnConditionDelegate(this.SturgiaArtisanNarrativeOptionOnCondition), new NarrativeMenuOptionOnSelectDelegate(this.SturgiaArtisanNarrativeOptionOnSelect), null);
			narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption4);
			NarrativeMenuOption narrativeMenuOption5 = new NarrativeMenuOption("sturgia_hunter_option", new TextObject("{=YcnK0Thk}Hunters", null), new TextObject("{=WyZ2UtFF}Your family had no taste for the authority of the boyars. They made their living deep in the woods, slashing and burning fields which they tended for a year or two before moving on. They hunted and trapped fox, hare, ermine, and other fur-bearing animals.", null), new GetNarrativeMenuOptionArgsDelegate(this.GetSturgiaHunterNarrativeOptionArgs), new NarrativeMenuOptionOnConditionDelegate(this.SturgiaHunterNarrativeOptionOnCondition), new NarrativeMenuOptionOnSelectDelegate(this.SturgiaHunterNarrativeOptionOnSelect), null);
			narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption5);
			NarrativeMenuOption narrativeMenuOption6 = new NarrativeMenuOption("sturgia_vagabond_option", new TextObject("{=TPoK3GSj}Vagabonds", null), new TextObject("{=2SDWhGmQ}Your family numbered among the poor migrants living in the slums that grow up outside the walls of the river cities, making whatever money they could from a variety of odd jobs. Sometimes they did services for one of the region's many criminal gangs.", null), new GetNarrativeMenuOptionArgsDelegate(this.GetSturgiaVagabondNarrativeOptionArgs), new NarrativeMenuOptionOnConditionDelegate(this.SturgiaVagabondNarrativeOptionOnCondition), new NarrativeMenuOptionOnSelectDelegate(this.SturgiaVagabondNarrativeOptionOnSelect), null);
			narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption6);
		}

		// Token: 0x06003B3B RID: 15163 RVA: 0x000F9214 File Offset: 0x000F7414
		private void GetSturgiaCompanionNarrativeOptionArgs(NarrativeMenuOptionArgs args)
		{
			SkillObject[] affectedSkills = new SkillObject[]
			{
				DefaultSkills.Riding,
				DefaultSkills.TwoHanded
			};
			args.SetAffectedSkills(affectedSkills);
			args.SetFocusToSkills(this._focusToAdd);
			args.SetLevelToSkills(this._skillLevelToAdd);
			args.SetLevelToAttribute(DefaultCharacterAttributes.Social, this._attributeLevelToAdd);
		}

		// Token: 0x06003B3C RID: 15164 RVA: 0x000F9268 File Offset: 0x000F7468
		private bool SturgiaCompanionNarrativeOptionOnCondition(CharacterCreationManager characterCreationManager)
		{
			return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "sturgia";
		}

		// Token: 0x06003B3D RID: 15165 RVA: 0x000F9284 File Offset: 0x000F7484
		private void SturgiaCompanionNarrativeOptionOnSelect(CharacterCreationManager characterCreationManager)
		{
			characterCreationManager.CharacterCreationContent.SetParentOccupation("retainer");
			string motherEquipmentId = this.GetMotherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
			string fatherEquipmentId = this.GetFatherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
			MBEquipmentRoster @object = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(motherEquipmentId);
			MBEquipmentRoster object2 = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(fatherEquipmentId);
			string motherAnimation = "act_character_creation_female_default_side_to_side_1";
			string fatherAnimation = "act_character_creation_male_default_side_to_side_1";
			this.UpdateParentEquipment(characterCreationManager, @object, object2, motherAnimation, fatherAnimation);
		}

		// Token: 0x06003B3E RID: 15166 RVA: 0x000F9324 File Offset: 0x000F7524
		private void GetSturgiaTraderNarrativeOptionArgs(NarrativeMenuOptionArgs args)
		{
			SkillObject[] affectedSkills = new SkillObject[]
			{
				DefaultSkills.Trade,
				DefaultSkills.Tactics
			};
			args.SetAffectedSkills(affectedSkills);
			args.SetFocusToSkills(this._focusToAdd);
			args.SetLevelToSkills(this._skillLevelToAdd);
			args.SetLevelToAttribute(DefaultCharacterAttributes.Cunning, this._attributeLevelToAdd);
		}

		// Token: 0x06003B3F RID: 15167 RVA: 0x000F9378 File Offset: 0x000F7578
		private bool SturgiaTraderNarrativeOptionOnCondition(CharacterCreationManager characterCreationManager)
		{
			return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "sturgia";
		}

		// Token: 0x06003B40 RID: 15168 RVA: 0x000F9394 File Offset: 0x000F7594
		private void SturgiaTraderNarrativeOptionOnSelect(CharacterCreationManager characterCreationManager)
		{
			characterCreationManager.CharacterCreationContent.SetParentOccupation("merchant_urban");
			string motherEquipmentId = this.GetMotherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
			string fatherEquipmentId = this.GetFatherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
			MBEquipmentRoster @object = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(motherEquipmentId);
			MBEquipmentRoster object2 = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(fatherEquipmentId);
			string motherAnimation = "act_character_creation_female_default_mother_front";
			string fatherAnimation = "act_character_creation_male_default_mother_front";
			this.UpdateParentEquipment(characterCreationManager, @object, object2, motherAnimation, fatherAnimation);
		}

		// Token: 0x06003B41 RID: 15169 RVA: 0x000F9434 File Offset: 0x000F7634
		private void GetSturgiaFarmerNarrativeOptionArgs(NarrativeMenuOptionArgs args)
		{
			SkillObject[] affectedSkills = new SkillObject[]
			{
				DefaultSkills.Athletics,
				DefaultSkills.Polearm
			};
			args.SetAffectedSkills(affectedSkills);
			args.SetFocusToSkills(this._focusToAdd);
			args.SetLevelToSkills(this._skillLevelToAdd);
			args.SetLevelToAttribute(DefaultCharacterAttributes.Endurance, this._attributeLevelToAdd);
		}

		// Token: 0x06003B42 RID: 15170 RVA: 0x000F9488 File Offset: 0x000F7688
		private bool SturgiaFarmerNarrativeOptionOnCondition(CharacterCreationManager characterCreationManager)
		{
			return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "sturgia";
		}

		// Token: 0x06003B43 RID: 15171 RVA: 0x000F94A4 File Offset: 0x000F76A4
		private void SturgiaFarmerNarrativeOptionOnSelect(CharacterCreationManager characterCreationManager)
		{
			characterCreationManager.CharacterCreationContent.SetParentOccupation("farmer");
			string motherEquipmentId = this.GetMotherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
			string fatherEquipmentId = this.GetFatherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
			MBEquipmentRoster @object = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(motherEquipmentId);
			MBEquipmentRoster object2 = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(fatherEquipmentId);
			string motherAnimation = "act_character_creation_female_default_father_sitting";
			string fatherAnimation = "act_character_creation_male_default_father_sitting";
			this.UpdateParentEquipment(characterCreationManager, @object, object2, motherAnimation, fatherAnimation);
		}

		// Token: 0x06003B44 RID: 15172 RVA: 0x000F9544 File Offset: 0x000F7744
		private void GetSturgiaArtisanNarrativeOptionArgs(NarrativeMenuOptionArgs args)
		{
			SkillObject[] affectedSkills = new SkillObject[]
			{
				DefaultSkills.Crafting,
				DefaultSkills.OneHanded
			};
			args.SetAffectedSkills(affectedSkills);
			args.SetFocusToSkills(this._focusToAdd);
			args.SetLevelToSkills(this._skillLevelToAdd);
			args.SetLevelToAttribute(DefaultCharacterAttributes.Intelligence, this._attributeLevelToAdd);
		}

		// Token: 0x06003B45 RID: 15173 RVA: 0x000F9598 File Offset: 0x000F7798
		private bool SturgiaArtisanNarrativeOptionOnCondition(CharacterCreationManager characterCreationManager)
		{
			return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "sturgia";
		}

		// Token: 0x06003B46 RID: 15174 RVA: 0x000F95B4 File Offset: 0x000F77B4
		private void SturgiaArtisanNarrativeOptionOnSelect(CharacterCreationManager characterCreationManager)
		{
			characterCreationManager.CharacterCreationContent.SetParentOccupation("artisan_urban");
			string motherEquipmentId = this.GetMotherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
			string fatherEquipmentId = this.GetFatherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
			MBEquipmentRoster @object = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(motherEquipmentId);
			MBEquipmentRoster object2 = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(fatherEquipmentId);
			string motherAnimation = "act_character_creation_female_default_side_to_side_2";
			string fatherAnimation = "act_character_creation_male_default_side_to_side_2";
			this.UpdateParentEquipment(characterCreationManager, @object, object2, motherAnimation, fatherAnimation);
		}

		// Token: 0x06003B47 RID: 15175 RVA: 0x000F9654 File Offset: 0x000F7854
		private void GetSturgiaHunterNarrativeOptionArgs(NarrativeMenuOptionArgs args)
		{
			SkillObject[] affectedSkills = new SkillObject[]
			{
				DefaultSkills.Scouting,
				DefaultSkills.Bow
			};
			args.SetAffectedSkills(affectedSkills);
			args.SetFocusToSkills(this._focusToAdd);
			args.SetLevelToSkills(this._skillLevelToAdd);
			args.SetLevelToAttribute(DefaultCharacterAttributes.Vigor, this._attributeLevelToAdd);
		}

		// Token: 0x06003B48 RID: 15176 RVA: 0x000F96A8 File Offset: 0x000F78A8
		private bool SturgiaHunterNarrativeOptionOnCondition(CharacterCreationManager characterCreationManager)
		{
			return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "sturgia";
		}

		// Token: 0x06003B49 RID: 15177 RVA: 0x000F96C4 File Offset: 0x000F78C4
		private void SturgiaHunterNarrativeOptionOnSelect(CharacterCreationManager characterCreationManager)
		{
			characterCreationManager.CharacterCreationContent.SetParentOccupation("hunter");
			string motherEquipmentId = this.GetMotherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
			string fatherEquipmentId = this.GetFatherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
			MBEquipmentRoster @object = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(motherEquipmentId);
			MBEquipmentRoster object2 = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(fatherEquipmentId);
			string motherAnimation = "act_character_creation_female_default_side_to_side_3";
			string fatherAnimation = "act_character_creation_male_default_side_to_side_3";
			this.UpdateParentEquipment(characterCreationManager, @object, object2, motherAnimation, fatherAnimation);
		}

		// Token: 0x06003B4A RID: 15178 RVA: 0x000F9764 File Offset: 0x000F7964
		private void GetSturgiaVagabondNarrativeOptionArgs(NarrativeMenuOptionArgs args)
		{
			SkillObject[] affectedSkills = new SkillObject[]
			{
				DefaultSkills.Roguery,
				DefaultSkills.Throwing
			};
			args.SetAffectedSkills(affectedSkills);
			args.SetFocusToSkills(this._focusToAdd);
			args.SetLevelToSkills(this._skillLevelToAdd);
			args.SetLevelToAttribute(DefaultCharacterAttributes.Control, this._attributeLevelToAdd);
		}

		// Token: 0x06003B4B RID: 15179 RVA: 0x000F97B8 File Offset: 0x000F79B8
		private bool SturgiaVagabondNarrativeOptionOnCondition(CharacterCreationManager characterCreationManager)
		{
			return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "sturgia";
		}

		// Token: 0x06003B4C RID: 15180 RVA: 0x000F97D4 File Offset: 0x000F79D4
		private void SturgiaVagabondNarrativeOptionOnSelect(CharacterCreationManager characterCreationManager)
		{
			characterCreationManager.CharacterCreationContent.SetParentOccupation("vagabond_urban");
			string motherEquipmentId = this.GetMotherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
			string fatherEquipmentId = this.GetFatherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
			MBEquipmentRoster @object = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(motherEquipmentId);
			MBEquipmentRoster object2 = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(fatherEquipmentId);
			string motherAnimation = "act_character_creation_female_default_hugging";
			string fatherAnimation = "act_character_creation_male_default_hugging";
			this.UpdateParentEquipment(characterCreationManager, @object, object2, motherAnimation, fatherAnimation);
		}

		// Token: 0x06003B4D RID: 15181 RVA: 0x000F9874 File Offset: 0x000F7A74
		private void AddAseraiParentNarrativeMenuOptions(NarrativeMenu narrativeMenu)
		{
			NarrativeMenuOption narrativeMenuOption = new NarrativeMenuOption("aserai_kinsfolk_option", new TextObject("{=Sw8OxnNr}Kinsfolk of an emir", null), new TextObject("{=MFrIHJZM}Your family was from a smaller offshoot of an emir's tribe. Your father's land gave him enough income to afford a horse but he was not quite wealthy enough to buy the armor needed to join the heavier cavalry. He fought as one of the light horsemen for which the desert is famous.", null), new GetNarrativeMenuOptionArgsDelegate(this.GetAseraiKinsfolkNarrativeOptionArgs), new NarrativeMenuOptionOnConditionDelegate(this.AseraiKinsfolkNarrativeOptionOnCondition), new NarrativeMenuOptionOnSelectDelegate(this.AseraiKinsfolkNarrativeOptionOnSelect), null);
			narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption);
			NarrativeMenuOption narrativeMenuOption2 = new NarrativeMenuOption("aserai_slave_option", new TextObject("{=ngFVgwDD}Warrior-slaves", null), new TextObject("{=GsPC2MgU}Your father was part of one of the slave-bodyguards maintained by the Aserai emirs. He fought by his master's side with tribe's armored cavalry, and was freed - perhaps for an act of valor, or perhaps he paid for his freedom with his share of the spoils of battle. He then married your mother.", null), new GetNarrativeMenuOptionArgsDelegate(this.GetAseraiSlaveNarrativeOptionArgs), new NarrativeMenuOptionOnConditionDelegate(this.AseraiSlaveNarrativeOptionOnCondition), new NarrativeMenuOptionOnSelectDelegate(this.AseraiSlaveNarrativeOptionOnSelect), null);
			narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption2);
			NarrativeMenuOption narrativeMenuOption3 = new NarrativeMenuOption("aserai_physician_option", new TextObject("{=bgy8LVvY}Physician", null), new TextObject("{=BhQlmQoj}Your family were respected physicians in an oasis town. They set bones and cured the sick, and their skills were in much demand. They were respected in the higher echelons of society too.", null), new GetNarrativeMenuOptionArgsDelegate(this.GetAseraiPhysicianNarrativeOptionArgs), new NarrativeMenuOptionOnConditionDelegate(this.AseraiPhysicianNarrativeOptionOnCondition), new NarrativeMenuOptionOnSelectDelegate(this.AseraiPhysicianNarrativeOptionOnSelect), null);
			narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption3);
			NarrativeMenuOption narrativeMenuOption4 = new NarrativeMenuOption("aserai_farmer_option", new TextObject("{=g31pXuqi}Oasis farmers", null), new TextObject("{=5P0KqBAw}Your family tilled the soil in one of the oases of the Nahasa and tended the palm orchards that produced the desert's famous dates. Your father was a member of the main foot levy of his tribe, fighting with his kinsmen under the emir's banner.", null), new GetNarrativeMenuOptionArgsDelegate(this.GetAseraiFarmerNarrativeOptionArgs), new NarrativeMenuOptionOnConditionDelegate(this.AseraiFarmerNarrativeOptionOnCondition), new NarrativeMenuOptionOnSelectDelegate(this.AseraiFarmerNarrativeOptionOnSelect), null);
			narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption4);
			NarrativeMenuOption narrativeMenuOption5 = new NarrativeMenuOption("aserai_herder_option", new TextObject("{=EEedqolz}Bedouin", null), new TextObject("{=PKhcPbBX}Your family were part of a nomadic clan, crisscrossing the wastes between wadi beds and wells to feed their herds of goats and camels on the scraggly scrubs of the Nahasa.", null), new GetNarrativeMenuOptionArgsDelegate(this.GetAseraiHerderNarrativeOptionArgs), new NarrativeMenuOptionOnConditionDelegate(this.AseraiHerderNarrativeOptionOnCondition), new NarrativeMenuOptionOnSelectDelegate(this.AseraiHerderNarrativeOptionOnSelect), null);
			narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption5);
			NarrativeMenuOption narrativeMenuOption6 = new NarrativeMenuOption("aserai_artisan_option", new TextObject("{=tRIrbTvv}Urban back-alley thugs", null), new TextObject("{=6bUSbsKC}Your father worked for a fitiwi, one of the strongmen who keep order in the poorer quarters of the oasis towns. He resolved disputes over land, dice and insults, imposing his authority with the fitiwi's traditional staff.", null), new GetNarrativeMenuOptionArgsDelegate(this.GetAseraiArtisanNarrativeOptionArgs), new NarrativeMenuOptionOnConditionDelegate(this.AseraiArtisanNarrativeOptionOnCondition), new NarrativeMenuOptionOnSelectDelegate(this.AseraiArtisanNarrativeOptionOnSelect), null);
			narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption6);
		}

		// Token: 0x06003B4E RID: 15182 RVA: 0x000F9A54 File Offset: 0x000F7C54
		private void GetAseraiKinsfolkNarrativeOptionArgs(NarrativeMenuOptionArgs args)
		{
			SkillObject[] affectedSkills = new SkillObject[]
			{
				DefaultSkills.Riding,
				DefaultSkills.Throwing
			};
			args.SetAffectedSkills(affectedSkills);
			args.SetFocusToSkills(this._focusToAdd);
			args.SetLevelToSkills(this._skillLevelToAdd);
			args.SetLevelToAttribute(DefaultCharacterAttributes.Social, this._attributeLevelToAdd);
		}

		// Token: 0x06003B4F RID: 15183 RVA: 0x000F9AA8 File Offset: 0x000F7CA8
		private bool AseraiKinsfolkNarrativeOptionOnCondition(CharacterCreationManager characterCreationManager)
		{
			return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "aserai";
		}

		// Token: 0x06003B50 RID: 15184 RVA: 0x000F9AC4 File Offset: 0x000F7CC4
		private void AseraiKinsfolkNarrativeOptionOnSelect(CharacterCreationManager characterCreationManager)
		{
			characterCreationManager.CharacterCreationContent.SetParentOccupation("retainer");
			string motherEquipmentId = this.GetMotherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
			string fatherEquipmentId = this.GetFatherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
			MBEquipmentRoster @object = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(motherEquipmentId);
			MBEquipmentRoster object2 = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(fatherEquipmentId);
			string motherAnimation = "act_character_creation_female_default_side_to_side_1";
			string fatherAnimation = "act_character_creation_male_default_side_to_side_1";
			this.UpdateParentEquipment(characterCreationManager, @object, object2, motherAnimation, fatherAnimation);
		}

		// Token: 0x06003B51 RID: 15185 RVA: 0x000F9B64 File Offset: 0x000F7D64
		private void GetAseraiSlaveNarrativeOptionArgs(NarrativeMenuOptionArgs args)
		{
			SkillObject[] affectedSkills = new SkillObject[]
			{
				DefaultSkills.Riding,
				DefaultSkills.Polearm
			};
			args.SetAffectedSkills(affectedSkills);
			args.SetFocusToSkills(this._focusToAdd);
			args.SetLevelToSkills(this._skillLevelToAdd);
			args.SetLevelToAttribute(DefaultCharacterAttributes.Vigor, this._attributeLevelToAdd);
		}

		// Token: 0x06003B52 RID: 15186 RVA: 0x000F9BB8 File Offset: 0x000F7DB8
		private bool AseraiSlaveNarrativeOptionOnCondition(CharacterCreationManager characterCreationManager)
		{
			return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "aserai";
		}

		// Token: 0x06003B53 RID: 15187 RVA: 0x000F9BD4 File Offset: 0x000F7DD4
		private void AseraiSlaveNarrativeOptionOnSelect(CharacterCreationManager characterCreationManager)
		{
			characterCreationManager.CharacterCreationContent.SetParentOccupation("mercenary_urban");
			string motherEquipmentId = this.GetMotherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
			string fatherEquipmentId = this.GetFatherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
			MBEquipmentRoster @object = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(motherEquipmentId);
			MBEquipmentRoster object2 = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(fatherEquipmentId);
			string motherAnimation = "act_character_creation_female_default_mother_front";
			string fatherAnimation = "act_character_creation_male_default_mother_front";
			this.UpdateParentEquipment(characterCreationManager, @object, object2, motherAnimation, fatherAnimation);
		}

		// Token: 0x06003B54 RID: 15188 RVA: 0x000F9C74 File Offset: 0x000F7E74
		private void GetAseraiPhysicianNarrativeOptionArgs(NarrativeMenuOptionArgs args)
		{
			SkillObject[] affectedSkills = new SkillObject[]
			{
				DefaultSkills.Medicine,
				DefaultSkills.Charm
			};
			args.SetAffectedSkills(affectedSkills);
			args.SetFocusToSkills(this._focusToAdd);
			args.SetLevelToSkills(this._skillLevelToAdd);
			args.SetLevelToAttribute(DefaultCharacterAttributes.Intelligence, this._attributeLevelToAdd);
		}

		// Token: 0x06003B55 RID: 15189 RVA: 0x000F9CC8 File Offset: 0x000F7EC8
		private bool AseraiPhysicianNarrativeOptionOnCondition(CharacterCreationManager characterCreationManager)
		{
			return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "aserai";
		}

		// Token: 0x06003B56 RID: 15190 RVA: 0x000F9CE4 File Offset: 0x000F7EE4
		private void AseraiPhysicianNarrativeOptionOnSelect(CharacterCreationManager characterCreationManager)
		{
			characterCreationManager.CharacterCreationContent.SetParentOccupation("physician_urban");
			string motherEquipmentId = this.GetMotherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
			string fatherEquipmentId = this.GetFatherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
			MBEquipmentRoster @object = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(motherEquipmentId);
			MBEquipmentRoster object2 = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(fatherEquipmentId);
			string motherAnimation = "act_character_creation_female_default_father_sitting";
			string fatherAnimation = "act_character_creation_male_default_father_sitting";
			this.UpdateParentEquipment(characterCreationManager, @object, object2, motherAnimation, fatherAnimation);
		}

		// Token: 0x06003B57 RID: 15191 RVA: 0x000F9D84 File Offset: 0x000F7F84
		private void GetAseraiFarmerNarrativeOptionArgs(NarrativeMenuOptionArgs args)
		{
			SkillObject[] affectedSkills = new SkillObject[]
			{
				DefaultSkills.Athletics,
				DefaultSkills.OneHanded
			};
			args.SetAffectedSkills(affectedSkills);
			args.SetFocusToSkills(this._focusToAdd);
			args.SetLevelToSkills(this._skillLevelToAdd);
			args.SetLevelToAttribute(DefaultCharacterAttributes.Endurance, this._attributeLevelToAdd);
		}

		// Token: 0x06003B58 RID: 15192 RVA: 0x000F9DD8 File Offset: 0x000F7FD8
		private bool AseraiFarmerNarrativeOptionOnCondition(CharacterCreationManager characterCreationManager)
		{
			return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "aserai";
		}

		// Token: 0x06003B59 RID: 15193 RVA: 0x000F9DF4 File Offset: 0x000F7FF4
		private void AseraiFarmerNarrativeOptionOnSelect(CharacterCreationManager characterCreationManager)
		{
			characterCreationManager.CharacterCreationContent.SetParentOccupation("farmer");
			string motherEquipmentId = this.GetMotherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
			string fatherEquipmentId = this.GetFatherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
			MBEquipmentRoster @object = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(motherEquipmentId);
			MBEquipmentRoster object2 = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(fatherEquipmentId);
			string motherAnimation = "act_character_creation_female_default_side_to_side_2";
			string fatherAnimation = "act_character_creation_male_default_side_to_side_2";
			this.UpdateParentEquipment(characterCreationManager, @object, object2, motherAnimation, fatherAnimation);
		}

		// Token: 0x06003B5A RID: 15194 RVA: 0x000F9E94 File Offset: 0x000F8094
		private void GetAseraiHerderNarrativeOptionArgs(NarrativeMenuOptionArgs args)
		{
			SkillObject[] affectedSkills = new SkillObject[]
			{
				DefaultSkills.Scouting,
				DefaultSkills.Bow
			};
			args.SetAffectedSkills(affectedSkills);
			args.SetFocusToSkills(this._focusToAdd);
			args.SetLevelToSkills(this._skillLevelToAdd);
			args.SetLevelToAttribute(DefaultCharacterAttributes.Cunning, this._attributeLevelToAdd);
		}

		// Token: 0x06003B5B RID: 15195 RVA: 0x000F9EE8 File Offset: 0x000F80E8
		private bool AseraiHerderNarrativeOptionOnCondition(CharacterCreationManager characterCreationManager)
		{
			return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "aserai";
		}

		// Token: 0x06003B5C RID: 15196 RVA: 0x000F9F04 File Offset: 0x000F8104
		private void AseraiHerderNarrativeOptionOnSelect(CharacterCreationManager characterCreationManager)
		{
			characterCreationManager.CharacterCreationContent.SetParentOccupation("herder");
			string motherEquipmentId = this.GetMotherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
			string fatherEquipmentId = this.GetFatherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
			MBEquipmentRoster @object = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(motherEquipmentId);
			MBEquipmentRoster object2 = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(fatherEquipmentId);
			string motherAnimation = "act_character_creation_female_default_side_to_side_3";
			string fatherAnimation = "act_character_creation_male_default_side_to_side_3";
			this.UpdateParentEquipment(characterCreationManager, @object, object2, motherAnimation, fatherAnimation);
		}

		// Token: 0x06003B5D RID: 15197 RVA: 0x000F9FA4 File Offset: 0x000F81A4
		private void GetAseraiArtisanNarrativeOptionArgs(NarrativeMenuOptionArgs args)
		{
			SkillObject[] affectedSkills = new SkillObject[]
			{
				DefaultSkills.Roguery,
				DefaultSkills.Polearm
			};
			args.SetAffectedSkills(affectedSkills);
			args.SetFocusToSkills(this._focusToAdd);
			args.SetLevelToSkills(this._skillLevelToAdd);
			args.SetLevelToAttribute(DefaultCharacterAttributes.Control, this._attributeLevelToAdd);
		}

		// Token: 0x06003B5E RID: 15198 RVA: 0x000F9FF8 File Offset: 0x000F81F8
		private bool AseraiArtisanNarrativeOptionOnCondition(CharacterCreationManager characterCreationManager)
		{
			return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "aserai";
		}

		// Token: 0x06003B5F RID: 15199 RVA: 0x000FA014 File Offset: 0x000F8214
		private void AseraiArtisanNarrativeOptionOnSelect(CharacterCreationManager characterCreationManager)
		{
			characterCreationManager.CharacterCreationContent.SetParentOccupation("artisan_urban");
			string motherEquipmentId = this.GetMotherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
			string fatherEquipmentId = this.GetFatherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
			MBEquipmentRoster @object = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(motherEquipmentId);
			MBEquipmentRoster object2 = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(fatherEquipmentId);
			string motherAnimation = "act_character_creation_female_default_hugging";
			string fatherAnimation = "act_character_creation_male_default_hugging";
			this.UpdateParentEquipment(characterCreationManager, @object, object2, motherAnimation, fatherAnimation);
		}

		// Token: 0x06003B60 RID: 15200 RVA: 0x000FA0B4 File Offset: 0x000F82B4
		private void AddBattaniaNarrativeMenuOptions(NarrativeMenu narrativeMenu)
		{
			NarrativeMenuOption narrativeMenuOption = new NarrativeMenuOption("battania_retainer_option", new TextObject("{=GeNKQlHR}Members of the chieftain's hearthguard", null), new TextObject("{=LpH8SYFL}Your family were the trusted kinfolk of a Battanian chieftain, and sat at his table in his great hall. Your father assisted his chief in running the affairs of the clan and trained with the traditional weapons of the Battanian elite, the two-handed sword or falx and the bow.", null), new GetNarrativeMenuOptionArgsDelegate(this.GetBattaniaRetainerNarrativeOptionArgs), new NarrativeMenuOptionOnConditionDelegate(this.BattaniaRetainerNarrativeOptionOnCondition), new NarrativeMenuOptionOnSelectDelegate(this.BattaniaRetainerNarrativeOptionOnSelect), null);
			narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption);
			NarrativeMenuOption narrativeMenuOption2 = new NarrativeMenuOption("battania_healer_option", new TextObject("{=AeBzTj6w}Healers", null), new TextObject("{=j6py5Rv5}Your parents were healers who gathered herbs and treated the sick. As a living reservoir of Battanian tradition, they were also asked to adjudicate many disputes between the clans.", null), new GetNarrativeMenuOptionArgsDelegate(this.GetBattaniaHealerNarrativeOptionArgs), new NarrativeMenuOptionOnConditionDelegate(this.BattaniaHealerNarrativeOptionOnCondition), new NarrativeMenuOptionOnSelectDelegate(this.BattaniaHealerNarrativeOptionOnSelect), null);
			narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption2);
			NarrativeMenuOption narrativeMenuOption3 = new NarrativeMenuOption("battania_farmer_option", new TextObject("{=tGEStbxb}Tribespeople", null), new TextObject("{=WchH8bS2}Your family were middle-ranking members of a Battanian clan, who tilled their own land. Your father fought with the kern, the main body of his people's warriors, joining in the screaming charges for which the Battanians were famous.", null), new GetNarrativeMenuOptionArgsDelegate(this.GetBattaniaFarmerNarrativeOptionArgs), new NarrativeMenuOptionOnConditionDelegate(this.BattaniaFarmerNarrativeOptionOnCondition), new NarrativeMenuOptionOnSelectDelegate(this.BattaniaFarmerNarrativeOptionOnSelect), null);
			narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption3);
			NarrativeMenuOption narrativeMenuOption4 = new NarrativeMenuOption("battania_artisan_option", new TextObject("{=BCU6RezA}Smiths", null), new TextObject("{=kg9YtrOg}Your family were smiths, a revered profession among the Battanians. They crafted everything from fine filigree jewelry in geometric designs to the well-balanced longswords favored by the Battanian aristocracy.", null), new GetNarrativeMenuOptionArgsDelegate(this.GetBattaniaArtisanNarrativeOptionArgs), new NarrativeMenuOptionOnConditionDelegate(this.BattaniaArtisanNarrativeOptionOnCondition), new NarrativeMenuOptionOnSelectDelegate(this.BattaniaArtisanNarrativeOptionOnSelect), null);
			narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption4);
			NarrativeMenuOption narrativeMenuOption5 = new NarrativeMenuOption("battania_hunter_option", new TextObject("{=7eWmU2mF}Foresters", null), new TextObject("{=7jBroUUQ}Your family had little land of their own, so they earned their living from the woods, hunting and trapping. They taught you from an early age that skills like finding game trails and killing an animal with one shot could make the difference between eating and starvation.", null), new GetNarrativeMenuOptionArgsDelegate(this.GetBattaniaHunterNarrativeOptionArgs), new NarrativeMenuOptionOnConditionDelegate(this.BattaniaHunterNarrativeOptionOnCondition), new NarrativeMenuOptionOnSelectDelegate(this.BattaniaHunterNarrativeOptionOnSelect), null);
			narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption5);
			NarrativeMenuOption narrativeMenuOption6 = new NarrativeMenuOption("battania_bard_option", new TextObject("{=SpJqhEEh}Bards", null), new TextObject("{=aVzcyhhy}Your father was a bard, drifting from chieftain's hall to chieftain's hall making his living singing the praises of one Battanian aristocrat and mocking his enemies, then going to his enemy's hall and doing the reverse. You learned from him that a clever tongue could spare you  from a life toiling in the fields, if you kept your wits about you.", null), new GetNarrativeMenuOptionArgsDelegate(this.GetBattaniaBardNarrativeOptionArgs), new NarrativeMenuOptionOnConditionDelegate(this.BattaniaBardNarrativeOptionOnCondition), new NarrativeMenuOptionOnSelectDelegate(this.BattaniaBardNarrativeOptionOnSelect), null);
			narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption6);
		}

		// Token: 0x06003B61 RID: 15201 RVA: 0x000FA294 File Offset: 0x000F8494
		private void GetBattaniaRetainerNarrativeOptionArgs(NarrativeMenuOptionArgs args)
		{
			SkillObject[] affectedSkills = new SkillObject[]
			{
				DefaultSkills.TwoHanded,
				DefaultSkills.Bow
			};
			args.SetAffectedSkills(affectedSkills);
			args.SetFocusToSkills(this._focusToAdd);
			args.SetLevelToSkills(this._skillLevelToAdd);
			args.SetLevelToAttribute(DefaultCharacterAttributes.Vigor, this._attributeLevelToAdd);
		}

		// Token: 0x06003B62 RID: 15202 RVA: 0x000FA2E8 File Offset: 0x000F84E8
		private bool BattaniaRetainerNarrativeOptionOnCondition(CharacterCreationManager characterCreationManager)
		{
			return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "battania";
		}

		// Token: 0x06003B63 RID: 15203 RVA: 0x000FA304 File Offset: 0x000F8504
		private void BattaniaRetainerNarrativeOptionOnSelect(CharacterCreationManager characterCreationManager)
		{
			characterCreationManager.CharacterCreationContent.SetParentOccupation("retainer_urban");
			string motherEquipmentId = this.GetMotherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
			string fatherEquipmentId = this.GetFatherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
			MBEquipmentRoster @object = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(motherEquipmentId);
			MBEquipmentRoster object2 = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(fatherEquipmentId);
			string motherAnimation = "act_character_creation_female_default_side_to_side_1";
			string fatherAnimation = "act_character_creation_male_default_side_to_side_1";
			this.UpdateParentEquipment(characterCreationManager, @object, object2, motherAnimation, fatherAnimation);
		}

		// Token: 0x06003B64 RID: 15204 RVA: 0x000FA3A4 File Offset: 0x000F85A4
		private void GetBattaniaHealerNarrativeOptionArgs(NarrativeMenuOptionArgs args)
		{
			SkillObject[] affectedSkills = new SkillObject[]
			{
				DefaultSkills.Medicine,
				DefaultSkills.Charm
			};
			args.SetAffectedSkills(affectedSkills);
			args.SetFocusToSkills(this._focusToAdd);
			args.SetLevelToSkills(this._skillLevelToAdd);
			args.SetLevelToAttribute(DefaultCharacterAttributes.Intelligence, this._attributeLevelToAdd);
		}

		// Token: 0x06003B65 RID: 15205 RVA: 0x000FA3F8 File Offset: 0x000F85F8
		private bool BattaniaHealerNarrativeOptionOnCondition(CharacterCreationManager characterCreationManager)
		{
			return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "battania";
		}

		// Token: 0x06003B66 RID: 15206 RVA: 0x000FA414 File Offset: 0x000F8614
		private void BattaniaHealerNarrativeOptionOnSelect(CharacterCreationManager characterCreationManager)
		{
			characterCreationManager.CharacterCreationContent.SetParentOccupation("healer");
			string motherEquipmentId = this.GetMotherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
			string fatherEquipmentId = this.GetFatherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
			MBEquipmentRoster @object = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(motherEquipmentId);
			MBEquipmentRoster object2 = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(fatherEquipmentId);
			string motherAnimation = "act_character_creation_female_default_mother_front";
			string fatherAnimation = "act_character_creation_male_default_mother_front";
			this.UpdateParentEquipment(characterCreationManager, @object, object2, motherAnimation, fatherAnimation);
		}

		// Token: 0x06003B67 RID: 15207 RVA: 0x000FA4B4 File Offset: 0x000F86B4
		private void GetBattaniaFarmerNarrativeOptionArgs(NarrativeMenuOptionArgs args)
		{
			SkillObject[] affectedSkills = new SkillObject[]
			{
				DefaultSkills.Athletics,
				DefaultSkills.Throwing
			};
			args.SetAffectedSkills(affectedSkills);
			args.SetFocusToSkills(this._focusToAdd);
			args.SetLevelToSkills(this._skillLevelToAdd);
			args.SetLevelToAttribute(DefaultCharacterAttributes.Control, this._attributeLevelToAdd);
		}

		// Token: 0x06003B68 RID: 15208 RVA: 0x000FA508 File Offset: 0x000F8708
		private bool BattaniaFarmerNarrativeOptionOnCondition(CharacterCreationManager characterCreationManager)
		{
			return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "battania";
		}

		// Token: 0x06003B69 RID: 15209 RVA: 0x000FA524 File Offset: 0x000F8724
		private void BattaniaFarmerNarrativeOptionOnSelect(CharacterCreationManager characterCreationManager)
		{
			characterCreationManager.CharacterCreationContent.SetParentOccupation("farmer");
			string motherEquipmentId = this.GetMotherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
			string fatherEquipmentId = this.GetFatherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
			MBEquipmentRoster @object = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(motherEquipmentId);
			MBEquipmentRoster object2 = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(fatherEquipmentId);
			string motherAnimation = "act_character_creation_female_default_father_sitting";
			string fatherAnimation = "act_character_creation_male_default_father_sitting";
			this.UpdateParentEquipment(characterCreationManager, @object, object2, motherAnimation, fatherAnimation);
		}

		// Token: 0x06003B6A RID: 15210 RVA: 0x000FA5C4 File Offset: 0x000F87C4
		private void GetBattaniaArtisanNarrativeOptionArgs(NarrativeMenuOptionArgs args)
		{
			SkillObject[] affectedSkills = new SkillObject[]
			{
				DefaultSkills.Crafting,
				DefaultSkills.TwoHanded
			};
			args.SetAffectedSkills(affectedSkills);
			args.SetFocusToSkills(this._focusToAdd);
			args.SetLevelToSkills(this._skillLevelToAdd);
			args.SetLevelToAttribute(DefaultCharacterAttributes.Endurance, this._attributeLevelToAdd);
		}

		// Token: 0x06003B6B RID: 15211 RVA: 0x000FA618 File Offset: 0x000F8818
		private bool BattaniaArtisanNarrativeOptionOnCondition(CharacterCreationManager characterCreationManager)
		{
			return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "battania";
		}

		// Token: 0x06003B6C RID: 15212 RVA: 0x000FA634 File Offset: 0x000F8834
		private void BattaniaArtisanNarrativeOptionOnSelect(CharacterCreationManager characterCreationManager)
		{
			characterCreationManager.CharacterCreationContent.SetParentOccupation("artisan_urban");
			string motherEquipmentId = this.GetMotherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
			string fatherEquipmentId = this.GetFatherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
			MBEquipmentRoster @object = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(motherEquipmentId);
			MBEquipmentRoster object2 = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(fatherEquipmentId);
			string motherAnimation = "act_character_creation_female_default_side_to_side_2";
			string fatherAnimation = "act_character_creation_male_default_side_to_side_2";
			this.UpdateParentEquipment(characterCreationManager, @object, object2, motherAnimation, fatherAnimation);
		}

		// Token: 0x06003B6D RID: 15213 RVA: 0x000FA6D4 File Offset: 0x000F88D4
		private void GetBattaniaHunterNarrativeOptionArgs(NarrativeMenuOptionArgs args)
		{
			SkillObject[] affectedSkills = new SkillObject[]
			{
				DefaultSkills.Scouting,
				DefaultSkills.Tactics
			};
			args.SetAffectedSkills(affectedSkills);
			args.SetFocusToSkills(this._focusToAdd);
			args.SetLevelToSkills(this._skillLevelToAdd);
			args.SetLevelToAttribute(DefaultCharacterAttributes.Cunning, this._attributeLevelToAdd);
		}

		// Token: 0x06003B6E RID: 15214 RVA: 0x000FA728 File Offset: 0x000F8928
		private bool BattaniaHunterNarrativeOptionOnCondition(CharacterCreationManager characterCreationManager)
		{
			return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "battania";
		}

		// Token: 0x06003B6F RID: 15215 RVA: 0x000FA744 File Offset: 0x000F8944
		private void BattaniaHunterNarrativeOptionOnSelect(CharacterCreationManager characterCreationManager)
		{
			characterCreationManager.CharacterCreationContent.SetParentOccupation("hunter");
			string motherEquipmentId = this.GetMotherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
			string fatherEquipmentId = this.GetFatherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
			MBEquipmentRoster @object = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(motherEquipmentId);
			MBEquipmentRoster object2 = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(fatherEquipmentId);
			string motherAnimation = "act_character_creation_female_default_side_to_side_3";
			string fatherAnimation = "act_character_creation_male_default_side_to_side_3";
			this.UpdateParentEquipment(characterCreationManager, @object, object2, motherAnimation, fatherAnimation);
		}

		// Token: 0x06003B70 RID: 15216 RVA: 0x000FA7E4 File Offset: 0x000F89E4
		private void GetBattaniaBardNarrativeOptionArgs(NarrativeMenuOptionArgs args)
		{
			SkillObject[] affectedSkills = new SkillObject[]
			{
				DefaultSkills.Roguery,
				DefaultSkills.Charm
			};
			args.SetAffectedSkills(affectedSkills);
			args.SetFocusToSkills(this._focusToAdd);
			args.SetLevelToSkills(this._skillLevelToAdd);
			args.SetLevelToAttribute(DefaultCharacterAttributes.Social, this._attributeLevelToAdd);
		}

		// Token: 0x06003B71 RID: 15217 RVA: 0x000FA838 File Offset: 0x000F8A38
		private bool BattaniaBardNarrativeOptionOnCondition(CharacterCreationManager characterCreationManager)
		{
			return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "battania";
		}

		// Token: 0x06003B72 RID: 15218 RVA: 0x000FA854 File Offset: 0x000F8A54
		private void BattaniaBardNarrativeOptionOnSelect(CharacterCreationManager characterCreationManager)
		{
			characterCreationManager.CharacterCreationContent.SetParentOccupation("bard_urban");
			string motherEquipmentId = this.GetMotherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
			string fatherEquipmentId = this.GetFatherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
			MBEquipmentRoster @object = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(motherEquipmentId);
			MBEquipmentRoster object2 = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(fatherEquipmentId);
			string motherAnimation = "act_character_creation_female_default_hugging";
			string fatherAnimation = "act_character_creation_male_default_hugging";
			this.UpdateParentEquipment(characterCreationManager, @object, object2, motherAnimation, fatherAnimation);
		}

		// Token: 0x06003B73 RID: 15219 RVA: 0x000FA8F4 File Offset: 0x000F8AF4
		private void AddKhuzaitNarrativeMenuOptions(NarrativeMenu narrativeMenu)
		{
			NarrativeMenuOption narrativeMenuOption = new NarrativeMenuOption("khuzait_retainer_option", new TextObject("{=FVaRDe2a}A noyan's kinsfolk", null), new TextObject("{=jAs3kDXh}Your family were the trusted kinsfolk of a Khuzait noyan, and shared his meals in the chieftain's yurt. Your father assisted his chief in running the affairs of the clan and fought in the core of armored lancers in the center of the Khuzait battle line.", null), new GetNarrativeMenuOptionArgsDelegate(this.GetKhuzaitRetainerNarrativeOptionArgs), new NarrativeMenuOptionOnConditionDelegate(this.KhuzaitRetainerNarrativeOptionOnCondition), new NarrativeMenuOptionOnSelectDelegate(this.KhuzaitRetainerNarrativeOptionOnSelect), null);
			narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption);
			NarrativeMenuOption narrativeMenuOption2 = new NarrativeMenuOption("khuzait_merhant_option", new TextObject("{=TkgLEDRM}Merchants", null), new TextObject("{=qPg3IDiq}Your family came from one of the merchant clans that dominated the cities in eastern Calradia before the Khuzait conquest. They adjusted quickly to their new masters, keeping the caravan routes running and ensuring that the tariff revenues that once went into imperial coffers now flowed to the khanate.", null), new GetNarrativeMenuOptionArgsDelegate(this.GetKhuzaitMerchantNarrativeOptionArgs), new NarrativeMenuOptionOnConditionDelegate(this.KhuzaitMerchantNarrativeOptionOnCondition), new NarrativeMenuOptionOnSelectDelegate(this.KhuzaitMerchantNarrativeOptionOnSelect), null);
			narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption2);
			NarrativeMenuOption narrativeMenuOption3 = new NarrativeMenuOption("khuzait_mercenary_option", new TextObject("{=tGEStbxb}Tribespeople", null), new TextObject("{=URgZ4ai4}Your family were middle-ranking members of one of the Khuzait clans. He had some herds of his own, but was not rich. When the Khuzait horde was summoned to battle, he fought with the horse archers, shooting and wheeling and wearing down the enemy before the lancers delivered the final punch.", null), new GetNarrativeMenuOptionArgsDelegate(this.GetKhuzaitHerderNarrativeOptionArgs), new NarrativeMenuOptionOnConditionDelegate(this.KhuzaitHerderNarrativeOptionOnCondition), new NarrativeMenuOptionOnSelectDelegate(this.KhuzaitHerderNarrativeOptionOnSelect), null);
			narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption3);
			NarrativeMenuOption narrativeMenuOption4 = new NarrativeMenuOption("khuzait_farmer_option", new TextObject("{=gQ2tAvCz}Farmers", null), new TextObject("{=5QSGoRFj}Your family tilled one of the small patches of arable land in the steppes for generations. When the Khuzaits came, they ceased paying taxes to the emperor and providing conscripts for his army, and served the khan instead.", null), new GetNarrativeMenuOptionArgsDelegate(this.GetKhuzaitFarmerNarrativeOptionArgs), new NarrativeMenuOptionOnConditionDelegate(this.KhuzaitFarmerNarrativeOptionOnCondition), new NarrativeMenuOptionOnSelectDelegate(this.KhuzaitFarmerNarrativeOptionOnSelect), null);
			narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption4);
			NarrativeMenuOption narrativeMenuOption5 = new NarrativeMenuOption("khuzait_healer_option", new TextObject("{=vfhVveLW}Shamans", null), new TextObject("{=WOKNhaG2}Your family were guardians of the sacred traditions of the Khuzaits, channelling the spirits of the wilderness and of the ancestors. They tended the sick and dispensed wisdom, resolving disputes and providing practical advice.", null), new GetNarrativeMenuOptionArgsDelegate(this.GetKhuzaitHealerNarrativeOptionArgs), new NarrativeMenuOptionOnConditionDelegate(this.KhuzaitHealerNarrativeOptionOnCondition), new NarrativeMenuOptionOnSelectDelegate(this.KhuzaitHealerNarrativeOptionOnSelect), null);
			narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption5);
			NarrativeMenuOption narrativeMenuOption6 = new NarrativeMenuOption("khuzait_herder_option", new TextObject("{=Xqba1Obq}Nomads", null), new TextObject("{=9aoQYpZs}Your family's clan never pledged its loyalty to the khan and never settled down, preferring to live out in the deep steppe away from his authority. They remain some of the finest trackers and scouts in the grasslands, as the ability to spot an enemy coming and move quickly is often all that protects their herds from their neighbors' predations.", null), new GetNarrativeMenuOptionArgsDelegate(this.GetKhuzaitNomadHerderNarrativeOptionArgs), new NarrativeMenuOptionOnConditionDelegate(this.KhuzaitNomadHerderNarrativeOptionOnCondition), new NarrativeMenuOptionOnSelectDelegate(this.KhuzaitNomadHerderNarrativeOptionOnSelect), null);
			narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption6);
		}

		// Token: 0x06003B74 RID: 15220 RVA: 0x000FAAD4 File Offset: 0x000F8CD4
		private void GetKhuzaitRetainerNarrativeOptionArgs(NarrativeMenuOptionArgs args)
		{
			SkillObject[] affectedSkills = new SkillObject[]
			{
				DefaultSkills.Riding,
				DefaultSkills.Polearm
			};
			args.SetAffectedSkills(affectedSkills);
			args.SetFocusToSkills(this._focusToAdd);
			args.SetLevelToSkills(this._skillLevelToAdd);
			args.SetLevelToAttribute(DefaultCharacterAttributes.Endurance, this._attributeLevelToAdd);
		}

		// Token: 0x06003B75 RID: 15221 RVA: 0x000FAB28 File Offset: 0x000F8D28
		private bool KhuzaitRetainerNarrativeOptionOnCondition(CharacterCreationManager characterCreationManager)
		{
			return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "khuzait";
		}

		// Token: 0x06003B76 RID: 15222 RVA: 0x000FAB44 File Offset: 0x000F8D44
		private void KhuzaitRetainerNarrativeOptionOnSelect(CharacterCreationManager characterCreationManager)
		{
			characterCreationManager.CharacterCreationContent.SetParentOccupation("retainer_urban");
			string motherEquipmentId = this.GetMotherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
			string fatherEquipmentId = this.GetFatherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
			MBEquipmentRoster @object = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(motherEquipmentId);
			MBEquipmentRoster object2 = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(fatherEquipmentId);
			string motherAnimation = "act_character_creation_female_default_side_to_side_1";
			string fatherAnimation = "act_character_creation_male_default_side_to_side_1";
			this.UpdateParentEquipment(characterCreationManager, @object, object2, motherAnimation, fatherAnimation);
		}

		// Token: 0x06003B77 RID: 15223 RVA: 0x000FABE4 File Offset: 0x000F8DE4
		private void GetKhuzaitMerchantNarrativeOptionArgs(NarrativeMenuOptionArgs args)
		{
			SkillObject[] affectedSkills = new SkillObject[]
			{
				DefaultSkills.Trade,
				DefaultSkills.Charm
			};
			args.SetAffectedSkills(affectedSkills);
			args.SetFocusToSkills(this._focusToAdd);
			args.SetLevelToSkills(this._skillLevelToAdd);
			args.SetLevelToAttribute(DefaultCharacterAttributes.Social, this._attributeLevelToAdd);
		}

		// Token: 0x06003B78 RID: 15224 RVA: 0x000FAC38 File Offset: 0x000F8E38
		private bool KhuzaitMerchantNarrativeOptionOnCondition(CharacterCreationManager characterCreationManager)
		{
			return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "khuzait";
		}

		// Token: 0x06003B79 RID: 15225 RVA: 0x000FAC54 File Offset: 0x000F8E54
		private void KhuzaitMerchantNarrativeOptionOnSelect(CharacterCreationManager characterCreationManager)
		{
			characterCreationManager.CharacterCreationContent.SetParentOccupation("merchant_urban");
			string motherEquipmentId = this.GetMotherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
			string fatherEquipmentId = this.GetFatherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
			MBEquipmentRoster @object = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(motherEquipmentId);
			MBEquipmentRoster object2 = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(fatherEquipmentId);
			string motherAnimation = "act_character_creation_female_default_mother_front";
			string fatherAnimation = "act_character_creation_male_default_mother_front";
			this.UpdateParentEquipment(characterCreationManager, @object, object2, motherAnimation, fatherAnimation);
		}

		// Token: 0x06003B7A RID: 15226 RVA: 0x000FACF4 File Offset: 0x000F8EF4
		private void GetKhuzaitHerderNarrativeOptionArgs(NarrativeMenuOptionArgs args)
		{
			SkillObject[] affectedSkills = new SkillObject[]
			{
				DefaultSkills.Bow,
				DefaultSkills.Riding
			};
			args.SetAffectedSkills(affectedSkills);
			args.SetFocusToSkills(this._focusToAdd);
			args.SetLevelToSkills(this._skillLevelToAdd);
			args.SetLevelToAttribute(DefaultCharacterAttributes.Control, this._attributeLevelToAdd);
		}

		// Token: 0x06003B7B RID: 15227 RVA: 0x000FAD48 File Offset: 0x000F8F48
		private bool KhuzaitHerderNarrativeOptionOnCondition(CharacterCreationManager characterCreationManager)
		{
			return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "khuzait";
		}

		// Token: 0x06003B7C RID: 15228 RVA: 0x000FAD64 File Offset: 0x000F8F64
		private void KhuzaitHerderNarrativeOptionOnSelect(CharacterCreationManager characterCreationManager)
		{
			characterCreationManager.CharacterCreationContent.SetParentOccupation("herder");
			string motherEquipmentId = this.GetMotherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
			string fatherEquipmentId = this.GetFatherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
			MBEquipmentRoster @object = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(motherEquipmentId);
			MBEquipmentRoster object2 = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(fatherEquipmentId);
			string motherAnimation = "act_character_creation_female_default_father_sitting";
			string fatherAnimation = "act_character_creation_male_default_father_sitting";
			this.UpdateParentEquipment(characterCreationManager, @object, object2, motherAnimation, fatherAnimation);
		}

		// Token: 0x06003B7D RID: 15229 RVA: 0x000FAE04 File Offset: 0x000F9004
		private void GetKhuzaitFarmerNarrativeOptionArgs(NarrativeMenuOptionArgs args)
		{
			SkillObject[] affectedSkills = new SkillObject[]
			{
				DefaultSkills.Polearm,
				DefaultSkills.Throwing
			};
			args.SetAffectedSkills(affectedSkills);
			args.SetFocusToSkills(this._focusToAdd);
			args.SetLevelToSkills(this._skillLevelToAdd);
			args.SetLevelToAttribute(DefaultCharacterAttributes.Vigor, this._attributeLevelToAdd);
		}

		// Token: 0x06003B7E RID: 15230 RVA: 0x000FAE58 File Offset: 0x000F9058
		private bool KhuzaitFarmerNarrativeOptionOnCondition(CharacterCreationManager characterCreationManager)
		{
			return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "khuzait";
		}

		// Token: 0x06003B7F RID: 15231 RVA: 0x000FAE74 File Offset: 0x000F9074
		private void KhuzaitFarmerNarrativeOptionOnSelect(CharacterCreationManager characterCreationManager)
		{
			characterCreationManager.CharacterCreationContent.SetParentOccupation("farmer");
			string motherEquipmentId = this.GetMotherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
			string fatherEquipmentId = this.GetFatherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
			MBEquipmentRoster @object = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(motherEquipmentId);
			MBEquipmentRoster object2 = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(fatherEquipmentId);
			string motherAnimation = "act_character_creation_female_default_side_to_side_2";
			string fatherAnimation = "act_character_creation_male_default_side_to_side_2";
			this.UpdateParentEquipment(characterCreationManager, @object, object2, motherAnimation, fatherAnimation);
		}

		// Token: 0x06003B80 RID: 15232 RVA: 0x000FAF14 File Offset: 0x000F9114
		private void GetKhuzaitHealerNarrativeOptionArgs(NarrativeMenuOptionArgs args)
		{
			SkillObject[] affectedSkills = new SkillObject[]
			{
				DefaultSkills.Medicine,
				DefaultSkills.Charm
			};
			args.SetAffectedSkills(affectedSkills);
			args.SetFocusToSkills(this._focusToAdd);
			args.SetLevelToSkills(this._skillLevelToAdd);
			args.SetLevelToAttribute(DefaultCharacterAttributes.Intelligence, this._attributeLevelToAdd);
		}

		// Token: 0x06003B81 RID: 15233 RVA: 0x000FAF68 File Offset: 0x000F9168
		private bool KhuzaitHealerNarrativeOptionOnCondition(CharacterCreationManager characterCreationManager)
		{
			return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "khuzait";
		}

		// Token: 0x06003B82 RID: 15234 RVA: 0x000FAF84 File Offset: 0x000F9184
		private void KhuzaitHealerNarrativeOptionOnSelect(CharacterCreationManager characterCreationManager)
		{
			characterCreationManager.CharacterCreationContent.SetParentOccupation("healer_urban");
			string motherEquipmentId = this.GetMotherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
			string fatherEquipmentId = this.GetFatherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
			MBEquipmentRoster @object = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(motherEquipmentId);
			MBEquipmentRoster object2 = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(fatherEquipmentId);
			string motherAnimation = "act_character_creation_female_default_side_to_side_3";
			string fatherAnimation = "act_character_creation_male_default_side_to_side_3";
			this.UpdateParentEquipment(characterCreationManager, @object, object2, motherAnimation, fatherAnimation);
		}

		// Token: 0x06003B83 RID: 15235 RVA: 0x000FB024 File Offset: 0x000F9224
		private void GetKhuzaitNomadHerderNarrativeOptionArgs(NarrativeMenuOptionArgs args)
		{
			SkillObject[] affectedSkills = new SkillObject[]
			{
				DefaultSkills.Scouting,
				DefaultSkills.Riding
			};
			args.SetAffectedSkills(affectedSkills);
			args.SetFocusToSkills(this._focusToAdd);
			args.SetLevelToSkills(this._skillLevelToAdd);
			args.SetLevelToAttribute(DefaultCharacterAttributes.Cunning, this._attributeLevelToAdd);
		}

		// Token: 0x06003B84 RID: 15236 RVA: 0x000FB078 File Offset: 0x000F9278
		private bool KhuzaitNomadHerderNarrativeOptionOnCondition(CharacterCreationManager characterCreationManager)
		{
			return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "khuzait";
		}

		// Token: 0x06003B85 RID: 15237 RVA: 0x000FB094 File Offset: 0x000F9294
		private void KhuzaitNomadHerderNarrativeOptionOnSelect(CharacterCreationManager characterCreationManager)
		{
			characterCreationManager.CharacterCreationContent.SetParentOccupation("herder");
			string motherEquipmentId = this.GetMotherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
			string fatherEquipmentId = this.GetFatherEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId);
			MBEquipmentRoster @object = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(motherEquipmentId);
			MBEquipmentRoster object2 = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(fatherEquipmentId);
			string motherAnimation = "act_character_creation_female_default_hugging";
			string fatherAnimation = "act_character_creation_male_default_hugging";
			this.UpdateParentEquipment(characterCreationManager, @object, object2, motherAnimation, fatherAnimation);
		}

		// Token: 0x06003B86 RID: 15238 RVA: 0x000FB134 File Offset: 0x000F9334
		private List<NarrativeMenuCharacterArgs> GetChildhoodMenuNarrativeMenuCharacterArgs(CultureObject culture, string occupationType, CharacterCreationManager characterCreationManager)
		{
			List<NarrativeMenuCharacterArgs> list = new List<NarrativeMenuCharacterArgs>();
			string playerChildhoodAgeEquipmentId = this.GetPlayerChildhoodAgeEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId, Hero.MainHero.IsFemale);
			list.Add(new NarrativeMenuCharacterArgs("player_childhood_character", 7, playerChildhoodAgeEquipmentId, "act_childhood_schooled", "spawnpoint_player_1", "", "", null, true, CharacterObject.PlayerCharacter.IsFemale));
			return list;
		}

		// Token: 0x06003B87 RID: 15239 RVA: 0x000FB1A8 File Offset: 0x000F93A8
		private void AddChildhoodMenu(CharacterCreationManager characterCreationManager)
		{
			List<NarrativeMenuCharacter> list = new List<NarrativeMenuCharacter>();
			BodyProperties bodyProperties = CharacterObject.PlayerCharacter.GetBodyProperties(CharacterObject.PlayerCharacter.Equipment, -1);
			bodyProperties = FaceGen.GetBodyPropertiesWithAge(ref bodyProperties, 7f);
			NarrativeMenuCharacter item = new NarrativeMenuCharacter("player_childhood_character", bodyProperties, CharacterObject.PlayerCharacter.Race, CharacterObject.PlayerCharacter.IsFemale);
			list.Add(item);
			NarrativeMenu narrativeMenu = new NarrativeMenu("narrative_childhood_menu", "narrative_parent_menu", "narrative_education_menu", new TextObject("{=8Yiwt1z6}Early Childhood", null), new TextObject("{=character_creation_content_16}As a child you were noted for...", null), list, new NarrativeMenu.GetNarrativeMenuCharacterArgsDelegate(this.GetChildhoodMenuNarrativeMenuCharacterArgs));
			this.AddChildhoodNarrativeMenuOptions(narrativeMenu);
			characterCreationManager.AddNewMenu(narrativeMenu);
		}

		// Token: 0x06003B88 RID: 15240 RVA: 0x000FB24C File Offset: 0x000F944C
		private void AddChildhoodNarrativeMenuOptions(NarrativeMenu narrativeMenu)
		{
			NarrativeMenuOption narrativeMenuOption = new NarrativeMenuOption("childhood_leadership_option", new TextObject("{=kmM68Qx4}your leadership skills.", null), new TextObject("{=FfNwXtii}If the wolf pup gang of your early childhood had an alpha, it was definitely you. All the other kids followed your lead as you decided what to play and where to play, and led them in games and mischief.", null), new GetNarrativeMenuOptionArgsDelegate(this.GetChildhoodLeadershipOptionArgs), new NarrativeMenuOptionOnConditionDelegate(this.ChildhoodLeadershipOptionOnCondition), new NarrativeMenuOptionOnSelectDelegate(this.ChildhoodLeadershipOptionOnSelect), null);
			narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption);
			NarrativeMenuOption narrativeMenuOption2 = new NarrativeMenuOption("childhood_brawn_option", new TextObject("{=5HXS8HEY}your brawn.", null), new TextObject("{=YKzuGc54}You were big, and other children looked to have you around in any scrap with children from a neighboring village. You pushed a plough and threw an axe like an adult.", null), new GetNarrativeMenuOptionArgsDelegate(this.GetChildhoodBrawnOptionArgs), new NarrativeMenuOptionOnConditionDelegate(this.ChildhoodBrawnOptionOnCondition), new NarrativeMenuOptionOnSelectDelegate(this.ChildhoodBrawnOptionOnSelect), null);
			narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption2);
			NarrativeMenuOption narrativeMenuOption3 = new NarrativeMenuOption("childhood_detail_option", new TextObject("{=QrYjPUEf}your attention to detail.", null), new TextObject("{=JUSHAPnu}You were quick on your feet and attentive to what was going on around you. Usually you could run away from trouble, though you could give a good account of yourself in a fight with other children if cornered.", null), new GetNarrativeMenuOptionArgsDelegate(this.GetChildhoodDetailOptionArgs), new NarrativeMenuOptionOnConditionDelegate(this.ChildhoodDetailOptionOnCondition), new NarrativeMenuOptionOnSelectDelegate(this.ChildhoodDetailOptionOnSelect), null);
			narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption3);
			NarrativeMenuOption narrativeMenuOption4 = new NarrativeMenuOption("childhood_smart_option", new TextObject("{=Y3UcaX74}your aptitude for numbers.", null), new TextObject("{=DFidSjIf}Most children around you had only the most rudimentary education, but you lingered after class to study letters and mathematics. You were fascinated by the marketplace - weights and measures, tallies and accounts, the chatter about profits and losses.", null), new GetNarrativeMenuOptionArgsDelegate(this.GetChildhoodSmartOptionArgs), new NarrativeMenuOptionOnConditionDelegate(this.ChildhoodSmartOptionOnCondition), new NarrativeMenuOptionOnSelectDelegate(this.ChildhoodSmartOptionOnSelect), null);
			narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption4);
			NarrativeMenuOption narrativeMenuOption5 = new NarrativeMenuOption("childhood_leader_option", new TextObject("{=GEYzLuwb}your way with people.", null), new TextObject("{=w2TEQq26}You were always attentive to other people, good at guessing their motivations. You studied how individuals were swayed, and tried out what you learned from adults on your friends.", null), new GetNarrativeMenuOptionArgsDelegate(this.GetChildhoodLeaderOptionArgs), new NarrativeMenuOptionOnConditionDelegate(this.ChildhoodLeaderOptionOnCondition), new NarrativeMenuOptionOnSelectDelegate(this.ChildhoodLeaderOptionOnSelect), null);
			narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption5);
			NarrativeMenuOption narrativeMenuOption6 = new NarrativeMenuOption("childhood_horse_option", new TextObject("{=MEgLE2kj}your skill with horses.", null), new TextObject("{=ngazFofr}You were always drawn to animals, and spent as much time as possible hanging out in the village stables. You could calm horses, and were sometimes called upon to break in new colts. You learned the basics of veterinary arts, much of which is applicable to humans as well.", null), new GetNarrativeMenuOptionArgsDelegate(this.GetChildhoodHorseOptionArgs), new NarrativeMenuOptionOnConditionDelegate(this.ChildhoodHorseOptionOnCondition), new NarrativeMenuOptionOnSelectDelegate(this.ChildhoodHorseOptionOnSelect), null);
			narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption6);
		}

		// Token: 0x06003B89 RID: 15241 RVA: 0x000FB42C File Offset: 0x000F962C
		private void GetChildhoodLeadershipOptionArgs(NarrativeMenuOptionArgs args)
		{
			SkillObject[] affectedSkills = new SkillObject[]
			{
				DefaultSkills.Leadership,
				DefaultSkills.Tactics
			};
			args.SetAffectedSkills(affectedSkills);
			args.SetFocusToSkills(this._focusToAdd);
			args.SetLevelToSkills(this._skillLevelToAdd);
			args.SetLevelToAttribute(DefaultCharacterAttributes.Cunning, this._attributeLevelToAdd);
		}

		// Token: 0x06003B8A RID: 15242 RVA: 0x000FB480 File Offset: 0x000F9680
		private bool ChildhoodLeadershipOptionOnCondition(CharacterCreationManager characterCreationManager)
		{
			return true;
		}

		// Token: 0x06003B8B RID: 15243 RVA: 0x000FB484 File Offset: 0x000F9684
		private void ChildhoodLeadershipOptionOnSelect(CharacterCreationManager characterCreationManager)
		{
			foreach (NarrativeMenuCharacter narrativeMenuCharacter in characterCreationManager.CurrentMenu.Characters)
			{
				if (narrativeMenuCharacter.StringId == "player_childhood_character")
				{
					narrativeMenuCharacter.SetAnimationId("act_childhood_leader");
				}
			}
		}

		// Token: 0x06003B8C RID: 15244 RVA: 0x000FB4F4 File Offset: 0x000F96F4
		private void GetChildhoodBrawnOptionArgs(NarrativeMenuOptionArgs args)
		{
			SkillObject[] affectedSkills = new SkillObject[]
			{
				DefaultSkills.TwoHanded,
				DefaultSkills.Throwing
			};
			args.SetAffectedSkills(affectedSkills);
			args.SetFocusToSkills(this._focusToAdd);
			args.SetLevelToSkills(this._skillLevelToAdd);
			args.SetLevelToAttribute(DefaultCharacterAttributes.Vigor, this._attributeLevelToAdd);
		}

		// Token: 0x06003B8D RID: 15245 RVA: 0x000FB548 File Offset: 0x000F9748
		private bool ChildhoodBrawnOptionOnCondition(CharacterCreationManager characterCreationManager)
		{
			return true;
		}

		// Token: 0x06003B8E RID: 15246 RVA: 0x000FB54C File Offset: 0x000F974C
		private void ChildhoodBrawnOptionOnSelect(CharacterCreationManager characterCreationManager)
		{
			foreach (NarrativeMenuCharacter narrativeMenuCharacter in characterCreationManager.CurrentMenu.Characters)
			{
				if (narrativeMenuCharacter.StringId == "player_childhood_character")
				{
					narrativeMenuCharacter.SetAnimationId("act_childhood_athlete");
				}
			}
		}

		// Token: 0x06003B8F RID: 15247 RVA: 0x000FB5BC File Offset: 0x000F97BC
		private void GetChildhoodDetailOptionArgs(NarrativeMenuOptionArgs args)
		{
			SkillObject[] affectedSkills = new SkillObject[]
			{
				DefaultSkills.Athletics,
				DefaultSkills.Bow
			};
			args.SetAffectedSkills(affectedSkills);
			args.SetFocusToSkills(this._focusToAdd);
			args.SetLevelToSkills(this._skillLevelToAdd);
			args.SetLevelToAttribute(DefaultCharacterAttributes.Control, this._attributeLevelToAdd);
		}

		// Token: 0x06003B90 RID: 15248 RVA: 0x000FB610 File Offset: 0x000F9810
		private bool ChildhoodDetailOptionOnCondition(CharacterCreationManager characterCreationManager)
		{
			return true;
		}

		// Token: 0x06003B91 RID: 15249 RVA: 0x000FB614 File Offset: 0x000F9814
		private void ChildhoodDetailOptionOnSelect(CharacterCreationManager characterCreationManager)
		{
			foreach (NarrativeMenuCharacter narrativeMenuCharacter in characterCreationManager.CurrentMenu.Characters)
			{
				if (narrativeMenuCharacter.StringId == "player_childhood_character")
				{
					narrativeMenuCharacter.SetAnimationId("act_childhood_memory");
				}
			}
		}

		// Token: 0x06003B92 RID: 15250 RVA: 0x000FB684 File Offset: 0x000F9884
		private void GetChildhoodSmartOptionArgs(NarrativeMenuOptionArgs args)
		{
			SkillObject[] affectedSkills = new SkillObject[]
			{
				DefaultSkills.Engineering,
				DefaultSkills.Trade
			};
			args.SetAffectedSkills(affectedSkills);
			args.SetFocusToSkills(this._focusToAdd);
			args.SetLevelToSkills(this._skillLevelToAdd);
			args.SetLevelToAttribute(DefaultCharacterAttributes.Intelligence, this._attributeLevelToAdd);
		}

		// Token: 0x06003B93 RID: 15251 RVA: 0x000FB6D8 File Offset: 0x000F98D8
		private bool ChildhoodSmartOptionOnCondition(CharacterCreationManager characterCreationManager)
		{
			return true;
		}

		// Token: 0x06003B94 RID: 15252 RVA: 0x000FB6DC File Offset: 0x000F98DC
		private void ChildhoodSmartOptionOnSelect(CharacterCreationManager characterCreationManager)
		{
			foreach (NarrativeMenuCharacter narrativeMenuCharacter in characterCreationManager.CurrentMenu.Characters)
			{
				if (narrativeMenuCharacter.StringId == "player_childhood_character")
				{
					narrativeMenuCharacter.SetAnimationId("act_childhood_numbers");
				}
			}
		}

		// Token: 0x06003B95 RID: 15253 RVA: 0x000FB74C File Offset: 0x000F994C
		private void GetChildhoodLeaderOptionArgs(NarrativeMenuOptionArgs args)
		{
			SkillObject[] affectedSkills = new SkillObject[]
			{
				DefaultSkills.Charm,
				DefaultSkills.Leadership
			};
			args.SetAffectedSkills(affectedSkills);
			args.SetFocusToSkills(this._focusToAdd);
			args.SetLevelToSkills(this._skillLevelToAdd);
			args.SetLevelToAttribute(DefaultCharacterAttributes.Social, this._attributeLevelToAdd);
		}

		// Token: 0x06003B96 RID: 15254 RVA: 0x000FB7A0 File Offset: 0x000F99A0
		private bool ChildhoodLeaderOptionOnCondition(CharacterCreationManager characterCreationManager)
		{
			return true;
		}

		// Token: 0x06003B97 RID: 15255 RVA: 0x000FB7A4 File Offset: 0x000F99A4
		private void ChildhoodLeaderOptionOnSelect(CharacterCreationManager characterCreationManager)
		{
			foreach (NarrativeMenuCharacter narrativeMenuCharacter in characterCreationManager.CurrentMenu.Characters)
			{
				if (narrativeMenuCharacter.StringId == "player_childhood_character")
				{
					narrativeMenuCharacter.SetAnimationId("act_childhood_manners");
				}
			}
		}

		// Token: 0x06003B98 RID: 15256 RVA: 0x000FB814 File Offset: 0x000F9A14
		private void GetChildhoodHorseOptionArgs(NarrativeMenuOptionArgs args)
		{
			SkillObject[] affectedSkills = new SkillObject[]
			{
				DefaultSkills.Riding,
				DefaultSkills.Medicine
			};
			args.SetAffectedSkills(affectedSkills);
			args.SetFocusToSkills(this._focusToAdd);
			args.SetLevelToSkills(this._skillLevelToAdd);
			args.SetLevelToAttribute(DefaultCharacterAttributes.Endurance, this._attributeLevelToAdd);
		}

		// Token: 0x06003B99 RID: 15257 RVA: 0x000FB868 File Offset: 0x000F9A68
		private bool ChildhoodHorseOptionOnCondition(CharacterCreationManager characterCreationManager)
		{
			return true;
		}

		// Token: 0x06003B9A RID: 15258 RVA: 0x000FB86C File Offset: 0x000F9A6C
		private void ChildhoodHorseOptionOnSelect(CharacterCreationManager characterCreationManager)
		{
			foreach (NarrativeMenuCharacter narrativeMenuCharacter in characterCreationManager.CurrentMenu.Characters)
			{
				if (narrativeMenuCharacter.StringId == "player_childhood_character")
				{
					narrativeMenuCharacter.SetAnimationId("act_childhood_animals");
				}
			}
		}

		// Token: 0x06003B9B RID: 15259 RVA: 0x000FB8DC File Offset: 0x000F9ADC
		private List<NarrativeMenuCharacterArgs> GetEducationMenuNarrativeMenuCharacterArgs(CultureObject culture, string occupationType, CharacterCreationManager characterCreationManager)
		{
			List<NarrativeMenuCharacterArgs> list = new List<NarrativeMenuCharacterArgs>();
			string playerEducationAgeEquipmentId = this.GetPlayerEducationAgeEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedParentOccupation, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId, Hero.MainHero.IsFemale);
			list.Add(new NarrativeMenuCharacterArgs("player_education_character", 12, playerEducationAgeEquipmentId, "act_childhood_schooled", "spawnpoint_player_1", "", "", null, true, CharacterObject.PlayerCharacter.IsFemale));
			return list;
		}

		// Token: 0x06003B9C RID: 15260 RVA: 0x000FB950 File Offset: 0x000F9B50
		public void AddEducationMenu(CharacterCreationManager characterCreationManager)
		{
			BodyProperties bodyProperties = CharacterObject.PlayerCharacter.GetBodyProperties(CharacterObject.PlayerCharacter.Equipment, -1);
			bodyProperties = FaceGen.GetBodyPropertiesWithAge(ref bodyProperties, 12f);
			List<NarrativeMenuCharacter> list = new List<NarrativeMenuCharacter>();
			NarrativeMenuCharacter item = new NarrativeMenuCharacter("player_education_character", bodyProperties, CharacterObject.PlayerCharacter.Race, CharacterObject.PlayerCharacter.IsFemale);
			list.Add(item);
			NarrativeMenu narrativeMenu = new NarrativeMenu("narrative_education_menu", "narrative_childhood_menu", "narrative_youth_menu", new TextObject("{=rcoueCmk}Adolescence", null), new TextObject("{=WYvnWcXQ}Like all village children you helped out in the fields. You also...", null), list, new NarrativeMenu.GetNarrativeMenuCharacterArgsDelegate(this.GetEducationMenuNarrativeMenuCharacterArgs));
			this.AddEducationMenuOptions(narrativeMenu);
			characterCreationManager.AddNewMenu(narrativeMenu);
		}

		// Token: 0x06003B9D RID: 15261 RVA: 0x000FB9F4 File Offset: 0x000F9BF4
		private void AddEducationMenuOptions(NarrativeMenu narrativeMenu)
		{
			NarrativeMenuOption narrativeMenuOption = new NarrativeMenuOption("education_herder_option", new TextObject("{=RKVNvimC}herded the sheep.", null), new TextObject("{=KfaqPpbK}You went with other fleet-footed youths to take the villages' sheep, goats or cattle to graze in pastures near the village. You were in charge of chasing down stray beasts, and always kept a big stone on hand to be hurled at lurking predators if necessary.", null), new GetNarrativeMenuOptionArgsDelegate(this.GetEducationHerderOptionArgs), new NarrativeMenuOptionOnConditionDelegate(this.EducationHerderOptionOnCondition), new NarrativeMenuOptionOnSelectDelegate(this.EducationHerderOptionOnSelect), null);
			narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption);
			NarrativeMenuOption narrativeMenuOption2 = new NarrativeMenuOption("education_smith_option", new TextObject("{=bTKiN0hr}worked in the village smithy.", null), new TextObject("{=y6j1bJTH}You were apprenticed to the local smith. You learned how to heat and forge metal, hammering for hours at a time until your muscles ached.", null), new GetNarrativeMenuOptionArgsDelegate(this.GetEducationSmithOptionArgs), new NarrativeMenuOptionOnConditionDelegate(this.EducationSmithOptionOnCondition), new NarrativeMenuOptionOnSelectDelegate(this.EducationSmithOptionOnSelect), null);
			narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption2);
			NarrativeMenuOption narrativeMenuOption3 = new NarrativeMenuOption("education_engineer_option", new TextObject("{=tI8ZLtoA}repaired projects.", null), new TextObject("{=6LFj919J}You helped dig wells, rethatch houses, and fix broken plows. You learned about the basics of construction, as well as what it takes to keep a farming community prosperous.", null), new GetNarrativeMenuOptionArgsDelegate(this.GetEducationEngineerOptionArgs), new NarrativeMenuOptionOnConditionDelegate(this.EducationEngineerOptionOnCondition), new NarrativeMenuOptionOnSelectDelegate(this.EducationEngineerOptionOnSelect), null);
			narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption3);
			NarrativeMenuOption narrativeMenuOption4 = new NarrativeMenuOption("education_doctor_option", new TextObject("{=TRwgSLD2}gathered herbs in the wild.", null), new TextObject("{=9ks4u5cH}You were sent by the village healer up into the hills to look for useful medicinal plants. You learned which herbs healed wounds or brought down a fever, and how to find them.", null), new GetNarrativeMenuOptionArgsDelegate(this.GetEducationDoctorOptionArgs), new NarrativeMenuOptionOnConditionDelegate(this.EducationDoctorOptionOnCondition), new NarrativeMenuOptionOnSelectDelegate(this.EducationDoctorOptionOnSelect), null);
			narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption4);
			NarrativeMenuOption narrativeMenuOption5 = new NarrativeMenuOption("education_hunter_option", new TextObject("{=T7m7ReTq}hunted small game.", null), new TextObject("{=RuvSk3QT}You accompanied a local hunter as he went into the wilderness, helping him set up traps and catch small animals.", null), new GetNarrativeMenuOptionArgsDelegate(this.GetEducationHunterOptionArgs), new NarrativeMenuOptionOnConditionDelegate(this.EducationHunterOptionOnCondition), new NarrativeMenuOptionOnSelectDelegate(this.EducationHunterOptionOnSelect), null);
			narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption5);
			NarrativeMenuOption narrativeMenuOption6 = new NarrativeMenuOption("education_merchant_option", new TextObject("{=qAbMagWq}sold product at the market.", null), new TextObject("{=DIgsfYfz}You took your family's goods to the nearest town to sell your produce and buy supplies. It was hard work, but you enjoyed the hubbub of the marketplace.", null), new GetNarrativeMenuOptionArgsDelegate(this.GetEducationMerchantOptionArgs), new NarrativeMenuOptionOnConditionDelegate(this.EducationMerchantOptionOnCondition), new NarrativeMenuOptionOnSelectDelegate(this.EducationMerchantOptionOnSelect), null);
			narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption6);
			NarrativeMenuOption narrativeMenuOption7 = new NarrativeMenuOption("education_watcher_option", new TextObject("{=go7Yu7KS}watched the militia training.", null), new TextObject("{=qnqdEJOv}You watched the town's watch practice shooting and perfect their plans to defend the walls in case of a siege.", null), new GetNarrativeMenuOptionArgsDelegate(this.GetEducationWatcherOptionArgs), new NarrativeMenuOptionOnConditionDelegate(this.EducationWatcherOptionOnCondition), new NarrativeMenuOptionOnSelectDelegate(this.EducationWatcherOptionOnSelect), null);
			narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption7);
			NarrativeMenuOption narrativeMenuOption8 = new NarrativeMenuOption("education_ganger_option", new TextObject("{=gAjvAGTa}hung out with the gangs in the alleys.", null), new TextObject("{=1SUTcF0J}The gang leaders who kept watch over the slums of Calradian cities were always in need of poor youth to run messages and back them up in turf wars, while thrill-seeking merchants' sons and daughters sometimes slummed it in their company as well.", null), new GetNarrativeMenuOptionArgsDelegate(this.GetEducationGangerOptionArgs), new NarrativeMenuOptionOnConditionDelegate(this.EducationGangerOptionOnCondition), new NarrativeMenuOptionOnSelectDelegate(this.EducationGangerOptionOnSelect), null);
			narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption8);
			NarrativeMenuOption narrativeMenuOption9 = new NarrativeMenuOption("education_docker_option", new TextObject("{=QVVCgajg}helped at building sites.", null), new TextObject("{=bhdkegZ4}All towns had their share of projects that were constantly in need of both skilled and unskilled labor. You learned how hoists and scaffolds were constructed, how planks and stones were hewn and fitted, and other skills.", null), new GetNarrativeMenuOptionArgsDelegate(this.GetEducationDockerOptionArgs), new NarrativeMenuOptionOnConditionDelegate(this.EducationDockerOptionOnCondition), new NarrativeMenuOptionOnSelectDelegate(this.EducationDockerOptionOnSelect), null);
			narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption9);
			NarrativeMenuOption narrativeMenuOption10 = new NarrativeMenuOption("education_marketer_option", new TextObject("{=JTsv6PFe}worked in the markets and caravanserais.", null), new TextObject("{=rmMcwSn8}You helped your family handle their business affairs, going down to the marketplace to make purchases and oversee the arrival of caravans.", null), new GetNarrativeMenuOptionArgsDelegate(this.GetEducationMarketerOptionArgs), new NarrativeMenuOptionOnConditionDelegate(this.EducationMarketerOptionOnCondition), new NarrativeMenuOptionOnSelectDelegate(this.EducationMarketerOptionOnSelect), null);
			narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption10);
			NarrativeMenuOption narrativeMenuOption11 = new NarrativeMenuOption("education_tutor_option", new TextObject("{=EMVojYzW}studied with your private tutor.", null), new TextObject("{=hXl25avg}Your family arranged for a private tutor and you took full advantage, reading voraciously on history, mathematics, and philosophy and discussing what you read with your tutor and classmates.", null), new GetNarrativeMenuOptionArgsDelegate(this.GetEducationTutorOptionArgs), new NarrativeMenuOptionOnConditionDelegate(this.EducationTutorOptionOnCondition), new NarrativeMenuOptionOnSelectDelegate(this.EducationTutorOptionOnSelect), null);
			narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption11);
			NarrativeMenuOption narrativeMenuOption12 = new NarrativeMenuOption("education_horser_option", new TextObject("{=hin3iA2D}cared for the horses.", null), new TextObject("{=Ghz90npw}Your family owned a few horses at the town stables and you took charge of their care. Many evenings you would take them out beyond the walls and gallup through the fields, racing other youth.", null), new GetNarrativeMenuOptionArgsDelegate(this.GetEducationPoorHorserOptionArgs), new NarrativeMenuOptionOnConditionDelegate(this.EducationPoorHorserOptionOnCondition), new NarrativeMenuOptionOnSelectDelegate(this.EducationPoorHorserOptionOnSelect), null);
			narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption12);
		}

		// Token: 0x06003B9E RID: 15262 RVA: 0x000FBDB0 File Offset: 0x000F9FB0
		private void GetEducationHerderOptionArgs(NarrativeMenuOptionArgs args)
		{
			SkillObject[] affectedSkills = new SkillObject[]
			{
				DefaultSkills.Athletics,
				DefaultSkills.Throwing
			};
			args.SetAffectedSkills(affectedSkills);
			args.SetFocusToSkills(this._focusToAdd);
			args.SetLevelToSkills(this._skillLevelToAdd);
			args.SetLevelToAttribute(DefaultCharacterAttributes.Control, this._attributeLevelToAdd);
		}

		// Token: 0x06003B9F RID: 15263 RVA: 0x000FBE04 File Offset: 0x000FA004
		private bool EducationHerderOptionOnCondition(CharacterCreationManager characterCreationManager)
		{
			return !CharacterCreationCampaignBehavior.CharacterOccupationTypes.IsUrbanOccupation(characterCreationManager.CharacterCreationContent.SelectedParentOccupation);
		}

		// Token: 0x06003BA0 RID: 15264 RVA: 0x000FBE1C File Offset: 0x000FA01C
		private void EducationHerderOptionOnSelect(CharacterCreationManager characterCreationManager)
		{
			foreach (NarrativeMenuCharacter narrativeMenuCharacter in characterCreationManager.CurrentMenu.Characters)
			{
				if (narrativeMenuCharacter.StringId == "player_education_character")
				{
					narrativeMenuCharacter.SetAnimationId("act_childhood_streets");
					narrativeMenuCharacter.SetLeftHandItem("");
					narrativeMenuCharacter.SetRightHandItem("carry_bostaff_rogue1");
					break;
				}
			}
		}

		// Token: 0x06003BA1 RID: 15265 RVA: 0x000FBEA4 File Offset: 0x000FA0A4
		private void GetEducationSmithOptionArgs(NarrativeMenuOptionArgs args)
		{
			SkillObject[] affectedSkills = new SkillObject[]
			{
				DefaultSkills.TwoHanded,
				DefaultSkills.Crafting
			};
			args.SetAffectedSkills(affectedSkills);
			args.SetFocusToSkills(this._focusToAdd);
			args.SetLevelToSkills(this._skillLevelToAdd);
			args.SetLevelToAttribute(DefaultCharacterAttributes.Vigor, this._attributeLevelToAdd);
		}

		// Token: 0x06003BA2 RID: 15266 RVA: 0x000FBEF8 File Offset: 0x000FA0F8
		private bool EducationSmithOptionOnCondition(CharacterCreationManager characterCreationManager)
		{
			return !CharacterCreationCampaignBehavior.CharacterOccupationTypes.IsUrbanOccupation(characterCreationManager.CharacterCreationContent.SelectedParentOccupation);
		}

		// Token: 0x06003BA3 RID: 15267 RVA: 0x000FBF10 File Offset: 0x000FA110
		private void EducationSmithOptionOnSelect(CharacterCreationManager characterCreationManager)
		{
			foreach (NarrativeMenuCharacter narrativeMenuCharacter in characterCreationManager.CurrentMenu.Characters)
			{
				if (narrativeMenuCharacter.StringId == "player_education_character")
				{
					narrativeMenuCharacter.SetAnimationId("act_childhood_militia");
					narrativeMenuCharacter.SetLeftHandItem("");
					narrativeMenuCharacter.SetRightHandItem("peasant_hammer_1_t1");
					break;
				}
			}
		}

		// Token: 0x06003BA4 RID: 15268 RVA: 0x000FBF98 File Offset: 0x000FA198
		private void GetEducationEngineerOptionArgs(NarrativeMenuOptionArgs args)
		{
			SkillObject[] affectedSkills = new SkillObject[]
			{
				DefaultSkills.Crafting,
				DefaultSkills.Engineering
			};
			args.SetAffectedSkills(affectedSkills);
			args.SetFocusToSkills(this._focusToAdd);
			args.SetLevelToSkills(this._skillLevelToAdd);
			args.SetLevelToAttribute(DefaultCharacterAttributes.Intelligence, this._attributeLevelToAdd);
		}

		// Token: 0x06003BA5 RID: 15269 RVA: 0x000FBFEC File Offset: 0x000FA1EC
		private bool EducationEngineerOptionOnCondition(CharacterCreationManager characterCreationManager)
		{
			return !CharacterCreationCampaignBehavior.CharacterOccupationTypes.IsUrbanOccupation(characterCreationManager.CharacterCreationContent.SelectedParentOccupation);
		}

		// Token: 0x06003BA6 RID: 15270 RVA: 0x000FC004 File Offset: 0x000FA204
		private void EducationEngineerOptionOnSelect(CharacterCreationManager characterCreationManager)
		{
			foreach (NarrativeMenuCharacter narrativeMenuCharacter in characterCreationManager.CurrentMenu.Characters)
			{
				if (narrativeMenuCharacter.StringId == "player_education_character")
				{
					narrativeMenuCharacter.SetAnimationId("act_childhood_grit");
					narrativeMenuCharacter.SetLeftHandItem("");
					narrativeMenuCharacter.SetRightHandItem("carry_hammer");
					break;
				}
			}
		}

		// Token: 0x06003BA7 RID: 15271 RVA: 0x000FC08C File Offset: 0x000FA28C
		private void GetEducationDoctorOptionArgs(NarrativeMenuOptionArgs args)
		{
			SkillObject[] affectedSkills = new SkillObject[]
			{
				DefaultSkills.Medicine,
				DefaultSkills.Scouting
			};
			args.SetAffectedSkills(affectedSkills);
			args.SetFocusToSkills(this._focusToAdd);
			args.SetLevelToSkills(this._skillLevelToAdd);
			args.SetLevelToAttribute(DefaultCharacterAttributes.Endurance, this._attributeLevelToAdd);
		}

		// Token: 0x06003BA8 RID: 15272 RVA: 0x000FC0E0 File Offset: 0x000FA2E0
		private bool EducationDoctorOptionOnCondition(CharacterCreationManager characterCreationManager)
		{
			return !CharacterCreationCampaignBehavior.CharacterOccupationTypes.IsUrbanOccupation(characterCreationManager.CharacterCreationContent.SelectedParentOccupation);
		}

		// Token: 0x06003BA9 RID: 15273 RVA: 0x000FC0F8 File Offset: 0x000FA2F8
		private void EducationDoctorOptionOnSelect(CharacterCreationManager characterCreationManager)
		{
			foreach (NarrativeMenuCharacter narrativeMenuCharacter in characterCreationManager.CurrentMenu.Characters)
			{
				if (narrativeMenuCharacter.StringId == "player_education_character")
				{
					narrativeMenuCharacter.SetAnimationId("act_childhood_peddlers");
					narrativeMenuCharacter.SetLeftHandItem("");
					narrativeMenuCharacter.SetRightHandItem("_to_carry_bd_basket_a");
					break;
				}
			}
		}

		// Token: 0x06003BAA RID: 15274 RVA: 0x000FC180 File Offset: 0x000FA380
		private void GetEducationHunterOptionArgs(NarrativeMenuOptionArgs args)
		{
			SkillObject[] affectedSkills = new SkillObject[]
			{
				DefaultSkills.Bow,
				DefaultSkills.Tactics
			};
			args.SetAffectedSkills(affectedSkills);
			args.SetFocusToSkills(this._focusToAdd);
			args.SetLevelToSkills(this._skillLevelToAdd);
			args.SetLevelToAttribute(DefaultCharacterAttributes.Cunning, this._attributeLevelToAdd);
		}

		// Token: 0x06003BAB RID: 15275 RVA: 0x000FC1D4 File Offset: 0x000FA3D4
		private bool EducationHunterOptionOnCondition(CharacterCreationManager characterCreationManager)
		{
			return !CharacterCreationCampaignBehavior.CharacterOccupationTypes.IsUrbanOccupation(characterCreationManager.CharacterCreationContent.SelectedParentOccupation);
		}

		// Token: 0x06003BAC RID: 15276 RVA: 0x000FC1EC File Offset: 0x000FA3EC
		private void EducationHunterOptionOnSelect(CharacterCreationManager characterCreationManager)
		{
			foreach (NarrativeMenuCharacter narrativeMenuCharacter in characterCreationManager.CurrentMenu.Characters)
			{
				if (narrativeMenuCharacter.StringId == "player_education_character")
				{
					narrativeMenuCharacter.SetAnimationId("act_childhood_sharp");
					narrativeMenuCharacter.SetLeftHandItem("");
					narrativeMenuCharacter.SetRightHandItem("composite_bow");
					break;
				}
			}
		}

		// Token: 0x06003BAD RID: 15277 RVA: 0x000FC274 File Offset: 0x000FA474
		private void GetEducationMerchantOptionArgs(NarrativeMenuOptionArgs args)
		{
			SkillObject[] affectedSkills = new SkillObject[]
			{
				DefaultSkills.Trade,
				DefaultSkills.Charm
			};
			args.SetAffectedSkills(affectedSkills);
			args.SetFocusToSkills(this._focusToAdd);
			args.SetLevelToSkills(this._skillLevelToAdd);
			args.SetLevelToAttribute(DefaultCharacterAttributes.Social, this._attributeLevelToAdd);
		}

		// Token: 0x06003BAE RID: 15278 RVA: 0x000FC2C8 File Offset: 0x000FA4C8
		private bool EducationMerchantOptionOnCondition(CharacterCreationManager characterCreationManager)
		{
			return !CharacterCreationCampaignBehavior.CharacterOccupationTypes.IsUrbanOccupation(characterCreationManager.CharacterCreationContent.SelectedParentOccupation);
		}

		// Token: 0x06003BAF RID: 15279 RVA: 0x000FC2E0 File Offset: 0x000FA4E0
		private void EducationMerchantOptionOnSelect(CharacterCreationManager characterCreationManager)
		{
			foreach (NarrativeMenuCharacter narrativeMenuCharacter in characterCreationManager.CurrentMenu.Characters)
			{
				if (narrativeMenuCharacter.StringId == "player_education_character")
				{
					narrativeMenuCharacter.SetAnimationId("act_childhood_peddlers_2");
					narrativeMenuCharacter.SetLeftHandItem("");
					narrativeMenuCharacter.SetRightHandItem("_to_carry_bd_fabric_c");
					break;
				}
			}
		}

		// Token: 0x06003BB0 RID: 15280 RVA: 0x000FC368 File Offset: 0x000FA568
		private void GetEducationWatcherOptionArgs(NarrativeMenuOptionArgs args)
		{
			SkillObject[] affectedSkills = new SkillObject[]
			{
				DefaultSkills.Polearm,
				DefaultSkills.Tactics
			};
			args.SetAffectedSkills(affectedSkills);
			args.SetFocusToSkills(this._focusToAdd);
			args.SetLevelToSkills(this._skillLevelToAdd);
			args.SetLevelToAttribute(DefaultCharacterAttributes.Control, this._attributeLevelToAdd);
		}

		// Token: 0x06003BB1 RID: 15281 RVA: 0x000FC3BC File Offset: 0x000FA5BC
		private bool EducationWatcherOptionOnCondition(CharacterCreationManager characterCreationManager)
		{
			return CharacterCreationCampaignBehavior.CharacterOccupationTypes.IsUrbanOccupation(characterCreationManager.CharacterCreationContent.SelectedParentOccupation);
		}

		// Token: 0x06003BB2 RID: 15282 RVA: 0x000FC3D0 File Offset: 0x000FA5D0
		private void EducationWatcherOptionOnSelect(CharacterCreationManager characterCreationManager)
		{
			foreach (NarrativeMenuCharacter narrativeMenuCharacter in characterCreationManager.CurrentMenu.Characters)
			{
				if (narrativeMenuCharacter.StringId == "player_education_character")
				{
					narrativeMenuCharacter.SetAnimationId("act_childhood_fox");
					narrativeMenuCharacter.SetLeftHandItem("");
					narrativeMenuCharacter.SetRightHandItem("");
					break;
				}
			}
		}

		// Token: 0x06003BB3 RID: 15283 RVA: 0x000FC458 File Offset: 0x000FA658
		private void GetEducationGangerOptionArgs(NarrativeMenuOptionArgs args)
		{
			SkillObject[] affectedSkills = new SkillObject[]
			{
				DefaultSkills.Roguery,
				DefaultSkills.OneHanded
			};
			args.SetAffectedSkills(affectedSkills);
			args.SetFocusToSkills(this._focusToAdd);
			args.SetLevelToSkills(this._skillLevelToAdd);
			args.SetLevelToAttribute(DefaultCharacterAttributes.Cunning, this._attributeLevelToAdd);
		}

		// Token: 0x06003BB4 RID: 15284 RVA: 0x000FC4AC File Offset: 0x000FA6AC
		private bool EducationGangerOptionOnCondition(CharacterCreationManager characterCreationManager)
		{
			return CharacterCreationCampaignBehavior.CharacterOccupationTypes.IsUrbanOccupation(characterCreationManager.CharacterCreationContent.SelectedParentOccupation);
		}

		// Token: 0x06003BB5 RID: 15285 RVA: 0x000FC4C0 File Offset: 0x000FA6C0
		private void EducationGangerOptionOnSelect(CharacterCreationManager characterCreationManager)
		{
			foreach (NarrativeMenuCharacter narrativeMenuCharacter in characterCreationManager.CurrentMenu.Characters)
			{
				if (narrativeMenuCharacter.StringId == "player_education_character")
				{
					narrativeMenuCharacter.SetAnimationId("act_childhood_athlete");
					narrativeMenuCharacter.SetLeftHandItem("");
					narrativeMenuCharacter.SetRightHandItem("");
					break;
				}
			}
		}

		// Token: 0x06003BB6 RID: 15286 RVA: 0x000FC548 File Offset: 0x000FA748
		private void GetEducationDockerOptionArgs(NarrativeMenuOptionArgs args)
		{
			SkillObject[] affectedSkills = new SkillObject[]
			{
				DefaultSkills.Athletics,
				DefaultSkills.Crafting
			};
			args.SetAffectedSkills(affectedSkills);
			args.SetFocusToSkills(this._focusToAdd);
			args.SetLevelToSkills(this._skillLevelToAdd);
			args.SetLevelToAttribute(DefaultCharacterAttributes.Vigor, this._attributeLevelToAdd);
		}

		// Token: 0x06003BB7 RID: 15287 RVA: 0x000FC59C File Offset: 0x000FA79C
		private bool EducationDockerOptionOnCondition(CharacterCreationManager characterCreationManager)
		{
			return CharacterCreationCampaignBehavior.CharacterOccupationTypes.IsUrbanOccupation(characterCreationManager.CharacterCreationContent.SelectedParentOccupation);
		}

		// Token: 0x06003BB8 RID: 15288 RVA: 0x000FC5B0 File Offset: 0x000FA7B0
		private void EducationDockerOptionOnSelect(CharacterCreationManager characterCreationManager)
		{
			foreach (NarrativeMenuCharacter narrativeMenuCharacter in characterCreationManager.CurrentMenu.Characters)
			{
				if (narrativeMenuCharacter.StringId == "player_education_character")
				{
					narrativeMenuCharacter.SetAnimationId("act_childhood_peddlers");
					narrativeMenuCharacter.SetLeftHandItem("");
					narrativeMenuCharacter.SetRightHandItem("_to_carry_bd_basket_a");
					break;
				}
			}
		}

		// Token: 0x06003BB9 RID: 15289 RVA: 0x000FC638 File Offset: 0x000FA838
		private void GetEducationMarketerOptionArgs(NarrativeMenuOptionArgs args)
		{
			SkillObject[] affectedSkills = new SkillObject[]
			{
				DefaultSkills.Trade,
				DefaultSkills.Charm
			};
			args.SetAffectedSkills(affectedSkills);
			args.SetFocusToSkills(this._focusToAdd);
			args.SetLevelToSkills(this._skillLevelToAdd);
			args.SetLevelToAttribute(DefaultCharacterAttributes.Social, this._attributeLevelToAdd);
		}

		// Token: 0x06003BBA RID: 15290 RVA: 0x000FC68C File Offset: 0x000FA88C
		private bool EducationMarketerOptionOnCondition(CharacterCreationManager characterCreationManager)
		{
			return CharacterCreationCampaignBehavior.CharacterOccupationTypes.IsUrbanOccupation(characterCreationManager.CharacterCreationContent.SelectedParentOccupation);
		}

		// Token: 0x06003BBB RID: 15291 RVA: 0x000FC6A0 File Offset: 0x000FA8A0
		private void EducationMarketerOptionOnSelect(CharacterCreationManager characterCreationManager)
		{
			foreach (NarrativeMenuCharacter narrativeMenuCharacter in characterCreationManager.CurrentMenu.Characters)
			{
				if (narrativeMenuCharacter.StringId == "player_education_character")
				{
					narrativeMenuCharacter.SetAnimationId("act_childhood_manners");
					narrativeMenuCharacter.SetLeftHandItem("");
					narrativeMenuCharacter.SetRightHandItem("");
					break;
				}
			}
		}

		// Token: 0x06003BBC RID: 15292 RVA: 0x000FC728 File Offset: 0x000FA928
		private void GetEducationTutorOptionArgs(NarrativeMenuOptionArgs args)
		{
			SkillObject[] affectedSkills = new SkillObject[]
			{
				DefaultSkills.Engineering,
				DefaultSkills.Leadership
			};
			args.SetAffectedSkills(affectedSkills);
			args.SetFocusToSkills(this._focusToAdd);
			args.SetLevelToSkills(this._skillLevelToAdd);
			args.SetLevelToAttribute(DefaultCharacterAttributes.Intelligence, this._attributeLevelToAdd);
		}

		// Token: 0x06003BBD RID: 15293 RVA: 0x000FC77C File Offset: 0x000FA97C
		private bool EducationTutorOptionOnCondition(CharacterCreationManager characterCreationManager)
		{
			return CharacterCreationCampaignBehavior.CharacterOccupationTypes.IsUrbanOccupation(characterCreationManager.CharacterCreationContent.SelectedParentOccupation);
		}

		// Token: 0x06003BBE RID: 15294 RVA: 0x000FC790 File Offset: 0x000FA990
		private void EducationTutorOptionOnSelect(CharacterCreationManager characterCreationManager)
		{
			foreach (NarrativeMenuCharacter narrativeMenuCharacter in characterCreationManager.CurrentMenu.Characters)
			{
				if (narrativeMenuCharacter.StringId == "player_education_character")
				{
					narrativeMenuCharacter.SetAnimationId("act_childhood_book");
					narrativeMenuCharacter.SetLeftHandItem("character_creation_notebook");
					narrativeMenuCharacter.SetRightHandItem("");
					break;
				}
			}
		}

		// Token: 0x06003BBF RID: 15295 RVA: 0x000FC818 File Offset: 0x000FAA18
		private void GetEducationPoorHorserOptionArgs(NarrativeMenuOptionArgs args)
		{
			SkillObject[] affectedSkills = new SkillObject[]
			{
				DefaultSkills.Riding,
				DefaultSkills.Steward
			};
			args.SetAffectedSkills(affectedSkills);
			args.SetFocusToSkills(this._focusToAdd);
			args.SetLevelToSkills(this._skillLevelToAdd);
			args.SetLevelToAttribute(DefaultCharacterAttributes.Endurance, this._attributeLevelToAdd);
		}

		// Token: 0x06003BC0 RID: 15296 RVA: 0x000FC86C File Offset: 0x000FAA6C
		private bool EducationPoorHorserOptionOnCondition(CharacterCreationManager characterCreationManager)
		{
			return CharacterCreationCampaignBehavior.CharacterOccupationTypes.IsUrbanOccupation(characterCreationManager.CharacterCreationContent.SelectedParentOccupation);
		}

		// Token: 0x06003BC1 RID: 15297 RVA: 0x000FC880 File Offset: 0x000FAA80
		private void EducationPoorHorserOptionOnSelect(CharacterCreationManager characterCreationManager)
		{
			foreach (NarrativeMenuCharacter narrativeMenuCharacter in characterCreationManager.CurrentMenu.Characters)
			{
				if (narrativeMenuCharacter.StringId == "player_education_character")
				{
					narrativeMenuCharacter.SetAnimationId("act_childhood_peddlers_2");
					narrativeMenuCharacter.SetLeftHandItem("");
					narrativeMenuCharacter.SetRightHandItem("_to_carry_bd_fabric_c");
					break;
				}
			}
		}

		// Token: 0x06003BC2 RID: 15298 RVA: 0x000FC908 File Offset: 0x000FAB08
		private List<NarrativeMenuCharacterArgs> GetYouthMenuNarrativeMenuCharacterArgs(CultureObject culture, string occupationType, CharacterCreationManager characterCreationManager)
		{
			if (string.IsNullOrEmpty(characterCreationManager.CharacterCreationContent.SelectedTitleType))
			{
				characterCreationManager.CharacterCreationContent.SelectedTitleType = "guard";
			}
			List<NarrativeMenuCharacterArgs> list = new List<NarrativeMenuCharacterArgs>();
			string playerEquipmentId = this.GetPlayerEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedTitleType, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId, Hero.MainHero.IsFemale);
			list.Add(new NarrativeMenuCharacterArgs("player_youth_character", 17, playerEquipmentId, "act_childhood_schooled", "spawnpoint_player_1", "", "", null, true, CharacterObject.PlayerCharacter.IsFemale));
			MBEquipmentRoster @object = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(playerEquipmentId);
			ItemObject item = @object.DefaultEquipment[EquipmentIndex.ArmorItemEndSlot].Item;
			list.Add(new NarrativeMenuCharacterArgs("narrative_character_horse", -1, "", "act_inventory_idle_start", "spawnpoint_mount_1", @object.DefaultEquipment[EquipmentIndex.ArmorItemEndSlot].Item.StringId, @object.DefaultEquipment[EquipmentIndex.HorseHarness].Item.StringId, MountCreationKey.GetRandomMountKey(item, CharacterObject.PlayerCharacter.GetMountKeySeed()), false, false));
			return list;
		}

		// Token: 0x06003BC3 RID: 15299 RVA: 0x000FCA2C File Offset: 0x000FAC2C
		private void AddYouthMenu(CharacterCreationManager characterCreationManager)
		{
			TextObject description = (CharacterObject.PlayerCharacter.IsFemale ? new TextObject("{=5kbeAC7k}In wartorn Calradia, especially in frontier or tribal areas, some women as well as men learn to fight from an early age. You...", null) : new TextObject("{=F7OO5SAa}As a youngster growing up in Calradia, war was never too far away. You...", null));
			BodyProperties bodyProperties = CharacterObject.PlayerCharacter.GetBodyProperties(CharacterObject.PlayerCharacter.Equipment, -1);
			bodyProperties = FaceGen.GetBodyPropertiesWithAge(ref bodyProperties, 17f);
			NarrativeMenuCharacter item = new NarrativeMenuCharacter("player_youth_character", bodyProperties, CharacterObject.PlayerCharacter.Race, CharacterObject.PlayerCharacter.IsFemale);
			NarrativeMenuCharacter item2 = new NarrativeMenuCharacter("narrative_character_horse");
			List<NarrativeMenuCharacter> list = new List<NarrativeMenuCharacter>();
			list.Add(item);
			list.Add(item2);
			NarrativeMenu narrativeMenu = new NarrativeMenu("narrative_youth_menu", "narrative_education_menu", "narrative_adulthood_menu", new TextObject("{=ok8lSW6M}Youth", null), description, list, new NarrativeMenu.GetNarrativeMenuCharacterArgsDelegate(this.GetYouthMenuNarrativeMenuCharacterArgs));
			this.AddYouthMenuOptions(narrativeMenu);
			characterCreationManager.AddNewMenu(narrativeMenu);
		}

		// Token: 0x06003BC4 RID: 15300 RVA: 0x000FCB04 File Offset: 0x000FAD04
		private void AddYouthMenuOptions(NarrativeMenu narrativeMenu)
		{
			NarrativeMenuOption narrativeMenuOption = new NarrativeMenuOption("youth_staff_first_option", new TextObject("{=CITG915d}joined a commander's staff.", null), new TextObject("{=wNHqFlDL}You were chosen by your superior officer to serve an imperial strategos as a courier. You were not given major responsibilities - mostly carrying messages and tending to his horse - but it did give you a chance to see how campaigns were planned and men were deployed in battle.", null), new GetNarrativeMenuOptionArgsDelegate(this.GetYouthStaffOptionArgs), new NarrativeMenuOptionOnConditionDelegate(this.YouthStaffOneOptionOnCondition), new NarrativeMenuOptionOnSelectDelegate(this.YouthStaffOptionOnSelect), null);
			narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption);
			NarrativeMenuOption narrativeMenuOption2 = new NarrativeMenuOption("youth_staff_second_option", new TextObject("{=CITG915d}joined a commander's staff.", null), new TextObject("{=ANbNblaH}You were picked as the courier of the commander of the local forces. You were not given major responsibilities - mostly carrying messages and tending to his horse - but it did give you a chance to see how campaigns were planned and men were deployed in battle.", null), new GetNarrativeMenuOptionArgsDelegate(this.GetYouthStaffOptionArgs), new NarrativeMenuOptionOnConditionDelegate(this.YouthStaffTwoOptionOnCondition), new NarrativeMenuOptionOnSelectDelegate(this.YouthStaffOptionOnSelect), null);
			narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption2);
			NarrativeMenuOption narrativeMenuOption3 = new NarrativeMenuOption("youth_groom_option", new TextObject("{=bhE2i6OU}served as a baron's groom.", null), new TextObject("{=i3k7YtA8}You were chosen by a knight to accompany a minor baron of the Vlandian kingdom. You were not given major responsibilities - mostly carrying messages and tending to his horse - but it did give you a chance to see how campaigns were planned and men were deployed in battle.", null), new GetNarrativeMenuOptionArgsDelegate(this.GetYouthGroomOptionArgs), new NarrativeMenuOptionOnConditionDelegate(this.YouthGroomOptionOnCondition), new NarrativeMenuOptionOnSelectDelegate(this.YouthGroomOptionOnSelect), null);
			narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption3);
			NarrativeMenuOption narrativeMenuOption4 = new NarrativeMenuOption("youth_servant_first_option", new TextObject("{=F2bgujPo}were a chieftain's servant.", null), new TextObject("{=AXWO4C69}Your were choosen among others to accompany a chieftain of your people. You were not given major responsibilities - mostly carrying messages and tending to his horse - but it did give you a chance to see how campaigns were planned and men were deployed in battle.", null), new GetNarrativeMenuOptionArgsDelegate(this.GetYouthServantOptionArgs), new NarrativeMenuOptionOnConditionDelegate(this.YouthServantOneOptionOnCondition), new NarrativeMenuOptionOnSelectDelegate(this.YouthServantOptionOnSelect), null);
			narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption4);
			NarrativeMenuOption narrativeMenuOption5 = new NarrativeMenuOption("youth_servant_second_option", new TextObject("{=F2bgujPo}were a chieftain's servant.", null), new TextObject("{=neMCgMZM}Local wise man picked you to become the messenger of a chieftain of your people. You were not given major responsibilities - mostly carrying messages and tending to his horse - but it did give you a chance to see how campaigns were planned and men were deployed in battle.", null), new GetNarrativeMenuOptionArgsDelegate(this.GetYouthServantOptionArgs), new NarrativeMenuOptionOnConditionDelegate(this.YouthServantTwoOptionOnCondition), new NarrativeMenuOptionOnSelectDelegate(this.YouthServantOptionOnSelect), null);
			narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption5);
			NarrativeMenuOption narrativeMenuOption6 = new NarrativeMenuOption("youth_cavalry_option", new TextObject("{=h2KnarLL}trained with the cavalry.", null), new TextObject("{=7cHsIMLP}You could never have bought the equipment on your own, but you were a good enough rider so that the local lord lent you a horse and equipment. You joined the armored cavalry, training with the lance.", null), new GetNarrativeMenuOptionArgsDelegate(this.GetYouthCavalryOptionArgs), new NarrativeMenuOptionOnConditionDelegate(this.YouthCavalryOptionOnCondition), new NarrativeMenuOptionOnSelectDelegate(this.YouthCavalryOptionOnSelect), null);
			narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption6);
			NarrativeMenuOption narrativeMenuOption7 = new NarrativeMenuOption("youth_hearth_option", new TextObject("{=zsC2t5Hb}trained with the hearth guard.", null), new TextObject("{=RmbWW6Bm}You were a big and imposing enough youth that the chief's guard allowed you to train alongside them, in preparation to join them some day.", null), new GetNarrativeMenuOptionArgsDelegate(this.GetYouthHearthOptionArgs), new NarrativeMenuOptionOnConditionDelegate(this.YouthHearthOptionOnCondition), new NarrativeMenuOptionOnSelectDelegate(this.YouthHearthOptionOnSelect), null);
			narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption7);
			NarrativeMenuOption narrativeMenuOption8 = new NarrativeMenuOption("youth_guard_high_register_option", new TextObject("{=aTncHUfL}stood guard with the garrisons.", null), new TextObject("{=63TAYbkx}Urban troops spend much of their time guarding the town walls. Most of their training was in missile weapons, especially useful during sieges.", null), new GetNarrativeMenuOptionArgsDelegate(this.GetYouthGuardHighRegisterOptionArgs), new NarrativeMenuOptionOnConditionDelegate(this.YouthGuardHighRegisterOptionOnCondition), new NarrativeMenuOptionOnSelectDelegate(this.YouthGuardHighRegisterOptionOnSelect), null);
			narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption8);
			NarrativeMenuOption narrativeMenuOption9 = new NarrativeMenuOption("youth_guard_low_register_option", new TextObject("{=aTncHUfL}stood guard with the garrisons.", null), new TextObject("{=oR58iNDz}Urban troops spend much of their time guarding the town walls. Most of their training was in missile weapons.", null), new GetNarrativeMenuOptionArgsDelegate(this.GetYouthGuardLowRegisterOptionArgs), new NarrativeMenuOptionOnConditionDelegate(this.YouthGuardLowRegisterOptionOnCondition), new NarrativeMenuOptionOnSelectDelegate(this.YouthGuardLowRegisterOptionOnSelect), null);
			narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption9);
			NarrativeMenuOption narrativeMenuOption10 = new NarrativeMenuOption("youth_guard_garrisons_register_option", new TextObject("{=aTncHUfL}stood guard with the garrisons.", null), new TextObject("{=e6lINjFg}The garrisons spent most of their time guarding the town walls, and their training focused largely on missile weapons.", null), new GetNarrativeMenuOptionArgsDelegate(this.GetYouthGuardGarrisonRegisterOptionArgs), new NarrativeMenuOptionOnConditionDelegate(this.YouthGuardGarrisonRegisterOptionOnCondition), new NarrativeMenuOptionOnSelectDelegate(this.YouthGuardGarrisonRegisterOptionOnSelect), null);
			narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption10);
			NarrativeMenuOption narrativeMenuOption11 = new NarrativeMenuOption("youth_guard_empire_register_option", new TextObject("{=aTncHUfL}stood guard with the garrisons.", null), new TextObject("{=oR58iNDz}Urban troops spend much of their time guarding the town walls. Most of their training was in missile weapons.", null), new GetNarrativeMenuOptionArgsDelegate(this.GetYouthGuardEmpireRegisterOptionArgs), new NarrativeMenuOptionOnConditionDelegate(this.YouthGuardEmpireRegisterOptionOnCondition), new NarrativeMenuOptionOnSelectDelegate(this.YouthGuardEmpireRegisterOptionOnSelect), null);
			narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption11);
			NarrativeMenuOption narrativeMenuOption12 = new NarrativeMenuOption("youth_rider_high_register_option", new TextObject("{=VlXOgIX6}rode with the scouts.", null), new TextObject("{=888lmJqs}All of Calradia's kingdoms recognize the value of good light cavalry and horse archers, and are sure to recruit nomads and borderers with the skills to fulfill those duties. You were a good enough rider that your neighbors pitched in to buy you a small pony and a good bow so that you could fulfill their levy obligations.", null), new GetNarrativeMenuOptionArgsDelegate(this.GetYouthRiderHighRegisterOptionArgs), new NarrativeMenuOptionOnConditionDelegate(this.YouthRiderHighRegisterOptionOnCondition), new NarrativeMenuOptionOnSelectDelegate(this.YouthRiderHighRegisterOptionOnSelect), null);
			narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption12);
			NarrativeMenuOption narrativeMenuOption13 = new NarrativeMenuOption("youth_rider_low_register_option", new TextObject("{=VlXOgIX6}rode with the scouts.", null), new TextObject("{=sYuN6hPD}All of Calradia's kingdoms recognize the value of good light cavalry, and are sure to recruit nomads and borderers with the skills to fulfill those duties. You were a good enough rider that your neighbors pitched in to buy you a small pony and a sheaf of javelins so that you could fulfill their levy obligations.", null), new GetNarrativeMenuOptionArgsDelegate(this.GetYouthRiderLowRegisterOptionArgs), new NarrativeMenuOptionOnConditionDelegate(this.YouthRiderLowRegisterOptionOnCondition), new NarrativeMenuOptionOnSelectDelegate(this.YouthRiderLowRegisterOptionOnSelect), null);
			narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption13);
			NarrativeMenuOption narrativeMenuOption14 = new NarrativeMenuOption("youth_infantry_option", new TextObject("{=a8arFSra}trained with the infantry.", null), new TextObject("{=afH90aNs}Levy armed with spear and shield, drawn from smallholding farmers, have always been the backbone of most armies of Calradia.", null), new GetNarrativeMenuOptionArgsDelegate(this.GetYouthInfantryOptionArgs), new NarrativeMenuOptionOnConditionDelegate(this.YouthInfantryOptionOnCondition), new NarrativeMenuOptionOnSelectDelegate(this.YouthInfantryOptionOnSelect), null);
			narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption14);
			NarrativeMenuOption narrativeMenuOption15 = new NarrativeMenuOption("youth_skirmisher_option", new TextObject("{=oMbOIPc9}joined the skirmishers.", null), new TextObject("{=bXAg5w19}Younger recruits, or those of a slighter build, or those too poor to buy shield and armor tend to join the skirmishers. Fighting with bow and javelin, they try to stay out of reach of the main enemy forces.", null), new GetNarrativeMenuOptionArgsDelegate(this.GetYouthSkirmisherOptionArgs), new NarrativeMenuOptionOnConditionDelegate(this.YouthSkirmisherOptionOnCondition), new NarrativeMenuOptionOnSelectDelegate(this.YouthSkirmisherOptionOnSelect), null);
			narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption15);
			NarrativeMenuOption narrativeMenuOption16 = new NarrativeMenuOption("youth_kern_option", new TextObject("{=cDWbwBwI}joined the kern.", null), new TextObject("{=tTb28jyU}Many Battanians fight as kern, versatile troops who could both harass the enemy line with their javelins or join in the final screaming charge once it weakened.", null), new GetNarrativeMenuOptionArgsDelegate(this.GetYouthKernOptionArgs), new NarrativeMenuOptionOnConditionDelegate(this.YouthKernOptionOnCondition), new NarrativeMenuOptionOnSelectDelegate(this.YouthKernOptionOnSelect), null);
			narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption16);
			NarrativeMenuOption narrativeMenuOption17 = new NarrativeMenuOption("youth_camp_option", new TextObject("{=GFUggps8}marched with the camp followers.", null), new TextObject("{=64rWqBLN}You avoided service with one of the main forces of your realm's armies, but followed instead in the train - the troops' wives, lovers and servants, and those who make their living by caring for, entertaining, or cheating the soldiery.", null), new GetNarrativeMenuOptionArgsDelegate(this.GetYouthCampOptionArgs), new NarrativeMenuOptionOnConditionDelegate(this.YouthCampOptionOnCondition), new NarrativeMenuOptionOnSelectDelegate(this.YouthCampOptionOnSelect), null);
			narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption17);
			NarrativeMenuOption narrativeMenuOption18 = new NarrativeMenuOption("youth_envoys_guard_first_option", new TextObject("{=YmPlLGXb}served as an envoy's guard", null), new TextObject("{=qPamcCkA}Your family arranged for you to accompany an envoy. You were not given major responsibilities - mostly carrying arms and trying to look imposing. - but it did give you a chance to travel a lot and socialise and see the world.", null), new GetNarrativeMenuOptionArgsDelegate(this.GetEnvoysGuardFirstOptionArgs), new NarrativeMenuOptionOnConditionDelegate(this.EnvoysGuardFirstOptionOnCondition), new NarrativeMenuOptionOnSelectDelegate(this.EnvoysGuardFirstOptionOnSelect), null);
			narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption18);
			NarrativeMenuOption narrativeMenuOption19 = new NarrativeMenuOption("youth_envoys_guard_second_option", new TextObject("{=YmPlLGXb}served as an envoy's guard", null), new TextObject("{=VYU1nEHP}Your family arranged for you to accompany an envoy. You were not given major responsibilities but it did give you a chance to travel and socialise and see a bit of the world.", null), new GetNarrativeMenuOptionArgsDelegate(this.GetEnvoysGuardSecondOptionArgs), new NarrativeMenuOptionOnConditionDelegate(this.EnvoysGuardSecondOptionOnCondition), new NarrativeMenuOptionOnSelectDelegate(this.EnvoysGuardSecondOptionOnSelect), null);
			narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption19);
		}

		// Token: 0x06003BC5 RID: 15301 RVA: 0x000FD0E8 File Offset: 0x000FB2E8
		private void GetYouthStaffOptionArgs(NarrativeMenuOptionArgs args)
		{
			SkillObject[] affectedSkills = new SkillObject[]
			{
				DefaultSkills.Steward,
				DefaultSkills.Tactics
			};
			args.SetAffectedSkills(affectedSkills);
			args.SetFocusToSkills(this._focusToAdd);
			args.SetLevelToSkills(this._skillLevelToAdd);
			args.SetLevelToAttribute(DefaultCharacterAttributes.Cunning, this._attributeLevelToAdd);
		}

		// Token: 0x06003BC6 RID: 15302 RVA: 0x000FD13C File Offset: 0x000FB33C
		private bool YouthStaffOneOptionOnCondition(CharacterCreationManager characterCreationManager)
		{
			return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "empire";
		}

		// Token: 0x06003BC7 RID: 15303 RVA: 0x000FD158 File Offset: 0x000FB358
		private bool YouthStaffTwoOptionOnCondition(CharacterCreationManager characterCreationManager)
		{
			return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "aserai";
		}

		// Token: 0x06003BC8 RID: 15304 RVA: 0x000FD174 File Offset: 0x000FB374
		private void YouthStaffOptionOnSelect(CharacterCreationManager characterCreationManager)
		{
			characterCreationManager.CharacterCreationContent.SelectedTitleType = "retainer";
			string playerEquipmentId = this.GetPlayerEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedTitleType, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId, Hero.MainHero.IsFemale);
			foreach (NarrativeMenuCharacter narrativeMenuCharacter in characterCreationManager.CurrentMenu.Characters)
			{
				if (narrativeMenuCharacter.StringId == "player_youth_character")
				{
					narrativeMenuCharacter.SetAnimationId("act_childhood_decisive");
					narrativeMenuCharacter.SetEquipment(Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(playerEquipmentId));
				}
			}
		}

		// Token: 0x06003BC9 RID: 15305 RVA: 0x000FD238 File Offset: 0x000FB438
		private void GetYouthGroomOptionArgs(NarrativeMenuOptionArgs args)
		{
			SkillObject[] affectedSkills = new SkillObject[]
			{
				DefaultSkills.Charm,
				DefaultSkills.Tactics
			};
			args.SetAffectedSkills(affectedSkills);
			args.SetFocusToSkills(this._focusToAdd);
			args.SetLevelToSkills(this._skillLevelToAdd);
			args.SetLevelToAttribute(DefaultCharacterAttributes.Social, this._attributeLevelToAdd);
		}

		// Token: 0x06003BCA RID: 15306 RVA: 0x000FD28C File Offset: 0x000FB48C
		private bool YouthGroomOptionOnCondition(CharacterCreationManager characterCreationManager)
		{
			return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "vlandia";
		}

		// Token: 0x06003BCB RID: 15307 RVA: 0x000FD2A8 File Offset: 0x000FB4A8
		private void YouthGroomOptionOnSelect(CharacterCreationManager characterCreationManager)
		{
			characterCreationManager.CharacterCreationContent.SelectedTitleType = "retainer";
			string playerEquipmentId = this.GetPlayerEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedTitleType, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId, Hero.MainHero.IsFemale);
			foreach (NarrativeMenuCharacter narrativeMenuCharacter in characterCreationManager.CurrentMenu.Characters)
			{
				if (narrativeMenuCharacter.StringId == "player_youth_character")
				{
					narrativeMenuCharacter.SetAnimationId("act_childhood_sharp");
					narrativeMenuCharacter.SetEquipment(Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(playerEquipmentId));
				}
			}
		}

		// Token: 0x06003BCC RID: 15308 RVA: 0x000FD36C File Offset: 0x000FB56C
		private void GetYouthServantOptionArgs(NarrativeMenuOptionArgs args)
		{
			SkillObject[] affectedSkills = new SkillObject[]
			{
				DefaultSkills.Steward,
				DefaultSkills.Tactics
			};
			args.SetAffectedSkills(affectedSkills);
			args.SetFocusToSkills(this._focusToAdd);
			args.SetLevelToSkills(this._skillLevelToAdd);
			args.SetLevelToAttribute(DefaultCharacterAttributes.Cunning, this._attributeLevelToAdd);
		}

		// Token: 0x06003BCD RID: 15309 RVA: 0x000FD3C0 File Offset: 0x000FB5C0
		private bool YouthServantOneOptionOnCondition(CharacterCreationManager characterCreationManager)
		{
			return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "khuzait";
		}

		// Token: 0x06003BCE RID: 15310 RVA: 0x000FD3DC File Offset: 0x000FB5DC
		private bool YouthServantTwoOptionOnCondition(CharacterCreationManager characterCreationManager)
		{
			return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "battania";
		}

		// Token: 0x06003BCF RID: 15311 RVA: 0x000FD3F8 File Offset: 0x000FB5F8
		private void YouthServantOptionOnSelect(CharacterCreationManager characterCreationManager)
		{
			characterCreationManager.CharacterCreationContent.SelectedTitleType = "retainer";
			string playerEquipmentId = this.GetPlayerEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedTitleType, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId, Hero.MainHero.IsFemale);
			foreach (NarrativeMenuCharacter narrativeMenuCharacter in characterCreationManager.CurrentMenu.Characters)
			{
				if (narrativeMenuCharacter.StringId == "player_youth_character")
				{
					narrativeMenuCharacter.SetAnimationId("act_childhood_ready");
					narrativeMenuCharacter.SetEquipment(Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(playerEquipmentId));
				}
			}
		}

		// Token: 0x06003BD0 RID: 15312 RVA: 0x000FD4BC File Offset: 0x000FB6BC
		private void GetYouthCavalryOptionArgs(NarrativeMenuOptionArgs args)
		{
			SkillObject[] affectedSkills = new SkillObject[]
			{
				DefaultSkills.Riding,
				DefaultSkills.Polearm
			};
			args.SetAffectedSkills(affectedSkills);
			args.SetFocusToSkills(this._focusToAdd);
			args.SetLevelToSkills(this._skillLevelToAdd);
			args.SetLevelToAttribute(DefaultCharacterAttributes.Endurance, this._attributeLevelToAdd);
		}

		// Token: 0x06003BD1 RID: 15313 RVA: 0x000FD510 File Offset: 0x000FB710
		private bool YouthCavalryOptionOnCondition(CharacterCreationManager characterCreationManager)
		{
			return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "vlandia";
		}

		// Token: 0x06003BD2 RID: 15314 RVA: 0x000FD52C File Offset: 0x000FB72C
		private void YouthCavalryOptionOnSelect(CharacterCreationManager characterCreationManager)
		{
			characterCreationManager.CharacterCreationContent.SelectedTitleType = "mercenary";
			string playerEquipmentId = this.GetPlayerEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedTitleType, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId, Hero.MainHero.IsFemale);
			foreach (NarrativeMenuCharacter narrativeMenuCharacter in characterCreationManager.CurrentMenu.Characters)
			{
				if (narrativeMenuCharacter.StringId == "player_youth_character")
				{
					narrativeMenuCharacter.SetAnimationId("act_childhood_apprentice");
					narrativeMenuCharacter.SetEquipment(Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(playerEquipmentId));
				}
			}
		}

		// Token: 0x06003BD3 RID: 15315 RVA: 0x000FD5F0 File Offset: 0x000FB7F0
		private void GetYouthHearthOptionArgs(NarrativeMenuOptionArgs args)
		{
			SkillObject[] affectedSkills = new SkillObject[]
			{
				DefaultSkills.Riding,
				DefaultSkills.Polearm
			};
			args.SetAffectedSkills(affectedSkills);
			args.SetFocusToSkills(this._focusToAdd);
			args.SetLevelToSkills(this._skillLevelToAdd);
			args.SetLevelToAttribute(DefaultCharacterAttributes.Endurance, this._attributeLevelToAdd);
		}

		// Token: 0x06003BD4 RID: 15316 RVA: 0x000FD644 File Offset: 0x000FB844
		private bool YouthHearthOptionOnCondition(CharacterCreationManager characterCreationManager)
		{
			return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "sturgia" || characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "battania";
		}

		// Token: 0x06003BD5 RID: 15317 RVA: 0x000FD680 File Offset: 0x000FB880
		private void YouthHearthOptionOnSelect(CharacterCreationManager characterCreationManager)
		{
			characterCreationManager.CharacterCreationContent.SelectedTitleType = "mercenary";
			string playerEquipmentId = this.GetPlayerEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedTitleType, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId, Hero.MainHero.IsFemale);
			foreach (NarrativeMenuCharacter narrativeMenuCharacter in characterCreationManager.CurrentMenu.Characters)
			{
				if (narrativeMenuCharacter.StringId == "player_youth_character")
				{
					narrativeMenuCharacter.SetAnimationId("act_childhood_athlete");
					narrativeMenuCharacter.SetEquipment(Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(playerEquipmentId));
				}
			}
		}

		// Token: 0x06003BD6 RID: 15318 RVA: 0x000FD744 File Offset: 0x000FB944
		private void GetYouthGuardHighRegisterOptionArgs(NarrativeMenuOptionArgs args)
		{
			SkillObject[] affectedSkills = new SkillObject[]
			{
				DefaultSkills.Crossbow,
				DefaultSkills.Engineering
			};
			args.SetAffectedSkills(affectedSkills);
			args.SetFocusToSkills(this._focusToAdd);
			args.SetLevelToSkills(this._skillLevelToAdd);
			args.SetLevelToAttribute(DefaultCharacterAttributes.Intelligence, this._attributeLevelToAdd);
		}

		// Token: 0x06003BD7 RID: 15319 RVA: 0x000FD798 File Offset: 0x000FB998
		private bool YouthGuardHighRegisterOptionOnCondition(CharacterCreationManager characterCreationManager)
		{
			return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "vlandia";
		}

		// Token: 0x06003BD8 RID: 15320 RVA: 0x000FD7B4 File Offset: 0x000FB9B4
		private void YouthGuardHighRegisterOptionOnSelect(CharacterCreationManager characterCreationManager)
		{
			characterCreationManager.CharacterCreationContent.SelectedTitleType = "guard";
			string playerEquipmentId = this.GetPlayerEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedTitleType, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId, Hero.MainHero.IsFemale);
			foreach (NarrativeMenuCharacter narrativeMenuCharacter in characterCreationManager.CurrentMenu.Characters)
			{
				if (narrativeMenuCharacter.StringId == "player_youth_character")
				{
					narrativeMenuCharacter.SetAnimationId("act_childhood_vibrant");
					narrativeMenuCharacter.SetEquipment(Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(playerEquipmentId));
				}
			}
		}

		// Token: 0x06003BD9 RID: 15321 RVA: 0x000FD878 File Offset: 0x000FBA78
		private void GetYouthGuardLowRegisterOptionArgs(NarrativeMenuOptionArgs args)
		{
			SkillObject[] affectedSkills = new SkillObject[]
			{
				DefaultSkills.Bow,
				DefaultSkills.Engineering
			};
			args.SetAffectedSkills(affectedSkills);
			args.SetFocusToSkills(this._focusToAdd);
			args.SetLevelToSkills(this._skillLevelToAdd);
			args.SetLevelToAttribute(DefaultCharacterAttributes.Intelligence, this._attributeLevelToAdd);
		}

		// Token: 0x06003BDA RID: 15322 RVA: 0x000FD8CC File Offset: 0x000FBACC
		private bool YouthGuardLowRegisterOptionOnCondition(CharacterCreationManager characterCreationManager)
		{
			return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "sturgia";
		}

		// Token: 0x06003BDB RID: 15323 RVA: 0x000FD8E8 File Offset: 0x000FBAE8
		private void YouthGuardLowRegisterOptionOnSelect(CharacterCreationManager characterCreationManager)
		{
			characterCreationManager.CharacterCreationContent.SelectedTitleType = "guard";
			string playerEquipmentId = this.GetPlayerEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedTitleType, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId, Hero.MainHero.IsFemale);
			foreach (NarrativeMenuCharacter narrativeMenuCharacter in characterCreationManager.CurrentMenu.Characters)
			{
				if (narrativeMenuCharacter.StringId == "player_youth_character")
				{
					narrativeMenuCharacter.SetAnimationId("act_childhood_sharp");
					narrativeMenuCharacter.SetEquipment(Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(playerEquipmentId));
				}
			}
		}

		// Token: 0x06003BDC RID: 15324 RVA: 0x000FD9AC File Offset: 0x000FBBAC
		private void GetYouthGuardGarrisonRegisterOptionArgs(NarrativeMenuOptionArgs args)
		{
			SkillObject[] affectedSkills = new SkillObject[]
			{
				DefaultSkills.Bow,
				DefaultSkills.Engineering
			};
			args.SetAffectedSkills(affectedSkills);
			args.SetFocusToSkills(this._focusToAdd);
			args.SetLevelToSkills(this._skillLevelToAdd);
			args.SetLevelToAttribute(DefaultCharacterAttributes.Intelligence, this._attributeLevelToAdd);
		}

		// Token: 0x06003BDD RID: 15325 RVA: 0x000FDA00 File Offset: 0x000FBC00
		private bool YouthGuardGarrisonRegisterOptionOnCondition(CharacterCreationManager characterCreationManager)
		{
			return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "battania" || characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "khuzait" || characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "aserai";
		}

		// Token: 0x06003BDE RID: 15326 RVA: 0x000FDA64 File Offset: 0x000FBC64
		private void YouthGuardGarrisonRegisterOptionOnSelect(CharacterCreationManager characterCreationManager)
		{
			characterCreationManager.CharacterCreationContent.SelectedTitleType = "guard";
			string playerEquipmentId = this.GetPlayerEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedTitleType, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId, Hero.MainHero.IsFemale);
			foreach (NarrativeMenuCharacter narrativeMenuCharacter in characterCreationManager.CurrentMenu.Characters)
			{
				if (narrativeMenuCharacter.StringId == "player_youth_character")
				{
					narrativeMenuCharacter.SetAnimationId("act_childhood_sharp");
					narrativeMenuCharacter.SetEquipment(Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(playerEquipmentId));
				}
			}
		}

		// Token: 0x06003BDF RID: 15327 RVA: 0x000FDB28 File Offset: 0x000FBD28
		private void GetYouthGuardEmpireRegisterOptionArgs(NarrativeMenuOptionArgs args)
		{
			SkillObject[] affectedSkills = new SkillObject[]
			{
				DefaultSkills.Crossbow,
				DefaultSkills.Engineering
			};
			args.SetAffectedSkills(affectedSkills);
			args.SetFocusToSkills(this._focusToAdd);
			args.SetLevelToSkills(this._skillLevelToAdd);
			args.SetLevelToAttribute(DefaultCharacterAttributes.Intelligence, this._attributeLevelToAdd);
		}

		// Token: 0x06003BE0 RID: 15328 RVA: 0x000FDB7C File Offset: 0x000FBD7C
		private bool YouthGuardEmpireRegisterOptionOnCondition(CharacterCreationManager characterCreationManager)
		{
			return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "empire";
		}

		// Token: 0x06003BE1 RID: 15329 RVA: 0x000FDB98 File Offset: 0x000FBD98
		private void YouthGuardEmpireRegisterOptionOnSelect(CharacterCreationManager characterCreationManager)
		{
			characterCreationManager.CharacterCreationContent.SelectedTitleType = "guard";
			string playerEquipmentId = this.GetPlayerEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedTitleType, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId, Hero.MainHero.IsFemale);
			foreach (NarrativeMenuCharacter narrativeMenuCharacter in characterCreationManager.CurrentMenu.Characters)
			{
				if (narrativeMenuCharacter.StringId == "player_youth_character")
				{
					narrativeMenuCharacter.SetAnimationId("act_childhood_sharp");
					narrativeMenuCharacter.SetEquipment(Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(playerEquipmentId));
				}
			}
		}

		// Token: 0x06003BE2 RID: 15330 RVA: 0x000FDC5C File Offset: 0x000FBE5C
		private void GetYouthRiderHighRegisterOptionArgs(NarrativeMenuOptionArgs args)
		{
			SkillObject[] affectedSkills = new SkillObject[]
			{
				DefaultSkills.Riding,
				DefaultSkills.Bow
			};
			args.SetAffectedSkills(affectedSkills);
			args.SetFocusToSkills(this._focusToAdd);
			args.SetLevelToSkills(this._skillLevelToAdd);
			args.SetLevelToAttribute(DefaultCharacterAttributes.Endurance, this._attributeLevelToAdd);
		}

		// Token: 0x06003BE3 RID: 15331 RVA: 0x000FDCB0 File Offset: 0x000FBEB0
		private bool YouthRiderHighRegisterOptionOnCondition(CharacterCreationManager characterCreationManager)
		{
			return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "empire" || characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "khuzait";
		}

		// Token: 0x06003BE4 RID: 15332 RVA: 0x000FDCEC File Offset: 0x000FBEEC
		private void YouthRiderHighRegisterOptionOnSelect(CharacterCreationManager characterCreationManager)
		{
			characterCreationManager.CharacterCreationContent.SelectedTitleType = "hunter";
			string playerEquipmentId = this.GetPlayerEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedTitleType, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId, Hero.MainHero.IsFemale);
			foreach (NarrativeMenuCharacter narrativeMenuCharacter in characterCreationManager.CurrentMenu.Characters)
			{
				if (narrativeMenuCharacter.StringId == "player_youth_character")
				{
					narrativeMenuCharacter.SetAnimationId("act_sturgia_mp_warrior_axe");
					narrativeMenuCharacter.SetEquipment(Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(playerEquipmentId));
				}
			}
		}

		// Token: 0x06003BE5 RID: 15333 RVA: 0x000FDDB0 File Offset: 0x000FBFB0
		private void GetYouthRiderLowRegisterOptionArgs(NarrativeMenuOptionArgs args)
		{
			SkillObject[] affectedSkills = new SkillObject[]
			{
				DefaultSkills.Riding,
				DefaultSkills.Bow
			};
			args.SetAffectedSkills(affectedSkills);
			args.SetFocusToSkills(this._focusToAdd);
			args.SetLevelToSkills(this._skillLevelToAdd);
			args.SetLevelToAttribute(DefaultCharacterAttributes.Endurance, this._attributeLevelToAdd);
		}

		// Token: 0x06003BE6 RID: 15334 RVA: 0x000FDE04 File Offset: 0x000FC004
		private bool YouthRiderLowRegisterOptionOnCondition(CharacterCreationManager characterCreationManager)
		{
			return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "aserai" || characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "sturgia";
		}

		// Token: 0x06003BE7 RID: 15335 RVA: 0x000FDE40 File Offset: 0x000FC040
		private void YouthRiderLowRegisterOptionOnSelect(CharacterCreationManager characterCreationManager)
		{
			characterCreationManager.CharacterCreationContent.SelectedTitleType = "hunter";
			string playerEquipmentId = this.GetPlayerEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedTitleType, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId, Hero.MainHero.IsFemale);
			foreach (NarrativeMenuCharacter narrativeMenuCharacter in characterCreationManager.CurrentMenu.Characters)
			{
				if (narrativeMenuCharacter.StringId == "player_youth_character")
				{
					narrativeMenuCharacter.SetAnimationId("act_sturgia_mp_huskarl_idle");
					narrativeMenuCharacter.SetEquipment(Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(playerEquipmentId));
				}
			}
		}

		// Token: 0x06003BE8 RID: 15336 RVA: 0x000FDF04 File Offset: 0x000FC104
		private void GetYouthInfantryOptionArgs(NarrativeMenuOptionArgs args)
		{
			SkillObject[] affectedSkills = new SkillObject[]
			{
				DefaultSkills.Polearm,
				DefaultSkills.OneHanded
			};
			args.SetAffectedSkills(affectedSkills);
			args.SetFocusToSkills(this._focusToAdd);
			args.SetLevelToSkills(this._skillLevelToAdd);
			args.SetLevelToAttribute(DefaultCharacterAttributes.Vigor, this._attributeLevelToAdd);
		}

		// Token: 0x06003BE9 RID: 15337 RVA: 0x000FDF58 File Offset: 0x000FC158
		private bool YouthInfantryOptionOnCondition(CharacterCreationManager characterCreationManager)
		{
			return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "empire" || characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "vlandia" || characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "khuzait" || characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "aserai" || characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "battania" || characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "sturgia";
		}

		// Token: 0x06003BEA RID: 15338 RVA: 0x000FE010 File Offset: 0x000FC210
		private void YouthInfantryOptionOnSelect(CharacterCreationManager characterCreationManager)
		{
			characterCreationManager.CharacterCreationContent.SelectedTitleType = "infantry";
			string playerEquipmentId = this.GetPlayerEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedTitleType, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId, Hero.MainHero.IsFemale);
			foreach (NarrativeMenuCharacter narrativeMenuCharacter in characterCreationManager.CurrentMenu.Characters)
			{
				if (narrativeMenuCharacter.StringId == "player_youth_character")
				{
					narrativeMenuCharacter.SetAnimationId("act_childhood_fierce");
					narrativeMenuCharacter.SetEquipment(Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(playerEquipmentId));
				}
			}
		}

		// Token: 0x06003BEB RID: 15339 RVA: 0x000FE0D4 File Offset: 0x000FC2D4
		private void GetYouthSkirmisherOptionArgs(NarrativeMenuOptionArgs args)
		{
			SkillObject[] affectedSkills = new SkillObject[]
			{
				DefaultSkills.Throwing,
				DefaultSkills.OneHanded
			};
			args.SetAffectedSkills(affectedSkills);
			args.SetFocusToSkills(this._focusToAdd);
			args.SetLevelToSkills(this._skillLevelToAdd);
			args.SetLevelToAttribute(DefaultCharacterAttributes.Control, this._attributeLevelToAdd);
		}

		// Token: 0x06003BEC RID: 15340 RVA: 0x000FE128 File Offset: 0x000FC328
		private bool YouthSkirmisherOptionOnCondition(CharacterCreationManager characterCreationManager)
		{
			return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "empire" || characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "vlandia" || characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "khuzait" || characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "aserai" || characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "sturgia";
		}

		// Token: 0x06003BED RID: 15341 RVA: 0x000FE1C4 File Offset: 0x000FC3C4
		private void YouthSkirmisherOptionOnSelect(CharacterCreationManager characterCreationManager)
		{
			characterCreationManager.CharacterCreationContent.SelectedTitleType = "skirmisher";
			string playerEquipmentId = this.GetPlayerEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedTitleType, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId, Hero.MainHero.IsFemale);
			foreach (NarrativeMenuCharacter narrativeMenuCharacter in characterCreationManager.CurrentMenu.Characters)
			{
				if (narrativeMenuCharacter.StringId == "player_youth_character")
				{
					narrativeMenuCharacter.SetAnimationId("act_childhood_fox");
					narrativeMenuCharacter.SetEquipment(Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(playerEquipmentId));
				}
			}
		}

		// Token: 0x06003BEE RID: 15342 RVA: 0x000FE288 File Offset: 0x000FC488
		private void GetYouthKernOptionArgs(NarrativeMenuOptionArgs args)
		{
			SkillObject[] affectedSkills = new SkillObject[]
			{
				DefaultSkills.Throwing,
				DefaultSkills.OneHanded
			};
			args.SetAffectedSkills(affectedSkills);
			args.SetFocusToSkills(this._focusToAdd);
			args.SetLevelToSkills(this._skillLevelToAdd);
			args.SetLevelToAttribute(DefaultCharacterAttributes.Control, this._attributeLevelToAdd);
		}

		// Token: 0x06003BEF RID: 15343 RVA: 0x000FE2DC File Offset: 0x000FC4DC
		private bool YouthKernOptionOnCondition(CharacterCreationManager characterCreationManager)
		{
			return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "battania";
		}

		// Token: 0x06003BF0 RID: 15344 RVA: 0x000FE2F8 File Offset: 0x000FC4F8
		private void YouthKernOptionOnSelect(CharacterCreationManager characterCreationManager)
		{
			characterCreationManager.CharacterCreationContent.SelectedTitleType = "kern";
			string playerEquipmentId = this.GetPlayerEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedTitleType, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId, Hero.MainHero.IsFemale);
			foreach (NarrativeMenuCharacter narrativeMenuCharacter in characterCreationManager.CurrentMenu.Characters)
			{
				if (narrativeMenuCharacter.StringId == "player_youth_character")
				{
					narrativeMenuCharacter.SetAnimationId("act_childhood_apprentice");
					narrativeMenuCharacter.SetEquipment(Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(playerEquipmentId));
				}
			}
		}

		// Token: 0x06003BF1 RID: 15345 RVA: 0x000FE3BC File Offset: 0x000FC5BC
		private void GetYouthCampOptionArgs(NarrativeMenuOptionArgs args)
		{
			SkillObject[] affectedSkills = new SkillObject[]
			{
				DefaultSkills.Roguery,
				DefaultSkills.Throwing
			};
			args.SetAffectedSkills(affectedSkills);
			args.SetFocusToSkills(this._focusToAdd);
			args.SetLevelToSkills(this._skillLevelToAdd);
			args.SetLevelToAttribute(DefaultCharacterAttributes.Cunning, this._attributeLevelToAdd);
		}

		// Token: 0x06003BF2 RID: 15346 RVA: 0x000FE410 File Offset: 0x000FC610
		private bool YouthCampOptionOnCondition(CharacterCreationManager characterCreationManager)
		{
			return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "vlandia" || characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "sturgia";
		}

		// Token: 0x06003BF3 RID: 15347 RVA: 0x000FE44C File Offset: 0x000FC64C
		private void YouthCampOptionOnSelect(CharacterCreationManager characterCreationManager)
		{
			characterCreationManager.CharacterCreationContent.SelectedTitleType = "bard";
			string playerEquipmentId = this.GetPlayerEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedTitleType, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId, Hero.MainHero.IsFemale);
			foreach (NarrativeMenuCharacter narrativeMenuCharacter in characterCreationManager.CurrentMenu.Characters)
			{
				if (narrativeMenuCharacter.StringId == "player_youth_character")
				{
					narrativeMenuCharacter.SetAnimationId("act_childhood_militia");
					narrativeMenuCharacter.SetEquipment(Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(playerEquipmentId));
				}
			}
		}

		// Token: 0x06003BF4 RID: 15348 RVA: 0x000FE510 File Offset: 0x000FC710
		private void GetEnvoysGuardFirstOptionArgs(NarrativeMenuOptionArgs args)
		{
			SkillObject[] affectedSkills = new SkillObject[]
			{
				DefaultSkills.Charm,
				DefaultSkills.Scouting
			};
			args.SetAffectedSkills(affectedSkills);
			args.SetFocusToSkills(this._focusToAdd);
			args.SetLevelToSkills(this._skillLevelToAdd);
			args.SetLevelToAttribute(DefaultCharacterAttributes.Social, this._attributeLevelToAdd);
		}

		// Token: 0x06003BF5 RID: 15349 RVA: 0x000FE564 File Offset: 0x000FC764
		private void GetEnvoysGuardSecondOptionArgs(NarrativeMenuOptionArgs args)
		{
			SkillObject[] affectedSkills = new SkillObject[]
			{
				DefaultSkills.Charm,
				DefaultSkills.Scouting
			};
			args.SetAffectedSkills(affectedSkills);
			args.SetFocusToSkills(this._focusToAdd);
			args.SetLevelToSkills(this._skillLevelToAdd);
			args.SetLevelToAttribute(DefaultCharacterAttributes.Social, this._attributeLevelToAdd);
		}

		// Token: 0x06003BF6 RID: 15350 RVA: 0x000FE5B8 File Offset: 0x000FC7B8
		private bool EnvoysGuardFirstOptionOnCondition(CharacterCreationManager characterCreationManager)
		{
			return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "empire" || characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "khuzait";
		}

		// Token: 0x06003BF7 RID: 15351 RVA: 0x000FE5F2 File Offset: 0x000FC7F2
		private bool EnvoysGuardSecondOptionOnCondition(CharacterCreationManager characterCreationManager)
		{
			return characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "battania" || characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "aserai";
		}

		// Token: 0x06003BF8 RID: 15352 RVA: 0x000FE62C File Offset: 0x000FC82C
		private void EnvoysGuardFirstOptionOnSelect(CharacterCreationManager characterCreationManager)
		{
			characterCreationManager.CharacterCreationContent.SelectedTitleType = "guard";
			string playerEquipmentId = this.GetPlayerEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedTitleType, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId, Hero.MainHero.IsFemale);
			foreach (NarrativeMenuCharacter narrativeMenuCharacter in characterCreationManager.CurrentMenu.Characters)
			{
				if (narrativeMenuCharacter.StringId == "player_youth_character")
				{
					narrativeMenuCharacter.SetAnimationId("act_childhood_sharp");
					narrativeMenuCharacter.SetEquipment(Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(playerEquipmentId));
				}
			}
		}

		// Token: 0x06003BF9 RID: 15353 RVA: 0x000FE6F0 File Offset: 0x000FC8F0
		private void EnvoysGuardSecondOptionOnSelect(CharacterCreationManager characterCreationManager)
		{
			characterCreationManager.CharacterCreationContent.SelectedTitleType = "guard";
			string playerEquipmentId = this.GetPlayerEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedTitleType, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId, Hero.MainHero.IsFemale);
			foreach (NarrativeMenuCharacter narrativeMenuCharacter in characterCreationManager.CurrentMenu.Characters)
			{
				if (narrativeMenuCharacter.StringId == "player_youth_character")
				{
					narrativeMenuCharacter.SetAnimationId("act_childhood_sharp");
					narrativeMenuCharacter.SetEquipment(Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(playerEquipmentId));
				}
			}
		}

		// Token: 0x06003BFA RID: 15354 RVA: 0x000FE7B4 File Offset: 0x000FC9B4
		private List<NarrativeMenuCharacterArgs> GetAdultMenuNarrativeMenuCharacterArgs(CultureObject culture, string occupationType, CharacterCreationManager characterCreationManager)
		{
			List<NarrativeMenuCharacterArgs> list = new List<NarrativeMenuCharacterArgs>();
			string playerEquipmentId = this.GetPlayerEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedTitleType, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId, Hero.MainHero.IsFemale);
			list.Add(new NarrativeMenuCharacterArgs("player_adulthood_character", 20, playerEquipmentId, "act_childhood_schooled", "spawnpoint_player_1", "", "", null, true, CharacterObject.PlayerCharacter.IsFemale));
			MBEquipmentRoster @object = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(playerEquipmentId);
			ItemObject item = @object.DefaultEquipment[EquipmentIndex.ArmorItemEndSlot].Item;
			list.Add(new NarrativeMenuCharacterArgs("narrative_character_horse", -1, "", "act_horse_stand_1", "spawnpoint_mount_1", @object.DefaultEquipment[EquipmentIndex.ArmorItemEndSlot].Item.StringId, @object.DefaultEquipment[EquipmentIndex.HorseHarness].Item.StringId, MountCreationKey.GetRandomMountKey(item, CharacterObject.PlayerCharacter.GetMountKeySeed()), false, false));
			return list;
		}

		// Token: 0x06003BFB RID: 15355 RVA: 0x000FE8B4 File Offset: 0x000FCAB4
		private void AddAdulthoodMenu(CharacterCreationManager characterCreationManager)
		{
			BodyProperties bodyProperties = CharacterObject.PlayerCharacter.GetBodyProperties(CharacterObject.PlayerCharacter.Equipment, -1);
			bodyProperties = FaceGen.GetBodyPropertiesWithAge(ref bodyProperties, 20f);
			NarrativeMenuCharacter item = new NarrativeMenuCharacter("player_adulthood_character", bodyProperties, CharacterObject.PlayerCharacter.Race, CharacterObject.PlayerCharacter.IsFemale);
			NarrativeMenuCharacter item2 = new NarrativeMenuCharacter("narrative_character_horse");
			List<NarrativeMenuCharacter> list = new List<NarrativeMenuCharacter>();
			list.Add(item);
			list.Add(item2);
			MBTextManager.SetTextVariable("EXP_VALUE", this._skillLevelToAdd);
			NarrativeMenu narrativeMenu = new NarrativeMenu("narrative_adulthood_menu", "narrative_youth_menu", "narrative_age_selection_menu", new TextObject("{=MafIe9yI}Young Adulthood", null), new TextObject("{=4WYY0X59}Before you set out for a life of adventure, your biggest achievement was...", null), list, new NarrativeMenu.GetNarrativeMenuCharacterArgsDelegate(this.GetAdultMenuNarrativeMenuCharacterArgs));
			this.AddAdulthoodMenuOptions(narrativeMenu);
			characterCreationManager.AddNewMenu(narrativeMenu);
		}

		// Token: 0x06003BFC RID: 15356 RVA: 0x000FE97C File Offset: 0x000FCB7C
		private void AddAdulthoodMenuOptions(NarrativeMenu narrativeMenu)
		{
			NarrativeMenuOption narrativeMenuOption = new NarrativeMenuOption("adulthood_defeated_enemy_option", new TextObject("{=8bwpVpgy}you defeated an enemy in battle.", null), new TextObject("{=1IEroJKs}Not everyone who musters for the levy marches to war, and not everyone who goes on campaign sees action. You did both, and you also took down an enemy warrior in direct one-to-one combat, in the full view of your comrades.", null), new GetNarrativeMenuOptionArgsDelegate(this.GetAdulthoodDefeatedEnemyOptionArgs), new NarrativeMenuOptionOnConditionDelegate(this.AdulthoodDefeatedEnemyOptionOnCondition), new NarrativeMenuOptionOnSelectDelegate(this.AdulthoodDefeatedEnemyOptionOnSelect), null);
			narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption);
			NarrativeMenuOption narrativeMenuOption2 = new NarrativeMenuOption("adulthood_manhunt_option", new TextObject("{=mP3uFbcq}you led a successful manhunt.", null), new TextObject("{=4f5xwzX0}When your community needed to organize a posse to pursue horse thieves, you were the obvious choice. You hunted down the raiders, surrounded them and forced their surrender, and took back your stolen property.", null), new GetNarrativeMenuOptionArgsDelegate(this.GetAdulthoodManhuntOptionArgs), new NarrativeMenuOptionOnConditionDelegate(this.AdulthoodManhuntOptionOnCondition), new NarrativeMenuOptionOnSelectDelegate(this.AdulthoodManhuntOptionOnSelect), null);
			narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption2);
			NarrativeMenuOption narrativeMenuOption3 = new NarrativeMenuOption("adulthood_caravan_leader_option", new TextObject("{=wfbtS71d}you led a caravan.", null), new TextObject("{=joRHKCkm}Your family needed someone trustworthy to take a caravan to a neighboring town. You organized supplies, ensured a constant watch to keep away bandits, and brought it safely to its destination.", null), new GetNarrativeMenuOptionArgsDelegate(this.GetAdulthoodCaravanLeaderOptionArgs), new NarrativeMenuOptionOnConditionDelegate(this.AdulthoodCaravanLeaderOptionOnCondition), new NarrativeMenuOptionOnSelectDelegate(this.AdulthoodCaravanLeaderOptionOnSelect), null);
			narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption3);
			NarrativeMenuOption narrativeMenuOption4 = new NarrativeMenuOption("adulthood_saved_village_option", new TextObject("{=x1HTX5hq}you saved your village from a flood.", null), new TextObject("{=bWlmGDf3}When a sudden storm caused the local stream to rise suddenly, your neighbors needed quick-thinking leadership. You provided it, directing them to build levees to save their homes.", null), new GetNarrativeMenuOptionArgsDelegate(this.GetAdulthoodSavedVillageOptionArgs), new NarrativeMenuOptionOnConditionDelegate(this.AdulthoodSavedVillageOptionOnCondition), new NarrativeMenuOptionOnSelectDelegate(this.AdulthoodSavedVillageOptionOnSelect), null);
			narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption4);
			NarrativeMenuOption narrativeMenuOption5 = new NarrativeMenuOption("adulthood_saved_city_option", new TextObject("{=s8PNllPN}you saved your city quarter from a fire.", null), new TextObject("{=ZAGR6PYc}When a sudden blaze broke out in a back alley, your neighbors needed quick-thinking leadership and you provided it. You organized a bucket line to the nearest well, putting the fire out before any homes were lost.", null), new GetNarrativeMenuOptionArgsDelegate(this.GetAdulthoodSavedCityOptionArgs), new NarrativeMenuOptionOnConditionDelegate(this.AdulthoodSavedCityOptionOnCondition), new NarrativeMenuOptionOnSelectDelegate(this.AdulthoodSavedCityOptionOnSelect), null);
			narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption5);
			NarrativeMenuOption narrativeMenuOption6 = new NarrativeMenuOption("adulthood_workshop_option", new TextObject("{=xORjDTal}you invested some money in a workshop.", null), new TextObject("{=PyVqDLBu}Your parents didn't give you much money, but they did leave just enough for you to secure a loan against a larger amount to build a small workshop. You paid back what you borrowed, and sold your enterprise for a profit.", null), new GetNarrativeMenuOptionArgsDelegate(this.GetAdulthoodWorkshopOptionArgs), new NarrativeMenuOptionOnConditionDelegate(this.AdulthoodWorkshopOptionOnCondition), new NarrativeMenuOptionOnSelectDelegate(this.AdulthoodWorkshopOptionOnSelect), null);
			narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption6);
			NarrativeMenuOption narrativeMenuOption7 = new NarrativeMenuOption("adulthood_investor_option", new TextObject("{=xKXcqRJI}you invested some money in land.", null), new TextObject("{=cbF9jdQo}Your parents didn't give you much money, but they did leave just enough for you to purchase a plot of unused land at the edge of the village. You cleared away rocks and dug an irrigation ditch, raised a few seasons of crops, than sold it for a considerable profit.", null), new GetNarrativeMenuOptionArgsDelegate(this.GetAdulthoodInvestorOptionArgs), new NarrativeMenuOptionOnConditionDelegate(this.AdulthoodInvestorOptionOnCondition), new NarrativeMenuOptionOnSelectDelegate(this.AdulthoodInvestorOptionOnSelect), null);
			narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption7);
			NarrativeMenuOption narrativeMenuOption8 = new NarrativeMenuOption("adulthood_hunter_option", new TextObject("{=TbNRtUjb}you hunted a dangerous animal.", null), new TextObject("{=I3PcdaaL}Wolves, bears are a constant menace to the flocks of northern Calradia, while hyenas and leopards trouble the south. You went with a group of your fellow villagers and fired the missile that brought down the beast.", null), new GetNarrativeMenuOptionArgsDelegate(this.GetAdulthoodHunterOptionArgs), new NarrativeMenuOptionOnConditionDelegate(this.AdulthoodHunterOptionOnCondition), new NarrativeMenuOptionOnSelectDelegate(this.AdulthoodHunterOptionOnSelect), null);
			narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption8);
			NarrativeMenuOption narrativeMenuOption9 = new NarrativeMenuOption("adulthood_siege_survivor_option", new TextObject("{=WbHfGCbd}you survived a siege.", null), new TextObject("{=FhZPjhli}Your hometown was briefly placed under siege, and you were called to defend the walls. Everyone did their part to repulse the enemy assault, and everyone is justly proud of what they endured.", null), new GetNarrativeMenuOptionArgsDelegate(this.GetAdulthoodSiegeSurvivorOptionArgs), new NarrativeMenuOptionOnConditionDelegate(this.AdulthoodSiegeSurvivorOptionOnCondition), new NarrativeMenuOptionOnSelectDelegate(this.AdulthoodSiegeSurvivorOptionOnSelect), null);
			narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption9);
			NarrativeMenuOption narrativeMenuOption10 = new NarrativeMenuOption("adulthood_escapade_high_register_option", new TextObject("{=kNXet6Um}you had a famous escapade in town.", null), new TextObject("{=DjeAJtix}Maybe it was a love affair, or maybe you cheated at dice, or maybe you just chose your words poorly when drinking with a dangerous crowd. Anyway, on one of your trips into town you got into the kind of trouble from which only a quick tongue or quick feet get you out alive.", null), new GetNarrativeMenuOptionArgsDelegate(this.GetAdulthoodEscapadeHighRegisterOptionArgs), new NarrativeMenuOptionOnConditionDelegate(this.AdulthoodEscapadeHighRegisterOptionOnCondition), new NarrativeMenuOptionOnSelectDelegate(this.AdulthoodEscapadeHighRegisterOptionOnSelect), null);
			narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption10);
			NarrativeMenuOption narrativeMenuOption11 = new NarrativeMenuOption("adulthood_escapade_low_register_option", new TextObject("{=qlOuiKXj}you had a famous escapade.", null), new TextObject("{=lD5Ob3R4}Maybe it was a love affair, or maybe you cheated at dice, or maybe you just chose your words poorly when drinking with a dangerous crowd. Anyway, you got into the kind of trouble from which only a quick tongue or quick feet get you out alive.", null), new GetNarrativeMenuOptionArgsDelegate(this.GetAdulthoodEscapadeLowRegisterOptionArgs), new NarrativeMenuOptionOnConditionDelegate(this.AdulthoodEscapadeLowRegisterOptionOnCondition), new NarrativeMenuOptionOnSelectDelegate(this.AdulthoodEscapadeLowRegisterOptionOnSelect), null);
			narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption11);
			NarrativeMenuOption narrativeMenuOption12 = new NarrativeMenuOption("adulthood_nice_person_option", new TextObject("{=Yqm0Dics}you treated people well.", null), new TextObject("{=dDmcqTzb}Yours wasn't the kind of reputation that local legends are made of, but it was the kind that wins you respect among those around you. You were consistently fair and honest in your business dealings and helpful to those in trouble. In doing so, you got a sense of what made people tick.", null), new GetNarrativeMenuOptionArgsDelegate(this.GetAdulthoodNicePersonOptionArgs), new NarrativeMenuOptionOnConditionDelegate(this.AdulthoodNicePersonOptionOnCondition), new NarrativeMenuOptionOnSelectDelegate(this.AdulthoodNicePersonOptionOnSelect), null);
			narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption12);
		}

		// Token: 0x06003BFD RID: 15357 RVA: 0x000FED38 File Offset: 0x000FCF38
		private void GetAdulthoodDefeatedEnemyOptionArgs(NarrativeMenuOptionArgs args)
		{
			SkillObject[] affectedSkills = new SkillObject[]
			{
				DefaultSkills.OneHanded,
				DefaultSkills.TwoHanded
			};
			args.SetAffectedSkills(affectedSkills);
			args.SetFocusToSkills(this._focusToAdd);
			args.SetLevelToSkills(this._skillLevelToAdd);
			args.SetLevelToAttribute(DefaultCharacterAttributes.Vigor, this._attributeLevelToAdd);
			TraitObject[] affectedTraits = new TraitObject[] { DefaultTraits.Valor };
			args.SetAffectedTraits(affectedTraits);
			args.SetLevelToTraits(1);
			args.SetRenownToAdd(20);
		}

		// Token: 0x06003BFE RID: 15358 RVA: 0x000FEDB1 File Offset: 0x000FCFB1
		private bool AdulthoodDefeatedEnemyOptionOnCondition(CharacterCreationManager characterCreationManager)
		{
			return true;
		}

		// Token: 0x06003BFF RID: 15359 RVA: 0x000FEDB4 File Offset: 0x000FCFB4
		private void AdulthoodDefeatedEnemyOptionOnSelect(CharacterCreationManager characterCreationManager)
		{
			foreach (NarrativeMenuCharacter narrativeMenuCharacter in characterCreationManager.CurrentMenu.Characters)
			{
				if (narrativeMenuCharacter.StringId == "player_adulthood_character")
				{
					narrativeMenuCharacter.SetAnimationId("act_childhood_athlete");
				}
			}
		}

		// Token: 0x06003C00 RID: 15360 RVA: 0x000FEE24 File Offset: 0x000FD024
		private void GetAdulthoodManhuntOptionArgs(NarrativeMenuOptionArgs args)
		{
			SkillObject[] affectedSkills = new SkillObject[]
			{
				DefaultSkills.Tactics,
				DefaultSkills.Leadership
			};
			args.SetAffectedSkills(affectedSkills);
			args.SetFocusToSkills(this._focusToAdd);
			args.SetLevelToSkills(this._skillLevelToAdd);
			args.SetLevelToAttribute(DefaultCharacterAttributes.Cunning, this._attributeLevelToAdd);
			TraitObject[] affectedTraits = new TraitObject[] { DefaultTraits.Calculating };
			args.SetAffectedTraits(affectedTraits);
			args.SetLevelToTraits(1);
			args.SetRenownToAdd(10);
		}

		// Token: 0x06003C01 RID: 15361 RVA: 0x000FEEA0 File Offset: 0x000FD0A0
		private bool AdulthoodManhuntOptionOnCondition(CharacterCreationManager characterCreationManager)
		{
			return !CharacterCreationCampaignBehavior.CharacterOccupationTypes.IsUrbanOccupation(characterCreationManager.CharacterCreationContent.SelectedParentOccupation) && (characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "vlandia" || characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "empire" || characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "aserai" || characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "battania" || characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "khuzait");
		}

		// Token: 0x06003C02 RID: 15362 RVA: 0x000FEF50 File Offset: 0x000FD150
		private void AdulthoodManhuntOptionOnSelect(CharacterCreationManager characterCreationManager)
		{
			foreach (NarrativeMenuCharacter narrativeMenuCharacter in characterCreationManager.CurrentMenu.Characters)
			{
				if (narrativeMenuCharacter.StringId == "player_adulthood_character")
				{
					narrativeMenuCharacter.SetAnimationId("act_battania_mp_clan_warrior_shieldperk_idle");
				}
			}
		}

		// Token: 0x06003C03 RID: 15363 RVA: 0x000FEFC0 File Offset: 0x000FD1C0
		private void GetAdulthoodCaravanLeaderOptionArgs(NarrativeMenuOptionArgs args)
		{
			SkillObject[] affectedSkills = new SkillObject[]
			{
				DefaultSkills.Trade,
				DefaultSkills.Leadership
			};
			args.SetAffectedSkills(affectedSkills);
			args.SetFocusToSkills(this._focusToAdd);
			args.SetLevelToSkills(this._skillLevelToAdd);
			args.SetLevelToAttribute(DefaultCharacterAttributes.Cunning, this._attributeLevelToAdd);
			TraitObject[] affectedTraits = new TraitObject[] { DefaultTraits.Calculating };
			args.SetAffectedTraits(affectedTraits);
			args.SetLevelToTraits(1);
			args.SetRenownToAdd(10);
		}

		// Token: 0x06003C04 RID: 15364 RVA: 0x000FF03C File Offset: 0x000FD23C
		private bool AdulthoodCaravanLeaderOptionOnCondition(CharacterCreationManager characterCreationManager)
		{
			return CharacterCreationCampaignBehavior.CharacterOccupationTypes.IsUrbanOccupation(characterCreationManager.CharacterCreationContent.SelectedParentOccupation) && (characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "vlandia" || characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "sturgia" || characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "empire" || characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "aserai" || characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "khuzait" || characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "nord");
		}

		// Token: 0x06003C05 RID: 15365 RVA: 0x000FF10C File Offset: 0x000FD30C
		private void AdulthoodCaravanLeaderOptionOnSelect(CharacterCreationManager characterCreationManager)
		{
			foreach (NarrativeMenuCharacter narrativeMenuCharacter in characterCreationManager.CurrentMenu.Characters)
			{
				if (narrativeMenuCharacter.StringId == "player_adulthood_character")
				{
					narrativeMenuCharacter.SetAnimationId("act_childhood_ready_handshield");
				}
			}
		}

		// Token: 0x06003C06 RID: 15366 RVA: 0x000FF17C File Offset: 0x000FD37C
		private void GetAdulthoodSavedVillageOptionArgs(NarrativeMenuOptionArgs args)
		{
			SkillObject[] affectedSkills = new SkillObject[]
			{
				DefaultSkills.Tactics,
				DefaultSkills.Leadership
			};
			args.SetAffectedSkills(affectedSkills);
			args.SetFocusToSkills(this._focusToAdd);
			args.SetLevelToSkills(this._skillLevelToAdd);
			args.SetLevelToAttribute(DefaultCharacterAttributes.Cunning, this._attributeLevelToAdd);
			TraitObject[] affectedTraits = new TraitObject[] { DefaultTraits.Valor };
			args.SetAffectedTraits(affectedTraits);
			args.SetLevelToTraits(1);
			args.SetRenownToAdd(10);
		}

		// Token: 0x06003C07 RID: 15367 RVA: 0x000FF1F8 File Offset: 0x000FD3F8
		private bool AdulthoodSavedVillageOptionOnCondition(CharacterCreationManager characterCreationManager)
		{
			return !CharacterCreationCampaignBehavior.CharacterOccupationTypes.IsUrbanOccupation(characterCreationManager.CharacterCreationContent.SelectedParentOccupation) && (characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "sturgia" || characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "nord");
		}

		// Token: 0x06003C08 RID: 15368 RVA: 0x000FF254 File Offset: 0x000FD454
		private void AdulthoodSavedVillageOptionOnSelect(CharacterCreationManager characterCreationManager)
		{
			foreach (NarrativeMenuCharacter narrativeMenuCharacter in characterCreationManager.CurrentMenu.Characters)
			{
				if (narrativeMenuCharacter.StringId == "player_adulthood_character")
				{
					narrativeMenuCharacter.SetAnimationId("act_drafted_to_war_pose");
				}
			}
		}

		// Token: 0x06003C09 RID: 15369 RVA: 0x000FF2C4 File Offset: 0x000FD4C4
		private void GetAdulthoodSavedCityOptionArgs(NarrativeMenuOptionArgs args)
		{
			SkillObject[] affectedSkills = new SkillObject[]
			{
				DefaultSkills.Tactics,
				DefaultSkills.Leadership
			};
			args.SetAffectedSkills(affectedSkills);
			args.SetFocusToSkills(this._focusToAdd);
			args.SetLevelToSkills(this._skillLevelToAdd);
			args.SetLevelToAttribute(DefaultCharacterAttributes.Cunning, this._attributeLevelToAdd);
			TraitObject[] affectedTraits = new TraitObject[] { DefaultTraits.Calculating };
			args.SetAffectedTraits(affectedTraits);
			args.SetLevelToTraits(1);
			args.SetRenownToAdd(10);
		}

		// Token: 0x06003C0A RID: 15370 RVA: 0x000FF33D File Offset: 0x000FD53D
		private bool AdulthoodSavedCityOptionOnCondition(CharacterCreationManager characterCreationManager)
		{
			return CharacterCreationCampaignBehavior.CharacterOccupationTypes.IsUrbanOccupation(characterCreationManager.CharacterCreationContent.SelectedParentOccupation) && characterCreationManager.CharacterCreationContent.SelectedCulture.StringId == "battania";
		}

		// Token: 0x06003C0B RID: 15371 RVA: 0x000FF370 File Offset: 0x000FD570
		private void AdulthoodSavedCityOptionOnSelect(CharacterCreationManager characterCreationManager)
		{
			foreach (NarrativeMenuCharacter narrativeMenuCharacter in characterCreationManager.CurrentMenu.Characters)
			{
				if (narrativeMenuCharacter.StringId == "player_adulthood_character")
				{
					narrativeMenuCharacter.SetAnimationId("act_childhood_vibrant");
				}
			}
		}

		// Token: 0x06003C0C RID: 15372 RVA: 0x000FF3E0 File Offset: 0x000FD5E0
		private void GetAdulthoodWorkshopOptionArgs(NarrativeMenuOptionArgs args)
		{
			SkillObject[] affectedSkills = new SkillObject[]
			{
				DefaultSkills.Trade,
				DefaultSkills.Crafting
			};
			args.SetAffectedSkills(affectedSkills);
			args.SetFocusToSkills(this._focusToAdd);
			args.SetLevelToSkills(this._skillLevelToAdd);
			args.SetLevelToAttribute(DefaultCharacterAttributes.Intelligence, this._attributeLevelToAdd);
			TraitObject[] affectedTraits = new TraitObject[] { DefaultTraits.Calculating };
			args.SetAffectedTraits(affectedTraits);
			args.SetLevelToTraits(1);
			args.SetRenownToAdd(10);
		}

		// Token: 0x06003C0D RID: 15373 RVA: 0x000FF459 File Offset: 0x000FD659
		private bool AdulthoodWorkshopOptionOnCondition(CharacterCreationManager characterCreationManager)
		{
			return CharacterCreationCampaignBehavior.CharacterOccupationTypes.IsUrbanOccupation(characterCreationManager.CharacterCreationContent.SelectedParentOccupation);
		}

		// Token: 0x06003C0E RID: 15374 RVA: 0x000FF46C File Offset: 0x000FD66C
		private void AdulthoodWorkshopOptionOnSelect(CharacterCreationManager characterCreationManager)
		{
			foreach (NarrativeMenuCharacter narrativeMenuCharacter in characterCreationManager.CurrentMenu.Characters)
			{
				if (narrativeMenuCharacter.StringId == "player_adulthood_character")
				{
					narrativeMenuCharacter.SetAnimationId("act_childhood_decisive");
				}
			}
		}

		// Token: 0x06003C0F RID: 15375 RVA: 0x000FF4DC File Offset: 0x000FD6DC
		private void GetAdulthoodInvestorOptionArgs(NarrativeMenuOptionArgs args)
		{
			SkillObject[] affectedSkills = new SkillObject[]
			{
				DefaultSkills.Trade,
				DefaultSkills.Crafting
			};
			args.SetAffectedSkills(affectedSkills);
			args.SetFocusToSkills(this._focusToAdd);
			args.SetLevelToSkills(this._skillLevelToAdd);
			args.SetLevelToAttribute(DefaultCharacterAttributes.Intelligence, this._attributeLevelToAdd);
			TraitObject[] affectedTraits = new TraitObject[] { DefaultTraits.Calculating };
			args.SetAffectedTraits(affectedTraits);
			args.SetLevelToTraits(1);
			args.SetRenownToAdd(10);
		}

		// Token: 0x06003C10 RID: 15376 RVA: 0x000FF555 File Offset: 0x000FD755
		private bool AdulthoodInvestorOptionOnCondition(CharacterCreationManager characterCreationManager)
		{
			return !CharacterCreationCampaignBehavior.CharacterOccupationTypes.IsUrbanOccupation(characterCreationManager.CharacterCreationContent.SelectedParentOccupation);
		}

		// Token: 0x06003C11 RID: 15377 RVA: 0x000FF56C File Offset: 0x000FD76C
		private void AdulthoodInvestorOptionOnSelect(CharacterCreationManager characterCreationManager)
		{
			foreach (NarrativeMenuCharacter narrativeMenuCharacter in characterCreationManager.CurrentMenu.Characters)
			{
				if (narrativeMenuCharacter.StringId == "player_adulthood_character")
				{
					narrativeMenuCharacter.SetAnimationId("act_childhood_decisive");
				}
			}
		}

		// Token: 0x06003C12 RID: 15378 RVA: 0x000FF5DC File Offset: 0x000FD7DC
		private void GetAdulthoodHunterOptionArgs(NarrativeMenuOptionArgs args)
		{
			SkillObject[] affectedSkills = new SkillObject[]
			{
				DefaultSkills.Polearm,
				DefaultSkills.Athletics
			};
			args.SetAffectedSkills(affectedSkills);
			args.SetFocusToSkills(this._focusToAdd);
			args.SetLevelToSkills(this._skillLevelToAdd);
			args.SetLevelToAttribute(DefaultCharacterAttributes.Control, this._attributeLevelToAdd);
			TraitObject[] affectedTraits = new TraitObject[] { DefaultTraits.Valor };
			args.SetAffectedTraits(affectedTraits);
			args.SetLevelToTraits(1);
			args.SetRenownToAdd(5);
		}

		// Token: 0x06003C13 RID: 15379 RVA: 0x000FF654 File Offset: 0x000FD854
		private bool AdulthoodHunterOptionOnCondition(CharacterCreationManager characterCreationManager)
		{
			return !CharacterCreationCampaignBehavior.CharacterOccupationTypes.IsUrbanOccupation(characterCreationManager.CharacterCreationContent.SelectedParentOccupation);
		}

		// Token: 0x06003C14 RID: 15380 RVA: 0x000FF66C File Offset: 0x000FD86C
		private void AdulthoodHunterOptionOnSelect(CharacterCreationManager characterCreationManager)
		{
			foreach (NarrativeMenuCharacter narrativeMenuCharacter in characterCreationManager.CurrentMenu.Characters)
			{
				if (narrativeMenuCharacter.StringId == "player_adulthood_character")
				{
					narrativeMenuCharacter.SetAnimationId("act_childhood_tough");
				}
			}
		}

		// Token: 0x06003C15 RID: 15381 RVA: 0x000FF6DC File Offset: 0x000FD8DC
		private void GetAdulthoodSiegeSurvivorOptionArgs(NarrativeMenuOptionArgs args)
		{
			SkillObject[] affectedSkills = new SkillObject[]
			{
				DefaultSkills.Bow,
				DefaultSkills.Crossbow
			};
			args.SetAffectedSkills(affectedSkills);
			args.SetFocusToSkills(this._focusToAdd);
			args.SetLevelToSkills(this._skillLevelToAdd);
			args.SetLevelToAttribute(DefaultCharacterAttributes.Control, this._attributeLevelToAdd);
			args.SetRenownToAdd(5);
		}

		// Token: 0x06003C16 RID: 15382 RVA: 0x000FF737 File Offset: 0x000FD937
		private bool AdulthoodSiegeSurvivorOptionOnCondition(CharacterCreationManager characterCreationManager)
		{
			return CharacterCreationCampaignBehavior.CharacterOccupationTypes.IsUrbanOccupation(characterCreationManager.CharacterCreationContent.SelectedParentOccupation);
		}

		// Token: 0x06003C17 RID: 15383 RVA: 0x000FF74C File Offset: 0x000FD94C
		private void AdulthoodSiegeSurvivorOptionOnSelect(CharacterCreationManager characterCreationManager)
		{
			foreach (NarrativeMenuCharacter narrativeMenuCharacter in characterCreationManager.CurrentMenu.Characters)
			{
				if (narrativeMenuCharacter.StringId == "player_adulthood_character")
				{
					narrativeMenuCharacter.SetAnimationId("act_childhood_tough");
				}
			}
		}

		// Token: 0x06003C18 RID: 15384 RVA: 0x000FF7BC File Offset: 0x000FD9BC
		private void GetAdulthoodEscapadeHighRegisterOptionArgs(NarrativeMenuOptionArgs args)
		{
			SkillObject[] affectedSkills = new SkillObject[]
			{
				DefaultSkills.Athletics,
				DefaultSkills.Roguery
			};
			args.SetAffectedSkills(affectedSkills);
			args.SetFocusToSkills(this._focusToAdd);
			args.SetLevelToSkills(this._skillLevelToAdd);
			args.SetLevelToAttribute(DefaultCharacterAttributes.Endurance, this._attributeLevelToAdd);
			TraitObject[] affectedTraits = new TraitObject[] { DefaultTraits.Valor };
			args.SetAffectedTraits(affectedTraits);
			args.SetLevelToTraits(1);
			args.SetRenownToAdd(5);
		}

		// Token: 0x06003C19 RID: 15385 RVA: 0x000FF834 File Offset: 0x000FDA34
		private bool AdulthoodEscapadeHighRegisterOptionOnCondition(CharacterCreationManager characterCreationManager)
		{
			return !CharacterCreationCampaignBehavior.CharacterOccupationTypes.IsUrbanOccupation(characterCreationManager.CharacterCreationContent.SelectedParentOccupation);
		}

		// Token: 0x06003C1A RID: 15386 RVA: 0x000FF84C File Offset: 0x000FDA4C
		private void AdulthoodEscapadeHighRegisterOptionOnSelect(CharacterCreationManager characterCreationManager)
		{
			foreach (NarrativeMenuCharacter narrativeMenuCharacter in characterCreationManager.CurrentMenu.Characters)
			{
				if (narrativeMenuCharacter.StringId == "player_adulthood_character")
				{
					narrativeMenuCharacter.SetAnimationId("act_childhood_clever");
				}
			}
		}

		// Token: 0x06003C1B RID: 15387 RVA: 0x000FF8BC File Offset: 0x000FDABC
		private void GetAdulthoodEscapadeLowRegisterOptionArgs(NarrativeMenuOptionArgs args)
		{
			SkillObject[] affectedSkills = new SkillObject[]
			{
				DefaultSkills.Athletics,
				DefaultSkills.Roguery
			};
			args.SetAffectedSkills(affectedSkills);
			args.SetFocusToSkills(this._focusToAdd);
			args.SetLevelToSkills(this._skillLevelToAdd);
			args.SetLevelToAttribute(DefaultCharacterAttributes.Endurance, this._attributeLevelToAdd);
			TraitObject[] affectedTraits = new TraitObject[] { DefaultTraits.Valor };
			args.SetAffectedTraits(affectedTraits);
			args.SetLevelToTraits(1);
			args.SetRenownToAdd(5);
		}

		// Token: 0x06003C1C RID: 15388 RVA: 0x000FF934 File Offset: 0x000FDB34
		private bool AdulthoodEscapadeLowRegisterOptionOnCondition(CharacterCreationManager characterCreationManager)
		{
			return CharacterCreationCampaignBehavior.CharacterOccupationTypes.IsUrbanOccupation(characterCreationManager.CharacterCreationContent.SelectedParentOccupation);
		}

		// Token: 0x06003C1D RID: 15389 RVA: 0x000FF948 File Offset: 0x000FDB48
		private void AdulthoodEscapadeLowRegisterOptionOnSelect(CharacterCreationManager characterCreationManager)
		{
			foreach (NarrativeMenuCharacter narrativeMenuCharacter in characterCreationManager.CurrentMenu.Characters)
			{
				if (narrativeMenuCharacter.StringId == "player_adulthood_character")
				{
					narrativeMenuCharacter.SetAnimationId("act_childhood_clever");
				}
			}
		}

		// Token: 0x06003C1E RID: 15390 RVA: 0x000FF9B8 File Offset: 0x000FDBB8
		private void GetAdulthoodNicePersonOptionArgs(NarrativeMenuOptionArgs args)
		{
			SkillObject[] affectedSkills = new SkillObject[]
			{
				DefaultSkills.Charm,
				DefaultSkills.Steward
			};
			args.SetAffectedSkills(affectedSkills);
			args.SetFocusToSkills(this._focusToAdd);
			args.SetLevelToSkills(this._skillLevelToAdd);
			args.SetLevelToAttribute(DefaultCharacterAttributes.Social, this._attributeLevelToAdd);
			TraitObject[] affectedTraits = new TraitObject[]
			{
				DefaultTraits.Mercy,
				DefaultTraits.Generosity,
				DefaultTraits.Honor
			};
			args.SetAffectedTraits(affectedTraits);
			args.SetLevelToTraits(1);
			args.SetRenownToAdd(5);
		}

		// Token: 0x06003C1F RID: 15391 RVA: 0x000FFA40 File Offset: 0x000FDC40
		private bool AdulthoodNicePersonOptionOnCondition(CharacterCreationManager characterCreationManager)
		{
			return true;
		}

		// Token: 0x06003C20 RID: 15392 RVA: 0x000FFA44 File Offset: 0x000FDC44
		private void AdulthoodNicePersonOptionOnSelect(CharacterCreationManager characterCreationManager)
		{
			foreach (NarrativeMenuCharacter narrativeMenuCharacter in characterCreationManager.CurrentMenu.Characters)
			{
				if (narrativeMenuCharacter.StringId == "player_adulthood_character")
				{
					narrativeMenuCharacter.SetAnimationId("act_childhood_manners");
				}
			}
		}

		// Token: 0x06003C21 RID: 15393 RVA: 0x000FFAB4 File Offset: 0x000FDCB4
		private List<NarrativeMenuCharacterArgs> GetAgeSelectionMenuNarrativeMenuCharacterArgs(CultureObject culture, string occupationType, CharacterCreationManager characterCreationManager)
		{
			List<NarrativeMenuCharacterArgs> list = new List<NarrativeMenuCharacterArgs>();
			string playerEquipmentId = this.GetPlayerEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedTitleType, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId, Hero.MainHero.IsFemale);
			list.Add(new NarrativeMenuCharacterArgs("player_age_selection_character", characterCreationManager.CharacterCreationContent.StartingAge, playerEquipmentId, "act_childhood_schooled", "spawnpoint_player_1", "", "", null, true, CharacterObject.PlayerCharacter.IsFemale));
			MBEquipmentRoster @object = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(playerEquipmentId);
			ItemObject item = @object.DefaultEquipment[EquipmentIndex.ArmorItemEndSlot].Item;
			list.Add(new NarrativeMenuCharacterArgs("narrative_character_horse", -1, "", "act_horse_stand_1", "spawnpoint_mount_1", @object.DefaultEquipment[EquipmentIndex.ArmorItemEndSlot].Item.StringId, @object.DefaultEquipment[EquipmentIndex.HorseHarness].Item.StringId, MountCreationKey.GetRandomMountKey(item, CharacterObject.PlayerCharacter.GetMountKeySeed()), false, false));
			return list;
		}

		// Token: 0x06003C22 RID: 15394 RVA: 0x000FFBBC File Offset: 0x000FDDBC
		private void AddAgeSelectionMenu(CharacterCreationManager characterCreationManager)
		{
			MBTextManager.SetTextVariable("EXP_VALUE", this._skillLevelToAdd);
			BodyProperties bodyProperties = CharacterObject.PlayerCharacter.GetBodyProperties(CharacterObject.PlayerCharacter.Equipment, -1);
			bodyProperties = FaceGen.GetBodyPropertiesWithAge(ref bodyProperties, (float)characterCreationManager.CharacterCreationContent.StartingAge);
			NarrativeMenuCharacter item = new NarrativeMenuCharacter("player_age_selection_character", bodyProperties, CharacterObject.PlayerCharacter.Race, CharacterObject.PlayerCharacter.IsFemale);
			NarrativeMenuCharacter item2 = new NarrativeMenuCharacter("narrative_character_horse");
			List<NarrativeMenuCharacter> list = new List<NarrativeMenuCharacter>();
			list.Add(item);
			list.Add(item2);
			NarrativeMenu narrativeMenu = new NarrativeMenu("narrative_age_selection_menu", "narrative_adulthood_menu", "", new TextObject("{=HDFEAYDk}Starting Age", null), new TextObject("{=VlOGrGSn}Your character started off on the adventuring path at the age of...", null), list, new NarrativeMenu.GetNarrativeMenuCharacterArgsDelegate(this.GetAgeSelectionMenuNarrativeMenuCharacterArgs));
			NarrativeMenuOption narrativeMenuOption = new NarrativeMenuOption("age_selection_young_adult_option", new TextObject("{=!}20", null), new TextObject("{=2k7adlh7}While lacking experience a bit, you are full with youthful energy, you are fully eager, for the long years of adventuring ahead.", null), new GetNarrativeMenuOptionArgsDelegate(this.GetAgeSelectionYoungAdultAgeOptionArgs), new NarrativeMenuOptionOnConditionDelegate(this.AgeSelectionYoungAdultAgeOptionOnCondition), new NarrativeMenuOptionOnSelectDelegate(this.AgeSelectionYoungAdultAgeOptionOnSelect), new NarrativeMenuOptionOnConsequenceDelegate(this.AgeSelectionYoungAdultAgeOptionOnConsequence));
			narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption);
			NarrativeMenuOption narrativeMenuOption2 = new NarrativeMenuOption("age_selection_adult_option", new TextObject("{=!}30", null), new TextObject("{=NUlVFRtK}You are at your prime, You still have some youthful energy but also have a substantial amount of experience under your belt. ", null), new GetNarrativeMenuOptionArgsDelegate(this.GetAgeSelectionAdultOptionArgs), new NarrativeMenuOptionOnConditionDelegate(this.AgeSelectionAdultOptionOnCondition), new NarrativeMenuOptionOnSelectDelegate(this.AgeSelectionAdultOptionOnSelect), new NarrativeMenuOptionOnConsequenceDelegate(this.AgeSelectionAdultOptionOnConsequence));
			narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption2);
			NarrativeMenuOption narrativeMenuOption3 = new NarrativeMenuOption("age_selection_middle_age_option", new TextObject("{=!}40", null), new TextObject("{=5MxTYApM}This is the right age for starting off, you have years of experience, and you are old enough for people to respect you and gather under your banner.", null), new GetNarrativeMenuOptionArgsDelegate(this.GetAgeSelectionMiddleAgeOptionArgs), new NarrativeMenuOptionOnConditionDelegate(this.AgeSelectionMiddleAgeOptionOnCondition), new NarrativeMenuOptionOnSelectDelegate(this.AgeSelectionMiddleAgeOptionOnSelect), new NarrativeMenuOptionOnConsequenceDelegate(this.AgeSelectionMiddleAgeOptionOnConsequence));
			narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption3);
			NarrativeMenuOption narrativeMenuOption4 = new NarrativeMenuOption("age_selection_elder_option", new TextObject("{=!}50", null), new TextObject("{=ePD5Afvy}While you are past your prime, there is still enough time to go on that last big adventure for you. And you have all the experience you need to overcome anything!", null), new GetNarrativeMenuOptionArgsDelegate(this.GetAgeSelectionElderOptionArgs), new NarrativeMenuOptionOnConditionDelegate(this.AgeSelectionElderOptionOnCondition), new NarrativeMenuOptionOnSelectDelegate(this.AgeSelectionElderOptionOnSelect), new NarrativeMenuOptionOnConsequenceDelegate(this.AgeSelectionElderOptionOnConsequence));
			narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption4);
			characterCreationManager.AddNewMenu(narrativeMenu);
		}

		// Token: 0x06003C23 RID: 15395 RVA: 0x000FFDEF File Offset: 0x000FDFEF
		private void GetAgeSelectionYoungAdultAgeOptionArgs(NarrativeMenuOptionArgs args)
		{
			args.SetUnspentFocusToAdd(2);
			args.SetUnspentAttributeToAdd(1);
		}

		// Token: 0x06003C24 RID: 15396 RVA: 0x000FFDFF File Offset: 0x000FDFFF
		private bool AgeSelectionYoungAdultAgeOptionOnCondition(CharacterCreationManager characterCreationManager)
		{
			return true;
		}

		// Token: 0x06003C25 RID: 15397 RVA: 0x000FFE04 File Offset: 0x000FE004
		private void AgeSelectionYoungAdultAgeOptionOnSelect(CharacterCreationManager characterCreationManager)
		{
			string playerEquipmentId = this.GetPlayerEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedTitleType, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId, Hero.MainHero.IsFemale);
			foreach (NarrativeMenuCharacter narrativeMenuCharacter in characterCreationManager.CurrentMenu.Characters)
			{
				if (narrativeMenuCharacter.StringId == "player_age_selection_character")
				{
					narrativeMenuCharacter.SetAnimationId("act_childhood_focus");
					narrativeMenuCharacter.ChangeAge(20f);
					MBEquipmentRoster @object = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(playerEquipmentId);
					if (@object == null)
					{
						Debug.FailedAssert("character creation menu character equipment should not be null!", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\CampaignBehaviors\\CharacterCreationCampaignBehavior.cs", "AgeSelectionYoungAdultAgeOptionOnSelect", 4884);
						@object = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>("player_char_creation_default");
					}
					narrativeMenuCharacter.SetEquipment(@object);
					break;
				}
			}
			characterCreationManager.CharacterCreationContent.StartingAge = 20;
			Hero.MainHero.SetBirthDay(CampaignTime.YearsFromNow(-20f));
		}

		// Token: 0x06003C26 RID: 15398 RVA: 0x000FFF1C File Offset: 0x000FE11C
		private void AgeSelectionYoungAdultAgeOptionOnConsequence(CharacterCreationManager characterCreationManager)
		{
			characterCreationManager.CharacterCreationContent.StartingAge = 20;
			this.ApplyMainHeroEquipment(characterCreationManager);
		}

		// Token: 0x06003C27 RID: 15399 RVA: 0x000FFF32 File Offset: 0x000FE132
		private void GetAgeSelectionAdultOptionArgs(NarrativeMenuOptionArgs args)
		{
			args.SetUnspentFocusToAdd(4);
			args.SetUnspentAttributeToAdd(2);
		}

		// Token: 0x06003C28 RID: 15400 RVA: 0x000FFF42 File Offset: 0x000FE142
		private bool AgeSelectionAdultOptionOnCondition(CharacterCreationManager characterCreationManager)
		{
			return true;
		}

		// Token: 0x06003C29 RID: 15401 RVA: 0x000FFF48 File Offset: 0x000FE148
		private void AgeSelectionAdultOptionOnSelect(CharacterCreationManager characterCreationManager)
		{
			string playerEquipmentId = this.GetPlayerEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedTitleType, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId, Hero.MainHero.IsFemale);
			foreach (NarrativeMenuCharacter narrativeMenuCharacter in characterCreationManager.CurrentMenu.Characters)
			{
				if (narrativeMenuCharacter.StringId == "player_age_selection_character")
				{
					narrativeMenuCharacter.SetAnimationId("act_childhood_athlete");
					narrativeMenuCharacter.ChangeAge(30f);
					MBEquipmentRoster @object = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(playerEquipmentId);
					if (@object == null)
					{
						Debug.FailedAssert("character creation menu character equipment should not be null!", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\CampaignBehaviors\\CharacterCreationCampaignBehavior.cs", "AgeSelectionAdultOptionOnSelect", 4934);
						@object = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>("player_char_creation_default");
					}
					narrativeMenuCharacter.SetEquipment(@object);
					break;
				}
			}
			characterCreationManager.CharacterCreationContent.StartingAge = 30;
			Hero.MainHero.SetBirthDay(CampaignTime.YearsFromNow(-30f));
		}

		// Token: 0x06003C2A RID: 15402 RVA: 0x00100060 File Offset: 0x000FE260
		private void AgeSelectionAdultOptionOnConsequence(CharacterCreationManager characterCreationManager)
		{
			characterCreationManager.CharacterCreationContent.StartingAge = 30;
			this.ApplyMainHeroEquipment(characterCreationManager);
		}

		// Token: 0x06003C2B RID: 15403 RVA: 0x00100076 File Offset: 0x000FE276
		private void GetAgeSelectionMiddleAgeOptionArgs(NarrativeMenuOptionArgs args)
		{
			args.SetUnspentFocusToAdd(6);
			args.SetUnspentAttributeToAdd(3);
		}

		// Token: 0x06003C2C RID: 15404 RVA: 0x00100086 File Offset: 0x000FE286
		private bool AgeSelectionMiddleAgeOptionOnCondition(CharacterCreationManager characterCreationManager)
		{
			return true;
		}

		// Token: 0x06003C2D RID: 15405 RVA: 0x0010008C File Offset: 0x000FE28C
		private void AgeSelectionMiddleAgeOptionOnSelect(CharacterCreationManager characterCreationManager)
		{
			string playerEquipmentId = this.GetPlayerEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedTitleType, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId, Hero.MainHero.IsFemale);
			foreach (NarrativeMenuCharacter narrativeMenuCharacter in characterCreationManager.CurrentMenu.Characters)
			{
				if (narrativeMenuCharacter.StringId == "player_age_selection_character")
				{
					narrativeMenuCharacter.SetAnimationId("act_childhood_sharp");
					narrativeMenuCharacter.ChangeAge(40f);
					MBEquipmentRoster @object = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(playerEquipmentId);
					if (@object == null)
					{
						Debug.FailedAssert("character creation menu character equipment should not be null!", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\CampaignBehaviors\\CharacterCreationCampaignBehavior.cs", "AgeSelectionMiddleAgeOptionOnSelect", 4984);
						@object = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>("player_char_creation_default");
					}
					narrativeMenuCharacter.SetEquipment(@object);
					break;
				}
			}
			characterCreationManager.CharacterCreationContent.StartingAge = 40;
			Hero.MainHero.SetBirthDay(CampaignTime.YearsFromNow(-40f));
		}

		// Token: 0x06003C2E RID: 15406 RVA: 0x001001A4 File Offset: 0x000FE3A4
		private void AgeSelectionMiddleAgeOptionOnConsequence(CharacterCreationManager characterCreationManager)
		{
			characterCreationManager.CharacterCreationContent.StartingAge = 40;
			this.ApplyMainHeroEquipment(characterCreationManager);
		}

		// Token: 0x06003C2F RID: 15407 RVA: 0x001001BA File Offset: 0x000FE3BA
		private void GetAgeSelectionElderOptionArgs(NarrativeMenuOptionArgs args)
		{
			args.SetUnspentFocusToAdd(8);
			args.SetUnspentAttributeToAdd(4);
		}

		// Token: 0x06003C30 RID: 15408 RVA: 0x001001CA File Offset: 0x000FE3CA
		private bool AgeSelectionElderOptionOnCondition(CharacterCreationManager characterCreationManager)
		{
			return true;
		}

		// Token: 0x06003C31 RID: 15409 RVA: 0x001001D0 File Offset: 0x000FE3D0
		private void AgeSelectionElderOptionOnSelect(CharacterCreationManager characterCreationManager)
		{
			string playerEquipmentId = this.GetPlayerEquipmentId(characterCreationManager, characterCreationManager.CharacterCreationContent.SelectedTitleType, characterCreationManager.CharacterCreationContent.SelectedCulture.StringId, Hero.MainHero.IsFemale);
			foreach (NarrativeMenuCharacter narrativeMenuCharacter in characterCreationManager.CurrentMenu.Characters)
			{
				if (narrativeMenuCharacter.StringId == "player_age_selection_character")
				{
					narrativeMenuCharacter.SetAnimationId("act_childhood_tough");
					narrativeMenuCharacter.ChangeAge(50f);
					MBEquipmentRoster @object = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(playerEquipmentId);
					if (@object == null)
					{
						Debug.FailedAssert("character creation menu character equipment should not be null!", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\CampaignBehaviors\\CharacterCreationCampaignBehavior.cs", "AgeSelectionElderOptionOnSelect", 5034);
						@object = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>("player_char_creation_default");
					}
					narrativeMenuCharacter.SetEquipment(@object);
					break;
				}
			}
			characterCreationManager.CharacterCreationContent.StartingAge = 50;
			Hero.MainHero.SetBirthDay(CampaignTime.YearsFromNow(-50f));
		}

		// Token: 0x06003C32 RID: 15410 RVA: 0x001002E8 File Offset: 0x000FE4E8
		private void AgeSelectionElderOptionOnConsequence(CharacterCreationManager characterCreationManager)
		{
			characterCreationManager.CharacterCreationContent.StartingAge = 50;
			this.ApplyMainHeroEquipment(characterCreationManager);
		}

		// Token: 0x06003C33 RID: 15411 RVA: 0x00100300 File Offset: 0x000FE500
		private void ApplyMainHeroEquipment(CharacterCreationManager characterCreationManager)
		{
			NarrativeMenu narrativeMenuWithId = characterCreationManager.GetNarrativeMenuWithId("narrative_age_selection_menu");
			NarrativeMenuCharacter narrativeMenuCharacter = null;
			foreach (NarrativeMenuCharacter narrativeMenuCharacter2 in narrativeMenuWithId.Characters)
			{
				if (narrativeMenuCharacter2.StringId.Equals("player_age_selection_character"))
				{
					narrativeMenuCharacter = narrativeMenuCharacter2;
					break;
				}
			}
			CharacterObject.PlayerCharacter.Equipment.FillFrom(narrativeMenuCharacter.Equipment.DefaultEquipment, true);
			CharacterObject.PlayerCharacter.FirstCivilianEquipment.FillFrom(narrativeMenuCharacter.Equipment.GetRandomCivilianEquipment(), true);
		}

		// Token: 0x06003C34 RID: 15412 RVA: 0x001003A4 File Offset: 0x000FE5A4
		public void SetHeroAge(float age)
		{
			Hero.MainHero.SetBirthDay(CampaignTime.YearsFromNow(-age));
		}

		// Token: 0x0400122C RID: 4652
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

		// Token: 0x0400122D RID: 4653
		private const int ChildhoodAge = 7;

		// Token: 0x0400122E RID: 4654
		private const int EducationAge = 12;

		// Token: 0x0400122F RID: 4655
		private const int YouthAge = 17;

		// Token: 0x04001230 RID: 4656
		private const int AccomplishmentAge = 20;

		// Token: 0x04001231 RID: 4657
		private const int ParentAge = 33;

		// Token: 0x04001232 RID: 4658
		private const int YoungAdultAge = 20;

		// Token: 0x04001233 RID: 4659
		private const int AdultAge = 30;

		// Token: 0x04001234 RID: 4660
		private const int MiddleAge = 40;

		// Token: 0x04001235 RID: 4661
		private const int ElderAge = 50;

		// Token: 0x04001236 RID: 4662
		public const int FocusToAddYouthStart = 2;

		// Token: 0x04001237 RID: 4663
		public const int FocusToAddAdultStart = 4;

		// Token: 0x04001238 RID: 4664
		public const int FocusToAddMiddleAgedStart = 6;

		// Token: 0x04001239 RID: 4665
		public const int FocusToAddElderlyStart = 8;

		// Token: 0x0400123A RID: 4666
		public const int AttributeToAddYouthStart = 1;

		// Token: 0x0400123B RID: 4667
		public const int AttributeToAddAdultStart = 2;

		// Token: 0x0400123C RID: 4668
		public const int AttributeToAddMiddleAgedStart = 3;

		// Token: 0x0400123D RID: 4669
		public const int AttributeToAddElderlyStart = 4;

		// Token: 0x0400123E RID: 4670
		public const string MotherNarrativeCharacterStringId = "mother_character";

		// Token: 0x0400123F RID: 4671
		public const string FatherNarrativeCharacterStringId = "father_character";

		// Token: 0x04001240 RID: 4672
		public const string PlayerChildhoodCharacterStringId = "player_childhood_character";

		// Token: 0x04001241 RID: 4673
		public const string PlayerEducationCharacterStringId = "player_education_character";

		// Token: 0x04001242 RID: 4674
		public const string PlayerYouthCharacterStringId = "player_youth_character";

		// Token: 0x04001243 RID: 4675
		public const string PlayerAdulthoodCharacterStringId = "player_adulthood_character";

		// Token: 0x04001244 RID: 4676
		public const string PlayerAgeSelectionCharacterStringId = "player_age_selection_character";

		// Token: 0x04001245 RID: 4677
		public const string HorseNarrativeCharacterStringId = "narrative_character_horse";

		// Token: 0x04001246 RID: 4678
		private int _focusToAdd = 1;

		// Token: 0x04001247 RID: 4679
		private int _skillLevelToAdd = 10;

		// Token: 0x04001248 RID: 4680
		private int _attributeLevelToAdd = 1;

		// Token: 0x020007C2 RID: 1986
		private static class CharacterOccupationTypes
		{
			// Token: 0x0600627F RID: 25215 RVA: 0x001BB1E8 File Offset: 0x001B93E8
			public static bool IsUrbanOccupation(string occupation)
			{
				return occupation == "retainer_urban" || occupation == "mercenary_urban" || occupation == "merchant_urban" || occupation == "vagabond_urban" || occupation == "artisan_urban" || occupation == "physician_urban" || occupation == "healer_urban" || occupation == "bard_urban";
			}

			// Token: 0x04001EF1 RID: 7921
			public const string Retainer = "retainer";

			// Token: 0x04001EF2 RID: 7922
			public const string Bard = "bard";

			// Token: 0x04001EF3 RID: 7923
			public const string Hunter = "hunter";

			// Token: 0x04001EF4 RID: 7924
			public const string Farmer = "farmer";

			// Token: 0x04001EF5 RID: 7925
			public const string Herder = "herder";

			// Token: 0x04001EF6 RID: 7926
			public const string Healer = "healer";

			// Token: 0x04001EF7 RID: 7927
			public const string Mercenary = "mercenary";

			// Token: 0x04001EF8 RID: 7928
			public const string Infantry = "infantry";

			// Token: 0x04001EF9 RID: 7929
			public const string Skirmisher = "skirmisher";

			// Token: 0x04001EFA RID: 7930
			public const string Kern = "kern";

			// Token: 0x04001EFB RID: 7931
			public const string Guard = "guard";

			// Token: 0x04001EFC RID: 7932
			public const string RetainerUrban = "retainer_urban";

			// Token: 0x04001EFD RID: 7933
			public const string MercenaryUrban = "mercenary_urban";

			// Token: 0x04001EFE RID: 7934
			public const string MerchantUrban = "merchant_urban";

			// Token: 0x04001EFF RID: 7935
			public const string VagabondUrban = "vagabond_urban";

			// Token: 0x04001F00 RID: 7936
			public const string ArtisanUrban = "artisan_urban";

			// Token: 0x04001F01 RID: 7937
			public const string PhysicianUrban = "physician_urban";

			// Token: 0x04001F02 RID: 7938
			public const string HealerUrban = "healer_urban";

			// Token: 0x04001F03 RID: 7939
			public const string BardUrban = "bard_urban";
		}
	}
}
