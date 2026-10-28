using System;
using System.Collections.Generic;
using System.Reflection;
using Bannerlord.UIExtenderEx.Extensions;
using Bannerlord.UIExtenderEx.ViewModels;
using HarmonyLib;
using HarmonyLib.BUTR.Extensions;
using TaleWorlds.Library;

namespace Bannerlord.UIExtenderEx.Patches;

internal static class ViewModelPatch
{
	public static void Patch(Harmony harmony)
	{
		harmony.TryPatch(AccessTools2.DeclaredConstructor(typeof(ViewModel)), AccessTools2.DeclaredMethod(typeof(ViewModelPatch), "ViewModelCtorPrefix"));
		harmony.Patch(AccessTools2.Method(typeof(ViewModel), "ExecuteCommand"), new HarmonyMethod(typeof(ViewModelPatch), "ExecuteCommandPatch"));
	}

	private static bool ViewModelCtorPrefix(ViewModel __instance, ref Type ____type, ref object ____propertiesAndMethods)
	{
		if (__instance is BUTRViewModel)
		{
			ViewModelExtensions.DataSourceTypeBindingPropertiesCollectionCtorDelegate dataSourceTypeBindingPropertiesCollectionCtor = ViewModelExtensions.DataSourceTypeBindingPropertiesCollectionCtor;
			if (dataSourceTypeBindingPropertiesCollectionCtor != null)
			{
				____type = __instance.GetType();
				____propertiesAndMethods = dataSourceTypeBindingPropertiesCollectionCtor(new Dictionary<string, PropertyInfo>(), new Dictionary<string, MethodInfo>());
				return false;
			}
		}
		return true;
	}

	private static bool ExecuteCommandPatch(object __instance, string commandName, object[] parameters)
	{
		if (__instance is ViewModelWrapper viewModelWrapper)
		{
			ViewModel viewModel = viewModelWrapper.Object;
			if (viewModel != null)
			{
				viewModel.ExecuteCommand(commandName, parameters);
				return false;
			}
		}
		return true;
	}
}
