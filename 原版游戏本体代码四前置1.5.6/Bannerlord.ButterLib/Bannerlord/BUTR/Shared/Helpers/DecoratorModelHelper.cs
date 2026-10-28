#define TRACE
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;

namespace Bannerlord.BUTR.Shared.Helpers;

[ExcludeFromCodeCoverage]
[DebuggerNonUserCode]
internal static class DecoratorModelHelper
{
	public static void AddDecoratorModel<TBase, TNew, TDef>(IGameStarter gameStarterObject, CampaignGameStarter gameStarter, Func<TBase, TNew> decoratorModelCtor) where TBase : GameModel where TNew : TBase where TDef : TBase, new()
	{
		TBase val = GetGameModel<TBase>(gameStarterObject);
		if (val == null)
		{
			Trace.TraceWarning("No default model of type \"" + typeof(TBase).FullName + "\" was found!");
			val = (TBase)(GameModel)new TDef();
		}
		TNew gameModel = decoratorModelCtor(val);
		gameStarter.AddModel(gameModel);
	}

	private static T? GetGameModel<T>(IGameStarter gameStarterObject) where T : GameModel
	{
		GameModel[] array = gameStarterObject.Models.ToArray();
		for (int num = array.Length - 1; num >= 0; num--)
		{
			if (array[num] is T result)
			{
				return result;
			}
		}
		return null;
	}
}
