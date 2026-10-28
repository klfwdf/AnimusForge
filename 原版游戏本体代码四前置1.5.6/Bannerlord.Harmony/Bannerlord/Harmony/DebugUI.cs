using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Bannerlord.BUTR.Shared.Extensions;
using Bannerlord.BUTR.Shared.Helpers;
using HarmonyLib;
using TaleWorlds.Engine;

namespace Bannerlord.Harmony;

public class DebugUI
{
	private record HarmonyPatches
	{
		public ICollection<Patch> Prefixes { get; } = new List<Patch>();

		public ICollection<Patch> Postfixes { get; } = new List<Patch>();

		public ICollection<Patch> Transpilers { get; } = new List<Patch>();

		public ICollection<Patch> Finalizers { get; } = new List<Patch>();
	}

	private static readonly string _windowTitle = "Harmony Debug UI";

	private static readonly string _displayModeChoises = "Patch List\0Patch List Grouped by Modules\0";

	private int _displayModeChoisesIndex;

	private float _timeCounter;

	private readonly Dictionary<string, Dictionary<MethodBase, HarmonyPatches>> _patchesByModule = new Dictionary<string, Dictionary<MethodBase, HarmonyPatches>>();

	public bool Visible { get; set; }

	public void Update(float dt)
	{
		if (!Visible)
		{
			return;
		}
		Begin();
		AddButtons();
		DisplayPatchedMethods();
		End();
		_timeCounter += dt;
		if (!(_timeCounter >= 3f))
		{
			return;
		}
		_timeCounter = 0f;
		_patchesByModule.Clear();
		foreach (MethodBase allPatchedMethod in HarmonyLib.Harmony.GetAllPatchedMethods())
		{
			Patches patchInfo = HarmonyLib.Harmony.GetPatchInfo(allPatchedMethod);
			if ((object)allPatchedMethod == null || patchInfo == null)
			{
				continue;
			}
			foreach (Patch prefix in patchInfo.Prefixes)
			{
				if ((object)prefix.PatchMethod.DeclaringType != null)
				{
					ModuleInfoExtendedHelper moduleByType = ModuleInfoHelper.GetModuleByType(prefix.PatchMethod.DeclaringType);
					string key = (((object)moduleByType == null) ? "NOT WITHIN A MODULE" : moduleByType.Id);
					if (!_patchesByModule.TryGetValue(key, out Dictionary<MethodBase, HarmonyPatches> value))
					{
						value = new Dictionary<MethodBase, HarmonyPatches>();
						_patchesByModule.Add(key, value);
					}
					if (!value.TryGetValue(allPatchedMethod, out var value2))
					{
						value2 = new HarmonyPatches();
						value.Add(allPatchedMethod, value2);
					}
					value2.Prefixes.Add(prefix);
				}
			}
			foreach (Patch postfix in patchInfo.Postfixes)
			{
				if ((object)postfix.PatchMethod.DeclaringType != null)
				{
					ModuleInfoExtendedHelper moduleByType2 = ModuleInfoHelper.GetModuleByType(postfix.PatchMethod.DeclaringType);
					string key2 = (((object)moduleByType2 == null) ? "NOT WITHIN A MODULE" : moduleByType2.Id);
					if (!_patchesByModule.TryGetValue(key2, out Dictionary<MethodBase, HarmonyPatches> value3))
					{
						value3 = new Dictionary<MethodBase, HarmonyPatches>();
						_patchesByModule.Add(key2, value3);
					}
					if (!value3.TryGetValue(allPatchedMethod, out var value4))
					{
						value4 = new HarmonyPatches();
						value3.Add(allPatchedMethod, value4);
					}
					value4.Postfixes.Add(postfix);
				}
			}
			foreach (Patch transpiler in patchInfo.Transpilers)
			{
				if ((object)transpiler.PatchMethod.DeclaringType != null)
				{
					ModuleInfoExtendedHelper moduleByType3 = ModuleInfoHelper.GetModuleByType(transpiler.PatchMethod.DeclaringType);
					string key3 = (((object)moduleByType3 == null) ? "NOT WITHIN A MODULE" : moduleByType3.Id);
					if (!_patchesByModule.TryGetValue(key3, out Dictionary<MethodBase, HarmonyPatches> value5))
					{
						value5 = new Dictionary<MethodBase, HarmonyPatches>();
						_patchesByModule.Add(key3, value5);
					}
					if (!value5.TryGetValue(allPatchedMethod, out var value6))
					{
						value6 = new HarmonyPatches();
						value5.Add(allPatchedMethod, value6);
					}
					value6.Transpilers.Add(transpiler);
				}
			}
			foreach (Patch finalizer in patchInfo.Finalizers)
			{
				if ((object)finalizer.PatchMethod.DeclaringType != null)
				{
					ModuleInfoExtendedHelper moduleByType4 = ModuleInfoHelper.GetModuleByType(finalizer.PatchMethod.DeclaringType);
					string key4 = (((object)moduleByType4 == null) ? "NOT WITHIN A MODULE" : moduleByType4.Id);
					if (!_patchesByModule.TryGetValue(key4, out Dictionary<MethodBase, HarmonyPatches> value7))
					{
						value7 = new Dictionary<MethodBase, HarmonyPatches>();
						_patchesByModule.Add(key4, value7);
					}
					if (!value7.TryGetValue(allPatchedMethod, out var value8))
					{
						value8 = new HarmonyPatches();
						value7.Add(allPatchedMethod, value8);
					}
					value8.Finalizers.Add(finalizer);
				}
			}
		}
	}

	private void DisplayPatchedMethods()
	{
		switch (_displayModeChoisesIndex)
		{
		case 0:
			if (Imgui.TreeNode("Patch List"))
			{
				DisplayPatchList();
				Imgui.TreePop();
			}
			break;
		case 1:
			if (Imgui.TreeNode("Patch List Grouped by Modules"))
			{
				DisplayPatchListGroupedByModules();
				Imgui.TreePop();
			}
			break;
		}
	}

	private void DisplayPatchList()
	{
		foreach (var (originalMethod, patches) in _patchesByModule.SelectMany<KeyValuePair<string, Dictionary<MethodBase, HarmonyPatches>>, KeyValuePair<MethodBase, HarmonyPatches>>((KeyValuePair<string, Dictionary<MethodBase, HarmonyPatches>> x) => x.Value))
		{
			DisplayOriginalWithPatches(originalMethod, patches);
		}
	}

	private void DisplayPatchListGroupedByModules()
	{
		foreach (var (name, dictionary2) in _patchesByModule)
		{
			if (!Imgui.TreeNode(name))
			{
				continue;
			}
			foreach (var (originalMethod, patches) in dictionary2)
			{
				DisplayOriginalWithPatches(originalMethod, patches);
			}
			Imgui.TreePop();
		}
	}

	private static void DisplayOriginalWithPatches(MethodBase originalMethod, HarmonyPatches patches)
	{
		IEnumerable<Patch> enumerable = patches.Prefixes.Concat<Patch>(patches.Postfixes).Concat<Patch>(patches.Transpilers).Concat<Patch>(patches.Finalizers);
		if (!enumerable.Any() || !Imgui.TreeNode(originalMethod.FullDescription()))
		{
			return;
		}
		Imgui.Columns(7);
		Imgui.Text("Type");
		foreach (Patch prefix in patches.Prefixes)
		{
			Imgui.Text("Prefix");
		}
		foreach (Patch postfix in patches.Postfixes)
		{
			Imgui.Text("Postfix");
		}
		foreach (Patch transpiler in patches.Transpilers)
		{
			Imgui.Text("Transpiler");
		}
		foreach (Patch finalizer in patches.Finalizers)
		{
			Imgui.Text("Finalizer");
		}
		Imgui.NextColumn();
		Imgui.Text("Owner");
		foreach (Patch item in enumerable)
		{
			Imgui.Text(item.owner);
		}
		Imgui.NextColumn();
		Imgui.Text("Method");
		foreach (Patch item2 in enumerable)
		{
			Imgui.Text(item2.PatchMethod.DeclaringType.FullName + "." + item2.PatchMethod.Name);
		}
		Imgui.NextColumn();
		Imgui.Text("Index");
		foreach (Patch item3 in enumerable)
		{
			Imgui.Text($"{item3.index}");
		}
		Imgui.NextColumn();
		Imgui.Text("Priority");
		foreach (Patch item4 in enumerable)
		{
			Imgui.Text($"{item4.priority}");
		}
		Imgui.NextColumn();
		Imgui.Text("Before");
		foreach (Patch item5 in enumerable)
		{
			Imgui.Text(string.Join(",", item5.before) ?? "");
		}
		Imgui.NextColumn();
		Imgui.Text("After");
		Imgui.Separator();
		foreach (Patch item6 in enumerable)
		{
			Imgui.Text(string.Join(",", item6.after) ?? "");
		}
		Imgui.Columns();
		Imgui.TreePop();
	}

	private void Begin()
	{
		Imgui.BeginMainThreadScope();
		Imgui.Begin(_windowTitle);
	}

	private void AddButtons()
	{
		Imgui.NewLine();
		Imgui.SameLine(20f);
		if (Imgui.SmallButton("Close DebugUI"))
		{
			Visible = false;
		}
		Imgui.NewLine();
		Imgui.SameLine(20f);
		Imgui.Combo("Display Mode", ref _displayModeChoisesIndex, _displayModeChoises);
		Imgui.NewLine();
	}

	private void End()
	{
		Imgui.End();
		Imgui.EndMainThreadScope();
	}
}
