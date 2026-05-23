using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TaleWorlds.Library;
using TaleWorlds.ModuleManager;

namespace TaleWorlds.MountAndBlade.Launcher.Library;

public static class LauncherPlatform
{
	private static LauncherPlatformType _platformType;

	private static IPlatformModuleExtension _platformModuleExtension;

	public static LauncherPlatformType PlatformType => _platformType;

	public static void Initialize(List<string> args)
	{
		_platformType = ReadWindowsPlatformFromFile();
		Assembly assembly = null;
		if (PlatformType == LauncherPlatformType.Steam)
		{
			assembly = AssemblyLoader.LoadFrom(ManagedDllFolder.Name + "TaleWorlds.MountAndBlade.Launcher.Steam.dll");
		}
		else if (PlatformType == LauncherPlatformType.Epic)
		{
			assembly = AssemblyLoader.LoadFrom(ManagedDllFolder.Name + "TaleWorlds.MountAndBlade.Launcher.Epic.dll");
		}
		if (assembly != null)
		{
			Type[] types = assembly.GetTypes();
			Type type = null;
			Type[] array = types;
			foreach (Type type2 in array)
			{
				if (type2.GetInterfaces().Contains(typeof(IPlatformModuleExtension)))
				{
					type = type2;
					break;
				}
			}
			_platformModuleExtension = (IPlatformModuleExtension)type.GetConstructor(new Type[0]).Invoke(new object[0]);
		}
		if (_platformModuleExtension != null)
		{
			ModuleHelper.InitializePlatformModuleExtension(_platformModuleExtension, args);
		}
	}

	public static void Destroy()
	{
		ModuleHelper.ClearPlatformModuleExtension();
		_platformModuleExtension = null;
	}

	private static LauncherPlatformType ReadWindowsPlatformFromFile()
	{
		LauncherPlatformType result = LauncherPlatformType.None;
		if (IsGdk())
		{
			result = LauncherPlatformType.Gdk;
		}
		else if (IsSteam())
		{
			result = LauncherPlatformType.Steam;
		}
		else if (IsEpic())
		{
			result = LauncherPlatformType.Epic;
		}
		else if (IsGog())
		{
			result = LauncherPlatformType.Gog;
		}
		else
		{
			Debug.ShowMessageBox("There is no target file! Please verify your files.", "ERROR", 4u);
			Environment.Exit(0);
		}
		return result;
	}

	private static bool IsSteam()
	{
		return File.Exists(string.Concat(BasePath.Name + "Modules/Native/", "steam.target"));
	}

	private static bool IsGog()
	{
		return File.Exists(string.Concat(BasePath.Name + "Modules/Native/", "gog.target"));
	}

	private static bool IsGdk()
	{
		return false;
	}

	private static bool IsEpic()
	{
		return File.Exists(string.Concat(BasePath.Name + "Modules/Native/", "epic.target"));
	}

	public static void SetLauncherMode(bool isLauncherModeActive)
	{
		_platformModuleExtension?.SetLauncherMode(isLauncherModeActive);
	}
}
