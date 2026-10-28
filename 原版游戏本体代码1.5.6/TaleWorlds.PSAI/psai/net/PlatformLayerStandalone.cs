using System.IO;
using TaleWorlds.Library;

namespace psai.net;

internal class PlatformLayerStandalone : IPlatformLayer
{
	private Logik m_logik;

	public PlatformLayerStandalone(Logik logik)
	{
		m_logik = logik;
	}

	public void Initialize()
	{
	}

	public void Release()
	{
	}

	public string ConvertFilePathForPlatform(string filepath)
	{
		string text = filepath.Replace('\\', '/');
		string text2 = "";
		string text3 = "";
		if (text.Contains("/"))
		{
			text3 = Path.GetDirectoryName(text) + "/";
			text2 = Path.GetFileNameWithoutExtension(text);
		}
		else
		{
			text2 = Path.GetFileNameWithoutExtension(text);
		}
		if (ApplicationPlatform.CurrentPlatform == Platform.Orbis)
		{
			return text3 + "PS4/" + text2 + ".fsb";
		}
		if (ApplicationPlatform.CurrentPlatform == Platform.Durango)
		{
			return text3 + "XboxOne/" + text2 + ".fsb";
		}
		return text3 + "PC/" + text2 + ".ogg";
	}

	public Stream GetStreamOnPsaiSoundtrackFile(string filepath)
	{
		if (Logik.CheckIfFileExists(filepath))
		{
			return File.OpenRead(filepath);
		}
		return null;
	}
}
