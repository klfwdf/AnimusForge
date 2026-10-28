using System;
using System.IO;
using System.Xml;
using Bannerlord.UIExtenderEx.Utils;
using TaleWorlds.Engine;

namespace Bannerlord.UIExtenderEx.Prefabs;

[Obsolete("Use Prefabs2.PrefabExtensionInsertPatch instead.")]
public abstract class ModulePrefabExtensionInsertPatch : PrefabExtensionInsertPatch
{
	private string Name { get; }

	private string ModuleName { get; }

	protected ModulePrefabExtensionInsertPatch(string name, string moduleName)
	{
		Name = name;
		ModuleName = moduleName;
	}

	public override XmlDocument GetPrefabExtension()
	{
		string text = System.IO.Path.Combine(Utilities.GetBasePath(), "Modules", ModuleName, "GUI", "PrefabExtensions", Name + ".xml");
		XmlDocument xmlDocument = new XmlDocument();
		if (File.Exists(text))
		{
			using XmlReader reader = XmlReader.Create(text, new XmlReaderSettings
			{
				IgnoreComments = true,
				IgnoreWhitespace = true
			});
			xmlDocument.Load(reader);
		}
		else
		{
			MessageUtils.Fail("Failed to get file " + text + " XML!");
		}
		if (!xmlDocument.HasChildNodes)
		{
			MessageUtils.Fail("Failed to parse extension (" + Name + ") XML!");
		}
		return xmlDocument;
	}
}
