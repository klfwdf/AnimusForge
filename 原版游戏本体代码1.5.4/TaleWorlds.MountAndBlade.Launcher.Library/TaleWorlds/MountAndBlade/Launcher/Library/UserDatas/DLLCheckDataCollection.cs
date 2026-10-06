using System.Collections.Generic;

namespace TaleWorlds.MountAndBlade.Launcher.Library.UserDatas;

public class DLLCheckDataCollection
{
	public List<DLLCheckData> DLLData { get; set; }

	public DLLCheckDataCollection()
	{
		DLLData = new List<DLLCheckData>();
	}
}
