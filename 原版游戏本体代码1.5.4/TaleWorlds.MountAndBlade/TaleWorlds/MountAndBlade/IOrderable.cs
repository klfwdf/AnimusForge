using TaleWorlds.Core;

namespace TaleWorlds.MountAndBlade;

public interface IOrderable
{
	OrderType GetOrder(BattleSideEnum side);
}
