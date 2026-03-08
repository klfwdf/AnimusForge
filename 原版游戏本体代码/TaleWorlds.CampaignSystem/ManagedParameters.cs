using System;
using System.IO;
using System.Xml;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem
{
	// Token: 0x0200009E RID: 158
	public sealed class ManagedParameters : IManagedParametersInitializer
	{
		// Token: 0x1700051E RID: 1310
		// (get) Token: 0x060012FC RID: 4860 RVA: 0x000542A6 File Offset: 0x000524A6
		public static ManagedParameters Instance { get; } = new ManagedParameters();

		// Token: 0x060012FD RID: 4861 RVA: 0x000542B0 File Offset: 0x000524B0
		public void Initialize(string relativeXmlPath)
		{
			XmlDocument doc = ManagedParameters.LoadXmlFile(relativeXmlPath);
			this.LoadFromXml(doc);
		}

		// Token: 0x060012FE RID: 4862 RVA: 0x000542CC File Offset: 0x000524CC
		private void LoadFromXml(XmlNode doc)
		{
			XmlNode xmlNode = null;
			if (doc.ChildNodes[1].ChildNodes[0].Name == "managed_campaign_parameters")
			{
				xmlNode = doc.ChildNodes[1].ChildNodes[0].ChildNodes[0];
			}
			while (xmlNode != null)
			{
				ManagedParametersEnum managedParametersEnum;
				if (xmlNode.Name == "managed_campaign_parameter" && xmlNode.NodeType != XmlNodeType.Comment && Enum.TryParse<ManagedParametersEnum>(xmlNode.Attributes["id"].Value, true, out managedParametersEnum))
				{
					this._managedParametersArray[(int)managedParametersEnum] = bool.Parse(xmlNode.Attributes["value"].Value);
				}
				xmlNode = xmlNode.NextSibling;
			}
		}

		// Token: 0x060012FF RID: 4863 RVA: 0x00054394 File Offset: 0x00052594
		private static XmlDocument LoadXmlFile(string path)
		{
			Debug.Print("opening " + path, 0, Debug.DebugColor.White, 17592186044416UL);
			XmlDocument xmlDocument = new XmlDocument();
			StreamReader streamReader = new StreamReader(path);
			string xml = streamReader.ReadToEnd();
			xmlDocument.LoadXml(xml);
			streamReader.Close();
			return xmlDocument;
		}

		// Token: 0x06001300 RID: 4864 RVA: 0x000543DD File Offset: 0x000525DD
		public bool GetManagedParameter(ManagedParametersEnum _managedParametersEnum)
		{
			return this._managedParametersArray[(int)_managedParametersEnum];
		}

		// Token: 0x06001301 RID: 4865 RVA: 0x000543E8 File Offset: 0x000525E8
		public bool SetManagedParameter(ManagedParametersEnum _managedParametersEnum, bool value)
		{
			this._managedParametersArray[(int)_managedParametersEnum] = value;
			return value;
		}

		// Token: 0x04000625 RID: 1573
		private readonly bool[] _managedParametersArray = new bool[2];
	}
}
