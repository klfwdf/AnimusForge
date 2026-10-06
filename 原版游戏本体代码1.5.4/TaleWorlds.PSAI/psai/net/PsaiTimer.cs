namespace psai.net;

internal class PsaiTimer
{
	private bool m_isSet;

	private bool m_isPaused;

	private int m_estimatedThresholdReachedTime;

	private int m_estimatedFireTime;

	private int m_timerPausedTimestamp;

	internal PsaiTimer()
	{
		m_isSet = false;
	}

	internal void SetTimer(int delayMillis, int remainingThresholdMilliseconds)
	{
		m_estimatedFireTime = Logik.GetTimestampMillisElapsedSinceInitialisation() + delayMillis;
		m_estimatedThresholdReachedTime = m_estimatedFireTime - remainingThresholdMilliseconds;
		m_isSet = true;
	}

	internal bool IsSet()
	{
		return m_isSet;
	}

	internal void Stop()
	{
		m_isSet = false;
	}

	internal void SetPaused(bool setPaused)
	{
		if (!m_isSet)
		{
			return;
		}
		if (setPaused)
		{
			if (!m_isPaused)
			{
				m_isPaused = true;
				m_timerPausedTimestamp = Logik.GetTimestampMillisElapsedSinceInitialisation();
			}
		}
		else if (m_isPaused)
		{
			m_isPaused = false;
			int num = Logik.GetTimestampMillisElapsedSinceInitialisation() - m_timerPausedTimestamp;
			m_estimatedFireTime += num;
			m_estimatedThresholdReachedTime += num;
		}
	}

	internal int GetRemainingMillisToFireTime()
	{
		if (m_isSet && !m_isPaused)
		{
			return m_estimatedFireTime - Logik.GetTimestampMillisElapsedSinceInitialisation();
		}
		return 999999;
	}

	internal int GetEstimatedFireTime()
	{
		if (m_isSet && !m_isPaused)
		{
			return m_estimatedFireTime;
		}
		return 999999;
	}

	internal bool ThresholdHasBeenReached()
	{
		if (m_isSet)
		{
			return Logik.GetTimestampMillisElapsedSinceInitialisation() >= m_estimatedThresholdReachedTime;
		}
		return false;
	}
}
