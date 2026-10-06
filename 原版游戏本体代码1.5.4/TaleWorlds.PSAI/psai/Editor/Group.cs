using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace psai.Editor;

[Serializable]
public class Group : PsaiMusicEntity, ICloneable
{
	[NonSerialized]
	private List<Segment> m_segments = new List<Segment>();

	[NonSerialized]
	private HashSet<Segment> _manualBridgeSnippetsOfTargetGroups = new HashSet<Segment>();

	[NonSerialized]
	private HashSet<Group> _manuallyBlockedGroups = new HashSet<Group>();

	[NonSerialized]
	private HashSet<Group> _manuallyLinkedGroups = new HashSet<Group>();

	private string _description = "";

	public List<Segment> Segments
	{
		get
		{
			return m_segments;
		}
		set
		{
			m_segments = value;
		}
	}

	public string Description
	{
		get
		{
			return _description;
		}
		set
		{
			_description = value;
		}
	}

	public int Serialization_Id { get; set; }

	public List<int> Serialization_ManuallyBlockedGroupIds { get; set; }

	public List<int> Serialization_ManuallyLinkedGroupIds { get; set; }

	public List<int> Serialization_ManualBridgeSegmentIds { get; set; }

	[XmlIgnore]
	public HashSet<Segment> ManualBridgeSnippetsOfTargetGroups
	{
		get
		{
			return _manualBridgeSnippetsOfTargetGroups;
		}
		set
		{
			_manualBridgeSnippetsOfTargetGroups = value;
		}
	}

	[XmlIgnore]
	public HashSet<Group> ManuallyBlockedGroups
	{
		get
		{
			return _manuallyBlockedGroups;
		}
		set
		{
			_manuallyBlockedGroups = value;
		}
	}

	[XmlIgnore]
	public HashSet<Group> ManuallyLinkedGroups
	{
		get
		{
			return _manuallyLinkedGroups;
		}
		set
		{
			_manuallyLinkedGroups = value;
		}
	}

	[XmlIgnore]
	public Theme Theme { get; set; }

	public override string GetClassString()
	{
		return "Group";
	}

	public Group()
	{
		base.Name = "new_group";
	}

	public Group(Theme parentTheme, string name)
		: this()
	{
		Theme = parentTheme;
		base.Name = name;
	}

	public Group(Theme parentTheme)
		: this()
	{
		Theme = parentTheme;
	}

	~Group()
	{
	}

	public void AddSegment(Segment snippet)
	{
		AddSnippet_internal(snippet, -1);
	}

	public void AddSegment(Segment snippet, int index)
	{
		AddSnippet_internal(snippet, index);
	}

	private void AddSnippet_internal(Segment snippet, int insertIndex)
	{
		snippet.Group = this;
		if (Theme != null)
		{
			snippet.ThemeId = Theme.Id;
		}
		if (insertIndex < 0 || insertIndex >= m_segments.Count)
		{
			m_segments.Add(snippet);
		}
		else
		{
			m_segments.Insert(insertIndex, snippet);
		}
	}

	public void RemoveSegment(Segment snippet)
	{
		m_segments.Remove(snippet);
	}

	public bool HasAtLeastOneBridgeSegmentToTargetGroup(Group targetGroup)
	{
		foreach (Segment manualBridgeSnippetsOfTargetGroup in _manualBridgeSnippetsOfTargetGroups)
		{
			if (manualBridgeSnippetsOfTargetGroup.Group == targetGroup)
			{
				return true;
			}
		}
		return targetGroup.ContainsAtLeastOneAutomaticBridgeSegment();
	}

	public bool ContainsAtLeastOneManualBridgeSegmentForSourceGroup(Group sourceGroup)
	{
		foreach (Segment manualBridgeSnippetsOfTargetGroup in sourceGroup.ManualBridgeSnippetsOfTargetGroups)
		{
			if (manualBridgeSnippetsOfTargetGroup.Group == this)
			{
				return true;
			}
		}
		return false;
	}

	public bool ContainsAtLeastOneAutomaticBridgeSegment()
	{
		foreach (Segment segment in Segments)
		{
			if (segment.IsAutomaticBridgeSegment)
			{
				return true;
			}
		}
		return false;
	}

	public override string ToString()
	{
		return "Group '" + base.Name + "' (" + Theme.Name + ")";
	}

	public override CompatibilitySetting GetCompatibilitySetting(PsaiMusicEntity targetEntity)
	{
		if (targetEntity is Group)
		{
			if (ManuallyBlockedGroups.Contains((Group)targetEntity))
			{
				return CompatibilitySetting.blocked;
			}
			if (ManuallyLinkedGroups.Contains((Group)targetEntity))
			{
				return CompatibilitySetting.allowed;
			}
		}
		return CompatibilitySetting.neutral;
	}

	public override CompatibilityType GetCompatibilityType(PsaiMusicEntity targetEntity, out CompatibilityReason reason)
	{
		if (targetEntity is Group)
		{
			Group obj = (Group)targetEntity;
			Theme theme = Theme;
			Theme theme2 = obj.Theme;
			if (theme.GetCompatibilityType(theme2, out reason) == CompatibilityType.logically_impossible)
			{
				return CompatibilityType.logically_impossible;
			}
			if (ManuallyBlockedGroups.Contains(obj))
			{
				reason = CompatibilityReason.manual_setting_within_same_hierarchy;
				return CompatibilityType.blocked_manually;
			}
			if (ManuallyLinkedGroups.Contains(obj))
			{
				reason = CompatibilityReason.manual_setting_within_same_hierarchy;
				return CompatibilityType.allowed_manually;
			}
			CompatibilityType compatibilityType = Theme.GetCompatibilityType(obj.Theme, out reason);
			switch (compatibilityType)
			{
			case CompatibilityType.blocked_manually:
				reason = CompatibilityReason.manual_setting_of_parent_entity;
				return CompatibilityType.blocked_implicitly;
			case CompatibilityType.allowed_manually:
				reason = CompatibilityReason.manual_setting_of_parent_entity;
				return CompatibilityType.allowed_implicitly;
			default:
				reason = CompatibilityReason.inherited_from_parent_hierarchy;
				return compatibilityType;
			}
		}
		reason = CompatibilityReason.not_set;
		return CompatibilityType.undefined;
	}

	public void SetAsParentGroupForAllSegments()
	{
		foreach (Segment segment in Segments)
		{
			segment.Group = this;
		}
	}

	public override PsaiMusicEntity GetParent()
	{
		return Theme;
	}

	public override List<PsaiMusicEntity> GetChildren()
	{
		List<PsaiMusicEntity> list = new List<PsaiMusicEntity>();
		list.Capacity = m_segments.Count;
		for (int i = 0; i < m_segments.Count; i++)
		{
			list.Add(m_segments[i]);
		}
		return list;
	}

	public override int GetIndexPositionWithinParentEntity(PsaiProject parentProject)
	{
		return Theme.Groups.IndexOf(this);
	}

	public override object Clone()
	{
		Group obj = (Group)MemberwiseClone();
		obj.Segments = new List<Segment>();
		obj.ManualBridgeSnippetsOfTargetGroups = new HashSet<Segment>();
		obj.ManuallyBlockedGroups = new HashSet<Group>();
		obj.ManuallyLinkedGroups = new HashSet<Group>();
		foreach (Segment segment in Segments)
		{
			obj.AddSegment((Segment)segment.Clone());
		}
		obj.ManuallyBlockedGroups = new HashSet<Group>();
		obj.ManuallyLinkedGroups = new HashSet<Group>();
		foreach (Group manuallyBlockedGroup in ManuallyBlockedGroups)
		{
			obj.ManuallyBlockedGroups.Add(manuallyBlockedGroup);
		}
		foreach (Group manuallyLinkedGroup in ManuallyLinkedGroups)
		{
			obj.ManuallyLinkedGroups.Add(manuallyLinkedGroup);
		}
		return obj;
	}

	public override PsaiMusicEntity ShallowCopy()
	{
		Group obj = (Group)MemberwiseClone();
		foreach (Segment segment in Segments)
		{
			segment.Group = obj;
		}
		return obj;
	}
}
