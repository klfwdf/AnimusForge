using System.Collections.Generic;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces;

public abstract class SettlementMenuOverlayModel : MBGameModel<SettlementMenuOverlayModel>
{
	public abstract Dictionary<Hero, bool> GetOverlayHeroes();
}
