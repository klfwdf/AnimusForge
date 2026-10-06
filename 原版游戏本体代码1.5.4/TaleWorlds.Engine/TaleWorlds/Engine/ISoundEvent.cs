using System;
using TaleWorlds.Library;

namespace TaleWorlds.Engine;

[ApplicationInterfaceBase]
internal interface ISoundEvent
{
	[EngineMethod("create_event_from_string", false, null, false)]
	int CreateEventFromString(string eventName, UIntPtr scene);

	[EngineMethod("get_event_id_from_string", false, null, false)]
	int GetEventIdFromString(string eventName);

	[EngineMethod("play_sound_2d", false, null, false)]
	bool PlaySound2D(int fmodEventIndex);

	[EngineMethod("get_total_event_count", false, null, false)]
	int GetTotalEventCount();

	[EngineMethod("set_event_min_max_distance", false, null, false)]
	void SetEventMinMaxDistance(int fmodEventIndex, Vec3 radius);

	[EngineMethod("create_event", false, null, false)]
	int CreateEvent(int fmodEventIndex, UIntPtr scene);

	[EngineMethod("release_event", false, null, false)]
	void ReleaseEvent(int eventId);

	[EngineMethod("set_event_parameter_from_string", false, null, false)]
	void SetEventParameterFromString(int eventId, string name, float value);

	[EngineMethod("get_event_min_max_distance", false, null, false)]
	Vec3 GetEventMinMaxDistance(int eventId);

	[EngineMethod("set_event_position", false, null, true)]
	void SetEventPosition(int eventId, ref Vec3 position);

	[EngineMethod("set_event_velocity", false, null, false)]
	void SetEventVelocity(int eventId, ref Vec3 velocity);

	[EngineMethod("start_event", false, null, false)]
	bool StartEvent(int eventId);

	[EngineMethod("start_event_in_position", false, null, false)]
	bool StartEventInPosition(int eventId, ref Vec3 position);

	[EngineMethod("stop_event", false, null, false)]
	void StopEvent(int eventId);

	[EngineMethod("pause_event", false, null, false)]
	void PauseEvent(int eventId);

	[EngineMethod("resume_event", false, null, false)]
	void ResumeEvent(int eventId);

	[EngineMethod("play_extra_event", false, null, false)]
	void PlayExtraEvent(int soundId, string eventName);

	[EngineMethod("set_switch", false, null, false)]
	void SetSwitch(int soundId, string switchGroupName, string newSwitchStateName);

	[EngineMethod("trigger_cue", false, null, false)]
	void TriggerCue(int eventId);

	[EngineMethod("set_event_parameter_at_index", false, null, false)]
	void SetEventParameterAtIndex(int soundId, int parameterIndex, float value);

	[EngineMethod("is_playing", false, null, false)]
	bool IsPlaying(int eventId);

	[EngineMethod("is_stopped", false, null, false)]
	bool IsStopped(int eventId);

	[EngineMethod("is_paused", false, null, false)]
	bool IsPaused(int eventId);

	[EngineMethod("is_valid", false, null, true)]
	bool IsValid(int eventId);

	[EngineMethod("create_event_from_external_file", false, null, false)]
	int CreateEventFromExternalFile(string programmerSoundEventName, string filePath, UIntPtr scene, bool is3d, bool isBlocking);

	[EngineMethod("create_event_from_sound_buffer", false, null, false)]
	int CreateEventFromSoundBuffer(string programmerSoundEventName, byte[] soundBuffer, UIntPtr scene, bool is3d, bool isBlocking);
}
