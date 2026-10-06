using TaleWorlds.ObjectSystem;

namespace TaleWorlds.CampaignSystem;

public struct MeetingSceneData
{
	public string SceneID { get; private set; }

	public string CultureString { get; private set; }

	public CultureObject Culture => MBObjectManager.Instance.GetObject<CultureObject>(CultureString);

	public MeetingSceneData(string sceneID, string cultureString)
	{
		SceneID = sceneID;
		CultureString = cultureString;
	}
}
