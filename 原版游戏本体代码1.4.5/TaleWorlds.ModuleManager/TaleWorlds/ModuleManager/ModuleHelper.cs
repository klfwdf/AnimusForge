using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TaleWorlds.Library;

namespace TaleWorlds.ModuleManager;

public static class ModuleHelper
{
	public const char ModuleVersionSeperator = ':';

	public static bool IsTestMode = false;

	public const char ModuleCodeSeperator = ';';

	public static readonly MBList<string> ModulesDisablingLoadingAfterBeingRemoved = new MBList<string> { "StoryMode", "NavalDLC" };

	public static readonly MBList<string> ModulesDisablingLoadingAfterBeingAdded = new MBList<string> { "NavalDLC" };

	private static IPlatformModuleExtension _platformModuleExtension;

	private static Dictionary<string, ModuleInfo> _loadedModules;

	private static List<ModuleInfo> results = null;

	private static string _pathPrefix => BasePath.Name + "Modules/";

	public static string GetModuleFullPath(string moduleId)
	{
		return _loadedModules[moduleId.ToLower()].FolderPath + "/";
	}

	public static ModuleInfo GetModuleInfo(string moduleId)
	{
		string key = moduleId.ToLower();
		if (_loadedModules.ContainsKey(key))
		{
			return _loadedModules[key];
		}
		return null;
	}

	public static void OnModuleDeactivated(string id)
	{
		ModuleInfo moduleInfo = GetModuleInfo(id);
		if (moduleInfo != null && moduleInfo.IsActive)
		{
			moduleInfo.DeactivateModule();
		}
	}

	public static void OnModuleActivated(string id)
	{
		ModuleInfo moduleInfo = GetModuleInfo(id);
		if (moduleInfo != null && !moduleInfo.IsActive)
		{
			moduleInfo.ActivateModule();
		}
	}

	public static void InitializeModules(string[] loadedModuleIds, string[] platformModulePaths = null)
	{
		_loadedModules = new Dictionary<string, ModuleInfo>();
		List<ModuleInfo> list = new List<ModuleInfo>();
		List<ModuleInfo> physicalModules = GetPhysicalModules(isLauncher: false);
		List<ModuleInfo> platformModules = GetPlatformModules(platformModulePaths);
		list.AddRange(physicalModules);
		list.AddRange(platformModules);
		foreach (ModuleInfo item in list)
		{
			if (item.Name == "NavalDLC")
			{
				if (item.RequiredBaseVersion != ApplicationVersion.FromParametersFile())
				{
					string lpCaption = "ERROR";
					Debug.ShowMessageBox("NavalDLC version is not matched with base game! Please verify game files if the problem persists.", lpCaption, 4u);
					Environment.Exit(0);
				}
				VirtualFolders.PlatformDLCPaths.Add("NavalDLC", item.FolderPath);
			}
		}
		List<ModuleInfo> list2 = new List<ModuleInfo>();
		foreach (string moduleId in loadedModuleIds)
		{
			ModuleInfo moduleInfo = list.Find((ModuleInfo x) => x.Id.ToLower().Equals(moduleId.ToLower()));
			if (moduleInfo != null)
			{
				if (moduleInfo.IsOfficial)
				{
					list2.Add(moduleInfo);
					moduleInfo.UpdateVersionChangeSet();
				}
				if (!_loadedModules.ContainsKey(moduleInfo.Id.ToLower()))
				{
					_loadedModules.Add(moduleInfo.Id.ToLower(), moduleInfo);
				}
			}
		}
		foreach (ModuleInfo value in _loadedModules.Values)
		{
			foreach (DependedModule dependedModule in value.DependedModules)
			{
				if (list2.Any((ModuleInfo m) => m.Id == dependedModule.ModuleId))
				{
					dependedModule.UpdateVersionChangeSet();
				}
			}
		}
	}

	public static ModuleInfo InitializeSingleModule(string modulePath)
	{
		Debug.Print("###Trying to load Module:  " + modulePath);
		ModuleInfo moduleInfo = new ModuleInfo();
		try
		{
			moduleInfo.LoadWithFullPath(modulePath);
		}
		catch (Exception ex)
		{
			string lpText = string.Concat("Module " + modulePath + " can't be loaded, there are some errors exception follows:." + Environment.NewLine + ex.Message, ex.StackTrace);
			string lpCaption = "ERROR";
			Debug.ShowMessageBox(lpText, lpCaption, 4u);
		}
		if (moduleInfo.Name == "NavalDLC")
		{
			VirtualFolders.PlatformDLCPaths.Add("NavalDLC", moduleInfo.FolderPath);
		}
		if (!_loadedModules.ContainsKey(moduleInfo.Id.ToLower()))
		{
			_loadedModules.Add(moduleInfo.Id.ToLower(), moduleInfo);
		}
		return moduleInfo;
	}

	public static bool IsModuleActive(string moduleId)
	{
		bool result = false;
		ModuleInfo moduleInfo = GetModuleInfo(moduleId);
		if (moduleInfo != null)
		{
			result = moduleInfo.IsActive;
		}
		return result;
	}

	public static void InitializePlatformModuleExtension(IPlatformModuleExtension moduleExtension, List<string> args)
	{
		_platformModuleExtension = moduleExtension;
		_platformModuleExtension.Initialize(args);
	}

	public static void ClearPlatformModuleExtension()
	{
		if (_platformModuleExtension != null)
		{
			_platformModuleExtension.Destroy();
			_platformModuleExtension = null;
		}
	}

	public static List<ModuleInfo> GetModuleInfos(string[] moduleIds)
	{
		List<ModuleInfo> list = new List<ModuleInfo>();
		for (int i = 0; i < moduleIds.Length; i++)
		{
			ModuleInfo moduleInfo = GetModuleInfo(moduleIds[i]);
			if (moduleInfo != null)
			{
				list.Add(moduleInfo);
			}
		}
		return list;
	}

	public static List<ModuleInfo> GetModules(Func<ModuleInfo, bool> cond = null)
	{
		List<ModuleInfo> list = new List<ModuleInfo>();
		foreach (ModuleInfo value in _loadedModules.Values)
		{
			if (cond == null || cond(value))
			{
				list.Add(value);
			}
		}
		return list;
	}

	public static Dictionary<string, ModuleInfo>.ValueCollection GetAllModules()
	{
		return _loadedModules.Values;
	}

	public static List<ModuleInfo> GetActiveModules()
	{
		return GetModules((ModuleInfo x) => x.IsActive);
	}

	public static string GetMbprojPath(string id)
	{
		string key = id.ToLower();
		if (_loadedModules.ContainsKey(key))
		{
			return _loadedModules[key].FolderPath + "/ModuleData/project.mbproj";
		}
		return "";
	}

	public static string GetXmlPathForNative(string moduleId, string xmlName)
	{
		return GetModuleFullPath(moduleId) + xmlName;
	}

	public static string GetXmlPathForNativeWBase(string moduleId, string xmlName)
	{
		return "$BASE/Modules/" + moduleId + "/" + xmlName;
	}

	public static string GetXsltPathForNative(string moduleId, string xsltName)
	{
		xsltName = xsltName.Remove(xsltName.Length - 4);
		return GetModuleFullPath(moduleId) + xsltName + ".xsl";
	}

	public static string GetPath(string id)
	{
		return GetModuleFullPath(id) + "SubModule.xml";
	}

	public static string GetXmlPath(string moduleId, string xmlName)
	{
		return GetModuleFullPath(moduleId) + "ModuleData/" + xmlName + ".xml";
	}

	public static string GetXsltPath(string moduleId, string xmlName)
	{
		return GetModuleFullPath(moduleId) + "ModuleData/" + xmlName + ".xsl";
	}

	public static string GetXsdPathForModules(string moduleId, string xsdName)
	{
		return GetModuleFullPath(moduleId) + "ModuleData/XmlSchemas/" + xsdName + ".xsd";
	}

	public static string GetXsdPath(string xmlInfoId)
	{
		return BasePath.Name + "XmlSchemas/" + xmlInfoId + ".xsd";
	}

	public static IEnumerable<ModuleInfo> GetDependentModulesOf(IEnumerable<ModuleInfo> source, ModuleInfo module)
	{
		foreach (DependedModule item in module.DependedModules)
		{
			ModuleInfo moduleInfo = source.FirstOrDefault((ModuleInfo i) => i.Id == item.ModuleId);
			if (moduleInfo != null)
			{
				yield return moduleInfo;
			}
		}
		foreach (ModuleInfo item2 in source)
		{
			if (item2.ModulesToLoadAfterThis.Any((DependedModule m) => m.ModuleId == module.Id))
			{
				yield return item2;
			}
		}
	}

	public static List<ModuleInfo> GetSortedModules(string[] moduleIDs)
	{
		List<ModuleInfo> modules = GetModuleInfos(moduleIDs);
		IList<ModuleInfo> list = MBMath.TopologySort(modules, (ModuleInfo module) => GetDependentModulesOf(modules, module));
		if (!(list is List<ModuleInfo> result))
		{
			return list.ToList();
		}
		return result;
	}

	public static List<ModuleInfo> GetModulesForLauncher()
	{
		if (results == null)
		{
			results = new List<ModuleInfo>();
			List<ModuleInfo> physicalModules = GetPhysicalModules(isLauncher: true);
			List<ModuleInfo> platformModules = GetPlatformModules();
			results.AddRange(physicalModules);
			results.AddRange(platformModules);
			List<ModuleInfo> list = new List<ModuleInfo>();
			foreach (ModuleInfo result in results)
			{
				if (result.IsOfficial)
				{
					list.Add(result);
					result.UpdateVersionChangeSet();
				}
			}
			foreach (ModuleInfo result2 in results)
			{
				foreach (DependedModule dependedModule in result2.DependedModules)
				{
					if (list.Any((ModuleInfo m) => m.Id == dependedModule.ModuleId))
					{
						dependedModule.UpdateVersionChangeSet();
					}
				}
			}
		}
		return results;
	}

	public static MBList<string> GetOfficialModuleIds()
	{
		return new MBList<string> { "Native", "Multiplayer", "SandBoxCore", "Sandbox", "CustomBattle", "StoryMode", "NavalDLC", "BirthAndDeath", "FastMode" };
	}

	private static List<ModuleInfo> GetPhysicalModules(bool isLauncher)
	{
		List<ModuleInfo> list = new List<ModuleInfo>();
		string[] directories = Directory.GetDirectories(_pathPrefix);
		foreach (string text in directories)
		{
			try
			{
				string path = Path.Combine(text, "SubModule.xml");
				if (!File.Exists(path))
				{
					continue;
				}
				ModuleInfo moduleInfo = new ModuleInfo();
				string directoryName = Path.GetDirectoryName(path);
				moduleInfo.LoadWithFullPath(directoryName);
				if (!(moduleInfo.Name == "NavalDLC") || _platformModuleExtension == null)
				{
					goto IL_00af;
				}
				if (!_platformModuleExtension.CheckEntitlement("Mount & Blade II: Bannerlord - War Sails"))
				{
					continue;
				}
				if (moduleInfo.RequiredBaseVersion != ApplicationVersion.FromParametersFile())
				{
					string lpCaption = "ERROR";
					Debug.ShowMessageBox("NavalDLC version is not matched with base game! Please verify game files if the problem persists.", lpCaption, 4u);
					if (!isLauncher)
					{
						Environment.Exit(0);
					}
				}
				goto IL_00af;
				IL_00af:
				list.Add(moduleInfo);
			}
			catch (Exception ex)
			{
				string lpText = "Module " + text + " can't be loaded, there are some errors." + Environment.NewLine + Environment.NewLine + ex.Message;
				string lpCaption2 = "ERROR";
				Debug.ShowMessageBox(lpText, lpCaption2, 4u);
			}
		}
		return list;
	}

	public static MBList<Assembly> GetActiveGameAssemblies()
	{
		HashSet<Assembly> hashSet = new HashSet<Assembly>();
		Queue<Assembly> queue = new Queue<Assembly>();
		Dictionary<string, Assembly> dictionary = new Dictionary<string, Assembly>();
		Assembly[] array;
		try
		{
			array = AppDomain.CurrentDomain.GetAssemblies();
		}
		catch (Exception ex)
		{
			Debug.FailedAssert(ex.Message, "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\Base\\TaleWorlds.ModuleManager\\ModuleHelper.cs", "GetActiveGameAssemblies", 443);
			array = new Assembly[0];
		}
		if (IsTestMode)
		{
			return array.ToMBList();
		}
		Assembly[] array2 = array;
		foreach (Assembly assembly in array2)
		{
			foreach (ModuleInfo activeModule in GetActiveModules())
			{
				if (IsAssemblyDirectlyReferencedInModule(assembly, activeModule))
				{
					queue.Enqueue(assembly);
				}
			}
			dictionary[assembly.GetName().Name] = assembly;
		}
		MBList<Assembly> mBList = new MBList<Assembly>();
		while (queue.Count > 0)
		{
			Assembly assembly2 = queue.Dequeue();
			if (!hashSet.Add(assembly2))
			{
				continue;
			}
			mBList.Add(assembly2);
			AssemblyName[] referencedAssemblies = assembly2.GetReferencedAssemblies();
			foreach (AssemblyName assemblyName in referencedAssemblies)
			{
				if (dictionary.ContainsKey(assemblyName.Name))
				{
					Assembly assembly3 = dictionary[assemblyName.Name];
					if (IsGameAssembly(assembly3))
					{
						queue.Enqueue(assembly3);
					}
				}
			}
		}
		return mBList;
	}

	private static bool IsAssemblyDirectlyReferencedInModule(Assembly assembly, ModuleInfo moduleInfo)
	{
		string text = assembly.GetName().Name + ".dll";
		foreach (SubModuleInfo subModule in moduleInfo.SubModules)
		{
			if (subModule.DLLName == text)
			{
				return true;
			}
			foreach (string assembly2 in subModule.Assemblies)
			{
				if (assembly2 == text)
				{
					return true;
				}
			}
		}
		return false;
	}

	private static bool IsGameAssembly(Assembly assembly)
	{
		AssemblyName name = assembly.GetName();
		if (!name.Name.StartsWith("System") && !name.Name.StartsWith("Microsoft") && !name.Name.StartsWith("mscorlib"))
		{
			return !name.Name.StartsWith("netstandard");
		}
		return false;
	}

	private static List<ModuleInfo> GetPlatformModules(string[] platformModulePaths = null)
	{
		List<ModuleInfo> list = new List<ModuleInfo>();
		if (platformModulePaths != null)
		{
			Debug.Print("GetPlatformModules platformModulePaths != null");
			string[] array = platformModulePaths;
			foreach (string text in array)
			{
				ModuleInfo moduleInfo = new ModuleInfo();
				try
				{
					moduleInfo.LoadWithFullPath(text);
					Debug.Print("dir " + text);
					list.Add(moduleInfo);
				}
				catch (Exception ex)
				{
					string lpText = "Module " + moduleInfo.Name + " with dir: " + text + " can't be loaded, there are some errors." + Environment.NewLine + Environment.NewLine + ex.ToString();
					string lpCaption = "ERROR";
					Debug.ShowMessageBox(lpText, lpCaption, 4u);
				}
			}
		}
		if (_platformModuleExtension != null)
		{
			string[] array = _platformModuleExtension.GetModulePaths();
			foreach (string text2 in array)
			{
				ModuleInfo moduleInfo2 = new ModuleInfo();
				try
				{
					moduleInfo2.LoadWithFullPath(text2);
					list.Add(moduleInfo2);
				}
				catch (Exception ex2)
				{
					string lpText2 = "Module " + moduleInfo2.Name + " with dir: " + text2 + " can't be loaded, there are some errors." + Environment.NewLine + Environment.NewLine + ex2.ToString();
					string lpCaption2 = "ERROR";
					Debug.ShowMessageBox(lpText2, lpCaption2, 4u);
				}
			}
		}
		return list;
	}
}
