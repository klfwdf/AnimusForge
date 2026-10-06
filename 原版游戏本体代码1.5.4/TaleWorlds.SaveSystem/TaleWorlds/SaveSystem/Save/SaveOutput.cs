using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TaleWorlds.Library;

namespace TaleWorlds.SaveSystem.Save;

public class SaveOutput
{
	private Task<SaveResultWithMessage> _continuingTask;

	public GameData Data { get; private set; }

	public SaveResult Result { get; private set; }

	public SaveError[] Errors { get; private set; }

	public bool Successful => Result == SaveResult.Success;

	public bool IsContinuing
	{
		get
		{
			Task<SaveResultWithMessage> continuingTask = _continuingTask;
			if (continuingTask == null)
			{
				return false;
			}
			return !continuingTask.IsCompleted;
		}
	}

	private SaveOutput()
	{
	}

	internal static SaveOutput CreateSuccessful(GameData data)
	{
		return new SaveOutput
		{
			Data = data,
			Result = SaveResult.Success
		};
	}

	internal static SaveOutput CreateFailed(IEnumerable<SaveError> errors, SaveResult result)
	{
		return new SaveOutput
		{
			Result = result,
			Errors = errors.ToArray()
		};
	}

	internal static SaveOutput CreateContinuing(Task<SaveResultWithMessage> continuingTask)
	{
		SaveOutput saveOutput = new SaveOutput();
		saveOutput._continuingTask = continuingTask;
		saveOutput._continuingTask.ContinueWith(delegate(Task<SaveResultWithMessage> t)
		{
			saveOutput.Result = t.Result.SaveResult;
		});
		return saveOutput;
	}

	public void PrintStatus()
	{
		Task<SaveResultWithMessage> continuingTask = _continuingTask;
		if (continuingTask != null && continuingTask.IsCompleted)
		{
			Result = _continuingTask.Result.SaveResult;
			Errors = new SaveError[0];
		}
		if (Result == SaveResult.Success)
		{
			Debug.Print("------Successfully saved------");
			return;
		}
		Debug.Print("Couldn't save because of errors listed below.");
		for (int i = 0; i < Errors.Length; i++)
		{
			SaveError saveError = Errors[i];
			Debug.Print("[" + i + "]" + saveError.Message);
			Debug.FailedAssert("SAVE FAILED: [" + i + "]" + saveError.Message + "\n", "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\Base\\TaleWorlds.SaveSystem\\Save\\SaveOutput.cs", "PrintStatus", 74);
		}
		Debug.Print("--------------------");
	}
}
