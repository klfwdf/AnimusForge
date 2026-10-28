using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using MonoMod.Core;
using MonoMod.Utils;

namespace HarmonyLib;

internal static class PatchTools
{
	private static readonly Dictionary<MethodBase, ICoreDetour> detours = new Dictionary<MethodBase, ICoreDetour>();

	internal static readonly string harmonyMethodFullName = typeof(HarmonyMethod).FullName;

	internal static readonly string harmonyAttributeFullName = typeof(HarmonyAttribute).FullName;

	internal static readonly string harmonyPatchAllFullName = typeof(HarmonyPatchAll).FullName;

	internal static readonly MethodInfo m_GetExecutingAssemblyReplacementTranspiler = SymbolExtensions.GetMethodInfo(() => GetExecutingAssemblyTranspiler(null));

	internal static readonly MethodInfo m_GetExecutingAssembly = SymbolExtensions.GetMethodInfo(() => Assembly.GetExecutingAssembly());

	internal static readonly MethodInfo m_GetExecutingAssemblyReplacement = SymbolExtensions.GetMethodInfo(() => GetExecutingAssemblyReplacement());

	internal static void DetourMethod(MethodBase method, MethodBase replacement)
	{
		lock (detours)
		{
			if (detours.TryGetValue(method, out var value))
			{
				value.Dispose();
			}
			detours[method] = DetourFactory.Current.CreateDetour(method, replacement);
		}
	}

	private static Assembly GetExecutingAssemblyReplacement()
	{
		StackFrame stackFrame = new StackTrace().GetFrames()?.Skip(1).FirstOrDefault();
		if (stackFrame != null)
		{
			MethodBase methodFromStackframe = Harmony.GetMethodFromStackframe(stackFrame);
			if ((object)methodFromStackframe != null)
			{
				return methodFromStackframe.Module.Assembly;
			}
		}
		return Assembly.GetExecutingAssembly();
	}

	internal static IEnumerable<CodeInstruction> GetExecutingAssemblyTranspiler(IEnumerable<CodeInstruction> instructions)
	{
		return instructions.MethodReplacer(m_GetExecutingAssembly, m_GetExecutingAssemblyReplacement);
	}

	public static MethodInfo CreateMethod(string name, Type returnType, List<KeyValuePair<string, Type>> parameters, Action<ILGenerator> generator)
	{
		Type[] parameterTypes = parameters.Select((KeyValuePair<string, Type> p) => p.Value).ToArray();
		if (AccessTools.IsMonoRuntime && !Tools.isWindows)
		{
			AssemblyName name2 = new AssemblyName("TempAssembly");
			AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(name2, AssemblyBuilderAccess.Run);
			ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule("TempModule");
			TypeBuilder typeBuilder = moduleBuilder.DefineType("TempType", TypeAttributes.Public);
			MethodBuilder methodBuilder = typeBuilder.DefineMethod(name, MethodAttributes.Public | MethodAttributes.Static, returnType, parameterTypes);
			for (int num = 0; num < parameters.Count; num++)
			{
				methodBuilder.DefineParameter(num + 1, ParameterAttributes.None, parameters[num].Key);
			}
			generator(methodBuilder.GetILGenerator());
			Type type = typeBuilder.CreateType();
			return type.GetMethod(name, BindingFlags.Static | BindingFlags.Public);
		}
		DynamicMethodDefinition dynamicMethodDefinition = new DynamicMethodDefinition(name, returnType, parameterTypes);
		for (int num2 = 0; num2 < parameters.Count; num2++)
		{
			dynamicMethodDefinition.Definition.Parameters[num2].Name = parameters[num2].Key;
		}
		generator(dynamicMethodDefinition.GetILGenerator());
		return dynamicMethodDefinition.Generate();
	}

	internal static MethodInfo GetPatchMethod(Type patchType, string attributeName)
	{
		MethodInfo methodInfo = patchType.GetMethods(AccessTools.all).FirstOrDefault((MethodInfo m) => m.GetCustomAttributes(inherit: true).Any((object a) => a.GetType().FullName == attributeName));
		if ((object)methodInfo == null)
		{
			string name = attributeName.Replace("HarmonyLib.Harmony", "");
			methodInfo = patchType.GetMethod(name, AccessTools.all);
		}
		return methodInfo;
	}

	internal static AssemblyBuilder DefineDynamicAssembly(string name)
	{
		AssemblyName name2 = new AssemblyName(name);
		return AppDomain.CurrentDomain.DefineDynamicAssembly(name2, AssemblyBuilderAccess.Run);
	}

	internal static List<AttributePatch> GetPatchMethods(Type type)
	{
		return (from attributePatch in AccessTools.GetDeclaredMethods(type).Select(AttributePatch.Create)
			where attributePatch != null
			select attributePatch).ToList();
	}

	internal static MethodBase GetOriginalMethod(this HarmonyMethod attr)
	{
		try
		{
			MethodType? methodType = attr.methodType;
			if (methodType.HasValue)
			{
				switch (methodType.GetValueOrDefault())
				{
				case MethodType.Normal:
					if (string.IsNullOrEmpty(attr.methodName))
					{
						return null;
					}
					return AccessTools.DeclaredMethod(attr.declaringType, attr.methodName, attr.argumentTypes);
				case MethodType.Getter:
					if (string.IsNullOrEmpty(attr.methodName))
					{
						return AccessTools.DeclaredIndexerGetter(attr.declaringType, attr.argumentTypes);
					}
					return AccessTools.DeclaredPropertyGetter(attr.declaringType, attr.methodName);
				case MethodType.Setter:
					if (string.IsNullOrEmpty(attr.methodName))
					{
						return AccessTools.DeclaredIndexerSetter(attr.declaringType, attr.argumentTypes);
					}
					return AccessTools.DeclaredPropertySetter(attr.declaringType, attr.methodName);
				case MethodType.Constructor:
					return AccessTools.DeclaredConstructor(attr.declaringType, attr.argumentTypes);
				case MethodType.StaticConstructor:
					return (from c in AccessTools.GetDeclaredConstructors(attr.declaringType)
						where c.IsStatic
						select c).FirstOrDefault();
				case MethodType.Enumerator:
				{
					if (string.IsNullOrEmpty(attr.methodName))
					{
						return null;
					}
					MethodInfo method = AccessTools.DeclaredMethod(attr.declaringType, attr.methodName, attr.argumentTypes);
					return AccessTools.EnumeratorMoveNext(method);
				}
				case MethodType.Async:
				{
					if (string.IsNullOrEmpty(attr.methodName))
					{
						return null;
					}
					MethodInfo method2 = AccessTools.DeclaredMethod(attr.declaringType, attr.methodName, attr.argumentTypes);
					return AccessTools.AsyncMoveNext(method2);
				}
				case MethodType.Finalizer:
					return AccessTools.DeclaredFinalizer(attr.declaringType);
				case MethodType.EventAdd:
					if (string.IsNullOrEmpty(attr.methodName))
					{
						return null;
					}
					return AccessTools.DeclaredEventAdder(attr.declaringType, attr.methodName);
				case MethodType.EventRemove:
					if (string.IsNullOrEmpty(attr.methodName))
					{
						return null;
					}
					return AccessTools.DeclaredEventRemover(attr.declaringType, attr.methodName);
				case MethodType.OperatorImplicit:
				case MethodType.OperatorExplicit:
				case MethodType.OperatorUnaryPlus:
				case MethodType.OperatorUnaryNegation:
				case MethodType.OperatorLogicalNot:
				case MethodType.OperatorOnesComplement:
				case MethodType.OperatorIncrement:
				case MethodType.OperatorDecrement:
				case MethodType.OperatorTrue:
				case MethodType.OperatorFalse:
				case MethodType.OperatorAddition:
				case MethodType.OperatorSubtraction:
				case MethodType.OperatorMultiply:
				case MethodType.OperatorDivision:
				case MethodType.OperatorModulus:
				case MethodType.OperatorBitwiseAnd:
				case MethodType.OperatorBitwiseOr:
				case MethodType.OperatorExclusiveOr:
				case MethodType.OperatorLeftShift:
				case MethodType.OperatorRightShift:
				case MethodType.OperatorEquality:
				case MethodType.OperatorInequality:
				case MethodType.OperatorGreaterThan:
				case MethodType.OperatorLessThan:
				case MethodType.OperatorGreaterThanOrEqual:
				case MethodType.OperatorLessThanOrEqual:
				case MethodType.OperatorComma:
				{
					string name = "op_" + attr.methodType.ToString().Replace("Operator", "");
					return AccessTools.DeclaredMethod(attr.declaringType, name, attr.argumentTypes);
				}
				}
			}
		}
		catch (AmbiguousMatchException ex)
		{
			throw new HarmonyException("Ambiguous match for HarmonyMethod[" + attr.Description() + "]", ex.InnerException ?? ex);
		}
		return null;
	}
}
