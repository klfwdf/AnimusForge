namespace TaleWorlds.MountAndBlade.Launcher.Library;

public class DLLResult
{
	public string DLLName { get; set; }

	public bool IsSafe { get; set; }

	public string Information { get; set; }

	public DLLResult(string dLLName, bool isSafe, string information)
	{
		DLLName = dLLName;
		IsSafe = isSafe;
		Information = information;
	}

	public DLLResult()
	{
		DLLName = "";
		IsSafe = false;
		Information = "";
	}
}
