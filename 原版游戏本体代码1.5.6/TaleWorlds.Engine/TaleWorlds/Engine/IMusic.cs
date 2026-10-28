using TaleWorlds.Library;

namespace TaleWorlds.Engine;

[ApplicationInterfaceBase]
internal interface IMusic
{
	[EngineMethod("get_free_music_channel_index", false, null, false)]
	int GetFreeMusicChannelIndex();

	[EngineMethod("load_clip", false, null, false)]
	void LoadClip(int index, string pathToClip);

	[EngineMethod("unload_clip", false, null, false)]
	void UnloadClip(int index);

	[EngineMethod("is_clip_loaded", false, null, false)]
	bool IsClipLoaded(int index);

	[EngineMethod("play_music", false, null, false)]
	void PlayMusic(int index);

	[EngineMethod("play_delayed", false, null, false)]
	void PlayDelayed(int index, int delayMilliseconds);

	[EngineMethod("is_music_playing", false, null, false)]
	bool IsMusicPlaying(int index);

	[EngineMethod("pause_music", false, null, false)]
	void PauseMusic(int index);

	[EngineMethod("stop_music", false, null, false)]
	void StopMusic(int index);

	[EngineMethod("set_volume", false, null, false)]
	void SetVolume(int index, float volume);
}
