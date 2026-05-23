using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Xml;
using TaleWorlds.Library;

namespace TaleWorlds.ModuleManager;

public class SubModuleInfo
{
	public enum SubModuleTags
	{
		RejectedPlatform,
		ExclusivePlatform,
		DedicatedServerType,
		IsNoRenderModeElement,
		DependantRuntimeLibrary,
		PlayerHostedDedicatedServer,
		EngineType
	}

	private const string CertHashString = "29B0C803942C9D4221EF0CFB1AB1FEE47683DF7D";

	private const string CertSerialNum = "61EB518586D5D0884531D7FBC0316B69";

	public readonly List<Tuple<SubModuleTags, string>> Tags;

	public string Name { get; private set; }

	public string DLLName { get; private set; }

	public string DLLPath { get; private set; }

	public bool IsTWCertifiedDLL { get; private set; }

	public bool DLLExists { get; private set; }

	public List<string> Assemblies { get; private set; }

	public string SubModuleClassTypeName { get; private set; }

	public SubModuleInfo()
	{
		Tags = new List<Tuple<SubModuleTags, string>>();
	}

	public void LoadFrom(XmlNode subModuleNode, string path, bool isOfficial)
	{
		Tags.Clear();
		Name = subModuleNode.SelectSingleNode("Name").Attributes["value"].InnerText;
		DLLName = subModuleNode.SelectSingleNode("DLLName").Attributes["value"].InnerText;
		string dLLName = DLLName;
		dLLName = Path.Combine(path, "bin\\Win64_Shipping_Client", DLLName);
		if (!string.IsNullOrEmpty(DLLName))
		{
			DLLExists = File.Exists(dLLName);
			DLLPath = dLLName;
			if (!DLLExists)
			{
				Debug.Print("Couldn't find .dll: " + DLLPath);
			}
			IsTWCertifiedDLL = DLLExists && GetIsTWCertified(dLLName, isOfficial);
		}
		SubModuleClassTypeName = subModuleNode.SelectSingleNode("SubModuleClassType").Attributes["value"].InnerText;
		Assemblies = new List<string>();
		if (subModuleNode.SelectSingleNode("Assemblies") != null)
		{
			XmlNodeList xmlNodeList = subModuleNode.SelectSingleNode("Assemblies").SelectNodes("Assembly");
			for (int i = 0; i < xmlNodeList.Count; i++)
			{
				Assemblies.Add(xmlNodeList[i].Attributes["value"].InnerText);
			}
		}
		XmlNode xmlNode = subModuleNode.SelectSingleNode("Tags");
		if (xmlNode == null)
		{
			return;
		}
		XmlNodeList xmlNodeList2 = xmlNode.SelectNodes("Tag");
		for (int j = 0; j < xmlNodeList2.Count; j++)
		{
			if (Enum.TryParse<SubModuleTags>(xmlNodeList2[j].Attributes["key"].InnerText, out var result))
			{
				string innerText = xmlNodeList2[j].Attributes["value"].InnerText;
				Tags.Add(new Tuple<SubModuleTags, string>(result, innerText));
				if (result == SubModuleTags.DedicatedServerType && innerText != "none")
				{
					IsTWCertifiedDLL = true;
				}
			}
		}
	}

	private bool GetIsTWCertified(string fileName, bool isOfficial)
	{
		try
		{
			X509Certificate2 certificate = new X509Certificate2(fileName);
			X509Chain x509Chain = X509Chain.Create();
			x509Chain.Build(certificate);
			X509ChainElementEnumerator enumerator = x509Chain.ChainElements.GetEnumerator();
			while (enumerator.MoveNext())
			{
				X509ChainElement current = enumerator.Current;
				if (current.Certificate.GetCertHashString() == "29B0C803942C9D4221EF0CFB1AB1FEE47683DF7D" && current.Certificate.GetSerialNumberString() == "61EB518586D5D0884531D7FBC0316B69")
				{
					return true;
				}
			}
			return false;
		}
		catch
		{
			return false;
		}
	}
}
