using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using HarmonyLib.BUTR.Extensions;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.GauntletUI.PrefabSystem;
using TaleWorlds.Library;

namespace Bannerlord.UIExtenderEx.Patches;

internal static class GauntletMoviePatch
{
	private static readonly ConcurrentDictionary<UIExtenderRuntime, List<string>> _widgetNames = new ConcurrentDictionary<UIExtenderRuntime, List<string>>();

	private static readonly ConcurrentDictionary<Type, Type[]> _widgetChildCache = new ConcurrentDictionary<Type, Type[]>();

	private static readonly AccessTools.FieldRef<GeneratedPrefabContext, Dictionary<string, Dictionary<string, CreateGeneratedWidget>>>? _generatedPrefabs = AccessTools2.FieldRefAccess<GeneratedPrefabContext, Dictionary<string, Dictionary<string, CreateGeneratedWidget>>>("_generatedPrefabs");

	public static void Register(UIExtenderRuntime runtime, string? autoGenWidgetName)
	{
		if (!string.IsNullOrEmpty(autoGenWidgetName))
		{
			_widgetNames.AddOrUpdate(runtime, (UIExtenderRuntime _) => new List<string>(1) { autoGenWidgetName }, delegate(UIExtenderRuntime _, List<string> list)
			{
				list.Add(autoGenWidgetName);
				return list;
			});
		}
	}

	public static void Deregister(UIExtenderRuntime runtime)
	{
		_widgetNames.TryRemove(runtime, out List<string> _);
	}

	public static void Patch(Harmony harmony)
	{
		MethodInfo methodInfo = AccessTools2.DeclaredMethod("TaleWorlds.GauntletUI.Data.GauntletMovie:Load");
		if ((object)methodInfo != null)
		{
			ParameterInfo[] parameters = methodInfo.GetParameters();
			if (parameters != null && parameters.Any((ParameterInfo x) => x.Name == "doNotUseGeneratedPrefabs"))
			{
				harmony.Patch(methodInfo, new HarmonyMethod(typeof(GauntletMoviePatch), "LoadPrefix"));
			}
		}
	}

	private static void LoadPrefix(WidgetFactory widgetFactory, string movieName, IViewModel? datasource, ref bool doNotUseGeneratedPrefabs)
	{
		HashSet<string> other = new HashSet<string>(UIExtender.GetAllRuntimes().SelectMany((UIExtenderRuntime x) => x.PrefabComponent.GetMoviesToPatch()));
		HashSet<string> hashSet = new HashSet<string>(GetAllInvolvedAutoGenNames(widgetFactory, movieName, datasource));
		if (hashSet.Overlaps(other))
		{
			doNotUseGeneratedPrefabs = true;
		}
		IEnumerable<string> source = _widgetNames.SelectMany<KeyValuePair<UIExtenderRuntime, List<string>>, string>((KeyValuePair<UIExtenderRuntime, List<string>> kv) => kv.Value);
		if (source.Contains(movieName))
		{
			doNotUseGeneratedPrefabs = true;
		}
		static IEnumerable<string> GetAllInvolvedAutoGenNames(WidgetFactory widgetFactory2, string key2, IViewModel? viewModel)
		{
			AccessTools.FieldRef<GeneratedPrefabContext, Dictionary<string, Dictionary<string, CreateGeneratedWidget>>>? generatedPrefabs = _generatedPrefabs;
			Dictionary<string, Dictionary<string, CreateGeneratedWidget>> dictionary = ((generatedPrefabs != null) ? generatedPrefabs(widgetFactory2.GeneratedPrefabContext) : null);
			if (dictionary != null)
			{
				string key = ((viewModel != null) ? viewModel.GetType().FullName : "Default");
				if (dictionary.TryGetValue(key2, out var value) && value.TryGetValue(key, out var value2))
				{
					Type type = AccessTools2.TypeByName(value2.Method.Name.Remove(0, "Create".Length));
					if ((object)type != null)
					{
						IEnumerable<Type> source2 = new List<Type> { type }.Concat(GetChildWidgets(type));
						IEnumerable<string> source3 = source2.Select((Type x) => x.Name);
						IEnumerable<string> source4 = source3.Where((string x) => x.Contains("__"));
						return source4.Select((string x) => x.Split(new string[1] { "__" }, StringSplitOptions.None)[0]);
					}
				}
			}
			return Enumerable.Empty<string>();
		}
		static IEnumerable<Type> GetChildWidgets(Type widgetType)
		{
			Type[] orAdd = _widgetChildCache.GetOrAdd(widgetType, (Type x) => (from fieldInfo in x.GetFields(AccessTools.all)
				select fieldInfo.FieldType into type
				where type.IsSubclassOf(typeof(Widget))
				select type).Distinct().ToArray());
			foreach (Type childWidgetType in orAdd.Where((Type x) => x != widgetType))
			{
				foreach (Type item in from x in GetChildWidgets(childWidgetType)
					where x != widgetType && x != childWidgetType
					select x)
				{
					yield return item;
				}
			}
		}
	}
}
