using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using Mono.Cecil;
using MonoMod.Logs;

namespace MonoMod.Utils
{
	// Token: 0x0200088A RID: 2186
	[NullableContext(1)]
	[Nullable(new byte[] { 0, 1 })]
	internal sealed class DMDEmitDynamicMethodGenerator : DMDGenerator<DMDEmitDynamicMethodGenerator>
	{
		// Token: 0x06002CD2 RID: 11474 RVA: 0x00095AF4 File Offset: 0x00093CF4
		protected override MethodInfo GenerateCore(DynamicMethodDefinition dmd, [Nullable(2)] object context)
		{
			MethodBase orig = dmd.OriginalMethod;
			MethodDefinition definition = dmd.Definition;
			if (definition == null)
			{
				throw new InvalidOperationException();
			}
			MethodDefinition def = definition;
			Type[] argTypes;
			if (orig != null)
			{
				ParameterInfo[] args = orig.GetParameters();
				int offs = 0;
				if (!orig.IsStatic)
				{
					offs++;
					argTypes = new Type[args.Length + 1];
					argTypes[0] = orig.GetThisParamType();
				}
				else
				{
					argTypes = new Type[args.Length];
				}
				for (int i = 0; i < args.Length; i++)
				{
					argTypes[i + offs] = args[i].ParameterType;
				}
			}
			else
			{
				int offs2 = 0;
				if (def.HasThis)
				{
					offs2++;
					argTypes = new Type[def.Parameters.Count + 1];
					Type type2 = def.DeclaringType.ResolveReflection();
					if (type2.IsValueType)
					{
						type2 = type2.MakeByRefType();
					}
					argTypes[0] = type2;
				}
				else
				{
					argTypes = new Type[def.Parameters.Count];
				}
				for (int j = 0; j < def.Parameters.Count; j++)
				{
					argTypes[j + offs2] = def.Parameters[j].ParameterType.ResolveReflection();
				}
			}
			string text;
			if ((text = dmd.Name) == null)
			{
				FormatInterpolatedStringHandler formatInterpolatedStringHandler = new FormatInterpolatedStringHandler(5, 1);
				formatInterpolatedStringHandler.AppendLiteral("DMD<");
				formatInterpolatedStringHandler.AppendFormatted<object>(orig ?? def.GetID(null, null, true, true));
				formatInterpolatedStringHandler.AppendLiteral(">");
				text = DebugFormatter.Format(ref formatInterpolatedStringHandler);
			}
			string name = text;
			MethodInfo methodInfo = orig as MethodInfo;
			Type retType = ((methodInfo != null) ? methodInfo.ReturnType : null) ?? def.ReturnType.ResolveReflection();
			bool flag;
			MMDbgLog.DebugLogTraceStringHandler debugLogTraceStringHandler = new MMDbgLog.DebugLogTraceStringHandler(22, 3, ref flag);
			if (flag)
			{
				debugLogTraceStringHandler.AppendLiteral("new DynamicMethod: ");
				debugLogTraceStringHandler.AppendFormatted<Type>(retType);
				debugLogTraceStringHandler.AppendLiteral(" ");
				debugLogTraceStringHandler.AppendFormatted(name);
				debugLogTraceStringHandler.AppendLiteral("(");
				debugLogTraceStringHandler.AppendFormatted(string.Join(",", argTypes.Select(delegate(Type type)
				{
					if (type == null)
					{
						return null;
					}
					return type.ToString();
				}).ToArray<string>()));
				debugLogTraceStringHandler.AppendLiteral(")");
			}
			MMDbgLog.Trace(ref debugLogTraceStringHandler);
			if (orig != null)
			{
				MMDbgLog.DebugLogTraceStringHandler debugLogTraceStringHandler2 = new MMDbgLog.DebugLogTraceStringHandler(6, 1, ref flag);
				if (flag)
				{
					debugLogTraceStringHandler2.AppendLiteral("orig: ");
					debugLogTraceStringHandler2.AppendFormatted<MethodBase>(orig);
				}
				MMDbgLog.Trace(ref debugLogTraceStringHandler2);
			}
			MMDbgLog.DebugLogTraceStringHandler debugLogTraceStringHandler3 = new MMDbgLog.DebugLogTraceStringHandler(9, 3, ref flag);
			if (flag)
			{
				debugLogTraceStringHandler3.AppendLiteral("mdef: ");
				TypeReference returnType = def.ReturnType;
				debugLogTraceStringHandler3.AppendFormatted(((returnType != null) ? returnType.ToString() : null) ?? "NULL");
				debugLogTraceStringHandler3.AppendLiteral(" ");
				debugLogTraceStringHandler3.AppendFormatted(name);
				debugLogTraceStringHandler3.AppendLiteral("(");
				debugLogTraceStringHandler3.AppendFormatted(string.Join(",", def.Parameters.Select(delegate(ParameterDefinition arg)
				{
					string text2;
					if (arg == null)
					{
						text2 = null;
					}
					else
					{
						TypeReference parameterType = arg.ParameterType;
						text2 = ((parameterType != null) ? parameterType.ToString() : null);
					}
					return text2 ?? "NULL";
				}).ToArray<string>()));
				debugLogTraceStringHandler3.AppendLiteral(")");
			}
			MMDbgLog.Trace(ref debugLogTraceStringHandler3);
			DynamicMethod dm = new DynamicMethod(name, typeof(void), argTypes, ((orig != null) ? orig.DeclaringType : null) ?? typeof(DynamicMethodDefinition), true);
			DMDEmitDynamicMethodGenerator._DynamicMethod_returnType.SetValue(dm, retType);
			ILGenerator il = dm.GetILGenerator();
			_DMDEmit.Generate(dmd, dm, il);
			return dm;
		}

		// Token: 0x06002CD4 RID: 11476 RVA: 0x00095E58 File Offset: 0x00094058
		// Note: this type is marked as 'beforefieldinit'.
		static DMDEmitDynamicMethodGenerator()
		{
			FieldInfo field;
			if ((field = typeof(DynamicMethod).GetField("returnType", BindingFlags.Instance | BindingFlags.NonPublic)) == null && (field = typeof(DynamicMethod).GetField("_returnType", BindingFlags.Instance | BindingFlags.NonPublic)) == null && (field = typeof(DynamicMethod).GetField("m_returnType", BindingFlags.Instance | BindingFlags.NonPublic)) == null)
			{
				throw new InvalidOperationException("Cannot find returnType field on DynamicMethod");
			}
			DMDEmitDynamicMethodGenerator._DynamicMethod_returnType = field;
		}

		// Token: 0x04003A86 RID: 14982
		private static readonly FieldInfo _DynamicMethod_returnType;
	}
}
