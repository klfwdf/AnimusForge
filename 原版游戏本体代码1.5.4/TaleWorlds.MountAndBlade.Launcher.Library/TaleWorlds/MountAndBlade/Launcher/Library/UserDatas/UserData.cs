using System.Linq;

namespace TaleWorlds.MountAndBlade.Launcher.Library.UserDatas;

public class UserData
{
	public GameType GameType { get; set; }

	public UserGameTypeData SingleplayerData { get; set; }

	public UserGameTypeData MultiplayerData { get; set; }

	public DLLCheckDataCollection DLLCheckData { get; set; }

	public UserData()
	{
		GameType = GameType.Singleplayer;
		SingleplayerData = new UserGameTypeData();
		MultiplayerData = new UserGameTypeData();
		DLLCheckData = new DLLCheckDataCollection();
	}

	public UserModData GetUserModData(bool isMultiplayer, string id)
	{
		return (isMultiplayer ? MultiplayerData : SingleplayerData).ModDatas.Find((UserModData x) => x.Id == id);
	}

	public uint? GetDLLLatestSizeInBytes(string dllName)
	{
		return DLLCheckData.DLLData.FirstOrDefault((DLLCheckData d) => d.DLLName == dllName)?.LatestSizeInBytes;
	}

	public bool GetDLLLatestIsDangerous(string dllName)
	{
		return DLLCheckData.DLLData.FirstOrDefault((DLLCheckData d) => d.DLLName == dllName)?.IsDangerous ?? true;
	}

	public string GetDLLLatestVerifyInformation(string dllName)
	{
		return DLLCheckData.DLLData.FirstOrDefault((DLLCheckData d) => d.DLLName == dllName)?.DLLVerifyInformation ?? "";
	}

	public void SetDLLLatestSizeInBytes(string dllName, uint sizeInBytes)
	{
		EnsureDLLIsAdded(dllName);
		DLLCheckData.DLLData.Find((DLLCheckData d) => d.DLLName == dllName).LatestSizeInBytes = sizeInBytes;
	}

	public void SetDLLLatestVerifyInformation(string dllName, string verifyInformation)
	{
		EnsureDLLIsAdded(dllName);
		DLLCheckData.DLLData.Find((DLLCheckData d) => d.DLLName == dllName).DLLVerifyInformation = verifyInformation;
	}

	public void SetDLLLatestIsDangerous(string dllName, bool isDangerous)
	{
		EnsureDLLIsAdded(dllName);
		DLLCheckData.DLLData.Find((DLLCheckData d) => d.DLLName == dllName).IsDangerous = isDangerous;
	}

	private void EnsureDLLIsAdded(string dllName)
	{
		if (!DLLCheckData.DLLData.Any((DLLCheckData d) => d.DLLName == dllName))
		{
			DLLCheckData.DLLData.Add(new DLLCheckData(dllName));
		}
	}
}
