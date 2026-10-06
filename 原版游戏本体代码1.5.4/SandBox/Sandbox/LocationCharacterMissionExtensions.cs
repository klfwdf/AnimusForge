using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.MountAndBlade;

namespace SandBox;

public static class LocationCharacterMissionExtensions
{
	public static AgentBuildData GetAgentBuildData(this LocationCharacter locationCharacter)
	{
		return new AgentBuildData(locationCharacter.AgentData);
	}
}
