using System;

namespace TaleWorlds.Library;

[Serializable]
public struct ManagedArray(IntPtr array, int length)
{
	internal IntPtr Array = array;

	internal int Length = length;
}
