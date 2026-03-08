using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace TaleWorlds.Core
{
	// Token: 0x0200001A RID: 26
	public class BasicCharacterObject : MBObjectBase
	{
		// Token: 0x17000047 RID: 71
		// (get) Token: 0x06000116 RID: 278 RVA: 0x0000541E File Offset: 0x0000361E
		public virtual TextObject Name
		{
			get
			{
				return this._basicName;
			}
		}

		// Token: 0x06000117 RID: 279 RVA: 0x00005426 File Offset: 0x00003626
		private void SetName(TextObject name)
		{
			this._basicName = name;
		}

		// Token: 0x06000118 RID: 280 RVA: 0x0000542F File Offset: 0x0000362F
		public override TextObject GetName()
		{
			return this.Name;
		}

		// Token: 0x06000119 RID: 281 RVA: 0x00005437 File Offset: 0x00003637
		public override string ToString()
		{
			return this.Name.ToString();
		}

		// Token: 0x17000048 RID: 72
		// (get) Token: 0x0600011A RID: 282 RVA: 0x00005444 File Offset: 0x00003644
		// (set) Token: 0x0600011B RID: 283 RVA: 0x0000544C File Offset: 0x0000364C
		public virtual MBBodyProperty BodyPropertyRange { get; protected set; }

		// Token: 0x17000049 RID: 73
		// (get) Token: 0x0600011C RID: 284 RVA: 0x00005455 File Offset: 0x00003655
		// (set) Token: 0x0600011D RID: 285 RVA: 0x0000545D File Offset: 0x0000365D
		public int DefaultFormationGroup { get; set; }

		// Token: 0x1700004A RID: 74
		// (get) Token: 0x0600011E RID: 286 RVA: 0x00005466 File Offset: 0x00003666
		// (set) Token: 0x0600011F RID: 287 RVA: 0x0000546E File Offset: 0x0000366E
		public FormationClass DefaultFormationClass { get; protected set; }

		// Token: 0x1700004B RID: 75
		// (get) Token: 0x06000120 RID: 288 RVA: 0x00005477 File Offset: 0x00003677
		// (set) Token: 0x06000121 RID: 289 RVA: 0x0000547F File Offset: 0x0000367F
		public float KnockbackResistance { get; private set; }

		// Token: 0x1700004C RID: 76
		// (get) Token: 0x06000122 RID: 290 RVA: 0x00005488 File Offset: 0x00003688
		// (set) Token: 0x06000123 RID: 291 RVA: 0x00005490 File Offset: 0x00003690
		public float KnockdownResistance { get; private set; }

		// Token: 0x1700004D RID: 77
		// (get) Token: 0x06000124 RID: 292 RVA: 0x00005499 File Offset: 0x00003699
		// (set) Token: 0x06000125 RID: 293 RVA: 0x000054A1 File Offset: 0x000036A1
		public float DismountResistance { get; private set; }

		// Token: 0x1700004E RID: 78
		// (get) Token: 0x06000126 RID: 294 RVA: 0x000054AA File Offset: 0x000036AA
		// (set) Token: 0x06000127 RID: 295 RVA: 0x000054B2 File Offset: 0x000036B2
		public FormationPositionPreference FormationPositionPreference { get; protected set; }

		// Token: 0x1700004F RID: 79
		// (get) Token: 0x06000128 RID: 296 RVA: 0x000054BB File Offset: 0x000036BB
		public bool IsInfantry
		{
			get
			{
				return !this.IsRanged && !this.IsMounted;
			}
		}

		// Token: 0x17000050 RID: 80
		// (get) Token: 0x06000129 RID: 297 RVA: 0x000054D0 File Offset: 0x000036D0
		public virtual bool IsMounted
		{
			get
			{
				return this._isMounted;
			}
		}

		// Token: 0x17000051 RID: 81
		// (get) Token: 0x0600012A RID: 298 RVA: 0x000054D8 File Offset: 0x000036D8
		public virtual bool IsRanged
		{
			get
			{
				return this._isRanged;
			}
		}

		// Token: 0x17000052 RID: 82
		// (get) Token: 0x0600012B RID: 299 RVA: 0x000054E0 File Offset: 0x000036E0
		public float SkillFactor
		{
			get
			{
				return (float)MathF.Min(this.Level, BasicCharacterObject.SkillAffectingMaxLevel) / (float)BasicCharacterObject.SkillAffectingMaxLevel;
			}
		}

		// Token: 0x17000053 RID: 83
		// (get) Token: 0x0600012C RID: 300 RVA: 0x000054FA File Offset: 0x000036FA
		// (set) Token: 0x0600012D RID: 301 RVA: 0x00005502 File Offset: 0x00003702
		public int Race { get; set; }

		// Token: 0x17000054 RID: 84
		// (get) Token: 0x0600012E RID: 302 RVA: 0x0000550B File Offset: 0x0000370B
		// (set) Token: 0x0600012F RID: 303 RVA: 0x00005513 File Offset: 0x00003713
		public virtual bool IsFemale { get; set; }

		// Token: 0x17000055 RID: 85
		// (get) Token: 0x06000130 RID: 304 RVA: 0x0000551C File Offset: 0x0000371C
		// (set) Token: 0x06000131 RID: 305 RVA: 0x00005524 File Offset: 0x00003724
		public bool FaceMeshCache { get; private set; }

		// Token: 0x17000056 RID: 86
		// (get) Token: 0x06000132 RID: 306 RVA: 0x0000552D File Offset: 0x0000372D
		protected virtual MBReadOnlyList<Equipment> AllEquipments
		{
			get
			{
				if (this._equipmentRoster == null)
				{
					return new MBList<Equipment> { MBEquipmentRoster.EmptyEquipment };
				}
				return this._equipmentRoster.AllEquipments;
			}
		}

		// Token: 0x17000057 RID: 87
		// (get) Token: 0x06000133 RID: 307 RVA: 0x00005553 File Offset: 0x00003753
		public virtual Equipment Equipment
		{
			get
			{
				if (this._equipmentRoster == null)
				{
					return MBEquipmentRoster.EmptyEquipment;
				}
				return this._equipmentRoster.DefaultEquipment;
			}
		}

		// Token: 0x17000058 RID: 88
		// (get) Token: 0x06000134 RID: 308 RVA: 0x0000556E File Offset: 0x0000376E
		public virtual IEnumerable<Equipment> BattleEquipments
		{
			get
			{
				return this.AllEquipments.WhereQ((Equipment e) => e.IsBattle);
			}
		}

		// Token: 0x17000059 RID: 89
		// (get) Token: 0x06000135 RID: 309 RVA: 0x0000559A File Offset: 0x0000379A
		public virtual Equipment FirstBattleEquipment
		{
			get
			{
				return this.AllEquipments.FirstOrDefaultQ((Equipment e) => e.IsBattle);
			}
		}

		// Token: 0x1700005A RID: 90
		// (get) Token: 0x06000136 RID: 310 RVA: 0x000055C6 File Offset: 0x000037C6
		public virtual Equipment RandomBattleEquipment
		{
			get
			{
				return this.AllEquipments.GetRandomElementWithPredicate((Equipment e) => e.IsBattle);
			}
		}

		// Token: 0x1700005B RID: 91
		// (get) Token: 0x06000137 RID: 311 RVA: 0x000055F2 File Offset: 0x000037F2
		public virtual IEnumerable<Equipment> CivilianEquipments
		{
			get
			{
				return this.AllEquipments.WhereQ((Equipment e) => e.IsCivilian);
			}
		}

		// Token: 0x1700005C RID: 92
		// (get) Token: 0x06000138 RID: 312 RVA: 0x0000561E File Offset: 0x0000381E
		public virtual Equipment FirstCivilianEquipment
		{
			get
			{
				return this.AllEquipments.FirstOrDefaultQ((Equipment e) => e.IsCivilian);
			}
		}

		// Token: 0x1700005D RID: 93
		// (get) Token: 0x06000139 RID: 313 RVA: 0x0000564A File Offset: 0x0000384A
		public virtual Equipment RandomCivilianEquipment
		{
			get
			{
				return this.AllEquipments.GetRandomElementWithPredicate((Equipment e) => e.IsCivilian);
			}
		}

		// Token: 0x1700005E RID: 94
		// (get) Token: 0x0600013A RID: 314 RVA: 0x00005676 File Offset: 0x00003876
		public virtual Equipment GetRandomEquipment
		{
			get
			{
				return this.AllEquipments.GetRandomElementWithPredicate((Equipment x) => !x.IsEmpty());
			}
		}

		// Token: 0x1700005F RID: 95
		// (get) Token: 0x0600013B RID: 315 RVA: 0x000056A2 File Offset: 0x000038A2
		// (set) Token: 0x0600013C RID: 316 RVA: 0x000056AA File Offset: 0x000038AA
		public bool IsObsolete { get; private set; }

		// Token: 0x0600013D RID: 317 RVA: 0x000056B3 File Offset: 0x000038B3
		public void InitializeEquipmentsOnLoad(BasicCharacterObject character)
		{
			this._equipmentRoster = character._equipmentRoster;
		}

		// Token: 0x0600013E RID: 318 RVA: 0x000056C4 File Offset: 0x000038C4
		public Equipment GetFirstEquipment(Func<Equipment, bool> predicate)
		{
			Equipment equipment = this.AllEquipments.FirstOrDefault(predicate);
			if (equipment != null)
			{
				return equipment;
			}
			return this.Equipment;
		}

		// Token: 0x17000060 RID: 96
		// (get) Token: 0x0600013F RID: 319 RVA: 0x000056E9 File Offset: 0x000038E9
		// (set) Token: 0x06000140 RID: 320 RVA: 0x000056F1 File Offset: 0x000038F1
		public virtual int Level { get; set; }

		// Token: 0x17000061 RID: 97
		// (get) Token: 0x06000141 RID: 321 RVA: 0x000056FA File Offset: 0x000038FA
		// (set) Token: 0x06000142 RID: 322 RVA: 0x00005702 File Offset: 0x00003902
		public BasicCultureObject Culture
		{
			get
			{
				return this._culture;
			}
			set
			{
				this._culture = value;
			}
		}

		// Token: 0x17000062 RID: 98
		// (get) Token: 0x06000143 RID: 323 RVA: 0x0000570B File Offset: 0x0000390B
		public virtual bool IsPlayerCharacter
		{
			get
			{
				return Game.Current.PlayerTroop == this;
			}
		}

		// Token: 0x17000063 RID: 99
		// (get) Token: 0x06000144 RID: 324 RVA: 0x0000571A File Offset: 0x0000391A
		// (set) Token: 0x06000145 RID: 325 RVA: 0x00005722 File Offset: 0x00003922
		public virtual float Age
		{
			get
			{
				return this._age;
			}
			set
			{
				this._age = value;
			}
		}

		// Token: 0x17000064 RID: 100
		// (get) Token: 0x06000146 RID: 326 RVA: 0x0000572B File Offset: 0x0000392B
		public virtual int HitPoints
		{
			get
			{
				return this.MaxHitPoints();
			}
		}

		// Token: 0x06000147 RID: 327 RVA: 0x00005733 File Offset: 0x00003933
		public virtual BodyProperties GetBodyPropertiesMin(bool returnBaseValue = false)
		{
			return this.BodyPropertyRange.BodyPropertyMin;
		}

		// Token: 0x06000148 RID: 328 RVA: 0x00005740 File Offset: 0x00003940
		protected void FillFrom(BasicCharacterObject character)
		{
			this._culture = character._culture;
			this.DefaultFormationClass = character.DefaultFormationClass;
			this.DefaultFormationGroup = character.DefaultFormationGroup;
			this.BodyPropertyRange = character.BodyPropertyRange;
			this.FormationPositionPreference = character.FormationPositionPreference;
			this.IsFemale = character.IsFemale;
			this.Race = character.Race;
			this.Level = character.Level;
			this._basicName = character._basicName;
			this._age = character._age;
			this.KnockbackResistance = character.KnockbackResistance;
			this.KnockdownResistance = character.KnockdownResistance;
			this.DismountResistance = character.DismountResistance;
			this.DefaultCharacterSkills = character.DefaultCharacterSkills;
			this.InitializeEquipmentsOnLoad(character);
		}

		// Token: 0x06000149 RID: 329 RVA: 0x000057FC File Offset: 0x000039FC
		public virtual BodyProperties GetBodyPropertiesMax(bool returnBaseValue = false)
		{
			return this.BodyPropertyRange.BodyPropertyMax;
		}

		// Token: 0x0600014A RID: 330 RVA: 0x0000580C File Offset: 0x00003A0C
		public virtual BodyProperties GetBodyProperties(Equipment equipment, int seed = -1)
		{
			BodyProperties bodyPropertiesMin = this.GetBodyPropertiesMin(false);
			BodyProperties bodyPropertiesMax = this.GetBodyPropertiesMax(false);
			return FaceGen.GetRandomBodyProperties(this.Race, this.IsFemale, bodyPropertiesMin, bodyPropertiesMax, (int)((equipment != null) ? equipment.HairCoverType : ArmorComponent.HairCoverTypes.None), seed, this.BodyPropertyRange.HairTags, this.BodyPropertyRange.BeardTags, this.BodyPropertyRange.TattooTags, 0f);
		}

		// Token: 0x0600014B RID: 331 RVA: 0x0000586F File Offset: 0x00003A6F
		public virtual void UpdatePlayerCharacterBodyProperties(BodyProperties properties, int race, bool isFemale)
		{
			this.BodyPropertyRange.Init(properties, properties);
			this.Race = race;
			this.IsFemale = isFemale;
		}

		// Token: 0x17000065 RID: 101
		// (get) Token: 0x0600014C RID: 332 RVA: 0x0000588C File Offset: 0x00003A8C
		// (set) Token: 0x0600014D RID: 333 RVA: 0x00005894 File Offset: 0x00003A94
		public float FaceDirtAmount { get; set; }

		// Token: 0x17000066 RID: 102
		// (get) Token: 0x0600014E RID: 334 RVA: 0x0000589D File Offset: 0x00003A9D
		public virtual bool IsHero
		{
			get
			{
				return this._isBasicHero;
			}
		}

		// Token: 0x17000067 RID: 103
		// (get) Token: 0x0600014F RID: 335 RVA: 0x000058A5 File Offset: 0x00003AA5
		// (set) Token: 0x06000150 RID: 336 RVA: 0x000058AD File Offset: 0x00003AAD
		public bool IsSoldier { get; private set; }

		// Token: 0x06000151 RID: 337 RVA: 0x000058B6 File Offset: 0x00003AB6
		public BasicCharacterObject()
		{
			this.DefaultFormationClass = FormationClass.Infantry;
		}

		// Token: 0x06000152 RID: 338 RVA: 0x000058C8 File Offset: 0x00003AC8
		public int GetDefaultFaceSeed(int rank)
		{
			int num = base.StringId.GetDeterministicHashCode() * 6791 + rank * 197;
			return ((num >= 0) ? num : (-num)) % 2000;
		}

		// Token: 0x06000153 RID: 339 RVA: 0x000058FE File Offset: 0x00003AFE
		public float GetStepSize()
		{
			return Math.Min(0.8f + 0.2f * (float)this.GetSkillValue(DefaultSkills.Athletics) * 0.00333333f, 1f);
		}

		// Token: 0x06000154 RID: 340 RVA: 0x00005928 File Offset: 0x00003B28
		public bool HasMount()
		{
			return this.Equipment[10].Item != null;
		}

		// Token: 0x06000155 RID: 341 RVA: 0x0000594D File Offset: 0x00003B4D
		public virtual int MaxHitPoints()
		{
			return FaceGen.GetBaseMonsterFromRace(this.Race).HitPoints;
		}

		// Token: 0x06000156 RID: 342 RVA: 0x00005960 File Offset: 0x00003B60
		public virtual float GetPower()
		{
			int num = this.Level + 10;
			return 0.2f + (float)(num * num) * 0.0025f;
		}

		// Token: 0x06000157 RID: 343 RVA: 0x00005987 File Offset: 0x00003B87
		public virtual float GetBattlePower()
		{
			return 1f;
		}

		// Token: 0x06000158 RID: 344 RVA: 0x0000598E File Offset: 0x00003B8E
		public virtual float GetMoraleResistance()
		{
			return 1f;
		}

		// Token: 0x06000159 RID: 345 RVA: 0x00005995 File Offset: 0x00003B95
		public virtual int GetMountKeySeed()
		{
			return MBRandom.RandomInt();
		}

		// Token: 0x0600015A RID: 346 RVA: 0x0000599C File Offset: 0x00003B9C
		public virtual int GetBattleTier()
		{
			if (this.IsHero)
			{
				return 7;
			}
			return MathF.Min(MathF.Max(MathF.Ceiling(((float)this.Level - 5f) / 5f), 0), 7);
		}

		// Token: 0x0600015B RID: 347 RVA: 0x000059CC File Offset: 0x00003BCC
		public MBCharacterSkills GetDefaultCharacterSkills()
		{
			return this.DefaultCharacterSkills;
		}

		// Token: 0x0600015C RID: 348 RVA: 0x000059D4 File Offset: 0x00003BD4
		public virtual int GetSkillValue(SkillObject skill)
		{
			return this.DefaultCharacterSkills.Skills.GetPropertyValue(skill);
		}

		// Token: 0x0600015D RID: 349 RVA: 0x000059E8 File Offset: 0x00003BE8
		protected void InitializeHeroBasicCharacterOnAfterLoad(BasicCharacterObject originCharacter)
		{
			this.IsSoldier = originCharacter.IsSoldier;
			this._isBasicHero = originCharacter._isBasicHero;
			this.DefaultCharacterSkills = originCharacter.DefaultCharacterSkills;
			this.BodyPropertyRange = originCharacter.BodyPropertyRange;
			this.IsFemale = originCharacter.IsFemale;
			this.Race = originCharacter.Race;
			this.Culture = originCharacter.Culture;
			this.DefaultFormationGroup = originCharacter.DefaultFormationGroup;
			this.DefaultFormationClass = originCharacter.DefaultFormationClass;
			this.FormationPositionPreference = originCharacter.FormationPositionPreference;
			this._equipmentRoster = originCharacter._equipmentRoster;
			this.KnockbackResistance = originCharacter.KnockbackResistance;
			this.KnockdownResistance = originCharacter.KnockdownResistance;
			this.DismountResistance = originCharacter.DismountResistance;
		}

		// Token: 0x0600015E RID: 350 RVA: 0x00005AA0 File Offset: 0x00003CA0
		public override void Deserialize(MBObjectManager objectManager, XmlNode node)
		{
			base.Deserialize(objectManager, node);
			XmlAttribute xmlAttribute = node.Attributes["name"];
			if (xmlAttribute != null)
			{
				this.SetName(new TextObject(xmlAttribute.Value, null));
			}
			this.Race = 0;
			XmlAttribute xmlAttribute2 = node.Attributes["race"];
			if (xmlAttribute2 != null)
			{
				this.Race = FaceGen.GetRaceOrDefault(xmlAttribute2.Value);
			}
			XmlNode xmlNode = node.Attributes["occupation"];
			if (xmlNode != null)
			{
				this.IsSoldier = xmlNode.InnerText.IndexOf("soldier", StringComparison.OrdinalIgnoreCase) >= 0;
			}
			this._isBasicHero = XmlHelper.ReadBool(node, "is_hero");
			this.FaceMeshCache = XmlHelper.ReadBool(node, "face_mesh_cache");
			this.IsObsolete = XmlHelper.ReadBool(node, "is_obsolete");
			MBCharacterSkills mbcharacterSkills = objectManager.ReadObjectReferenceFromXml("skill_template", typeof(MBCharacterSkills), node) as MBCharacterSkills;
			if (mbcharacterSkills != null)
			{
				this.DefaultCharacterSkills = mbcharacterSkills;
			}
			else
			{
				this.DefaultCharacterSkills = MBObjectManager.Instance.CreateObject<MBCharacterSkills>(base.StringId);
			}
			BodyProperties bodyPropertyMin = default(BodyProperties);
			BodyProperties bodyProperties = default(BodyProperties);
			string text = "";
			string text2 = "";
			string text3 = "";
			foreach (object obj in node.ChildNodes)
			{
				XmlNode xmlNode2 = (XmlNode)obj;
				if (xmlNode2.Name == "Skills" || xmlNode2.Name == "skills")
				{
					if (mbcharacterSkills == null)
					{
						this.DefaultCharacterSkills.Init(objectManager, xmlNode2);
					}
				}
				else if (xmlNode2.Name == "Equipments" || xmlNode2.Name == "equipments")
				{
					List<XmlNode> list = new List<XmlNode>();
					foreach (object obj2 in xmlNode2.ChildNodes)
					{
						XmlNode xmlNode3 = (XmlNode)obj2;
						if (xmlNode3.Name == "equipment")
						{
							list.Add(xmlNode3);
						}
					}
					foreach (object obj3 in xmlNode2.ChildNodes)
					{
						XmlNode xmlNode4 = (XmlNode)obj3;
						if (xmlNode4.Name == "EquipmentRoster" || xmlNode4.Name == "equipmentRoster")
						{
							if (this._equipmentRoster == null)
							{
								this._equipmentRoster = MBObjectManager.Instance.CreateObject<MBEquipmentRoster>(base.StringId);
							}
							this._equipmentRoster.Init(objectManager, xmlNode4);
						}
						else if (xmlNode4.Name == "EquipmentSet" || xmlNode4.Name == "equipmentSet")
						{
							string innerText = xmlNode4.Attributes["id"].InnerText;
							Equipment.EquipmentType equipmentType = Equipment.EquipmentType.Battle;
							if (xmlNode4.Attributes["equipmentType"] != null)
							{
								if (!Enum.TryParse<Equipment.EquipmentType>(xmlNode4.Attributes["equipmentType"].Value, out equipmentType))
								{
									Debug.FailedAssert("This equipment definition is wrong", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.Core\\BasicCharacterObject.cs", "Deserialize", 450);
								}
							}
							else if (xmlNode4.Attributes["civilian"] != null && bool.Parse(xmlNode4.Attributes["civilian"].InnerText))
							{
								equipmentType = Equipment.EquipmentType.Civilian;
							}
							if (this._equipmentRoster == null)
							{
								this._equipmentRoster = MBObjectManager.Instance.CreateObject<MBEquipmentRoster>(base.StringId);
							}
							this._equipmentRoster.AddEquipmentRoster(MBObjectManager.Instance.GetObject<MBEquipmentRoster>(innerText), equipmentType);
						}
					}
					if (list.Count > 0)
					{
						this._equipmentRoster.AddOverridenEquipments(objectManager, list);
					}
				}
				else
				{
					if (xmlNode2.Name == "face")
					{
						using (IEnumerator enumerator2 = xmlNode2.ChildNodes.GetEnumerator())
						{
							while (enumerator2.MoveNext())
							{
								object obj4 = enumerator2.Current;
								XmlNode xmlNode5 = (XmlNode)obj4;
								if (xmlNode5.Name == "hair_tags")
								{
									using (IEnumerator enumerator3 = xmlNode5.ChildNodes.GetEnumerator())
									{
										while (enumerator3.MoveNext())
										{
											object obj5 = enumerator3.Current;
											XmlNode xmlNode6 = (XmlNode)obj5;
											text = text + xmlNode6.Attributes["name"].Value + ",";
										}
										continue;
									}
								}
								if (xmlNode5.Name == "beard_tags")
								{
									using (IEnumerator enumerator3 = xmlNode5.ChildNodes.GetEnumerator())
									{
										while (enumerator3.MoveNext())
										{
											object obj6 = enumerator3.Current;
											XmlNode xmlNode7 = (XmlNode)obj6;
											text2 = text2 + xmlNode7.Attributes["name"].Value + ",";
										}
										continue;
									}
								}
								if (xmlNode5.Name == "tattoo_tags")
								{
									using (IEnumerator enumerator3 = xmlNode5.ChildNodes.GetEnumerator())
									{
										while (enumerator3.MoveNext())
										{
											object obj7 = enumerator3.Current;
											XmlNode xmlNode8 = (XmlNode)obj7;
											text3 = text3 + xmlNode8.Attributes["name"].Value + ",";
										}
										continue;
									}
								}
								if (xmlNode5.Name == "BodyProperties")
								{
									if (!BodyProperties.FromXmlNode(xmlNode5, out bodyPropertyMin))
									{
										Debug.FailedAssert("cannot read body properties", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.Core\\BasicCharacterObject.cs", "Deserialize", 509);
									}
								}
								else if (xmlNode5.Name == "BodyPropertiesMax")
								{
									if (!BodyProperties.FromXmlNode(xmlNode5, out bodyProperties))
									{
										bodyPropertyMin = bodyProperties;
										Debug.FailedAssert("cannot read max body properties", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.Core\\BasicCharacterObject.cs", "Deserialize", 518);
									}
								}
								else if (xmlNode5.Name == "face_key_template")
								{
									MBBodyProperty bodyPropertyRange = objectManager.ReadObjectReferenceFromXml<MBBodyProperty>("value", xmlNode5);
									this.BodyPropertyRange = bodyPropertyRange;
								}
							}
							continue;
						}
					}
					if (xmlNode2.Name == "Resistances" || xmlNode2.Name == "resistances")
					{
						this.KnockbackResistance = XmlHelper.ReadFloat(xmlNode2, "knockback", 25f) * 0.01f;
						this.KnockbackResistance = MBMath.ClampFloat(this.KnockbackResistance, 0f, 1f);
						this.KnockdownResistance = XmlHelper.ReadFloat(xmlNode2, "knockdown", 50f) * 0.01f;
						this.KnockdownResistance = MBMath.ClampFloat(this.KnockdownResistance, 0f, 1f);
						this.DismountResistance = XmlHelper.ReadFloat(xmlNode2, "dismount", 50f) * 0.01f;
						this.DismountResistance = MBMath.ClampFloat(this.DismountResistance, 0f, 1f);
					}
				}
			}
			if (this.BodyPropertyRange == null)
			{
				this.BodyPropertyRange = MBObjectManager.Instance.RegisterPresumedObject<MBBodyProperty>(new MBBodyProperty(base.StringId));
				this.BodyPropertyRange.Init(bodyPropertyMin, bodyProperties);
			}
			this.IsFemale = false;
			this.DefaultFormationGroup = 0;
			XmlNode xmlNode9 = node.Attributes["is_female"];
			if (xmlNode9 != null)
			{
				this.IsFemale = Convert.ToBoolean(xmlNode9.InnerText);
			}
			this.Culture = objectManager.ReadObjectReferenceFromXml<BasicCultureObject>("culture", node);
			XmlNode xmlNode10 = node.Attributes["age"];
			this.Age = ((xmlNode10 == null) ? MathF.Max(20f, this.BodyPropertyRange.BodyPropertyMax.Age) : ((float)Convert.ToInt32(xmlNode10.InnerText)));
			XmlNode xmlNode11 = node.Attributes["level"];
			this.Level = ((xmlNode11 != null) ? Convert.ToInt32(xmlNode11.InnerText) : 1);
			XmlNode xmlNode12 = node.Attributes["default_group"];
			if (xmlNode12 != null)
			{
				this.DefaultFormationGroup = this.FetchDefaultFormationGroup(xmlNode12.InnerText);
			}
			this.DefaultFormationClass = (FormationClass)this.DefaultFormationGroup;
			this._isRanged = this.DefaultFormationClass.IsRanged();
			this._isMounted = this.DefaultFormationClass.IsMounted();
			XmlNode xmlNode13 = node.Attributes["formation_position_preference"];
			this.FormationPositionPreference = ((xmlNode13 != null) ? ((FormationPositionPreference)Enum.Parse(typeof(FormationPositionPreference), xmlNode13.InnerText)) : FormationPositionPreference.Middle);
			bool flag = !string.IsNullOrEmpty(text);
			bool flag2 = !string.IsNullOrEmpty(text2);
			bool flag3 = !string.IsNullOrEmpty(text3);
			if (flag || flag2 || flag3)
			{
				if (this.BodyPropertyRange.HairTags != text || this.BodyPropertyRange.BeardTags != text2 || this.BodyPropertyRange.TattooTags != text3)
				{
					this.BodyPropertyRange = MBBodyProperty.CreateFrom(this.BodyPropertyRange);
				}
				if (flag)
				{
					this.BodyPropertyRange.HairTags = text;
				}
				if (flag2)
				{
					this.BodyPropertyRange.BeardTags = text2;
				}
				if (flag3)
				{
					this.BodyPropertyRange.TattooTags = text3;
				}
			}
			XmlNode xmlNode14 = node.Attributes["default_equipment_set"];
			if (xmlNode14 != null)
			{
				this._equipmentRoster.InitializeDefaultEquipment(xmlNode14.Value);
			}
			MBEquipmentRoster equipmentRoster = this._equipmentRoster;
			if (equipmentRoster == null)
			{
				return;
			}
			equipmentRoster.OrderEquipments();
		}

		// Token: 0x0600015F RID: 351 RVA: 0x000064E0 File Offset: 0x000046E0
		protected void AddEquipment(MBEquipmentRoster equipmentRoster, Equipment.EquipmentType equipmentType)
		{
			this._equipmentRoster.AddEquipmentRoster(equipmentRoster, equipmentType);
		}

		// Token: 0x06000160 RID: 352 RVA: 0x000064F0 File Offset: 0x000046F0
		protected int FetchDefaultFormationGroup(string innerText)
		{
			FormationClass result;
			if (Enum.TryParse<FormationClass>(innerText, true, out result))
			{
				return (int)result;
			}
			return -1;
		}

		// Token: 0x06000161 RID: 353 RVA: 0x0000650B File Offset: 0x0000470B
		public virtual FormationClass GetFormationClass()
		{
			return this.DefaultFormationClass;
		}

		// Token: 0x06000162 RID: 354 RVA: 0x00006513 File Offset: 0x00004713
		internal static void AutoGeneratedStaticCollectObjectsBasicCharacterObject(object o, List<object> collectedObjects)
		{
			((BasicCharacterObject)o).AutoGeneratedInstanceCollectObjects(collectedObjects);
		}

		// Token: 0x06000163 RID: 355 RVA: 0x00006521 File Offset: 0x00004721
		protected override void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects)
		{
			base.AutoGeneratedInstanceCollectObjects(collectedObjects);
		}

		// Token: 0x04000134 RID: 308
		public static readonly int SkillAffectingMaxLevel = 32;

		// Token: 0x04000135 RID: 309
		public const float DefaultKnockbackResistance = 25f;

		// Token: 0x04000136 RID: 310
		public const float DefaultKnockdownResistance = 50f;

		// Token: 0x04000137 RID: 311
		public const float DefaultDismountResistance = 50f;

		// Token: 0x04000138 RID: 312
		public const int MaxBattleTier = 7;

		// Token: 0x04000139 RID: 313
		protected TextObject _basicName;

		// Token: 0x0400013D RID: 317
		private bool _isMounted;

		// Token: 0x0400013E RID: 318
		private bool _isRanged;

		// Token: 0x04000146 RID: 326
		private MBEquipmentRoster _equipmentRoster;

		// Token: 0x04000149 RID: 329
		private BasicCultureObject _culture;

		// Token: 0x0400014A RID: 330
		[CachedData]
		private float _age;

		// Token: 0x0400014C RID: 332
		[CachedData]
		private bool _isBasicHero;

		// Token: 0x0400014E RID: 334
		protected MBCharacterSkills DefaultCharacterSkills;
	}
}
