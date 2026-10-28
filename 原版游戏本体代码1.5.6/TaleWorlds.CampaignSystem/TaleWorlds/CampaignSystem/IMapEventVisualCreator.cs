using TaleWorlds.CampaignSystem.MapEvents;

namespace TaleWorlds.CampaignSystem;

public interface IMapEventVisualCreator
{
	IMapEventVisual CreateMapEventVisual(MapEvent mapEvent);
}
