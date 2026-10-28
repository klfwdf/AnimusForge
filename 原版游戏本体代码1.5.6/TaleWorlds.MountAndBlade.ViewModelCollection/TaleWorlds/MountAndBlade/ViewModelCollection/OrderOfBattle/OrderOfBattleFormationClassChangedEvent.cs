using TaleWorlds.Library.EventSystem;

namespace TaleWorlds.MountAndBlade.ViewModelCollection.OrderOfBattle;

public class OrderOfBattleFormationClassChangedEvent : EventBase
{
	public Formation Formation { get; private set; }

	public OrderOfBattleFormationClassChangedEvent(Formation formation)
	{
		Formation = formation;
	}
}
