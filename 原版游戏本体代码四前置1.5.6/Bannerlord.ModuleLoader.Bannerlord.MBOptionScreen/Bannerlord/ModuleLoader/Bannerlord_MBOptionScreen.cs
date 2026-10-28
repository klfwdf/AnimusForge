#define TRACE
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Bannerlord.BUTR.Shared.Helpers;
using Bannerlord.ModuleLoader.SubModuleWrappers;
using Bannerlord.ModuleLoader.SubModuleWrappers.Patches;
using Bannerlord.ModuleManager;
using HarmonyLib;
using TaleWorlds.MountAndBlade;

namespace Bannerlord.ModuleLoader;

public sealed class Bannerlord_MBOptionScreen : MBSubModuleBaseListWrapper
{
	private readonly ModuleInfoExtended? ModuleInfo;

	private readonly Harmony _harmonyWrappers;

	private bool ServiceRegistrationWasCalled { get; set; }

	public Bannerlord_MBOptionScreen()
	{
		ModuleInfo = ModuleInfoHelper.GetModuleByType(typeof(LoaderHelper));
		_harmonyWrappers = new Harmony(ModuleInfo?.Id ?? "Bannerlord.ModuleLoader ERROR");
		MBSubModuleBasePatch.Enable(_harmonyWrappers);
	}

	public override void OnServiceRegistration()
	{
		ServiceRegistrationWasCalled = true;
		Load();
		base.OnServiceRegistration();
	}

	public override void OnSubModuleLoad()
	{
		if (!ServiceRegistrationWasCalled)
		{
			Load();
		}
		base.OnSubModuleLoad();
	}

	private void Load()
	{
		if ((object)ModuleInfo == null)
		{
			Trace.TraceError("Failed to find LoaderSubModule!");
			return;
		}
		SubModuleInfoExtended subModuleInfoExtended = ModuleInfo.SubModules.FirstOrDefault((SubModuleInfoExtended x) => string.Equals(x.Name, "Bannerlord Module Loader", StringComparison.Ordinal));
		if ((object)subModuleInfoExtended == null)
		{
			Trace.TraceError("Failed to find 'Bannerlord Module Loader' in '{0}'!", ModuleInfo.Id);
			return;
		}
		string text = (subModuleInfoExtended.Tags.TryGetValue("LoaderFilter", out IReadOnlyList<string> value) ? value.FirstOrDefault() : string.Empty);
		if (text == null)
		{
			Trace.TraceError("Failed to find 'LoaderFilter' in 'Bannerlord Module Loader' in '{0}'!", ModuleInfo.Id);
			return;
		}
		IReadOnlyList<string> readOnlyList2;
		if (!subModuleInfoExtended.Tags.TryGetValue("LoaderSubModuleOrder", out IReadOnlyList<string> value2))
		{
			IReadOnlyList<string> readOnlyList = Array.Empty<string>();
			readOnlyList2 = readOnlyList;
		}
		else
		{
			readOnlyList2 = value2;
		}
		IReadOnlyList<string> source = readOnlyList2;
		Dictionary<string, int> indexLookup = source.Select((string v, int i) => (v: v, i: i)).ToDictionary(((string v, int i) x) => x.v, ((string v, int i) x) => x.i);
		IOrderedEnumerable<MBSubModuleBase> source2 = from x in LoaderHelper.LoadAllImplementations(text)
			orderby (!indexLookup.TryGetValue(x.GetType().FullName, out var value3)) ? int.MaxValue : value3
			select x;
		_subModules.AddRange(source2.Select((MBSubModuleBase x) => new MBSubModuleBaseWrapper(x)));
	}
}
