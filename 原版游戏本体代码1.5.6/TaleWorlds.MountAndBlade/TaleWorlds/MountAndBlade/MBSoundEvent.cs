using System;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public static class MBSoundEvent
{
	public static bool PlaySound(int soundCodeId, in Vec3 position)
	{
		return MBAPI.IMBSoundEvent.PlaySound(soundCodeId, in position);
	}

	public static bool PlaySound(int soundCodeId, Vec3 position)
	{
		Vec3 position2 = position;
		return MBAPI.IMBSoundEvent.PlaySound(soundCodeId, in position2);
	}

	public static bool PlaySound(int soundCodeId, ref SoundEventParameter parameter, Vec3 position)
	{
		Vec3 position2 = position;
		return PlaySound(soundCodeId, ref parameter, in position2);
	}

	public static bool PlaySound(string soundPath, ref SoundEventParameter parameter, Vec3 position)
	{
		int eventIdFromString = SoundEvent.GetEventIdFromString(soundPath);
		Vec3 position2 = position;
		return PlaySound(eventIdFromString, ref parameter, in position2);
	}

	public static bool PlaySound(int soundCodeId, ref SoundEventParameter parameter, in Vec3 position)
	{
		return MBAPI.IMBSoundEvent.PlaySoundWithParam(soundCodeId, parameter, in position);
	}

	public static void PlayEventFromSoundBuffer(string eventId, byte[] soundData, Scene scene, bool is3d, bool isBlocking)
	{
		MBAPI.IMBSoundEvent.CreateEventFromSoundBuffer(eventId, soundData, (scene != null) ? scene.Pointer : UIntPtr.Zero, is3d, isBlocking);
	}

	public static void CreateEventFromExternalFile(string programmerEventName, string soundFilePath, Scene scene, bool is3d, bool isBlocking)
	{
		MBAPI.IMBSoundEvent.CreateEventFromExternalFile(programmerEventName, soundFilePath, (scene != null) ? scene.Pointer : UIntPtr.Zero, is3d, isBlocking);
	}
}
