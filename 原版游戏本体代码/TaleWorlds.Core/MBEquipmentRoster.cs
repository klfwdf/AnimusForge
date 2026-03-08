using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using TaleWorlds.Library;
using TaleWorlds.ObjectSystem;

namespace TaleWorlds.Core
{
	// Token: 0x0200009E RID: 158
	public class MBEquipmentRoster : MBObjectBase
	{
		// Token: 0x17000318 RID: 792
		// (get) Token: 0x060008F3 RID: 2291 RVA: 0x0001D6D6 File Offset: 0x0001B8D6
		// (set) Token: 0x060008F4 RID: 2292 RVA: 0x0001D6DE File Offset: 0x0001B8DE
		public EquipmentFlags EquipmentFlags { get; private set; }

		// Token: 0x060008F5 RID: 2293 RVA: 0x0001D6E7 File Offset: 0x0001B8E7
		public bool HasEquipmentFlags(EquipmentFlags flags)
		{
			return (this.EquipmentFlags & flags) == flags;
		}

		// Token: 0x060008F6 RID: 2294 RVA: 0x0001D6F4 File Offset: 0x0001B8F4
		public bool IsEquipmentTemplate()
		{
			return this.EquipmentFlags > EquipmentFlags.None;
		}

		// Token: 0x17000319 RID: 793
		// (get) Token: 0x060008F7 RID: 2295 RVA: 0x0001D6FF File Offset: 0x0001B8FF
		public MBReadOnlyList<Equipment> AllEquipments
		{
			get
			{
				if (this._equipments.IsEmpty<Equipment>())
				{
					return new MBList<Equipment>(1) { MBEquipmentRoster.EmptyEquipment };
				}
				return this._equipments;
			}
		}

		// Token: 0x1700031A RID: 794
		// (get) Token: 0x060008F8 RID: 2296 RVA: 0x0001D726 File Offset: 0x0001B926
		public Equipment DefaultEquipment
		{
			get
			{
				if (!this._equipments.IsEmpty<Equipment>())
				{
					return this._equipments.FirstOrDefault<Equipment>();
				}
				return MBEquipmentRoster.EmptyEquipment;
			}
		}

		// Token: 0x060008F9 RID: 2297 RVA: 0x0001D746 File Offset: 0x0001B946
		public void Init(MBObjectManager objectManager, XmlNode node)
		{
			if (node.Name == "EquipmentRoster")
			{
				this.InitEquipment(objectManager, node);
				return;
			}
			Debug.FailedAssert("false", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.Core\\MBEquipmentRoster.cs", "Init", 81);
		}

		// Token: 0x060008FA RID: 2298 RVA: 0x0001D77C File Offset: 0x0001B97C
		public override void Deserialize(MBObjectManager objectManager, XmlNode node)
		{
			base.Deserialize(objectManager, node);
			if (node.Attributes["culture"] != null)
			{
				this.EquipmentCulture = MBObjectManager.Instance.ReadObjectReferenceFromXml<BasicCultureObject>("culture", node);
			}
			foreach (object obj in node.ChildNodes)
			{
				XmlNode xmlNode = (XmlNode)obj;
				if (xmlNode.Name == "EquipmentSet")
				{
					this.InitEquipment(objectManager, xmlNode);
				}
				if (xmlNode.Name == "Flags")
				{
					foreach (object obj2 in xmlNode.Attributes)
					{
						XmlAttribute xmlAttribute = (XmlAttribute)obj2;
						EquipmentFlags equipmentFlags = (EquipmentFlags)Enum.Parse(typeof(EquipmentFlags), xmlAttribute.Name);
						if (bool.Parse(xmlAttribute.InnerText))
						{
							this.EquipmentFlags |= equipmentFlags;
						}
					}
				}
			}
		}

		// Token: 0x060008FB RID: 2299 RVA: 0x0001D8B0 File Offset: 0x0001BAB0
		private void InitEquipment(MBObjectManager objectManager, XmlNode node)
		{
			base.Initialize();
			Equipment.EquipmentType equipmentType = Equipment.EquipmentType.Battle;
			if (node.Attributes["equipmentType"] != null)
			{
				if (!Enum.TryParse<Equipment.EquipmentType>(node.Attributes["equipmentType"].Value, out equipmentType))
				{
					Debug.FailedAssert("This equipment definition is wrong", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.Core\\MBEquipmentRoster.cs", "InitEquipment", 127);
				}
			}
			else if (node.Attributes["civilian"] != null && bool.Parse(node.Attributes["civilian"].Value))
			{
				equipmentType = Equipment.EquipmentType.Civilian;
			}
			Equipment equipment = new Equipment(equipmentType);
			equipment.Deserialize(objectManager, node);
			this._equipments.Add(equipment);
			base.AfterInitialized();
		}

		// Token: 0x060008FC RID: 2300 RVA: 0x0001D960 File Offset: 0x0001BB60
		public void AddEquipmentRoster(MBEquipmentRoster equipmentRoster, Equipment.EquipmentType equipmentType)
		{
			foreach (Equipment equipment in equipmentRoster._equipments.ToList<Equipment>())
			{
				if ((equipmentType == Equipment.EquipmentType.Stealth && equipment.IsStealth) || (equipmentType == Equipment.EquipmentType.Civilian && equipment.IsCivilian) || (equipmentType == Equipment.EquipmentType.Battle && equipment.IsBattle))
				{
					this._equipments.Add(equipment);
				}
			}
			this.EquipmentFlags = equipmentRoster.EquipmentFlags;
		}

		// Token: 0x060008FD RID: 2301 RVA: 0x0001D9EC File Offset: 0x0001BBEC
		public void AddOverridenEquipments(MBObjectManager objectManager, List<XmlNode> overridenEquipmentSlots)
		{
			List<Equipment> list = this._equipments.ToList<Equipment>();
			this._equipments.Clear();
			foreach (Equipment equipment in list)
			{
				this._equipments.Add(equipment.Clone(false));
			}
			foreach (XmlNode node in overridenEquipmentSlots)
			{
				foreach (Equipment equipment2 in this._equipments)
				{
					equipment2.DeserializeNode(objectManager, node);
				}
			}
		}

		// Token: 0x060008FE RID: 2302 RVA: 0x0001DAD4 File Offset: 0x0001BCD4
		public void OrderEquipments()
		{
			this._equipments = new MBList<Equipment>(from eq in this._equipments
				orderby !eq.IsCivilian && !eq.IsStealth descending
				select eq);
		}

		// Token: 0x060008FF RID: 2303 RVA: 0x0001DB0B File Offset: 0x0001BD0B
		public void InitializeDefaultEquipment(string equipmentName)
		{
			if (this._equipments[0] == null)
			{
				this._equipments[0] = new Equipment(Equipment.EquipmentType.Battle);
			}
			this._equipments[0].FillFrom(Game.Current.GetDefaultEquipmentWithName(equipmentName), true);
		}

		// Token: 0x04000513 RID: 1299
		public static readonly Equipment EmptyEquipment = new Equipment(Equipment.EquipmentType.Civilian);

		// Token: 0x04000514 RID: 1300
		private MBList<Equipment> _equipments = new MBList<Equipment>();

		// Token: 0x04000515 RID: 1301
		public BasicCultureObject EquipmentCulture;
	}
}
