using System.Collections.Generic;
using System.Text;

namespace psai.net;

public class Theme
{
	public int id;

	public string Name;

	public ThemeType themeType;

	public int priority;

	public int restSecondsMax;

	public int restSecondsMin;

	public List<Segment> m_segments;

	public float intensityAfterRest;

	public int musicDurationGeneral;

	public int musicDurationAfterRest;

	public Weighting weightings;

	public static bool ThemeInterruptionBehaviorRequiresEvaluationOfSegmentCompatibilities(ThemeInterruptionBehavior interruptionBehavior)
	{
		if (interruptionBehavior != ThemeInterruptionBehavior.immediately && interruptionBehavior != ThemeInterruptionBehavior.at_end_of_current_snippet && interruptionBehavior != ThemeInterruptionBehavior.layer)
		{
			return interruptionBehavior == ThemeInterruptionBehavior.never;
		}
		return true;
	}

	public static string ThemeTypeToString(ThemeType themeType)
	{
		return themeType switch
		{
			ThemeType.basicMood => "Basic Mood", 
			ThemeType.basicMoodAlt => "Mood Alteration", 
			ThemeType.dramaticEvent => "Dramatic Event", 
			ThemeType.action => "Action", 
			ThemeType.shock => "Shock", 
			ThemeType.highlightLayer => "Highlight Layer", 
			_ => "", 
		};
	}

	public static ThemeInterruptionBehavior GetThemeInterruptionBehavior(ThemeType sourceThemeType, ThemeType targetThemeType)
	{
		switch (sourceThemeType)
		{
		case ThemeType.basicMood:
			switch (targetThemeType)
			{
			case ThemeType.basicMood:
				return ThemeInterruptionBehavior.at_end_of_current_snippet;
			case ThemeType.basicMoodAlt:
				return ThemeInterruptionBehavior.at_end_of_current_snippet;
			case ThemeType.dramaticEvent:
				return ThemeInterruptionBehavior.immediately;
			case ThemeType.action:
				return ThemeInterruptionBehavior.immediately;
			case ThemeType.shock:
				return ThemeInterruptionBehavior.immediately;
			case ThemeType.highlightLayer:
				return ThemeInterruptionBehavior.layer;
			}
			break;
		case ThemeType.basicMoodAlt:
			switch (targetThemeType)
			{
			case ThemeType.basicMood:
				return ThemeInterruptionBehavior.never;
			case ThemeType.basicMoodAlt:
				return ThemeInterruptionBehavior.at_end_of_current_snippet;
			case ThemeType.dramaticEvent:
				return ThemeInterruptionBehavior.immediately;
			case ThemeType.action:
				return ThemeInterruptionBehavior.immediately;
			case ThemeType.shock:
				return ThemeInterruptionBehavior.immediately;
			case ThemeType.highlightLayer:
				return ThemeInterruptionBehavior.layer;
			}
			break;
		case ThemeType.dramaticEvent:
			switch (targetThemeType)
			{
			case ThemeType.basicMood:
				return ThemeInterruptionBehavior.never;
			case ThemeType.basicMoodAlt:
				return ThemeInterruptionBehavior.never;
			case ThemeType.dramaticEvent:
				return ThemeInterruptionBehavior.at_end_of_current_snippet;
			case ThemeType.action:
				return ThemeInterruptionBehavior.immediately;
			case ThemeType.shock:
				return ThemeInterruptionBehavior.immediately;
			case ThemeType.highlightLayer:
				return ThemeInterruptionBehavior.layer;
			}
			break;
		case ThemeType.action:
			switch (targetThemeType)
			{
			case ThemeType.basicMood:
				return ThemeInterruptionBehavior.never;
			case ThemeType.basicMoodAlt:
				return ThemeInterruptionBehavior.never;
			case ThemeType.dramaticEvent:
				return ThemeInterruptionBehavior.never;
			case ThemeType.action:
				return ThemeInterruptionBehavior.at_end_of_current_snippet;
			case ThemeType.shock:
				return ThemeInterruptionBehavior.immediately;
			case ThemeType.highlightLayer:
				return ThemeInterruptionBehavior.layer;
			}
			break;
		case ThemeType.shock:
			switch (targetThemeType)
			{
			case ThemeType.basicMood:
				return ThemeInterruptionBehavior.never;
			case ThemeType.basicMoodAlt:
				return ThemeInterruptionBehavior.never;
			case ThemeType.dramaticEvent:
				return ThemeInterruptionBehavior.never;
			case ThemeType.action:
				return ThemeInterruptionBehavior.never;
			case ThemeType.shock:
				return ThemeInterruptionBehavior.immediately;
			case ThemeType.highlightLayer:
				return ThemeInterruptionBehavior.layer;
			}
			break;
		case ThemeType.highlightLayer:
			return ThemeInterruptionBehavior.never;
		}
		return ThemeInterruptionBehavior.undefined;
	}

	public Theme()
	{
		m_segments = new List<Segment>();
		weightings = new Weighting();
		id = -1;
		restSecondsMax = 0;
		restSecondsMin = 0;
		priority = 0;
		themeType = ThemeType.none;
		Name = "";
	}

	public override string ToString()
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append(Name);
		stringBuilder.Append(" (");
		stringBuilder.Append(id);
		stringBuilder.Append(")");
		stringBuilder.Append(" [");
		stringBuilder.Append(themeType);
		stringBuilder.Append("]");
		return stringBuilder.ToString();
	}

	internal void BuildSequencesToEndSegmentForAllSnippets()
	{
		foreach (Segment segment in m_segments)
		{
			segment.nextSnippetToShortestEndSequence = null;
		}
		List<Segment> list = new List<Segment>();
		foreach (Segment segment2 in m_segments)
		{
			if ((segment2.SnippetTypeBitfield & 4) > 0)
			{
				list.Add(segment2);
			}
		}
		SetTheNextSnippetToShortestEndSequenceForAllSourceSnippetsOfTheSnippetsInThisList(list.ToArray());
	}

	private void SetTheNextSnippetToShortestEndSequenceForAllSourceSnippetsOfTheSnippetsInThisList(Segment[] listOfSnippetsWithValidEndSequences)
	{
		Dictionary<Segment, List<Segment>> dictionary = new Dictionary<Segment, List<Segment>>();
		foreach (Segment segment in listOfSnippetsWithValidEndSequences)
		{
			foreach (Segment item in GetSetOfAllSourceSegmentsCompatibleToSegment(segment, 1f, SegmentSuitability.end))
			{
				if (item.nextSnippetToShortestEndSequence == null && item.ThemeId == segment.ThemeId)
				{
					if (dictionary.TryGetValue(item, out var value))
					{
						value.Add(segment);
						continue;
					}
					List<Segment> list = new List<Segment>();
					list.Add(segment);
					dictionary[item] = list;
				}
			}
		}
		foreach (Segment key in dictionary.Keys)
		{
			key.nextSnippetToShortestEndSequence = key.ReturnSegmentWithLowestIntensityDifference(dictionary[key]);
		}
		Segment[] array = new Segment[dictionary.Count];
		dictionary.Keys.CopyTo(array, 0);
		if (array.Length != 0)
		{
			SetTheNextSnippetToShortestEndSequenceForAllSourceSnippetsOfTheSnippetsInThisList(array);
		}
	}

	internal void BuildSequencesToTargetThemeForAllSegments(Soundtrack soundtrack, Theme targetTheme)
	{
		foreach (Segment segment in m_segments)
		{
			segment.MapOfNextTransitionSegmentToTheme.Remove(targetTheme.id);
		}
		List<Segment> list = new List<Segment>();
		foreach (Segment segment2 in m_segments)
		{
			if (segment2.CheckIfAtLeastOneDirectTransitionOrLayeringIsPossible(soundtrack, targetTheme.id))
			{
				list.Add(segment2);
			}
		}
		SetTheNextSegmentToShortestTransitionSequenceToTargetThemeForAllSourceSegmentsOfTheSegmentsInThisList(list.ToArray(), soundtrack, targetTheme);
	}

	private List<Segment> GetSetOfAllSourceSegmentsCompatibleToSegment(Segment targetSnippet, float minCompatibilityThreshold, SegmentSuitability doNotIncludeSegmentsWithThisSuitability)
	{
		List<Segment> list = new List<Segment>();
		foreach (Segment segment in m_segments)
		{
			if (segment.IsUsableAs(doNotIncludeSegmentsWithThisSuitability))
			{
				continue;
			}
			foreach (Follower follower in segment.Followers)
			{
				if (follower.snippetId == targetSnippet.Id && follower.compatibility >= minCompatibilityThreshold)
				{
					list.Add(segment);
				}
			}
		}
		return list;
	}

	private void SetTheNextSegmentToShortestTransitionSequenceToTargetThemeForAllSourceSegmentsOfTheSegmentsInThisList(Segment[] listOfSnippetsWithValidTransitionSequencesToTargetTheme, Soundtrack soundtrack, Theme targetTheme)
	{
		Dictionary<Segment, List<Segment>> dictionary = new Dictionary<Segment, List<Segment>>();
		foreach (Segment segment in listOfSnippetsWithValidTransitionSequencesToTargetTheme)
		{
			List<Segment> setOfAllSourceSegmentsCompatibleToSegment = GetSetOfAllSourceSegmentsCompatibleToSegment(segment, 1f, SegmentSuitability.none);
			setOfAllSourceSegmentsCompatibleToSegment.Remove(segment);
			foreach (Segment item in setOfAllSourceSegmentsCompatibleToSegment)
			{
				if (item.ThemeId == segment.ThemeId && !item.MapOfNextTransitionSegmentToTheme.ContainsKey(targetTheme.id) && !item.CheckIfAtLeastOneDirectTransitionOrLayeringIsPossible(soundtrack, targetTheme.id))
				{
					if (dictionary.TryGetValue(item, out var value))
					{
						value.Add(segment);
						continue;
					}
					List<Segment> list = new List<Segment>();
					list.Add(segment);
					dictionary[item] = list;
				}
			}
		}
		foreach (Segment key in dictionary.Keys)
		{
			key.MapOfNextTransitionSegmentToTheme[targetTheme.id] = key.ReturnSegmentWithLowestIntensityDifference(dictionary[key]);
		}
		Segment[] array = new Segment[dictionary.Count];
		dictionary.Keys.CopyTo(array, 0);
		if (array.Length != 0)
		{
			SetTheNextSegmentToShortestTransitionSequenceToTargetThemeForAllSourceSegmentsOfTheSegmentsInThisList(array, soundtrack, targetTheme);
		}
	}
}
