using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.Core;

public interface ITrackableBase
{
	TextObject GetName();

	Vec3 GetPosition();
}
