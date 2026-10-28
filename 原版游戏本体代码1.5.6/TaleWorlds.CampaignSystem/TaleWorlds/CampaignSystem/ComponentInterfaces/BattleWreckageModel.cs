using TaleWorlds.CampaignSystem.BattleWreckages;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces;

public abstract class BattleWreckageModel : MBGameModel<BattleWreckageModel>
{
	public abstract bool CanPlayerInteractWithWreckage(out TextObject explanation);

	public abstract int GetMaxWreckageCountForMapEventType(MapEvent mapEvent);

	public abstract BattleWreckage.WreckageType GetWreckageTypeForMapEvent(MapEvent mapEvent);

	public abstract int GetWreckageCreationBattleSizeThreshold(MapEvent mapEvent);
}
