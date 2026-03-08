using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using MonoMod.Core;
using MonoMod.Utils;

namespace HarmonyLib
{
	// Token: 0x02000059 RID: 89
	internal static class PatchTools
	{
		// Token: 0x060001BC RID: 444 RVA: 0x0000CA6C File Offset: 0x0000AC6C
		internal static void DetourMethod(MethodBase method, MethodBase replacement)
		{
			Dictionary<MethodBase, ICoreDetour> obj = PatchTools.detours;
			lock (obj)
			{
				ICoreDetour detour;
				if (PatchTools.detours.TryGetValue(method, out detour))
				{
					detour.Dispose();
				}
				PatchTools.detours[method] = DetourFactory.Current.CreateDetour(method, replacement, true);
			}
		}

		// Token: 0x060001BD RID: 445 RVA: 0x0000CAD4 File Offset: 0x0000ACD4
		private static Assembly GetExecutingAssemblyReplacement()
		{
			StackFrame[] frames = new StackTrace().GetFrames();
			StackFrame frame = ((frames != null) ? frames.Skip(1).FirstOrDefault<StackFrame>() : null);
			if (frame != null)
			{
				MethodBase original = Harmony.GetMethodFromStackframe(frame);
				if (original != null)
				{
					return original.Module.Assembly;
				}
			}
			return Assembly.GetExecutingAssembly();
		}

		// Token: 0x060001BE RID: 446 RVA: 0x0000CB1D File Offset: 0x0000AD1D
		internal static IEnumerable<CodeInstruction> GetExecutingAssemblyTranspiler(IEnumerable<CodeInstruction> instructions)
		{
			return instructions.MethodReplacer(PatchTools.m_GetExecutingAssembly, PatchTools.m_GetExecutingAssemblyReplacement);
		}

		// Token: 0x060001BF RID: 447 RVA: 0x0000CB30 File Offset: 0x0000AD30
		public static MethodInfo CreateMethod(string name, Type returnType, List<KeyValuePair<string, Type>> parameters, Action<ILGenerator> generator)
		{
			Type[] parameterTypes = (from p in parameters
				select p.Value).ToArray<Type>();
			if (AccessTools.IsMonoRuntime && !Tools.isWindows)
			{
				AssemblyName assemblyName = new AssemblyName("TempAssembly");
				AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
				ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule("TempModule");
				TypeBuilder typeBuilder = moduleBuilder.DefineType("TempType", TypeAttributes.Public);
				MethodBuilder methodBuilder = typeBuilder.DefineMethod(name, MethodAttributes.FamANDAssem | MethodAttributes.Family | MethodAttributes.Static, returnType, parameterTypes);
				for (int i = 0; i < parameters.Count; i++)
				{
					methodBuilder.DefineParameter(i + 1, ParameterAttributes.None, parameters[i].Key);
				}
				generator(methodBuilder.GetILGenerator());
				Type createdType = typeBuilder.CreateType();
				return createdType.GetMethod(name, BindingFlags.Static | BindingFlags.Public);
			}
			DynamicMethodDefinition dynamicMethod = new DynamicMethodDefinition(name, returnType, parameterTypes);
			for (int j = 0; j < parameters.Count; j++)
			{
				dynamicMethod.Definition.Parameters[j].Name = parameters[j].Key;
			}
			generator(dynamicMethod.GetILGenerator());
			return dynamicMethod.Generate();
		}

		// Token: 0x060001C0 RID: 448 RVA: 0x0000CC68 File Offset: 0x0000AE68
		internal static MethodInfo GetPatchMethod(Type patchType, string attributeName)
		{
			Func<object, bool> <>9__1;
			MethodInfo method = patchType.GetMethods(AccessTools.all).FirstOrDefault(delegate(MethodInfo m)
			{
				IEnumerable<object> customAttributes = m.GetCustomAttributes(true);
				Func<object, bool> predicate;
				if ((predicate = <>9__1) == null)
				{
					predicate = (<>9__1 = (object a) => a.GetType().FullName == attributeName);
				}
				return customAttributes.Any(predicate);
			});
			if (method == null)
			{
				string methodName = attributeName.Replace("HarmonyLib.Harmony", "");
				method = patchType.GetMethod(methodName, AccessTools.all);
			}
			return method;
		}

		// Token: 0x060001C1 RID: 449 RVA: 0x0000CCC8 File Offset: 0x0000AEC8
		internal static AssemblyBuilder DefineDynamicAssembly(string name)
		{
			AssemblyName assemblyName = new AssemblyName(name);
			return AppDomain.CurrentDomain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
		}

		// Token: 0x060001C2 RID: 450 RVA: 0x0000CCE8 File Offset: 0x0000AEE8
		internal static List<AttributePatch> GetPatchMethods(Type type)
		{
			IEnumerable<MethodInfo> declaredMethods = AccessTools.GetDeclaredMethods(type);
			Func<MethodInfo, AttributePatch> selector;
			if ((selector = PatchTools.<>O.<0>__Create) == null)
			{
				selector = (PatchTools.<>O.<0>__Create = new Func<MethodInfo, AttributePatch>(AttributePatch.Create));
			}
			return (from attributePatch in declaredMethods.Select(selector)
				where attributePatch != null
				select attributePatch).ToList<AttributePatch>();
		}

		// Token: 0x060001C3 RID: 451 RVA: 0x0000CD44 File Offset: 0x0000AF44
		internal static MethodBase GetOriginalMethod(this HarmonyMethod attr)
		{
			try
			{
				MethodType? methodType = attr.methodType;
				if (methodType != null)
				{
					switch (methodType.GetValueOrDefault())
					{
					case MethodType.Normal:
						if (string.IsNullOrEmpty(attr.methodName))
						{
							return null;
						}
						return AccessTools.DeclaredMethod(attr.declaringType, attr.methodName, attr.argumentTypes, null);
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
						return AccessTools.DeclaredConstructor(attr.declaringType, attr.argumentTypes, false);
					case MethodType.StaticConstructor:
						return (from c in AccessTools.GetDeclaredConstructors(attr.declaringType, null)
							where c.IsStatic
							select c).FirstOrDefault<ConstructorInfo>();
					case MethodType.Enumerator:
					{
						if (string.IsNullOrEmpty(attr.methodName))
						{
							return null;
						}
						MethodInfo enumMethod = AccessTools.DeclaredMethod(attr.declaringType, attr.methodName, attr.argumentTypes, null);
						return AccessTools.EnumeratorMoveNext(enumMethod);
					}
					case MethodType.Async:
					{
						if (string.IsNullOrEmpty(attr.methodName))
						{
							return null;
						}
						MethodInfo asyncMethod = AccessTools.DeclaredMethod(attr.declaringType, attr.methodName, attr.argumentTypes, null);
						return AccessTools.AsyncMoveNext(asyncMethod);
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
						string methodName = "op_" + attr.methodType.ToString().Replace("Operator", "");
						return AccessTools.DeclaredMethod(attr.declaringType, methodName, attr.argumentTypes, null);
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

		// Token: 0x04000130 RID: 304
		private static readonly Dictionary<MethodBase, ICoreDetour> detours = new Dictionary<MethodBase, ICoreDetour>();

		// Token: 0x04000131 RID: 305
		internal static readonly string harmonyMethodFullName = typeof(HarmonyMethod).FullName;

		// Token: 0x04000132 RID: 306
		internal static readonly string harmonyAttributeFullName = typeof(HarmonyAttribute).FullName;

		// Token: 0x04000133 RID: 307
		internal static readonly string harmonyPatchAllFullName = typeof(HarmonyPatchAll).FullName;

		// Token: 0x04000134 RID: 308
		internal static readonly MethodInfo m_GetExecutingAssemblyReplacementTranspiler = SymbolExtensions.GetMethodInfo(Expression.Lambda<Action>(Expression.Call(null, methodof(PatchTools.GetExecutingAssemblyTranspiler(IEnumerable<CodeInstruction>)), new Expression[] { Expression.Constant(null, typeof(IEnumerable<CodeInstruction>)) }), Array.Empty<ParameterExpression>()));

		// Token: 0x04000135 RID: 309
		internal static readonly MethodInfo m_GetExecutingAssembly = SymbolExtensions.GetMethodInfo(Expression.Lambda<Action>(Expression.Call(null, methodof(Assembly.GetExecutingAssembly()), Array.Empty<Expression>()), Array.Empty<ParameterExpression>()));

		// Token: 0x04000136 RID: 310
		internal static readonly MethodInfo m_GetExecutingAssemblyReplacement = SymbolExtensions.GetMethodInfo(Expression.Lambda<Action>(Expression.Call(null, methodof(PatchTools.GetExecutingAssemblyReplacement()), Array.Empty<Expression>()), Array.Empty<ParameterExpression>()));

		// Token: 0x0200005A RID: 90
		[CompilerGenerated]
		private static class <>O
		{
			// Token: 0x04000137 RID: 311
			public static Func<MethodInfo, AttributePatch> <0>__Create;
		}
	}
}
