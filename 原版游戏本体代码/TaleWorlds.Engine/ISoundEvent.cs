using System;
using TaleWorlds.Library;

namespace TaleWorlds.Engine
{
	// Token: 0x02000041 RID: 65
	[ApplicationInterfaceBase]
	internal interface ISoundEvent
	{
		// Token: 0x06000694 RID: 1684
		[EngineMethod("create_event_from_string", false, null, false)]
		int CreateEventFromString(string eventName, UIntPtr scene);

		// Token: 0x06000695 RID: 1685
		[EngineMethod("get_event_id_from_string", false, null, false)]
		int GetEventIdFromString(string eventName);

		// Token: 0x06000696 RID: 1686
		[EngineMethod("play_sound_2d", false, null, false)]
		bool PlaySound2D(int fmodEventIndex);

		// Token: 0x06000697 RID: 1687
		[EngineMethod("get_total_event_count", false, null, false)]
		int GetTotalEventCount();

		// Token: 0x06000698 RID: 1688
		[EngineMethod("set_event_min_max_distance", false, null, false)]
		void SetEventMinMaxDistance(int fmodEventIndex, Vec3 radius);

		// Token: 0x06000699 RID: 1689
		[EngineMethod("create_event", false, null, false)]
		int CreateEvent(int fmodEventIndex, UIntPtr scene);

		// Token: 0x0600069A RID: 1690
		[EngineMethod("release_event", false, null, false)]
		void ReleaseEvent(int eventId);

		// Token: 0x0600069B RID: 1691
		[EngineMethod("set_event_parameter_from_string", false, null, false)]
		void SetEventParameterFromString(int eventId, string name, float value);

		// Token: 0x0600069C RID: 1692
		[EngineMethod("get_event_min_max_distance", false, null, false)]
		Vec3 GetEventMinMaxDistance(int eventId);

		// Token: 0x0600069D RID: 1693
		[EngineMethod("set_event_position", false, null, true)]
		void SetEventPosition(int eventId, ref Vec3 position);

		// Token: 0x0600069E RID: 1694
		[EngineMethod("set_event_velocity", false, null, false)]
		void SetEventVelocity(int eventId, ref Vec3 velocity);

		// Token: 0x0600069F RID: 1695
		[EngineMethod("start_event", false, null, false)]
		bool StartEvent(int eventId);

		// Token: 0x060006A0 RID: 1696
		[EngineMethod("start_event_in_position", false, null, false)]
		bool StartEventInPosition(int eventId, ref Vec3 position);

		// Token: 0x060006A1 RID: 1697
		[EngineMethod("stop_event", false, null, false)]
		void StopEvent(int eventId);

		// Token: 0x060006A2 RID: 1698
		[EngineMethod("pause_event", false, null, false)]
		void PauseEvent(int eventId);

		// Token: 0x060006A3 RID: 1699
		[EngineMethod("resume_event", false, null, false)]
		void ResumeEvent(int eventId);

		// Token: 0x060006A4 RID: 1700
		[EngineMethod("play_extra_event", false, null, false)]
		void PlayExtraEvent(int soundId, string eventName);

		// Token: 0x060006A5 RID: 1701
		[EngineMethod("set_switch", false, null, false)]
		void SetSwitch(int soundId, string switchGroupName, string newSwitchStateName);

		// Token: 0x060006A6 RID: 1702
		[EngineMethod("trigger_cue", false, null, false)]
		void TriggerCue(int eventId);

		// Token: 0x060006A7 RID: 1703
		[EngineMethod("set_event_parameter_at_index", false, null, false)]
		void SetEventParameterAtIndex(int soundId, int parameterIndex, float value);

		// Token: 0x060006A8 RID: 1704
		[EngineMethod("is_playing", false, null, false)]
		bool IsPlaying(int eventId);

		// Token: 0x060006A9 RID: 1705
		[EngineMethod("is_paused", false, null, false)]
		bool IsPaused(int eventId);

		// Token: 0x060006AA RID: 1706
		[EngineMethod("is_valid", false, null, true)]
		bool IsValid(int eventId);

		// Token: 0x060006AB RID: 1707
		[EngineMethod("create_event_from_external_file", false, null, false)]
		int CreateEventFromExternalFile(string programmerSoundEventName, string filePath, UIntPtr scene, bool is3d, bool isBlocking);

		// Token: 0x060006AC RID: 1708
		[EngineMethod("create_event_from_sound_buffer", false, null, false)]
		int CreateEventFromSoundBuffer(string programmerSoundEventName, byte[] soundBuffer, UIntPtr scene, bool is3d, bool isBlocking);
	}
}
