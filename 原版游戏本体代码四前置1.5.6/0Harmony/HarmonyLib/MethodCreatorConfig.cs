using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using MonoMod.Utils;

namespace HarmonyLib;

internal class MethodCreatorConfig
{
	internal readonly MethodBase original;

	internal readonly MethodBase source;

	internal readonly List<MethodInfo> prefixes;

	internal readonly List<MethodInfo> postfixes;

	internal readonly List<MethodInfo> transpilers;

	internal readonly List<MethodInfo> finalizers;

	internal readonly List<Infix> innerprefixes;

	internal readonly List<Infix> innerpostfixes;

	internal readonly bool debug;

	internal int patchIndex;

	internal DynamicMethodDefinition patch;

	internal Dictionary<MethodInfo, List<InjectedParameter>> injections;

	internal Type returnType;

	internal ILGenerator il;

	internal List<CodeInstruction> instructions;

	internal LocalBuilder[] originalVariables;

	internal VariableState localVariables;

	internal LocalBuilder resultVariable;

	internal Label? skipOriginalLabel;

	internal LocalBuilder runOriginalVariable;

	internal LocalBuilder exceptionVariable;

	internal LocalBuilder finalizedVariable;

	internal MethodBase MethodBase => source ?? original;

	internal bool OriginalIsStatic => original.IsStatic;

	internal IEnumerable<MethodInfo> Fixes => prefixes.Union(postfixes).Union(finalizers);

	internal IEnumerable<Infix> InnerFixes => innerprefixes.Union(innerpostfixes);

	internal MethodCreatorConfig(MethodBase original, MethodBase source, List<MethodInfo> prefixes, List<MethodInfo> postfixes, List<MethodInfo> transpilers, List<MethodInfo> finalizers, List<Infix> innerprefixes, List<Infix> innerpostfixes, bool debug)
	{
		this.original = original;
		this.source = source;
		this.prefixes = prefixes;
		this.postfixes = postfixes;
		this.transpilers = transpilers;
		this.finalizers = finalizers;
		this.innerprefixes = innerprefixes;
		this.innerpostfixes = innerpostfixes;
		this.debug = debug;
	}

	internal bool Prepare()
	{
		PatchInfo patchInfo = HarmonySharedState.GetPatchInfo(original) ?? new PatchInfo();
		patchIndex = patchInfo.VersionCount + 1;
		patch = MethodPatcherTools.CreateDynamicMethod(original, $"_Patch{patchIndex}", debug);
		if (patch == null)
		{
			return false;
		}
		injections = Fixes.Union(InnerFixes.Select((Infix fix) => fix.OuterMethod)).ToDictionary((MethodInfo fix) => fix, (MethodInfo fix) => (from p in fix.GetParameters()
			select new InjectedParameter(fix, p)).ToList());
		returnType = AccessTools.GetReturnedType(original);
		il = patch.GetILGenerator();
		instructions = new List<CodeInstruction>();
		return true;
	}

	internal void AddCode(CodeInstruction code)
	{
		instructions.Add(code);
	}

	internal void AddCodes(IEnumerable<CodeInstruction> codes)
	{
		instructions.AddRange(codes);
	}

	internal void AddLocal(InjectionType type, LocalBuilder local)
	{
		localVariables.Add(type, local);
	}

	internal void AddLocal(string name, LocalBuilder local)
	{
		localVariables.Add(name, local);
	}

	internal LocalBuilder GetLocal(InjectionType type)
	{
		return localVariables[type];
	}

	internal LocalBuilder GetLocal(string name)
	{
		return localVariables[name];
	}

	internal bool HasLocal(string name)
	{
		LocalBuilder local;
		return localVariables.TryGetValue(name, out local);
	}

	internal LocalBuilder DeclareLocal(Type type, bool isPinned = false)
	{
		return il.DeclareLocal(type, isPinned);
	}

	internal Label DefineLabel()
	{
		return il.DefineLabel();
	}

	internal IEnumerable<InjectedParameter> InjectionsFor(MethodInfo fix, InjectionType type = InjectionType.Unknown)
	{
		if (injections.TryGetValue(fix, out var value))
		{
			if (type != InjectionType.Unknown)
			{
				return value.Where((InjectedParameter pair) => pair.injectionType == type);
			}
			return value;
		}
		return Array.Empty<InjectedParameter>();
	}

	internal bool AnyFixHas(InjectionType type)
	{
		return injections.Values.SelectMany((List<InjectedParameter> list) => list).Any((InjectedParameter pair) => pair.injectionType == type);
	}

	internal void WithFixes(Action<MethodInfo> action)
	{
		foreach (MethodInfo fix in Fixes)
		{
			action(fix);
		}
		foreach (Infix innerFix in InnerFixes)
		{
			action(innerFix.OuterMethod);
		}
	}
}
