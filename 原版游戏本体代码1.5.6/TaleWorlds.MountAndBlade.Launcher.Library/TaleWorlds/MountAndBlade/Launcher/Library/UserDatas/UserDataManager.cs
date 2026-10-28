using System;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace TaleWorlds.MountAndBlade.Launcher.Library.UserDatas;

public class UserDataManager
{
	private const string DataFolder = "\\Mount and Blade II Bannerlord\\Configs\\";

	private const string FileName = "LauncherData.xml";

	private readonly string _filePath;

	public UserData UserData { get; private set; }

	public UserDataManager()
	{
		UserData = new UserData();
		string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
		folderPath += "\\Mount and Blade II Bannerlord\\Configs\\";
		if (!Directory.Exists(folderPath))
		{
			try
			{
				Directory.CreateDirectory(folderPath);
			}
			catch (Exception value)
			{
				Console.WriteLine(value);
			}
		}
		_filePath = folderPath + "LauncherData.xml";
	}

	public bool HasUserData()
	{
		return File.Exists(_filePath);
	}

	public void LoadUserData()
	{
		if (!File.Exists(_filePath))
		{
			return;
		}
		XmlSerializer xmlSerializer = new XmlSerializer(typeof(UserData));
		try
		{
			using XmlReader xmlReader = XmlReader.Create(_filePath);
			UserData = (UserData)xmlSerializer.Deserialize(xmlReader);
		}
		catch (Exception value)
		{
			Console.WriteLine(value);
		}
	}

	public void SaveUserData()
	{
		XmlSerializer xmlSerializer = new XmlSerializer(typeof(UserData));
		try
		{
			using XmlWriter xmlWriter = XmlWriter.Create(_filePath, new XmlWriterSettings
			{
				Indent = true
			});
			xmlSerializer.Serialize(xmlWriter, UserData);
		}
		catch (Exception value)
		{
			Console.WriteLine(value);
		}
	}
}
