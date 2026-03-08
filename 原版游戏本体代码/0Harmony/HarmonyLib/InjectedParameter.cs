using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace HarmonyLib
{
	// Token: 0x02000027 RID: 39
	internal class InjectedParameter
	{
		// Token: 0x060000BD RID: 189 RVA: 0x00005512 File Offset: 0x00003712
		internal InjectedParameter(MethodInfo method, ParameterInfo parameterInfo)
		{
			this.parameterInfo = parameterInfo;
			this.realName = this.CalculateRealName(method);
			this.injectionType = InjectedParameter.Type(this.realName);
		}

		// Token: 0x060000BE RID: 190 RVA: 0x00005540 File Offset: 0x00003740
		private string CalculateRealName(MethodInfo method)
		{
			IEnumerable<HarmonyArgument> baseArgs = method.GetArgumentAttributes();
			if (method.DeclaringType != null)
			{
				baseArgs = baseArgs.Union(method.DeclaringType.GetArgumentAttributes());
			}
			HarmonyArgument arg = this.parameterInfo.GetArgumentAttribute();
			if (arg != null)
			{
				return arg.OriginalName ?? this.parameterInfo.Name;
			}
			return baseArgs.GetRealName(this.parameterInfo.Name, null) ?? this.parameterInfo.Name;
		}

		// Token: 0x060000BF RID: 191 RVA: 0x000055B4 File Offset: 0x000037B4
		private static InjectionType Type(string name)
		{
			InjectionType type;
			if (InjectedParameter.types.TryGetValue(name, out type))
			{
				return type;
			}
			return InjectionType.Unknown;
		}

		// Token: 0x0400006F RID: 111
		internal ParameterInfo parameterInfo;

		// Token: 0x04000070 RID: 112
		internal string realName;

		// Token: 0x04000071 RID: 113
		internal InjectionType injectionType;

		// Token: 0x04000072 RID: 114
		internal const string INSTANCE_PARAM = "__instance";

		// Token: 0x04000073 RID: 115
		internal const string ORIGINAL_METHOD_PARAM = "__originalMethod";

		// Token: 0x04000074 RID: 116
		internal const string ARGS_ARRAY_VAR = "__args";

		// Token: 0x04000075 RID: 117
		internal const string RESULT_VAR = "__result";

		// Token: 0x04000076 RID: 118
		internal const string RESULT_REF_VAR = "__resultRef";

		// Token: 0x04000077 RID: 119
		internal const string STATE_VAR = "__state";

		// Token: 0x04000078 RID: 120
		internal const string EXCEPTION_VAR = "__exception";

		// Token: 0x04000079 RID: 121
		internal const string RUN_ORIGINAL_VAR = "__runOriginal";

		// Token: 0x0400007A RID: 122
		private static readonly Dictionary<string, InjectionType> types = new Dictionary<string, InjectionType>
		{
			{
				"__instance",
				InjectionType.Instance
			},
			{
				"__originalMethod",
				InjectionType.OriginalMethod
			},
			{
				"__args",
				InjectionType.ArgsArray
			},
			{
				"__result",
				InjectionType.Result
			},
			{
				"__resultRef",
				InjectionType.ResultRef
			},
			{
				"__state",
				InjectionType.State
			},
			{
				"__exception",
				InjectionType.Exception
			},
			{
				"__runOriginal",
				InjectionType.RunOriginal
			}
		};
	}
}
