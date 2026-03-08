using System;

namespace SandBox.GauntletUI.Map
{
	// Token: 0x02000037 RID: 55
	public interface IGauntletMapEventVisualHandler
	{
		// Token: 0x06000295 RID: 661
		void OnNewEventStarted(GauntletMapEventVisual newEvent);

		// Token: 0x06000296 RID: 662
		void OnInitialized(GauntletMapEventVisual newEvent);

		// Token: 0x06000297 RID: 663
		void OnEventEnded(GauntletMapEventVisual newEvent);

		// Token: 0x06000298 RID: 664
		void OnEventVisibilityChanged(GauntletMapEventVisual visibilityChangedEvent);
	}
}
