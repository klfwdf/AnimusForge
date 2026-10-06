namespace SandBox.GauntletUI.Map;

public interface IGauntletMapEventVisualHandler
{
	void OnNewEventStarted(GauntletMapEventVisual newEvent);

	void OnInitialized(GauntletMapEventVisual newEvent);

	void OnEventEnded(GauntletMapEventVisual newEvent);

	void OnEventVisibilityChanged(GauntletMapEventVisual visibilityChangedEvent);
}
