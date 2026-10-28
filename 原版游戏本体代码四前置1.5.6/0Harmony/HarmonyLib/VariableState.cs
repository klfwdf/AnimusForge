using System;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace HarmonyLib;

internal class VariableState
{
	private readonly Dictionary<InjectionType, LocalBuilder> injected = new Dictionary<InjectionType, LocalBuilder>();

	private readonly Dictionary<string, LocalBuilder> other = new Dictionary<string, LocalBuilder>();

	public LocalBuilder this[InjectionType type]
	{
		get
		{
			if (injected.TryGetValue(type, out var value))
			{
				return value;
			}
			throw new ArgumentException($"VariableState: variable of type {type} not found");
		}
		set
		{
			injected[type] = value;
		}
	}

	public LocalBuilder this[string name]
	{
		get
		{
			if (other.TryGetValue(name, out var value))
			{
				return value;
			}
			throw new ArgumentException("VariableState: variable named '" + name + "' not found");
		}
		set
		{
			other[name] = value;
		}
	}

	public void Add(InjectionType type, LocalBuilder local)
	{
		injected[type] = local;
	}

	public void Add(string name, LocalBuilder local)
	{
		other[name] = local;
	}

	public bool TryGetValue(InjectionType type, out LocalBuilder local)
	{
		return injected.TryGetValue(type, out local);
	}

	public bool TryGetValue(string name, out LocalBuilder local)
	{
		return other.TryGetValue(name, out local);
	}
}
