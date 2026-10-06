using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces;

public abstract class TavernMercenaryTroopsModel : MBGameModel<TavernMercenaryTroopsModel>
{
	public abstract float RegularMercenariesSpawnChance { get; }
}
