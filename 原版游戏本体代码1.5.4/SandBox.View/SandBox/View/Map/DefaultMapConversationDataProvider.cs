using TaleWorlds.Core;

namespace SandBox.View.Map;

public class DefaultMapConversationDataProvider : IMapConversationDataProvider
{
	string IMapConversationDataProvider.GetAtmosphereNameFromData(MapConversationTableauData data)
	{
		string text = ((data.TimeOfDay <= 3f || data.TimeOfDay >= 21f) ? "night" : ((!(data.TimeOfDay > 8f) || !(data.TimeOfDay < 16f)) ? "sunset" : "noon"));
		if (data.Settlement == null || data.Settlement.IsHideout)
		{
			if (data.IsCurrentTerrainUnderSnow)
			{
				return "conv_snow_" + text + "_0";
			}
			return data.ConversationTerrainType switch
			{
				TerrainType.Desert => "conv_desert_" + text + "_0", 
				TerrainType.Steppe => "conv_steppe_" + text + "_0", 
				TerrainType.Forest => "conv_forest_" + text + "_0", 
				_ => "conv_plains_" + text + "_0", 
			};
		}
		string stringId = data.Settlement.Culture.StringId;
		bool isLocationInside;
		string locationNameFromLocationId = GetLocationNameFromLocationId(data.LocationId, out isLocationInside);
		if (locationNameFromLocationId != null)
		{
			if (isLocationInside)
			{
				return "conv_" + stringId + "_" + locationNameFromLocationId + "_0";
			}
			return "conv_" + stringId + "_" + locationNameFromLocationId + "_" + text + "_0";
		}
		return "conv_" + stringId + "_town_" + text + "_0";
	}

	private static string GetLocationNameFromLocationId(string locationId, out bool isLocationInside)
	{
		switch (locationId)
		{
		case "tavern":
			isLocationInside = true;
			return "tavern";
		case "lordshall":
			isLocationInside = true;
			return "lordshall";
		case "port":
			isLocationInside = false;
			return "shipyard";
		default:
			isLocationInside = false;
			return null;
		}
	}
}
