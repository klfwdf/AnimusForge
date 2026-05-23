using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Mono.Cecil;
using MonoMod.Utils;

namespace HarmonyLib
{
	// Token: 0x0200001F RID: 31
	internal static class HarmonySharedState
	{
		// Token: 0x0600009C RID: 156 RVA: 0x00004A84 File Offset: 0x00002C84
		static HarmonySharedState()
		{
			Type type = HarmonySharedState.GetOrCreateSharedStateType();
			if (AccessTools.IsMonoRuntime)
			{
				FieldInfo field = AccessTools.Field(typeof(StackFrame), "methodAddress");
				if (field != null)
				{
					HarmonySharedState.methodAddressRef = AccessTools.FieldRefAccess<StackFrame, long>(field);
				}
			}
			FieldInfo versionField = type.GetField("version");
			if ((int)versionField.GetValue(null) == 0)
			{
				versionField.SetValue(null, 102);
			}
			HarmonySharedState.actualVersion = (int)versionField.GetValue(null);
			FieldInfo stateField = type.GetField("state");
			if (stateField.GetValue(null) == null)
			{
				stateField.SetValue(null, new Dictionary<MethodBase, byte[]>());
			}
			FieldInfo originalsField = type.GetField("originals");
			if (originalsField != null && originalsField.GetValue(null) == null)
			{
				originalsField.SetValue(null, new Dictionary<MethodInfo, MethodBase>());
			}
			FieldInfo originalsMonoField = type.GetField("originalsMono");
			if (originalsMonoField != null && originalsMonoField.GetValue(null) == null)
			{
				originalsMonoField.SetValue(null, new Dictionary<long, MethodBase[]>());
			}
			HarmonySharedState.state = (Dictionary<MethodBase, byte[]>)stateField.GetValue(null);
			HarmonySharedState.originals = new Dictionary<MethodInfo, MethodBase>();
			if (originalsField != null)
			{
				HarmonySharedState.originals = (Dictionary<MethodInfo, MethodBase>)originalsField.GetValue(null);
			}
			HarmonySharedState.originalsMono = new Dictionary<long, MethodBase[]>();
			if (originalsMonoField != null)
			{
				HarmonySharedState.originalsMono = (Dictionary<long, MethodBase[]>)originalsMonoField.GetValue(null);
			}
		}

		// Token: 0x0600009D RID: 157 RVA: 0x00004BD4 File Offset: 0x00002DD4
		private static Type GetOrCreateSharedStateType()
		{
			Type type = Type.GetType("HarmonySharedState", false);
			if (type != null)
			{
				return type;
			}
			Type type2;
			using (ModuleDefinition module = ModuleDefinition.CreateModule("HarmonySharedState", new ModuleParameters
			{
				Kind = ModuleKind.Dll,
				ReflectionImporterProvider = MMReflectionImporter.Provider
			}))
			{
				Mono.Cecil.TypeAttributes attr = Mono.Cecil.TypeAttributes.Public | Mono.Cecil.TypeAttributes.Abstract | Mono.Cecil.TypeAttributes.Sealed;
				TypeDefinition typedef = new TypeDefinition("", "HarmonySharedState", attr)
				{
					BaseType = module.TypeSystem.Object
				};
				module.Types.Add(typedef);
				typedef.Fields.Add(new FieldDefinition("state", Mono.Cecil.FieldAttributes.FamANDAssem | Mono.Cecil.FieldAttributes.Family | Mono.Cecil.FieldAttributes.Static, module.ImportReference(typeof(Dictionary<MethodBase, byte[]>))));
				typedef.Fields.Add(new FieldDefinition("originals", Mono.Cecil.FieldAttributes.FamANDAssem | Mono.Cecil.FieldAttributes.Family | Mono.Cecil.FieldAttributes.Static, module.ImportReference(typeof(Dictionary<MethodInfo, MethodBase>))));
				typedef.Fields.Add(new FieldDefinition("originalsMono", Mono.Cecil.FieldAttributes.FamANDAssem | Mono.Cecil.FieldAttributes.Family | Mono.Cecil.FieldAttributes.Static, module.ImportReference(typeof(Dictionary<long, MethodBase[]>))));
				typedef.Fields.Add(new FieldDefinition("version", Mono.Cecil.FieldAttributes.FamANDAssem | Mono.Cecil.FieldAttributes.Family | Mono.Cecil.FieldAttributes.Static, module.ImportReference(typeof(int))));
				type2 = ReflectionHelper.Load(module).GetType("HarmonySharedState");
			}
			return type2;
		}

		// Token: 0x0600009E RID: 158 RVA: 0x00004D1C File Offset: 0x00002F1C
		internal static PatchInfo GetPatchInfo(MethodBase method)
		{
			Dictionary<MethodBase, byte[]> obj = HarmonySharedState.state;
			byte[] bytes;
			lock (obj)
			{
				bytes = HarmonySharedState.state.GetValueSafe(method);
			}
			if (bytes == null)
			{
				return null;
			}
			return PatchInfoSerialization.Deserialize(bytes);
		}

		// Token: 0x0600009F RID: 159 RVA: 0x00004D6C File Offset: 0x00002F6C
		internal static IEnumerable<MethodBase> GetPatchedMethods()
		{
			Dictionary<MethodBase, byte[]> obj = HarmonySharedState.state;
			IEnumerable<MethodBase> result;
			lock (obj)
			{
				result = HarmonySharedState.state.Keys.ToArray<MethodBase>();
			}
			return result;
		}

		// Token: 0x060000A0 RID: 160 RVA: 0x00004DB8 File Offset: 0x00002FB8
		internal static void UpdatePatchInfo(MethodBase original, MethodInfo replacement, PatchInfo patchInfo)
		{
			patchInfo.VersionCount++;
			byte[] bytes = patchInfo.Serialize();
			Dictionary<MethodBase, byte[]> obj = HarmonySharedState.state;
			lock (obj)
			{
				HarmonySharedState.state[original] = bytes;
			}
			Dictionary<MethodInfo, MethodBase> obj2 = HarmonySharedState.originals;
			lock (obj2)
			{
				HarmonySharedState.originals[replacement.Identifiable()] = original;
			}
			if (AccessTools.IsMonoRuntime)
			{
				long methodAddress = (long)replacement.MethodHandle.GetFunctionPointer();
				Dictionary<long, MethodBase[]> obj3 = HarmonySharedState.originalsMono;
				lock (obj3)
				{
					HarmonySharedState.originalsMono[methodAddress] = new MethodBase[] { original, replacement };
				}
			}
		}

		// Token: 0x060000A1 RID: 161 RVA: 0x00004EB4 File Offset: 0x000030B4
		internal static MethodBase GetRealMethod(MethodInfo method, bool useReplacement)
		{
			MethodInfo identifiableMethod = method.Identifiable();
			Dictionary<MethodInfo, MethodBase> obj = HarmonySharedState.originals;
			lock (obj)
			{
				MethodBase original;
				if (HarmonySharedState.originals.TryGetValue(identifiableMethod, out original))
				{
					return original;
				}
			}
			if (AccessTools.IsMonoRuntime)
			{
				long methodAddress = (long)method.MethodHandle.GetFunctionPointer();
				Dictionary<long, MethodBase[]> obj2 = HarmonySharedState.originalsMono;
				lock (obj2)
				{
					MethodBase[] info;
					if (HarmonySharedState.originalsMono.TryGetValue(methodAddress, out info))
					{
						return useReplacement ? info[1] : info[0];
					}
				}
			}
			return method;
		}

		// Token: 0x060000A2 RID: 162 RVA: 0x00004F78 File Offset: 0x00003178
		internal unsafe static MethodBase GetStackFrameMethod(StackFrame frame, bool useReplacement)
		{
			MethodInfo method = frame.GetMethod() as MethodInfo;
			if (method != null)
			{
				return HarmonySharedState.GetRealMethod(method, useReplacement);
			}
			if (HarmonySharedState.methodAddressRef != null)
			{
				long methodAddress = *HarmonySharedState.methodAddressRef(frame);
				Dictionary<long, MethodBase[]> obj = HarmonySharedState.originalsMono;
				lock (obj)
				{
					MethodBase[] info;
					if (HarmonySharedState.originalsMono.TryGetValue(methodAddress, out info))
					{
						return useReplacement ? info[1] : info[0];
					}
				}
			}
			return null;
		}

		// Token: 0x0400004A RID: 74
		private const string name = "HarmonySharedState";

		// Token: 0x0400004B RID: 75
		internal const int internalVersion = 102;

		// Token: 0x0400004C RID: 76
		private static readonly Dictionary<MethodBase, byte[]> state;

		// Token: 0x0400004D RID: 77
		private static readonly Dictionary<MethodInfo, MethodBase> originals;

		// Token: 0x0400004E RID: 78
		private static readonly Dictionary<long, MethodBase[]> originalsMono;

		// Token: 0x0400004F RID: 79
		private static readonly AccessTools.FieldRef<StackFrame, long> methodAddressRef;

		// Token: 0x04000050 RID: 80
		internal static readonly int actualVersion;
	}
}
