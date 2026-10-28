using TaleWorlds.ObjectSystem;

namespace TaleWorlds.MountAndBlade;

public class MultiplayerStarter
{
	private readonly MBObjectManager _objectManager;

	public MultiplayerStarter(MBObjectManager objectManager)
	{
		_objectManager = objectManager;
	}

	public void LoadXMLFromFile(string xmlPath, string xsdPath)
	{
		_objectManager.LoadOneXmlFromFile(xmlPath, xsdPath);
	}

	public void ClearEmptyObjects()
	{
		_objectManager.UnregisterNonReadyObjects();
	}
}
