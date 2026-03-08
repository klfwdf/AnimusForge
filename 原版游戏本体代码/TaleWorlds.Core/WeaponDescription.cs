using System;
using System.Collections;
using System.Xml;
using TaleWorlds.Library;
using TaleWorlds.ObjectSystem;

namespace TaleWorlds.Core
{
	// Token: 0x0200004A RID: 74
	public class WeaponDescription : MBObjectBase
	{
		// Token: 0x17000212 RID: 530
		// (get) Token: 0x06000636 RID: 1590 RVA: 0x000158C6 File Offset: 0x00013AC6
		// (set) Token: 0x06000637 RID: 1591 RVA: 0x000158CE File Offset: 0x00013ACE
		public WeaponClass WeaponClass { get; private set; }

		// Token: 0x17000213 RID: 531
		// (get) Token: 0x06000638 RID: 1592 RVA: 0x000158D7 File Offset: 0x00013AD7
		// (set) Token: 0x06000639 RID: 1593 RVA: 0x000158DF File Offset: 0x00013ADF
		public WeaponFlags WeaponFlags { get; private set; }

		// Token: 0x17000214 RID: 532
		// (get) Token: 0x0600063A RID: 1594 RVA: 0x000158E8 File Offset: 0x00013AE8
		// (set) Token: 0x0600063B RID: 1595 RVA: 0x000158F0 File Offset: 0x00013AF0
		public string ItemUsageFeatures { get; private set; }

		// Token: 0x17000215 RID: 533
		// (get) Token: 0x0600063C RID: 1596 RVA: 0x000158F9 File Offset: 0x00013AF9
		// (set) Token: 0x0600063D RID: 1597 RVA: 0x00015901 File Offset: 0x00013B01
		public bool RotatedInHand { get; private set; }

		// Token: 0x17000216 RID: 534
		// (get) Token: 0x0600063E RID: 1598 RVA: 0x0001590A File Offset: 0x00013B0A
		// (set) Token: 0x0600063F RID: 1599 RVA: 0x00015912 File Offset: 0x00013B12
		public bool IsHiddenFromUI { get; set; }

		// Token: 0x17000217 RID: 535
		// (get) Token: 0x06000640 RID: 1600 RVA: 0x0001591B File Offset: 0x00013B1B
		public MBReadOnlyList<CraftingPiece> AvailablePieces
		{
			get
			{
				return this._availablePieces;
			}
		}

		// Token: 0x06000641 RID: 1601 RVA: 0x00015924 File Offset: 0x00013B24
		public override void Deserialize(MBObjectManager objectManager, XmlNode node)
		{
			base.Deserialize(objectManager, node);
			this.WeaponClass = ((node.Attributes["weapon_class"] != null) ? ((WeaponClass)Enum.Parse(typeof(WeaponClass), node.Attributes["weapon_class"].Value)) : WeaponClass.Undefined);
			this.ItemUsageFeatures = ((node.Attributes["item_usage_features"] != null) ? node.Attributes["item_usage_features"].Value : "");
			this.RotatedInHand = XmlHelper.ReadBool(node, "rotated_in_hand");
			this.UseCenterOfMassAsHandBase = XmlHelper.ReadBool(node, "use_center_of_mass_as_hand_base");
			foreach (object obj in node.ChildNodes)
			{
				XmlNode xmlNode = (XmlNode)obj;
				if (xmlNode.Name == "WeaponFlags")
				{
					using (IEnumerator enumerator2 = xmlNode.ChildNodes.GetEnumerator())
					{
						while (enumerator2.MoveNext())
						{
							object obj2 = enumerator2.Current;
							XmlNode xmlNode2 = (XmlNode)obj2;
							this.WeaponFlags |= (WeaponFlags)Enum.Parse(typeof(WeaponFlags), xmlNode2.Attributes["value"].Value);
						}
						continue;
					}
				}
				if (xmlNode.Name == "AvailablePieces")
				{
					this._availablePieces = new MBList<CraftingPiece>();
					foreach (object obj3 in xmlNode.ChildNodes)
					{
						XmlNode xmlNode3 = (XmlNode)obj3;
						if (xmlNode3.NodeType == XmlNodeType.Element)
						{
							string value = xmlNode3.Attributes["id"].Value;
							CraftingPiece @object = MBObjectManager.Instance.GetObject<CraftingPiece>(value);
							if (@object != null)
							{
								this._availablePieces.Add(@object);
							}
						}
					}
				}
			}
		}

		// Token: 0x040002E8 RID: 744
		public bool UseCenterOfMassAsHandBase;

		// Token: 0x040002EA RID: 746
		private MBList<CraftingPiece> _availablePieces;
	}
}
