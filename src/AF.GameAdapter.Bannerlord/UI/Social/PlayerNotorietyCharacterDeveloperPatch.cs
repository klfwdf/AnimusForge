using System;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.CampaignSystem.ViewModelCollection.CharacterDeveloper;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.GauntletUI.Data;
using TaleWorlds.Library;
using TaleWorlds.TwoDimension;

namespace AnimusForge;

public static class PlayerNotorietyCharacterDeveloperPatch
{
	private const string ButtonId = "AnimusForgePlayerNotorietyButton";
	private static bool _patched;

	public static void EnsurePatched(Harmony harmony)
	{
		if (_patched)
		{
			return;
		}
		MethodInfo loadMovie = AccessTools.Method(typeof(GauntletMovie), nameof(GauntletMovie.Load));
		if (loadMovie == null)
		{
			Logger.Log("PlayerNotoriety", "[WARN] GauntletMovie.Load not found; character screen notoriety button skipped.");
			return;
		}
		(harmony ?? new Harmony("AnimusForge.player.notoriety.character")).Patch(loadMovie, postfix: new HarmonyMethod(typeof(PlayerNotorietyCharacterDeveloperPatch), nameof(LoadMoviePostfix)));
		_patched = true;
		Logger.Log("PlayerNotoriety", "[INFO] Character developer notoriety button patch enabled.");
	}

	public static void LoadMoviePostfix(string movieName, IViewModel datasource, IGauntletMovie __result)
	{
		try
		{
			if (!string.Equals(movieName, "CharacterDeveloper", StringComparison.Ordinal) || !(datasource is CharacterDeveloperVM))
			{
				return;
			}
			EnsureButton(__result?.RootWidget);
		}
		catch (Exception ex)
		{
			Logger.Log("PlayerNotoriety", "[WARN] Failed to inject character screen notoriety button: " + ex.Message);
		}
	}

	private static void EnsureButton(Widget root)
	{
		if (root == null || root.FindChild(ButtonId, includeAllChildren: true) != null)
		{
			return;
		}
		ButtonWidget button = new ButtonWidget(root.Context)
		{
			Id = ButtonId,
			WidthSizePolicy = SizePolicy.Fixed,
			HeightSizePolicy = SizePolicy.Fixed,
			SuggestedWidth = 230f,
			SuggestedHeight = 42f,
			HorizontalAlignment = HorizontalAlignment.Right,
			VerticalAlignment = VerticalAlignment.Top,
			MarginTop = 82f,
			MarginRight = 72f,
			Brush = root.Context.GetBrush("Popup.Done.Button.NineGrid") ?? root.Context.GetBrush("ButtonBrush2"),
			IsEnabled = true,
			IsVisible = true,
			DoNotAcceptEvents = false,
			DoNotPassEventsToChildren = true
		};
		button.ClickEventHandlers.Add(delegate
		{
			PlayerNotorietyBehavior.OpenPlayerNotorietyViewForExternal();
		});
		TextWidget textWidget = new TextWidget(root.Context)
		{
			WidthSizePolicy = SizePolicy.StretchToParent,
			HeightSizePolicy = SizePolicy.StretchToParent,
			Text = "玩家知名度/履历",
			Brush = root.Context.GetBrush("Popup.Button.Text") ?? root.Context.GetBrush("Encyclopedia.SubPage.Info.Text"),
			DoNotAcceptEvents = true
		};
		if (textWidget.Brush != null)
		{
			textWidget.Brush.FontSize = 18;
			textWidget.Brush.TextHorizontalAlignment = TextHorizontalAlignment.Center;
			textWidget.Brush.TextVerticalAlignment = TextVerticalAlignment.Center;
		}
		button.AddChild(textWidget);
		root.AddChild(button);
	}
}
