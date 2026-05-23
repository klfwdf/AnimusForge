using System.Collections.Generic;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors;

public interface ITradeRumorCampaignBehavior : ICampaignBehavior
{
	IEnumerable<TradeRumor> TradeRumors { get; }
}
