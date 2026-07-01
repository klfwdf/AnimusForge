using System;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

internal static class MissionViewExceptionGuard
{
	public static Exception Filter(Exception exception, bool requireMainAgent, string source)
	{
		if (exception == null)
		{
			return null;
		}
		if (!(exception is NullReferenceException))
		{
			return exception;
		}
		if (!IsInvalidMissionViewState(requireMainAgent, out string reason))
		{
			return exception;
		}
		try
		{
			Logger.LogTrace("System", "Suppressed mission view NullReferenceException. source=" + (source ?? "") + ", reason=" + reason);
		}
		catch
		{
		}
		return null;
	}

	private static bool IsInvalidMissionViewState(bool requireMainAgent, out string reason)
	{
		reason = "";
		try
		{
			Mission mission = Mission.Current;
			if (mission == null)
			{
				reason = "mission_null";
				return true;
			}
			try
			{
				if (mission.MissionEnded)
				{
					reason = "mission_ended";
					return true;
				}
			}
			catch (NullReferenceException)
			{
				reason = "mission_ended_state_null";
				return true;
			}
			catch
			{
				return false;
			}
			try
			{
				if (mission.Scene == null)
				{
					reason = "scene_null";
					return true;
				}
			}
			catch (NullReferenceException)
			{
				reason = "scene_state_null";
				return true;
			}
			catch
			{
				return false;
			}
			if (requireMainAgent)
			{
				try
				{
					if (mission.MainAgent == null)
					{
						reason = "main_agent_null";
						return true;
					}
				}
				catch (NullReferenceException)
				{
					reason = "main_agent_state_null";
					return true;
				}
				catch
				{
					return false;
				}
			}
		}
		catch (NullReferenceException)
		{
			reason = "mission_state_null";
			return true;
		}
		catch
		{
			return false;
		}
		return false;
	}
}
