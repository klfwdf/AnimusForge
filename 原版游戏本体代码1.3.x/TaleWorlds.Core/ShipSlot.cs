using System;
using System.Xml;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace TaleWorlds.Core
{
	// Token: 0x020000C2 RID: 194
	public class ShipSlot : MBObjectBase
	{
		// Token: 0x170003B2 RID: 946
		// (get) Token: 0x06000AB5 RID: 2741 RVA: 0x00022914 File Offset: 0x00020B14
		// (set) Token: 0x06000AB6 RID: 2742 RVA: 0x0002291C File Offset: 0x00020B1C
		public string TypeId { get; private set; }

		// Token: 0x170003B3 RID: 947
		// (get) Token: 0x06000AB7 RID: 2743 RVA: 0x00022925 File Offset: 0x00020B25
		// (set) Token: 0x06000AB8 RID: 2744 RVA: 0x0002292D File Offset: 0x00020B2D
		public string MainPrefabId { get; private set; }

		// Token: 0x170003B4 RID: 948
		// (get) Token: 0x06000AB9 RID: 2745 RVA: 0x00022936 File Offset: 0x00020B36
		public MBReadOnlyList<ShipUpgradePiece> MatchingPieces
		{
			get
			{
				return this._matchingPieces;
			}
		}

		// Token: 0x06000ABA RID: 2746 RVA: 0x0002293E File Offset: 0x00020B3E
		public ShipSlot()
		{
			this._matchingPieces = new MBList<ShipUpgradePiece>();
		}

		// Token: 0x06000ABB RID: 2747 RVA: 0x00022951 File Offset: 0x00020B51
		public override void AfterRegister()
		{
			base.AfterRegister();
			this.Initialize();
			base.IsReady = true;
		}

		// Token: 0x06000ABC RID: 2748 RVA: 0x00022966 File Offset: 0x00020B66
		public void AddMatchingPiece(ShipUpgradePiece upgradePiece)
		{
			if (!this._matchingPieces.Contains(upgradePiece))
			{
				this._matchingPieces.Add(upgradePiece);
			}
		}

		// Token: 0x06000ABD RID: 2749 RVA: 0x00022982 File Offset: 0x00020B82
		public TextObject GetSlotTypeName()
		{
			return GameTexts.FindText("str_ship_slot_type", this.TypeId);
		}

		// Token: 0x06000ABE RID: 2750 RVA: 0x00022994 File Offset: 0x00020B94
		public override void Deserialize(MBObjectManager objectManager, XmlNode node)
		{
			base.Deserialize(objectManager, node);
			XmlAttribute xmlAttribute = node.Attributes["prefab_id"];
			this.MainPrefabId = ((xmlAttribute != null) ? xmlAttribute.Value : null) ?? base.StringId;
			string typeId = this.TypeId;
			XmlAttribute xmlAttribute2 = node.Attributes["type_id"];
			this.TypeId = ((xmlAttribute2 != null) ? xmlAttribute2.Value : null) ?? base.StringId;
			foreach (object obj in node.ChildNodes)
			{
				XmlNode xmlNode = (XmlNode)obj;
				if (xmlNode.Name.Equals("ShipUpgradePieces"))
				{
					foreach (object obj2 in xmlNode.ChildNodes)
					{
						XmlNode xmlNode2 = (XmlNode)obj2;
						if (xmlNode2.Name.Equals("ShipUpgradePiece"))
						{
							string value = xmlNode2.Attributes["id"].Value;
							ShipUpgradePiece @object = MBObjectManager.Instance.GetObject<ShipUpgradePiece>(value);
							this.AddMatchingPiece(@object);
							@object.AddTargetSlot(this);
						}
					}
				}
			}
		}

		// Token: 0x04000603 RID: 1539
		private MBList<ShipUpgradePiece> _matchingPieces;
	}
}
