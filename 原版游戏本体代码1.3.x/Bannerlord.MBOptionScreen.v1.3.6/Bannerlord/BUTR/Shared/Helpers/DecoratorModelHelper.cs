using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;

namespace Bannerlord.BUTR.Shared.Helpers
{
	// Token: 0x02000050 RID: 80
	[NullableContext(1)]
	[Nullable(0)]
	[ExcludeFromCodeCoverage]
	[DebuggerNonUserCode]
	internal static class DecoratorModelHelper
	{
		// Token: 0x060002A5 RID: 677 RVA: 0x00009514 File Offset: 0x00007714
		public static void AddDecoratorModel<[Nullable(0)] TBase, [Nullable(0)] TNew, [Nullable(0)] TDef>(IGameStarter gameStarterObject, CampaignGameStarter gameStarter, Func<TBase, TNew> decoratorModelCtor) where TBase : GameModel where TNew : TBase where TDef : TBase, new()
		{
			TBase currentModel = DecoratorModelHelper.GetGameModel<TBase>(gameStarterObject);
			if (currentModel == null)
			{
				Trace.TraceWarning("No default model of type \"" + typeof(TBase).FullName + "\" was found!");
				currentModel = (TBase)((object)Activator.CreateInstance<TDef>());
			}
			TNew newModel = decoratorModelCtor(currentModel);
			gameStarter.AddModel(newModel);
		}

		// Token: 0x060002A6 RID: 678 RVA: 0x00009578 File Offset: 0x00007778
		[return: Nullable(2)]
		private static T GetGameModel<[Nullable(0)] T>(IGameStarter gameStarterObject) where T : GameModel
		{
			GameModel[] models = gameStarterObject.Models.ToArray<GameModel>();
			for (int index = models.Length - 1; index >= 0; index--)
			{
				T gameModel = models[index] as T;
				if (gameModel != null)
				{
					return gameModel;
				}
			}
			return default(T);
		}
	}
}
