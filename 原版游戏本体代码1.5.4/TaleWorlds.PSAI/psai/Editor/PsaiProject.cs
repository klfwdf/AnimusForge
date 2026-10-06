using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;
using psai.net;

namespace psai.Editor;

public class PsaiProject : ICloneable
{
	public static readonly string SERIALIZATION_PROTOCOL_VERSION = "1.0";

	private ProjectProperties _projectProperties = new ProjectProperties();

	private List<Theme> _themes = new List<Theme>();

	private static XmlSerializer _serializer = new XmlSerializer(typeof(PsaiProject));

	public string InitialExportDirectory { get; set; }

	public string SerializedByProtocolVersion { get; set; }

	public ProjectProperties Properties
	{
		get
		{
			return _projectProperties;
		}
		set
		{
			_projectProperties = value;
		}
	}

	public List<Theme> Themes
	{
		get
		{
			return _themes;
		}
		set
		{
			_themes = value;
		}
	}

	public void Init()
	{
		_projectProperties = new ProjectProperties();
		_themes.Clear();
	}

	public static PsaiProject LoadProjectFromStream(StreamReader reader, string path)
	{
		PsaiProject psaiProject = null;
		try
		{
			psaiProject = (PsaiProject)_serializer.Deserialize(reader);
			reader.Close();
			psaiProject.ReconstructIds(path);
			return psaiProject;
		}
		catch (Exception ex)
		{
			throw ex;
		}
	}

	public static PsaiProject LoadProjectFromXmlFile(string filename)
	{
		try
		{
			StreamReader streamReader = new StreamReader(filename);
			if (streamReader != null)
			{
				return LoadProjectFromStream(streamReader, filename);
			}
			return null;
		}
		catch (Exception ex)
		{
			throw ex;
		}
	}

	public void SaveAsXmlFile(string filename)
	{
		PrepareForXmlSerialization();
		try
		{
			TextWriter textWriter = new StreamWriter(filename);
			_serializer.Serialize(textWriter, this);
			textWriter.Close();
		}
		catch (Exception ex)
		{
			throw ex;
		}
	}

	public void Report(bool reportGroups, bool reportSegments)
	{
	}

	public bool ConvertProjectFile_From_Legacy_To_0_9_12(string pathToProjectFile)
	{
		if (File.Exists(pathToProjectFile))
		{
			FileStream input = new FileStream(pathToProjectFile, FileMode.Open, FileAccess.Read);
			XmlReaderSettings settings = new XmlReaderSettings();
			using (XmlReader.Create(input, settings))
			{
			}
		}
		return false;
	}

	public void ReconstructReferencesAfterXmlDeserialization()
	{
		foreach (Theme theme in _themes)
		{
			foreach (Group group in theme.Groups)
			{
				group.Theme = theme;
				foreach (Segment segment in group.Segments)
				{
					segment.Group = group;
				}
			}
		}
		foreach (Theme theme2 in _themes)
		{
			theme2.ManuallyBlockedTargetThemes.Clear();
			foreach (int serialization_ManuallyBlockedThemeId in theme2.Serialization_ManuallyBlockedThemeIds)
			{
				Theme themeById = GetThemeById(serialization_ManuallyBlockedThemeId);
				if (themeById != null)
				{
					theme2.ManuallyBlockedTargetThemes.Add(themeById);
				}
			}
			foreach (Group group2 in theme2.Groups)
			{
				group2.ManuallyBlockedGroups.Clear();
				if (group2.Serialization_ManuallyBlockedGroupIds == null)
				{
					group2.Serialization_ManuallyBlockedGroupIds = new List<int>();
				}
				foreach (int serialization_ManuallyBlockedGroupId in group2.Serialization_ManuallyBlockedGroupIds)
				{
					Group groupBySerializationId = GetGroupBySerializationId(serialization_ManuallyBlockedGroupId);
					if (groupBySerializationId != null)
					{
						group2.ManuallyBlockedGroups.Add(groupBySerializationId);
					}
				}
				group2.ManuallyLinkedGroups.Clear();
				if (group2.Serialization_ManuallyLinkedGroupIds == null)
				{
					group2.Serialization_ManuallyLinkedGroupIds = new List<int>();
				}
				foreach (int serialization_ManuallyLinkedGroupId in group2.Serialization_ManuallyLinkedGroupIds)
				{
					Group groupBySerializationId2 = GetGroupBySerializationId(serialization_ManuallyLinkedGroupId);
					if (groupBySerializationId2 != null)
					{
						group2.ManuallyLinkedGroups.Add(groupBySerializationId2);
					}
				}
				group2.ManualBridgeSnippetsOfTargetGroups.Clear();
				if (group2.Serialization_ManualBridgeSegmentIds == null)
				{
					group2.Serialization_ManualBridgeSegmentIds = new List<int>();
				}
				foreach (int serialization_ManualBridgeSegmentId in group2.Serialization_ManualBridgeSegmentIds)
				{
					Segment snippetById = GetSnippetById(serialization_ManualBridgeSegmentId);
					if (snippetById != null)
					{
						group2.ManualBridgeSnippetsOfTargetGroups.Add(snippetById);
					}
				}
				foreach (Segment segment2 in group2.Segments)
				{
					segment2.ManuallyBlockedSnippets = new HashSet<Segment>();
					if (segment2.Serialization_ManuallyBlockedSegmentIds == null)
					{
						segment2.Serialization_ManuallyBlockedSegmentIds = new List<int>();
					}
					foreach (int serialization_ManuallyBlockedSegmentId in segment2.Serialization_ManuallyBlockedSegmentIds)
					{
						Segment snippetById2 = GetSnippetById(serialization_ManuallyBlockedSegmentId);
						segment2.ManuallyBlockedSnippets.Add(snippetById2);
					}
					segment2.ManuallyLinkedSnippets = new HashSet<Segment>();
					if (segment2.Serialization_ManuallyLinkedSegmentIds == null)
					{
						segment2.Serialization_ManuallyLinkedSegmentIds = new List<int>();
					}
					foreach (int serialization_ManuallyLinkedSegmentId in segment2.Serialization_ManuallyLinkedSegmentIds)
					{
						Segment snippetById3 = GetSnippetById(serialization_ManuallyLinkedSegmentId);
						segment2.ManuallyLinkedSnippets.Add(snippetById3);
					}
				}
			}
		}
	}

	public void MergeProjects(PsaiProject project)
	{
		_themes.AddRange(project._themes);
	}

	public void ReconstructIds(string path)
	{
		foreach (Theme theme in _themes)
		{
			theme.Id = int.Parse(_projectProperties.ModuleIdPrefix + theme.Id);
			foreach (Group group in theme.Groups)
			{
				group.Serialization_Id = int.Parse(_projectProperties.ModuleIdPrefix + group.Serialization_Id);
				if (group.Serialization_ManuallyBlockedGroupIds == null)
				{
					group.Serialization_ManuallyBlockedGroupIds = new List<int>();
				}
				for (int i = 0; i < group.Serialization_ManuallyBlockedGroupIds.Count; i++)
				{
					group.Serialization_ManuallyBlockedGroupIds[i] = int.Parse(_projectProperties.ModuleIdPrefix + group.Serialization_ManuallyBlockedGroupIds[i]);
				}
				if (group.Serialization_ManuallyLinkedGroupIds == null)
				{
					group.Serialization_ManuallyLinkedGroupIds = new List<int>();
				}
				for (int j = 0; j < group.Serialization_ManuallyLinkedGroupIds.Count; j++)
				{
					group.Serialization_ManuallyLinkedGroupIds[j] = int.Parse(_projectProperties.ModuleIdPrefix + group.Serialization_ManuallyLinkedGroupIds[j]);
				}
				if (group.Serialization_ManualBridgeSegmentIds == null)
				{
					group.Serialization_ManualBridgeSegmentIds = new List<int>();
				}
				for (int k = 0; k < group.Serialization_ManualBridgeSegmentIds.Count; k++)
				{
					group.Serialization_ManualBridgeSegmentIds[k] = int.Parse(_projectProperties.ModuleIdPrefix + group.Serialization_ManualBridgeSegmentIds[k]);
				}
				foreach (Segment segment in group.Segments)
				{
					segment.Id = int.Parse(_projectProperties.ModuleIdPrefix + segment.Id);
					if (segment.Serialization_ManuallyBlockedSegmentIds == null)
					{
						segment.Serialization_ManuallyBlockedSegmentIds = new List<int>();
					}
					for (int l = 0; l < segment.Serialization_ManuallyBlockedSegmentIds.Count; l++)
					{
						segment.Serialization_ManuallyBlockedSegmentIds[l] = int.Parse(_projectProperties.ModuleIdPrefix + segment.Serialization_ManuallyBlockedSegmentIds[l]);
					}
					if (segment.Serialization_ManuallyLinkedSegmentIds == null)
					{
						segment.Serialization_ManuallyLinkedSegmentIds = new List<int>();
					}
					for (int m = 0; m < segment.Serialization_ManuallyLinkedSegmentIds.Count; m++)
					{
						segment.Serialization_ManuallyLinkedSegmentIds[m] = int.Parse(_projectProperties.ModuleIdPrefix + segment.Serialization_ManuallyLinkedSegmentIds[m]);
					}
					segment.AudioData.FilePathRelativeToProjectDir = segment.AudioData.FilePathRelativeToProjectDir;
					segment.AudioData.ModuleID = path;
				}
			}
			for (int n = 0; n < theme.Serialization_ManuallyBlockedThemeIds.Count; n++)
			{
				theme.Serialization_ManuallyBlockedThemeIds[n] = int.Parse(_projectProperties.ModuleIdPrefix + theme.Serialization_ManuallyBlockedThemeIds[n]);
			}
		}
	}

	public void DebugCheckProjectIntegrity()
	{
		foreach (Theme theme in _themes)
		{
			foreach (Group group in theme.Groups)
			{
				foreach (Segment segment in group.Segments)
				{
					foreach (int serialization_ManuallyBlockedSegmentId in segment.Serialization_ManuallyBlockedSegmentIds)
					{
						GetSnippetById(serialization_ManuallyBlockedSegmentId);
					}
					foreach (int serialization_ManuallyLinkedSegmentId in segment.Serialization_ManuallyLinkedSegmentIds)
					{
						GetSnippetById(serialization_ManuallyLinkedSegmentId);
					}
					GetThemeById(segment.ThemeId);
				}
				foreach (int serialization_ManuallyBlockedGroupId in group.Serialization_ManuallyBlockedGroupIds)
				{
					GetGroupBySerializationId(serialization_ManuallyBlockedGroupId);
				}
				foreach (int serialization_ManuallyLinkedGroupId in group.Serialization_ManuallyLinkedGroupIds)
				{
					GetGroupBySerializationId(serialization_ManuallyLinkedGroupId);
				}
				foreach (int serialization_ManualBridgeSegmentId in group.Serialization_ManualBridgeSegmentIds)
				{
					GetSnippetById(serialization_ManualBridgeSegmentId);
				}
			}
			foreach (int serialization_ManuallyBlockedThemeId in theme.Serialization_ManuallyBlockedThemeIds)
			{
				GetThemeById(serialization_ManuallyBlockedThemeId);
			}
		}
	}

	public Soundtrack BuildPsaiDotNetSoundtrackFromProject()
	{
		Soundtrack soundtrack = new Soundtrack();
		foreach (Theme theme in Themes)
		{
			if (theme.ThemeTypeInt == 6)
			{
				foreach (Segment segmentsOfAllGroup in theme.GetSegmentsOfAllGroups())
				{
					segmentsOfAllGroup.IsUsableAtEnd = true;
					segmentsOfAllGroup.IsUsableInMiddle = true;
					segmentsOfAllGroup.IsUsableAtEnd = true;
				}
			}
			soundtrack.m_themes.Add(theme.Id, theme.CreatePsaiDotNetVersion());
		}
		foreach (Segment segmentsOfAllTheme in GetSegmentsOfAllThemes())
		{
			segmentsOfAllTheme.BuildCompatibleSegmentsSet(this);
			psai.net.Segment segment = segmentsOfAllTheme.CreatePsaiDotNetVersion(this);
			soundtrack.m_snippets.Add(segment.Id, segment);
			soundtrack.getThemeById(segment.ThemeId).m_segments.Add(segment);
		}
		soundtrack.BuildAllIndirectionSequences();
		return soundtrack;
	}

	private void PrepareForXmlSerialization()
	{
		SerializedByProtocolVersion = SERIALIZATION_PROTOCOL_VERSION;
		int num = 1;
		foreach (Theme theme in _themes)
		{
			foreach (Group group in theme.Groups)
			{
				group.Serialization_Id = num;
				num++;
			}
		}
		foreach (Theme theme2 in _themes)
		{
			theme2.Serialization_ManuallyBlockedThemeIds = new List<int>();
			foreach (Theme manuallyBlockedTargetTheme in theme2.ManuallyBlockedTargetThemes)
			{
				if (manuallyBlockedTargetTheme != null)
				{
					theme2.Serialization_ManuallyBlockedThemeIds.Add(manuallyBlockedTargetTheme.Id);
				}
			}
			foreach (Group group2 in theme2.Groups)
			{
				group2.Serialization_ManuallyBlockedGroupIds = new List<int>();
				foreach (Group manuallyBlockedGroup in group2.ManuallyBlockedGroups)
				{
					if (manuallyBlockedGroup != null)
					{
						group2.Serialization_ManuallyBlockedGroupIds.Add(manuallyBlockedGroup.Serialization_Id);
					}
				}
				group2.Serialization_ManuallyLinkedGroupIds = new List<int>();
				foreach (Group manuallyLinkedGroup in group2.ManuallyLinkedGroups)
				{
					if (manuallyLinkedGroup != null)
					{
						group2.Serialization_ManuallyLinkedGroupIds.Add(manuallyLinkedGroup.Serialization_Id);
					}
				}
				group2.Serialization_ManualBridgeSegmentIds = new List<int>();
				foreach (Segment manualBridgeSnippetsOfTargetGroup in group2.ManualBridgeSnippetsOfTargetGroups)
				{
					if (manualBridgeSnippetsOfTargetGroup != null)
					{
						group2.Serialization_ManualBridgeSegmentIds.Add(manualBridgeSnippetsOfTargetGroup.Id);
					}
				}
				foreach (Segment segment in group2.Segments)
				{
					segment.Serialization_ManuallyBlockedSegmentIds = new List<int>();
					foreach (Segment manuallyBlockedSnippet in segment.ManuallyBlockedSnippets)
					{
						if (manuallyBlockedSnippet != null)
						{
							segment.Serialization_ManuallyBlockedSegmentIds.Add(manuallyBlockedSnippet.Id);
						}
					}
					segment.Serialization_ManuallyLinkedSegmentIds = new List<int>();
					foreach (Segment manuallyLinkedSnippet in segment.ManuallyLinkedSnippets)
					{
						if (manuallyLinkedSnippet != null)
						{
							segment.Serialization_ManuallyLinkedSegmentIds.Add(manuallyLinkedSnippet.Id);
						}
					}
				}
			}
		}
	}

	public HashSet<Segment> GetSegmentsOfAllThemes()
	{
		HashSet<Segment> hashSet = new HashSet<Segment>();
		foreach (Theme theme in Themes)
		{
			hashSet.UnionWith(theme.GetSegmentsOfAllGroups());
		}
		return hashSet;
	}

	public Theme GetThemeById(int themeId)
	{
		foreach (Theme theme in Themes)
		{
			if (theme.Id == themeId)
			{
				return theme;
			}
		}
		return null;
	}

	public Segment GetSnippetById(int id)
	{
		foreach (Theme theme in Themes)
		{
			foreach (Segment segmentsOfAllGroup in theme.GetSegmentsOfAllGroups())
			{
				if (segmentsOfAllGroup.Id == id)
				{
					return segmentsOfAllGroup;
				}
			}
		}
		return null;
	}

	public Group GetGroupBySerializationId(int id)
	{
		foreach (Theme theme in _themes)
		{
			foreach (Group group in theme.Groups)
			{
				if (group.Serialization_Id == id)
				{
					return group;
				}
			}
		}
		return null;
	}

	public void AddPsaiMusicEntity(PsaiMusicEntity entity)
	{
		AddPsaiMusicEntity(entity, -1);
	}

	public void AddPsaiMusicEntity(PsaiMusicEntity entity, int targetIndex)
	{
		if (entity is Segment)
		{
			Segment segment = (Segment)entity;
			if (GetSnippetById(segment.Id) != null)
			{
				segment.Id = GetNextFreeSnippetId(segment.Id);
			}
			if (segment.Group != null)
			{
				segment.Group.AddSegment(segment, targetIndex);
			}
		}
		else if (entity is Group)
		{
			Group obj = (Group)entity;
			if (obj.Theme != null)
			{
				obj.Theme.Groups.Add(obj);
			}
		}
		else if (entity is Theme)
		{
			Theme theme = (Theme)entity;
			if (GetThemeById(theme.Id) != null)
			{
				theme.Id = GetNextFreeThemeId(theme.Id);
			}
			Themes.Add(theme);
		}
	}

	public void DeleteMusicEntity(PsaiMusicEntity entity)
	{
		if (entity is Segment)
		{
			Segment segment = (Segment)entity;
			if (segment.Group != null)
			{
				segment.Group.RemoveSegment(segment);
			}
			{
				foreach (Segment segmentsOfAllTheme in GetSegmentsOfAllThemes())
				{
					if (segmentsOfAllTheme.ManuallyLinkedSnippets.Contains(segment))
					{
						segmentsOfAllTheme.ManuallyLinkedSnippets.Remove(segment);
					}
					if (segmentsOfAllTheme.ManuallyBlockedSnippets.Contains(segment))
					{
						segmentsOfAllTheme.ManuallyBlockedSnippets.Remove(segment);
					}
				}
				return;
			}
		}
		if (entity is Group)
		{
			Group obj = (Group)entity;
			if (obj.Theme != null)
			{
				obj.Theme.Groups.Remove(obj);
			}
			{
				foreach (Group groupsOfAllTheme in GetGroupsOfAllThemes())
				{
					if (groupsOfAllTheme.ManuallyBlockedGroups.Contains(obj))
					{
						groupsOfAllTheme.ManuallyBlockedGroups.Remove(obj);
					}
					if (groupsOfAllTheme.ManuallyLinkedGroups.Contains(obj))
					{
						groupsOfAllTheme.ManuallyLinkedGroups.Remove(obj);
					}
				}
				return;
			}
		}
		if (!(entity is Theme))
		{
			return;
		}
		Theme item = (Theme)entity;
		Themes.Remove(item);
		foreach (Theme theme in Themes)
		{
			if (theme.ManuallyBlockedTargetThemes.Contains(item))
			{
				theme.ManuallyBlockedTargetThemes.Remove(item);
			}
		}
	}

	public int GetHighestSegmentId()
	{
		int num = 0;
		foreach (Segment segmentsOfAllTheme in GetSegmentsOfAllThemes())
		{
			if (segmentsOfAllTheme.Id > num)
			{
				num = segmentsOfAllTheme.Id;
			}
		}
		return num;
	}

	public int GetNextFreeSnippetId(int idToStartSearchFrom)
	{
		int i = idToStartSearchFrom;
		if (i <= 1)
		{
			i = 1;
		}
		for (; GetSnippetById(i) != null; i++)
		{
		}
		return i;
	}

	public HashSet<Group> GetGroupsOfAllThemes()
	{
		HashSet<Group> hashSet = new HashSet<Group>();
		foreach (Theme theme in Themes)
		{
			foreach (Group group in theme.Groups)
			{
				hashSet.Add(group);
			}
		}
		return hashSet;
	}

	public bool CheckIfSnippetIsManualBridgeSnippetForSourceGroup(Segment snippet, Group sourceGroup)
	{
		return sourceGroup.ManualBridgeSnippetsOfTargetGroups.Contains(snippet);
	}

	public bool CheckIfThereIsAtLeastOneBridgeSnippetFromSourceGroupToTargetGroup(Group sourceGroup, Group targetGroup)
	{
		if (!targetGroup.ContainsAtLeastOneAutomaticBridgeSegment())
		{
			return targetGroup.ContainsAtLeastOneManualBridgeSegmentForSourceGroup(sourceGroup);
		}
		return true;
	}

	public bool CheckIfSnippetIsManualBridgeSnippetToAnyGroup(Segment snippet, bool getGroups, out List<Group> groups)
	{
		groups = new List<Group>();
		foreach (Theme theme in Themes)
		{
			foreach (Group group in theme.Groups)
			{
				if (group.ManualBridgeSnippetsOfTargetGroups.Contains(snippet))
				{
					if (!getGroups)
					{
						return true;
					}
					groups.Add(group);
				}
			}
		}
		return groups.Count > 0;
	}

	public void DoUpdateAllParentThemeIdsAndGroupsOfChildPsaiEntities()
	{
		foreach (Theme theme in Themes)
		{
			foreach (Group group in theme.Groups)
			{
				group.Theme = theme;
				foreach (Segment segment in group.Segments)
				{
					segment.Group = group;
					segment.ThemeId = theme.Id;
				}
			}
		}
	}

	public int GetNextFreeThemeId(int idToStartSearchFrom)
	{
		int i = idToStartSearchFrom;
		if (i <= 1)
		{
			i = 1;
		}
		for (; GetThemeById(i) != null; i++)
		{
		}
		return i;
	}

	public bool CheckIfThemeIdIsInUse(int themeId)
	{
		foreach (Theme theme in Themes)
		{
			if (theme.Id == themeId)
			{
				return true;
			}
		}
		return false;
	}

	public List<Segment> GetSnippetsById(int id)
	{
		List<Segment> list = new List<Segment>();
		foreach (Segment segmentsOfAllTheme in GetSegmentsOfAllThemes())
		{
			if (segmentsOfAllTheme.Id == id)
			{
				list.Add(segmentsOfAllTheme);
			}
		}
		return list;
	}

	public object Clone()
	{
		PsaiProject psaiProject = new PsaiProject();
		psaiProject.Properties = (ProjectProperties)Properties.Clone();
		psaiProject.Themes.Clear();
		foreach (Theme theme2 in Themes)
		{
			Theme entity = (Theme)theme2.Clone();
			psaiProject.AddPsaiMusicEntity(entity);
		}
		HashSet<Segment> segmentsOfAllThemes = GetSegmentsOfAllThemes();
		HashSet<Segment> segmentsOfAllThemes2 = psaiProject.GetSegmentsOfAllThemes();
		Dictionary<Theme, Theme> dictionary = new Dictionary<Theme, Theme>();
		List<Theme>.Enumerator enumerator2 = Themes.GetEnumerator();
		List<Theme>.Enumerator enumerator3 = psaiProject.Themes.GetEnumerator();
		while (enumerator2.MoveNext())
		{
			enumerator3.MoveNext();
			dictionary.Add(enumerator2.Current, enumerator3.Current);
		}
		Dictionary<Segment, Segment> dictionary2 = new Dictionary<Segment, Segment>();
		HashSet<Segment>.Enumerator enumerator4 = segmentsOfAllThemes.GetEnumerator();
		HashSet<Segment>.Enumerator enumerator5 = segmentsOfAllThemes2.GetEnumerator();
		while (enumerator4.MoveNext())
		{
			enumerator5.MoveNext();
			dictionary2.Add(enumerator4.Current, enumerator5.Current);
		}
		Dictionary<Group, Group> dictionary3 = new Dictionary<Group, Group>();
		foreach (Theme key2 in dictionary.Keys)
		{
			Theme theme = dictionary[key2];
			foreach (Theme manuallyBlockedTargetTheme in key2.ManuallyBlockedTargetThemes)
			{
				if (dictionary.Keys.Contains(manuallyBlockedTargetTheme))
				{
					theme.ManuallyBlockedTargetThemes.Add(dictionary[manuallyBlockedTargetTheme]);
				}
			}
			for (int i = 0; i < key2.Groups.Count; i++)
			{
				Group key = key2.Groups[i];
				Group value = theme.Groups[i];
				dictionary3.Add(key, value);
			}
		}
		foreach (Group key3 in dictionary3.Keys)
		{
			foreach (Group manuallyLinkedGroup in key3.ManuallyLinkedGroups)
			{
				if (dictionary3.Keys.Contains(manuallyLinkedGroup))
				{
					dictionary3[key3].ManuallyLinkedGroups.Add(dictionary3[manuallyLinkedGroup]);
				}
			}
			foreach (Group manuallyBlockedGroup in key3.ManuallyBlockedGroups)
			{
				if (dictionary3.Keys.Contains(manuallyBlockedGroup))
				{
					dictionary3[key3].ManuallyBlockedGroups.Add(dictionary3[manuallyBlockedGroup]);
				}
			}
			foreach (Segment manualBridgeSnippetsOfTargetGroup in key3.ManualBridgeSnippetsOfTargetGroups)
			{
				if (dictionary2.Keys.Contains(manualBridgeSnippetsOfTargetGroup))
				{
					Segment item = dictionary2[manualBridgeSnippetsOfTargetGroup];
					dictionary3[key3].ManualBridgeSnippetsOfTargetGroups.Add(item);
				}
			}
		}
		foreach (Segment item2 in segmentsOfAllThemes)
		{
			foreach (Segment manuallyBlockedSnippet in item2.ManuallyBlockedSnippets)
			{
				if (dictionary2.Keys.Contains(manuallyBlockedSnippet))
				{
					dictionary2[item2].ManuallyBlockedSnippets.Add(dictionary2[manuallyBlockedSnippet]);
				}
			}
			foreach (Segment manuallyLinkedSnippet in item2.ManuallyLinkedSnippets)
			{
				if (dictionary2.Keys.Contains(manuallyLinkedSnippet))
				{
					dictionary2[item2].ManuallyLinkedSnippets.Add(dictionary2[manuallyLinkedSnippet]);
				}
			}
		}
		return psaiProject;
	}
}
