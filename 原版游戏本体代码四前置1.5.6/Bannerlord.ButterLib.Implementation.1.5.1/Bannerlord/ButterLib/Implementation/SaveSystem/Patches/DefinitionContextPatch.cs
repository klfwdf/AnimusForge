using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Bannerlord.ButterLib.Common.Extensions;
using HarmonyLib;
using HarmonyLib.BUTR.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using TaleWorlds.SaveSystem.Definition;

namespace Bannerlord.ButterLib.Implementation.SaveSystem.Patches;

internal sealed class DefinitionContextPatch
{
	private delegate bool IsContainerDelegate(Type type);

	private abstract class Patch
	{
		private readonly string PatchMethodName;

		internal readonly MethodInfo? PatchMethod;

		internal readonly MethodInfo? TargetMethod;

		internal virtual bool IsReady => ThisNotNull(PatchMethod, "PatchMethod") & ThisNotNull(TargetMethod, "TargetMethod");

		protected Patch(string patchMethodName, MethodInfo? targetMethod)
		{
			PatchMethodName = patchMethodName;
			PatchMethod = ResolvePatchMethod();
			TargetMethod = targetMethod;
		}

		internal abstract bool EnablePatch(Harmony harmony);

		private MethodInfo? ResolvePatchMethod()
		{
			return AccessTools2.Method(GetType().DeclaringType, PatchMethodName);
		}

		protected bool ThisNotNull(object? obj, string objName)
		{
			return NotNull(obj, objName, "Patch " + PatchMethodName + ": ");
		}
	}

	private sealed class ConstructContainerDefinitionPrefixPatch : PrefixPatch
	{
		internal override bool IsReady => base.IsReady & ThisNotNull(IsContainer, "IsContainer delegate");

		internal ConstructContainerDefinitionPrefixPatch()
			: base("ConstructContainerDefinitionPrefix", TargetTypeMethod("ConstructContainerDefinition"))
		{
		}
	}

	private class PrefixPatch : Patch
	{
		internal PrefixPatch(string patchMethodName, MethodInfo? targetMethod)
			: base(patchMethodName, targetMethod)
		{
		}

		internal override bool EnablePatch(Harmony harmony)
		{
			return (object)harmony.Patch(TargetMethod, new HarmonyMethod(PatchMethod)) != null;
		}
	}

	private static ILogger _log = null;

	private static readonly IsContainerDelegate? IsContainer = AccessTools2.GetDelegate<IsContainerDelegate>("TaleWorlds.SaveSystem.TypeExtensions:IsContainer", new Type[1] { typeof(Type) });

	private static readonly Patch[] Patches = new Patch[10]
	{
		new PrefixPatch("AddBasicTypeDefinitionPrefix", TargetTypeMethod("AddBasicTypeDefinition")),
		new PrefixPatch("AddClassDefinitionPrefix", TargetTypeMethod("AddClassDefinition")),
		new PrefixPatch("AddContainerDefinitionPrefix", TargetTypeMethod("AddContainerDefinition")),
		new PrefixPatch("AddEnumDefinitionPrefix", TargetTypeMethod("AddEnumDefinition")),
		new PrefixPatch("AddGenericClassDefinitionPrefix", TargetTypeMethod("AddGenericClassDefinition")),
		new PrefixPatch("AddGenericStructDefinitionPrefix", TargetTypeMethod("AddGenericStructDefinition")),
		new PrefixPatch("AddInterfaceDefinitionPrefix", TargetTypeMethod("AddInterfaceDefinition")),
		new PrefixPatch("AddRootClassDefinitionPrefix", TargetTypeMethod("AddRootClassDefinition")),
		new PrefixPatch("AddStructDefinitionPrefix", TargetTypeMethod("AddStructDefinition")),
		new ConstructContainerDefinitionPrefixPatch()
	};

	internal static bool Enable(Harmony harmony)
	{
		_log = (ButterLibSubModule.Instance?.GetServiceProvider() ?? ButterLibSubModule.Instance?.GetTempServiceProvider())?.GetService<ILogger<DefinitionContextPatch>>() ?? NullLogger<DefinitionContextPatch>.Instance;
		if (Patches.Select((Patch p) => p.IsReady).All((bool ready) => ready))
		{
			return Patches.All((Patch p) => p.EnablePatch(harmony));
		}
		return false;
	}

	internal static bool Disable(Harmony harmony)
	{
		return false;
	}

	private static MethodInfo? TargetTypeMethod(string name)
	{
		return AccessTools2.Method("TaleWorlds.SaveSystem.Definition.DefinitionContext:" + name);
	}

	private static bool CanAddTypeDefinition(TypeDefinitionBase? typeDef, Dictionary<Type, TypeDefinitionBase> typeDict)
	{
		if ((object)typeDef?.Type == null)
		{
			return false;
		}
		if (typeDict.ContainsKey(typeDef.Type))
		{
			_log.LogTrace("Suppressed duplicate definition of serializable type: {Type}", typeDef.Type.FullName);
			return false;
		}
		return true;
	}

	private static bool AddRootClassDefinitionPrefix(TypeDefinitionBase? rootClassDefinition, Dictionary<Type, TypeDefinitionBase> ____allTypeDefinitions)
	{
		return CanAddTypeDefinition(rootClassDefinition, ____allTypeDefinitions);
	}

	private static bool AddClassDefinitionPrefix(TypeDefinitionBase? classDefinition, Dictionary<Type, TypeDefinitionBase> ____allTypeDefinitions)
	{
		return CanAddTypeDefinition(classDefinition, ____allTypeDefinitions);
	}

	private static bool AddStructDefinitionPrefix(TypeDefinitionBase? structDefinition, Dictionary<Type, TypeDefinitionBase> ____allTypeDefinitions)
	{
		return CanAddTypeDefinition(structDefinition, ____allTypeDefinitions);
	}

	private static bool AddInterfaceDefinitionPrefix(TypeDefinitionBase? interfaceDefinition, Dictionary<Type, TypeDefinitionBase> ____allTypeDefinitions)
	{
		return CanAddTypeDefinition(interfaceDefinition, ____allTypeDefinitions);
	}

	private static bool AddEnumDefinitionPrefix(TypeDefinitionBase? enumDefinition, Dictionary<Type, TypeDefinitionBase> ____allTypeDefinitions)
	{
		return CanAddTypeDefinition(enumDefinition, ____allTypeDefinitions);
	}

	private static bool AddContainerDefinitionPrefix(TypeDefinitionBase? containerDefinition, Dictionary<Type, TypeDefinitionBase> ____allTypeDefinitions)
	{
		return CanAddTypeDefinition(containerDefinition, ____allTypeDefinitions);
	}

	private static bool AddBasicTypeDefinitionPrefix(TypeDefinitionBase? basicTypeDefinition, Dictionary<Type, TypeDefinitionBase> ____allTypeDefinitions)
	{
		return CanAddTypeDefinition(basicTypeDefinition, ____allTypeDefinitions);
	}

	private static bool AddGenericClassDefinitionPrefix(TypeDefinitionBase? genericClassDefinition, Dictionary<Type, TypeDefinitionBase> ____allTypeDefinitions)
	{
		return CanAddTypeDefinition(genericClassDefinition, ____allTypeDefinitions);
	}

	private static bool AddGenericStructDefinitionPrefix(TypeDefinitionBase? genericStructDefinition, Dictionary<Type, TypeDefinitionBase> ____allTypeDefinitions)
	{
		return CanAddTypeDefinition(genericStructDefinition, ____allTypeDefinitions);
	}

	private static bool ConstructContainerDefinitionPrefix(Type? type)
	{
		if ((object)type != null)
		{
			return IsContainer(type);
		}
		return false;
	}

	private static bool NotNull<T>(T obj, string name, string? errPrefix = null) where T : class?
	{
		if (obj == null)
		{
			string text = errPrefix ?? string.Empty;
			_log.LogError("{Prefix}{Name} is null!", text, name);
			return false;
		}
		return true;
	}
}
