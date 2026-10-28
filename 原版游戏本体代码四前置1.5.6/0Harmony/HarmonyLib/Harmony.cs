using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using MonoMod;

namespace HarmonyLib;

public class Harmony
{
	public static bool DEBUG;

	private static readonly ConditionalWeakTable<Assembly, Dictionary<string, List<Type>>> AssemblyCachedCategories = new ConditionalWeakTable<Assembly, Dictionary<string, List<Type>>>();

	public string Id { get; private set; }

	public Harmony(string id)
	{
		if (string.IsNullOrEmpty(id))
		{
			throw new ArgumentException("id cannot be null or empty");
		}
		try
		{
			string environmentVariable = Environment.GetEnvironmentVariable("HARMONY_DEBUG");
			if (environmentVariable != null && environmentVariable.Length > 0)
			{
				environmentVariable = environmentVariable.Trim();
				DEBUG = environmentVariable == "1" || bool.Parse(environmentVariable);
			}
		}
		catch
		{
		}
		if (DEBUG)
		{
			Assembly assembly = typeof(Harmony).Assembly;
			Version version = assembly.GetName().Version;
			string value = assembly.Location;
			string value2 = Environment.Version.ToString();
			string value3 = Environment.OSVersion.Platform.ToString();
			if (string.IsNullOrEmpty(value))
			{
				value = new Uri(assembly.CodeBase).LocalPath;
			}
			FileLog.Log($"### Harmony id={id}, version={version}, location={value}, env/clr={value2}, platform={value3}");
			MethodBase outsideCaller = AccessTools.GetOutsideCaller();
			if ((object)outsideCaller.DeclaringType != null)
			{
				Assembly assembly2 = outsideCaller.DeclaringType.Assembly;
				value = assembly2.Location;
				if (string.IsNullOrEmpty(value))
				{
					value = new Uri(assembly2.CodeBase).LocalPath;
				}
				FileLog.Log("### Started from " + outsideCaller.FullDescription() + ", location " + value);
				FileLog.Log($"### At {DateTime.Now:yyyy-MM-dd hh.mm.ss}");
			}
		}
		Id = id;
	}

	public void PatchAll()
	{
		MethodBase method = new StackTrace().GetFrame(1).GetMethod();
		Assembly assembly = method.ReflectedType.Assembly;
		PatchAll(assembly);
	}

	public PatchProcessor CreateProcessor(MethodBase original)
	{
		return new PatchProcessor(this, original);
	}

	public PatchClassProcessor CreateClassProcessor(Type type)
	{
		return new PatchClassProcessor(this, type);
	}

	public ReversePatcher CreateReversePatcher(MethodBase original, HarmonyMethod standin)
	{
		return new ReversePatcher(this, original, standin);
	}

	public void PatchAll(Assembly assembly)
	{
		AccessTools.GetTypesFromAssembly(assembly).DoIf((Type type) => type.HasHarmonyAttribute(), delegate(Type type)
		{
			CreateClassProcessor(type).Patch();
		});
	}

	public void PatchAllUncategorized()
	{
		MethodBase method = new StackTrace().GetFrame(1).GetMethod();
		Assembly assembly = method.ReflectedType.Assembly;
		PatchAllUncategorized(assembly);
	}

	public void PatchAllUncategorized(Assembly assembly)
	{
		PatchClassProcessor[] sequence = (from type in AccessTools.GetTypesFromAssembly(assembly)
			where type.HasHarmonyAttribute()
			select type).Select(CreateClassProcessor).ToArray();
		sequence.DoIf((PatchClassProcessor patchClass) => string.IsNullOrEmpty(patchClass.Category), delegate(PatchClassProcessor patchClass)
		{
			patchClass.Patch();
		});
	}

	public void PatchCategory(string category)
	{
		MethodBase method = new StackTrace().GetFrame(1).GetMethod();
		Assembly assembly = method.ReflectedType.Assembly;
		PatchCategory(assembly, category);
	}

	public void PatchCategory(Assembly assembly, string category)
	{
		Dictionary<string, List<Type>> value = AssemblyCachedCategories.GetValue(assembly, BuildCategoryCache);
		if (value.TryGetValue(category, out var value2))
		{
			value2.Do(delegate(Type type)
			{
				CreateClassProcessor(type).Patch();
			});
		}
	}

	private static Dictionary<string, List<Type>> BuildCategoryCache(Assembly assembly)
	{
		Dictionary<string, List<Type>> dictionary = new Dictionary<string, List<Type>>();
		Type[] typesFromAssembly = AccessTools.GetTypesFromAssembly(assembly);
		foreach (Type type in typesFromAssembly)
		{
			List<HarmonyMethod> fromType = HarmonyMethodExtensions.GetFromType(type);
			if (fromType.Count == 0)
			{
				continue;
			}
			HarmonyMethod harmonyMethod = HarmonyMethod.Merge(fromType);
			string category = harmonyMethod.category;
			if (!string.IsNullOrEmpty(category))
			{
				if (!dictionary.TryGetValue(category, out var value) && value == null)
				{
					value = new List<Type>();
				}
				value.Add(type);
				dictionary[category] = value;
			}
		}
		return dictionary;
	}

	public MethodInfo Patch(MethodBase original, HarmonyMethod prefix = null, HarmonyMethod postfix = null, HarmonyMethod transpiler = null, HarmonyMethod finalizer = null)
	{
		PatchProcessor patchProcessor = CreateProcessor(original);
		patchProcessor.AddPrefix(prefix);
		patchProcessor.AddPostfix(postfix);
		patchProcessor.AddTranspiler(transpiler);
		patchProcessor.AddFinalizer(finalizer);
		return patchProcessor.Patch();
	}

	public static MethodInfo ReversePatch(MethodBase original, HarmonyMethod standin, MethodInfo transpiler = null)
	{
		return PatchFunctions.ReversePatch(standin, original, transpiler);
	}

	public void UnpatchAll(string harmonyID = null)
	{
		List<MethodBase> list = GetAllPatchedMethods().ToList();
		foreach (MethodBase original in list)
		{
			bool flag = original.HasMethodBody();
			Patches patchInfo = GetPatchInfo(original);
			if (flag)
			{
				patchInfo.Postfixes.DoIf(IDCheck, delegate(Patch patch)
				{
					Unpatch(original, patch.PatchMethod);
				});
				patchInfo.Prefixes.DoIf(IDCheck, delegate(Patch patch)
				{
					Unpatch(original, patch.PatchMethod);
				});
				patchInfo.InnerPostfixes.DoIf(IDCheck, delegate(Patch patch)
				{
					Unpatch(original, patch.PatchMethod);
				});
				patchInfo.InnerPrefixes.DoIf(IDCheck, delegate(Patch patch)
				{
					Unpatch(original, patch.PatchMethod);
				});
			}
			patchInfo.Transpilers.DoIf(IDCheck, delegate(Patch patch)
			{
				Unpatch(original, patch.PatchMethod);
			});
			if (flag)
			{
				patchInfo.Finalizers.DoIf(IDCheck, delegate(Patch patch)
				{
					Unpatch(original, patch.PatchMethod);
				});
			}
		}
		bool IDCheck(Patch patch)
		{
			if (harmonyID != null)
			{
				return patch.owner == harmonyID;
			}
			return true;
		}
	}

	public void Unpatch(MethodBase original, HarmonyPatchType type, string harmonyID = "*")
	{
		PatchProcessor patchProcessor = CreateProcessor(original);
		patchProcessor.Unpatch(type, harmonyID);
	}

	public void Unpatch(MethodBase original, MethodInfo patch)
	{
		PatchProcessor patchProcessor = CreateProcessor(original);
		patchProcessor.Unpatch(patch);
	}

	public void UnpatchCategory(string category)
	{
		MethodBase method = new StackTrace().GetFrame(1).GetMethod();
		Assembly assembly = method.ReflectedType.Assembly;
		UnpatchCategory(assembly, category);
	}

	public void UnpatchCategory(Assembly assembly, string category)
	{
		Dictionary<string, List<Type>> value = AssemblyCachedCategories.GetValue(assembly, BuildCategoryCache);
		if (value.TryGetValue(category, out var value2))
		{
			value2.Do(delegate(Type type)
			{
				CreateClassProcessor(type).Unpatch();
			});
		}
	}

	public static bool HasAnyPatches(string harmonyID)
	{
		return GetAllPatchedMethods().Select(GetPatchInfo).Any((Patches info) => info.Owners.Contains(harmonyID));
	}

	public static Patches GetPatchInfo(MethodBase method)
	{
		return PatchProcessor.GetPatchInfo(method);
	}

	public IEnumerable<MethodBase> GetPatchedMethods()
	{
		return from original in GetAllPatchedMethods()
			where GetPatchInfo(original).Owners.Contains(Id)
			select original;
	}

	public static IEnumerable<MethodBase> GetAllPatchedMethods()
	{
		return PatchProcessor.GetAllPatchedMethods();
	}

	public static MethodBase GetOriginalMethod(MethodInfo replacement)
	{
		if (replacement == null)
		{
			throw new ArgumentNullException("replacement");
		}
		return HarmonySharedState.GetRealMethod(replacement, useReplacement: false);
	}

	public static MethodBase GetMethodFromStackframe(StackFrame frame)
	{
		if (frame == null)
		{
			throw new ArgumentNullException("frame");
		}
		return HarmonySharedState.GetStackFrameMethod(frame, useReplacement: true);
	}

	public static MethodBase GetOriginalMethodFromStackframe(StackFrame frame)
	{
		if (frame == null)
		{
			throw new ArgumentNullException("frame");
		}
		return HarmonySharedState.GetStackFrameMethod(frame, useReplacement: false);
	}

	public static Dictionary<string, Version> VersionInfo(out Version currentVersion)
	{
		return PatchProcessor.VersionInfo(out currentVersion);
	}

	public static void SetSwitch(string name, object value)
	{
		Switches.SetSwitchValue(name, value);
	}

	public static void ClearSwitch(string name)
	{
		Switches.ClearSwitchValue(name);
	}

	public static bool TryGetSwitch(string name, out object value)
	{
		return Switches.TryGetSwitchValue(name, out value);
	}

	public static bool TryIsSwitchEnabled(string name, out bool isEnabled)
	{
		return Switches.TryGetSwitchEnabled(name, out isEnabled);
	}
}
