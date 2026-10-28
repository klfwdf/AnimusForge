using System;
using System.IO;
using System.Reflection;
using System.Xml;
using Bannerlord.UIExtenderEx.Utils;

namespace Bannerlord.UIExtenderEx.Prefabs;

[Obsolete("PrefabExtensionInsertPatch is obsolete")]
public abstract class EmbedPrefabExtensionInsertPatch : PrefabExtensionInsertPatch
{
	private Assembly Assembly { get; }

	private string Path { get; }

	protected EmbedPrefabExtensionInsertPatch(Assembly assembly, string path)
	{
		Assembly = assembly;
		Path = path;
	}

	public override XmlDocument GetPrefabExtension()
	{
		using Stream stream = Assembly.GetManifestResourceStream(Path);
		XmlDocument xmlDocument = new XmlDocument();
		if (stream != null)
		{
			using XmlReader reader = XmlReader.Create(stream, new XmlReaderSettings
			{
				IgnoreComments = true,
				IgnoreWhitespace = true
			});
			xmlDocument.Load(reader);
		}
		else
		{
			MessageUtils.Fail("Failed get stream from assembly resource (" + Assembly.FullName + " " + Path + ")!");
		}
		if (!xmlDocument.HasChildNodes)
		{
			MessageUtils.Fail("Failed to parse extension (" + Assembly.FullName + " " + Path + ") XML!");
		}
		return xmlDocument;
	}
}
