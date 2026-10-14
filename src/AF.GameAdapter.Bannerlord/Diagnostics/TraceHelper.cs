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
				var settings = GlobalSettings<DuelSettings>.Instance;
				return settings != null && settings.EnableDeepTrace;
			}
			catch
			{
				return false;
			}
		}
	}
}
