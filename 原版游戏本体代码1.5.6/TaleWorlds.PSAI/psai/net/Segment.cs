using System;
using System.Collections.Generic;
using System.Text;

namespace psai.net;

public class Segment
{
	public AudioData audioData;

	public Dictionary<int, Segment> MapOfNextTransitionSegmentToTheme;

	private Dictionary<int, bool> _mapDirectTransitionToThemeIsPossible;

	private int _snippetTypeBitfield;

	public Segment nextSnippetToShortestEndSequence;

	public int Id { get; set; }

	public float Intensity { get; set; }

	public int ThemeId { get; set; }

	public string Name { get; set; }

	public int Playcount { get; set; }

	public int MaxPreBeatMsOfCompatibleSnippetsWithinSameTheme { get; set; }

	public List<Follower> Followers { get; private set; }

	public int SnippetTypeBitfield
	{
		get
		{
			return _snippetTypeBitfield;
		}
		set
		{
			_snippetTypeBitfield = value;
		}
	}

	public Segment()
	{
		Followers = new List<Follower>();
		_mapDirectTransitionToThemeIsPossible = new Dictionary<int, bool>();
		MapOfNextTransitionSegmentToTheme = new Dictionary<int, Segment>();
	}

	public override string ToString()
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append(Name);
		stringBuilder.Append(" (");
		stringBuilder.Append(Id);
		stringBuilder.Append(")");
		stringBuilder.Append(" ");
		stringBuilder.Append(GetStringFromSegmentSuitabilities(SnippetTypeBitfield));
		stringBuilder.Append(" [");
		stringBuilder.Append(Intensity.ToString("F2"));
		stringBuilder.Append("]");
		return stringBuilder.ToString();
	}

	public bool IsUsableAs(SegmentSuitability snippetType)
	{
		return (int)((uint)SnippetTypeBitfield & (uint)snippetType) > 0;
	}

	public bool IsUsableOnlyAs(SegmentSuitability snippetType)
	{
		return ((uint)SnippetTypeBitfield & (uint)snippetType) == (uint)snippetType;
	}

	private void SetSnippetTypeFlag(SegmentSuitability snippetType)
	{
		SnippetTypeBitfield |= (int)snippetType;
	}

	private void ClearSnippetTypeFlag(SegmentSuitability snippetType)
	{
		SnippetTypeBitfield &= (int)(~snippetType);
	}

	public Segment ReturnSegmentWithLowestIntensityDifference(List<Segment> argSnippets)
	{
		float num = 1f;
		Segment result = null;
		for (int i = 0; i < argSnippets.Count; i++)
		{
			Segment segment = argSnippets[i];
			if (segment != this)
			{
				float num2 = Math.Abs(segment.Intensity - Intensity);
				if (num2 == 0f)
				{
					return segment;
				}
				if (num2 < num)
				{
					num = num2;
					result = segment;
				}
			}
		}
		return result;
	}

	internal bool CheckIfAnyDirectOrIndirectTransitionIsPossible(Soundtrack soundtrack, int targetThemeId)
	{
		if (CheckIfAtLeastOneDirectTransitionOrLayeringIsPossible(soundtrack, targetThemeId))
		{
			return true;
		}
		return MapOfNextTransitionSegmentToTheme.ContainsKey(targetThemeId);
	}

	public bool CheckIfAtLeastOneDirectTransitionOrLayeringIsPossible(Soundtrack soundtrack, int targetThemeId)
	{
		if (_mapDirectTransitionToThemeIsPossible.TryGetValue(targetThemeId, out var value))
		{
			return value;
		}
		foreach (Follower follower in Followers)
		{
			if (soundtrack.GetSegmentById(follower.snippetId).ThemeId == targetThemeId)
			{
				_mapDirectTransitionToThemeIsPossible[targetThemeId] = true;
				return true;
			}
		}
		_mapDirectTransitionToThemeIsPossible[targetThemeId] = false;
		return false;
	}

	public static string GetStringFromSegmentSuitabilities(int snippetTypeBitfield)
	{
		StringBuilder stringBuilder = new StringBuilder(20);
		stringBuilder.Append("[ ");
		if (snippetTypeBitfield == 0)
		{
			stringBuilder.Append("NULL ");
		}
		if ((snippetTypeBitfield & 1) > 0)
		{
			stringBuilder.Append("START ");
		}
		if ((snippetTypeBitfield & 2) > 0)
		{
			stringBuilder.Append("MID ");
		}
		if ((snippetTypeBitfield & 8) > 0)
		{
			stringBuilder.Append("BRIDGE ");
		}
		if ((snippetTypeBitfield & 4) > 0)
		{
			stringBuilder.Append("END ");
		}
		stringBuilder.Append("]");
		return stringBuilder.ToString();
	}
}
