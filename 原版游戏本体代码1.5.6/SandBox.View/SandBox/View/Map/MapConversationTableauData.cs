using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;

namespace SandBox.View.Map;

public class MapConversationTableauData
{
	public ConversationCharacterData PlayerCharacterData { get; private set; }

	public ConversationCharacterData ConversationPartnerData { get; private set; }

	public TerrainType ConversationTerrainType { get; private set; }

	public float TimeOfDay { get; private set; }

	public bool IsCurrentTerrainUnderSnow { get; private set; }

	public Settlement Settlement { get; private set; }

	public string LocationId { get; private set; }

	public bool IsSnowing { get; private set; }

	public bool IsRaining { get; private set; }

	private MapConversationTableauData()
	{
	}

	public static MapConversationTableauData CreateFrom(ConversationCharacterData playerCharacterData, ConversationCharacterData conversationPartnerData, TerrainType terrainType, float timeOfDay, bool isCurrentTerrainUnderSnow, Settlement settlement, string locationId, bool isRaining, bool isSnowing)
	{
		return new MapConversationTableauData
		{
			PlayerCharacterData = playerCharacterData,
			ConversationPartnerData = conversationPartnerData,
			ConversationTerrainType = terrainType,
			TimeOfDay = timeOfDay,
			IsCurrentTerrainUnderSnow = isCurrentTerrainUnderSnow,
			Settlement = settlement,
			LocationId = locationId,
			IsRaining = isRaining,
			IsSnowing = isSnowing
		};
	}
}
