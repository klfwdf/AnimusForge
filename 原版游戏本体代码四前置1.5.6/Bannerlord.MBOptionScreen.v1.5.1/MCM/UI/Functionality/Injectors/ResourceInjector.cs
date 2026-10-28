using System;
using System.IO;
using System.Xml;
using Bannerlord.BUTR.Shared.Helpers;
using MCM.UI.Exceptions;
using TaleWorlds.Library;

namespace MCM.UI.Functionality.Injectors;

public abstract class ResourceInjector
{
	private static readonly bool SwapVerticalLayoutMethod = ComputeSwapVerticalLayoutMethod();

	private static bool ComputeSwapVerticalLayoutMethod()
	{
		ApplicationVersion? applicationVersion = Bannerlord.BUTR.Shared.Helpers.ApplicationVersionHelper.GameVersion();
		if (applicationVersion.HasValue)
		{
			ApplicationVersion valueOrDefault = applicationVersion.GetValueOrDefault();
			if (valueOrDefault.Major >= 1)
			{
				if (valueOrDefault.Major == 1)
				{
					return valueOrDefault.Minor < 4;
				}
				return false;
			}
			return true;
		}
		return false;
	}

	protected static XmlDocument Load(string embedPath)
	{
		using Stream stream = typeof(DefaultResourceInjector).Assembly.GetManifestResourceStream(embedPath);
		if (stream == null)
		{
			throw new MCMUIEmbedResourceNotFoundException("Could not find embed resource '" + embedPath + "'!");
		}
		using XmlReader reader = XmlReader.Create(stream, new XmlReaderSettings
		{
			IgnoreComments = true
		});
		XmlDocument xmlDocument = new XmlDocument();
		xmlDocument.Load(reader);
		if (SwapVerticalLayoutMethod && xmlDocument.DocumentElement != null)
		{
			SwapVerticalLayoutMethods(xmlDocument.DocumentElement);
		}
		return xmlDocument;
	}

	private static void SwapVerticalLayoutMethods(XmlNode node)
	{
		if (node.Attributes != null)
		{
			foreach (XmlAttribute attribute in node.Attributes)
			{
				if (attribute.Name.Equals("LayoutImp.LayoutMethod", StringComparison.Ordinal))
				{
					if (attribute.Value.Equals("VerticalTopToBottom", StringComparison.Ordinal))
					{
						attribute.Value = "VerticalBottomToTop";
					}
					else if (attribute.Value.Equals("VerticalBottomToTop", StringComparison.Ordinal))
					{
						attribute.Value = "VerticalTopToBottom";
					}
				}
			}
		}
		foreach (XmlNode childNode in node.ChildNodes)
		{
			SwapVerticalLayoutMethods(childNode);
		}
	}

	public abstract void Inject();
}
