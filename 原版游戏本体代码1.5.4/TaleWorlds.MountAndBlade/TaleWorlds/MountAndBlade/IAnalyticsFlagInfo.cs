using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.Objects;

namespace TaleWorlds.MountAndBlade;

public interface IAnalyticsFlagInfo : IMissionBehavior
{
	MBReadOnlyList<FlagCapturePoint> AllCapturePoints { get; }

	Team GetFlagOwnerTeam(FlagCapturePoint flag);
}
