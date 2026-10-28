using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Xml;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace TaleWorlds.Core;

public sealed class ItemObject : MBObjectBase
{
	public enum ItemUsageSetFlags
	{
		RequiresMount = 1,
		RequiresNoMount = 2,
		RequiresShield = 4,
		RequiresNoShield = 8,
		PassiveUsage = 0x10
	}

	public enum ItemTypeEnum
	{
		Invalid,
		Horse,
		OneHandedWeapon,
		TwoHandedWeapon,
		Polearm,
		Arrows,
		Bolts,
		SlingStones,
		Shield,
		Bow,
		Crossbow,
		Sling,
		Thrown,
		Goods,
		HeadArmor,
		BodyArmor,
		LegArmor,
		HandArmor,
		Pistol,
		Musket,
		Bullets,
		Animal,
		Book,
		ChestArmor,
		Cape,
		HorseHarness,
		Banner
	}

	public enum ItemTiers
	{
		Tier1,
		Tier2,
		Tier3,
		Tier4,
		Tier5,
		Tier6,
		NumTiers
	}

	public const float DefaultAppearanceValue = 0.5f;

	public const int MaxHolsterSlotCount = 4;

	private const float TierfOverrideAdjustmentValue = 1f;

	public ItemTypeEnum Type;

	public ItemComponent ItemComponent { get; private set; }

	public string MultiMeshName { get; private set; }

	public string HolsterMeshName { get; private set; }

	public string HolsterWithWeaponMeshName { get; private set; }

	public string[] ItemHolsters { get; private set; }

	public Vec3 HolsterPositionShift { get; private set; }

	public bool HasLowerHolsterPriority { get; private set; }

	public string FlyingMeshName { get; private set; }

	public string BodyName { get; private set; }

	public string SkeletonName { get; private set; }

	public string StaticAnimationName { get; private set; }

	public string HolsterBodyName { get; private set; }

	public string CollisionBodyName { get; private set; }

	public bool RecalculateBody { get; private set; }

	public string PrefabName { get; private set; }

	public TextObject Name { get; private set; }

	public ItemFlags ItemFlags { get; private set; }

	public ItemCategory ItemCategory { get; private set; }

	public int Value { get; private set; }

	public float Effectiveness { get; private set; }

	public float Weight { get; private set; }

	public int Difficulty { get; private set; }

	public float Appearance { get; private set; }

	public bool IsUsingTableau { get; private set; }

	public bool IsUsingTeamColor => ItemFlags.HasAnyFlag(ItemFlags.UseTeamColor);

	public bool DoesNotHideChest => ItemFlags.HasAnyFlag(ItemFlags.DoesNotHideChest);

	public bool IsCivilian => ItemFlags.HasAnyFlag(ItemFlags.Civilian);

	public bool IsStealthItem => ItemFlags.HasAnyFlag(ItemFlags.Stealth);

	public bool UsingFacegenScaling
	{
		get
		{
			if (Type == ItemTypeEnum.HeadArmor)
			{
				return ArmorComponent.MeshesMask.HasAnyFlag(SkinMask.HeadVisible);
			}
			return false;
		}
	}

	public string ArmBandMeshName { get; private set; }

	public bool IsFood { get; private set; }

	public bool IsUniqueItem { get; private set; }

	public float ScaleFactor { get; private set; }

	public BasicCultureObject Culture { get; private set; }

	public bool MultiplayerItem { get; private set; }

	public bool NotMerchandise { get; private set; }

	public bool IsCraftedByPlayer { get; private set; }

	public int LodAtlasIndex { get; private set; }

	private float TierfOverride { get; set; }

	public bool IsTransferable => Game.Current.BasicModels.ItemValueModel.GetIsTransferable(this);

	public float Tierf
	{
		get
		{
			if (TierfOverride >= 1f)
			{
				return TierfOverride - 1f;
			}
			return Game.Current.BasicModels.ItemValueModel.CalculateTier(this);
		}
	}

	public bool IsCraftedWeapon => WeaponDesign != null;

	public ItemTiers Tier
	{
		get
		{
			if (ItemComponent == null)
			{
				return ItemTiers.Tier1;
			}
			return (ItemTiers)(MBMath.ClampInt(TaleWorlds.Library.MathF.Round(Tierf), 0, 6) - 1);
		}
	}

	public WeaponDesign WeaponDesign { get; private set; }

	public WeaponComponentData PrimaryWeapon => WeaponComponent?.PrimaryWeapon;

	public WeaponComponent WeaponComponent => ItemComponent as WeaponComponent;

	public bool HasWeaponComponent => WeaponComponent != null;

	public HorseComponent HorseComponent => ItemComponent as HorseComponent;

	public bool HasHorseComponent => HorseComponent != null;

	public ArmorComponent ArmorComponent => ItemComponent as ArmorComponent;

	public bool HasArmorComponent => ArmorComponent != null;

	public BannerComponent BannerComponent => ItemComponent as BannerComponent;

	public bool HasBannerComponent => BannerComponent != null;

	public SaddleComponent SaddleComponent => ItemComponent as SaddleComponent;

	public bool HasSaddleComponent => SaddleComponent != null;

	public TradeItemComponent FoodComponent => ItemComponent as TradeItemComponent;

	public bool HasFoodComponent => FoodComponent != null;

	public MBReadOnlyList<WeaponComponentData> Weapons => WeaponComponent?.Weapons;

	public ItemTypeEnum ItemType
	{
		get
		{
			return Type;
		}
		private set
		{
			Type = value;
		}
	}

	public bool IsMountable
	{
		get
		{
			if (HasHorseComponent)
			{
				return HorseComponent.IsRideable;
			}
			return false;
		}
	}

	public bool IsTradeGood => ItemType == ItemTypeEnum.Goods;

	public bool IsBannerItem => ItemType == ItemTypeEnum.Banner;

	public bool IsAnimal
	{
		get
		{
			if (HasHorseComponent)
			{
				return !HorseComponent.IsRideable;
			}
			return false;
		}
	}

	public SkillObject RelevantSkill
	{
		get
		{
			SkillObject result = null;
			if (PrimaryWeapon != null)
			{
				result = PrimaryWeapon.RelevantSkill;
			}
			else if (HasHorseComponent)
			{
				result = DefaultSkills.Riding;
			}
			return result;
		}
	}

	internal static void AutoGeneratedStaticCollectObjectsItemObject(object o, List<object> collectedObjects)
	{
		((ItemObject)o).AutoGeneratedInstanceCollectObjects(collectedObjects);
	}

	protected override void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects)
	{
		base.AutoGeneratedInstanceCollectObjects(collectedObjects);
	}

	public ItemObject()
	{
	}

	public ItemObject(string stringId)
		: base(stringId)
	{
	}

	public ItemObject(ItemObject itemToCopy)
		: base(itemToCopy)
	{
		ItemComponent = itemToCopy.ItemComponent;
		MultiMeshName = itemToCopy.MultiMeshName;
		HolsterMeshName = itemToCopy.HolsterMeshName;
		HolsterWithWeaponMeshName = itemToCopy.HolsterWithWeaponMeshName;
		ItemHolsters = itemToCopy.ItemHolsters;
		HolsterPositionShift = itemToCopy.HolsterPositionShift;
		FlyingMeshName = itemToCopy.FlyingMeshName;
		BodyName = itemToCopy.BodyName;
		SkeletonName = itemToCopy.SkeletonName;
		StaticAnimationName = itemToCopy.StaticAnimationName;
		HolsterBodyName = itemToCopy.HolsterBodyName;
		CollisionBodyName = itemToCopy.CollisionBodyName;
		RecalculateBody = itemToCopy.RecalculateBody;
		PrefabName = itemToCopy.PrefabName;
		Name = itemToCopy.Name;
		ItemFlags = itemToCopy.ItemFlags;
		Value = itemToCopy.Value;
		Weight = itemToCopy.Weight;
		Difficulty = itemToCopy.Difficulty;
		ArmBandMeshName = itemToCopy.ArmBandMeshName;
		IsFood = itemToCopy.IsFood;
		Type = itemToCopy.Type;
		ScaleFactor = itemToCopy.ScaleFactor;
		IsUniqueItem = false;
	}

	internal void SetCraftedWeaponName(TextObject weaponName)
	{
		Name = weaponName;
		if (WeaponDesign != null)
		{
			WeaponDesign.SetWeaponName(Name);
		}
	}

	public static ItemObject InitializeTradeGood(ItemObject item, TextObject name, string meshName, ItemCategory category, int value, float weight, ItemTypeEnum itemType, bool isFood = false)
	{
		item.Initialize();
		item.Name = name;
		item.MultiMeshName = meshName;
		item.ItemCategory = category;
		item.Value = value;
		item.Weight = weight;
		item.ItemType = itemType;
		item.IsFood = isFood;
		item.ItemComponent = new TradeItemComponent();
		item.AfterInitialized();
		item.ItemFlags |= ItemFlags.Civilian;
		return item;
	}

	public static void InitAsPlayerCraftedItem(ref ItemObject itemObject)
	{
		itemObject.IsCraftedByPlayer = true;
	}

	internal static void InitCraftedItemObject(ref ItemObject itemObject, TextObject name, BasicCultureObject culture, ItemFlags itemProperties, float weight, float appearance, WeaponDesign craftedData, ItemTypeEnum itemType)
	{
		BladeData bladeData = craftedData.UsedPieces[0].CraftingPiece.BladeData;
		itemObject.Weight = weight;
		itemObject.Name = name;
		itemObject.MultiMeshName = "";
		itemObject.HolsterMeshName = "";
		itemObject.HolsterWithWeaponMeshName = "";
		itemObject.ItemHolsters = (string[])craftedData.Template.ItemHolsters.Clone();
		itemObject.HolsterPositionShift = craftedData.HolsterShiftAmount;
		itemObject.FlyingMeshName = "";
		itemObject.BodyName = bladeData?.BodyName;
		itemObject.HolsterBodyName = bladeData?.HolsterBodyName ?? bladeData?.BodyName;
		itemObject.CollisionBodyName = "";
		itemObject.RecalculateBody = true;
		itemObject.Culture = culture;
		itemObject.Difficulty = 0;
		itemObject.ScaleFactor = 1f;
		itemObject.Type = itemType;
		itemObject.ItemFlags = itemProperties;
		itemObject.Appearance = appearance;
		itemObject.WeaponDesign = craftedData;
	}

	public override int GetHashCode()
	{
		return (int)base.Id.SubId;
	}

	public void SetItemFlagsForCosmetics(ItemFlags newFlags)
	{
		ItemFlags = newFlags;
	}

	public void DetermineItemCategoryForItem()
	{
		if (Game.Current.BasicModels.ItemCategorySelector != null && ItemCategory == null)
		{
			ItemCategory = Game.Current.BasicModels.ItemCategorySelector.GetItemCategoryForItem(this);
		}
	}

	public static ItemObject GetCraftedItemObjectFromHashedCode(string hashedCode)
	{
		foreach (ItemObject objectType in MBObjectManager.Instance.GetObjectTypeList<ItemObject>())
		{
			if (objectType.IsCraftedWeapon && objectType.WeaponDesign.HashedCode == hashedCode)
			{
				return objectType;
			}
		}
		return null;
	}

	public void AddWeapon(WeaponComponentData weapon, ItemModifierGroup itemModifierGroup)
	{
		if (WeaponComponent == null)
		{
			ItemComponent = new WeaponComponent(this);
		}
		WeaponComponent.AddWeapon(weapon, itemModifierGroup);
	}

	public override void Deserialize(MBObjectManager objectManager, XmlNode node)
	{
		base.Deserialize(objectManager, node);
		if (node.Name == "CraftedItem")
		{
			XmlNode xmlNode = node.Attributes["multiplayer_item"];
			if (xmlNode != null && !string.IsNullOrEmpty(xmlNode.InnerText))
			{
				MultiplayerItem = xmlNode.InnerText == "true";
			}
			XmlNode xmlNode2 = node.Attributes["is_merchandise"];
			if (xmlNode2 != null && !string.IsNullOrEmpty(xmlNode2.InnerText))
			{
				NotMerchandise = xmlNode2.InnerText != "true";
			}
			TextObject craftedWeaponName = new TextObject(node.Attributes["name"].InnerText);
			string innerText = node.Attributes["crafting_template"].InnerText;
			bool num = node.Attributes["has_modifier"] == null || node.Attributes["has_modifier"].InnerText != "false";
			string text = node.Attributes["modifier_group"]?.Value;
			ItemModifierGroup itemModifierGroup = null;
			if (num)
			{
				itemModifierGroup = ((text != null) ? Game.Current.ObjectManager.GetObject<ItemModifierGroup>(text) : CraftingTemplate.GetTemplateFromId(innerText).ItemModifierGroup);
			}
			WeaponDesignElement[] array = new WeaponDesignElement[4];
			XmlNode xmlNode3 = null;
			for (int i = 0; i < node.ChildNodes.Count; i++)
			{
				if (node.ChildNodes[i].Name == "Pieces")
				{
					xmlNode3 = node.ChildNodes[i];
					break;
				}
			}
			foreach (XmlNode childNode in xmlNode3.ChildNodes)
			{
				if (childNode.Name == "Piece")
				{
					XmlAttribute? xmlAttribute = childNode.Attributes["id"];
					XmlAttribute xmlAttribute2 = childNode.Attributes["Type"];
					XmlAttribute xmlAttribute3 = childNode.Attributes["scale_factor"];
					string innerText2 = xmlAttribute.InnerText;
					CraftingPiece.PieceTypes pieceTypes = (CraftingPiece.PieceTypes)Enum.Parse(typeof(CraftingPiece.PieceTypes), xmlAttribute2.InnerText);
					CraftingPiece craftingPiece = MBObjectManager.Instance.GetObject<CraftingPiece>(innerText2);
					array[(int)pieceTypes] = WeaponDesignElement.CreateUsablePiece(craftingPiece);
					if (xmlAttribute3 != null)
					{
						array[(int)pieceTypes].SetScale(int.Parse(xmlAttribute3.Value));
					}
				}
			}
			ItemObject itemObject = Crafting.CreatePreCraftedWeaponOnDeserialize(this, array, innerText, craftedWeaponName, itemModifierGroup);
			if (itemObject.WeaponComponent == null)
			{
				TaleWorlds.Library.Debug.FailedAssert("Crafted item: " + itemObject.StringId + " can not be initialized, item replaced with Trash item.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.Core\\ItemObject.cs", "Deserialize", 448);
				MBObjectManager.Instance.UnregisterObject(this);
				return;
			}
			Effectiveness = CalculateEffectiveness();
			if (node.Attributes["value"] != null)
			{
				Value = int.Parse(node.Attributes["value"].Value);
			}
			else
			{
				DetermineValue();
			}
			if (node.Attributes["culture"] != null)
			{
				Culture = (BasicCultureObject)objectManager.ReadObjectReferenceFromXml("culture", typeof(BasicCultureObject), node);
			}
		}
		else
		{
			Name = new TextObject(node.Attributes["name"].InnerText);
			XmlNode xmlNode5 = node.Attributes["multiplayer_item"];
			if (xmlNode5 != null && !string.IsNullOrEmpty(xmlNode5.InnerText))
			{
				MultiplayerItem = xmlNode5.InnerText == "true";
			}
			XmlNode xmlNode6 = node.Attributes["is_merchandise"];
			if (xmlNode6 != null && !string.IsNullOrEmpty(xmlNode6.InnerText))
			{
				NotMerchandise = xmlNode6.InnerText != "true";
			}
			XmlNode xmlNode7 = node.Attributes["mesh"];
			if (xmlNode7 != null && !string.IsNullOrEmpty(xmlNode7.InnerText))
			{
				MultiMeshName = xmlNode7.InnerText;
			}
			HolsterMeshName = ((node.Attributes["holster_mesh"] != null) ? node.Attributes["holster_mesh"].Value : null);
			HolsterWithWeaponMeshName = ((node.Attributes["holster_mesh_with_weapon"] != null) ? node.Attributes["holster_mesh_with_weapon"].Value : null);
			FlyingMeshName = ((node.Attributes["flying_mesh"] != null) ? node.Attributes["flying_mesh"].Value : null);
			HasLowerHolsterPriority = false;
			if (node.Attributes["item_holsters"] != null)
			{
				ItemHolsters = node.Attributes["item_holsters"].Value.Split(new char[1] { ':' });
				if (node.Attributes["has_lower_holster_priority"] != null)
				{
					HasLowerHolsterPriority = bool.Parse(node.Attributes["has_lower_holster_priority"].Value);
				}
			}
			else
			{
				ItemHolsters = new string[4];
			}
			HolsterPositionShift = ((node.Attributes["holster_position_shift"] != null) ? Vec3.Parse(node.Attributes["holster_position_shift"].Value) : Vec3.Zero);
			BodyName = ((node.Attributes["body_name"] != null) ? node.Attributes["body_name"].Value : null);
			SkeletonName = ((node.Attributes["skeleton_name"] != null) ? node.Attributes["skeleton_name"].Value : null);
			StaticAnimationName = ((node.Attributes["static_animation_name"] != null) ? node.Attributes["static_animation_name"].Value : null);
			HolsterBodyName = ((node.Attributes["holster_body_name"] != null) ? node.Attributes["holster_body_name"].Value : null);
			CollisionBodyName = ((node.Attributes["shield_body_name"] != null) ? node.Attributes["shield_body_name"].Value : null);
			RecalculateBody = node.Attributes["recalculate_body"] != null && bool.Parse(node.Attributes["recalculate_body"].Value);
			XmlNode xmlNode8 = node.Attributes["prefab"];
			if (xmlNode8 != null && !string.IsNullOrEmpty(xmlNode8.InnerText))
			{
				PrefabName = xmlNode8.InnerText;
			}
			else
			{
				PrefabName = "";
			}
			Culture = (BasicCultureObject)objectManager.ReadObjectReferenceFromXml("culture", typeof(BasicCultureObject), node);
			string text2 = ((node.Attributes["item_category"] != null) ? node.Attributes["item_category"].Value : null);
			if (!string.IsNullOrEmpty(text2))
			{
				ItemCategory = Game.Current.ObjectManager.GetObject<ItemCategory>(text2);
			}
			Weight = ((node.Attributes["weight"] != null) ? float.Parse(node.Attributes["weight"].Value) : 1f);
			LodAtlasIndex = ((node.Attributes["lod_atlas_index"] != null) ? int.Parse(node.Attributes["lod_atlas_index"].Value) : (-1));
			XmlAttribute xmlAttribute4 = node.Attributes["difficulty"];
			if (xmlAttribute4 != null)
			{
				Difficulty = int.Parse(xmlAttribute4.Value);
			}
			XmlAttribute xmlAttribute5 = node.Attributes["appearance"];
			Appearance = ((xmlAttribute5 != null) ? float.Parse(xmlAttribute5.Value) : 0.5f);
			XmlAttribute xmlAttribute6 = node.Attributes["IsFood"];
			if (xmlAttribute6 != null)
			{
				IsFood = Convert.ToBoolean(xmlAttribute6.InnerText);
			}
			IsUsingTableau = node.Attributes["using_tableau"] != null && Convert.ToBoolean(node.Attributes["using_tableau"].InnerText);
			XmlNode xmlNode9 = node.Attributes["using_arm_band"];
			if (xmlNode9 != null)
			{
				ArmBandMeshName = Convert.ToString(xmlNode9.InnerText);
			}
			ScaleFactor = ((node.Attributes["scale_factor"] != null) ? float.Parse(node.Attributes["scale_factor"].Value) : 1f);
			ItemFlags = (ItemFlags)0u;
			foreach (XmlNode childNode2 in node.ChildNodes)
			{
				if (childNode2.Name == "ItemComponent")
				{
					foreach (XmlNode childNode3 in childNode2.ChildNodes)
					{
						if (childNode3.NodeType != XmlNodeType.Comment)
						{
							ItemComponent itemComponent;
							switch (childNode3.Name)
							{
							case "Armor":
								itemComponent = new ArmorComponent(this);
								break;
							case "Weapon":
								itemComponent = ItemComponent ?? new WeaponComponent(this);
								break;
							case "Horse":
								itemComponent = new HorseComponent();
								break;
							case "Trade":
								itemComponent = new TradeItemComponent();
								break;
							case "Food":
								TaleWorlds.Library.Debug.FailedAssert("FoodComponent tag has been converted to TradeComponent. Use Trade xml node type", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.Core\\ItemObject.cs", "Deserialize", 687);
								itemComponent = null;
								break;
							case "Banner":
								itemComponent = new BannerComponent(this);
								break;
							default:
								throw new Exception("Wrong ItemComponent type.");
							}
							if (itemComponent != null)
							{
								itemComponent.Deserialize(objectManager, childNode3);
								ItemComponent = itemComponent;
							}
						}
					}
				}
				else
				{
					if (!(childNode2.Name == "Flags"))
					{
						continue;
					}
					foreach (ItemFlags value in Enum.GetValues(typeof(ItemFlags)))
					{
						XmlAttribute xmlAttribute7 = childNode2.Attributes[value.ToString()];
						if (xmlAttribute7 != null && xmlAttribute7.Value.ToLowerInvariant() != "false")
						{
							ItemFlags |= value;
						}
					}
				}
			}
			XmlAttribute xmlAttribute8 = node.Attributes["Type"];
			if (xmlAttribute8 != null)
			{
				Type = (ItemTypeEnum)Enum.Parse(typeof(ItemTypeEnum), xmlAttribute8.Value, ignoreCase: true);
				if (WeaponComponent != null)
				{
					ItemTypeEnum itemType = WeaponComponent.GetItemType();
					if (Type != itemType)
					{
						TaleWorlds.Library.Debug.Print(string.Concat("ItemType for \"", base.StringId, "\" has been overridden by WeaponClass from \"", Type, "\" to \"", itemType, "\""), 0, TaleWorlds.Library.Debug.DebugColor.Red, 64uL);
					}
					Type = itemType;
				}
			}
			if (Type == ItemTypeEnum.Banner && !(ItemComponent is BannerComponent) && !(base.StringId == "campaign_banner_small"))
			{
				TaleWorlds.Library.Debug.FailedAssert(string.Concat("Banner item with name: ", Name, " is not properly set. It must either be a campaign banner or it must have a banner component."), "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.Core\\ItemObject.cs", "Deserialize", 749);
				TaleWorlds.Library.Debug.Print(string.Concat("Banner item with name: ", Name, " is not properly set. It must either be a campaign banner or it must have a banner component."), 0, TaleWorlds.Library.Debug.DebugColor.Yellow);
			}
			XmlAttribute xmlAttribute9 = node.Attributes["AmmoOffset"];
			if (xmlAttribute9 != null)
			{
				string[] array2 = xmlAttribute9.Value.Split(new char[1] { ',' });
				WeaponComponent.PrimaryWeapon.SetAmmoOffset(new Vec3(0f, 0f, 0f, -1f));
				if (array2.Length == 3)
				{
					try
					{
						Vec3 ammoOffset = new Vec3(float.Parse(array2[0], CultureInfo.InvariantCulture), float.Parse(array2[1], CultureInfo.InvariantCulture), float.Parse(array2[2], CultureInfo.InvariantCulture));
						WeaponComponent.PrimaryWeapon.SetAmmoOffset(ammoOffset);
					}
					catch (Exception)
					{
						TaleWorlds.Library.Debug.FailedAssert("[DEBUG]Throw Base Offset is not valid", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.Core\\ItemObject.cs", "Deserialize", 776);
					}
				}
				else
				{
					TaleWorlds.Library.Debug.FailedAssert("[DEBUG]Throw Base Offset is not valid", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.Core\\ItemObject.cs", "Deserialize", 781);
				}
			}
			if (node.Attributes["tier_override"] != null)
			{
				float num2 = float.Parse(node.Attributes["tier_override"].Value);
				TierfOverride = num2 + 1f;
			}
			Effectiveness = CalculateEffectiveness();
			if (node.Attributes["value"] != null)
			{
				Value = int.Parse(node.Attributes["value"].Value);
			}
			else
			{
				DetermineValue();
			}
			if (PrimaryWeapon != null)
			{
				if (PrimaryWeapon.IsMeleeWeapon || PrimaryWeapon.IsRangedWeapon)
				{
					if (!string.IsNullOrEmpty(BodyName))
					{
					}
				}
				else if (PrimaryWeapon.IsConsumable)
				{
					string.IsNullOrEmpty(HolsterBodyName);
					if (!string.IsNullOrEmpty(BodyName))
					{
					}
				}
				else if (PrimaryWeapon.IsShield)
				{
					if (!string.IsNullOrEmpty(BodyName))
					{
						_ = RecalculateBody;
					}
					string.IsNullOrEmpty(CollisionBodyName);
				}
			}
		}
		DetermineItemCategoryForItem();
		Game.Current.ItemObjectDeserialized(this);
	}

	public override string ToString()
	{
		return base.StringId;
	}

	public static ItemObject GetItemFromWeaponKind(int weaponKind)
	{
		if (weaponKind < 0)
		{
			return null;
		}
		return MBObjectManager.Instance.GetObject(new MBGUID((uint)weaponKind)) as ItemObject;
	}

	[Conditional("_RGL_KEEP_ASSERTS")]
	private void MakeSureProperFlagsSetForOneAndTwoHandedWeapons()
	{
		if (PrimaryWeapon != null)
		{
			if ((Type == ItemTypeEnum.Bow || Type == ItemTypeEnum.Crossbow || Type == ItemTypeEnum.TwoHandedWeapon) && !PrimaryWeapon.WeaponFlags.HasAnyFlag(WeaponFlags.NotUsableWithOneHand))
			{
				TaleWorlds.Library.Debug.FailedAssert(string.Concat(Name, ": Two Handed Item does not have NotUsableWithOneHand flag!"), "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.Core\\ItemObject.cs", "MakeSureProperFlagsSetForOneAndTwoHandedWeapons", 959);
				PrimaryWeapon.WeaponFlags |= WeaponFlags.NotUsableWithOneHand;
			}
			if ((Type == ItemTypeEnum.Bow || Type == ItemTypeEnum.Crossbow) && !PrimaryWeapon.WeaponFlags.HasAnyFlag(WeaponFlags.TwoHandIdleOnMount))
			{
				TaleWorlds.Library.Debug.FailedAssert(string.Concat(Name, ": Two Handed Item does not have TwoHandIdleOnMount flag!"), "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.Core\\ItemObject.cs", "MakeSureProperFlagsSetForOneAndTwoHandedWeapons", 968);
				PrimaryWeapon.WeaponFlags |= WeaponFlags.TwoHandIdleOnMount;
			}
			if ((Type == ItemTypeEnum.OneHandedWeapon || Type == ItemTypeEnum.Shield) && PrimaryWeapon.WeaponFlags.HasAnyFlag(WeaponFlags.NotUsableWithOneHand))
			{
				TaleWorlds.Library.Debug.FailedAssert(string.Concat(Name, ": One Handed Item has TwoHanded flag!"), "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.Core\\ItemObject.cs", "MakeSureProperFlagsSetForOneAndTwoHandedWeapons", 977);
				PrimaryWeapon.WeaponFlags &= ~WeaponFlags.NotUsableWithOneHand;
			}
		}
	}

	[Conditional("DEBUG")]
	private void DebugMakeSurePhysicsMaterialCorrectlySet()
	{
	}

	[Conditional("DEBUG")]
	private void MakeSureWeaponLengthAndMissileSpeedCorrect()
	{
		if (WeaponComponent == null)
		{
			return;
		}
		foreach (WeaponComponentData weapon in WeaponComponent.Weapons)
		{
			_ = weapon.WeaponLength;
			_ = 0;
			if (Type == ItemTypeEnum.Arrows || Type == ItemTypeEnum.Bolts || Type == ItemTypeEnum.SlingStones || Type == ItemTypeEnum.Bullets || Type == ItemTypeEnum.Thrown)
			{
				_ = weapon.MissileSpeed;
				_ = 0;
			}
		}
	}

	public static ItemTypeEnum GetAmmoTypeForItemType(ItemTypeEnum itemType)
	{
		return itemType switch
		{
			ItemTypeEnum.Bow => ItemTypeEnum.Arrows, 
			ItemTypeEnum.Crossbow => ItemTypeEnum.Bolts, 
			ItemTypeEnum.Sling => ItemTypeEnum.SlingStones, 
			ItemTypeEnum.Pistol => ItemTypeEnum.Bullets, 
			ItemTypeEnum.Thrown => ItemTypeEnum.Thrown, 
			_ => ItemTypeEnum.Invalid, 
		};
	}

	public static float GetAirFrictionConstant(WeaponClass weaponClass, WeaponFlags weaponFlags)
	{
		switch (weaponClass)
		{
		case WeaponClass.Arrow:
			if (weaponFlags.HasAnyFlag(WeaponFlags.MultiplePenetration))
			{
				return ManagedParameters.Instance.GetManagedParameter(ManagedParametersEnum.AirFrictionBallistaBolt);
			}
			return ManagedParameters.Instance.GetManagedParameter(ManagedParametersEnum.AirFrictionArrow);
		case WeaponClass.Bolt:
			return ManagedParameters.Instance.GetManagedParameter(ManagedParametersEnum.AirFrictionArrow);
		case WeaponClass.SlingStone:
		case WeaponClass.Cartridge:
			return ManagedParameters.Instance.GetManagedParameter(ManagedParametersEnum.AirFrictionBullet);
		case WeaponClass.Bow:
		case WeaponClass.Crossbow:
			return ManagedParameters.Instance.GetManagedParameter(ManagedParametersEnum.AirFrictionArrow);
		case WeaponClass.Sling:
		case WeaponClass.Stone:
			return ManagedParameters.Instance.GetManagedParameter(ManagedParametersEnum.AirFrictionStone);
		case WeaponClass.Boulder:
			return ManagedParameters.Instance.GetManagedParameter(ManagedParametersEnum.AirFrictionBoulder);
		case WeaponClass.ThrowingAxe:
			return ManagedParameters.Instance.GetManagedParameter(ManagedParametersEnum.AirFrictionAxe);
		case WeaponClass.ThrowingKnife:
			return ManagedParameters.Instance.GetManagedParameter(ManagedParametersEnum.AirFrictionKnife);
		case WeaponClass.Javelin:
			return ManagedParameters.Instance.GetManagedParameter(ManagedParametersEnum.AirFrictionJavelin);
		case WeaponClass.Pistol:
		case WeaponClass.Musket:
			return ManagedParameters.Instance.GetManagedParameter(ManagedParametersEnum.AirFrictionBullet);
		case WeaponClass.BallistaStone:
			return ManagedParameters.Instance.GetManagedParameter(ManagedParametersEnum.AirFrictionBallistaStone);
		case WeaponClass.BallistaBoulder:
			return ManagedParameters.Instance.GetManagedParameter(ManagedParametersEnum.AirFrictionBallistaBoulder);
		default:
			return ManagedParameters.Instance.GetManagedParameter(ManagedParametersEnum.AirFrictionArrow);
		}
	}

	private float CalculateEffectiveness()
	{
		float result = 1f;
		ArmorComponent armorComponent = ArmorComponent;
		if (armorComponent != null)
		{
			result = ((Type != ItemTypeEnum.HorseHarness) ? (((float)armorComponent.HeadArmor * 34f + (float)armorComponent.BodyArmor * 42f + (float)armorComponent.LegArmor * 12f + (float)armorComponent.ArmArmor * 12f) * 0.03f) : ((float)armorComponent.BodyArmor * 1.67f));
		}
		if (WeaponComponent != null)
		{
			WeaponComponentData primaryWeapon = WeaponComponent.PrimaryWeapon;
			float num = 1f;
			switch (primaryWeapon.WeaponClass)
			{
			case WeaponClass.Dagger:
				num = 0.4f;
				break;
			case WeaponClass.OneHandedSword:
				num = 0.55f;
				break;
			case WeaponClass.TwoHandedSword:
				num = 0.6f;
				break;
			case WeaponClass.OneHandedAxe:
				num = 0.5f;
				break;
			case WeaponClass.TwoHandedAxe:
				num = 0.55f;
				break;
			case WeaponClass.Mace:
				num = 0.5f;
				break;
			case WeaponClass.Pick:
				num = 0.4f;
				break;
			case WeaponClass.TwoHandedMace:
				num = 0.55f;
				break;
			case WeaponClass.OneHandedPolearm:
				num = 0.4f;
				break;
			case WeaponClass.TwoHandedPolearm:
				num = 0.4f;
				break;
			case WeaponClass.LowGripPolearm:
				num = 0.4f;
				break;
			case WeaponClass.Arrow:
				num = 3f;
				break;
			case WeaponClass.Bolt:
				num = 3f;
				break;
			case WeaponClass.Cartridge:
				num = 3f;
				break;
			case WeaponClass.Bow:
				num = 0.55f;
				break;
			case WeaponClass.Crossbow:
				num = 0.57f;
				break;
			case WeaponClass.Sling:
				num = 0.1f;
				break;
			case WeaponClass.Stone:
			case WeaponClass.BallistaStone:
				num = 0.1f;
				break;
			case WeaponClass.SlingStone:
				num = 0.1f;
				break;
			case WeaponClass.Boulder:
			case WeaponClass.BallistaBoulder:
				num = 0.1f;
				break;
			case WeaponClass.ThrowingAxe:
				num = 0.25f;
				break;
			case WeaponClass.ThrowingKnife:
				num = 0.2f;
				break;
			case WeaponClass.Javelin:
				num = 0.28f;
				break;
			case WeaponClass.Pistol:
				num = 1f;
				break;
			case WeaponClass.Musket:
				num = 1f;
				break;
			case WeaponClass.SmallShield:
				num = 0.4f;
				break;
			case WeaponClass.LargeShield:
				num = 0.5f;
				break;
			}
			if (primaryWeapon.IsRangedWeapon)
			{
				result = ((!primaryWeapon.IsConsumable) ? (((float)(primaryWeapon.MissileSpeed * primaryWeapon.MissileDamage) * 1.75f + (float)(primaryWeapon.ThrustSpeed * primaryWeapon.Accuracy) * 0.3f) * 0.01f * (float)primaryWeapon.MaxDataValue * num) : (((float)(primaryWeapon.MissileDamage * primaryWeapon.MissileSpeed) * 1.775f + (float)(primaryWeapon.Accuracy * primaryWeapon.MaxDataValue) * 25f + (float)primaryWeapon.WeaponLength * 4f) * 0.006944f * (float)primaryWeapon.MaxDataValue * num));
			}
			else if (primaryWeapon.IsMeleeWeapon)
			{
				float b = (float)(primaryWeapon.ThrustSpeed * primaryWeapon.ThrustDamage) * 0.01f;
				float a = (float)(primaryWeapon.SwingSpeed * primaryWeapon.SwingDamage) * 0.01f;
				float num2 = TaleWorlds.Library.MathF.Max(a, b);
				float num3 = TaleWorlds.Library.MathF.Min(a, b);
				result = ((num2 + num3 * num3 / num2) * 120f + (float)primaryWeapon.Handling * 15f + (float)primaryWeapon.WeaponLength * 20f + Weight * 5f) * 0.01f * num;
			}
			else if (primaryWeapon.IsConsumable)
			{
				result = ((float)primaryWeapon.MissileDamage * 550f + (float)primaryWeapon.MissileSpeed * 15f + (float)primaryWeapon.MaxDataValue * 60f) * 0.01f * num;
			}
			else if (primaryWeapon.IsShield)
			{
				result = ((float)primaryWeapon.BodyArmor * 60f + (float)primaryWeapon.ThrustSpeed * 10f + (float)primaryWeapon.MaxDataValue * 40f + (float)primaryWeapon.WeaponLength * 20f) * 0.01f * num;
			}
		}
		if (HorseComponent != null)
		{
			result = ((float)(HorseComponent.ChargeDamage * HorseComponent.Speed + HorseComponent.Maneuver * HorseComponent.Speed) + (float)HorseComponent.BodyLength * Weight * 0.025f) * (float)(HorseComponent.HitPoints + HorseComponent.HitPointBonus) * 0.0001f;
		}
		return result;
	}

	internal void DetermineValue()
	{
		Value = Game.Current.BasicModels.ItemValueModel?.CalculateValue(this) ?? 1;
	}

	public WeaponComponentData GetWeaponWithUsageIndex(int usageIndex)
	{
		return Weapons.ElementAt(usageIndex);
	}
}
