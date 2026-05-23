using System.Collections.Generic;
using Helpers;
using StoryMode.StoryModeObjects;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterCreationContent;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace StoryMode.GameComponents.CampaignBehaviors;

public class StoryModeCharacterCreationCampaignBehavior : CampaignBehaviorBase, ICharacterCreationContentHandler
{
	private const string BrotherNarrativeCharacterStringId = "brother_character";

	private const string PlayerEscapeNarrativeCharacterStringId = "player_escape_character";

	private int _focusToAdd = 1;

	private int _skillLevelToAdd = 10;

	private int _attributeLevelToAdd = 1;

	private CharacterCreationManager _characterCreationManager => (GameStateManager.Current.ActiveState as CharacterCreationState)?.CharacterCreationManager;

	public override void RegisterEvents()
	{
		CampaignEvents.OnCharacterCreationInitializedEvent.AddNonSerializedListener(this, OnCharacterCreationInitialized);
		CampaignEvents.OnCharacterCreationIsOverEvent.AddNonSerializedListener(this, OnCharacterCreationIsOver);
		CampaignEvents.OnGameLoadFinishedEvent.AddNonSerializedListener(this, OnGameLoadFinished);
	}

	private void OnGameLoadFinished()
	{
		if (!MBSaveLoad.LastLoadedGameVersion.IsOlderThan(ApplicationVersion.FromString("v1.3.1.52060")) || !(Hero.MainHero.StringId == "main_hero"))
		{
			return;
		}
		if (Hero.MainHero.Father == null)
		{
			Hero.MainHero.Father = StoryModeHeroes.MainHeroFather;
		}
		if (Hero.MainHero.Mother == null)
		{
			Hero.MainHero.Mother = StoryModeHeroes.MainHeroMother;
		}
		if (!Hero.MainHero.Father.IsDead && !Hero.MainHero.Mother.IsDead)
		{
			if (Hero.MainHero.Father.Spouse == null)
			{
				Hero.MainHero.Father.Spouse = Hero.MainHero.Mother;
			}
			if (Hero.MainHero.Mother.Spouse == null)
			{
				Hero.MainHero.Mother.Spouse = Hero.MainHero.Father;
			}
		}
	}

	public override void SyncData(IDataStore dataStore)
	{
	}

	private void OnCharacterCreationIsOver()
	{
		UpdateHomeSettlementsOfFamily();
		FinalizeFamilyStory();
	}

	private void UpdateHomeSettlementsOfFamily()
	{
		Settlement homeSettlement = Hero.MainHero.HomeSettlement;
		StoryModeHeroes.MainHeroFather.BornSettlement = homeSettlement;
		StoryModeHeroes.MainHeroFather.UpdateHomeSettlement();
		StoryModeHeroes.MainHeroMother.BornSettlement = homeSettlement;
		StoryModeHeroes.MainHeroMother.UpdateHomeSettlement();
		StoryModeHeroes.LittleBrother.BornSettlement = homeSettlement;
		StoryModeHeroes.LittleBrother.UpdateHomeSettlement();
		StoryModeHeroes.LittleSister.BornSettlement = homeSettlement;
		StoryModeHeroes.LittleSister.UpdateHomeSettlement();
		StoryModeHeroes.ElderBrother.BornSettlement = homeSettlement;
		StoryModeHeroes.ElderBrother.UpdateHomeSettlement();
	}

	private void FinalizeFamilyStory()
	{
		TextObject textObject = new TextObject("{=h68qCoz3}{PLAYER_LITTLE_BROTHER.NAME} is the little brother of {PLAYER.LINK}. He has been abducted by bandits, who intend to sell him into slavery.");
		StringHelpers.SetCharacterProperties("PLAYER_LITTLE_BROTHER", StoryModeHeroes.LittleBrother.CharacterObject, textObject);
		StringHelpers.SetCharacterProperties("PLAYER", CharacterObject.PlayerCharacter, textObject);
		StoryModeHeroes.LittleBrother.EncyclopediaText = textObject;
		TextObject textObject2 = GameTexts.FindText("little_sister_encyclopedia_text");
		StringHelpers.SetCharacterProperties("PLAYER_LITTLE_SISTER", StoryModeHeroes.LittleSister.CharacterObject, textObject2);
		StringHelpers.SetCharacterProperties("PLAYER", CharacterObject.PlayerCharacter, textObject2);
		StoryModeHeroes.LittleSister.EncyclopediaText = textObject2;
		TextObject textObject3 = new TextObject("{=XmvaRfLM}{PLAYER_FATHER.NAME} was the father of {PLAYER.LINK}. He was slain when raiders attacked the inn at which his family was staying.");
		StringHelpers.SetCharacterProperties("PLAYER_FATHER", StoryModeHeroes.MainHeroFather.CharacterObject, textObject3);
		StringHelpers.SetCharacterProperties("PLAYER", CharacterObject.PlayerCharacter, textObject3);
		StoryModeHeroes.MainHeroFather.EncyclopediaText = textObject3;
		TextObject textObject4 = new TextObject("{=hrhvEWP8}{PLAYER_MOTHER.NAME} was the mother of {PLAYER.LINK}. She was slain when raiders attacked the inn at which her family was staying.");
		StringHelpers.SetCharacterProperties("PLAYER_MOTHER", StoryModeHeroes.MainHeroMother.CharacterObject, textObject4);
		StringHelpers.SetCharacterProperties("PLAYER", CharacterObject.PlayerCharacter, textObject4);
		StoryModeHeroes.MainHeroMother.EncyclopediaText = textObject4;
		TextObject textObject5 = new TextObject("{=bsWSecYa}{PLAYER_BROTHER.NAME} is the elder brother of {PLAYER.LINK}. He has gone in search of the family's two youngest siblings, {PLAYER_LITTLE_BROTHER.NAME} and {PLAYER_LITTLE_SISTER.NAME}.");
		StringHelpers.SetCharacterProperties("PLAYER_BROTHER", StoryModeHeroes.ElderBrother.CharacterObject, textObject5);
		StringHelpers.SetCharacterProperties("PLAYER", CharacterObject.PlayerCharacter, textObject5);
		StringHelpers.SetCharacterProperties("PLAYER_LITTLE_BROTHER", StoryModeHeroes.LittleBrother.CharacterObject, textObject5);
		StringHelpers.SetCharacterProperties("PLAYER_LITTLE_SISTER", StoryModeHeroes.LittleSister.CharacterObject, textObject5);
		StoryModeHeroes.ElderBrother.EncyclopediaText = textObject5;
	}

	private void OnCharacterCreationInitialized(CharacterCreationManager characterCreationManager)
	{
		_focusToAdd = characterCreationManager.CharacterCreationContent.FocusToAdd;
		_skillLevelToAdd = characterCreationManager.CharacterCreationContent.SkillLevelToAdd;
		_attributeLevelToAdd = characterCreationManager.CharacterCreationContent.AttributeLevelToAdd;
		characterCreationManager.RegisterCharacterCreationContentHandler(this, 900);
	}

	public void InitializeCharacterCreationStages(CharacterCreationManager characterCreationManager)
	{
		characterCreationManager.RemoveStage<CharacterCreationBannerEditorStage>();
		characterCreationManager.RemoveStage<CharacterCreationClanNamingStage>();
	}

	public void InitializeData(CharacterCreationManager characterCreationManager)
	{
		Hero.MainHero.Mother = StoryModeHeroes.MainHeroMother;
		Hero.MainHero.Father = StoryModeHeroes.MainHeroFather;
		characterCreationManager.CharacterCreationContent.ChangeReviewPageDescription(new TextObject("{=wbhKgpmr}You prepare to set off with your brother on a mission of vengeance and rescue. Here is your character. Continue if you are ready, or go back to make changes."));
		characterCreationManager.DeleteNarrativeMenuWithId("narrative_age_selection_menu");
		AddEscapeMenu(characterCreationManager);
	}

	void ICharacterCreationContentHandler.InitializeContent(CharacterCreationManager characterCreationManager)
	{
		InitializeCharacterCreationStages(characterCreationManager);
		InitializeData(characterCreationManager);
	}

	void ICharacterCreationContentHandler.AfterInitializeContent(CharacterCreationManager characterCreationManager)
	{
		ModifyParentMenu(characterCreationManager);
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
		ApplyCulture(_characterCreationManager.CharacterCreationContent.SelectedCulture);
	}

	private void ApplyCulture(CultureObject culture)
	{
		StoryModeHeroes.LittleBrother.Culture = culture;
		StoryModeHeroes.LittleSister.Culture = culture;
	}

	private void FaceGenUpdated()
	{
		NarrativeMenu narrativeMenuWithId = _characterCreationManager.GetNarrativeMenuWithId("narrative_parent_menu");
		BodyProperties bodyProperties = BodyProperties.Default;
		BodyProperties bodyProperties2 = BodyProperties.Default;
		foreach (NarrativeMenuCharacter character in narrativeMenuWithId.Characters)
		{
			if (character.StringId == "mother_character")
			{
				bodyProperties = character.BodyProperties;
			}
			if (character.StringId == "father_character")
			{
				bodyProperties2 = character.BodyProperties;
			}
		}
		Hero elderBrother = StoryModeHeroes.ElderBrother;
		uint hashCode = (uint)Hero.MainHero.BodyProperties.GetHashCode();
		string hairTags = Hero.MainHero.Culture.ToString().ToLower() + ",";
		string beardTags = Hero.MainHero.Culture.ToString().ToLower() + ",";
		int seed = Hero.MainHero.RandomIntWithSeed(hashCode, 1, 100);
		CreateSibling(StoryModeHeroes.LittleBrother, bodyProperties, bodyProperties2, hashCode + 1);
		CreateSibling(StoryModeHeroes.LittleSister, bodyProperties, bodyProperties2, hashCode + 2);
		BodyProperties randomBodyProperties = BodyProperties.GetRandomBodyProperties(elderBrother.CharacterObject.Race, elderBrother.IsFemale, bodyProperties, bodyProperties2, 1, seed, hairTags, beardTags, Hero.MainHero.Father.CharacterObject.BodyPropertyRange.TattooTags);
		randomBodyProperties = new BodyProperties(new DynamicBodyProperties(elderBrother.Age, 0.5f, 0.5f), randomBodyProperties.StaticProperties);
		elderBrother.StaticBodyProperties = randomBodyProperties.StaticProperties;
		elderBrother.Weight = randomBodyProperties.Weight;
		elderBrother.Build = randomBodyProperties.Build;
		foreach (NarrativeMenu narrativeMenu in _characterCreationManager.NarrativeMenus)
		{
			foreach (NarrativeMenuCharacter character2 in narrativeMenu.Characters)
			{
				if (character2.StringId.Equals("player_escape_character"))
				{
					character2.UpdateBodyProperties(CharacterObject.PlayerCharacter.GetBodyProperties(null), CharacterObject.PlayerCharacter.Race, isFemale: false);
				}
				if (character2.StringId.Equals("brother_character"))
				{
					character2.UpdateBodyProperties(elderBrother.BodyProperties, CharacterObject.PlayerCharacter.Race, isFemale: false);
				}
			}
		}
	}

	private void ModifyParentMenu(CharacterCreationManager characterCreationManager)
	{
		foreach (NarrativeMenuOption characterCreationMenuOption in characterCreationManager.GetNarrativeMenuWithId("narrative_parent_menu").CharacterCreationMenuOptions)
		{
			characterCreationMenuOption.SetOnConsequence(FinalizeParentsAndLittleSiblings);
		}
	}

	private List<NarrativeMenuCharacterArgs> GetEscapeMenuNarrativeMenuCharacterArgs(CultureObject culture, string occupationType, CharacterCreationManager characterCreationManager)
	{
		List<NarrativeMenuCharacterArgs> obj = new List<NarrativeMenuCharacterArgs>
		{
			new NarrativeMenuCharacterArgs(equipmentId: "brother_char_creation_" + characterCreationManager.CharacterCreationContent.SelectedCulture.StringId, characterId: "brother_character", age: (int)StoryModeHeroes.ElderBrother.Age, animationId: "act_childhood_schooled", spawnPointEntityId: "spawnpoint_brother_brother_stage")
		};
		string text = string.Concat(str3: characterCreationManager.CharacterCreationContent.SelectedTitleType.ToString().ToLower(), str0: "player_char_creation_", str1: characterCreationManager.CharacterCreationContent.SelectedCulture.StringId, str2: "_");
		obj.Add(new NarrativeMenuCharacterArgs(equipmentId: text + (Hero.MainHero.IsFemale ? "_f" : "_m"), characterId: "player_escape_character", age: (int)CharacterObject.PlayerCharacter.Age, animationId: "act_childhood_schooled", spawnPointEntityId: "spawnpoint_player_brother_stage", leftHandItemId: "", rightHandItemId: "", mountCreationKey: null, isHuman: true, isFemale: CharacterObject.PlayerCharacter.IsFemale));
		return obj;
	}

	private void AddEscapeMenu(CharacterCreationManager characterCreationManager)
	{
		MBTextManager.SetTextVariable("EXP_VALUE", _skillLevelToAdd);
		List<NarrativeMenuCharacter> list = new List<NarrativeMenuCharacter>();
		NarrativeMenu narrativeMenuWithId = characterCreationManager.GetNarrativeMenuWithId("narrative_parent_menu");
		BodyProperties bodyProperties = BodyProperties.Default;
		BodyProperties bodyProperties2 = BodyProperties.Default;
		foreach (NarrativeMenuCharacter character in narrativeMenuWithId.Characters)
		{
			if (character.StringId == "mother_character")
			{
				bodyProperties = character.BodyProperties;
			}
			if (character.StringId == "father_character")
			{
				bodyProperties2 = character.BodyProperties;
			}
		}
		Hero elderBrother = StoryModeHeroes.ElderBrother;
		uint hashCode = (uint)Hero.MainHero.BodyProperties.GetHashCode();
		string hairTags = Hero.MainHero.Culture.ToString().ToLower() + ",";
		string beardTags = Hero.MainHero.Culture.ToString().ToLower() + ",";
		int seed = Hero.MainHero.RandomIntWithSeed(hashCode, 1, 100);
		CreateSibling(StoryModeHeroes.LittleBrother, bodyProperties, bodyProperties2, hashCode + 1);
		CreateSibling(StoryModeHeroes.LittleSister, bodyProperties, bodyProperties2, hashCode + 2);
		BodyProperties randomBodyProperties = BodyProperties.GetRandomBodyProperties(elderBrother.CharacterObject.Race, elderBrother.IsFemale, bodyProperties, bodyProperties2, 1, seed, hairTags, beardTags, Hero.MainHero.Father.CharacterObject.BodyPropertyRange.TattooTags);
		randomBodyProperties = new BodyProperties(new DynamicBodyProperties(elderBrother.Age, 0.5f, 0.5f), randomBodyProperties.StaticProperties);
		elderBrother.StaticBodyProperties = randomBodyProperties.StaticProperties;
		elderBrother.Weight = randomBodyProperties.Weight;
		elderBrother.Build = randomBodyProperties.Build;
		NarrativeMenuCharacter item = new NarrativeMenuCharacter("brother_character", randomBodyProperties, elderBrother.CharacterObject.Race, elderBrother.CharacterObject.IsFemale);
		list.Add(item);
		NarrativeMenuCharacter item2 = new NarrativeMenuCharacter("player_escape_character", Hero.MainHero.BodyProperties, CharacterObject.PlayerCharacter.Race, CharacterObject.PlayerCharacter.IsFemale);
		list.Add(item2);
		NarrativeMenu narrativeMenu = new NarrativeMenu("narrative_escape_menu", "narrative_adulthood_menu", "", new TextObject("{=peNBA0WW}Story Background"), new TextObject("{=jg3T5AyE}Like many families in Calradia, your life was upended by war. Your home was ravaged by the passage of army after army. Eventually, you sold your property and set off with your father, mother, brother, and your two younger siblings to a new town you'd heard was safer. But you did not make it. Along the way, the inn at which you were staying was attacked by raiders. Your parents were slain and your two youngest siblings seized, but you and your brother survived because..."), list, GetEscapeMenuNarrativeMenuCharacterArgs);
		AddEscapeNarrativeMenuOptions(narrativeMenu);
		characterCreationManager.AddNewMenu(narrativeMenu);
	}

	private void AddEscapeNarrativeMenuOptions(NarrativeMenu narrativeMenu)
	{
		NarrativeMenuOption narrativeMenuOption = new NarrativeMenuOption("escape_subdued_raider_option", new TextObject("{=6vCHovVH}you subdued a raider."), new TextObject("{=CvBoRaFv}You were able to grab a knife in the confusion of the attack. You stabbed a raider blocking your way."), GetEscapeSubduedRaiderNarrativeOptionArgs, EscapeSubduedRaiderNarrativeOptionOnCondition, EscapeSubduedRaiderNarrativeOptionOnSelect, FinalizeMainHeroAndElderBrother);
		narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption);
		NarrativeMenuOption narrativeMenuOption2 = new NarrativeMenuOption("escape_arrow_option", new TextObject("{=2XhW49TX}you drove them off with arrows."), new TextObject("{=ccf67J3J}You grabbed a bow and sent a few arrows the raiders' way. They took cover, giving you the opportunity to flee with your brother."), GetEscapeArrowNarrativeOptionArgs, EscapeArrowNarrativeOptionOnCondition, EscapeArrowNarrativeOptionOnSelect, FinalizeMainHeroAndElderBrother);
		narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption2);
		NarrativeMenuOption narrativeMenuOption3 = new NarrativeMenuOption("escape_horse_option", new TextObject("{=gOI8lKcl}you rode off on a fast horse."), new TextObject("{=cepWNzEA}Jumping on the two remaining horses in the inn's burning stable, you and your brother broke out of the encircling raiders and rode off."), GetEscapeHorseNarrativeOptionArgs, EscapeHorseNarrativeOptionOnCondition, EscapeHorseNarrativeOptionOnSelect, FinalizeMainHeroAndElderBrother);
		narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption3);
		NarrativeMenuOption narrativeMenuOption4 = new NarrativeMenuOption("escape_tricked_option", new TextObject("{=EdUppdLZ}you tricked the raiders."), new TextObject("{=ZqOvtLBM}In the confusion of the attack you shouted that someone had found treasure in the back room. You then made your way out of the undefended entrance with your brother."), GetEscapeTrickedNarrativeOptionArgs, EscapeTrickedNarrativeOptionOnCondition, EscapeTrickedNarrativeOptionOnSelect, FinalizeMainHeroAndElderBrother);
		narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption4);
		NarrativeMenuOption narrativeMenuOption5 = new NarrativeMenuOption("escape_breakout_option", new TextObject("{=qhAhPWdp}you organized the travelers to break out."), new TextObject("{=Lmfi0cYk}You encouraged the few travellers in the inn to break out in a coordinated fashion. Raiders killed or captured most but you and your brother were able to escape."), GetEscapeBreakOutNarrativeOptionArgs, EscapeBreakOutNarrativeOptionOnCondition, EscapeBreakOutNarrativeOptionOnSelect, FinalizeMainHeroAndElderBrother);
		narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption5);
		NarrativeMenuOption narrativeMenuOption6 = new NarrativeMenuOption("escape_makeshift_fortification_option", new TextObject("{=7AEw4RbK}you threw up makeshift fortifications."), new TextObject("{=Lmfi0cYk}You encouraged the few travellers in the inn to break out in a coordinated fashion. Raiders killed or captured most but you and your brother were able to escape."), GetMakeshiftFortificationNarrativeOptionArgs, MakeshiftFortificationNarrativeOptionOnCondition, MakeshiftFortificationNarrativeOptionOnSelect, FinalizeMainHeroAndElderBrother);
		narrativeMenu.AddNarrativeMenuOption(narrativeMenuOption6);
	}

	private void GetEscapeSubduedRaiderNarrativeOptionArgs(NarrativeMenuOptionArgs args)
	{
		SkillObject[] affectedSkills = new SkillObject[2]
		{
			DefaultSkills.OneHanded,
			DefaultSkills.Athletics
		};
		args.SetAffectedSkills(affectedSkills);
		args.SetFocusToSkills(_focusToAdd);
		args.SetLevelToSkills(_skillLevelToAdd);
		args.SetLevelToAttribute(DefaultCharacterAttributes.Vigor, _attributeLevelToAdd);
	}

	private bool EscapeSubduedRaiderNarrativeOptionOnCondition(CharacterCreationManager characterCreationManager)
	{
		return true;
	}

	private void EscapeSubduedRaiderNarrativeOptionOnSelect(CharacterCreationManager characterCreationManager)
	{
		string animationId = "act_childhood_fierce";
		string animationId2 = "act_childhood_athlete";
		foreach (NarrativeMenuCharacter character in characterCreationManager.CurrentMenu.Characters)
		{
			if (character.StringId.Equals("player_escape_character"))
			{
				character.SetAnimationId(animationId);
			}
			if (character.StringId.Equals("brother_character"))
			{
				character.SetAnimationId(animationId2);
			}
		}
	}

	private void GetEscapeArrowNarrativeOptionArgs(NarrativeMenuOptionArgs args)
	{
		SkillObject[] affectedSkills = new SkillObject[2]
		{
			DefaultSkills.Bow,
			DefaultSkills.Tactics
		};
		args.SetAffectedSkills(affectedSkills);
		args.SetFocusToSkills(_focusToAdd);
		args.SetLevelToSkills(_skillLevelToAdd);
		args.SetLevelToAttribute(DefaultCharacterAttributes.Control, _attributeLevelToAdd);
	}

	private bool EscapeArrowNarrativeOptionOnCondition(CharacterCreationManager characterCreationManager)
	{
		return true;
	}

	private void EscapeArrowNarrativeOptionOnSelect(CharacterCreationManager characterCreationManager)
	{
		string animationId = "act_childhood_athlete";
		string animationId2 = "act_childhood_sharp";
		foreach (NarrativeMenuCharacter character in characterCreationManager.CurrentMenu.Characters)
		{
			if (character.StringId.Equals("player_escape_character"))
			{
				character.SetAnimationId(animationId);
			}
			if (character.StringId.Equals("brother_character"))
			{
				character.SetAnimationId(animationId2);
			}
		}
	}

	private void GetEscapeHorseNarrativeOptionArgs(NarrativeMenuOptionArgs args)
	{
		SkillObject[] affectedSkills = new SkillObject[2]
		{
			DefaultSkills.Riding,
			DefaultSkills.Scouting
		};
		args.SetAffectedSkills(affectedSkills);
		args.SetFocusToSkills(_focusToAdd);
		args.SetLevelToSkills(_skillLevelToAdd);
		args.SetLevelToAttribute(DefaultCharacterAttributes.Endurance, _attributeLevelToAdd);
	}

	private bool EscapeHorseNarrativeOptionOnCondition(CharacterCreationManager characterCreationManager)
	{
		return true;
	}

	private void EscapeHorseNarrativeOptionOnSelect(CharacterCreationManager characterCreationManager)
	{
		string animationId = "act_childhood_tough";
		string animationId2 = "act_childhood_decisive";
		foreach (NarrativeMenuCharacter character in characterCreationManager.CurrentMenu.Characters)
		{
			if (character.StringId.Equals("player_escape_character"))
			{
				character.SetAnimationId(animationId);
			}
			if (character.StringId.Equals("brother_character"))
			{
				character.SetAnimationId(animationId2);
			}
		}
	}

	private void GetEscapeTrickedNarrativeOptionArgs(NarrativeMenuOptionArgs args)
	{
		SkillObject[] affectedSkills = new SkillObject[2]
		{
			DefaultSkills.Roguery,
			DefaultSkills.Tactics
		};
		args.SetAffectedSkills(affectedSkills);
		args.SetFocusToSkills(_focusToAdd);
		args.SetLevelToSkills(_skillLevelToAdd);
		args.SetLevelToAttribute(DefaultCharacterAttributes.Cunning, _attributeLevelToAdd);
	}

	private bool EscapeTrickedNarrativeOptionOnCondition(CharacterCreationManager characterCreationManager)
	{
		return true;
	}

	private void EscapeTrickedNarrativeOptionOnSelect(CharacterCreationManager characterCreationManager)
	{
		string animationId = "act_childhood_ready_handshield";
		string animationId2 = "act_aserai_aserai_mp_archer_idle";
		foreach (NarrativeMenuCharacter character in characterCreationManager.CurrentMenu.Characters)
		{
			if (character.StringId.Equals("player_escape_character"))
			{
				character.SetAnimationId(animationId);
			}
			if (character.StringId.Equals("brother_character"))
			{
				character.SetAnimationId(animationId2);
			}
		}
	}

	private void GetEscapeBreakOutNarrativeOptionArgs(NarrativeMenuOptionArgs args)
	{
		SkillObject[] affectedSkills = new SkillObject[2]
		{
			DefaultSkills.Leadership,
			DefaultSkills.Charm
		};
		args.SetAffectedSkills(affectedSkills);
		args.SetFocusToSkills(_focusToAdd);
		args.SetLevelToSkills(_skillLevelToAdd);
		args.SetLevelToAttribute(DefaultCharacterAttributes.Social, _attributeLevelToAdd);
	}

	private bool EscapeBreakOutNarrativeOptionOnCondition(CharacterCreationManager characterCreationManager)
	{
		return true;
	}

	private void EscapeBreakOutNarrativeOptionOnSelect(CharacterCreationManager characterCreationManager)
	{
		string animationId = "act_childhood_manners";
		string animationId2 = "act_childhood_tough";
		foreach (NarrativeMenuCharacter character in characterCreationManager.CurrentMenu.Characters)
		{
			if (character.StringId.Equals("player_escape_character"))
			{
				character.SetAnimationId(animationId);
			}
			if (character.StringId.Equals("brother_character"))
			{
				character.SetAnimationId(animationId2);
			}
		}
	}

	private void GetMakeshiftFortificationNarrativeOptionArgs(NarrativeMenuOptionArgs args)
	{
		SkillObject[] affectedSkills = new SkillObject[2]
		{
			DefaultSkills.Engineering,
			DefaultSkills.TwoHanded
		};
		args.SetAffectedSkills(affectedSkills);
		args.SetFocusToSkills(_focusToAdd);
		args.SetLevelToSkills(_skillLevelToAdd);
		args.SetLevelToAttribute(DefaultCharacterAttributes.Intelligence, _attributeLevelToAdd);
	}

	private bool MakeshiftFortificationNarrativeOptionOnCondition(CharacterCreationManager characterCreationManager)
	{
		return true;
	}

	private void MakeshiftFortificationNarrativeOptionOnSelect(CharacterCreationManager characterCreationManager)
	{
		string animationId = "act_childhood_ready_handshield";
		string animationId2 = "act_khuzait_mp_rabble_idle";
		foreach (NarrativeMenuCharacter character in characterCreationManager.CurrentMenu.Characters)
		{
			if (character.StringId.Equals("player_escape_character"))
			{
				character.SetAnimationId(animationId);
			}
			if (character.StringId.Equals("brother_character"))
			{
				character.SetAnimationId(animationId2);
			}
		}
	}

	private void FinalizeParentsAndLittleSiblings(CharacterCreationManager characterCreationManager)
	{
		CharacterObject characterObject = Game.Current.ObjectManager.GetObject<CharacterObject>("main_hero_mother");
		CharacterObject characterObject2 = Game.Current.ObjectManager.GetObject<CharacterObject>("main_hero_father");
		CharacterObject characterObject3 = StoryModeHeroes.ElderBrother.CharacterObject;
		NarrativeMenuCharacter narrativeMenuCharacter = null;
		NarrativeMenuCharacter narrativeMenuCharacter2 = null;
		foreach (NarrativeMenuCharacter character in characterCreationManager.GetNarrativeMenuWithId("narrative_parent_menu").Characters)
		{
			if (character.StringId.Equals("mother_character"))
			{
				narrativeMenuCharacter = character;
			}
			if (character.StringId.Equals("father_character"))
			{
				narrativeMenuCharacter2 = character;
			}
		}
		characterObject.HeroObject.StaticBodyProperties = narrativeMenuCharacter.BodyProperties.StaticProperties;
		characterObject2.HeroObject.StaticBodyProperties = narrativeMenuCharacter2.BodyProperties.StaticProperties;
		characterObject.HeroObject.Weight = narrativeMenuCharacter.BodyProperties.Weight;
		characterObject.HeroObject.Build = narrativeMenuCharacter.BodyProperties.Build;
		characterObject2.HeroObject.Weight = narrativeMenuCharacter2.BodyProperties.Weight;
		characterObject2.HeroObject.Build = narrativeMenuCharacter2.BodyProperties.Build;
		if (narrativeMenuCharacter.Equipment != null)
		{
			EquipmentHelper.AssignHeroEquipmentFromEquipment(characterObject.HeroObject, narrativeMenuCharacter.Equipment.DefaultEquipment);
		}
		if (narrativeMenuCharacter2.Equipment != null)
		{
			EquipmentHelper.AssignHeroEquipmentFromEquipment(characterObject2.HeroObject, narrativeMenuCharacter2.Equipment.DefaultEquipment);
		}
		if (characterObject3.Equipment != null)
		{
			EquipmentHelper.AssignHeroEquipmentFromEquipment(characterObject3.HeroObject, characterObject3.Equipment);
		}
		characterObject.HeroObject.Culture = Hero.MainHero.Culture;
		characterObject2.HeroObject.Culture = Hero.MainHero.Culture;
		characterObject3.HeroObject.Culture = Hero.MainHero.Culture;
		StringHelpers.SetCharacterProperties("PLAYER", CharacterObject.PlayerCharacter);
		TextObject textObject = GameTexts.FindText("str_player_little_brother_name", Hero.MainHero.Culture.StringId);
		StoryModeHeroes.LittleBrother.SetName(textObject, textObject);
		StoryModeHeroes.LittleBrother.SetHasMet();
		TextObject textObject2 = GameTexts.FindText("str_player_little_sister_name", Hero.MainHero.Culture.StringId);
		StoryModeHeroes.LittleSister.SetName(textObject2, textObject2);
		StoryModeHeroes.LittleSister.SetHasMet();
		TextObject textObject3 = GameTexts.FindText("str_player_father_name", Hero.MainHero.Culture.StringId);
		characterObject2.HeroObject.SetName(textObject3, textObject3);
		TextObject textObject4 = GameTexts.FindText("str_player_mother_name", Hero.MainHero.Culture.StringId);
		characterObject.HeroObject.SetName(textObject4, textObject4);
		TextObject textObject5 = GameTexts.FindText("str_player_brother_name", Hero.MainHero.Culture.StringId);
		characterObject3.HeroObject.SetName(textObject5, textObject5);
		characterObject.HeroObject.Spouse = characterObject2.HeroObject;
		characterObject2.HeroObject.Spouse = characterObject.HeroObject;
		characterObject.HeroObject.UpdateHomeSettlement();
		characterObject2.HeroObject.UpdateHomeSettlement();
		characterObject3.HeroObject.UpdateHomeSettlement();
		characterObject.HeroObject.SetHasMet();
		characterObject2.HeroObject.SetHasMet();
		characterObject3.HeroObject.SetHasMet();
	}

	private void FinalizeMainHeroAndElderBrother(CharacterCreationManager characterCreationManager)
	{
		NarrativeMenu narrativeMenuWithId = characterCreationManager.GetNarrativeMenuWithId("narrative_escape_menu");
		NarrativeMenuCharacter narrativeMenuCharacter = null;
		NarrativeMenuCharacter narrativeMenuCharacter2 = null;
		foreach (NarrativeMenuCharacter character in narrativeMenuWithId.Characters)
		{
			if (character.StringId.Equals("player_escape_character"))
			{
				narrativeMenuCharacter = character;
			}
			if (character.StringId.Equals("brother_character"))
			{
				narrativeMenuCharacter2 = character;
			}
		}
		CharacterObject.PlayerCharacter.Equipment.FillFrom(narrativeMenuCharacter.Equipment.DefaultEquipment);
		CharacterObject.PlayerCharacter.FirstCivilianEquipment.FillFrom(narrativeMenuCharacter.Equipment.GetRandomCivilianEquipment());
		Hero elderBrother = StoryModeHeroes.ElderBrother;
		elderBrother.CharacterObject.Equipment.FillFrom(narrativeMenuCharacter2.Equipment.DefaultEquipment);
		elderBrother.CharacterObject.FirstCivilianEquipment.FillFrom(narrativeMenuCharacter2.Equipment.GetRandomCivilianEquipment());
	}

	protected void CreateSibling(Hero hero, BodyProperties motherBodyProperties, BodyProperties fatherBodyProperties, uint seed)
	{
		string hairTags = Hero.MainHero.Culture.ToString().ToLower() + ",";
		string beardTags = Hero.MainHero.Culture.ToString().ToLower() + ",";
		int seed2 = Hero.MainHero.RandomIntWithSeed(seed, 1, 100);
		BodyProperties randomBodyProperties = BodyProperties.GetRandomBodyProperties(hero.CharacterObject.Race, hero.IsFemale, motherBodyProperties, fatherBodyProperties, 1, seed2, hairTags, beardTags, hero.IsFemale ? Hero.MainHero.Mother.CharacterObject.BodyPropertyRange.TattooTags : Hero.MainHero.Father.CharacterObject.BodyPropertyRange.TattooTags);
		randomBodyProperties = new BodyProperties(new DynamicBodyProperties(hero.Age, 0.5f, 0.5f), randomBodyProperties.StaticProperties);
		hero.StaticBodyProperties = randomBodyProperties.StaticProperties;
		hero.Weight = randomBodyProperties.Weight;
		hero.Build = randomBodyProperties.Build;
	}
}
