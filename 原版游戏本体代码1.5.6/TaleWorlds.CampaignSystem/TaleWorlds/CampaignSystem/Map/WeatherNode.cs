using TaleWorlds.CampaignSystem.ComponentInterfaces;

namespace TaleWorlds.CampaignSystem.Map;

public class WeatherNode
{
	public CampaignVec2 Position;

	public MapWeatherModel.WeatherEvent CurrentWeatherEvent;

	public bool IsVisuallyDirty { get; private set; }

	public WeatherNode(CampaignVec2 position)
	{
		Position = position;
		CurrentWeatherEvent = MapWeatherModel.WeatherEvent.Clear;
	}

	public void SetVisualDirty()
	{
		IsVisuallyDirty = true;
	}

	public void OnVisualUpdated()
	{
		IsVisuallyDirty = false;
	}
}
