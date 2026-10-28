using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem;

public interface ICampaignMission
{
	TaleWorlds.Core.GameState State { get; }

	IMissionTroopSupplier AgentSupplier { get; }

	Location Location { get; set; }

	Alley LastVisitedAlley { get; set; }

	MissionMode Mode { get; }

	void SetMissionMode(MissionMode newMode, bool atStart);

	void OnCloseEncounterMenu();

	bool AgentLookingAtAgent(IAgent agent1, IAgent agent2);

	void OnCharacterLocationChanged(LocationCharacter locationCharacter, Location fromLocation, Location toLocation);

	void OnProcessSentence();

	void OnConversationContinue();

	bool CheckIfAgentCanFollow(IAgent agent);

	void AddAgentFollowing(IAgent agent);

	bool CheckIfAgentCanUnFollow(IAgent agent);

	void RemoveAgentFollowing(IAgent agent);

	void OnConversationPlay(string idleActionId, string idleFaceAnimId, string reactionId, string reactionFaceAnimId, string soundPath);

	void OnConversationStart(IAgent agent, bool setActionsInstantly);

	void OnConversationEnd(IAgent agent);

	void EndMission();

	void FadeOutCharacter(CharacterObject characterObject);

	void OnGameStateChanged();
}
