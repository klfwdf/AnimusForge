using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml.Serialization;
using psai.net;

namespace psai.Editor;

[Serializable]
public class Segment : PsaiMusicEntity, ICloneable
{
	private const int DEFAULT_SNIPPET_TYPES = 3;

	private const float DEFAULT_INTENSITY = 0.5f;

	private const float COMPATIBILITY_PERCENTAGE_SAME_GROUP = 1f;

	private const float COMPATIBILITY_PERCENTAGE_OTHER_GROUP = 0.5f;

	private float _intensity;

	[NonSerialized]
	private Dictionary<int, float> _compatibleSnippetsIds = new Dictionary<int, float>();

	[NonSerialized]
	private HashSet<Segment> _manuallyLinkedSnippets = new HashSet<Segment>();

	[NonSerialized]
	private HashSet<Segment> _manuallyBlockedSnippets = new HashSet<Segment>();

	public int Id { get; set; }

	public bool IsAutomaticBridgeSegment { get; set; }

	public float Intensity
	{
		get
		{
			return _intensity;
		}
		set
		{
			if (value >= 0f && value <= 1f)
			{
				_intensity = value;
			}
		}
	}

	public bool IsUsableAtStart { get; set; }

	public bool IsUsableInMiddle { get; set; }

	public bool IsUsableAtEnd { get; set; }

	public AudioData AudioData { get; set; }

	public bool CalculatePostAndPrebeatLengthBasedOnBeats
	{
		get
		{
			return AudioData.CalculatePostAndPrebeatLengthBasedOnBeats;
		}
		set
		{
			AudioData.CalculatePostAndPrebeatLengthBasedOnBeats = value;
		}
	}

	public int PreBeatLengthInSamples
	{
		get
		{
			return AudioData.PreBeatLengthInSamples;
		}
		set
		{
			AudioData.PreBeatLengthInSamples = value;
		}
	}

	public int PostBeatLengthInSamples
	{
		get
		{
			return AudioData.PostBeatLengthInSamples;
		}
		set
		{
			AudioData.PostBeatLengthInSamples = value;
		}
	}

	public float PreBeats
	{
		get
		{
			return AudioData.PreBeats;
		}
		set
		{
			AudioData.PreBeats = value;
		}
	}

	public float PostBeats
	{
		get
		{
			return AudioData.PostBeats;
		}
		set
		{
			AudioData.PostBeats = value;
		}
	}

	public float Bpm
	{
		get
		{
			return AudioData.Bpm;
		}
		set
		{
			AudioData.Bpm = value;
		}
	}

	public int SampleRate
	{
		get
		{
			return AudioData.SampleRate;
		}
		set
		{
			AudioData.SampleRate = value;
		}
	}

	public int TotalLengthInSamples => AudioData.TotalLengthInSamples;

	public int BitsPerSample
	{
		get
		{
			return AudioData.BitsPerSample;
		}
		set
		{
			AudioData.BitsPerSample = value;
		}
	}

	public int ThemeId { get; set; }

	public List<int> Serialization_ManuallyBlockedSegmentIds { get; set; }

	public List<int> Serialization_ManuallyLinkedSegmentIds { get; set; }

	public CompatibilityType DefaultCompatibiltyAsFollower { get; set; }

	[XmlIgnore]
	public Group Group { get; set; }

	[XmlIgnore]
	public HashSet<Segment> ManuallyLinkedSnippets
	{
		get
		{
			return _manuallyLinkedSnippets;
		}
		set
		{
			_manuallyLinkedSnippets = value;
		}
	}

	[XmlIgnore]
	public HashSet<Segment> ManuallyBlockedSnippets
	{
		get
		{
			return _manuallyBlockedSnippets;
		}
		set
		{
			_manuallyBlockedSnippets = value;
		}
	}

	[XmlIgnore]
	public Dictionary<int, float> CompatibleSnippetsIds => _compatibleSnippetsIds;

	public override List<PsaiMusicEntity> GetChildren()
	{
		return null;
	}

	public override string GetClassString()
	{
		return "Segment";
	}

	public Segment()
	{
		init();
	}

	public Segment(int id, string name, int snippetTypes, float intensity)
	{
		init();
		Id = id;
		SetStartMiddleEndPropertiesFromBitfield(snippetTypes);
		base.Name = name;
		Intensity = intensity;
	}

	public Segment(int id, AudioData audioData)
	{
		init();
		AudioData = audioData;
		Id = id;
		base.Name = Path.GetFileNameWithoutExtension(Path.GetFileName(audioData.FilePathRelativeToProjectDir));
	}

	private void init()
	{
		Id = 1;
		base.Name = "new segment";
		DefaultCompatibiltyAsFollower = CompatibilityType.allowed_implicitly;
		SetStartMiddleEndPropertiesFromBitfield(3);
		Intensity = 0.5f;
		AudioData = new AudioData();
	}

	public override object Clone()
	{
		Segment segment = (Segment)MemberwiseClone();
		segment.AudioData = (AudioData)AudioData.Clone();
		segment.ManuallyBlockedSnippets = new HashSet<Segment>();
		segment.ManuallyLinkedSnippets = new HashSet<Segment>();
		foreach (Segment manuallyBlockedSnippet in _manuallyBlockedSnippets)
		{
			segment._manuallyBlockedSnippets.Add(manuallyBlockedSnippet);
		}
		foreach (Segment manuallyLinkedSnippet in _manuallyLinkedSnippets)
		{
			segment._manuallyLinkedSnippets.Add(manuallyLinkedSnippet);
		}
		return segment;
	}

	public override string ToString()
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append("Segment '");
		stringBuilder.Append(base.Name);
		stringBuilder.Append("'");
		return stringBuilder.ToString();
	}

	public bool AddCompatibleSnippet(Segment snippet, float compatibility)
	{
		if (snippet != null && compatibility >= 0f && compatibility <= 1f)
		{
			_compatibleSnippetsIds[snippet.Id] = compatibility;
			return true;
		}
		return false;
	}

	public override bool PropertyDifferencesAffectCompatibilities(PsaiMusicEntity otherEntity)
	{
		if (otherEntity is Segment)
		{
			Segment segment = otherEntity as Segment;
			if (!IsUsableAtStart.Equals(segment.IsUsableAtStart))
			{
				return true;
			}
			if (!IsUsableInMiddle.Equals(segment.IsUsableInMiddle))
			{
				return true;
			}
			if (!IsUsableAtEnd.Equals(segment.IsUsableAtEnd))
			{
				return true;
			}
			if (!IsAutomaticBridgeSegment.Equals(segment.IsAutomaticBridgeSegment))
			{
				return true;
			}
			if (!DefaultCompatibiltyAsFollower.Equals(segment.DefaultCompatibiltyAsFollower))
			{
				return true;
			}
		}
		return false;
	}

	public void BuildCompatibleSegmentsSet(PsaiProject project)
	{
		HashSet<Segment> segmentsOfAllThemes = project.GetSegmentsOfAllThemes();
		CompatibleSnippetsIds.Clear();
		foreach (Segment item in segmentsOfAllThemes)
		{
			bool flag = false;
			CompatibilityReason reason = CompatibilityReason.not_set;
			if (GetCompatibilityType(item, out reason) switch
			{
				CompatibilityType.allowed_implicitly => true, 
				CompatibilityType.allowed_manually => true, 
				_ => false, 
			})
			{
				float compatibility = ((item.Group != Group) ? 0.5f : 1f);
				AddCompatibleSnippet(item, compatibility);
			}
		}
	}

	public void SetStartMiddleEndPropertiesFromBitfield(int bitfield)
	{
		IsUsableAtStart = ReadOutSegmentSuitabilityFlag(bitfield, SegmentSuitability.start);
		IsUsableInMiddle = ReadOutSegmentSuitabilityFlag(bitfield, SegmentSuitability.middle);
		IsUsableAtEnd = ReadOutSegmentSuitabilityFlag(bitfield, SegmentSuitability.end);
	}

	public int CreateSegmentSuitabilityBitfield(PsaiProject parentProject)
	{
		int bitfield = 0;
		if (IsAutomaticBridgeSegment || IsBridgeSnippetToAnyGroup(parentProject))
		{
			SetSegmentSuitabilityFlag(ref bitfield, SegmentSuitability.bridge);
		}
		if (IsUsableAtStart)
		{
			SetSegmentSuitabilityFlag(ref bitfield, SegmentSuitability.start);
		}
		if (IsUsableInMiddle)
		{
			SetSegmentSuitabilityFlag(ref bitfield, SegmentSuitability.middle);
		}
		if (IsUsableAtEnd)
		{
			SetSegmentSuitabilityFlag(ref bitfield, SegmentSuitability.end);
		}
		return bitfield;
	}

	public psai.net.Segment CreatePsaiDotNetVersion(PsaiProject parentProject)
	{
		psai.net.Segment segment = new psai.net.Segment();
		segment.audioData = AudioData.CreatePsaiDotNetVersion();
		segment.Id = Id;
		segment.Intensity = Intensity;
		segment.SnippetTypeBitfield = CreateSegmentSuitabilityBitfield(parentProject);
		segment.ThemeId = ThemeId;
		segment.Name = base.Name;
		segment.Followers.Capacity = CompatibleSnippetsIds.Count;
		foreach (KeyValuePair<int, float> compatibleSnippetsId in CompatibleSnippetsIds)
		{
			segment.Followers.Add(new Follower(compatibleSnippetsId.Key, compatibleSnippetsId.Value));
		}
		return segment;
	}

	public bool HasOnlyStartSuitability()
	{
		if (IsUsableAtStart && !IsUsableInMiddle)
		{
			return !IsUsableAtEnd;
		}
		return false;
	}

	public bool HasOnlyMiddleSuitability()
	{
		if (!IsUsableAtStart && IsUsableInMiddle)
		{
			return !IsUsableAtEnd;
		}
		return false;
	}

	public bool HasOnlyEndSuitability()
	{
		if (!IsUsableAtStart && !IsUsableInMiddle)
		{
			return IsUsableAtEnd;
		}
		return false;
	}

	public static bool ReadOutSegmentSuitabilityFlag(int bitfield, SegmentSuitability suitability)
	{
		return (int)((uint)bitfield & (uint)suitability) > 0;
	}

	public static void SetSegmentSuitabilityFlag(ref int bitfield, SegmentSuitability snippetType)
	{
		bitfield |= (int)snippetType;
	}

	public static void ClearSegmentSuitabilityFlag(ref int bitfield, SegmentSuitability snippetType)
	{
		bitfield &= (int)(~snippetType);
	}

	public bool IsBridgeSnippetToAnyGroup(PsaiProject project)
	{
		List<Group> groups = null;
		if (!IsAutomaticBridgeSegment)
		{
			return project.CheckIfSnippetIsManualBridgeSnippetToAnyGroup(this, getGroups: false, out groups);
		}
		return true;
	}

	public bool IsManualBridgeSnippetForAnyGroup(PsaiProject project)
	{
		List<Group> groups = null;
		return project.CheckIfSnippetIsManualBridgeSnippetToAnyGroup(this, getGroups: false, out groups);
	}

	public bool IsManualBridgeSegmentForSourceGroup(Group sourceGroup)
	{
		return sourceGroup.ManualBridgeSnippetsOfTargetGroups.Contains(this);
	}

	public override CompatibilitySetting GetCompatibilitySetting(PsaiMusicEntity targetEntity)
	{
		if (targetEntity is Segment)
		{
			if (ManuallyBlockedSnippets.Contains((Segment)targetEntity))
			{
				return CompatibilitySetting.blocked;
			}
			if (ManuallyLinkedSnippets.Contains((Segment)targetEntity))
			{
				return CompatibilitySetting.allowed;
			}
		}
		return CompatibilitySetting.neutral;
	}

	public override CompatibilityType GetCompatibilityType(PsaiMusicEntity targetEntity, out CompatibilityReason reason)
	{
		reason = CompatibilityReason.not_set;
		if (targetEntity is Segment)
		{
			Segment segment = (Segment)targetEntity;
			Group obj = Group;
			Group obj2 = segment.Group;
			if (obj.GetCompatibilityType(obj2, out reason) == CompatibilityType.logically_impossible)
			{
				return CompatibilityType.logically_impossible;
			}
			if (HasOnlyEndSuitability() && segment.HasOnlyEndSuitability())
			{
				reason = CompatibilityReason.target_segment_and_source_segment_are_both_only_usable_at_end;
				return CompatibilityType.logically_impossible;
			}
			if (obj != obj2 && segment.HasOnlyEndSuitability() && !segment.IsAutomaticBridgeSegment && !segment.IsManualBridgeSegmentForSourceGroup(obj))
			{
				reason = CompatibilityReason.target_segment_is_of_a_different_group_and_is_only_usable_at_end;
				return CompatibilityType.logically_impossible;
			}
			if (obj == obj2 && !segment.IsUsableInMiddle && !segment.IsUsableAtEnd && (segment.IsAutomaticBridgeSegment || segment.IsManualBridgeSegmentForSourceGroup(obj)))
			{
				reason = CompatibilityReason.target_segment_is_a_pure_bridge_segment_within_the_same_group;
				return CompatibilityType.blocked_implicitly;
			}
			if (ManuallyLinkedSnippets.Contains(segment))
			{
				reason = CompatibilityReason.manual_setting_within_same_hierarchy;
				return CompatibilityType.allowed_manually;
			}
			if (ManuallyBlockedSnippets.Contains(segment))
			{
				reason = CompatibilityReason.manual_setting_within_same_hierarchy;
				return CompatibilityType.blocked_manually;
			}
			if (obj != null && obj != obj2 && (obj2.ContainsAtLeastOneAutomaticBridgeSegment() || obj2.ContainsAtLeastOneManualBridgeSegmentForSourceGroup(obj)))
			{
				if (segment.IsManualBridgeSegmentForSourceGroup(obj))
				{
					reason = CompatibilityReason.target_segment_is_a_manual_bridge_segment_for_the_source_group;
					return CompatibilityType.allowed_manually;
				}
				if (segment.IsAutomaticBridgeSegment)
				{
					reason = CompatibilityReason.target_segment_is_an_automatic_bridge_segment;
					return CompatibilityType.allowed_implicitly;
				}
				reason = CompatibilityReason.target_group_contains_at_least_one_bridge_segment;
				return CompatibilityType.blocked_implicitly;
			}
			if (HasOnlyEndSuitability())
			{
				reason = CompatibilityReason.anything_may_be_played_after_a_pure_end_segment;
				return CompatibilityType.allowed_implicitly;
			}
			if (segment.DefaultCompatibiltyAsFollower == CompatibilityType.allowed_implicitly)
			{
				switch (Group.GetCompatibilityType(segment.Group, out reason))
				{
				case CompatibilityType.blocked_manually:
					reason = CompatibilityReason.manual_setting_of_parent_entity;
					return CompatibilityType.blocked_implicitly;
				case CompatibilityType.blocked_implicitly:
					return CompatibilityType.blocked_implicitly;
				case CompatibilityType.allowed_manually:
					reason = CompatibilityReason.manual_setting_of_parent_entity;
					return CompatibilityType.allowed_implicitly;
				default:
					reason = CompatibilityReason.inherited_from_parent_hierarchy;
					return CompatibilityType.allowed_implicitly;
				}
			}
			reason = CompatibilityReason.default_compatibility_of_the_target_segment_as_a_follower;
			return segment.DefaultCompatibiltyAsFollower;
		}
		return CompatibilityType.undefined;
	}

	public override PsaiMusicEntity GetParent()
	{
		return Group;
	}

	public override int GetIndexPositionWithinParentEntity(PsaiProject parentProject)
	{
		return Group.Segments.IndexOf(this);
	}

	public static Segment GetExampleSnippet1()
	{
		return new Segment
		{
			Name = "snippet1",
			Intensity = 0.5f
		};
	}

	public static Segment GetExampleSnippet2()
	{
		return new Segment
		{
			Name = "snippet2",
			Intensity = 0.6f
		};
	}

	public static Segment GetExampleSnippet3()
	{
		return new Segment
		{
			Name = "snippet3",
			Intensity = 0.7f
		};
	}

	public static Segment GetExampleSnippet4()
	{
		return new Segment
		{
			Name = "snippet4",
			Intensity = 0.7f
		};
	}
}
