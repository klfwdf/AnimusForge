using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace HarmonyLib;

internal class InjectedParameter
{
	internal ParameterInfo parameterInfo;

	internal string realName;

	internal InjectionType injectionType;

	internal const string INSTANCE_PARAM = "__instance";

	internal const string ORIGINAL_METHOD_PARAM = "__originalMethod";

	internal const string ARGS_ARRAY_VAR = "__args";

	internal const string RESULT_VAR = "__result";

	internal const string RESULT_REF_VAR = "__resultRef";

	internal const string STATE_VAR = "__state";

	internal const string EXCEPTION_VAR = "__exception";

	internal const string RUN_ORIGINAL_VAR = "__runOriginal";

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

	internal InjectedParameter(MethodInfo method, ParameterInfo parameterInfo)
	{
		this.parameterInfo = parameterInfo;
		realName = CalculateRealName(method);
		injectionType = Type(realName);
	}

	private string CalculateRealName(MethodInfo method)
	{
		IEnumerable<HarmonyArgument> enumerable = method.GetArgumentAttributes();
		if ((object)method.DeclaringType != null)
		{
			enumerable = enumerable.Union(method.DeclaringType.GetArgumentAttributes());
		}
		HarmonyArgument argumentAttribute = parameterInfo.GetArgumentAttribute();
		if (argumentAttribute != null)
		{
			return argumentAttribute.OriginalName ?? parameterInfo.Name;
		}
		return enumerable.GetRealName(parameterInfo.Name, null) ?? parameterInfo.Name;
	}

	private static InjectionType Type(string name)
	{
		if (types.TryGetValue(name, out var value))
		{
			return value;
		}
		return InjectionType.Unknown;
	}
}
