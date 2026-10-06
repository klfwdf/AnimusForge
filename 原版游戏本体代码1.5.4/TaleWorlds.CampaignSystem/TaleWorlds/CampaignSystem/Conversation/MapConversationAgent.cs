using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.Conversation;

public class MapConversationAgent : IAgent
{
	private CharacterObject _characterObject;

	public bool DeliveredLine;

	public BasicCharacterObject Character => _characterObject;

	public AgentState State => AgentState.Active;

	public IMissionTeam Team => null;

	public IAgentOriginBase Origin => null;

	public float Age => Character.Age;

	public MapConversationAgent(CharacterObject characterObject)
	{
		_characterObject = characterObject;
	}

	public bool IsEnemyOf(IAgent agent)
	{
		return false;
	}

	public bool IsFriendOf(IAgent agent)
	{
		return true;
	}

	public bool IsActive()
	{
		return true;
	}

	public void SetAsConversationAgent(bool set)
	{
	}

	public void OnConversationStarted()
	{
	}
}
