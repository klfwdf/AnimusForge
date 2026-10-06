using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public interface IOrderableWithInteractionArea : IOrderable
{
	bool IsPointInsideInteractionArea(Vec3 point);
}
