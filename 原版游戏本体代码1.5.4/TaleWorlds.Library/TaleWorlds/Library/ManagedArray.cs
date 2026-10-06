using System;

namespace TaleWorlds.Library;

[Serializable]
public struct ManagedArray
{
	internal IntPtr Array;

	internal int Length;

	public ManagedArray(IntPtr array, int length)
	{
		Array = array;
		Length = length;
	}
}
