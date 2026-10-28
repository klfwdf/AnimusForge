using System.Runtime.InteropServices;

namespace System;

[StructLayout(LayoutKind.Sequential)]
internal sealed class Pinnable<T>
{
	public T Data;
}
