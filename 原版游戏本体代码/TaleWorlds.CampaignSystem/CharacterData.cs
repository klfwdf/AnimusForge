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

namespace TaleWorlds.CampaignSystem
{
	// Token: 0x0200007B RID: 123
	public class CharacterData
	{
		// Token: 0x06001064 RID: 4196 RVA: 0x0004DB05 File Offset: 0x0004BD05
		private CharacterData()
		{
		}

		// Token: 0x06001065 RID: 4197 RVA: 0x0004DB10 File Offset: 0x0004BD10
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
			characterData.BodyPropertyKeys = new ulong[]
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
			characterData.SkillsArray = new CharacterData.SkillObjectData[Skills.All.Count];
			characterData.AttributesArray = new CharacterData.PropertyObjectData[Attributes.All.Count];
			characterData.Traits = new CharacterData.PropertyObjectData[TraitObject.All.Count];
			for (int i = 0; i < Skills.All.Count; i++)
			{
				characterData.SkillsArray[i] = new CharacterData.SkillObjectData(Skills.All[i].StringId, hero.GetSkillValue(Skills.All[i]), hero.HeroDeveloper.GetSkillXpProgress(Skills.All[i]), hero.HeroDeveloper.GetFocus(Skills.All[i]));
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
				characterData.AttributesArray[k] = new CharacterData.PropertyObjectData(Attributes.All[k].StringId, hero.GetAttributeValue(Attributes.All[k]));
			}
			for (int l = 0; l < TraitObject.All.Count; l++)
			{
				characterData.Traits[l] = new CharacterData.PropertyObjectData(TraitObject.All[l].StringId, hero.GetTraitLevel(TraitObject.All[l]));
			}
			return characterData;
		}

		// Token: 0x06001066 RID: 4198 RVA: 0x0004DE20 File Offset: 0x0004C020
		private static void InitializeHeroFromCharacterData(Hero target, CharacterData characterData)
		{
			TextObject textObject = GameTexts.FindText("str_generic_character_firstname", null);
			textObject.SetTextVariable("CHARACTER_FIRSTNAME", new TextObject(characterData.Name, null));
			TextObject textObject2 = GameTexts.FindText("str_generic_character_name", null);
			textObject2.SetTextVariable("CHARACTER_NAME", new TextObject(characterData.Name, null));
			target.Gold = characterData.Gold;
			target.IsFemale = characterData.IsFemale;
			target.CharacterObject.Race = characterData.Race;
			float num = characterData.Age;
			if (num < (float)Campaign.Current.Models.AgeModel.HeroComesOfAge)
			{
				num = (float)Campaign.Current.Models.AgeModel.HeroComesOfAge;
			}
			target.SetBirthDay(CampaignTime.YearsFromNow(-num));
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
			CultureObject @object = MBObjectManager.Instance.GetObject<CultureObject>(characterData.Culture);
			if (@object != null)
			{
				target.Culture = @object;
			}
			ulong[] bodyPropertyKeys = characterData.BodyPropertyKeys;
			target.StaticBodyProperties = new StaticBodyProperties(bodyPropertyKeys[0], bodyPropertyKeys[1], bodyPropertyKeys[2], bodyPropertyKeys[3], bodyPropertyKeys[4], bodyPropertyKeys[5], bodyPropertyKeys[6], bodyPropertyKeys[7]);
			target.HeroDeveloper.UnspentFocusPoints = characterData.UnspentFocusPoints;
			target.HeroDeveloper.UnspentAttributePoints = characterData.UnspentAttributePoints;
			for (int j = 0; j < characterData.SkillsArray.Length; j++)
			{
				CharacterData.SkillObjectData skillObjectData = characterData.SkillsArray[j];
				string stringId = skillObjectData.StringId;
				int num2 = skillObjectData.Value;
				int num3 = skillObjectData.Focus;
				int progress = skillObjectData.Progress;
				SkillObject object2 = MBObjectManager.Instance.GetObject<SkillObject>(stringId);
				if (object2 != null)
				{
					int focus = target.HeroDeveloper.GetFocus(object2);
					num2 = Math.Max(0, num2);
					int xpRequiredForSkillLevel = Campaign.Current.Models.CharacterDevelopmentModel.GetXpRequiredForSkillLevel(num2);
					int xpRequiredForSkillLevel2 = Campaign.Current.Models.CharacterDevelopmentModel.GetXpRequiredForSkillLevel(num2 + 1);
					int num4 = Math.Min(progress + xpRequiredForSkillLevel, xpRequiredForSkillLevel2);
					target.HeroDeveloper.SetSkillXp(object2, (float)num4);
					target.SetSkillValue(object2, num2);
					num3 = Math.Max(Math.Min(num3, Campaign.Current.Models.CharacterDevelopmentModel.MaxFocusPerSkill), 0);
					if (focus < num3)
					{
						target.HeroDeveloper.AddFocus(object2, num3 - focus, false);
					}
					else
					{
						target.HeroDeveloper.RemoveFocus(object2, focus - num3);
					}
				}
			}
			for (int k = 0; k < characterData.Traits.Length; k++)
			{
				CharacterData.PropertyObjectData propertyObjectData = characterData.Traits[k];
				string stringId2 = propertyObjectData.StringId;
				int num5 = propertyObjectData.Value;
				TraitObject object3 = MBObjectManager.Instance.GetObject<TraitObject>(stringId2);
				if (object3 != null)
				{
					num5 = Math.Max(Math.Min(num5, object3.MaxValue), object3.MinValue);
					target.SetTraitLevel(object3, num5);
				}
			}
			for (int l = 0; l < characterData.AttributesArray.Length; l++)
			{
				CharacterData.PropertyObjectData propertyObjectData2 = characterData.AttributesArray[l];
				string stringId3 = propertyObjectData2.StringId;
				int value = propertyObjectData2.Value;
				CharacterAttribute object4 = MBObjectManager.Instance.GetObject<CharacterAttribute>(stringId3);
				if (object4 != null)
				{
					int changeAmount = ((target.GetAttributeValue(object4) > value) ? (value - target.GetAttributeValue(object4)) : (value - target.GetAttributeValue(object4)));
					target.HeroDeveloper.AddAttribute(object4, changeAmount, false);
				}
			}
			target.ClearPerks();
			for (int m = 0; m < characterData.UnlockedPerks.Length; m++)
			{
				string objectName = characterData.UnlockedPerks[m];
				PerkObject object5 = MBObjectManager.Instance.GetObject<PerkObject>(objectName);
				if (object5 != null)
				{
					target.HeroDeveloper.AddPerk(object5);
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

		// Token: 0x06001067 RID: 4199 RVA: 0x0004E344 File Offset: 0x0004C544
		public static void ExportCharacter(Hero hero, string path)
		{
			CharacterData o = CharacterData.CreateFrom(hero);
			XmlSerializer xmlSerializer = new XmlSerializer(typeof(CharacterData));
			using (StreamWriter streamWriter = new StreamWriter(path))
			{
				xmlSerializer.Serialize(streamWriter, o);
			}
		}

		// Token: 0x06001068 RID: 4200 RVA: 0x0004E394 File Offset: 0x0004C594
		public static void ImportCharacter(Hero hero, string path)
		{
			XmlSerializer xmlSerializer = new XmlSerializer(typeof(CharacterData));
			using (FileStream fileStream = new FileStream(path, FileMode.Open))
			{
				CharacterData characterData = (CharacterData)xmlSerializer.Deserialize(fileStream);
				CharacterData.InitializeHeroFromCharacterData(Hero.MainHero, characterData);
			}
		}

		// Token: 0x0400049A RID: 1178
		public const string CharacterDataExtension = "char";

		// Token: 0x0400049B RID: 1179
		[XmlElement]
		public string Name;

		// Token: 0x0400049C RID: 1180
		[XmlElement]
		public bool IsFemale;

		// Token: 0x0400049D RID: 1181
		[XmlElement]
		public int Gold;

		// Token: 0x0400049E RID: 1182
		[XmlElement]
		public int Race;

		// Token: 0x0400049F RID: 1183
		[XmlElement]
		public int Level;

		// Token: 0x040004A0 RID: 1184
		[XmlElement]
		public string Culture;

		// Token: 0x040004A1 RID: 1185
		[XmlElement]
		public float Age;

		// Token: 0x040004A2 RID: 1186
		[XmlElement]
		public float Weight;

		// Token: 0x040004A3 RID: 1187
		[XmlElement]
		public float Build;

		// Token: 0x040004A4 RID: 1188
		[XmlElement]
		public string CivilianEquipmentCode;

		// Token: 0x040004A5 RID: 1189
		[XmlElement]
		public string BattleEquipmentCode;

		// Token: 0x040004A6 RID: 1190
		[XmlElement]
		public string StealthEquipmentCode;

		// Token: 0x040004A7 RID: 1191
		[XmlArray("BodyPropertyKeys")]
		[XmlArrayItem("Key")]
		public ulong[] BodyPropertyKeys;

		// Token: 0x040004A8 RID: 1192
		[XmlElement]
		public int UnspentFocusPoints;

		// Token: 0x040004A9 RID: 1193
		[XmlElement]
		public int UnspentAttributePoints;

		// Token: 0x040004AA RID: 1194
		[XmlArray("Perks")]
		[XmlArrayItem("Perk")]
		public string[] UnlockedPerks;

		// Token: 0x040004AB RID: 1195
		[XmlArray("Attributes")]
		[XmlArrayItem("Attribute")]
		public CharacterData.PropertyObjectData[] AttributesArray;

		// Token: 0x040004AC RID: 1196
		[XmlArray("Traits")]
		[XmlArrayItem("Trait")]
		public CharacterData.PropertyObjectData[] Traits;

		// Token: 0x040004AD RID: 1197
		[XmlArray("Skills")]
		[XmlArrayItem("Skill")]
		public CharacterData.SkillObjectData[] SkillsArray;

		// Token: 0x0200053B RID: 1339
		public class PropertyObjectData
		{
			// Token: 0x06004C21 RID: 19489 RVA: 0x0017944D File Offset: 0x0017764D
			public PropertyObjectData(string id, int value)
			{
				this.StringId = id;
				this.Value = value;
			}

			// Token: 0x06004C22 RID: 19490 RVA: 0x00179463 File Offset: 0x00177663
			public PropertyObjectData()
			{
			}

			// Token: 0x04001636 RID: 5686
			[XmlElement]
			public string StringId;

			// Token: 0x04001637 RID: 5687
			[XmlElement]
			public int Value;
		}

		// Token: 0x0200053C RID: 1340
		public class SkillObjectData : CharacterData.PropertyObjectData
		{
			// Token: 0x06004C23 RID: 19491 RVA: 0x0017946B File Offset: 0x0017766B
			public SkillObjectData(string id, int value, int progress, int focus)
				: base(id, value)
			{
				this.Focus = focus;
				this.Progress = progress;
			}

			// Token: 0x06004C24 RID: 19492 RVA: 0x00179484 File Offset: 0x00177684
			public SkillObjectData()
			{
			}

			// Token: 0x04001638 RID: 5688
			[XmlElement]
			public int Focus;

			// Token: 0x04001639 RID: 5689
			[XmlElement]
			public int Progress;
		}
	}
}
