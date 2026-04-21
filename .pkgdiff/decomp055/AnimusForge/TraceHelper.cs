using MCM.Abstractions.Base.Global;

namespace AnimusForge;

public static class TraceHelper
{
	public static bool IsEnabled
	{
		get
		{
			try
			{
				return GlobalSettings<DuelSettings>.Instance != null && GlobalSettings<DuelSettings>.Instance.EnableDeepTrace;
			}
			catch
			{
				return false;
			}
		}
	}
}
