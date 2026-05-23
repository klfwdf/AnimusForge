using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.Network;

namespace TaleWorlds.MountAndBlade;

public static class DedicatedServerConsoleCommandManager
{
	private static readonly List<Type> _commandHandlerTypes;

	static DedicatedServerConsoleCommandManager()
	{
		_commandHandlerTypes = new List<Type>();
		AddType(typeof(DedicatedServerConsoleCommandManager));
	}

	public static void AddType(Type type)
	{
		_commandHandlerTypes.Add(type);
	}

	internal static void HandleConsoleCommand(string command)
	{
		int num = command.IndexOf(' ');
		string text = "";
		string text2;
		if (num > 0)
		{
			text2 = command.Substring(0, num);
			text = command.Substring(num + 1);
		}
		else
		{
			text2 = command;
		}
		bool flag = false;
		if (MultiplayerOptions.TryGetOptionTypeFromString(text2, out var optionType, out var optionAttribute))
		{
			if (text == "?")
			{
				Debug.Print(string.Concat("--", optionType, ": ", optionAttribute.Description), 0, Debug.DebugColor.White, 17179869184uL);
				Debug.Print("--" + (optionAttribute.HasBounds ? ("Min: " + optionAttribute.BoundsMin + ", Max: " + optionAttribute.BoundsMax + ". ") : "") + "Current value: " + optionType.GetValueText(), 0, Debug.DebugColor.White, 17179869184uL);
			}
			else if (text != "")
			{
				if (optionAttribute.OptionValueType == MultiplayerOptions.OptionValueType.String)
				{
					optionType.SetValue(text);
				}
				else if (optionAttribute.OptionValueType == MultiplayerOptions.OptionValueType.Integer)
				{
					if (int.TryParse(text, out var result))
					{
						optionType.SetValue(result);
					}
				}
				else if (optionAttribute.OptionValueType == MultiplayerOptions.OptionValueType.Enum)
				{
					if (int.TryParse(text, out var result2))
					{
						optionType.SetValue(result2);
					}
				}
				else if (optionAttribute.OptionValueType == MultiplayerOptions.OptionValueType.Bool)
				{
					if (bool.TryParse(text, out var result3))
					{
						optionType.SetValue(result3);
					}
				}
				else
				{
					Debug.FailedAssert("No valid type found for multiplayer option.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.Multiplayer\\DedicatedServerConsoleCommandManager.cs", "HandleConsoleCommand", 81);
				}
				Debug.Print(string.Concat("--Changed: ", optionType, ", to: ", optionType.GetValueText()), 0, Debug.DebugColor.White, 17179869184uL);
			}
			else
			{
				Debug.Print(string.Concat("--Value of: ", optionType, ", is: ", optionType.GetValueText()), 0, Debug.DebugColor.White, 17179869184uL);
			}
			flag = true;
		}
		if (!flag)
		{
			foreach (Type commandHandlerType in _commandHandlerTypes)
			{
				MethodInfo[] methods = commandHandlerType.GetMethods(BindingFlags.Static | BindingFlags.NonPublic);
				foreach (MethodInfo methodInfo in methods)
				{
					object[] customAttributesSafe = methodInfo.GetCustomAttributesSafe(inherit: false);
					for (int j = 0; j < customAttributesSafe.Length; j++)
					{
						if (customAttributesSafe[j] is ConsoleCommandMethod consoleCommandMethod && consoleCommandMethod.CommandName.Equals(text2))
						{
							if (text == "?")
							{
								Debug.Print("--" + consoleCommandMethod.CommandName + ": " + consoleCommandMethod.Description, 0, Debug.DebugColor.White, 17179869184uL);
							}
							else
							{
								methodInfo.Invoke(null, (string.IsNullOrEmpty(text) ? null : new List<object> { text })?.ToArray());
							}
							flag = true;
						}
					}
				}
			}
		}
		if (!flag)
		{
			bool found;
			string message = CommandLineFunctionality.CallFunction(text2, text, out found);
			if (found)
			{
				Debug.Print(message, 0, Debug.DebugColor.White, 17179869184uL);
				flag = true;
			}
		}
		if (!flag)
		{
			Debug.Print("--Invalid command is given.", 0, Debug.DebugColor.White, 17179869184uL);
		}
	}

	[UsedImplicitly]
	[ConsoleCommandMethod("list", "Displays a list of all multiplayer options, their values and other possible commands")]
	private static void ListAllCommands()
	{
		Debug.Print("--List of all multiplayer command and their current values:", 0, Debug.DebugColor.White, 17179869184uL);
		for (MultiplayerOptions.OptionType optionType = MultiplayerOptions.OptionType.ServerName; optionType < MultiplayerOptions.OptionType.NumOfSlots; optionType++)
		{
			Debug.Print(string.Concat("----", optionType, ": ", optionType.GetValueText()), 0, Debug.DebugColor.White, 17179869184uL);
		}
		Debug.Print("--List of additional commands:", 0, Debug.DebugColor.White, 17179869184uL);
		foreach (Type commandHandlerType in _commandHandlerTypes)
		{
			MethodInfo[] methods = commandHandlerType.GetMethods(BindingFlags.Static | BindingFlags.NonPublic);
			for (int i = 0; i < methods.Length; i++)
			{
				object[] customAttributesSafe = methods[i].GetCustomAttributesSafe(inherit: false);
				for (int j = 0; j < customAttributesSafe.Length; j++)
				{
					if (customAttributesSafe[j] is ConsoleCommandMethod consoleCommandMethod)
					{
						Debug.Print("----" + consoleCommandMethod.CommandName, 0, Debug.DebugColor.White, 17179869184uL);
					}
				}
			}
		}
		Debug.Print("--Add '?' after a command to get a more detailed description.", 0, Debug.DebugColor.White, 17179869184uL);
	}

	[UsedImplicitly]
	[ConsoleCommandMethod("set_winner_team", "Sets the winner team of flag domination based multiplayer missions.")]
	private static void SetWinnerTeam(string winnerTeamAsString)
	{
		MissionMultiplayerFlagDomination.SetWinnerTeam(int.Parse(winnerTeamAsString));
	}

	[UsedImplicitly]
	[ConsoleCommandMethod("set_server_bandwidth_limit_in_mbps", "Overrides server's older bandwidth limit in megabit(s) per second.")]
	private static void SetServerBandwidthLimitInMbps(string bandwidthLimitAsString)
	{
		GameNetwork.SetServerBandwidthLimitInMbps(double.Parse(bandwidthLimitAsString));
	}

	[UsedImplicitly]
	[ConsoleCommandMethod("set_server_tickrate", "Overrides server's older tickrate setting.")]
	private static void SetServerTickRate(string tickrateAsString)
	{
		double num = double.Parse(tickrateAsString);
		GameNetwork.SetServerTickRate(num);
		GameNetwork.SetServerFrameRate(num);
	}

	[UsedImplicitly]
	[ConsoleCommandMethod("stats", "Displays some game statistics, like FPS and players on the server.")]
	private static void ShowStats()
	{
		Debug.Print("--Current FPS: " + Utilities.GetFps(), 0, Debug.DebugColor.White, 17179869184uL);
		Debug.Print("--Active Players: " + GameNetwork.NetworkPeers.Count((NetworkCommunicator x) => x.IsSynchronized), 0, Debug.DebugColor.White, 17179869184uL);
	}

	[UsedImplicitly]
	[ConsoleCommandMethod("open_monitor", "Opens up the monitor window with continuous data-representations on server performance and network usage.")]
	private static void OpenMonitor()
	{
		DebugNetworkEventStatistics.ControlActivate();
		DebugNetworkEventStatistics.OpenExternalMonitor();
	}

	[UsedImplicitly]
	[ConsoleCommandMethod("crash_game", "Crashes the game process.")]
	private static void CrashGame()
	{
		Debug.Print("Crashing the process...", 0, Debug.DebugColor.White, 17179869184uL);
		throw new Exception("Game crashed by user command");
	}
}
