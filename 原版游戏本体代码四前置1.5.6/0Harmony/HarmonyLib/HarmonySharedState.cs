using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Mono.Cecil;
using MonoMod.Utils;

namespace HarmonyLib;

internal static class HarmonySharedState
{
	private const string name = "HarmonySharedState";

	internal const int internalVersion = 102;

	private static readonly Dictionary<MethodBase, byte[]> state;

	private static readonly Dictionary<MethodInfo, MethodBase> originals;

	private static readonly Dictionary<long, MethodBase[]> originalsMono;

	private static readonly AccessTools.FieldRef<StackFrame, long> methodAddressRef;

	internal static readonly int actualVersion;

	static HarmonySharedState()
	{
		Type orCreateSharedStateType = GetOrCreateSharedStateType();
		if (AccessTools.IsMonoRuntime)
		{
			FieldInfo fieldInfo = AccessTools.Field(typeof(StackFrame), "methodAddress");
			if ((object)fieldInfo != null)
			{
				methodAddressRef = AccessTools.FieldRefAccess<StackFrame, long>(fieldInfo);
			}
		}
		FieldInfo field = orCreateSharedStateType.GetField("version");
		if ((int)field.GetValue(null) == 0)
		{
			field.SetValue(null, 102);
		}
		actualVersion = (int)field.GetValue(null);
		FieldInfo field2 = orCreateSharedStateType.GetField("state");
		if (field2.GetValue(null) == null)
		{
			field2.SetValue(null, new Dictionary<MethodBase, byte[]>());
		}
		FieldInfo field3 = orCreateSharedStateType.GetField("originals");
		if (field3 != null && field3.GetValue(null) == null)
		{
			field3.SetValue(null, new Dictionary<MethodInfo, MethodBase>());
		}
		FieldInfo field4 = orCreateSharedStateType.GetField("originalsMono");
		if (field4 != null && field4.GetValue(null) == null)
		{
			field4.SetValue(null, new Dictionary<long, MethodBase[]>());
		}
		state = (Dictionary<MethodBase, byte[]>)field2.GetValue(null);
		originals = new Dictionary<MethodInfo, MethodBase>();
		if (field3 != null)
		{
			originals = (Dictionary<MethodInfo, MethodBase>)field3.GetValue(null);
		}
		originalsMono = new Dictionary<long, MethodBase[]>();
		if (field4 != null)
		{
			originalsMono = (Dictionary<long, MethodBase[]>)field4.GetValue(null);
		}
	}

	private static Type GetOrCreateSharedStateType()
	{
		Type type = Type.GetType("HarmonySharedState", throwOnError: false);
		if (type != null)
		{
			return type;
		}
		using ModuleDefinition moduleDefinition = ModuleDefinition.CreateModule("HarmonySharedState", new ModuleParameters
		{
			Kind = ModuleKind.Dll,
			ReflectionImporterProvider = MMReflectionImporter.Provider
		});
		Mono.Cecil.TypeAttributes attributes = Mono.Cecil.TypeAttributes.Public | Mono.Cecil.TypeAttributes.Abstract | Mono.Cecil.TypeAttributes.Sealed;
		TypeDefinition typeDefinition = new TypeDefinition("", "HarmonySharedState", attributes)
		{
			BaseType = moduleDefinition.TypeSystem.Object
		};
		moduleDefinition.Types.Add(typeDefinition);
		typeDefinition.Fields.Add(new FieldDefinition("state", Mono.Cecil.FieldAttributes.Public | Mono.Cecil.FieldAttributes.Static, moduleDefinition.ImportReference(typeof(Dictionary<MethodBase, byte[]>))));
		typeDefinition.Fields.Add(new FieldDefinition("originals", Mono.Cecil.FieldAttributes.Public | Mono.Cecil.FieldAttributes.Static, moduleDefinition.ImportReference(typeof(Dictionary<MethodInfo, MethodBase>))));
		typeDefinition.Fields.Add(new FieldDefinition("originalsMono", Mono.Cecil.FieldAttributes.Public | Mono.Cecil.FieldAttributes.Static, moduleDefinition.ImportReference(typeof(Dictionary<long, MethodBase[]>))));
		typeDefinition.Fields.Add(new FieldDefinition("version", Mono.Cecil.FieldAttributes.Public | Mono.Cecil.FieldAttributes.Static, moduleDefinition.ImportReference(typeof(int))));
		return ReflectionHelper.Load(moduleDefinition).GetType("HarmonySharedState");
	}

	internal static PatchInfo GetPatchInfo(MethodBase method)
	{
		byte[] valueSafe;
		lock (state)
		{
			valueSafe = state.GetValueSafe(method);
		}
		if (valueSafe == null)
		{
			return null;
		}
		return PatchInfoSerialization.Deserialize(valueSafe);
	}

	internal static IEnumerable<MethodBase> GetPatchedMethods()
	{
		lock (state)
		{
			return state.Keys.ToArray();
		}
	}

	internal static void UpdatePatchInfo(MethodBase original, MethodInfo replacement, PatchInfo patchInfo)
	{
		patchInfo.VersionCount++;
		byte[] value = patchInfo.Serialize();
		lock (state)
		{
			state[original] = value;
		}
		lock (originals)
		{
			originals[replacement.Identifiable()] = original;
		}
		if (AccessTools.IsMonoRuntime)
		{
			long key = (long)replacement.MethodHandle.GetFunctionPointer();
			lock (originalsMono)
			{
				originalsMono[key] = new MethodBase[2] { original, replacement };
			}
		}
	}

	internal static MethodBase GetRealMethod(MethodInfo method, bool useReplacement)
	{
		MethodInfo key = method.Identifiable();
		lock (originals)
		{
			if (originals.TryGetValue(key, out var value))
			{
				return value;
			}
		}
		if (AccessTools.IsMonoRuntime)
		{
			long key2 = (long)method.MethodHandle.GetFunctionPointer();
			lock (originalsMono)
			{
				if (originalsMono.TryGetValue(key2, out var value2))
				{
					return useReplacement ? value2[1] : value2[0];
				}
			}
		}
		return method;
	}

	internal static MethodBase GetStackFrameMethod(StackFrame frame, bool useReplacement)
	{
		MethodInfo methodInfo = frame.GetMethod() as MethodInfo;
		if (methodInfo != null)
		{
			return GetRealMethod(methodInfo, useReplacement);
		}
		if (methodAddressRef != null)
		{
			long key = methodAddressRef(frame);
			lock (originalsMono)
			{
				if (originalsMono.TryGetValue(key, out var value))
				{
					return useReplacement ? value[1] : value[0];
				}
			}
		}
		return null;
	}
}
