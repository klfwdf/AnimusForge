namespace psai.net;

public struct PsaiInfo
{
	public PsaiState psaiState { get; private set; }

	public PsaiState upcomingPsaiState { get; private set; }

	public int lastBasicMoodThemeId { get; private set; }

	public int effectiveThemeId { get; private set; }

	public int upcomingThemeId { get; private set; }

	public float currentIntensity { get; private set; }

	public float upcomingIntensity { get; private set; }

	public int themesQueued { get; private set; }

	public int targetSegmentId { get; private set; }

	public bool intensityIsHeld { get; private set; }

	public bool returningToLastBasicMood { get; private set; }

	public int remainingMillisecondsInRestMode { get; private set; }

	public bool paused { get; private set; }

	public PsaiInfo(PsaiState m_psaiState, PsaiState m_upcomingPsaiState, int m_lastBasicMoodThemeId, int m_effectiveThemeId, int m_upcomingThemeId, float m_currentIntensity, float m_upcomingIntensity, int m_themesQueued, int m_targetSegmentId, bool m_intensityIsHeld, bool m_returningToLastBasicMood, int m_remainingMillisecondsInRestMode, bool m_paused)
	{
		psaiState = m_psaiState;
		upcomingPsaiState = m_upcomingPsaiState;
		lastBasicMoodThemeId = m_lastBasicMoodThemeId;
		effectiveThemeId = m_effectiveThemeId;
		upcomingThemeId = m_upcomingThemeId;
		currentIntensity = m_currentIntensity;
		upcomingIntensity = m_upcomingIntensity;
		themesQueued = m_themesQueued;
		targetSegmentId = m_targetSegmentId;
		intensityIsHeld = m_intensityIsHeld;
		returningToLastBasicMood = m_returningToLastBasicMood;
		remainingMillisecondsInRestMode = m_remainingMillisecondsInRestMode;
		paused = m_paused;
	}
}
