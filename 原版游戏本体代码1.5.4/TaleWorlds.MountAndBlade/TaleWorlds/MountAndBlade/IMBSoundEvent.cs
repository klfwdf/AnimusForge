using System;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

[ScriptingInterfaceBase]
internal interface IMBSoundEvent
{
	[EngineMethod("create_event_from_external_file", false, null, false)]
	int CreateEventFromExternalFile(string programmerSoundEventName, string filePath, UIntPtr scene, bool is3d, bool isBlocking);

	[EngineMethod("create_event_from_sound_buffer", false, null, false)]
	int CreateEventFromSoundBuffer(string programmerSoundEventName, byte[] soundBuffer, UIntPtr scene, bool is3d, bool isBlocking);

	[EngineMethod("play_sound", false, null, false)]
	bool PlaySound(int fmodEventIndex, in Vec3 position);

	[EngineMethod("play_sound_with_int_param", false, null, false)]
	bool PlaySoundWithIntParam(int fmodEventIndex, int paramIndex, float paramVal, in Vec3 position);

	[EngineMethod("play_sound_with_str_param", false, null, false)]
	bool PlaySoundWithStrParam(int fmodEventIndex, string paramName, float paramVal, in Vec3 position);

	[EngineMethod("play_sound_with_param", false, null, false)]
	bool PlaySoundWithParam(int soundCodeId, SoundEventParameter parameter, in Vec3 position);
}
