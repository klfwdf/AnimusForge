using System;
using System.Runtime.InteropServices;
using TaleWorlds.DotNet;
using TaleWorlds.Library;

namespace TaleWorlds.Engine;

[EngineStruct("rglScript_component_field_holder", false, null)]
public struct ScriptComponentFieldHolder
{
	public MatrixFrame matrixFrame;

	public Vec3 color;

	public Vec3 v3;

	public UIntPtr entityPointer;

	public UIntPtr texturePointer;

	public UIntPtr meshPointer;

	public UIntPtr materialPointer;

	[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
	public string s;

	[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
	public string enumValue;

	public double d;

	public float f;

	public int b;

	public int i;
}
