using System.Collections.Generic;

namespace psai.net;

public class Soundtrack
{
	public Dictionary<int, Theme> m_themes;

	public Dictionary<int, Segment> m_snippets;

	public Soundtrack()
	{
		m_themes = new Dictionary<int, Theme>();
		m_snippets = new Dictionary<int, Segment>();
	}

	public void Clear()
	{
		m_themes.Clear();
		m_snippets.Clear();
	}

	public Theme getThemeById(int id)
	{
		m_themes.TryGetValue(id, out var value);
		return value;
	}

	public Segment GetSegmentById(int id)
	{
		m_snippets.TryGetValue(id, out var value);
		return value;
	}

	public SoundtrackInfo getSoundtrackInfo()
	{
		SoundtrackInfo soundtrackInfo = new SoundtrackInfo();
		soundtrackInfo.themeCount = m_themes.Count;
		soundtrackInfo.themeIds = new int[m_themes.Count];
		int num = 0;
		foreach (int key in m_themes.Keys)
		{
			soundtrackInfo.themeIds[num] = key;
			num++;
		}
		return soundtrackInfo;
	}

	public ThemeInfo getThemeInfo(int themeId)
	{
		Theme themeById = getThemeById(themeId);
		if (themeById != null)
		{
			ThemeInfo themeInfo = new ThemeInfo();
			themeInfo.id = themeById.id;
			themeInfo.type = themeById.themeType;
			themeInfo.name = themeById.Name;
			themeInfo.segmentIds = new int[themeById.m_segments.Count];
			for (int i = 0; i < themeById.m_segments.Count; i++)
			{
				themeInfo.segmentIds[i] = themeById.m_segments[i].Id;
			}
			return themeInfo;
		}
		return null;
	}

	public SegmentInfo getSegmentInfo(int snippetId)
	{
		SegmentInfo segmentInfo = new SegmentInfo();
		Segment segmentById = GetSegmentById(snippetId);
		if (segmentById != null)
		{
			segmentInfo.id = segmentById.Id;
			segmentInfo.intensity = segmentById.Intensity;
			segmentInfo.segmentSuitabilitiesBitfield = segmentById.SnippetTypeBitfield;
			segmentInfo.themeId = segmentById.ThemeId;
			segmentInfo.playcount = segmentById.Playcount;
			segmentInfo.name = segmentById.Name;
			segmentInfo.fullLengthInMilliseconds = segmentById.audioData.GetFullLengthInMilliseconds();
			segmentInfo.preBeatLengthInMilliseconds = segmentById.audioData.GetPreBeatZoneInMilliseconds();
			segmentInfo.postBeatLengthInMilliseconds = segmentById.audioData.GetPostBeatZoneInMilliseconds();
		}
		return segmentInfo;
	}

	public void UpdateMaxPreBeatMsOfCompatibleMiddleOrBridgeSnippets()
	{
		foreach (Segment value in m_snippets.Values)
		{
			value.MaxPreBeatMsOfCompatibleSnippetsWithinSameTheme = 0;
			int count = value.Followers.Count;
			for (int i = 0; i < count; i++)
			{
				int snippetId = value.Followers[i].snippetId;
				Segment segmentById = GetSegmentById(snippetId);
				if (segmentById != null && (segmentById.SnippetTypeBitfield & 0xA) > 0)
				{
					int preBeatZoneInMilliseconds = segmentById.audioData.GetPreBeatZoneInMilliseconds();
					if (value.MaxPreBeatMsOfCompatibleSnippetsWithinSameTheme < preBeatZoneInMilliseconds)
					{
						value.MaxPreBeatMsOfCompatibleSnippetsWithinSameTheme = preBeatZoneInMilliseconds;
					}
				}
			}
		}
	}

	public void BuildAllIndirectionSequences()
	{
		foreach (Theme value in m_themes.Values)
		{
			value.BuildSequencesToEndSegmentForAllSnippets();
			foreach (Theme value2 in m_themes.Values)
			{
				if (value != value2 && value2.themeType != ThemeType.highlightLayer && Theme.ThemeInterruptionBehaviorRequiresEvaluationOfSegmentCompatibilities(Theme.GetThemeInterruptionBehavior(value.themeType, value2.themeType)))
				{
					value.BuildSequencesToTargetThemeForAllSegments(this, value2);
				}
			}
		}
	}
}
