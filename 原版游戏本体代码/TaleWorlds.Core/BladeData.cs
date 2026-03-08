using System;
using System.Xml;
using TaleWorlds.ObjectSystem;

namespace TaleWorlds.Core
{
	// Token: 0x02000048 RID: 72
	public sealed class BladeData : MBObjectBase
	{
		// Token: 0x17000204 RID: 516
		// (get) Token: 0x06000617 RID: 1559 RVA: 0x000154EB File Offset: 0x000136EB
		// (set) Token: 0x06000618 RID: 1560 RVA: 0x000154F3 File Offset: 0x000136F3
		public DamageTypes ThrustDamageType { get; private set; }

		// Token: 0x17000205 RID: 517
		// (get) Token: 0x06000619 RID: 1561 RVA: 0x000154FC File Offset: 0x000136FC
		// (set) Token: 0x0600061A RID: 1562 RVA: 0x00015504 File Offset: 0x00013704
		public float ThrustDamageFactor { get; private set; }

		// Token: 0x17000206 RID: 518
		// (get) Token: 0x0600061B RID: 1563 RVA: 0x0001550D File Offset: 0x0001370D
		// (set) Token: 0x0600061C RID: 1564 RVA: 0x00015515 File Offset: 0x00013715
		public DamageTypes SwingDamageType { get; private set; }

		// Token: 0x17000207 RID: 519
		// (get) Token: 0x0600061D RID: 1565 RVA: 0x0001551E File Offset: 0x0001371E
		// (set) Token: 0x0600061E RID: 1566 RVA: 0x00015526 File Offset: 0x00013726
		public float SwingDamageFactor { get; private set; }

		// Token: 0x17000208 RID: 520
		// (get) Token: 0x0600061F RID: 1567 RVA: 0x0001552F File Offset: 0x0001372F
		// (set) Token: 0x06000620 RID: 1568 RVA: 0x00015537 File Offset: 0x00013737
		public float BladeLength { get; private set; }

		// Token: 0x17000209 RID: 521
		// (get) Token: 0x06000621 RID: 1569 RVA: 0x00015540 File Offset: 0x00013740
		// (set) Token: 0x06000622 RID: 1570 RVA: 0x00015548 File Offset: 0x00013748
		public float BladeWidth { get; private set; }

		// Token: 0x1700020A RID: 522
		// (get) Token: 0x06000623 RID: 1571 RVA: 0x00015551 File Offset: 0x00013751
		// (set) Token: 0x06000624 RID: 1572 RVA: 0x00015559 File Offset: 0x00013759
		public short StackAmount { get; private set; }

		// Token: 0x1700020B RID: 523
		// (get) Token: 0x06000625 RID: 1573 RVA: 0x00015562 File Offset: 0x00013762
		// (set) Token: 0x06000626 RID: 1574 RVA: 0x0001556A File Offset: 0x0001376A
		public string PhysicsMaterial { get; private set; }

		// Token: 0x1700020C RID: 524
		// (get) Token: 0x06000627 RID: 1575 RVA: 0x00015573 File Offset: 0x00013773
		// (set) Token: 0x06000628 RID: 1576 RVA: 0x0001557B File Offset: 0x0001377B
		public string BodyName { get; private set; }

		// Token: 0x1700020D RID: 525
		// (get) Token: 0x06000629 RID: 1577 RVA: 0x00015584 File Offset: 0x00013784
		// (set) Token: 0x0600062A RID: 1578 RVA: 0x0001558C File Offset: 0x0001378C
		public string HolsterMeshName { get; private set; }

		// Token: 0x1700020E RID: 526
		// (get) Token: 0x0600062B RID: 1579 RVA: 0x00015595 File Offset: 0x00013795
		// (set) Token: 0x0600062C RID: 1580 RVA: 0x0001559D File Offset: 0x0001379D
		public string HolsterBodyName { get; private set; }

		// Token: 0x1700020F RID: 527
		// (get) Token: 0x0600062D RID: 1581 RVA: 0x000155A6 File Offset: 0x000137A6
		// (set) Token: 0x0600062E RID: 1582 RVA: 0x000155AE File Offset: 0x000137AE
		public float HolsterMeshLength { get; private set; }

		// Token: 0x0600062F RID: 1583 RVA: 0x000155B7 File Offset: 0x000137B7
		public BladeData(CraftingPiece.PieceTypes pieceType, float bladeLength)
		{
			this.PieceType = pieceType;
			this.BladeLength = bladeLength;
			this.ThrustDamageType = DamageTypes.Invalid;
			this.SwingDamageType = DamageTypes.Invalid;
		}

		// Token: 0x06000630 RID: 1584 RVA: 0x000155DC File Offset: 0x000137DC
		public override void Deserialize(MBObjectManager objectManager, XmlNode childNode)
		{
			this.Initialize();
			XmlAttribute xmlAttribute = childNode.Attributes["stack_amount"];
			XmlAttribute xmlAttribute2 = childNode.Attributes["blade_length"];
			XmlAttribute xmlAttribute3 = childNode.Attributes["blade_width"];
			XmlAttribute xmlAttribute4 = childNode.Attributes["physics_material"];
			XmlAttribute xmlAttribute5 = childNode.Attributes["body_name"];
			XmlAttribute xmlAttribute6 = childNode.Attributes["holster_mesh"];
			XmlAttribute xmlAttribute7 = childNode.Attributes["holster_body_name"];
			XmlAttribute xmlAttribute8 = childNode.Attributes["holster_mesh_length"];
			this.StackAmount = ((xmlAttribute != null) ? short.Parse(xmlAttribute.Value) : 1);
			this.BladeLength = ((xmlAttribute2 != null) ? (0.01f * float.Parse(xmlAttribute2.Value)) : this.BladeLength);
			this.BladeWidth = ((xmlAttribute3 != null) ? (0.01f * float.Parse(xmlAttribute3.Value)) : (0.15f + this.BladeLength * 0.3f));
			this.PhysicsMaterial = ((xmlAttribute4 != null) ? xmlAttribute4.InnerText : null);
			this.BodyName = ((xmlAttribute5 != null) ? xmlAttribute5.InnerText : null);
			this.HolsterMeshName = ((xmlAttribute6 != null) ? xmlAttribute6.InnerText : null);
			this.HolsterBodyName = ((xmlAttribute7 != null) ? xmlAttribute7.InnerText : null);
			this.HolsterMeshLength = 0.01f * ((xmlAttribute8 != null) ? float.Parse(xmlAttribute8.Value) : 0f);
			foreach (object obj in childNode.ChildNodes)
			{
				XmlNode xmlNode = (XmlNode)obj;
				string name = xmlNode.Name;
				if (!(name == "Thrust"))
				{
					if (name == "Swing")
					{
						XmlAttribute xmlAttribute9 = xmlNode.Attributes["damage_type"];
						XmlAttribute xmlAttribute10 = xmlNode.Attributes["damage_factor"];
						this.SwingDamageType = (DamageTypes)Enum.Parse(typeof(DamageTypes), xmlAttribute9.Value, true);
						this.SwingDamageFactor = float.Parse(xmlAttribute10.Value);
					}
				}
				else
				{
					XmlAttribute xmlAttribute11 = xmlNode.Attributes["damage_type"];
					XmlAttribute xmlAttribute12 = xmlNode.Attributes["damage_factor"];
					this.ThrustDamageType = (DamageTypes)Enum.Parse(typeof(DamageTypes), xmlAttribute11.Value, true);
					this.ThrustDamageFactor = float.Parse(xmlAttribute12.Value);
				}
			}
		}

		// Token: 0x040002D5 RID: 725
		public readonly CraftingPiece.PieceTypes PieceType;
	}
}
