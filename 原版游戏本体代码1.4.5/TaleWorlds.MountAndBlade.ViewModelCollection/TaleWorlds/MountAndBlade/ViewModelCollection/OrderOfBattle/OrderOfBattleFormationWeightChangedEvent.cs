using TaleWorlds.Library.EventSystem;

namespace TaleWorlds.MountAndBlade.ViewModelCollection.OrderOfBattle;

public class OrderOfBattleFormationWeightChangedEvent : EventBase
{
	public Formation Formation { get; private set; }

	public OrderOfBattleFormationWeightChangedEvent(Formation formation)
	{
		Formation = formation;
	}
}
