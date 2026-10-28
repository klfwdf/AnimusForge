using System.Collections.Generic;

namespace psai.net;

public class PsaiCore
{
	private Logik m_logik;

	private static PsaiCore s_singleton;

	public static PsaiCore Instance
	{
		get
		{
			if (s_singleton == null)
			{
				s_singleton = new PsaiCore();
			}
			return s_singleton;
		}
		set
		{
			s_singleton = null;
		}
	}

	public static bool IsInstanceInitialized()
	{
		return s_singleton != null;
	}

	public PsaiCore()
	{
		m_logik = Logik.Instance;
	}

	public PsaiResult SetMaximumLatencyNeededByPlatformToBufferSounddata(int latencyInMilliseconds)
	{
		return m_logik.SetMaximumLatencyNeededByPlatformToBufferSounddata(latencyInMilliseconds);
	}

	public PsaiResult SetMaximumLatencyNeededByPlatformToPlayBackBufferedSounddata(int latencyInMilliseconds)
	{
		return m_logik.SetMaximumLatencyNeededByPlatformToPlayBackBufferedSounds(latencyInMilliseconds);
	}

	public PsaiResult LoadSoundtrackFromProjectFile(List<string> pathToProjectFiles)
	{
		return m_logik.LoadSoundtrackFromProjectFile(pathToProjectFiles);
	}

	public PsaiResult TriggerMusicTheme(int themeId, float intensity)
	{
		return m_logik.TriggerMusicTheme(themeId, intensity);
	}

	public PsaiResult TriggerMusicTheme(int themeId, float intensity, int musicDurationInSeconds)
	{
		return m_logik.TriggerMusicTheme(themeId, intensity, musicDurationInSeconds);
	}

	public PsaiResult AddToCurrentIntensity(float deltaIntensity)
	{
		return m_logik.AddToCurrentIntensity(deltaIntensity, resetIntensityFalloffToFullMusicDuration: false);
	}

	public PsaiResult StopMusic(bool immediately)
	{
		return m_logik.StopMusic(immediately);
	}

	public PsaiResult StopMusic(bool immediately, float fadeOutSeconds)
	{
		return m_logik.StopMusic(immediately, (int)(fadeOutSeconds * 1000f));
	}

	public PsaiResult ReturnToLastBasicMood(bool immediately)
	{
		return m_logik.ReturnToLastBasicMood(immediately);
	}

	public PsaiResult GoToRest(bool immediately, float fadeOutSeconds)
	{
		return m_logik.GoToRest(immediately, (int)(fadeOutSeconds * 1000f));
	}

	public PsaiResult GoToRest(bool immediately, float fadeOutSeconds, int restTimeMin, int restTimeMax)
	{
		return m_logik.GoToRest(immediately, (int)(fadeOutSeconds * 1000f), restTimeMin, restTimeMax);
	}

	public PsaiResult HoldCurrentIntensity(bool hold)
	{
		return m_logik.HoldCurrentIntensity(hold);
	}

	public string GetVersion()
	{
		return m_logik.getVersion();
	}

	public PsaiResult Update()
	{
		return m_logik.Update();
	}

	public float GetVolume()
	{
		return m_logik.getVolume();
	}

	public void SetVolume(float volume)
	{
		m_logik.setVolume(volume);
	}

	public void SetPaused(bool setPaused)
	{
		m_logik.setPaused(setPaused);
	}

	public float GetCurrentIntensity()
	{
		return m_logik.getCurrentIntensity();
	}

	public PsaiInfo GetPsaiInfo()
	{
		return m_logik.getPsaiInfo();
	}

	public SoundtrackInfo GetSoundtrackInfo()
	{
		return m_logik.m_soundtrack.getSoundtrackInfo();
	}

	public ThemeInfo GetThemeInfo(int themeId)
	{
		return m_logik.m_soundtrack.getThemeInfo(themeId);
	}

	public SegmentInfo GetSegmentInfo(int segmentId)
	{
		return m_logik.m_soundtrack.getSegmentInfo(segmentId);
	}

	public int GetCurrentSegmentId()
	{
		return m_logik.getCurrentSnippetId();
	}

	public int GetCurrentThemeId()
	{
		return m_logik.getEffectiveThemeId();
	}

	public int GetRemainingMillisecondsOfCurrentSegmentPlayback()
	{
		return m_logik.GetRemainingMillisecondsOfCurrentSegmentPlayback();
	}

	public int GetRemainingMillisecondsUntilNextSegmentStart()
	{
		return m_logik.GetRemainingMillisecondsUntilNextSegmentStart();
	}

	public PsaiResult MenuModeEnter(int menuThemeId, float menuThemeIntensity)
	{
		return m_logik.MenuModeEnter(menuThemeId, menuThemeIntensity);
	}

	public PsaiResult MenuModeLeave()
	{
		return m_logik.MenuModeLeave();
	}

	public bool MenuModeIsActive()
	{
		return m_logik.menuModeIsActive();
	}

	public bool CutSceneIsActive()
	{
		return m_logik.cutSceneIsActive();
	}

	public PsaiResult CutSceneEnter(int themeId, float intensity)
	{
		return m_logik.CutSceneEnter(themeId, intensity);
	}

	public PsaiResult CutSceneLeave(bool immediately, bool reset)
	{
		return m_logik.CutSceneLeave(immediately, reset);
	}

	public PsaiResult PlaySegment(int segmentId)
	{
		return m_logik.PlaySegmentLayeredAndImmediately(segmentId);
	}

	public bool CheckIfAtLeastOneDirectTransitionOrLayeringIsPossible(int sourceSegmentId, int targetThemeId)
	{
		return m_logik.CheckIfAtLeastOneDirectTransitionOrLayeringIsPossible(sourceSegmentId, targetThemeId);
	}

	public void SetLastBasicMood(int themeId)
	{
		m_logik.SetLastBasicMood(themeId);
	}

	public void Release()
	{
		m_logik.Release();
		m_logik = null;
	}
}
