using System;
using System.Collections;
using System.Xml;
using TaleWorlds.ObjectSystem;

namespace TaleWorlds.Core
{
	// Token: 0x0200009B RID: 155
	public class MBBodyProperty : MBObjectBase
	{
		// Token: 0x17000312 RID: 786
		// (get) Token: 0x060008E1 RID: 2273 RVA: 0x0001D275 File Offset: 0x0001B475
		// (set) Token: 0x060008E2 RID: 2274 RVA: 0x0001D27D File Offset: 0x0001B47D
		public string HairTags { get; set; } = "";

		// Token: 0x17000313 RID: 787
		// (get) Token: 0x060008E3 RID: 2275 RVA: 0x0001D286 File Offset: 0x0001B486
		// (set) Token: 0x060008E4 RID: 2276 RVA: 0x0001D28E File Offset: 0x0001B48E
		public string BeardTags { get; set; } = "";

		// Token: 0x17000314 RID: 788
		// (get) Token: 0x060008E5 RID: 2277 RVA: 0x0001D297 File Offset: 0x0001B497
		// (set) Token: 0x060008E6 RID: 2278 RVA: 0x0001D29F File Offset: 0x0001B49F
		public string TattooTags { get; set; } = "";

		// Token: 0x17000315 RID: 789
		// (get) Token: 0x060008E7 RID: 2279 RVA: 0x0001D2A8 File Offset: 0x0001B4A8
		public BodyProperties BodyPropertyMin
		{
			get
			{
				return this._bodyPropertyMin;
			}
		}

		// Token: 0x17000316 RID: 790
		// (get) Token: 0x060008E8 RID: 2280 RVA: 0x0001D2B0 File Offset: 0x0001B4B0
		public BodyProperties BodyPropertyMax
		{
			get
			{
				return this._bodyPropertyMax;
			}
		}

		// Token: 0x060008E9 RID: 2281 RVA: 0x0001D2B8 File Offset: 0x0001B4B8
		public MBBodyProperty(string stringId)
			: base(stringId)
		{
		}

		// Token: 0x060008EA RID: 2282 RVA: 0x0001D2E2 File Offset: 0x0001B4E2
		public MBBodyProperty()
		{
		}

		// Token: 0x060008EB RID: 2283 RVA: 0x0001D30C File Offset: 0x0001B50C
		public static MBBodyProperty CreateFrom(MBBodyProperty bodyProperty)
		{
			MBBodyProperty mbbodyProperty = MBObjectManager.Instance.CreateObject<MBBodyProperty>();
			mbbodyProperty.HairTags = bodyProperty.HairTags;
			mbbodyProperty.BeardTags = bodyProperty.BeardTags;
			mbbodyProperty.TattooTags = bodyProperty.TattooTags;
			mbbodyProperty._bodyPropertyMin = bodyProperty._bodyPropertyMin;
			mbbodyProperty._bodyPropertyMax = bodyProperty._bodyPropertyMax;
			return mbbodyProperty;
		}

		// Token: 0x060008EC RID: 2284 RVA: 0x0001D360 File Offset: 0x0001B560
		public void Init(BodyProperties bodyPropertyMin, BodyProperties bodyPropertyMax)
		{
			base.Initialize();
			this._bodyPropertyMin = bodyPropertyMin;
			this._bodyPropertyMax = bodyPropertyMax;
			if (this._bodyPropertyMax.Age <= 0f)
			{
				this._bodyPropertyMax = this._bodyPropertyMin;
			}
			if (this._bodyPropertyMin.Age <= 0f)
			{
				this._bodyPropertyMin = this._bodyPropertyMax;
			}
			base.AfterInitialized();
		}

		// Token: 0x060008ED RID: 2285 RVA: 0x0001D3C4 File Offset: 0x0001B5C4
		public override void Deserialize(MBObjectManager objectManager, XmlNode node)
		{
			base.Deserialize(objectManager, node);
			foreach (object obj in node.ChildNodes)
			{
				XmlNode xmlNode = (XmlNode)obj;
				if (xmlNode.Name == "BodyPropertiesMin")
				{
					BodyProperties.FromXmlNode(xmlNode, out this._bodyPropertyMin);
				}
				else if (xmlNode.Name == "BodyPropertiesMax")
				{
					BodyProperties.FromXmlNode(xmlNode, out this._bodyPropertyMax);
				}
				else
				{
					if (xmlNode.Name == "hair_tags")
					{
						using (IEnumerator enumerator2 = xmlNode.ChildNodes.GetEnumerator())
						{
							while (enumerator2.MoveNext())
							{
								object obj2 = enumerator2.Current;
								XmlNode xmlNode2 = (XmlNode)obj2;
								string hairTags = this.HairTags;
								XmlAttributeCollection attributes = xmlNode2.Attributes;
								this.HairTags = hairTags + ((attributes != null) ? attributes["name"].Value : null) + ",";
							}
							continue;
						}
					}
					if (xmlNode.Name == "beard_tags")
					{
						using (IEnumerator enumerator2 = xmlNode.ChildNodes.GetEnumerator())
						{
							while (enumerator2.MoveNext())
							{
								object obj3 = enumerator2.Current;
								XmlNode xmlNode3 = (XmlNode)obj3;
								string beardTags = this.BeardTags;
								XmlAttributeCollection attributes2 = xmlNode3.Attributes;
								this.BeardTags = beardTags + ((attributes2 != null) ? attributes2["name"].Value : null) + ",";
							}
							continue;
						}
					}
					if (xmlNode.Name == "tattoo_tags")
					{
						foreach (object obj4 in xmlNode.ChildNodes)
						{
							XmlNode xmlNode4 = (XmlNode)obj4;
							string tattooTags = this.TattooTags;
							XmlAttributeCollection attributes3 = xmlNode4.Attributes;
							this.TattooTags = tattooTags + ((attributes3 != null) ? attributes3["name"].Value : null) + ",";
						}
					}
				}
			}
			if (this._bodyPropertyMax.Age <= 0f)
			{
				this._bodyPropertyMax = this._bodyPropertyMin;
			}
			if (this._bodyPropertyMin.Age <= 0f)
			{
				this._bodyPropertyMin = this._bodyPropertyMax;
			}
		}

		// Token: 0x040004FE RID: 1278
		private BodyProperties _bodyPropertyMin;

		// Token: 0x040004FF RID: 1279
		private BodyProperties _bodyPropertyMax;
	}
}
