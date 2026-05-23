using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.ModuleManager;
using psai.Editor;

namespace psai.net;

internal class Logik
{
	private const string PSAI_VERSION = ".NET 1.7.3";

	private const string m_fullVersionString = "psai Version .NET 1.7.3";

	internal const float COMPATIBILITY_PERCENTAGE_SAME_GROUP = 1f;

	internal const float COMPATIBILITY_PERCENTAGE_OTHER_GROUP = 0.5f;

	internal const int PSAI_CHANNEL_COUNT = 9;

	internal const int PSAI_CHANNEL_COUNT_HIGHLIGHTS = 2;

	internal const int PSAI_FADING_UPDATE_INVERVAL_MILLIS = 50;

	internal const int PSAI_FADEOUTMILLIS_PLAYIMMEDIATELY = 500;

	internal const int PSAI_FADEOUTMILLIS_STOPMUSIC = 1000;

	internal const int PSAI_FADEOUTMILLIS_HIGHLIGHT_INTERRUPTED = 2000;

	internal const int SNIPPET_TYPE_MIDDLE_OR_BRIDGE = 10;

	private static Random s_random;

	internal Soundtrack m_soundtrack;

	private List<FadeData> m_fadeVoices;

	private int m_currentVoiceNumber;

	private int m_targetVoice;

	private IPlatformLayer m_platformLayer;

	private bool m_initializationFailure;

	private static Stopwatch m_stopWatch;

	private Theme m_lastBasicMood;

	private int m_hilightVoiceIndex;

	private int m_lastRegularVoiceNumberReturned;

	private float m_psaiMasterVolume;

	private Segment m_currentSegmentPlaying;

	private int m_currentSnippetTypeRequested;

	private Theme m_effectiveTheme;

	private int m_timeStampCurrentSnippetPlaycall;

	private int m_estimatedTimestampOfTargetSnippetPlayback;

	private int m_timeStampOfLastIntensitySetForCurrentTheme;

	private int m_timeStampRestStart;

	private Segment m_targetSegment;

	private int m_targetSegmentSuitabilitiesRequested;

	private float m_currentIntensitySlope;

	private float m_lastIntensity;

	private bool m_holdIntensity;

	private float m_heldIntensity;

	private bool m_scheduleFadeoutUponSnippetPlayback;

	private float m_startOrRetriggerIntensityOfCurrentTheme;

	private int m_lastMusicDuration;

	private int m_remainingMusicDurationAtTimeOfHoldIntensity;

	private PsaiState m_psaiState;

	private PsaiState m_psaiStateIntended;

	private List<ThemeQueueEntry> m_themeQueue;

	private PsaiPlayMode m_psaiPlayMode;

	private PsaiPlayMode m_psaiPlayModeIntended;

	private bool m_returnToLastBasicMoodFlag;

	private PlaybackChannel[] m_playbackChannels = new PlaybackChannel[9];

	internal static int s_audioLayerMaximumLatencyForPlayingbackPrebufferedSounds;

	internal static int s_audioLayerMaximumLatencyForBufferingSounds;

	internal static int s_audioLayerMaximumLatencyForPlayingBackUnbufferedSounds;

	internal static int s_updateIntervalMillis;

	private PsaiTimer m_timerStartSnippetPlayback = new PsaiTimer();

	private PsaiTimer m_timerSegmentEndApproaching = new PsaiTimer();

	private PsaiTimer m_timerSegmentEndReached = new PsaiTimer();

	private PsaiTimer m_timerFades = new PsaiTimer();

	private PsaiTimer m_timerWakeUpFromRest = new PsaiTimer();

	private int m_timeStampOfLastFadeUpdate;

	private ThemeQueueEntry m_latestEndOfSegmentTriggerCall = new ThemeQueueEntry();

	private bool m_paused;

	private int m_timeStampPauseOn;

	private int m_restModeSecondsOverride;

	internal static Logik Instance { get; private set; }

	internal void Release()
	{
		for (int i = 0; i < m_playbackChannels.Length; i++)
		{
			m_playbackChannels[i].Release();
		}
		m_platformLayer.Release();
	}

	internal static int GetRandomInt(int min, int max)
	{
		return s_random.Next(min, max);
	}

	internal static float GetRandomFloat()
	{
		return (float)s_random.NextDouble();
	}

	private static void UpdateMaximumLatencyForPlayingBackUnbufferedSounds()
	{
		s_audioLayerMaximumLatencyForPlayingBackUnbufferedSounds = s_audioLayerMaximumLatencyForBufferingSounds + s_audioLayerMaximumLatencyForPlayingbackPrebufferedSounds;
	}

	internal PsaiResult SetMaximumLatencyNeededByPlatformToBufferSounddata(int latencyInMilliseconds)
	{
		if (latencyInMilliseconds >= 0)
		{
			s_audioLayerMaximumLatencyForBufferingSounds = latencyInMilliseconds;
			UpdateMaximumLatencyForPlayingBackUnbufferedSounds();
			return PsaiResult.OK;
		}
		return PsaiResult.invalidParam;
	}

	internal PsaiResult SetMaximumLatencyNeededByPlatformToPlayBackBufferedSounds(int latencyInMilliseconds)
	{
		if (latencyInMilliseconds >= 0)
		{
			s_audioLayerMaximumLatencyForBufferingSounds = latencyInMilliseconds;
			UpdateMaximumLatencyForPlayingBackUnbufferedSounds();
			return PsaiResult.OK;
		}
		return PsaiResult.invalidParam;
	}

	static Logik()
	{
		s_audioLayerMaximumLatencyForPlayingbackPrebufferedSounds = 50;
		s_audioLayerMaximumLatencyForBufferingSounds = 200;
		s_updateIntervalMillis = 100;
		m_stopWatch = new Stopwatch();
		s_random = new Random();
		m_stopWatch.Start();
		UpdateMaximumLatencyForPlayingBackUnbufferedSounds();
		Instance = new Logik();
	}

	private Logik()
	{
		m_platformLayer = new PlatformLayerStandalone(this);
		m_platformLayer.Initialize();
		m_soundtrack = new Soundtrack();
		m_themeQueue = new List<ThemeQueueEntry>();
		m_fadeVoices = new List<FadeData>();
		for (int i = 0; i < 9; i++)
		{
			bool isMainChannel = i < 7;
			m_playbackChannels[i] = new PlaybackChannel(this, isMainChannel);
		}
		m_hilightVoiceIndex = -1;
		m_lastRegularVoiceNumberReturned = -1;
		m_currentVoiceNumber = -1;
		m_targetVoice = -1;
		m_psaiMasterVolume = 1f;
		m_effectiveTheme = null;
		m_currentSegmentPlaying = null;
		m_currentSnippetTypeRequested = 0;
		m_targetSegment = null;
		m_targetSegmentSuitabilitiesRequested = 0;
		m_psaiState = PsaiState.notready;
		m_psaiStateIntended = PsaiState.notready;
		m_paused = false;
	}

	internal PsaiResult LoadSoundtrackFromProjectFile(List<string> pathToProjectFiles)
	{
		PsaiProject psaiProject = null;
		m_initializationFailure = false;
		foreach (string pathToProjectFile in pathToProjectFiles)
		{
			try
			{
				string text = ModuleHelper.GetModuleFullPath(pathToProjectFile) + "Music/soundtrack.xml";
				StreamReader streamReader = new StreamReader(text);
				PsaiProject psaiProject2 = null;
				if (streamReader == null)
				{
					TaleWorlds.Library.Debug.Print("Cannot find the music xml for the following path: " + text, 0, TaleWorlds.Library.Debug.DebugColor.Red, 281474976710656uL);
					continue;
				}
				psaiProject2 = PsaiProject.LoadProjectFromStream(streamReader, pathToProjectFile);
				if (psaiProject == null)
				{
					psaiProject = psaiProject2;
				}
				else
				{
					psaiProject.MergeProjects(psaiProject2);
				}
			}
			catch (Exception ex)
			{
				MBDebug.Print("\t Error Occurred: \n\t" + ex.Source + "\n\t" + ex.Message + "\n\t" + ex.StackTrace);
			}
		}
		psaiProject.ReconstructReferencesAfterXmlDeserialization();
		psaiProject.DebugCheckProjectIntegrity();
		if (psaiProject != null)
		{
			return LoadSoundtrackByPsaiProject(psaiProject);
		}
		return PsaiResult.error_file;
	}

	public PsaiResult LoadSoundtrackByPsaiProject(PsaiProject psaiProject)
	{
		m_soundtrack = psaiProject.BuildPsaiDotNetSoundtrackFromProject();
		InitMembersAfterSoundtrackHasLoaded();
		return PsaiResult.OK;
	}

	private void InitMembersAfterSoundtrackHasLoaded()
	{
		m_themeQueue.Clear();
		m_fadeVoices.Clear();
		foreach (Segment value in m_soundtrack.m_snippets.Values)
		{
			value.audioData.filePathRelativeToProjectDir = m_platformLayer.ConvertFilePathForPlatform(value.audioData.filePathRelativeToProjectDir);
		}
		m_soundtrack.UpdateMaxPreBeatMsOfCompatibleMiddleOrBridgeSnippets();
		m_lastBasicMood = m_soundtrack.getThemeById(GetLastBasicMoodId());
		m_psaiState = PsaiState.silence;
		m_psaiStateIntended = PsaiState.silence;
		m_psaiPlayMode = PsaiPlayMode.regular;
		m_psaiPlayModeIntended = PsaiPlayMode.regular;
		m_returnToLastBasicMoodFlag = false;
		m_holdIntensity = false;
		m_latestEndOfSegmentTriggerCall.themeId = -1;
		m_soundtrack.BuildAllIndirectionSequences();
	}

	internal int GetLastBasicMoodId()
	{
		if (m_lastBasicMood != null)
		{
			return m_lastBasicMood.id;
		}
		return -1;
	}

	public void SetLastBasicMood(int themeId)
	{
		Theme themeById = m_soundtrack.getThemeById(themeId);
		if (themeById != null)
		{
			SetThemeAsLastBasicMood(themeById);
		}
		else
		{
			m_lastBasicMood = null;
		}
	}

	internal static bool CheckIfFileExists(string filepath)
	{
		return File.Exists(filepath);
	}

	private int GetRemainingMusicDurationSecondsOfCurrentTheme()
	{
		int num = GetTimestampMillisElapsedSinceInitialisation() - m_timeStampOfLastIntensitySetForCurrentTheme;
		return m_lastMusicDuration - num / 1000;
	}

	internal static int GetTimestampMillisElapsedSinceInitialisation()
	{
		return (int)m_stopWatch.ElapsedMilliseconds;
	}

	private PsaiResult Readfile_ProtoBuf(Stream stream)
	{
		if (stream == null)
		{
			return PsaiResult.file_notFound;
		}
		m_soundtrack.Clear();
		m_themeQueue.Clear();
		return PsaiResult.error_file;
	}

	internal string getVersion()
	{
		return "psai Version .NET 1.7.3";
	}

	internal long GetCurrentSystemTimeMillis()
	{
		return GetTimestampMillisElapsedSinceInitialisation();
	}

	private void startFade(int voiceId, int fadeoutMillis, int timeOffsetMillis)
	{
		if (voiceId <= -1)
		{
			return;
		}
		float fadeOutVolume = m_playbackChannels[voiceId].FadeOutVolume;
		for (int i = 0; i < m_fadeVoices.Count; i++)
		{
			FadeData fadeData = m_fadeVoices[i];
			if (fadeData.voiceNumber == voiceId)
			{
				fadeData.delayMillis = 0;
				fadeData.fadeoutDeltaVolumePerUpdate = fadeOutVolume / ((float)fadeoutMillis / 50f);
				fadeData.currentVolume = fadeOutVolume;
				return;
			}
		}
		if (fadeOutVolume > 0f)
		{
			AddFadeData(voiceId, fadeoutMillis, fadeOutVolume, timeOffsetMillis);
			if (!m_timerFades.IsSet())
			{
				m_timeStampOfLastFadeUpdate = GetTimestampMillisElapsedSinceInitialisation();
				m_timerFades.SetTimer(50, 0);
			}
		}
	}

	private void AddFadeData(int voiceNumber, int fadeoutMillis, float currentVolume, int delayMillis)
	{
		FadeData fadeData = new FadeData();
		fadeData.voiceNumber = voiceNumber;
		fadeData.fadeoutDeltaVolumePerUpdate = currentVolume / ((float)fadeoutMillis / 50f);
		fadeData.currentVolume = currentVolume;
		fadeData.delayMillis = delayMillis;
		m_fadeVoices.Add(fadeData);
	}

	internal int getNextVoiceNumber(bool forHighlight)
	{
		int num = 0;
		if (!forHighlight)
		{
			num = m_lastRegularVoiceNumberReturned + 1;
			if (num >= 7)
			{
				num = 0;
			}
			m_lastRegularVoiceNumberReturned = num;
		}
		else
		{
			num = m_hilightVoiceIndex + 1;
			if (num == 0 || num == 9)
			{
				num = 7;
			}
		}
		return num;
	}

	private void PsaiErrorCheck(PsaiResult result, string infoAboutLastCall)
	{
	}

	private int GetMillisElapsedAfterCurrentSnippetPlaycall()
	{
		if (m_currentSegmentPlaying != null)
		{
			if (!m_paused)
			{
				return GetTimestampMillisElapsedSinceInitialisation() - m_timeStampCurrentSnippetPlaycall;
			}
			return m_timeStampPauseOn - m_timeStampCurrentSnippetPlaycall;
		}
		return 0;
	}

	internal PsaiResult setPaused(bool setPaused)
	{
		if ((setPaused && !m_paused) || (!setPaused && m_paused))
		{
			m_paused = setPaused;
			PlaybackChannel[] playbackChannels = m_playbackChannels;
			for (int i = 0; i < playbackChannels.Length; i++)
			{
				playbackChannels[i].Paused = setPaused;
			}
			m_timerStartSnippetPlayback.SetPaused(setPaused);
			m_timerSegmentEndApproaching.SetPaused(setPaused);
			m_timerSegmentEndReached.SetPaused(setPaused);
			m_timerWakeUpFromRest.SetPaused(setPaused);
			if (setPaused)
			{
				m_timeStampPauseOn = GetTimestampMillisElapsedSinceInitialisation();
				m_lastIntensity = getCurrentIntensity();
			}
			else
			{
				int num = GetTimestampMillisElapsedSinceInitialisation() - m_timeStampPauseOn;
				int num2 = m_timeStampPauseOn - m_timeStampCurrentSnippetPlaycall;
				m_timeStampCurrentSnippetPlaycall = GetTimestampMillisElapsedSinceInitialisation() - num2;
				m_timeStampOfLastIntensitySetForCurrentTheme += num;
				m_estimatedTimestampOfTargetSnippetPlayback += num;
			}
			return PsaiResult.OK;
		}
		return PsaiResult.commandIgnored;
	}

	internal PsaiResult Update()
	{
		if (!m_paused)
		{
			if (m_timerStartSnippetPlayback.ThresholdHasBeenReached())
			{
				m_timerStartSnippetPlayback.Stop();
				PlayTargetSegmentImmediately();
			}
			if (m_timerSegmentEndApproaching.ThresholdHasBeenReached())
			{
				m_timerSegmentEndApproaching.Stop();
				SegmentEndApproachingHandler();
			}
			if (m_timerSegmentEndReached.ThresholdHasBeenReached())
			{
				m_timerSegmentEndReached.Stop();
				SegmentEndReachedHandler();
			}
			if (m_timerWakeUpFromRest.ThresholdHasBeenReached())
			{
				m_timerWakeUpFromRest.Stop();
				WakeUpFromRestHandler();
			}
			if (m_timerFades.ThresholdHasBeenReached())
			{
				m_timerFades.Stop();
				updateFades();
			}
		}
		return PsaiResult.OK;
	}

	private void SetThemeAsLastBasicMood(Theme latestBasicMood)
	{
		if (latestBasicMood != null)
		{
			m_lastBasicMood = latestBasicMood;
		}
	}

	private bool CheckIfAnyThemeIsCurrentlyPlaying()
	{
		if (m_psaiState == PsaiState.playing && m_currentSegmentPlaying != null)
		{
			return m_effectiveTheme != null;
		}
		return false;
	}

	internal PsaiResult ReturnToLastBasicMood(bool immediately)
	{
		if (m_initializationFailure)
		{
			return PsaiResult.initialization_error;
		}
		if (m_lastBasicMood == null)
		{
			return PsaiResult.no_basicmood_set;
		}
		if (m_paused)
		{
			setPaused(setPaused: false);
		}
		if (m_psaiPlayModeIntended == PsaiPlayMode.regular)
		{
			switch (m_psaiState)
			{
			case PsaiState.playing:
				m_themeQueue.Clear();
				m_holdIntensity = false;
				m_latestEndOfSegmentTriggerCall.themeId = -1;
				if (m_currentSegmentPlaying != null && m_effectiveTheme.themeType != ThemeType.basicMood)
				{
					bool flag = false;
					if (!immediately)
					{
						flag = CheckIfThereIsAPathToEndSegmentForEffectiveSegmentAndLogWarningIfThereIsnt();
					}
					if (immediately || !flag)
					{
						PlayThemeNowOrAtEndOfCurrentSegmentAndStartEvaluation(GetLastBasicMoodId(), m_lastBasicMood.intensityAfterRest, m_lastBasicMood.musicDurationGeneral, immediately: true, holdIntensity: false);
					}
					else
					{
						m_psaiStateIntended = PsaiState.playing;
						m_returnToLastBasicMoodFlag = true;
					}
					return PsaiResult.OK;
				}
				return PsaiResult.commandIgnored;
			case PsaiState.silence:
			case PsaiState.rest:
				PlayThemeNowOrAtEndOfCurrentSegmentAndStartEvaluation(GetLastBasicMoodId(), m_lastBasicMood.intensityAfterRest, m_lastBasicMood.musicDurationGeneral, immediately: true, holdIntensity: false);
				return PsaiResult.OK;
			default:
				return PsaiResult.internal_error;
			}
		}
		if (m_psaiPlayModeIntended == PsaiPlayMode.menuMode)
		{
			return PsaiResult.commandIgnoredMenuModeActive;
		}
		if (m_psaiPlayModeIntended == PsaiPlayMode.cutScene)
		{
			return PsaiResult.commandIgnoredCutsceneActive;
		}
		return PsaiResult.internal_error;
	}

	internal int getUpcomingThemeId()
	{
		PsaiState psaiState = m_psaiState;
		if (psaiState == PsaiState.playing)
		{
			if (m_latestEndOfSegmentTriggerCall.themeId != -1)
			{
				return m_latestEndOfSegmentTriggerCall.themeId;
			}
			ThemeQueueEntry followingThemeQueueEntry = getFollowingThemeQueueEntry();
			if (followingThemeQueueEntry != null)
			{
				return followingThemeQueueEntry.themeId;
			}
		}
		return -1;
	}

	internal PsaiResult TriggerMusicTheme(int argThemeId, float argIntensity)
	{
		Theme themeById = m_soundtrack.getThemeById(argThemeId);
		if (themeById == null)
		{
			return PsaiResult.unknown_theme;
		}
		if (themeById.m_segments.Count == 0)
		{
			return PsaiResult.essential_segment_missing;
		}
		return TriggerMusicTheme(themeById, argIntensity, themeById.musicDurationGeneral);
	}

	internal PsaiResult TriggerMusicTheme(int argThemeId, float argIntensity, int argMusicDuration)
	{
		Theme themeById = m_soundtrack.getThemeById(argThemeId);
		if (themeById == null)
		{
			return PsaiResult.unknown_theme;
		}
		return TriggerMusicTheme(themeById, argIntensity, argMusicDuration);
	}

	internal PsaiResult TriggerMusicTheme(Theme argTheme, float argIntensity, int argMusicDuration)
	{
		if (m_initializationFailure)
		{
			return PsaiResult.initialization_error;
		}
		if (m_paused)
		{
			setPaused(setPaused: false);
		}
		if (argIntensity > 1f)
		{
			argIntensity = 1f;
		}
		else if (argIntensity < 0f)
		{
			argIntensity = 0f;
		}
		if (m_psaiPlayMode == PsaiPlayMode.menuMode)
		{
			return PsaiResult.commandIgnoredMenuModeActive;
		}
		if (m_psaiPlayModeIntended == PsaiPlayMode.cutScene)
		{
			return PsaiResult.commandIgnoredCutsceneActive;
		}
		if (m_psaiPlayMode == PsaiPlayMode.cutScene && m_psaiStateIntended == PsaiState.silence && m_currentSegmentPlaying != null)
		{
			m_psaiState = PsaiState.playing;
			m_psaiStateIntended = PsaiState.playing;
			return PlayThemeNowOrAtEndOfCurrentSegmentAndStartEvaluation(argTheme.id, argIntensity, argMusicDuration, immediately: true, holdIntensity: false);
		}
		Segment effectiveSegment = GetEffectiveSegment();
		if (argTheme.themeType == ThemeType.highlightLayer)
		{
			if (effectiveSegment != null && m_effectiveTheme != null && effectiveSegment != null && !effectiveSegment.CheckIfAtLeastOneDirectTransitionOrLayeringIsPossible(m_soundtrack, argTheme.id))
			{
				return PsaiResult.triggerDenied;
			}
			return startHighlight(argTheme);
		}
		if (m_returnToLastBasicMoodFlag && argTheme.themeType != ThemeType.basicMood)
		{
			m_returnToLastBasicMoodFlag = false;
		}
		if (argTheme.themeType == ThemeType.basicMood)
		{
			SetThemeAsLastBasicMood(argTheme);
		}
		if (effectiveSegment == null || m_psaiState == PsaiState.silence || m_psaiState == PsaiState.rest)
		{
			return PlayThemeNowOrAtEndOfCurrentSegmentAndStartEvaluation(argTheme.id, argIntensity, argMusicDuration, immediately: true, holdIntensity: false);
		}
		Theme themeById = m_soundtrack.getThemeById(effectiveSegment.ThemeId);
		if (m_psaiStateIntended == PsaiState.silence && effectiveSegment != null)
		{
			ThemeInterruptionBehavior themeInterruptionBehavior = Theme.GetThemeInterruptionBehavior(themeById.themeType, argTheme.themeType);
			if (themeInterruptionBehavior == ThemeInterruptionBehavior.at_end_of_current_snippet || themeInterruptionBehavior == ThemeInterruptionBehavior.never)
			{
				m_psaiStateIntended = PsaiState.playing;
				return PlayThemeNowOrAtEndOfCurrentSegmentAndStartEvaluation(argTheme.id, argIntensity, argMusicDuration, immediately: false, holdIntensity: false);
			}
		}
		if (effectiveSegment.ThemeId == argTheme.id)
		{
			m_latestEndOfSegmentTriggerCall.themeId = -1;
			SetCurrentIntensityAndMusicDuration(argIntensity, argMusicDuration, recalculateIntensitySlope: true);
			m_psaiStateIntended = PsaiState.playing;
			return PsaiResult.OK;
		}
		switch (argTheme.themeType)
		{
		case ThemeType.basicMood:
			switch (themeById.themeType)
			{
			case ThemeType.basicMood:
				return PlayThemeAtEndOfCurrentSegment(argTheme, argIntensity, argMusicDuration);
			case ThemeType.basicMoodAlt:
				return PsaiResult.OK;
			case ThemeType.dramaticEvent:
				return PsaiResult.OK;
			case ThemeType.action:
				return PsaiResult.OK;
			case ThemeType.shock:
				return PsaiResult.OK;
			}
			break;
		case ThemeType.basicMoodAlt:
			switch (themeById.themeType)
			{
			case ThemeType.basicMood:
				return PlayThemeAtEndOfCurrentSegment(argTheme, argIntensity, argMusicDuration);
			case ThemeType.basicMoodAlt:
				return PlayThemeAtEndOfCurrentSegment(argTheme, argIntensity, argMusicDuration);
			case ThemeType.dramaticEvent:
				return PlayThemeAtEndOfCurrentSegment(argTheme, argIntensity, argMusicDuration);
			case ThemeType.action:
				return PsaiResult.triggerIgnoredLowPriority;
			case ThemeType.shock:
				if (getThemeTypeOfFirstThemeQueueEntry() == ThemeType.action)
				{
					return PsaiResult.triggerIgnoredLowPriority;
				}
				return PlayThemeAtEndOfCurrentSegment(argTheme, argIntensity, argMusicDuration);
			}
			break;
		case ThemeType.dramaticEvent:
			switch (themeById.themeType)
			{
			case ThemeType.basicMood:
				return PlayThemeNowOrAtEndOfCurrentSegmentAndStartEvaluation(argTheme.id, argIntensity, argMusicDuration, immediately: true, holdIntensity: false);
			case ThemeType.basicMoodAlt:
				return PlayThemeNowOrAtEndOfCurrentSegmentAndStartEvaluation(argTheme.id, argIntensity, argMusicDuration, immediately: true, holdIntensity: false);
			case ThemeType.dramaticEvent:
				return PlayThemeAtEndOfCurrentSegment(argTheme, argIntensity, argMusicDuration);
			case ThemeType.action:
				return PsaiResult.triggerIgnoredLowPriority;
			case ThemeType.shock:
				if (getThemeTypeOfFirstThemeQueueEntry() == ThemeType.action)
				{
					return PsaiResult.triggerIgnoredLowPriority;
				}
				return PlayThemeAtEndOfCurrentTheme(argTheme, argIntensity, argMusicDuration);
			}
			break;
		case ThemeType.action:
			switch (themeById.themeType)
			{
			case ThemeType.basicMood:
				ClearLatestEndOfSegmentTriggerCall();
				return PlayThemeNowOrAtEndOfCurrentSegmentAndStartEvaluation(argTheme.id, argIntensity, argMusicDuration, immediately: true, holdIntensity: false);
			case ThemeType.basicMoodAlt:
				ClearLatestEndOfSegmentTriggerCall();
				return PlayThemeNowOrAtEndOfCurrentSegmentAndStartEvaluation(argTheme.id, argIntensity, argMusicDuration, immediately: true, holdIntensity: false);
			case ThemeType.dramaticEvent:
				ClearLatestEndOfSegmentTriggerCall();
				return PlayThemeNowOrAtEndOfCurrentSegmentAndStartEvaluation(argTheme.id, argIntensity, argMusicDuration, immediately: true, holdIntensity: false);
			case ThemeType.action:
				return PlayThemeAtEndOfCurrentSegment(argTheme, argIntensity, argMusicDuration);
			case ThemeType.shock:
				return PlayThemeAtEndOfCurrentTheme(argTheme, argIntensity, argMusicDuration);
			}
			break;
		case ThemeType.shock:
			switch (themeById.themeType)
			{
			case ThemeType.basicMood:
				return PlayThemeNowOrAtEndOfCurrentSegmentAndStartEvaluation(argTheme.id, argIntensity, argMusicDuration, immediately: true, holdIntensity: false);
			case ThemeType.basicMoodAlt:
				return PlayThemeNowOrAtEndOfCurrentSegmentAndStartEvaluation(argTheme.id, argIntensity, argMusicDuration, immediately: true, holdIntensity: false);
			case ThemeType.dramaticEvent:
				return PlayThemeNowOrAtEndOfCurrentSegmentAndStartEvaluation(argTheme.id, argIntensity, argMusicDuration, immediately: true, holdIntensity: false);
			case ThemeType.action:
				ClearQueuedTheme();
				PushEffectiveThemeToThemeQueue(PsaiPlayMode.regular);
				return PlayThemeNowOrAtEndOfCurrentSegmentAndStartEvaluation(argTheme.id, argIntensity, argMusicDuration, immediately: true, holdIntensity: false);
			case ThemeType.shock:
				return PlayThemeAtEndOfCurrentSegment(argTheme, argIntensity, argMusicDuration);
			}
			break;
		}
		return PsaiResult.internal_error;
	}

	internal static float ClampPercentValue(float argValue)
	{
		if (argValue > 1f)
		{
			return 1f;
		}
		if (argValue < 0f)
		{
			return 0f;
		}
		return argValue;
	}

	internal PsaiResult AddToCurrentIntensity(float deltaIntensity, bool resetIntensityFalloffToFullMusicDuration)
	{
		if (m_psaiState == PsaiState.playing && m_psaiPlayMode == PsaiPlayMode.regular)
		{
			if (m_latestEndOfSegmentTriggerCall.themeId != -1)
			{
				m_latestEndOfSegmentTriggerCall.startIntensity = ClampPercentValue(m_latestEndOfSegmentTriggerCall.startIntensity + deltaIntensity);
			}
			else
			{
				float intensity = ClampPercentValue(getCurrentIntensity() + deltaIntensity);
				SetCurrentIntensityAndMusicDuration(intensity, m_lastMusicDuration, resetIntensityFalloffToFullMusicDuration);
			}
			return PsaiResult.OK;
		}
		return PsaiResult.notReady;
	}

	internal PsaiResult PlaySegmentLayeredAndImmediately(int segmentId)
	{
		Segment segmentById = m_soundtrack.GetSegmentById(segmentId);
		if (segmentById != null)
		{
			PlaySegmentLayeredAndImmediately(segmentById);
		}
		return PsaiResult.invalidHandle;
	}

	internal void PlaySegmentLayeredAndImmediately(Segment segment)
	{
		m_hilightVoiceIndex = getNextVoiceNumber(forHighlight: true);
		m_playbackChannels[m_hilightVoiceIndex].StopChannel();
		m_playbackChannels[m_hilightVoiceIndex].ReleaseSegment();
		m_playbackChannels[m_hilightVoiceIndex].FadeOutVolume = 1f;
		m_playbackChannels[m_hilightVoiceIndex].ScheduleSegmentPlayback(segment, s_audioLayerMaximumLatencyForBufferingSounds + s_audioLayerMaximumLatencyForPlayingbackPrebufferedSounds);
	}

	private PsaiResult startHighlight(Theme highlightTheme)
	{
		if (highlightTheme.m_segments.Count > 0)
		{
			Segment segment;
			if (m_currentSegmentPlaying != null)
			{
				segment = GetBestCompatibleSegment(m_currentSegmentPlaying, highlightTheme.id, getCurrentIntensity(), 15);
			}
			else
			{
				int randomInt = GetRandomInt(0, highlightTheme.m_segments.Count);
				segment = m_soundtrack.GetSegmentById(highlightTheme.m_segments[randomInt].Id);
			}
			if (segment != null)
			{
				PlaySegmentLayeredAndImmediately(segment);
				segment.Playcount++;
				return PsaiResult.OK;
			}
			return PsaiResult.essential_segment_missing;
		}
		return PsaiResult.essential_segment_missing;
	}

	private void ClearLatestEndOfSegmentTriggerCall()
	{
		m_latestEndOfSegmentTriggerCall.themeId = -1;
	}

	private void ClearQueuedTheme()
	{
		m_themeQueue.Clear();
	}

	private bool pushThemeToThemeQueue(int themeId, float intensity, int musicDuration, bool clearThemeQueue, int restTimeMillis, PsaiPlayMode playMode, bool holdIntensity)
	{
		if (clearThemeQueue)
		{
			m_themeQueue.Clear();
		}
		if (m_soundtrack.getThemeById(themeId) != null)
		{
			ThemeQueueEntry themeQueueEntry = new ThemeQueueEntry();
			themeQueueEntry.themeId = themeId;
			themeQueueEntry.startIntensity = intensity;
			themeQueueEntry.musicDuration = musicDuration;
			themeQueueEntry.restTimeMillis = restTimeMillis;
			themeQueueEntry.playmode = playMode;
			themeQueueEntry.holdIntensity = holdIntensity;
			m_themeQueue.Insert(0, themeQueueEntry);
			m_psaiStateIntended = PsaiState.playing;
			return true;
		}
		return false;
	}

	private ThemeType getThemeTypeOfFirstThemeQueueEntry()
	{
		ThemeQueueEntry followingThemeQueueEntry = getFollowingThemeQueueEntry();
		if (followingThemeQueueEntry != null)
		{
			Theme themeById = m_soundtrack.getThemeById(followingThemeQueueEntry.themeId);
			if (themeById != null)
			{
				return themeById.themeType;
			}
		}
		return ThemeType.none;
	}

	internal float getUpcomingIntensity()
	{
		if (m_psaiState == PsaiState.playing && m_latestEndOfSegmentTriggerCall.themeId != -1)
		{
			return m_latestEndOfSegmentTriggerCall.startIntensity;
		}
		return getCurrentIntensity();
	}

	internal float getCurrentIntensity()
	{
		if (m_paused)
		{
			return m_lastIntensity;
		}
		if (m_psaiState == PsaiState.playing && m_psaiStateIntended == PsaiState.playing && !m_returnToLastBasicMoodFlag)
		{
			if (m_holdIntensity)
			{
				return m_heldIntensity;
			}
			float num = 0f;
			if (m_effectiveTheme == null)
			{
				num = 0f;
			}
			else
			{
				if (m_targetSegment != null && (m_currentSegmentPlaying == null || m_currentSegmentPlaying.ThemeId != m_targetSegment.ThemeId))
				{
					return m_targetSegment.Intensity;
				}
				int num2 = GetTimestampMillisElapsedSinceInitialisation() - m_timeStampOfLastIntensitySetForCurrentTheme;
				num = m_startOrRetriggerIntensityOfCurrentTheme - (float)num2 * m_currentIntensitySlope / 1000f;
				if (num < 0f)
				{
					num = 0f;
				}
			}
			m_lastIntensity = num;
			return num;
		}
		return 0f;
	}

	private PsaiResult PlaySegment(Segment targetSnippet, bool immediately)
	{
		if (m_initializationFailure)
		{
			return PsaiResult.initialization_error;
		}
		m_timerSegmentEndApproaching.Stop();
		m_timerStartSnippetPlayback.Stop();
		m_targetVoice = getNextVoiceNumber(forHighlight: false);
		PsaiResult psaiResult = LoadSegment(targetSnippet, m_targetVoice);
		PsaiErrorCheck(psaiResult, "LoadSegment()");
		if (psaiResult != PsaiResult.OK)
		{
			return psaiResult;
		}
		int num = 0;
		m_targetSegment = targetSnippet;
		if (immediately || m_currentSegmentPlaying == null)
		{
			if (m_playbackChannels[m_targetVoice].CheckIfSegmentHadEnoughTimeToLoad())
			{
				m_estimatedTimestampOfTargetSnippetPlayback = GetTimestampMillisElapsedSinceInitialisation() + s_audioLayerMaximumLatencyForPlayingbackPrebufferedSounds;
			}
			else
			{
				m_estimatedTimestampOfTargetSnippetPlayback = GetTimestampMillisElapsedSinceInitialisation() + s_audioLayerMaximumLatencyForPlayingBackUnbufferedSounds;
			}
			PlayTargetSegmentImmediately();
		}
		else
		{
			int millisElapsedAfterCurrentSnippetPlaycall = GetMillisElapsedAfterCurrentSnippetPlaycall();
			num = m_currentSegmentPlaying.audioData.GetFullLengthInMilliseconds() - m_currentSegmentPlaying.audioData.GetPostBeatZoneInMilliseconds() - targetSnippet.audioData.GetPreBeatZoneInMilliseconds() - millisElapsedAfterCurrentSnippetPlaycall;
			if (num > s_audioLayerMaximumLatencyForPlayingBackUnbufferedSounds)
			{
				m_estimatedTimestampOfTargetSnippetPlayback = GetTimestampMillisElapsedSinceInitialisation() + num;
				m_timerStartSnippetPlayback.SetTimer(num, s_audioLayerMaximumLatencyForPlayingbackPrebufferedSounds);
			}
			else
			{
				m_estimatedTimestampOfTargetSnippetPlayback = GetTimestampMillisElapsedSinceInitialisation() + s_audioLayerMaximumLatencyForPlayingbackPrebufferedSounds;
				PlayTargetSegmentImmediately();
			}
		}
		return PsaiResult.OK;
	}

	private PsaiResult LoadSegment(Segment snippet, int channelIndex)
	{
		if (snippet == null || channelIndex >= 9)
		{
			return PsaiResult.invalidHandle;
		}
		m_playbackChannels[channelIndex].LoadSegment(snippet);
		return PsaiResult.OK;
	}

	private void PlayTargetSegmentImmediately()
	{
		int num = 0;
		num = ((!m_playbackChannels[m_targetVoice].CheckIfSegmentHadEnoughTimeToLoad()) ? (m_playbackChannels[m_targetVoice].GetMillisecondsUntilLoadingWillHaveFinished() + s_audioLayerMaximumLatencyForPlayingbackPrebufferedSounds) : (m_estimatedTimestampOfTargetSnippetPlayback - GetTimestampMillisElapsedSinceInitialisation()));
		m_playbackChannels[m_targetVoice].FadeOutVolume = 1f;
		m_playbackChannels[m_targetVoice].ScheduleSegmentPlayback(m_targetSegment, num);
		if (m_scheduleFadeoutUponSnippetPlayback)
		{
			startFade(m_currentVoiceNumber, 500, m_targetSegment.audioData.GetPreBeatZoneInMilliseconds() + num);
			m_scheduleFadeoutUponSnippetPlayback = false;
		}
		m_psaiPlayMode = m_psaiPlayModeIntended;
		m_currentVoiceNumber = m_targetVoice;
		m_currentSegmentPlaying = m_targetSegment;
		m_currentSnippetTypeRequested = m_targetSegmentSuitabilitiesRequested;
		m_currentSegmentPlaying.Playcount++;
		SetSegmentEndApproachingAndReachedTimersAfterPlaybackHasStarted(num);
		m_targetSegment = null;
		m_psaiState = PsaiState.playing;
	}

	internal void SetSegmentEndApproachingAndReachedTimersAfterPlaybackHasStarted(int snippetPlaybackDelayMillis)
	{
		m_timeStampCurrentSnippetPlaycall = GetTimestampMillisElapsedSinceInitialisation() + snippetPlaybackDelayMillis;
		int num = m_currentSegmentPlaying.audioData.GetFullLengthInMilliseconds() + snippetPlaybackDelayMillis;
		int num2 = num - m_currentSegmentPlaying.audioData.GetPostBeatZoneInMilliseconds() - m_currentSegmentPlaying.MaxPreBeatMsOfCompatibleSnippetsWithinSameTheme - s_audioLayerMaximumLatencyForPlayingBackUnbufferedSounds - 2 * s_updateIntervalMillis;
		if (num2 < 0)
		{
			num2 = 0;
		}
		m_timerSegmentEndApproaching.SetTimer(num2, s_updateIntervalMillis);
		m_timerSegmentEndReached.SetTimer(num, 0);
	}

	internal float getVolume()
	{
		return m_psaiMasterVolume;
	}

	internal void setVolume(float vol)
	{
		m_psaiMasterVolume = vol;
		if ((double)vol > 1.0)
		{
			m_psaiMasterVolume = 1f;
		}
		else if (vol < 0f)
		{
			m_psaiMasterVolume = 0f;
		}
		for (int i = 0; i < m_playbackChannels.Length; i++)
		{
			m_playbackChannels[i].MasterVolume = m_psaiMasterVolume;
		}
	}

	private PsaiResult PlayThemeNowOrAtEndOfCurrentSegmentAndStartEvaluation(ThemeQueueEntry tqe, bool immediately)
	{
		return PlayThemeNowOrAtEndOfCurrentSegmentAndStartEvaluation(tqe.themeId, tqe.startIntensity, tqe.musicDuration, immediately, tqe.holdIntensity);
	}

	private PsaiResult PlayThemeNowOrAtEndOfCurrentSegmentAndStartEvaluation(int themeId, float intensity, int musicDuration, bool immediately, bool holdIntensity)
	{
		SetCurrentIntensityAndMusicDuration(intensity, musicDuration, recalculateIntensitySlope: true);
		m_psaiStateIntended = PsaiState.playing;
		m_heldIntensity = intensity;
		if (m_psaiState == PsaiState.rest)
		{
			m_timerWakeUpFromRest.Stop();
		}
		m_targetSegmentSuitabilitiesRequested = 1;
		if (m_psaiState == PsaiState.playing && m_currentSegmentPlaying != null)
		{
			if (m_currentSegmentPlaying.IsUsableOnlyAs(SegmentSuitability.end))
			{
				m_targetSegmentSuitabilitiesRequested = 1;
			}
			else if (getEffectiveThemeId() == themeId)
			{
				m_targetSegmentSuitabilitiesRequested = 2;
			}
			else
			{
				m_targetSegmentSuitabilitiesRequested = 10;
			}
		}
		m_effectiveTheme = m_soundtrack.getThemeById(themeId);
		Segment segment = (((m_targetSegmentSuitabilitiesRequested & 1) <= 0 && GetEffectiveSegment() != null) ? GetBestCompatibleSegment(GetEffectiveSegment(), themeId, intensity, m_targetSegmentSuitabilitiesRequested) : GetBestStartSegmentForTheme(themeId, intensity));
		if (segment == null)
		{
			segment = substituteSegment(themeId);
			if (segment == null)
			{
				StopMusic(immediately: true);
				return PsaiResult.essential_segment_missing;
			}
		}
		m_holdIntensity = holdIntensity;
		if (immediately && GetEffectiveSegment() != null)
		{
			m_scheduleFadeoutUponSnippetPlayback = true;
		}
		if (segment != null)
		{
			return PlaySegment(segment, immediately);
		}
		return PsaiResult.internal_error;
	}

	internal PsaiResult StopMusic(bool immediately)
	{
		return StopMusic(immediately, 1000);
	}

	internal PsaiResult StopMusic(bool immediately, int fadeOutMilliSeconds)
	{
		if (immediately && fadeOutMilliSeconds <= 0)
		{
			fadeOutMilliSeconds = 1000;
		}
		if (m_paused)
		{
			setPaused(setPaused: false);
		}
		ClearLatestEndOfSegmentTriggerCall();
		ClearQueuedTheme();
		if (m_psaiPlayMode == PsaiPlayMode.menuMode)
		{
			return PsaiResult.commandIgnoredMenuModeActive;
		}
		if (m_psaiPlayModeIntended == PsaiPlayMode.cutScene)
		{
			return PsaiResult.commandIgnoredCutsceneActive;
		}
		if (m_initializationFailure)
		{
			return PsaiResult.initialization_error;
		}
		if (m_psaiStateIntended == PsaiState.silence && !immediately)
		{
			return PsaiResult.commandIgnored;
		}
		m_returnToLastBasicMoodFlag = false;
		m_holdIntensity = false;
		switch (m_psaiState)
		{
		case PsaiState.silence:
		case PsaiState.playing:
		{
			if (GetEffectiveSegment() == null)
			{
				break;
			}
			bool flag = false;
			if (!immediately)
			{
				flag = CheckIfThereIsAPathToEndSegmentForEffectiveSegmentAndLogWarningIfThereIsnt();
			}
			if (immediately || !flag)
			{
				startFade(m_currentVoiceNumber, fadeOutMilliSeconds, 0);
				EnterSilenceMode();
				break;
			}
			if (m_latestEndOfSegmentTriggerCall.themeId != -1)
			{
				m_latestEndOfSegmentTriggerCall.themeId = -1;
			}
			m_psaiStateIntended = PsaiState.silence;
			break;
		}
		case PsaiState.rest:
			EnterSilenceMode();
			break;
		}
		return PsaiResult.OK;
	}

	private void WriteLogWarningIfThereIsNoDirectPathForEffectiveSnippetToEndSnippet()
	{
	}

	private bool CheckIfThereIsAPathToEndSegmentForEffectiveSegmentAndLogWarningIfThereIsnt()
	{
		Segment effectiveSegment = GetEffectiveSegment();
		if (!effectiveSegment.IsUsableAs(SegmentSuitability.end) && effectiveSegment.nextSnippetToShortestEndSequence == null)
		{
			return false;
		}
		return true;
	}

	private void updateFades()
	{
		bool flag = false;
		int timestampMillisElapsedSinceInitialisation = GetTimestampMillisElapsedSinceInitialisation();
		int num = timestampMillisElapsedSinceInitialisation - m_timeStampOfLastFadeUpdate;
		m_timeStampOfLastFadeUpdate = timestampMillisElapsedSinceInitialisation;
		int num2 = 0;
		while (num2 < m_fadeVoices.Count)
		{
			FadeData fadeData = m_fadeVoices[num2];
			if (fadeData.delayMillis > 0)
			{
				fadeData.delayMillis -= num;
				if (fadeData.delayMillis <= 0)
				{
					fadeData.delayMillis = 0;
				}
				flag = true;
				num2++;
				continue;
			}
			float num3 = fadeData.currentVolume - fadeData.fadeoutDeltaVolumePerUpdate;
			if (num3 > 0f)
			{
				flag = true;
				fadeData.currentVolume = num3;
				m_playbackChannels[fadeData.voiceNumber].FadeOutVolume = num3;
				num2++;
				continue;
			}
			int voiceNumber = fadeData.voiceNumber;
			if (m_playbackChannels[voiceNumber].IsPlaying())
			{
				m_playbackChannels[voiceNumber].StopChannel();
				m_playbackChannels[voiceNumber].ReleaseSegment();
				m_fadeVoices.RemoveAt(num2);
			}
			else
			{
				m_fadeVoices.RemoveAt(num2);
			}
		}
		if (flag)
		{
			m_timerFades.SetTimer(50, 0);
		}
	}

	internal PsaiResult HoldCurrentIntensity(bool hold)
	{
		switch (m_psaiPlayModeIntended)
		{
		case PsaiPlayMode.cutScene:
			return PsaiResult.commandIgnoredCutsceneActive;
		case PsaiPlayMode.menuMode:
			return PsaiResult.commandIgnoredMenuModeActive;
		case PsaiPlayMode.regular:
			if (hold && m_holdIntensity)
			{
				return PsaiResult.commandIgnored;
			}
			if (!hold && !m_holdIntensity)
			{
				return PsaiResult.commandIgnored;
			}
			if (hold)
			{
				m_remainingMusicDurationAtTimeOfHoldIntensity = GetRemainingMusicDurationSecondsOfCurrentTheme();
				m_heldIntensity = getCurrentIntensity();
				m_holdIntensity = true;
			}
			else
			{
				SetCurrentIntensityAndMusicDuration(m_heldIntensity, m_remainingMusicDurationAtTimeOfHoldIntensity, recalculateIntensitySlope: false);
				m_holdIntensity = false;
			}
			return PsaiResult.OK;
		default:
			return PsaiResult.internal_error;
		}
	}

	private void SetCurrentIntensityAndMusicDuration(float intensity, int musicDuration, bool recalculateIntensitySlope)
	{
		m_timeStampOfLastIntensitySetForCurrentTheme = GetTimestampMillisElapsedSinceInitialisation();
		m_lastMusicDuration = musicDuration;
		if (recalculateIntensitySlope)
		{
			m_currentIntensitySlope = intensity / (float)musicDuration;
		}
		m_startOrRetriggerIntensityOfCurrentTheme = intensity;
	}

	private void SegmentEndApproachingHandler()
	{
		if (m_latestEndOfSegmentTriggerCall.themeId != -1)
		{
			m_psaiState = PsaiState.playing;
			m_psaiStateIntended = PsaiState.playing;
		}
		switch (m_psaiStateIntended)
		{
		case PsaiState.silence:
			if (m_currentSegmentPlaying == null || m_currentSegmentPlaying.IsUsableAs(SegmentSuitability.end))
			{
				EnterSilenceMode();
			}
			else
			{
				PlaySegment(m_currentSegmentPlaying.nextSnippetToShortestEndSequence, immediately: false);
			}
			return;
		case PsaiState.rest:
			if (m_currentSegmentPlaying == null || m_currentSegmentPlaying.IsUsableAs(SegmentSuitability.end))
			{
				if (m_psaiState != PsaiState.rest)
				{
					EnterRestMode(GetLastBasicMoodId(), getEffectiveThemeId());
				}
			}
			else
			{
				PlaySegment(m_currentSegmentPlaying.nextSnippetToShortestEndSequence, immediately: false);
			}
			return;
		}
		if (m_returnToLastBasicMoodFlag)
		{
			if ((m_currentSegmentPlaying.SnippetTypeBitfield & 4) == 0 && CheckIfThereIsAPathToEndSegmentForEffectiveSegmentAndLogWarningIfThereIsnt())
			{
				WriteLogWarningIfThereIsNoDirectPathForEffectiveSnippetToEndSnippet();
				PlaySegment(m_currentSegmentPlaying.nextSnippetToShortestEndSequence, immediately: false);
			}
			else
			{
				PlayThemeNowOrAtEndOfCurrentSegmentAndStartEvaluation(GetLastBasicMoodId(), m_lastBasicMood.intensityAfterRest, m_lastBasicMood.musicDurationGeneral, immediately: false, holdIntensity: false);
				m_returnToLastBasicMoodFlag = false;
			}
		}
		else if (m_psaiPlayMode == PsaiPlayMode.regular && m_latestEndOfSegmentTriggerCall.themeId != -1)
		{
			Theme themeById = m_soundtrack.getThemeById(m_latestEndOfSegmentTriggerCall.themeId);
			Segment value;
			if (m_currentSegmentPlaying.CheckIfAtLeastOneDirectTransitionOrLayeringIsPossible(m_soundtrack, themeById.id))
			{
				PlayThemeNowOrAtEndOfCurrentSegmentAndStartEvaluation(m_latestEndOfSegmentTriggerCall.themeId, m_latestEndOfSegmentTriggerCall.startIntensity, m_latestEndOfSegmentTriggerCall.musicDuration, immediately: false, m_latestEndOfSegmentTriggerCall.holdIntensity);
				m_latestEndOfSegmentTriggerCall.themeId = -1;
			}
			else if (m_currentSegmentPlaying.MapOfNextTransitionSegmentToTheme.TryGetValue(themeById.id, out value))
			{
				PlaySegment(value, immediately: false);
			}
			else
			{
				PlayThemeNowOrAtEndOfCurrentSegmentAndStartEvaluation(m_latestEndOfSegmentTriggerCall.themeId, m_latestEndOfSegmentTriggerCall.startIntensity, m_latestEndOfSegmentTriggerCall.musicDuration, immediately: true, m_latestEndOfSegmentTriggerCall.holdIntensity);
				m_latestEndOfSegmentTriggerCall.themeId = -1;
			}
		}
		else if (getCurrentIntensity() > 0f)
		{
			m_latestEndOfSegmentTriggerCall.themeId = -1;
			PlaySegmentOfCurrentTheme(SegmentSuitability.middle);
		}
		else
		{
			IntensityZeroHandler();
		}
	}

	private PsaiResult PlayThemeAtEndOfCurrentSegment(Theme argTheme, float intensity, int musicDuration)
	{
		m_latestEndOfSegmentTriggerCall.themeId = argTheme.id;
		m_latestEndOfSegmentTriggerCall.startIntensity = intensity;
		m_latestEndOfSegmentTriggerCall.musicDuration = musicDuration;
		m_psaiStateIntended = PsaiState.playing;
		return PsaiResult.OK;
	}

	private PsaiResult PlayThemeAtEndOfCurrentTheme(Theme argTheme, float argIntensity, int argMusicDuration)
	{
		ClearLatestEndOfSegmentTriggerCall();
		ClearQueuedTheme();
		pushThemeToThemeQueue(argTheme.id, argIntensity, argMusicDuration, clearThemeQueue: false, 1, PsaiPlayMode.regular, holdIntensity: false);
		return PsaiResult.OK;
	}

	private void SegmentEndReachedHandler()
	{
		if (m_targetSegment == null)
		{
			m_currentSegmentPlaying = null;
		}
	}

	private void InitiateTransitionToRestOrSilence(PsaiState psaiStateIntended)
	{
		if ((psaiStateIntended == PsaiState.rest || psaiStateIntended == PsaiState.silence) && m_currentSegmentPlaying != null)
		{
			if (m_currentSegmentPlaying.IsUsableAs(SegmentSuitability.end))
			{
				EnterRestMode(GetLastBasicMoodId(), getEffectiveThemeId());
			}
			else if (!CheckIfThereIsAPathToEndSegmentForEffectiveSegmentAndLogWarningIfThereIsnt())
			{
				startFade(m_currentVoiceNumber, GetRemainingMillisecondsOfCurrentSegmentPlayback(), 0);
				EnterRestMode(GetLastBasicMoodId(), getEffectiveThemeId());
			}
			else
			{
				WriteLogWarningIfThereIsNoDirectPathForEffectiveSnippetToEndSnippet();
				PlaySegment(m_currentSegmentPlaying.nextSnippetToShortestEndSequence, immediately: false);
				m_psaiStateIntended = psaiStateIntended;
			}
		}
	}

	private void InitiateTransitionToRestMode()
	{
		InitiateTransitionToRestOrSilence(PsaiState.rest);
	}

	private void IntensityZeroHandler()
	{
		if (m_currentSegmentPlaying == null)
		{
			return;
		}
		switch (m_soundtrack.getThemeById(m_currentSegmentPlaying.ThemeId).themeType)
		{
		case ThemeType.basicMood:
			InitiateTransitionToRestMode();
			break;
		case ThemeType.action:
			if (m_lastBasicMood == null)
			{
				InitiateTransitionToRestOrSilence(PsaiState.silence);
			}
			else
			{
				InitiateTransitionToRestMode();
			}
			break;
		case ThemeType.basicMoodAlt:
		case ThemeType.dramaticEvent:
			if (m_lastBasicMood != null)
			{
				PlayThemeNowOrAtEndOfCurrentSegmentAndStartEvaluation(m_lastBasicMood.id, m_lastBasicMood.intensityAfterRest, m_lastBasicMood.musicDurationAfterRest, immediately: false, holdIntensity: false);
			}
			else
			{
				InitiateTransitionToRestOrSilence(PsaiState.silence);
			}
			break;
		case ThemeType.shock:
		{
			ThemeQueueEntry followingThemeQueueEntry = getFollowingThemeQueueEntry();
			if (followingThemeQueueEntry != null)
			{
				if (m_currentSegmentPlaying.CheckIfAnyDirectOrIndirectTransitionIsPossible(m_soundtrack, followingThemeQueueEntry.themeId))
				{
					PopAndPlayNextFollowingTheme(immediately: false);
				}
				else
				{
					InitiateTransitionToRestMode();
				}
			}
			else
			{
				InitiateTransitionToRestMode();
			}
			break;
		}
		case (ThemeType)4:
		case ThemeType.highlightLayer:
			break;
		}
	}

	public PsaiResult GoToRest(bool immediately, int fadeOutMilliSeconds)
	{
		return GoToRest(immediately, fadeOutMilliSeconds, -1, -1);
	}

	public PsaiResult GoToRest(bool immediately, int fadeOutMilliSeconds, int restSecondsOverrideMin, int restSecondsOverrideMax)
	{
		if (restSecondsOverrideMin > restSecondsOverrideMax)
		{
			return PsaiResult.invalidParam;
		}
		if (fadeOutMilliSeconds < 0)
		{
			return PsaiResult.invalidParam;
		}
		if (restSecondsOverrideMin >= 0 && restSecondsOverrideMax >= 0)
		{
			m_restModeSecondsOverride = GetRandomInt(restSecondsOverrideMin, restSecondsOverrideMax);
		}
		else
		{
			m_restModeSecondsOverride = -1;
		}
		if (!immediately)
		{
			InitiateTransitionToRestMode();
		}
		else
		{
			startFade(m_currentVoiceNumber, fadeOutMilliSeconds, 0);
			EnterRestMode(GetLastBasicMoodId(), getEffectiveThemeId());
		}
		return PsaiResult.OK;
	}

	private void EnterRestMode(int themeIdToWakeUpWith, int themeIdToUseForRestingTimeCalculation)
	{
		m_psaiState = PsaiState.rest;
		m_holdIntensity = false;
		m_timerStartSnippetPlayback.Stop();
		m_timerSegmentEndApproaching.Stop();
		m_timerWakeUpFromRest.Stop();
		m_effectiveTheme = m_soundtrack.getThemeById(themeIdToWakeUpWith);
		if (m_effectiveTheme != null)
		{
			int num = 0;
			if (m_restModeSecondsOverride > 0)
			{
				num = m_restModeSecondsOverride;
				m_restModeSecondsOverride = -1;
			}
			else
			{
				Theme themeById = m_soundtrack.getThemeById(themeIdToUseForRestingTimeCalculation);
				num = ((themeById == null) ? (GetRandomInt(m_effectiveTheme.restSecondsMin, m_effectiveTheme.restSecondsMax) * 1000) : (GetRandomInt(themeById.restSecondsMin, themeById.restSecondsMax) * 1000));
			}
			if (num > 0)
			{
				m_timeStampRestStart = GetTimestampMillisElapsedSinceInitialisation();
				m_timerWakeUpFromRest.SetTimer(num, 0);
			}
			else
			{
				WakeUpFromRestHandler();
			}
		}
	}

	private void WakeUpFromRestHandler()
	{
		if (m_effectiveTheme != null)
		{
			PlayThemeNowOrAtEndOfCurrentSegmentAndStartEvaluation(m_effectiveTheme.id, m_effectiveTheme.intensityAfterRest, m_effectiveTheme.musicDurationAfterRest, immediately: true, holdIntensity: false);
			m_psaiState = PsaiState.playing;
			m_psaiStateIntended = PsaiState.playing;
		}
	}

	private Segment GetBestCompatibleSegment(Segment sourceSegment, int targetThemeId, float intensity, int allowedSegmentSuitabilities)
	{
		float num = 0f;
		int num2 = 0;
		int num3 = 0;
		if (sourceSegment == null)
		{
			return null;
		}
		List<Follower> list = new List<Follower>();
		int count = sourceSegment.Followers.Count;
		for (int i = 0; i < count; i++)
		{
			int snippetId = sourceSegment.Followers[i].snippetId;
			Segment segmentById = m_soundtrack.GetSegmentById(snippetId);
			if (segmentById != null && (allowedSegmentSuitabilities & segmentById.SnippetTypeBitfield) > 0 && segmentById.ThemeId == targetThemeId)
			{
				if (i == 0)
				{
					num2 = segmentById.Playcount;
				}
				else if (segmentById.Playcount < num2)
				{
					num2 = segmentById.Playcount;
				}
				if (segmentById.Playcount > num3)
				{
					num3 = segmentById.Playcount;
				}
				float num4 = intensity - segmentById.Intensity;
				if (num4 < 0f)
				{
					num4 *= -1f;
				}
				if (num4 > num)
				{
					num = num4;
				}
				list.Add(sourceSegment.Followers[i]);
			}
		}
		if (list.Count == 0)
		{
			return null;
		}
		Weighting weighting = null;
		Theme themeById = m_soundtrack.getThemeById(targetThemeId);
		if (themeById != null)
		{
			weighting = themeById.weightings;
		}
		return ChooseBestSegmentFromList(list, weighting, intensity, num3, num2, num);
	}

	private Segment GetBestStartSegmentForTheme(int themeId, float intensity)
	{
		Theme themeById = m_soundtrack.getThemeById(themeId);
		if (themeById == null)
		{
			return null;
		}
		return GetBestStartSegmentForTheme_internal(themeById, intensity);
	}

	private Segment GetBestStartSegmentForTheme_internal(Theme theme, float intensity)
	{
		float num = 0f;
		int num2 = 0;
		int num3 = 0;
		List<Follower> list = new List<Follower>();
		int count = theme.m_segments.Count;
		for (int i = 0; i < count; i++)
		{
			Segment segment = theme.m_segments[i];
			if (segment != null && (1 & segment.SnippetTypeBitfield) > 0)
			{
				if (i == 0)
				{
					num2 = segment.Playcount;
				}
				else if (segment.Playcount < num2)
				{
					num2 = segment.Playcount;
				}
				if (segment.Playcount > num3)
				{
					num3 = segment.Playcount;
				}
				float num4 = intensity - segment.Intensity;
				if (num4 < 0f)
				{
					num4 *= -1f;
				}
				if (num4 > num)
				{
					num = num4;
				}
				Follower item = new Follower(segment.Id, 1f);
				list.Add(item);
			}
		}
		Weighting weightings = theme.weightings;
		return ChooseBestSegmentFromList(list, weightings, intensity, num3, num2, num);
	}

	private Segment ChooseBestSegmentFromList(List<Follower> segmentList, Weighting weighting, float intensity, int maxPlaycount, int minPlaycount, float maxDeltaIntensity)
	{
		Segment segment = null;
		if (segmentList.Count == 0)
		{
			return null;
		}
		float num = 1f;
		float num2 = 1f;
		int num3 = maxPlaycount - minPlaycount;
		num = ((num3 <= 0) ? 0f : (1f / (float)num3));
		num2 = ((!(maxDeltaIntensity > 0f)) ? 0f : (1f / maxDeltaIntensity));
		float num4 = 0f;
		int count = segmentList.Count;
		for (int i = 0; i < count; i++)
		{
			Segment segmentById = m_soundtrack.GetSegmentById(segmentList[i].snippetId);
			float num5 = 1f - weighting.switchGroups;
			float num6 = 1f - weighting.intensityVsVariety;
			float intensityVsVariety = weighting.intensityVsVariety;
			float num7 = 1f - weighting.lowPlaycountVsRandom;
			float lowPlaycountVsRandom = weighting.lowPlaycountVsRandom;
			float num8 = segmentList[i].compatibility * num5;
			float num9 = intensity - segmentById.Intensity;
			if (num9 < 0f)
			{
				num9 *= -1f;
			}
			float num10 = (1f - num9 * num2) * num6;
			float num11 = (float)(segmentById.Playcount - minPlaycount) * num;
			float num12 = (1f - num11) * num7;
			float num13 = GetRandomFloat() * lowPlaycountVsRandom;
			float num14 = (num12 + num13) * intensityVsVariety * 0.5f;
			float num15 = num8 + num10 + num14;
			if (segment == null || num15 > num4)
			{
				segment = segmentById;
				num4 = num15;
			}
		}
		return segment;
	}

	private void PlaySegmentOfCurrentTheme(SegmentSuitability snippetType)
	{
		if (m_effectiveTheme == null)
		{
			return;
		}
		float currentIntensity = getCurrentIntensity();
		if (m_currentSegmentPlaying != null)
		{
			Segment segment = GetBestCompatibleSegment(m_currentSegmentPlaying, m_effectiveTheme.id, currentIntensity, (int)snippetType);
			m_targetSegmentSuitabilitiesRequested = (int)snippetType;
			if (segment == null)
			{
				segment = substituteSegment(m_effectiveTheme.id);
			}
			if (segment != null)
			{
				PlaySegment(segment, immediately: false);
			}
		}
	}

	private Segment substituteSegment(int themeId)
	{
		Segment result = null;
		Theme themeById = m_soundtrack.getThemeById(themeId);
		if (themeById != null && themeById.m_segments.Count > 0)
		{
			result = themeById.m_segments[0];
		}
		return result;
	}

	private void EnterSilenceMode()
	{
		m_timerStartSnippetPlayback.Stop();
		m_timerSegmentEndApproaching.Stop();
		m_timerWakeUpFromRest.Stop();
		m_targetSegment = null;
		m_effectiveTheme = null;
		m_scheduleFadeoutUponSnippetPlayback = false;
		m_psaiStateIntended = PsaiState.silence;
		m_psaiState = PsaiState.silence;
	}

	internal bool menuModeIsActive()
	{
		return m_psaiPlayMode == PsaiPlayMode.menuMode;
	}

	internal bool cutSceneIsActive()
	{
		return m_psaiPlayModeIntended == PsaiPlayMode.cutScene;
	}

	internal PsaiResult MenuModeEnter(int menuThemeId, float menuIntensity)
	{
		if (m_initializationFailure)
		{
			return PsaiResult.initialization_error;
		}
		if (m_paused)
		{
			setPaused(setPaused: false);
		}
		if (m_psaiPlayMode != PsaiPlayMode.menuMode)
		{
			if (m_psaiPlayMode == PsaiPlayMode.cutScene && m_psaiPlayModeIntended == PsaiPlayMode.regular)
			{
				PushEffectiveThemeToThemeQueue(m_psaiPlayModeIntended);
			}
			else if (m_psaiState == PsaiState.playing)
			{
				PushEffectiveThemeToThemeQueue(m_psaiPlayMode);
			}
			if (m_soundtrack.getThemeById(menuThemeId) != null)
			{
				PlayThemeNowOrAtEndOfCurrentSegmentAndStartEvaluation(menuThemeId, menuIntensity, 666, immediately: true, holdIntensity: true);
			}
			else
			{
				setPaused(setPaused: false);
			}
			SetPlayMode(PsaiPlayMode.menuMode);
			m_psaiPlayModeIntended = PsaiPlayMode.menuMode;
			return PsaiResult.OK;
		}
		return PsaiResult.commandIgnoredMenuModeActive;
	}

	internal PsaiResult MenuModeLeave()
	{
		if (m_initializationFailure)
		{
			return PsaiResult.initialization_error;
		}
		if (m_paused)
		{
			setPaused(setPaused: false);
		}
		if (m_psaiPlayMode == PsaiPlayMode.menuMode)
		{
			if (getFollowingThemeQueueEntry() != null)
			{
				PopAndPlayNextFollowingTheme(immediately: true);
				return PsaiResult.OK;
			}
			m_psaiStateIntended = PsaiState.silence;
			m_psaiState = PsaiState.silence;
			SetPlayMode(PsaiPlayMode.regular);
			m_psaiPlayModeIntended = PsaiPlayMode.regular;
			StopMusic(immediately: true);
			return PsaiResult.OK;
		}
		return PsaiResult.commandIgnored;
	}

	internal PsaiResult CutSceneEnter(int themeId, float intensity)
	{
		if (m_initializationFailure)
		{
			return PsaiResult.initialization_error;
		}
		switch (m_psaiPlayModeIntended)
		{
		case PsaiPlayMode.cutScene:
			return PsaiResult.commandIgnoredCutsceneActive;
		case PsaiPlayMode.menuMode:
			return PsaiResult.commandIgnoredMenuModeActive;
		default:
		{
			PushEffectiveThemeToThemeQueue(PsaiPlayMode.regular);
			Theme themeById = m_soundtrack.getThemeById(themeId);
			SetPlayMode(PsaiPlayMode.cutScene);
			m_psaiPlayModeIntended = PsaiPlayMode.cutScene;
			if (themeById != null)
			{
				PlayThemeNowOrAtEndOfCurrentSegmentAndStartEvaluation(themeId, intensity, themeById.musicDurationGeneral, immediately: true, holdIntensity: true);
				return PsaiResult.OK;
			}
			PlayThemeNowOrAtEndOfCurrentSegmentAndStartEvaluation(m_lastBasicMood.id, intensity, m_lastBasicMood.musicDurationGeneral, immediately: true, holdIntensity: true);
			return PsaiResult.unknown_theme;
		}
		}
	}

	internal PsaiResult CutSceneLeave(bool immediately, bool reset)
	{
		if (m_initializationFailure)
		{
			return PsaiResult.initialization_error;
		}
		if (m_psaiPlayMode == PsaiPlayMode.cutScene && m_psaiPlayModeIntended == PsaiPlayMode.cutScene)
		{
			if (reset)
			{
				m_themeQueue.Clear();
			}
			if (getFollowingThemeQueueEntry() != null)
			{
				m_psaiPlayModeIntended = PsaiPlayMode.regular;
				PopAndPlayNextFollowingTheme(immediately);
				return PsaiResult.OK;
			}
			m_psaiStateIntended = PsaiState.silence;
			m_psaiState = PsaiState.silence;
			m_psaiPlayModeIntended = PsaiPlayMode.regular;
			StopMusic(immediately);
			return PsaiResult.OK;
		}
		return PsaiResult.commandIgnored;
	}

	private ThemeQueueEntry getFollowingThemeQueueEntry()
	{
		if (m_themeQueue.Count > 0)
		{
			return m_themeQueue[0];
		}
		return null;
	}

	private void SetPlayMode(PsaiPlayMode playMode)
	{
		m_psaiPlayMode = playMode;
	}

	private void PushEffectiveThemeToThemeQueue(PsaiPlayMode playModeToReturnTo)
	{
		if (m_psaiState == PsaiState.rest)
		{
			int restTimeMillis = GetTimestampMillisElapsedSinceInitialisation() - m_timeStampRestStart;
			m_timerWakeUpFromRest.Stop();
			pushThemeToThemeQueue(m_lastBasicMood.id, m_lastBasicMood.intensityAfterRest, 0, clearThemeQueue: true, restTimeMillis, PsaiPlayMode.regular, holdIntensity: false);
			return;
		}
		if (m_latestEndOfSegmentTriggerCall.themeId != -1)
		{
			Theme themeById = m_soundtrack.getThemeById(m_latestEndOfSegmentTriggerCall.themeId);
			pushThemeToThemeQueue(themeById.id, m_latestEndOfSegmentTriggerCall.startIntensity, m_latestEndOfSegmentTriggerCall.musicDuration, clearThemeQueue: false, 0, PsaiPlayMode.regular, holdIntensity: false);
			m_latestEndOfSegmentTriggerCall.themeId = -1;
			return;
		}
		Segment effectiveSegment = GetEffectiveSegment();
		if (effectiveSegment != null)
		{
			if ((effectiveSegment == m_targetSegment && m_currentSegmentPlaying == null) || (m_targetSegment != null && m_currentSegmentPlaying != null && m_targetSegment.ThemeId != m_currentSegmentPlaying.ThemeId))
			{
				Theme themeById2 = m_soundtrack.getThemeById(m_targetSegment.ThemeId);
				pushThemeToThemeQueue(m_targetSegment.ThemeId, getUpcomingIntensity(), themeById2.musicDurationGeneral, clearThemeQueue: false, 0, playModeToReturnTo, m_holdIntensity);
			}
			else
			{
				pushThemeToThemeQueue(effectiveSegment.ThemeId, getCurrentIntensity(), GetRemainingMusicDurationSecondsOfCurrentTheme(), clearThemeQueue: false, 0, playModeToReturnTo, m_holdIntensity);
			}
		}
	}

	private Segment GetEffectiveSegment()
	{
		if (m_targetSegment != null)
		{
			return m_targetSegment;
		}
		if (m_currentSegmentPlaying != null)
		{
			return m_currentSegmentPlaying;
		}
		return null;
	}

	internal int getEffectiveThemeId()
	{
		if (m_effectiveTheme != null)
		{
			return m_effectiveTheme.id;
		}
		return -1;
	}

	private int GetEffectiveSegmentSuitabilitiesRequested()
	{
		if (m_targetSegment != null)
		{
			return m_targetSegmentSuitabilitiesRequested;
		}
		return m_currentSnippetTypeRequested;
	}

	private void PopAndPlayNextFollowingTheme(bool immediately)
	{
		if (getFollowingThemeQueueEntry() != null)
		{
			ThemeQueueEntry followingThemeQueueEntry = getFollowingThemeQueueEntry();
			m_psaiPlayModeIntended = followingThemeQueueEntry.playmode;
			switch (m_psaiPlayModeIntended)
			{
			case PsaiPlayMode.regular:
				PlayThemeNowOrAtEndOfCurrentSegmentAndStartEvaluation(followingThemeQueueEntry, immediately);
				break;
			case PsaiPlayMode.cutScene:
				PlayThemeNowOrAtEndOfCurrentSegmentAndStartEvaluation(followingThemeQueueEntry.themeId, followingThemeQueueEntry.startIntensity, followingThemeQueueEntry.musicDuration, immediately, followingThemeQueueEntry.holdIntensity);
				break;
			case PsaiPlayMode.menuMode:
				PlayThemeNowOrAtEndOfCurrentSegmentAndStartEvaluation(followingThemeQueueEntry.themeId, followingThemeQueueEntry.startIntensity, followingThemeQueueEntry.musicDuration, immediately, followingThemeQueueEntry.holdIntensity);
				break;
			}
			removeFirstFollowingThemeQueueEntry();
		}
	}

	private void removeFirstFollowingThemeQueueEntry()
	{
		if (m_themeQueue.Count > 0)
		{
			m_themeQueue.RemoveAt(0);
		}
	}

	internal PsaiInfo getPsaiInfo()
	{
		return new PsaiInfo(m_psaiState, m_psaiStateIntended, m_lastBasicMood?.id ?? (-1), getEffectiveThemeId(), getUpcomingThemeId(), getCurrentIntensity(), getUpcomingIntensity(), m_themeQueue.Count, m_targetSegment?.Id ?? (-1), m_holdIntensity, m_returnToLastBasicMoodFlag, m_timerWakeUpFromRest.IsSet() ? m_timerWakeUpFromRest.GetRemainingMillisToFireTime() : 0, m_paused);
	}

	internal int getCurrentSnippetId()
	{
		if (m_currentSegmentPlaying != null)
		{
			return m_currentSegmentPlaying.Id;
		}
		return -1;
	}

	internal int GetRemainingMillisecondsOfCurrentSegmentPlayback()
	{
		if (m_currentSegmentPlaying != null)
		{
			return m_currentSegmentPlaying.audioData.GetFullLengthInMilliseconds() - GetMillisElapsedAfterCurrentSnippetPlaycall();
		}
		return -1;
	}

	internal int GetRemainingMillisecondsUntilNextSegmentStart()
	{
		if (m_timerStartSnippetPlayback.IsSet() && m_timerStartSnippetPlayback.IsSet())
		{
			return m_timerStartSnippetPlayback.GetRemainingMillisToFireTime();
		}
		return -1;
	}

	internal bool CheckIfAtLeastOneDirectTransitionOrLayeringIsPossible(int sourceSegmentId, int targetThemeId)
	{
		return m_soundtrack.GetSegmentById(sourceSegmentId)?.CheckIfAtLeastOneDirectTransitionOrLayeringIsPossible(m_soundtrack, targetThemeId) ?? false;
	}
}
