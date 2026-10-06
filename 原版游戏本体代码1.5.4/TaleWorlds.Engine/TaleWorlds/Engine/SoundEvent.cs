using System;
using TaleWorlds.Library;

namespace TaleWorlds.Engine;

public class SoundEvent
{
	private const int NullSoundId = -1;

	private static readonly SoundEvent NullSoundEvent = new SoundEvent(-1);

	private int _soundId;

	public bool IsValid
	{
		get
		{
			if (_soundId != -1)
			{
				return EngineApplicationInterface.ISoundEvent.IsValid(_soundId);
			}
			return false;
		}
	}

	public int GetSoundId()
	{
		return _soundId;
	}

	private SoundEvent(int soundId)
	{
		_soundId = soundId;
	}

	public static SoundEvent CreateEventFromString(string eventId, Scene scene)
	{
		UIntPtr scene2 = ((scene == null) ? UIntPtr.Zero : scene.Pointer);
		return new SoundEvent(EngineApplicationInterface.ISoundEvent.CreateEventFromString(eventId, scene2));
	}

	public void SetEventMinMaxDistance(Vec3 newRadius)
	{
		EngineApplicationInterface.ISoundEvent.SetEventMinMaxDistance(_soundId, newRadius);
	}

	public static int GetEventIdFromString(string name)
	{
		return EngineApplicationInterface.ISoundEvent.GetEventIdFromString(name);
	}

	public static bool PlaySound2D(int soundCodeId)
	{
		return EngineApplicationInterface.ISoundEvent.PlaySound2D(soundCodeId);
	}

	public static bool PlaySound2D(string soundName)
	{
		return PlaySound2D(GetEventIdFromString(soundName));
	}

	public static int GetTotalEventCount()
	{
		return EngineApplicationInterface.ISoundEvent.GetTotalEventCount();
	}

	public static SoundEvent CreateEvent(int soundCodeId, Scene scene)
	{
		return new SoundEvent(EngineApplicationInterface.ISoundEvent.CreateEvent(soundCodeId, scene.Pointer));
	}

	public bool IsNullSoundEvent()
	{
		return this == NullSoundEvent;
	}

	public bool Play()
	{
		return EngineApplicationInterface.ISoundEvent.StartEvent(_soundId);
	}

	public void Pause()
	{
		EngineApplicationInterface.ISoundEvent.PauseEvent(_soundId);
	}

	public void Resume()
	{
		EngineApplicationInterface.ISoundEvent.ResumeEvent(_soundId);
	}

	public void PlayExtraEvent(string eventName)
	{
		EngineApplicationInterface.ISoundEvent.PlayExtraEvent(_soundId, eventName);
	}

	public void SetSwitch(string switchGroupName, string newSwitchStateName)
	{
		EngineApplicationInterface.ISoundEvent.SetSwitch(_soundId, switchGroupName, newSwitchStateName);
	}

	public void TriggerCue()
	{
		EngineApplicationInterface.ISoundEvent.TriggerCue(_soundId);
	}

	public bool PlayInPosition(Vec3 position)
	{
		return EngineApplicationInterface.ISoundEvent.StartEventInPosition(_soundId, ref position);
	}

	public void Stop()
	{
		if (IsValid)
		{
			EngineApplicationInterface.ISoundEvent.StopEvent(_soundId);
			_soundId = -1;
		}
	}

	public void SetParameter(string parameterName, float value)
	{
		EngineApplicationInterface.ISoundEvent.SetEventParameterFromString(_soundId, parameterName, value);
	}

	public void SetParameter(int parameterIndex, float value)
	{
		EngineApplicationInterface.ISoundEvent.SetEventParameterAtIndex(_soundId, parameterIndex, value);
	}

	public Vec3 GetEventMinMaxDistance()
	{
		return EngineApplicationInterface.ISoundEvent.GetEventMinMaxDistance(_soundId);
	}

	public void SetPosition(Vec3 vec)
	{
		if (IsValid)
		{
			EngineApplicationInterface.ISoundEvent.SetEventPosition(_soundId, ref vec);
		}
	}

	public void SetVelocity(Vec3 vec)
	{
		if (IsValid)
		{
			EngineApplicationInterface.ISoundEvent.SetEventVelocity(_soundId, ref vec);
		}
	}

	public void Release()
	{
		MBDebug.Print("Release Sound Event " + _soundId, 0, Debug.DebugColor.Red);
		if (IsValid)
		{
			if (IsPlaying())
			{
				Stop();
			}
			EngineApplicationInterface.ISoundEvent.ReleaseEvent(_soundId);
		}
	}

	public bool IsPlaying()
	{
		return EngineApplicationInterface.ISoundEvent.IsPlaying(_soundId);
	}

	public bool IsPaused()
	{
		return EngineApplicationInterface.ISoundEvent.IsPaused(_soundId);
	}

	public bool IsStopped()
	{
		return EngineApplicationInterface.ISoundEvent.IsStopped(_soundId);
	}

	public static SoundEvent CreateEventFromSoundBuffer(string eventId, byte[] soundData, Scene scene, bool is3d, bool isBlocking)
	{
		return new SoundEvent(EngineApplicationInterface.ISoundEvent.CreateEventFromSoundBuffer(eventId, soundData, (scene != null) ? scene.Pointer : UIntPtr.Zero, is3d, isBlocking));
	}

	public static SoundEvent CreateEventFromExternalFile(string programmerEventName, string soundFilePath, Scene scene, bool is3d, bool isBlocking)
	{
		return new SoundEvent(EngineApplicationInterface.ISoundEvent.CreateEventFromExternalFile(programmerEventName, soundFilePath, scene?.Pointer ?? UIntPtr.Zero, is3d, isBlocking));
	}
}
