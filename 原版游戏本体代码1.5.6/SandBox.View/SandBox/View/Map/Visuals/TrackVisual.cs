using SandBox.View.Map.Managers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace SandBox.View.Map.Visuals;

public class TrackVisual : MapEntityVisual<Track>
{
	private static TextObject _defaultTrackTitle = new TextObject("{=maptrack}Track");

	public override CampaignVec2 InteractionPositionForPlayer => base.MapEntity.Position;

	public override MapEntityVisual AttachedTo => null;

	public TrackVisual(Track track)
		: base(track)
	{
	}

	public override Vec3 GetVisualPosition()
	{
		return base.MapEntity.Position.AsVec3();
	}

	public override bool IsVisibleOrFadingOut()
	{
		return base.MapEntity.IsDetected;
	}

	public override void OnHover()
	{
		InformationManager.ShowTooltip(typeof(Track), base.MapEntity);
	}

	public override bool OnMapClick(bool followModifierUsed)
	{
		return false;
	}

	public override void OnOpenEncyclopedia()
	{
	}

	public override void ReleaseResources()
	{
		MapTracksVisualManager.Current.ReleaseResources(base.MapEntity);
	}
}
