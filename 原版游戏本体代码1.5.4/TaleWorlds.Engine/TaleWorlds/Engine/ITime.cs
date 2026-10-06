using TaleWorlds.Library;

namespace TaleWorlds.Engine;

[ApplicationInterfaceBase]
internal interface ITime
{
	[EngineMethod("get_application_time", false, null, false)]
	float GetApplicationTime();
}
