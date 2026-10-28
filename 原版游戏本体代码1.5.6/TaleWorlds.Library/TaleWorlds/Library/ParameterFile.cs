using System;
using System.IO;
using System.Xml;

namespace TaleWorlds.Library;

public class ParameterFile
{
	private int _failedAttemptsCount;

	private const int MaxFailedAttemptsCount = 100;

	public string Path { get; private set; }

	public DateTime LastCheckedTime { get; private set; }

	public ParameterContainer ParameterContainer { get; private set; }

	public ParameterFile(string path)
	{
		ParameterContainer = new ParameterContainer();
		Path = path;
		LastCheckedTime = DateTime.MinValue;
	}

	public bool CheckIfNeedsToBeRefreshed()
	{
		return File.GetLastWriteTime(Path) > LastCheckedTime;
	}

	public void Refresh()
	{
		ParameterContainer.ClearParameters();
		DateTime lastWriteTime = File.GetLastWriteTime(Path);
		XmlDocument xmlDocument = new XmlDocument();
		try
		{
			xmlDocument.Load(Path);
		}
		catch
		{
			_failedAttemptsCount++;
			if (_failedAttemptsCount >= 100)
			{
				Debug.FailedAssert("Could not load parameters file", "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\Base\\TaleWorlds.Library\\ParameterFile.cs", "Refresh", 47);
			}
			return;
		}
		_failedAttemptsCount = 0;
		foreach (XmlElement childNode in xmlDocument.FirstChild.ChildNodes)
		{
			string attribute = childNode.GetAttribute("name");
			string attribute2 = childNode.GetAttribute("value");
			ParameterContainer.AddParameter(attribute, attribute2, overwriteIfExists: true);
		}
		LastCheckedTime = lastWriteTime;
	}
}
