namespace TaleWorlds.MountAndBlade.Launcher.Library.UserDatas;

public class DLLCheckData
{
	public string DLLName { get; set; }

	public string DLLVerifyInformation { get; set; }

	public uint LatestSizeInBytes { get; set; }

	public bool IsDangerous { get; set; }

	public DLLCheckData(string dllname)
	{
		LatestSizeInBytes = 0u;
		IsDangerous = true;
		DLLName = dllname;
		DLLVerifyInformation = "";
	}

	public DLLCheckData()
	{
	}
}
