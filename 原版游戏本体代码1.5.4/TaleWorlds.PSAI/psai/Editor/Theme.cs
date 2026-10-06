using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using psai.net;

namespace psai.Editor;

[Serializable]
public class Theme : PsaiMusicEntity, ICloneable
{
	internal static float PLAYCOUNT_VS_RANDOM_WEIGHTING_IF_PLAYCOUNT_PREFERRED = 0.8f;

	private static readonly string DEFAULT_NAME = "new_theme";

	private static readonly int DEFAULT_PRIORITY = 1;

	private static readonly int DEFAULT_REST_SECONDS_MIN = 30;

	private static readonly int DEFAULT_REST_SECONDS_MAX = 60;

	private static readonly int DEFAULT_FADEOUT_MS = 20;

	private static readonly int DEFAULT_THEME_DURATION_SECONDS = 60;

	private static readonly float DEFAULT_INTENSITY_AFTER_REST = 0.5f;

	private static readonly int DEFAULT_THEME_DURATION_SECONDS_AFTER_REST = 40;

	private static readonly float DEFAULT_WEIGHTING_COMPATIBILITY = 0.5f;

	private static readonly float DEFAULT_WEIGHTING_INTENSITY = 0.5f;

	private static readonly float DEFAULT_WEIGHTING_LOW_PLAYCOUNT_VS_RANDOM = 0f;

	private static readonly int DEFAULT_THEMETYPEINT = 1;

	private List<Group> _groups;

	private HashSet<Theme> _manuallyBlockedThemes = new HashSet<Theme>();

	private float _intensityAfterRest;

	private int _id;

	public int Id
	{
		get
		{
			return _id;
		}
		set
		{
			_id = value;
			SetAsParentThemeForAllGroupsAndSegments();
		}
	}

	public string Description { get; set; }

	public int ThemeTypeInt { get; set; }

	public List<int> Serialization_ManuallyBlockedThemeIds { get; set; }

	[XmlIgnore]
	public HashSet<Theme> ManuallyBlockedTargetThemes
	{
		get
		{
			return _manuallyBlockedThemes;
		}
		private set
		{
			_manuallyBlockedThemes = value;
		}
	}

	public float IntensityAfterRest
	{
		get
		{
			return _intensityAfterRest;
		}
		set
		{
			_intensityAfterRest = value;
			if (_intensityAfterRest <= 0f)
			{
				_intensityAfterRest = 0.01f;
			}
		}
	}

	public int MusicPhaseSecondsAfterRest { get; set; }

	public int MusicPhaseSecondsGeneral { get; set; }

	public int RestSecondsMin { get; set; }

	public int RestSecondsMax { get; set; }

	public int FadeoutMs { get; set; }

	public int Priority { get; set; }

	public float WeightingSwitchGroups { get; set; }

	public float WeightingIntensityVsVariance { get; set; }

	public float WeightingLowPlaycountVsRandom { get; set; }

	public List<Group> Groups
	{
		get
		{
			return _groups;
		}
		set
		{
			_groups = value;
		}
	}

	public static bool ConvertPlaycountVsRandomWeightingToBooleanPlaycountPreferred(float weightingPlaycountVsRandom)
	{
		return weightingPlaycountVsRandom >= PLAYCOUNT_VS_RANDOM_WEIGHTING_IF_PLAYCOUNT_PREFERRED;
	}

	public override string GetClassString()
	{
		if (ThemeTypeInt == 6)
		{
			return "Highlight Layer";
		}
		return "Theme";
	}

	public override List<PsaiMusicEntity> GetChildren()
	{
		List<PsaiMusicEntity> list = new List<PsaiMusicEntity>();
		for (int i = 0; i < Groups.Count; i++)
		{
			list.Add(_groups[i]);
		}
		return list;
	}

	public Theme()
	{
		Initialize();
	}

	public Theme(int id)
	{
		Initialize();
		Id = id;
	}

	public Theme(int id, string name)
	{
		Initialize();
		Id = id;
		base.Name = name;
	}

	public override PsaiMusicEntity GetParent()
	{
		return null;
	}

	private void Initialize()
	{
		base.Name = DEFAULT_NAME;
		ThemeTypeInt = DEFAULT_THEMETYPEINT;
		IntensityAfterRest = DEFAULT_INTENSITY_AFTER_REST;
		MusicPhaseSecondsAfterRest = DEFAULT_THEME_DURATION_SECONDS;
		MusicPhaseSecondsGeneral = DEFAULT_THEME_DURATION_SECONDS_AFTER_REST;
		WeightingSwitchGroups = DEFAULT_WEIGHTING_COMPATIBILITY;
		WeightingIntensityVsVariance = DEFAULT_WEIGHTING_INTENSITY;
		WeightingLowPlaycountVsRandom = DEFAULT_WEIGHTING_LOW_PLAYCOUNT_VS_RANDOM;
		Priority = DEFAULT_PRIORITY;
		RestSecondsMin = DEFAULT_REST_SECONDS_MIN;
		RestSecondsMax = DEFAULT_REST_SECONDS_MAX;
		FadeoutMs = DEFAULT_FADEOUT_MS;
		_groups = new List<Group>();
	}

	public override string ToString()
	{
		return "Theme '" + base.Name + "'";
	}

	public bool AddGroup(Group groupToAdd)
	{
		foreach (Group group in _groups)
		{
			if (group.Name.Equals(groupToAdd.Name))
			{
				return false;
			}
		}
		_groups.Add(groupToAdd);
		foreach (Segment segmentsOfAllGroup in GetSegmentsOfAllGroups())
		{
			segmentsOfAllGroup.ThemeId = Id;
		}
		return true;
	}

	public void DeleteGroup(Group group)
	{
		if (group != _groups[0])
		{
			_groups.Remove(group);
		}
	}

	public HashSet<Segment> GetSegmentsOfAllGroups()
	{
		HashSet<Segment> hashSet = new HashSet<Segment>();
		foreach (Group group in _groups)
		{
			foreach (Segment segment in group.Segments)
			{
				hashSet.Add(segment);
			}
		}
		return hashSet;
	}

	public HashSet<string> GetAudioDataRelativeFilePathsUsedByThisTheme()
	{
		HashSet<string> hashSet = new HashSet<string>();
		foreach (Segment segmentsOfAllGroup in GetSegmentsOfAllGroups())
		{
			if (!hashSet.Contains(segmentsOfAllGroup.AudioData.FilePathRelativeToProjectDir))
			{
				hashSet.Add(segmentsOfAllGroup.AudioData.FilePathRelativeToProjectDir);
			}
		}
		return hashSet;
	}

	public override CompatibilitySetting GetCompatibilitySetting(PsaiMusicEntity targetEntity)
	{
		if (targetEntity is Theme && ManuallyBlockedTargetThemes.Contains((Theme)targetEntity))
		{
			return CompatibilitySetting.blocked;
		}
		return CompatibilitySetting.neutral;
	}

	public override CompatibilityType GetCompatibilityType(PsaiMusicEntity targetEntity, out CompatibilityReason reason)
	{
		if (targetEntity is Theme)
		{
			Theme theme = targetEntity as Theme;
			ThemeInterruptionBehavior themeInterruptionBehavior = psai.net.Theme.GetThemeInterruptionBehavior((ThemeType)ThemeTypeInt, (ThemeType)theme.ThemeTypeInt);
			if (psai.net.Theme.ThemeInterruptionBehaviorRequiresEvaluationOfSegmentCompatibilities(themeInterruptionBehavior))
			{
				if (ManuallyBlockedTargetThemes.Contains(targetEntity as Theme))
				{
					reason = CompatibilityReason.manual_setting_within_same_hierarchy;
					return CompatibilityType.blocked_manually;
				}
				reason = CompatibilityReason.default_behavior_of_psai;
				return CompatibilityType.allowed_implicitly;
			}
			if (themeInterruptionBehavior == ThemeInterruptionBehavior.never)
			{
				reason = CompatibilityReason.target_theme_will_never_interrupt_source;
				return CompatibilityType.logically_impossible;
			}
		}
		reason = CompatibilityReason.not_set;
		return CompatibilityType.undefined;
	}

	public override int GetIndexPositionWithinParentEntity(PsaiProject parentProject)
	{
		return parentProject.Themes.IndexOf(this);
	}

	public override bool PropertyDifferencesAffectCompatibilities(PsaiMusicEntity otherEntity)
	{
		if (otherEntity is Theme)
		{
			Theme theme = otherEntity as Theme;
			if (ThemeTypeInt != theme.ThemeTypeInt)
			{
				return true;
			}
		}
		return false;
	}

	public void SetAsParentThemeForAllGroupsAndSegments()
	{
		foreach (Group group in Groups)
		{
			group.Theme = this;
		}
		foreach (Segment segmentsOfAllGroup in GetSegmentsOfAllGroups())
		{
			segmentsOfAllGroup.ThemeId = Id;
		}
	}

	public psai.net.Theme CreatePsaiDotNetVersion()
	{
		return new psai.net.Theme
		{
			id = Id,
			Name = base.Name,
			themeType = (ThemeType)ThemeTypeInt,
			intensityAfterRest = IntensityAfterRest,
			musicDurationGeneral = MusicPhaseSecondsGeneral,
			musicDurationAfterRest = MusicPhaseSecondsAfterRest,
			restSecondsMin = RestSecondsMin,
			restSecondsMax = RestSecondsMax,
			priority = Priority,
			weightings = 
			{
				switchGroups = WeightingSwitchGroups,
				intensityVsVariety = WeightingIntensityVsVariance,
				lowPlaycountVsRandom = WeightingLowPlaycountVsRandom
			}
		};
	}

	public static Theme getTestTheme1()
	{
		Theme obj = new Theme(1, "Forest")
		{
			ThemeTypeInt = 1
		};
		Group obj2 = new Group(obj, "wald_streicher");
		Group obj3 = new Group(obj, "wald_choir");
		Segment snippet = new Segment(101, "wald_streicher_1", 1, 0.4f);
		Segment snippet2 = new Segment(102, "wald_streicher_2", 2, 0.4f);
		Segment snippet3 = new Segment(103, "wald_streicher_3", 2, 0.6f);
		Segment snippet4 = new Segment(104, "wald_streicher_4", 4, 0.6f);
		Segment snippet5 = new Segment(105, "wald_streicher_5", 1, 1f);
		Segment snippet6 = new Segment(106, "wald_streicher_6", 4, 1f);
		Segment snippet7 = new Segment(111, "wald_choir_1", 1, 0.4f);
		Segment snippet8 = new Segment(112, "wald_choir_2", 2, 0.4f);
		Segment snippet9 = new Segment(113, "wald_choir_3", 2, 0.6f);
		Segment snippet10 = new Segment(114, "wald_choir_4", 1, 0.6f);
		Segment snippet11 = new Segment(115, "wald_choir_5", 4, 1f);
		Segment snippet12 = new Segment(116, "wald_choir_6", 4, 1f);
		obj2.AddSegment(snippet);
		obj2.AddSegment(snippet2);
		obj2.AddSegment(snippet3);
		obj2.AddSegment(snippet4);
		obj2.AddSegment(snippet5);
		obj2.AddSegment(snippet6);
		obj3.AddSegment(snippet7);
		obj3.AddSegment(snippet8);
		obj3.AddSegment(snippet9);
		obj3.AddSegment(snippet10);
		obj3.AddSegment(snippet11);
		obj3.AddSegment(snippet12);
		obj.AddGroup(obj2);
		obj.AddGroup(obj3);
		return obj;
	}

	public static Theme getTestTheme2()
	{
		Theme obj = new Theme(2, "Cave")
		{
			ThemeTypeInt = 1
		};
		Group obj2 = new Group(obj, "cave horns");
		Group obj3 = new Group(obj, "cave choir");
		Segment snippet = new Segment(201, "cave_horns_1", 1, 0.4f);
		Segment snippet2 = new Segment(202, "cave_horns_2", 2, 0.4f);
		Segment snippet3 = new Segment(203, "cave_horns_3", 2, 0.6f);
		Segment snippet4 = new Segment(204, "cave_horns_4", 2, 0.6f);
		Segment snippet5 = new Segment(205, "cave_horns_5", 2, 1f);
		Segment snippet6 = new Segment(206, "cave_horns_6", 4, 1f);
		Segment snippet7 = new Segment(211, "cave_choir_1", 1, 0.4f);
		Segment snippet8 = new Segment(212, "cave_choir_2", 2, 0.4f);
		Segment snippet9 = new Segment(213, "cave_choir_3", 2, 0.6f);
		Segment snippet10 = new Segment(214, "cave_choir_4", 2, 0.6f);
		Segment snippet11 = new Segment(215, "cave_choir_5", 2, 1f);
		Segment snippet12 = new Segment(216, "cave_choir_6", 4, 1f);
		obj2.AddSegment(snippet);
		obj2.AddSegment(snippet2);
		obj2.AddSegment(snippet3);
		obj2.AddSegment(snippet4);
		obj2.AddSegment(snippet5);
		obj2.AddSegment(snippet6);
		obj3.AddSegment(snippet7);
		obj3.AddSegment(snippet8);
		obj3.AddSegment(snippet9);
		obj3.AddSegment(snippet10);
		obj3.AddSegment(snippet11);
		obj3.AddSegment(snippet12);
		obj.AddGroup(obj2);
		obj.AddGroup(obj3);
		return obj;
	}

	public override object Clone()
	{
		Theme theme = (Theme)MemberwiseClone();
		theme.Groups = new List<Group>();
		theme._manuallyBlockedThemes = new HashSet<Theme>();
		foreach (Group group in Groups)
		{
			theme.AddGroup((Group)group.Clone());
		}
		foreach (Theme manuallyBlockedTheme in _manuallyBlockedThemes)
		{
			theme._manuallyBlockedThemes.Add(manuallyBlockedTheme);
		}
		return theme;
	}

	public override PsaiMusicEntity ShallowCopy()
	{
		return (Theme)MemberwiseClone();
	}
}
