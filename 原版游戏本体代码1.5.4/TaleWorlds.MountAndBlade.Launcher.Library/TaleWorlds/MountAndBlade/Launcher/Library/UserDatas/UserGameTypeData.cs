using System.Collections.Generic;

namespace TaleWorlds.MountAndBlade.Launcher.Library.UserDatas;

public class UserGameTypeData
{
	public List<UserModData> ModDatas { get; set; }

	public UserGameTypeData()
	{
		ModDatas = new List<UserModData>();
	}
}
