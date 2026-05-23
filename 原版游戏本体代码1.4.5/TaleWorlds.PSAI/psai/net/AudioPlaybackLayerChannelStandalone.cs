using System.IO;
using TaleWorlds.Engine;
using TaleWorlds.ModuleManager;

namespace psai.net;

public class AudioPlaybackLayerChannelStandalone : IAudioPlaybackLayerChannel
{
	private AudioData _audioData;

	private int index;

	public AudioPlaybackLayerChannelStandalone()
	{
		index = Music.GetFreeMusicChannelIndex();
	}

	~AudioPlaybackLayerChannelStandalone()
	{
	}

	public void Release()
	{
	}

	internal void StopIfPlaying()
	{
		Music.StopMusic(index);
	}

	public PsaiResult LoadSegment(Segment segment)
	{
		_audioData = segment.audioData;
		string pathToClip = System.IO.Path.Combine(ModuleHelper.GetModuleFullPath(_audioData.moduleId) + "Music/", _audioData.filePathRelativeToProjectDir);
		Music.LoadClip(index, pathToClip);
		return PsaiResult.OK;
	}

	public PsaiResult ReleaseSegment()
	{
		Music.UnloadClip(index);
		return PsaiResult.OK;
	}

	public PsaiResult ScheduleSegmentPlayback(Segment snippet, int delayMilliseconds)
	{
		Music.PlayDelayed(index, delayMilliseconds);
		return PsaiResult.OK;
	}

	public PsaiResult StopChannel()
	{
		StopIfPlaying();
		return PsaiResult.OK;
	}

	public PsaiResult SetVolume(float volume)
	{
		Music.SetVolume(index, volume);
		return PsaiResult.OK;
	}

	public PsaiResult SetPaused(bool paused)
	{
		if (paused)
		{
			Music.PauseMusic(index);
		}
		else
		{
			Music.PlayMusic(index);
		}
		return PsaiResult.OK;
	}
}
