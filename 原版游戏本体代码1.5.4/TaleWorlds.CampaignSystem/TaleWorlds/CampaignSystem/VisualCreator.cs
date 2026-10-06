using TaleWorlds.CampaignSystem.MapEvents;

namespace TaleWorlds.CampaignSystem;

public class VisualCreator
{
	public IMapEventVisualCreator MapEventVisualCreator { get; set; }

	public IMapEventVisual CreateMapEventVisual(MapEvent mapEvent)
	{
		return MapEventVisualCreator?.CreateMapEventVisual(mapEvent);
	}
}
