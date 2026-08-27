using AnimusForge.ExpeditionParade.Configuration;
using AnimusForge.ExpeditionParade.Core;
using AnimusForge.ExpeditionParade.Runtime;
using TaleWorlds.CampaignSystem;

namespace AnimusForge.ExpeditionParade.Campaign;

internal sealed class ParadeCampaignBehavior : CampaignBehaviorBase
{
	public override void RegisterEvents()
	{
		ParadeOperationResult result = ParadeFrameworkRuntime.Initialize(new ParadeSettings());
		Logger.Log("ExpeditionParade", "Framework registration: " + result);
	}

	public override void SyncData(IDataStore dataStore)
	{
		// The framework deliberately owns no persistent campaign state.
	}
}
