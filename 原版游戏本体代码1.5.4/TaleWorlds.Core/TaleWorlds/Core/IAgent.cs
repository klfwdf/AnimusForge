namespace TaleWorlds.Core;

public interface IAgent
{
	BasicCharacterObject Character { get; }

	AgentState State { get; }

	IMissionTeam Team { get; }

	IAgentOriginBase Origin { get; }

	float Age { get; }

	bool IsEnemyOf(IAgent agent);

	bool IsFriendOf(IAgent agent);

	bool IsActive();

	void SetAsConversationAgent(bool set);

	void OnConversationStarted();
}
