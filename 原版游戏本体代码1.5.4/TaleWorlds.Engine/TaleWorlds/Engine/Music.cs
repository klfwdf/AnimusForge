namespace TaleWorlds.Engine;

public class Music
{
	public static int GetFreeMusicChannelIndex()
	{
		return EngineApplicationInterface.IMusic.GetFreeMusicChannelIndex();
	}

	public static void LoadClip(int index, string pathToClip)
	{
		EngineApplicationInterface.IMusic.LoadClip(index, pathToClip);
	}

	public static void UnloadClip(int index)
	{
		EngineApplicationInterface.IMusic.UnloadClip(index);
	}

	public static bool IsClipLoaded(int index)
	{
		return EngineApplicationInterface.IMusic.IsClipLoaded(index);
	}

	public static void PlayMusic(int index)
	{
		EngineApplicationInterface.IMusic.PlayMusic(index);
	}

	public static void PlayDelayed(int index, int deltaMilliseconds)
	{
		EngineApplicationInterface.IMusic.PlayDelayed(index, deltaMilliseconds);
	}

	public static bool IsMusicPlaying(int index)
	{
		return EngineApplicationInterface.IMusic.IsMusicPlaying(index);
	}

	public static void PauseMusic(int index)
	{
		EngineApplicationInterface.IMusic.PauseMusic(index);
	}

	public static void StopMusic(int index)
	{
		EngineApplicationInterface.IMusic.StopMusic(index);
	}

	public static void SetVolume(int index, float volume)
	{
		EngineApplicationInterface.IMusic.SetVolume(index, volume);
	}
}
