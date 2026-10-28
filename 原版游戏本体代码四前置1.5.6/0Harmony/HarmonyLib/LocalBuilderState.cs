using System.Collections.Generic;
using System.Reflection.Emit;

namespace HarmonyLib;

internal class LocalBuilderState
{
	private readonly Dictionary<string, LocalBuilder> locals = new Dictionary<string, LocalBuilder>();

	public LocalBuilder this[string key]
	{
		get
		{
			return locals[key];
		}
		set
		{
			locals[key] = value;
		}
	}

	public void Add(string key, LocalBuilder local)
	{
		locals[key] = local;
	}

	public bool TryGetValue(string key, out LocalBuilder local)
	{
		return locals.TryGetValue(key, out local);
	}
}
