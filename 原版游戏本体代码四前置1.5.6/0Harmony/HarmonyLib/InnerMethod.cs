using System;
using System.Linq;
using System.Reflection;

namespace HarmonyLib;

[Serializable]
public class InnerMethod
{
	[NonSerialized]
	private MethodInfo method;

	private int methodToken;

	private string moduleGUID;

	public int[] positions;

	public MethodInfo Method
	{
		get
		{
			if ((object)method == null)
			{
				method = AccessTools.GetMethodByModuleAndToken(moduleGUID, methodToken);
			}
			return method;
		}
		set
		{
			method = value;
			methodToken = method.MetadataToken;
			moduleGUID = method.Module.ModuleVersionId.ToString();
		}
	}

	public InnerMethod(MethodInfo method, params int[] positions)
	{
		if (method == null)
		{
			throw new ArgumentNullException("method");
		}
		if (positions.Any((int p) => p == 0))
		{
			throw new ArgumentException("positions cannot contain zeros");
		}
		Method = method;
		this.positions = positions;
	}

	internal InnerMethod(int methodToken, string moduleGUID, int[] positions)
	{
		this.methodToken = methodToken;
		this.moduleGUID = moduleGUID;
		this.positions = positions;
	}
}
