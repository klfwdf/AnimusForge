using System.Runtime.InteropServices;

namespace TaleWorlds.DotNet;

internal class NativeObjectKeeper
{
	public int TimerToReleaseStrongRef;

	public GCHandle gcHandle;
}
