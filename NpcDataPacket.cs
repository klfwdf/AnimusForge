namespace AnimusForge;

public class NpcDataPacket
{
	public string Name;

	public string RoleDesc;

	public string PersonalityDesc;

	public string BackgroundDesc;

	public int AgentIndex;

	public bool IsHero;

	public string CultureId;

	public string UnnamedKey;

	public string TroopId;

	public string UnnamedRank;

	public bool IsFemale;

	public float Age;

	public string PromptGivenName;

	public string PromptDisplayName;

	// Position snapshot used by scene prompt rendering. Keeping it on the packet
	// avoids rescanning Mission.Agents for every nearby-person line and relay turn.
	public bool HasScenePosition;

	public float ScenePositionX;

	public float ScenePositionY;

	public float ScenePositionZ;
}
