using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace TaleWorlds.Core;

public class BasicCharacterObject : MBObjectBase
{
	public static readonly int SkillAffectingMaxLevel = 32;

	public const float DefaultKnockbackResistance = 25f;

	public const float DefaultKnockdownResistance = 50f;

	public const float DefaultDismountResistance = 50f;

	public const int MaxBattleTier = 7;

	protected TextObject _basicName;

	private bool _isMounted;

	private bool _isRanged;

	private MBEquipmentRoster _equipmentRoster;

	private BasicCultureObject _culture;

	[CachedData]
	private float _age;

	[CachedData]
	private bool _isBasicHero;

	protected MBCharacterSkills DefaultCharacterSkills;

	public virtual TextObject Name => _basicName;

	public virtual MBBodyProperty BodyPropertyRange { get; protected set; }

	public int DefaultFormationGroup { get; set; }

	public FormationClass DefaultFormationClass { get; protected set; }

	public float KnockbackResistance { get; private set; }

	public float KnockdownResistance { get; private set; }

	public float DismountResistance { get; private set; }

	public FormationPositionPreference FormationPositionPreference { get; protected set; }

	public bool IsInfantry
	{
		get
		{
			if (!IsRanged)
			{
				return !IsMounted;
			}
			return false;
		}
	}

	public virtual bool IsMounted => _isMounted;

	public virtual bool IsRanged => _isRanged;

	public float SkillFactor => (float)TaleWorlds.Library.MathF.Min(Level, SkillAffectingMaxLevel) / (float)SkillAffectingMaxLevel;

	public int Race { get; set; }

	public virtual bool IsFemale { get; set; }

	public bool FaceMeshCache { get; private set; }

	protected virtual MBReadOnlyList<Equipment> AllEquipments
	{
		get
		{
			if (_equipmentRoster == null)
			{
				return new MBList<Equipment> { MBEquipmentRoster.EmptyEquipment };
			}
			return _equipmentRoster.AllEquipments;
		}
	}

	public virtual Equipment Equipment
	{
		get
		{
			if (_equipmentRoster == null)
			{
				return MBEquipmentRoster.EmptyEquipment;
			}
			return _equipmentRoster.DefaultEquipment;
		}
	}

	public virtual IEnumerable<Equipment> BattleEquipments => AllEquipments.WhereQ((Equipment e) => e.IsBattle);

	public virtual Equipment FirstBattleEquipment => AllEquipments.FirstOrDefaultQ((Equipment e) => e.IsBattle);

	public virtual Equipment RandomBattleEquipment => AllEquipments.GetRandomElementWithPredicate((Equipment e) => e.IsBattle);

	public virtual IEnumerable<Equipment> CivilianEquipments => AllEquipments.WhereQ((Equipment e) => e.IsCivilian);

	public virtual Equipment FirstCivilianEquipment => AllEquipments.FirstOrDefaultQ((Equipment e) => e.IsCivilian);

	public virtual Equipment RandomCivilianEquipment => AllEquipments.GetRandomElementWithPredicate((Equipment e) => e.IsCivilian);

	public virtual Equipment GetRandomEquipment => AllEquipments.GetRandomElementWithPredicate((Equipment x) => !x.IsEmpty());

	public bool IsObsolete { get; private set; }

	public virtual int Level { get; set; }

	public BasicCultureObject Culture
	{
		get
		{
			return _culture;
		}
		set
		{
			_culture = value;
		}
	}

	public virtual bool IsPlayerCharacter => Game.Current.PlayerTroop == this;

	public virtual float Age
	{
		get
		{
			return _age;
		}
		set
		{
			_age = value;
		}
	}

	public virtual int HitPoints => MaxHitPoints();

	public float FaceDirtAmount { get; set; }

	public virtual bool IsHero => _isBasicHero;

	public bool IsSoldier { get; private set; }

	private void SetName(TextObject name)
	{
		_basicName = name;
	}

	public override TextObject GetName()
	{
		return Name;
	}

	public override string ToString()
	{
		return Name.ToString();
	}

	public void InitializeEquipmentsOnLoad(BasicCharacterObject character)
	{
		_equipmentRoster = character._equipmentRoster;
	}

	public Equipment GetFirstEquipment(Func<Equipment, bool> predicate)
	{
		Equipment equipment = AllEquipments.FirstOrDefault(predicate);
		if (equipment != null)
		{
			return equipment;
		}
		return Equipment;
	}

	public virtual BodyProperties GetBodyPropertiesMin(bool returnBaseValue = false)
	{
		return BodyPropertyRange.BodyPropertyMin;
	}

	protected void FillFrom(BasicCharacterObject character)
	{
		_culture = character._culture;
		DefaultFormationClass = character.DefaultFormationClass;
		DefaultFormationGroup = character.DefaultFormationGroup;
		BodyPropertyRange = character.BodyPropertyRange;
		FormationPositionPreference = character.FormationPositionPreference;
		IsFemale = character.IsFemale;
		Race = character.Race;
		Level = character.Level;
		_basicName = character._basicName;
		_age = character._age;
		KnockbackResistance = character.KnockbackResistance;
		KnockdownResistance = character.KnockdownResistance;
		DismountResistance = character.DismountResistance;
		DefaultCharacterSkills = character.DefaultCharacterSkills;
		InitializeEquipmentsOnLoad(character);
	}

	public virtual BodyProperties GetBodyPropertiesMax(bool returnBaseValue = false)
	{
		return BodyPropertyRange.BodyPropertyMax;
	}

	public virtual BodyProperties GetBodyProperties(Equipment equipment, int seed = -1)
	{
		BodyProperties bodyPropertiesMin = GetBodyPropertiesMin();
		BodyProperties bodyPropertiesMax = GetBodyPropertiesMax();
		return FaceGen.GetRandomBodyProperties(Race, IsFemale, bodyPropertiesMin, bodyPropertiesMax, (int)(equipment?.HairCoverType ?? ArmorComponent.HairCoverTypes.None), seed, BodyPropertyRange.HairTags, BodyPropertyRange.BeardTags, BodyPropertyRange.TattooTags, 0f);
	}

	public virtual void UpdatePlayerCharacterBodyProperties(BodyProperties properties, int race, bool isFemale)
	{
		BodyPropertyRange.Init(properties, properties);
		Race = race;
		IsFemale = isFemale;
	}

	public BasicCharacterObject()
	{
		DefaultFormationClass = FormationClass.Infantry;
	}

	public int GetDefaultFaceSeed(int rank)
	{
		int num = base.StringId.GetDeterministicHashCode() * 6791 + rank * 197;
		return ((num >= 0) ? num : (-num)) % 2000;
	}

	public float GetStepSize()
	{
		return Math.Min(0.8f + 0.2f * (float)GetSkillValue(DefaultSkills.Athletics) * 0.00333333f, 1f);
	}

	public bool HasMount()
	{
		return Equipment[10].Item != null;
	}

	public virtual int MaxHitPoints()
	{
		return FaceGen.GetBaseMonsterFromRace(Race).HitPoints;
	}

	public virtual float GetPower()
	{
		int num = Level + 10;
		return 0.2f + (float)(num * num) * 0.0025f;
	}

	public virtual float GetBattlePower()
	{
		return 1f;
	}

	public virtual float GetMoraleResistance()
	{
		return 1f;
	}

	public virtual int GetMountKeySeed()
	{
		return MBRandom.RandomInt();
	}

	public virtual int GetBattleTier()
	{
		if (IsHero)
		{
			return 7;
		}
		return TaleWorlds.Library.MathF.Min(TaleWorlds.Library.MathF.Max(TaleWorlds.Library.MathF.Ceiling(((float)Level - 5f) / 5f), 0), 7);
	}

	public MBCharacterSkills GetDefaultCharacterSkills()
	{
		return DefaultCharacterSkills;
	}

	public virtual int GetSkillValue(SkillObject skill)
	{
		return DefaultCharacterSkills.Skills.GetPropertyValue(skill);
	}

	protected void InitializeHeroBasicCharacterOnAfterLoad(BasicCharacterObject originCharacter)
	{
		IsSoldier = originCharacter.IsSoldier;
		_isBasicHero = originCharacter._isBasicHero;
		DefaultCharacterSkills = originCharacter.DefaultCharacterSkills;
		BodyPropertyRange = originCharacter.BodyPropertyRange;
		IsFemale = originCharacter.IsFemale;
		Race = originCharacter.Race;
		Culture = originCharacter.Culture;
		DefaultFormationGroup = originCharacter.DefaultFormationGroup;
		DefaultFormationClass = originCharacter.DefaultFormationClass;
		FormationPositionPreference = originCharacter.FormationPositionPreference;
		_equipmentRoster = originCharacter._equipmentRoster;
		KnockbackResistance = originCharacter.KnockbackResistance;
		KnockdownResistance = originCharacter.KnockdownResistance;
		DismountResistance = originCharacter.DismountResistance;
	}

	public override void Deserialize(MBObjectManager objectManager, XmlNode node)
	{
		base.Deserialize(objectManager, node);
		XmlAttribute xmlAttribute = node.Attributes["name"];
		if (xmlAttribute != null)
		{
			SetName(new TextObject(xmlAttribute.Value));
		}
		Race = 0;
		XmlAttribute xmlAttribute2 = node.Attributes["race"];
		if (xmlAttribute2 != null)
		{
			Race = FaceGen.GetRaceOrDefault(xmlAttribute2.Value);
		}
		XmlNode xmlNode = node.Attributes["occupation"];
		if (xmlNode != null)
		{
			IsSoldier = xmlNode.InnerText.IndexOf("soldier", StringComparison.OrdinalIgnoreCase) >= 0;
		}
		_isBasicHero = XmlHelper.ReadBool(node, "is_hero");
		FaceMeshCache = XmlHelper.ReadBool(node, "face_mesh_cache");
		IsObsolete = XmlHelper.ReadBool(node, "is_obsolete");
		MBCharacterSkills mBCharacterSkills = objectManager.ReadObjectReferenceFromXml("skill_template", typeof(MBCharacterSkills), node) as MBCharacterSkills;
		if (mBCharacterSkills != null)
		{
			DefaultCharacterSkills = mBCharacterSkills;
		}
		else
		{
			DefaultCharacterSkills = MBObjectManager.Instance.CreateObject<MBCharacterSkills>(base.StringId);
		}
		BodyProperties bodyProperties = default(BodyProperties);
		BodyProperties bodyProperties2 = default(BodyProperties);
		string text = "";
		string text2 = "";
		string text3 = "";
		foreach (XmlNode childNode in node.ChildNodes)
		{
			if (childNode.Name == "Skills" || childNode.Name == "skills")
			{
				if (mBCharacterSkills == null)
				{
					DefaultCharacterSkills.Init(objectManager, childNode);
				}
			}
			else if (childNode.Name == "Equipments" || childNode.Name == "equipments")
			{
				List<XmlNode> list = new List<XmlNode>();
				foreach (XmlNode childNode2 in childNode.ChildNodes)
				{
					if (childNode2.Name == "equipment")
					{
						list.Add(childNode2);
					}
				}
				foreach (XmlNode childNode3 in childNode.ChildNodes)
				{
					if (childNode3.Name == "EquipmentRoster" || childNode3.Name == "equipmentRoster")
					{
						if (_equipmentRoster == null)
						{
							_equipmentRoster = MBObjectManager.Instance.CreateObject<MBEquipmentRoster>(base.StringId);
						}
						_equipmentRoster.Init(objectManager, childNode3);
					}
					else
					{
						if (!(childNode3.Name == "EquipmentSet") && !(childNode3.Name == "equipmentSet"))
						{
							continue;
						}
						string innerText = childNode3.Attributes["id"].InnerText;
						Equipment.EquipmentType result = Equipment.EquipmentType.Battle;
						if (childNode3.Attributes["equipmentType"] != null)
						{
							if (!Enum.TryParse<Equipment.EquipmentType>(childNode3.Attributes["equipmentType"].Value, out result))
							{
								Debug.FailedAssert("This equipment definition is wrong", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.Core\\BasicCharacterObject.cs", "Deserialize", 449);
							}
						}
						else if (childNode3.Attributes["civilian"] != null)
						{
							Debug.FailedAssert("This civilian tag should not be used anymore, the equipmentSet type should be defined as equipmentType=civilian in the .xml file", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.Core\\BasicCharacterObject.cs", "Deserialize", 454);
							if (bool.Parse(childNode3.Attributes["civilian"].InnerText))
							{
								result = Equipment.EquipmentType.Civilian;
							}
						}
						if (_equipmentRoster == null)
						{
							_equipmentRoster = MBObjectManager.Instance.CreateObject<MBEquipmentRoster>(base.StringId);
						}
						_equipmentRoster.AddEquipmentRoster(MBObjectManager.Instance.GetObject<MBEquipmentRoster>(innerText), result);
					}
				}
				if (list.Count > 0)
				{
					_equipmentRoster.AddOverriddenEquipments(objectManager, list);
				}
			}
			else if (childNode.Name == "face")
			{
				foreach (XmlNode childNode4 in childNode.ChildNodes)
				{
					if (childNode4.Name == "hair_tags")
					{
						foreach (XmlNode childNode5 in childNode4.ChildNodes)
						{
							text = text + childNode5.Attributes["name"].Value + ",";
						}
					}
					else if (childNode4.Name == "beard_tags")
					{
						foreach (XmlNode childNode6 in childNode4.ChildNodes)
						{
							text2 = text2 + childNode6.Attributes["name"].Value + ",";
						}
					}
					else if (childNode4.Name == "tattoo_tags")
					{
						foreach (XmlNode childNode7 in childNode4.ChildNodes)
						{
							text3 = text3 + childNode7.Attributes["name"].Value + ",";
						}
					}
					else if (childNode4.Name == "BodyProperties")
					{
						if (!BodyProperties.FromXmlNode(childNode4, out bodyProperties))
						{
							Debug.FailedAssert("cannot read body properties", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.Core\\BasicCharacterObject.cs", "Deserialize", 513);
						}
					}
					else if (childNode4.Name == "BodyPropertiesMax")
					{
						if (!BodyProperties.FromXmlNode(childNode4, out bodyProperties2))
						{
							bodyProperties = bodyProperties2;
							Debug.FailedAssert("cannot read max body properties", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.Core\\BasicCharacterObject.cs", "Deserialize", 522);
						}
					}
					else if (childNode4.Name == "face_key_template")
					{
						MBBodyProperty bodyPropertyRange = objectManager.ReadObjectReferenceFromXml<MBBodyProperty>("value", childNode4);
						BodyPropertyRange = bodyPropertyRange;
					}
				}
			}
			else if (childNode.Name == "Resistances" || childNode.Name == "resistances")
			{
				KnockbackResistance = XmlHelper.ReadFloat(childNode, "knockback", 25f) * 0.01f;
				KnockbackResistance = MBMath.ClampFloat(KnockbackResistance, 0f, 1f);
				KnockdownResistance = XmlHelper.ReadFloat(childNode, "knockdown", 50f) * 0.01f;
				KnockdownResistance = MBMath.ClampFloat(KnockdownResistance, 0f, 1f);
				DismountResistance = XmlHelper.ReadFloat(childNode, "dismount", 50f) * 0.01f;
				DismountResistance = MBMath.ClampFloat(DismountResistance, 0f, 1f);
			}
		}
		if (BodyPropertyRange == null)
		{
			BodyPropertyRange = MBObjectManager.Instance.RegisterPresumedObject(new MBBodyProperty(base.StringId));
			BodyPropertyRange.Init(bodyProperties, bodyProperties2);
		}
		IsFemale = false;
		DefaultFormationGroup = 0;
		XmlNode xmlNode9 = node.Attributes["is_female"];
		if (xmlNode9 != null)
		{
			IsFemale = Convert.ToBoolean(xmlNode9.InnerText);
		}
		Culture = objectManager.ReadObjectReferenceFromXml<BasicCultureObject>("culture", node);
		XmlNode xmlNode10 = node.Attributes["age"];
		Age = ((xmlNode10 == null) ? TaleWorlds.Library.MathF.Max(20f, BodyPropertyRange.BodyPropertyMax.Age) : ((float)Convert.ToInt32(xmlNode10.InnerText)));
		XmlNode xmlNode11 = node.Attributes["level"];
		Level = ((xmlNode11 == null) ? 1 : Convert.ToInt32(xmlNode11.InnerText));
		XmlNode xmlNode12 = node.Attributes["default_group"];
		if (xmlNode12 != null)
		{
			DefaultFormationGroup = FetchDefaultFormationGroup(xmlNode12.InnerText);
		}
		DefaultFormationClass = (FormationClass)DefaultFormationGroup;
		_isRanged = DefaultFormationClass.IsRanged();
		_isMounted = DefaultFormationClass.IsMounted();
		XmlNode xmlNode13 = node.Attributes["formation_position_preference"];
		FormationPositionPreference = ((xmlNode13 == null) ? FormationPositionPreference.Middle : ((FormationPositionPreference)Enum.Parse(typeof(FormationPositionPreference), xmlNode13.InnerText)));
		bool flag = !string.IsNullOrEmpty(text);
		bool flag2 = !string.IsNullOrEmpty(text2);
		bool flag3 = !string.IsNullOrEmpty(text3);
		if (flag || flag2 || flag3)
		{
			if (BodyPropertyRange.HairTags != text || BodyPropertyRange.BeardTags != text2 || BodyPropertyRange.TattooTags != text3)
			{
				BodyPropertyRange = MBBodyProperty.CreateFrom(BodyPropertyRange);
			}
			if (flag)
			{
				BodyPropertyRange.HairTags = text;
			}
			if (flag2)
			{
				BodyPropertyRange.BeardTags = text2;
			}
			if (flag3)
			{
				BodyPropertyRange.TattooTags = text3;
			}
		}
		XmlNode xmlNode14 = node.Attributes["default_equipment_set"];
		if (xmlNode14 != null)
		{
			_equipmentRoster.InitializeDefaultEquipment(xmlNode14.Value);
		}
		_equipmentRoster?.OrderEquipments();
	}

	protected void AddEquipment(MBEquipmentRoster equipmentRoster, Equipment.EquipmentType equipmentType)
	{
		_equipmentRoster.AddEquipmentRoster(equipmentRoster, equipmentType);
	}

	protected int FetchDefaultFormationGroup(string innerText)
	{
		if (Enum.TryParse<FormationClass>(innerText, ignoreCase: true, out var result))
		{
			return (int)result;
		}
		return -1;
	}

	public virtual FormationClass GetFormationClass()
	{
		return DefaultFormationClass;
	}

	internal static void AutoGeneratedStaticCollectObjectsBasicCharacterObject(object o, List<object> collectedObjects)
	{
		((BasicCharacterObject)o).AutoGeneratedInstanceCollectObjects(collectedObjects);
	}

	protected override void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects)
	{
		base.AutoGeneratedInstanceCollectObjects(collectedObjects);
	}
}
