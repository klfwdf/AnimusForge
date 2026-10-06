using System.Runtime.InteropServices;
using TaleWorlds.DotNet;

namespace TaleWorlds.Engine;

[EngineStruct("Managed_sound_event_parameter", false, null)]
public struct SoundEventParameter
{
	[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
	[CustomEngineStructMemberData("str_id")]
	internal string ParamName;

	internal float Value;

	public SoundEventParameter(string paramName, float value)
	{
		ParamName = paramName;
		Value = value;
	}

	public void Update(string paramName, float value)
	{
		ParamName = paramName;
		Value = value;
	}
}
