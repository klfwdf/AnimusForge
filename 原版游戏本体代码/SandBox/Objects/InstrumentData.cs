using System;
using System.Xml;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.ObjectSystem;

namespace SandBox.Objects
{
	// Token: 0x02000039 RID: 57
	public class InstrumentData : MBObjectBase
	{
		// Token: 0x17000027 RID: 39
		// (get) Token: 0x060001F7 RID: 503 RVA: 0x0000CAEF File Offset: 0x0000ACEF
		public MBReadOnlyList<ValueTuple<HumanBone, string>> InstrumentEntities
		{
			get
			{
				return this._instrumentEntities;
			}
		}

		// Token: 0x17000028 RID: 40
		// (get) Token: 0x060001F8 RID: 504 RVA: 0x0000CAF7 File Offset: 0x0000ACF7
		// (set) Token: 0x060001F9 RID: 505 RVA: 0x0000CAFF File Offset: 0x0000ACFF
		public string SittingAction { get; private set; }

		// Token: 0x17000029 RID: 41
		// (get) Token: 0x060001FA RID: 506 RVA: 0x0000CB08 File Offset: 0x0000AD08
		// (set) Token: 0x060001FB RID: 507 RVA: 0x0000CB10 File Offset: 0x0000AD10
		public string StandingAction { get; private set; }

		// Token: 0x1700002A RID: 42
		// (get) Token: 0x060001FC RID: 508 RVA: 0x0000CB19 File Offset: 0x0000AD19
		// (set) Token: 0x060001FD RID: 509 RVA: 0x0000CB21 File Offset: 0x0000AD21
		public string Tag { get; private set; }

		// Token: 0x1700002B RID: 43
		// (get) Token: 0x060001FE RID: 510 RVA: 0x0000CB2A File Offset: 0x0000AD2A
		// (set) Token: 0x060001FF RID: 511 RVA: 0x0000CB32 File Offset: 0x0000AD32
		public bool IsDataWithoutInstrument { get; private set; }

		// Token: 0x06000200 RID: 512 RVA: 0x0000CB3B File Offset: 0x0000AD3B
		public InstrumentData()
		{
		}

		// Token: 0x06000201 RID: 513 RVA: 0x0000CB43 File Offset: 0x0000AD43
		public InstrumentData(string stringId)
			: base(stringId)
		{
		}

		// Token: 0x06000202 RID: 514 RVA: 0x0000CB4C File Offset: 0x0000AD4C
		public void InitializeInstrumentData(string sittingAction, string standingAction, bool isDataWithoutInstrument)
		{
			this.SittingAction = sittingAction;
			this.StandingAction = standingAction;
			this._instrumentEntities = new MBList<ValueTuple<HumanBone, string>>(0);
			this.IsDataWithoutInstrument = isDataWithoutInstrument;
			this.Tag = string.Empty;
		}

		// Token: 0x06000203 RID: 515 RVA: 0x0000CB7C File Offset: 0x0000AD7C
		public override void Deserialize(MBObjectManager objectManager, XmlNode node)
		{
			base.Deserialize(objectManager, node);
			this.SittingAction = Convert.ToString(node.Attributes["sittingAction"].Value);
			this.StandingAction = Convert.ToString(node.Attributes["standingAction"].Value);
			XmlAttribute xmlAttribute = node.Attributes["tag"];
			this.Tag = Convert.ToString((xmlAttribute != null) ? xmlAttribute.Value : null);
			this._instrumentEntities = new MBList<ValueTuple<HumanBone, string>>();
			if (node.HasChildNodes)
			{
				foreach (object obj in node.ChildNodes)
				{
					XmlNode xmlNode = (XmlNode)obj;
					if (xmlNode.Name == "Entities")
					{
						foreach (object obj2 in xmlNode.ChildNodes)
						{
							XmlNode xmlNode2 = (XmlNode)obj2;
							if (xmlNode2.Name == "Entity")
							{
								XmlAttributeCollection attributes = xmlNode2.Attributes;
								if (((attributes != null) ? attributes["name"] : null) != null && xmlNode2.Attributes["bone"] != null)
								{
									string item = Convert.ToString(xmlNode2.Attributes["name"].Value);
									HumanBone item2;
									if (Enum.TryParse<HumanBone>(xmlNode2.Attributes["bone"].Value, out item2))
									{
										this._instrumentEntities.Add(new ValueTuple<HumanBone, string>(item2, item));
									}
									else
									{
										Debug.FailedAssert("Couldn't parse bone xml node for instrument.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox\\Objects\\InstrumentData.cs", "Deserialize", 62);
									}
								}
								else
								{
									Debug.FailedAssert("Couldn't find required attributes of entity xml node in Instrument", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox\\Objects\\InstrumentData.cs", "Deserialize", 67);
								}
							}
						}
					}
				}
			}
			this._instrumentEntities.Capacity = this._instrumentEntities.Count;
		}

		// Token: 0x040000BC RID: 188
		private MBList<ValueTuple<HumanBone, string>> _instrumentEntities;
	}
}
