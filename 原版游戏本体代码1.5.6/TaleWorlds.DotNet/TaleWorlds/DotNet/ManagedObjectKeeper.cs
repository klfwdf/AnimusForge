using System.Runtime.InteropServices;

namespace TaleWorlds.DotNet;

internal class ManagedObjectKeeper
{
	public int TimerToReleaseStrongRef;

	public GCHandle gcHandle;
}
