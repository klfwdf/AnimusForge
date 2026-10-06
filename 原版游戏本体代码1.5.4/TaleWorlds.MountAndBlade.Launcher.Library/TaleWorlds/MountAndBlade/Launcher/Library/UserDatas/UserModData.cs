using TaleWorlds.ModuleManager;

namespace TaleWorlds.MountAndBlade.Launcher.Library.UserDatas;

public class UserModData
{
	public string Id { get; set; }

	public string LastKnownVersion { get; set; }

	public bool IsSelected { get; set; }

	public UserModData()
	{
	}

	public UserModData(string id, string lastKnownVersion, bool isSelected)
	{
		Id = id;
		LastKnownVersion = lastKnownVersion;
		IsSelected = isSelected;
	}

	public bool IsUpdatedToBeDefault(ModuleInfo module)
	{
		if (LastKnownVersion != module.Version.ToString())
		{
			return module.IsDefault;
		}
		return false;
	}
}
