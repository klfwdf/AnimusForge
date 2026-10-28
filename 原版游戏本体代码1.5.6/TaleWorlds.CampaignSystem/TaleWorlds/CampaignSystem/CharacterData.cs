using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace TaleWorlds.CampaignSystem;

public class CharacterData
{
	public class PropertyObjectData
	{
		[XmlElement]
		public string StringId;

		[XmlElement]
		public int Value;

		public PropertyObjectData(string id, int value)
		{
			StringId = id;
			Value = value;
		}

		public PropertyObjectData()
		{
		}
	}

	public class SkillObjectData : PropertyObjectData
	{
		[XmlElement]
		public int Focus;

		[XmlElement]
		public int Progress;

		public SkillObjectData(string id, int value, int progress, int focus)
			: base(id, value)
		{
			Focus = focus;
			Progress = progress;
		}

		public SkillObjectData()
		{
		}
	}

	public const string CharacterDataExtension = "char";

	[XmlElement]
	public string Name;

	[XmlElement]
	public bool IsFemale;

	[XmlElement]
	public int Gold;

	[XmlElement]
	public int Race;

	[XmlElement]
	public int Level;

	[XmlElement]
	public string Culture;

	[XmlElement]
	public float Age;

	[XmlElement]
	public float Weight;

	[XmlElement]
	public float Build;

	[XmlElement]
	public string CivilianEquipmentCode;

	[XmlElement]
	public string BattleEquipmentCode;

	[XmlElement]
	public string StealthEquipmentCode;

	[XmlArray("BodyPropertyKeys")]
	[XmlArrayItem("Key")]
	public ulong[] BodyPropertyKeys;

	[XmlElement]
	public int UnspentFocusPoints;

	[XmlElement]
	public int UnspentAttributePoints;

	[XmlArray("Perks")]
	[XmlArrayItem("Perk")]
	public string[] UnlockedPerks;

	[XmlArray("Attributes")]
	[XmlArrayItem("Attribute")]
	public PropertyObjectData[] AttributesArray;

	[XmlArray("Traits")]
	[XmlArrayItem("Trait")]
	public PropertyObjectData[] Traits;

	[XmlArray("Skills")]
	[XmlArrayItem("Skill")]
	public SkillObjectData[] SkillsArray;

	private CharacterData()
	{
	}

	private static CharacterData CreateFrom(Hero hero)
	{
		CharacterData characterData = new CharacterData();
		characterData.Name = hero.Name.ToString();
		characterData.Age = hero.Age;
		characterData.Culture = hero.Culture.StringId;
		characterData.Gold = hero.Gold;
		characterData.Race = hero.CharacterObject.Race;
		characterData.Level = hero.Level;
		characterData.IsFemale = hero.IsFemale;
		characterData.Weight = hero.Weight;
		characterData.Build = hero.Build;
		characterData.CivilianEquipmentCode = hero.CivilianEquipment.CalculateEquipmentCode();
		characterData.StealthEquipmentCode = hero.StealthEquipment.CalculateEquipmentCode();
		characterData.BattleEquipmentCode = hero.BattleEquipment.CalculateEquipmentCode();
		characterData.BodyPropertyKeys = new ulong[8]
		{
			hero.StaticBodyProperties.KeyPart1,
			hero.StaticBodyProperties.KeyPart2,
			hero.StaticBodyProperties.KeyPart3,
			hero.StaticBodyProperties.KeyPart4,
			hero.StaticBodyProperties.KeyPart5,
			hero.StaticBodyProperties.KeyPart6,
			hero.StaticBodyProperties.KeyPart7,
			hero.StaticBodyProperties.KeyPart8
		};
		characterData.UnspentAttributePoints = hero.HeroDeveloper.UnspentAttributePoints;
		characterData.UnspentFocusPoints = hero.HeroDeveloper.UnspentFocusPoints;
		characterData.SkillsArray = new SkillObjectData[Skills.All.Count];
		characterData.AttributesArray = new PropertyObjectData[Attributes.All.Count];
		characterData.Traits = new PropertyObjectData[TraitObject.All.Count];
		for (int i = 0; i < Skills.All.Count; i++)
		{
			characterData.SkillsArray[i] = new SkillObjectData(Skills.All[i].StringId, hero.GetSkillValue(Skills.All[i]), hero.HeroDeveloper.GetSkillXpProgress(Skills.All[i]), hero.HeroDeveloper.GetFocus(Skills.All[i]));
		}
		List<string> list = new List<string>();
		for (int j = 0; j < PerkObject.All.Count; j++)
		{
			if (hero.GetPerkValue(PerkObject.All[j]))
			{
				list.Add(PerkObject.All[j].StringId);
			}
		}
		characterData.UnlockedPerks = list.ToArray();
		for (int k = 0; k < Attributes.All.Count; k++)
		{
			characterData.AttributesArray[k] = new PropertyObjectData(Attributes.All[k].StringId, hero.GetAttributeValue(Attributes.All[k]));
		}
		for (int l = 0; l < TraitObject.All.Count; l++)
		{
			characterData.Traits[l] = new PropertyObjectData(TraitObject.All[l].StringId, hero.GetTraitLevel(TraitObject.All[l]));
		}
		return characterData;
	}

	private static void InitializeHeroFromCharacterData(Hero target, CharacterData characterData)
	{
		TextObject textObject = GameTexts.FindText("str_generic_character_firstname");
		textObject.SetTextVariable("CHARACTER_FIRSTNAME", new TextObject(characterData.Name));
		TextObject textObject2 = GameTexts.FindText("str_generic_character_name");
		textObject2.SetTextVariable("CHARACTER_NAME", new TextObject(characterData.Name));
		target.Gold = characterData.Gold;
		target.IsFemale = characterData.IsFemale;
		target.CharacterObject.Race = characterData.Race;
		float num = characterData.Age;
		if (num < (float)Campaign.Current.Models.AgeModel.HeroComesOfAge)
		{
			num = Campaign.Current.Models.AgeModel.HeroComesOfAge;
		}
		target.SetBirthDay(CampaignTime.YearsFromNow(0f - num));
		target.Weight = characterData.Weight;
		target.Build = characterData.Build;
		target.Level = characterData.Level;
		Equipment equipment = Equipment.CreateFromEquipmentCode(characterData.BattleEquipmentCode);
		Equipment equipment2 = Equipment.CreateFromEquipmentCode(characterData.CivilianEquipmentCode);
		Equipment equipment3 = null;
		if (!string.IsNullOrEmpty(characterData.StealthEquipmentCode))
		{
			equipment3 = Equipment.CreateFromEquipmentCode(characterData.StealthEquipmentCode);
		}
		for (int i = 0; i < 12; i++)
		{
			if (target.PartyBelongedTo != null)
			{
				if (!target.BattleEquipment[i].IsEmpty)
				{
					target.PartyBelongedTo.ItemRoster.AddToCounts(target.BattleEquipment[i], 1);
				}
				if (!target.CivilianEquipment[i].IsEmpty)
				{
					target.PartyBelongedTo.ItemRoster.AddToCounts(target.CivilianEquipment[i], 1);
				}
				if (!target.StealthEquipment[i].IsEmpty)
				{
					target.PartyBelongedTo.ItemRoster.AddToCounts(target.StealthEquipment[i], 1);
				}
			}
			target.BattleEquipment[i] = equipment[i];
			target.CivilianEquipment[i] = equipment2[i];
			if (equipment3 != null)
			{
				target.StealthEquipment[i] = equipment3[i];
			}
		}
		CultureObject cultureObject = MBObjectManager.Instance.GetObject<CultureObject>(characterData.Culture);
		if (cultureObject != null)
		{
			target.Culture = cultureObject;
		}
		ulong[] bodyPropertyKeys = characterData.BodyPropertyKeys;
		target.StaticBodyProperties = new StaticBodyProperties(bodyPropertyKeys[0], bodyPropertyKeys[1], bodyPropertyKeys[2], bodyPropertyKeys[3], bodyPropertyKeys[4], bodyPropertyKeys[5], bodyPropertyKeys[6], bodyPropertyKeys[7]);
		target.HeroDeveloper.UnspentFocusPoints = characterData.UnspentFocusPoints;
		target.HeroDeveloper.UnspentAttributePoints = characterData.UnspentAttributePoints;
		for (int j = 0; j < characterData.SkillsArray.Length; j++)
		{
			SkillObjectData obj = characterData.SkillsArray[j];
			string stringId = obj.StringId;
			int value = obj.Value;
			int focus = obj.Focus;
			int progress = obj.Progress;
			SkillObject skillObject = MBObjectManager.Instance.GetObject<SkillObject>(stringId);
			if (skillObject != null)
			{
				int focus2 = target.HeroDeveloper.GetFocus(skillObject);
				value = Math.Max(0, value);
				int xpRequiredForSkillLevel = Campaign.Current.Models.CharacterDevelopmentModel.GetXpRequiredForSkillLevel(value);
				int xpRequiredForSkillLevel2 = Campaign.Current.Models.CharacterDevelopmentModel.GetXpRequiredForSkillLevel(value + 1);
				int num2 = Math.Min(progress + xpRequiredForSkillLevel, xpRequiredForSkillLevel2);
				target.HeroDeveloper.SetSkillXp(skillObject, num2);
				target.SetSkillValue(skillObject, value);
				focus = Math.Max(Math.Min(focus, Campaign.Current.Models.CharacterDevelopmentModel.MaxFocusPerSkill), 0);
				if (focus2 < focus)
				{
					target.HeroDeveloper.AddFocus(skillObject, focus - focus2, checkUnspentFocusPoints: false);
				}
				else
				{
					target.HeroDeveloper.RemoveFocus(skillObject, focus2 - focus);
				}
			}
		}
		for (int k = 0; k < characterData.Traits.Length; k++)
		{
			PropertyObjectData obj2 = characterData.Traits[k];
			string stringId2 = obj2.StringId;
			int value2 = obj2.Value;
			TraitObject traitObject = MBObjectManager.Instance.GetObject<TraitObject>(stringId2);
			if (traitObject != null)
			{
				value2 = Math.Max(Math.Min(value2, traitObject.MaxValue), traitObject.MinValue);
				target.SetTraitLevel(traitObject, value2);
			}
		}
		for (int l = 0; l < characterData.AttributesArray.Length; l++)
		{
			PropertyObjectData obj3 = characterData.AttributesArray[l];
			string stringId3 = obj3.StringId;
			int value3 = obj3.Value;
			CharacterAttribute characterAttribute = MBObjectManager.Instance.GetObject<CharacterAttribute>(stringId3);
			if (characterAttribute != null)
			{
				int changeAmount = ((target.GetAttributeValue(characterAttribute) > value3) ? (value3 - target.GetAttributeValue(characterAttribute)) : (value3 - target.GetAttributeValue(characterAttribute)));
				target.HeroDeveloper.AddAttribute(characterAttribute, changeAmount, checkUnspentPoints: false);
			}
		}
		target.ClearPerks();
		for (int m = 0; m < characterData.UnlockedPerks.Length; m++)
		{
			string objectName = characterData.UnlockedPerks[m];
			PerkObject perkObject = MBObjectManager.Instance.GetObject<PerkObject>(objectName);
			if (perkObject != null)
			{
				target.HeroDeveloper.AddPerk(perkObject);
			}
		}
		target.HeroDeveloper.SetInitialLevel(target.Level);
		target.SetName(textObject2, textObject);
		Hero.SetHeroEncyclopediaTextAndLinks(target);
		if (GameStateManager.Current.ActiveState is MapState && target.PartyBelongedTo != null)
		{
			target.PartyBelongedTo.Party.SetVisualAsDirty();
		}
	}

	public static void ExportCharacter(Hero hero, string path)
	{
		CharacterData o = CreateFrom(hero);
		XmlSerializer xmlSerializer = new XmlSerializer(typeof(CharacterData));
		using StreamWriter textWriter = new StreamWriter(path);
		xmlSerializer.Serialize(textWriter, o);
	}

	public static void ImportCharacter(Hero hero, string path)
	{
		XmlSerializer xmlSerializer = new XmlSerializer(typeof(CharacterData));
		using FileStream stream = new FileStream(path, FileMode.Open);
		CharacterData characterData = (CharacterData)xmlSerializer.Deserialize(stream);
		InitializeHeroFromCharacterData(Hero.MainHero, characterData);
	}
}
