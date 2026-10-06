using System;
using System.Collections.Generic;
using System.Globalization;
using System.Xml;
using TaleWorlds.ObjectSystem;

namespace TaleWorlds.Core;

public class ArmorComponent : ItemComponent
{
	public enum ArmorMaterialTypes : sbyte
	{
		None,
		Cloth,
		Leather,
		Chainmail,
		Plate
	}

	public enum HairCoverTypes
	{
		None,
		Type1,
		Type2,
		Type3,
		Type4,
		All,
		NumHairCoverTypes
	}

	public enum BeardCoverTypes
	{
		None,
		Type1,
		Type2,
		Type3,
		Type4,
		All,
		NumBeardBoverTypes
	}

	public enum HorseHarnessCoverTypes
	{
		None,
		Type1,
		Type2,
		All,
		HorseHarnessCoverTypes
	}

	public enum HorseTailCoverTypes
	{
		None,
		All
	}

	public enum BodyMeshTypes
	{
		Normal,
		Upperbody,
		Shoulders,
		BodyMeshTypesNum
	}

	public enum BodyDeformTypes
	{
		Medium,
		Large,
		Skinny,
		BodyMeshTypesNum
	}

	public int HeadArmor { get; private set; }

	public int BodyArmor { get; private set; }

	public int LegArmor { get; private set; }

	public int ArmArmor { get; private set; }

	public int ManeuverBonus { get; private set; }

	public int SpeedBonus { get; private set; }

	public int ChargeBonus { get; private set; }

	public int FamilyType { get; private set; }

	public bool MultiMeshHasGenderVariations { get; private set; }

	public ArmorMaterialTypes MaterialType { get; private set; }

	public SkinMask MeshesMask { get; private set; }

	public BodyMeshTypes BodyMeshType { get; private set; }

	public BodyDeformTypes BodyDeformType { get; private set; }

	public HairCoverTypes HairCoverType { get; private set; }

	public BeardCoverTypes BeardCoverType { get; private set; }

	public HorseHarnessCoverTypes ManeCoverType { get; private set; }

	public HorseTailCoverTypes TailCoverType { get; private set; }

	public int StealthFactor { get; private set; }

	public string ReinsMesh { get; private set; }

	public bool IsNoSlim { get; private set; }

	public string ReinsRopeMesh => ReinsMesh + "_rope";

	public ArmorComponent(ItemObject item)
	{
		base.Item = item;
	}

	public override ItemComponent GetCopy()
	{
		return new ArmorComponent(base.Item)
		{
			HeadArmor = HeadArmor,
			BodyArmor = BodyArmor,
			LegArmor = LegArmor,
			ArmArmor = ArmArmor,
			MultiMeshHasGenderVariations = MultiMeshHasGenderVariations,
			MaterialType = MaterialType,
			MeshesMask = MeshesMask,
			BodyMeshType = BodyMeshType,
			HairCoverType = HairCoverType,
			BeardCoverType = BeardCoverType,
			ManeCoverType = ManeCoverType,
			TailCoverType = TailCoverType,
			BodyDeformType = BodyDeformType,
			ManeuverBonus = ManeuverBonus,
			SpeedBonus = SpeedBonus,
			ChargeBonus = ChargeBonus,
			FamilyType = FamilyType,
			ReinsMesh = ReinsMesh,
			StealthFactor = StealthFactor
		};
	}

	public override void Deserialize(MBObjectManager objectManager, XmlNode node)
	{
		base.Deserialize(objectManager, node);
		HeadArmor = ((node.Attributes["head_armor"] != null) ? int.Parse(node.Attributes["head_armor"].Value) : 0);
		BodyArmor = ((node.Attributes["body_armor"] != null) ? int.Parse(node.Attributes["body_armor"].Value) : 0);
		LegArmor = ((node.Attributes["leg_armor"] != null) ? int.Parse(node.Attributes["leg_armor"].Value) : 0);
		ArmArmor = ((node.Attributes["arm_armor"] != null) ? int.Parse(node.Attributes["arm_armor"].Value) : 0);
		FamilyType = ((node.Attributes["family_type"] != null) ? int.Parse(node.Attributes["family_type"].Value) : 0);
		ManeuverBonus = ((node.Attributes["maneuver_bonus"] != null) ? int.Parse(node.Attributes["maneuver_bonus"].Value) : 0);
		SpeedBonus = ((node.Attributes["speed_bonus"] != null) ? int.Parse(node.Attributes["speed_bonus"].Value) : 0);
		ChargeBonus = ((node.Attributes["charge_bonus"] != null) ? int.Parse(node.Attributes["charge_bonus"].Value) : 0);
		MaterialType = ((node.Attributes["material_type"] != null) ? ((ArmorMaterialTypes)Enum.Parse(typeof(ArmorMaterialTypes), node.Attributes["material_type"].Value)) : ArmorMaterialTypes.None);
		_ = MaterialType;
		MultiMeshHasGenderVariations = true;
		if (node.Attributes["has_gender_variations"] != null)
		{
			MultiMeshHasGenderVariations = Convert.ToBoolean(node.Attributes["has_gender_variations"].Value);
		}
		BodyMeshType = BodyMeshTypes.Normal;
		if (node.Attributes["body_mesh_type"] != null)
		{
			string value = node.Attributes["body_mesh_type"].Value;
			if (value == "upperbody")
			{
				BodyMeshType = BodyMeshTypes.Upperbody;
			}
			else if (value == "shoulders")
			{
				BodyMeshType = BodyMeshTypes.Shoulders;
			}
		}
		BodyDeformType = BodyDeformTypes.Medium;
		if (node.Attributes["body_deform_type"] != null)
		{
			string value2 = node.Attributes["body_deform_type"].Value;
			if (value2 == "large")
			{
				BodyDeformType = BodyDeformTypes.Large;
			}
			else if (value2 == "skinny")
			{
				BodyDeformType = BodyDeformTypes.Skinny;
			}
		}
		HairCoverType = ((node.Attributes["hair_cover_type"] != null) ? ((HairCoverTypes)Enum.Parse(typeof(HairCoverTypes), node.Attributes["hair_cover_type"].Value, ignoreCase: true)) : HairCoverTypes.None);
		BeardCoverType = ((node.Attributes["beard_cover_type"] != null) ? ((BeardCoverTypes)Enum.Parse(typeof(BeardCoverTypes), node.Attributes["beard_cover_type"].Value, ignoreCase: true)) : BeardCoverTypes.None);
		ManeCoverType = ((node.Attributes["mane_cover_type"] != null) ? ((HorseHarnessCoverTypes)Enum.Parse(typeof(HorseHarnessCoverTypes), node.Attributes["mane_cover_type"].Value, ignoreCase: true)) : HorseHarnessCoverTypes.None);
		TailCoverType = ((node.Attributes["tail_cover_type"] != null) ? ((HorseTailCoverTypes)Enum.Parse(typeof(HorseTailCoverTypes), node.Attributes["tail_cover_type"].Value, ignoreCase: true)) : HorseTailCoverTypes.None);
		StealthFactor = ((node.Attributes["stealth_factor"] != null) ? int.Parse(node.Attributes["stealth_factor"].InnerText, CultureInfo.InvariantCulture.NumberFormat) : 0);
		ReinsMesh = ((node.Attributes["reins_mesh"] != null) ? node.Attributes["reins_mesh"].Value : "");
		bool num = node.Attributes["covers_head"] != null && Convert.ToBoolean(node.Attributes["covers_head"].Value);
		bool flag = node.Attributes["covers_body"] != null && Convert.ToBoolean(node.Attributes["covers_body"].Value);
		bool flag2 = node.Attributes["covers_hands"] != null && Convert.ToBoolean(node.Attributes["covers_hands"].Value);
		bool flag3 = node.Attributes["covers_legs"] != null && Convert.ToBoolean(node.Attributes["covers_legs"].Value);
		if (!num)
		{
			MeshesMask |= SkinMask.HeadVisible;
		}
		if (!flag)
		{
			MeshesMask |= SkinMask.BodyVisible;
		}
		if (!flag2)
		{
			MeshesMask |= SkinMask.HandsVisible;
		}
		if (!flag3)
		{
			MeshesMask |= SkinMask.LegsVisible;
		}
		IsNoSlim = node.Attributes["no_slim"] != null && Convert.ToBoolean(node.Attributes["no_slim"].Value);
	}

	internal static void AutoGeneratedStaticCollectObjectsArmorComponent(object o, List<object> collectedObjects)
	{
		((ArmorComponent)o).AutoGeneratedInstanceCollectObjects(collectedObjects);
	}

	protected override void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects)
	{
		base.AutoGeneratedInstanceCollectObjects(collectedObjects);
	}
}
