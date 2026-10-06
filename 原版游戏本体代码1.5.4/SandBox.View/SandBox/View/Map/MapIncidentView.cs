using TaleWorlds.CampaignSystem.Incidents;

namespace SandBox.View.Map;

public class MapIncidentView : MapView
{
	public readonly Incident Incident;

	public MapIncidentView()
	{
	}

	public MapIncidentView(Incident incident)
	{
		Incident = incident;
	}
}
