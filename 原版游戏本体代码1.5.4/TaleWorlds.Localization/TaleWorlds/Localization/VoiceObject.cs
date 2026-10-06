using System.Xml;
using TaleWorlds.Library;

namespace TaleWorlds.Localization;

public class VoiceObject
{
	private readonly MBList<string> _voicePaths;

	public MBReadOnlyList<string> VoicePaths => _voicePaths;

	private VoiceObject()
	{
		_voicePaths = new MBList<string>();
	}

	private void AddVoicePath(string voicePath)
	{
		_voicePaths.Add(voicePath);
	}

	public void AddVoicePaths(XmlNode node, string modulePath)
	{
		foreach (XmlNode childNode in node.ChildNodes)
		{
			if (childNode.Name == "Voice")
			{
				string voicePath = modulePath + "/" + childNode.Attributes["path"].InnerText;
				AddVoicePath(voicePath);
			}
		}
	}

	public static VoiceObject Deserialize(XmlNode node, string modulePath)
	{
		VoiceObject voiceObject = new VoiceObject();
		foreach (XmlNode childNode in node.ChildNodes)
		{
			if (childNode.Name == "Voice")
			{
				string voicePath = modulePath + "/" + childNode.Attributes["path"].InnerText;
				voiceObject.AddVoicePath(voicePath);
			}
		}
		return voiceObject;
	}
}
