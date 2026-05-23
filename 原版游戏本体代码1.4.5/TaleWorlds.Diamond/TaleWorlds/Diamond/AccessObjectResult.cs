using TaleWorlds.Localization;

namespace TaleWorlds.Diamond;

public class AccessObjectResult
{
	public AccessObject AccessObject { get; set; }

	public bool Success { get; set; }

	public TextObject FailReason { get; set; }

	public static AccessObjectResult CreateSuccess(AccessObject accessObject)
	{
		return new AccessObjectResult
		{
			Success = true,
			AccessObject = accessObject
		};
	}

	public static AccessObjectResult CreateFailed(TextObject failReason)
	{
		return new AccessObjectResult
		{
			Success = false,
			FailReason = failReason
		};
	}
}
