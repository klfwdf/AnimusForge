using System;
using System.IO;
using System.Xml;
using TaleWorlds.Library;
using TaleWorlds.ObjectSystem;

namespace TaleWorlds.Core
{
	// Token: 0x0200009A RID: 154
	public sealed class ManagedParameters : IManagedParametersInitializer
	{
		// Token: 0x17000311 RID: 785
		// (get) Token: 0x060008D8 RID: 2264 RVA: 0x0001D063 File Offset: 0x0001B263
		public static ManagedParameters Instance { get; } = new ManagedParameters();

		// Token: 0x060008D9 RID: 2265 RVA: 0x0001D06A File Offset: 0x0001B26A
		public static float GetParameter(ManagedParametersEnum managedParameterType)
		{
			return ManagedParameters.Instance._managedParametersArray[(int)managedParameterType];
		}

		// Token: 0x060008DA RID: 2266 RVA: 0x0001D078 File Offset: 0x0001B278
		public static void SetParameter(ManagedParametersEnum managedParameterType, float newValue)
		{
			ManagedParameters.Instance._managedParametersArray[(int)managedParameterType] = newValue;
		}

		// Token: 0x060008DB RID: 2267 RVA: 0x0001D088 File Offset: 0x0001B288
		public void Initialize(string relativeXmlPath)
		{
			XmlDocument mergedXmlForManaged = MBObjectManager.GetMergedXmlForManaged("CoreParameters", true, true, "");
			this.LoadFromXml(mergedXmlForManaged);
		}

		// Token: 0x060008DC RID: 2268 RVA: 0x0001D0AE File Offset: 0x0001B2AE
		private ManagedParameters()
		{
		}

		// Token: 0x060008DD RID: 2269 RVA: 0x0001D0C4 File Offset: 0x0001B2C4
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

		// Token: 0x060008DE RID: 2270 RVA: 0x0001D110 File Offset: 0x0001B310
		private void LoadFromXml(XmlNode doc)
		{
			Debug.Print("loading managed_core_parameters.xml", 0, Debug.DebugColor.White, 17592186044416UL);
			if (doc.ChildNodes.Count <= 1)
			{
				throw new TWXmlLoadException("Incorrect XML document format.");
			}
			if (doc.ChildNodes[1].Name != "base")
			{
				throw new TWXmlLoadException("Incorrect XML document format.");
			}
			if (doc.ChildNodes[1].ChildNodes[0].Name != "managed_core_parameters")
			{
				throw new TWXmlLoadException("Incorrect XML document format.");
			}
			XmlNode xmlNode = null;
			if (doc.ChildNodes[1].ChildNodes[0].Name == "managed_core_parameters")
			{
				xmlNode = doc.ChildNodes[1].ChildNodes[0].ChildNodes[0];
			}
			while (xmlNode != null)
			{
				ManagedParametersEnum managedParametersEnum;
				if (xmlNode.Name == "managed_core_parameter" && xmlNode.NodeType != XmlNodeType.Comment && Enum.TryParse<ManagedParametersEnum>(xmlNode.Attributes["id"].Value, true, out managedParametersEnum))
				{
					this._managedParametersArray[(int)managedParametersEnum] = float.Parse(xmlNode.Attributes["value"].Value);
				}
				xmlNode = xmlNode.NextSibling;
			}
		}

		// Token: 0x060008DF RID: 2271 RVA: 0x0001D25F File Offset: 0x0001B45F
		public float GetManagedParameter(ManagedParametersEnum managedParameterEnum)
		{
			return this._managedParametersArray[(int)managedParameterEnum];
		}

		// Token: 0x040004F9 RID: 1273
		private readonly float[] _managedParametersArray = new float[72];
	}
}
