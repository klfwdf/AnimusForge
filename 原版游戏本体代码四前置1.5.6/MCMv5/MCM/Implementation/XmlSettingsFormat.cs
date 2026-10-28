using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using BUTR.DependencyInjection;
using BUTR.DependencyInjection.Logger;
using MCM.Abstractions.Base;
using MCM.Abstractions.GameFeatures;
using MCM.Common;
using Newtonsoft.Json;

namespace MCM.Implementation;

public sealed class XmlSettingsFormat : BaseJsonSettingsFormat
{
	public override IEnumerable<string> FormatTypes => new string[1] { "xml" };

	public XmlSettingsFormat(IBUTRLogger<XmlSettingsFormat> logger)
		: base(logger)
	{
	}

	public override bool Save(BaseSettings settings, GameDirectory directory, string filename)
	{
		IFileSystemProvider service = GenericServiceProvider.GetService<IFileSystemProvider>();
		if (service == null)
		{
			return false;
		}
		GameFile orCreateFile = service.GetOrCreateFile(directory, filename + ".xml");
		if ((object)orCreateFile == null)
		{
			return false;
		}
		string value = SaveJson(settings);
		string name;
		if (settings is IWrapper wrapper)
		{
			object obj = wrapper.Object;
			if (obj != null)
			{
				name = obj.GetType().Name;
				goto IL_007f;
			}
		}
		name = settings.GetType().Name;
		goto IL_007f;
		IL_007f:
		XmlDocument xmlDocument = JsonConvert.DeserializeXmlNode(value, name);
		if (xmlDocument == null)
		{
			return false;
		}
		using MemoryStream memoryStream = new MemoryStream();
		using StreamWriter writer = new StreamWriter(memoryStream);
		xmlDocument.Save(writer);
		try
		{
			return service.WriteData(orCreateFile, memoryStream.ToArray());
		}
		catch (Exception)
		{
			return false;
		}
	}

	public override BaseSettings Load(BaseSettings settings, GameDirectory directory, string filename)
	{
		IFileSystemProvider service = GenericServiceProvider.GetService<IFileSystemProvider>();
		if (service == null)
		{
			return settings;
		}
		GameFile file = service.GetFile(directory, filename + ".xml");
		if ((object)file == null)
		{
			return settings;
		}
		byte[] array = service.ReadData(file);
		if (array == null)
		{
			Save(settings, directory, filename);
			return settings;
		}
		XmlDocument xmlDocument = new XmlDocument();
		using MemoryStream inStream = new MemoryStream(array);
		xmlDocument.Load(inStream);
		XmlElement xmlElement = xmlDocument[settings.GetType().Name];
		if (xmlElement == null)
		{
			Save(settings, directory, filename);
			return settings;
		}
		string content = JsonConvert.SerializeXmlNode(xmlElement, Newtonsoft.Json.Formatting.None, omitRootObject: true);
		if (!TryLoadFromJson(ref settings, content))
		{
			Save(settings, directory, filename);
		}
		return settings;
	}
}
