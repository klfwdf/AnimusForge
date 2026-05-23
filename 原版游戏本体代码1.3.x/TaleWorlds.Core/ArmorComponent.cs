using System;
using System.Collections.Generic;
using System.Globalization;
using System.Xml;
using TaleWorlds.ObjectSystem;

namespace TaleWorlds.Core
{
	// Token: 0x0200000D RID: 13
	public class ArmorComponent : ItemComponent
	{
		// Token: 0x17000017 RID: 23
		// (get) Token: 0x06000050 RID: 80 RVA: 0x00002578 File Offset: 0x00000778
		// (set) Token: 0x06000051 RID: 81 RVA: 0x00002580 File Offset: 0x00000780
		public int HeadArmor { get; private set; }

		// Token: 0x17000018 RID: 24
		// (get) Token: 0x06000052 RID: 82 RVA: 0x00002589 File Offset: 0x00000789
		// (set) Token: 0x06000053 RID: 83 RVA: 0x00002591 File Offset: 0x00000791
		public int BodyArmor { get; private set; }

		// Token: 0x17000019 RID: 25
		// (get) Token: 0x06000054 RID: 84 RVA: 0x0000259A File Offset: 0x0000079A
		// (set) Token: 0x06000055 RID: 85 RVA: 0x000025A2 File Offset: 0x000007A2
		public int LegArmor { get; private set; }

		// Token: 0x1700001A RID: 26
		// (get) Token: 0x06000056 RID: 86 RVA: 0x000025AB File Offset: 0x000007AB
		// (set) Token: 0x06000057 RID: 87 RVA: 0x000025B3 File Offset: 0x000007B3
		public int ArmArmor { get; private set; }

		// Token: 0x1700001B RID: 27
		// (get) Token: 0x06000058 RID: 88 RVA: 0x000025BC File Offset: 0x000007BC
		// (set) Token: 0x06000059 RID: 89 RVA: 0x000025C4 File Offset: 0x000007C4
		public int ManeuverBonus { get; private set; }

		// Token: 0x1700001C RID: 28
		// (get) Token: 0x0600005A RID: 90 RVA: 0x000025CD File Offset: 0x000007CD
		// (set) Token: 0x0600005B RID: 91 RVA: 0x000025D5 File Offset: 0x000007D5
		public int SpeedBonus { get; private set; }

		// Token: 0x1700001D RID: 29
		// (get) Token: 0x0600005C RID: 92 RVA: 0x000025DE File Offset: 0x000007DE
		// (set) Token: 0x0600005D RID: 93 RVA: 0x000025E6 File Offset: 0x000007E6
		public int ChargeBonus { get; private set; }

		// Token: 0x1700001E RID: 30
		// (get) Token: 0x0600005E RID: 94 RVA: 0x000025EF File Offset: 0x000007EF
		// (set) Token: 0x0600005F RID: 95 RVA: 0x000025F7 File Offset: 0x000007F7
		public int FamilyType { get; private set; }

		// Token: 0x1700001F RID: 31
		// (get) Token: 0x06000060 RID: 96 RVA: 0x00002600 File Offset: 0x00000800
		// (set) Token: 0x06000061 RID: 97 RVA: 0x00002608 File Offset: 0x00000808
		public bool MultiMeshHasGenderVariations { get; private set; }

		// Token: 0x17000020 RID: 32
		// (get) Token: 0x06000062 RID: 98 RVA: 0x00002611 File Offset: 0x00000811
		// (set) Token: 0x06000063 RID: 99 RVA: 0x00002619 File Offset: 0x00000819
		public ArmorComponent.ArmorMaterialTypes MaterialType { get; private set; }

		// Token: 0x17000021 RID: 33
		// (get) Token: 0x06000064 RID: 100 RVA: 0x00002622 File Offset: 0x00000822
		// (set) Token: 0x06000065 RID: 101 RVA: 0x0000262A File Offset: 0x0000082A
		public SkinMask MeshesMask { get; private set; }

		// Token: 0x17000022 RID: 34
		// (get) Token: 0x06000066 RID: 102 RVA: 0x00002633 File Offset: 0x00000833
		// (set) Token: 0x06000067 RID: 103 RVA: 0x0000263B File Offset: 0x0000083B
		public ArmorComponent.BodyMeshTypes BodyMeshType { get; private set; }

		// Token: 0x17000023 RID: 35
		// (get) Token: 0x06000068 RID: 104 RVA: 0x00002644 File Offset: 0x00000844
		// (set) Token: 0x06000069 RID: 105 RVA: 0x0000264C File Offset: 0x0000084C
		public ArmorComponent.BodyDeformTypes BodyDeformType { get; private set; }

		// Token: 0x17000024 RID: 36
		// (get) Token: 0x0600006A RID: 106 RVA: 0x00002655 File Offset: 0x00000855
		// (set) Token: 0x0600006B RID: 107 RVA: 0x0000265D File Offset: 0x0000085D
		public ArmorComponent.HairCoverTypes HairCoverType { get; private set; }

		// Token: 0x17000025 RID: 37
		// (get) Token: 0x0600006C RID: 108 RVA: 0x00002666 File Offset: 0x00000866
		// (set) Token: 0x0600006D RID: 109 RVA: 0x0000266E File Offset: 0x0000086E
		public ArmorComponent.BeardCoverTypes BeardCoverType { get; private set; }

		// Token: 0x17000026 RID: 38
		// (get) Token: 0x0600006E RID: 110 RVA: 0x00002677 File Offset: 0x00000877
		// (set) Token: 0x0600006F RID: 111 RVA: 0x0000267F File Offset: 0x0000087F
		public ArmorComponent.HorseHarnessCoverTypes ManeCoverType { get; private set; }

		// Token: 0x17000027 RID: 39
		// (get) Token: 0x06000070 RID: 112 RVA: 0x00002688 File Offset: 0x00000888
		// (set) Token: 0x06000071 RID: 113 RVA: 0x00002690 File Offset: 0x00000890
		public ArmorComponent.HorseTailCoverTypes TailCoverType { get; private set; }

		// Token: 0x17000028 RID: 40
		// (get) Token: 0x06000072 RID: 114 RVA: 0x00002699 File Offset: 0x00000899
		// (set) Token: 0x06000073 RID: 115 RVA: 0x000026A1 File Offset: 0x000008A1
		public int StealthFactor { get; private set; }

		// Token: 0x17000029 RID: 41
		// (get) Token: 0x06000074 RID: 116 RVA: 0x000026AA File Offset: 0x000008AA
		// (set) Token: 0x06000075 RID: 117 RVA: 0x000026B2 File Offset: 0x000008B2
		public string ReinsMesh { get; private set; }

		// Token: 0x1700002A RID: 42
		// (get) Token: 0x06000076 RID: 118 RVA: 0x000026BB File Offset: 0x000008BB
		public string ReinsRopeMesh
		{
			get
			{
				return this.ReinsMesh + "_rope";
			}
		}

		// Token: 0x06000077 RID: 119 RVA: 0x000026CD File Offset: 0x000008CD
		public ArmorComponent(ItemObject item)
		{
			base.Item = item;
		}

		// Token: 0x06000078 RID: 120 RVA: 0x000026DC File Offset: 0x000008DC
		public override ItemComponent GetCopy()
		{
			return new ArmorComponent(base.Item)
			{
				HeadArmor = this.HeadArmor,
				BodyArmor = this.BodyArmor,
				LegArmor = this.LegArmor,
				ArmArmor = this.ArmArmor,
				MultiMeshHasGenderVariations = this.MultiMeshHasGenderVariations,
				MaterialType = this.MaterialType,
				MeshesMask = this.MeshesMask,
				BodyMeshType = this.BodyMeshType,
				HairCoverType = this.HairCoverType,
				BeardCoverType = this.BeardCoverType,
				ManeCoverType = this.ManeCoverType,
				TailCoverType = this.TailCoverType,
				BodyDeformType = this.BodyDeformType,
				ManeuverBonus = this.ManeuverBonus,
				SpeedBonus = this.SpeedBonus,
				ChargeBonus = this.ChargeBonus,
				FamilyType = this.FamilyType,
				ReinsMesh = this.ReinsMesh,
				StealthFactor = this.StealthFactor
			};
		}

		// Token: 0x06000079 RID: 121 RVA: 0x000027DC File Offset: 0x000009DC
		public override void Deserialize(MBObjectManager objectManager, XmlNode node)
		{
			base.Deserialize(objectManager, node);
			this.HeadArmor = ((node.Attributes["head_armor"] != null) ? int.Parse(node.Attributes["head_armor"].Value) : 0);
			this.BodyArmor = ((node.Attributes["body_armor"] != null) ? int.Parse(node.Attributes["body_armor"].Value) : 0);
			this.LegArmor = ((node.Attributes["leg_armor"] != null) ? int.Parse(node.Attributes["leg_armor"].Value) : 0);
			this.ArmArmor = ((node.Attributes["arm_armor"] != null) ? int.Parse(node.Attributes["arm_armor"].Value) : 0);
			this.FamilyType = ((node.Attributes["family_type"] != null) ? int.Parse(node.Attributes["family_type"].Value) : 0);
			this.ManeuverBonus = ((node.Attributes["maneuver_bonus"] != null) ? int.Parse(node.Attributes["maneuver_bonus"].Value) : 0);
			this.SpeedBonus = ((node.Attributes["speed_bonus"] != null) ? int.Parse(node.Attributes["speed_bonus"].Value) : 0);
			this.ChargeBonus = ((node.Attributes["charge_bonus"] != null) ? int.Parse(node.Attributes["charge_bonus"].Value) : 0);
			this.MaterialType = ((node.Attributes["material_type"] != null) ? ((ArmorComponent.ArmorMaterialTypes)Enum.Parse(typeof(ArmorComponent.ArmorMaterialTypes), node.Attributes["material_type"].Value)) : ArmorComponent.ArmorMaterialTypes.None);
			ArmorComponent.ArmorMaterialTypes materialType = this.MaterialType;
			this.MultiMeshHasGenderVariations = true;
			if (node.Attributes["has_gender_variations"] != null)
			{
				this.MultiMeshHasGenderVariations = Convert.ToBoolean(node.Attributes["has_gender_variations"].Value);
			}
			this.BodyMeshType = ArmorComponent.BodyMeshTypes.Normal;
			if (node.Attributes["body_mesh_type"] != null)
			{
				string value = node.Attributes["body_mesh_type"].Value;
				if (value == "upperbody")
				{
					this.BodyMeshType = ArmorComponent.BodyMeshTypes.Upperbody;
				}
				else if (value == "shoulders")
				{
					this.BodyMeshType = ArmorComponent.BodyMeshTypes.Shoulders;
				}
			}
			this.BodyDeformType = ArmorComponent.BodyDeformTypes.Medium;
			if (node.Attributes["body_deform_type"] != null)
			{
				string value2 = node.Attributes["body_deform_type"].Value;
				if (value2 == "large")
				{
					this.BodyDeformType = ArmorComponent.BodyDeformTypes.Large;
				}
				else if (value2 == "skinny")
				{
					this.BodyDeformType = ArmorComponent.BodyDeformTypes.Skinny;
				}
			}
			this.HairCoverType = ((node.Attributes["hair_cover_type"] != null) ? ((ArmorComponent.HairCoverTypes)Enum.Parse(typeof(ArmorComponent.HairCoverTypes), node.Attributes["hair_cover_type"].Value, true)) : ArmorComponent.HairCoverTypes.None);
			this.BeardCoverType = ((node.Attributes["beard_cover_type"] != null) ? ((ArmorComponent.BeardCoverTypes)Enum.Parse(typeof(ArmorComponent.BeardCoverTypes), node.Attributes["beard_cover_type"].Value, true)) : ArmorComponent.BeardCoverTypes.None);
			this.ManeCoverType = ((node.Attributes["mane_cover_type"] != null) ? ((ArmorComponent.HorseHarnessCoverTypes)Enum.Parse(typeof(ArmorComponent.HorseHarnessCoverTypes), node.Attributes["mane_cover_type"].Value, true)) : ArmorComponent.HorseHarnessCoverTypes.None);
			this.TailCoverType = ((node.Attributes["tail_cover_type"] != null) ? ((ArmorComponent.HorseTailCoverTypes)Enum.Parse(typeof(ArmorComponent.HorseTailCoverTypes), node.Attributes["tail_cover_type"].Value, true)) : ArmorComponent.HorseTailCoverTypes.None);
			this.StealthFactor = ((node.Attributes["stealth_factor"] != null) ? int.Parse(node.Attributes["stealth_factor"].InnerText, CultureInfo.InvariantCulture.NumberFormat) : 0);
			this.ReinsMesh = ((node.Attributes["reins_mesh"] != null) ? node.Attributes["reins_mesh"].Value : "");
			bool flag = node.Attributes["covers_head"] != null && Convert.ToBoolean(node.Attributes["covers_head"].Value);
			bool flag2 = node.Attributes["covers_body"] != null && Convert.ToBoolean(node.Attributes["covers_body"].Value);
			bool flag3 = node.Attributes["covers_hands"] != null && Convert.ToBoolean(node.Attributes["covers_hands"].Value);
			bool flag4 = node.Attributes["covers_legs"] != null && Convert.ToBoolean(node.Attributes["covers_legs"].Value);
			if (!flag)
			{
				this.MeshesMask |= SkinMask.HeadVisible;
			}
			if (!flag2)
			{
				this.MeshesMask |= SkinMask.BodyVisible;
			}
			if (!flag3)
			{
				this.MeshesMask |= SkinMask.HandsVisible;
			}
			if (!flag4)
			{
				this.MeshesMask |= SkinMask.LegsVisible;
			}
		}

		// Token: 0x0600007A RID: 122 RVA: 0x00002D64 File Offset: 0x00000F64
		internal static void AutoGeneratedStaticCollectObjectsArmorComponent(object o, List<object> collectedObjects)
		{
			((ArmorComponent)o).AutoGeneratedInstanceCollectObjects(collectedObjects);
		}

		// Token: 0x0600007B RID: 123 RVA: 0x00002D72 File Offset: 0x00000F72
		protected override void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects)
		{
			base.AutoGeneratedInstanceCollectObjects(collectedObjects);
		}

		// Token: 0x020000EB RID: 235
		public enum ArmorMaterialTypes : sbyte
		{
			// Token: 0x040006BA RID: 1722
			None,
			// Token: 0x040006BB RID: 1723
			Cloth,
			// Token: 0x040006BC RID: 1724
			Leather,
			// Token: 0x040006BD RID: 1725
			Chainmail,
			// Token: 0x040006BE RID: 1726
			Plate
		}

		// Token: 0x020000EC RID: 236
		public enum HairCoverTypes
		{
			// Token: 0x040006C0 RID: 1728
			None,
			// Token: 0x040006C1 RID: 1729
			Type1,
			// Token: 0x040006C2 RID: 1730
			Type2,
			// Token: 0x040006C3 RID: 1731
			Type3,
			// Token: 0x040006C4 RID: 1732
			Type4,
			// Token: 0x040006C5 RID: 1733
			All,
			// Token: 0x040006C6 RID: 1734
			NumHairCoverTypes
		}

		// Token: 0x020000ED RID: 237
		public enum BeardCoverTypes
		{
			// Token: 0x040006C8 RID: 1736
			None,
			// Token: 0x040006C9 RID: 1737
			Type1,
			// Token: 0x040006CA RID: 1738
			Type2,
			// Token: 0x040006CB RID: 1739
			Type3,
			// Token: 0x040006CC RID: 1740
			Type4,
			// Token: 0x040006CD RID: 1741
			All,
			// Token: 0x040006CE RID: 1742
			NumBeardBoverTypes
		}

		// Token: 0x020000EE RID: 238
		public enum HorseHarnessCoverTypes
		{
			// Token: 0x040006D0 RID: 1744
			None,
			// Token: 0x040006D1 RID: 1745
			Type1,
			// Token: 0x040006D2 RID: 1746
			Type2,
			// Token: 0x040006D3 RID: 1747
			All,
			// Token: 0x040006D4 RID: 1748
			HorseHarnessCoverTypes
		}

		// Token: 0x020000EF RID: 239
		public enum HorseTailCoverTypes
		{
			// Token: 0x040006D6 RID: 1750
			None,
			// Token: 0x040006D7 RID: 1751
			All
		}

		// Token: 0x020000F0 RID: 240
		public enum BodyMeshTypes
		{
			// Token: 0x040006D9 RID: 1753
			Normal,
			// Token: 0x040006DA RID: 1754
			Upperbody,
			// Token: 0x040006DB RID: 1755
			Shoulders,
			// Token: 0x040006DC RID: 1756
			BodyMeshTypesNum
		}

		// Token: 0x020000F1 RID: 241
		public enum BodyDeformTypes
		{
			// Token: 0x040006DE RID: 1758
			Medium,
			// Token: 0x040006DF RID: 1759
			Large,
			// Token: 0x040006E0 RID: 1760
			Skinny,
			// Token: 0x040006E1 RID: 1761
			BodyMeshTypesNum
		}
	}
}
