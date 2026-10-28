using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace HarmonyLib;

public class Patches
{
	public readonly ReadOnlyCollection<Patch> Prefixes;

	public readonly ReadOnlyCollection<Patch> Postfixes;

	public readonly ReadOnlyCollection<Patch> Transpilers;

	public readonly ReadOnlyCollection<Patch> Finalizers;

	public readonly ReadOnlyCollection<Patch> InnerPrefixes;

	public readonly ReadOnlyCollection<Patch> InnerPostfixes;

	public ReadOnlyCollection<string> Owners
	{
		get
		{
			HashSet<string> hashSet = new HashSet<string>();
			hashSet.UnionWith(Prefixes.Select((Patch p) => p.owner));
			hashSet.UnionWith(Postfixes.Select((Patch p) => p.owner));
			hashSet.UnionWith(Transpilers.Select((Patch p) => p.owner));
			hashSet.UnionWith(Finalizers.Select((Patch p) => p.owner));
			hashSet.UnionWith(InnerPrefixes.Select((Patch p) => p.owner));
			hashSet.UnionWith(InnerPostfixes.Select((Patch p) => p.owner));
			return hashSet.ToList().AsReadOnly();
		}
	}

	public Patches(Patch[] prefixes, Patch[] postfixes, Patch[] transpilers, Patch[] finalizers, Patch[] innerprefixes, Patch[] innerpostfixes)
	{
		if (prefixes == null)
		{
			prefixes = Array.Empty<Patch>();
		}
		if (postfixes == null)
		{
			postfixes = Array.Empty<Patch>();
		}
		if (transpilers == null)
		{
			transpilers = Array.Empty<Patch>();
		}
		if (finalizers == null)
		{
			finalizers = Array.Empty<Patch>();
		}
		if (innerprefixes == null)
		{
			innerprefixes = Array.Empty<Patch>();
		}
		if (innerpostfixes == null)
		{
			innerpostfixes = Array.Empty<Patch>();
		}
		Prefixes = prefixes.ToList().AsReadOnly();
		Postfixes = postfixes.ToList().AsReadOnly();
		Transpilers = transpilers.ToList().AsReadOnly();
		Finalizers = finalizers.ToList().AsReadOnly();
		InnerPrefixes = innerprefixes.ToList().AsReadOnly();
		InnerPostfixes = innerpostfixes.ToList().AsReadOnly();
	}
}
