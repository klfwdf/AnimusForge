using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using TaleWorlds.ModuleManager;

namespace TaleWorlds.MountAndBlade.Diamond;

[Serializable]
public class ModuleInfoModel
{
	public string Id { get; private set; }

	public string Name { get; private set; }

	public ModuleCategory Category { get; private set; }

	public string Version { get; private set; }

	[JsonIgnore]
	public bool IsOptional => Category == ModuleCategory.MultiplayerOptional;

	[JsonConstructor]
	private ModuleInfoModel(string id, string name, string version, ModuleCategory category)
	{
		Id = id;
		Name = name;
		Version = version;
		Category = category;
	}

	internal ModuleInfoModel(ModuleInfo moduleInfo)
		: this(moduleInfo.Id, moduleInfo.Name, moduleInfo.Version.ToString(), moduleInfo.Category)
	{
	}

	public static bool ShouldIncludeInSession(ModuleInfo moduleInfo)
	{
		if (!moduleInfo.IsOfficial)
		{
			return moduleInfo.HasMultiplayerCategory;
		}
		return false;
	}

	public static bool TryCreateForSession(ModuleInfo moduleInfo, out ModuleInfoModel moduleInfoModel)
	{
		if (ShouldIncludeInSession(moduleInfo))
		{
			moduleInfoModel = new ModuleInfoModel(moduleInfo);
			return true;
		}
		moduleInfoModel = null;
		return false;
	}

	public override bool Equals(object obj)
	{
		if (obj is ModuleInfoModel moduleInfoModel && Id == moduleInfoModel.Id)
		{
			return Version == moduleInfoModel.Version;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return (-612338121 * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Id)) * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Version);
	}
}
