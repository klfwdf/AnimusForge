using System;

namespace psai.net;

public class PlaybackChannel
{
	private Logik m_logik;

	private int m_timeStampOfPlaybackStart;

	private int m_timeStampOfSnippetLoad;

	private bool m_playbackIsScheduled;

	private bool m_stoppedExplicitly;

	private IAudioPlaybackLayerChannel m_audioPlaybackLayerChannel;

	private float m_masterVolume = 1f;

	private float m_fadeOutVolume = 1f;

	private bool m_paused;

	private bool m_isMainChannel;

	internal Segment Segment { get; private set; }

	internal bool Paused
	{
		get
		{
			return m_paused;
		}
		set
		{
			m_paused = value;
			m_audioPlaybackLayerChannel.SetPaused(value);
		}
	}

	internal float MasterVolume
	{
		get
		{
			return m_masterVolume;
		}
		set
		{
			if (value >= 0f && value <= 1f)
			{
				m_masterVolume = value;
				UpdateVolume();
			}
		}
	}

	internal float FadeOutVolume
	{
		get
		{
			return m_fadeOutVolume;
		}
		set
		{
			if (value >= 0f && value <= 1f)
			{
				m_fadeOutVolume = value;
				UpdateVolume();
			}
		}
	}

	internal PlaybackChannel(Logik logik, bool isMainChannel)
	{
		m_audioPlaybackLayerChannel = new AudioPlaybackLayerChannelStandalone();
		m_logik = logik;
		m_isMainChannel = isMainChannel;
	}

	internal void Release()
	{
		m_audioPlaybackLayerChannel.Release();
	}

	internal ChannelState GetCurrentChannelState()
	{
		if (Segment == null || m_stoppedExplicitly)
		{
			return ChannelState.stopped;
		}
		float num = GetCountdownToPlaybackInMilliseconds();
		if (m_playbackIsScheduled && num > 0f)
		{
			return ChannelState.load;
		}
		if (num * -1f > (float)Segment.audioData.GetFullLengthInMilliseconds())
		{
			return ChannelState.stopped;
		}
		return ChannelState.playing;
	}

	internal bool IsPlaying()
	{
		return GetCurrentChannelState() == ChannelState.playing;
	}

	internal void LoadSegment(Segment snippet)
	{
		Segment = snippet;
		m_timeStampOfSnippetLoad = Logik.GetTimestampMillisElapsedSinceInitialisation();
		m_playbackIsScheduled = false;
		m_stoppedExplicitly = false;
		if (m_audioPlaybackLayerChannel != null)
		{
			m_audioPlaybackLayerChannel.LoadSegment(snippet);
		}
	}

	internal bool CheckIfSegmentHadEnoughTimeToLoad()
	{
		return GetMillisecondsSinceSegmentLoad() >= Logik.s_audioLayerMaximumLatencyForBufferingSounds;
	}

	internal int GetMillisecondsSinceSegmentLoad()
	{
		return Logik.GetTimestampMillisElapsedSinceInitialisation() - m_timeStampOfSnippetLoad;
	}

	internal int GetMillisecondsUntilLoadingWillHaveFinished()
	{
		return Math.Max(0, Logik.s_audioLayerMaximumLatencyForBufferingSounds - GetMillisecondsSinceSegmentLoad());
	}

	internal void StopChannel()
	{
		m_stoppedExplicitly = true;
		if (m_audioPlaybackLayerChannel != null)
		{
			m_audioPlaybackLayerChannel.StopChannel();
		}
	}

	internal void ReleaseSegment()
	{
		Segment = null;
		if (m_audioPlaybackLayerChannel != null)
		{
			m_audioPlaybackLayerChannel.ReleaseSegment();
		}
	}

	internal int GetCountdownToPlaybackInMilliseconds()
	{
		return m_timeStampOfPlaybackStart - Logik.GetTimestampMillisElapsedSinceInitialisation();
	}

	internal void ScheduleSegmentPlayback(Segment snippet, int delayInMilliseconds)
	{
		if (delayInMilliseconds < 0)
		{
			delayInMilliseconds = 0;
		}
		if (snippet != Segment)
		{
			LoadSegment(snippet);
		}
		m_stoppedExplicitly = false;
		m_playbackIsScheduled = true;
		m_timeStampOfPlaybackStart = Logik.GetTimestampMillisElapsedSinceInitialisation() + delayInMilliseconds;
		if (m_audioPlaybackLayerChannel != null)
		{
			m_audioPlaybackLayerChannel.ScheduleSegmentPlayback(snippet, delayInMilliseconds);
		}
	}

	private void UpdateVolume()
	{
		if (m_audioPlaybackLayerChannel != null)
		{
			float volume = MasterVolume * FadeOutVolume;
			m_audioPlaybackLayerChannel.SetVolume(volume);
		}
	}

	public void OnPlaybackHasStarted()
	{
		m_timeStampOfPlaybackStart = Logik.GetTimestampMillisElapsedSinceInitialisation();
		if (m_isMainChannel)
		{
			m_logik.SetSegmentEndApproachingAndReachedTimersAfterPlaybackHasStarted(0);
		}
	}
}
