using System.Runtime.InteropServices;
using TaleWorlds.DotNet;

namespace TaleWorlds.Engine;

[EngineStruct("Managed_sound_event_parameter", false, null)]
public struct SoundEventParameter(string paramName, float value)
{
	[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
	[CustomEngineStructMemberData("str_id")]
	internal string ParamName = paramName;

	internal float Value = value;

	public void Update(string paramName, float value)
	{
		ParamName = paramName;
		Value = value;
	}
}
